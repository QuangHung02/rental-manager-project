using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.DTOs;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class InvoiceService
{
    private readonly InvoiceCalculationService _calculationService = new();

    public List<Invoice> GetAll()
    {
        using var db = DbContextFactory.Create();
        return db.Invoices
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.Room).ThenInclude(x => x!.RoomTenants).ThenInclude(x => x.Tenant)
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .AsNoTracking()
            .OrderByDescending(x => x.BillingMonth)
            .ThenBy(x => x.Room!.RoomName)
            .ToList();
    }

    public Invoice Generate(int roomId, string billingMonth, bool recreate = false)
    {
        using var db = DbContextFactory.Create();
        var existing = db.Invoices.Include(x => x.Items).FirstOrDefault(x => x.RoomId == roomId && x.BillingMonth == billingMonth);
        if (existing is not null)
        {
            if (!recreate)
            {
                throw new ValidationException("Hóa đơn của phòng này trong tháng đã chọn đã tồn tại.");
            }

            if (existing.Status is InvoiceStatus.Issued or InvoiceStatus.Partial or InvoiceStatus.Paid)
            {
                throw new ValidationException("Không thể tạo lại hóa đơn đã chốt hoặc đã thanh toán.");
            }

            EnsureMeterReadingsPresent(db, roomId, billingMonth);
            db.InvoiceItems.RemoveRange(existing.Items);
            db.Invoices.Remove(existing);
            db.SaveChanges();
        }

        EnsureRoomHasActiveTenantForBillingMonth(db, roomId, billingMonth);
        EnsureRoomHasRepresentativeIfOccupied(db, roomId);
        EnsureMeterReadingsPresent(db, roomId, billingMonth);
        var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    public void GenerateAll(string billingMonth)
    {
        GenerateAllEligible(billingMonth);
    }

    public InvoiceGenerationResult GenerateAllEligible(string billingMonth)
    {
        using var db = DbContextFactory.Create();
        var result = new InvoiceGenerationResult();
        var rooms = db.Rooms
            .Include(x => x.Property)
            .Where(x => x.Status != RoomStatus.Inactive)
            .OrderBy(x => x.Property!.Name)
            .ThenBy(x => x.RoomName)
            .ToList();

        foreach (var room in rooms)
        {
            var reason = GetSkipReason(db, room.Id, billingMonth);
            if (reason is not null)
            {
                result.SkippedRooms.Add(new InvoiceGenerationSkipRow
                {
                    PropertyName = room.Property?.Name ?? string.Empty,
                    RoomName = room.RoomName,
                    Reason = reason
                });
                continue;
            }

            try
            {
                var invoice = _calculationService.BuildDraftInvoice(room.Id, billingMonth);
                db.Invoices.Add(invoice);
                result.CreatedCount++;
            }
            catch (ValidationException ex)
            {
                result.SkippedRooms.Add(new InvoiceGenerationSkipRow
                {
                    PropertyName = room.Property?.Name ?? string.Empty,
                    RoomName = room.RoomName,
                    Reason = ex.Message
                });
            }
        }

        db.SaveChanges();
        return result;
    }

    public List<InvoiceReadinessRow> GetReadiness(string billingMonth)
    {
        using var db = DbContextFactory.Create();
        var rooms = db.Rooms
            .Include(x => x.Property)
            .Where(x => x.Status != RoomStatus.Inactive)
            .AsNoTracking()
            .OrderBy(x => x.Property!.Name)
            .ThenBy(x => x.RoomName)
            .ToList();

        return rooms.Select(room =>
        {
            var status = GetSkipReason(db, room.Id, billingMonth) ?? "Đủ dữ liệu";

            return new InvoiceReadinessRow
            {
                RoomId = room.Id,
                PropertyName = room.Property?.Name ?? string.Empty,
                RoomName = room.RoomName,
                StatusText = status
            };
        }).ToList();
    }

    public int GenerateReady(string billingMonth)
    {
        var readyRoomIds = GetReadiness(billingMonth)
            .Where(x => x.StatusText == "Đủ dữ liệu")
            .Select(x => x.RoomId)
            .ToList();

        if (readyRoomIds.Count == 0)
        {
            readyRoomIds = GetReadiness(billingMonth)
                .Where(x => x.StatusText == "Đủ dữ liệu")
                .Select(x => x.RoomId)
                .ToList();
        }

        using var db = DbContextFactory.Create();
        var createdCount = 0;
        foreach (var roomId in readyRoomIds)
        {
            if (db.Invoices.Any(x => x.RoomId == roomId && x.BillingMonth == billingMonth))
            {
                continue;
            }

            EnsureRoomHasActiveTenantForBillingMonth(db, roomId, billingMonth);
            EnsureRoomHasRepresentativeIfOccupied(db, roomId);
            EnsureMeterReadingsPresent(db, roomId, billingMonth);
            var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
            db.Invoices.Add(invoice);
            createdCount++;
        }

        db.SaveChanges();
        return createdCount;
    }

    public void Issue(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices.Find(invoiceId) ?? throw new ValidationException("Không tìm thấy hóa đơn đã chọn.");
        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedDate = DateTime.Today;
            invoice.UpdatedAt = DateTime.Now;
            db.SaveChanges();
        }
    }

    public void Cancel(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices.Find(invoiceId) ?? throw new ValidationException("Không tìm thấy hóa đơn đã chọn.");
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }

    public string CopyText(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.Room).ThenInclude(x => x!.RoomTenants).ThenInclude(x => x.Tenant)
            .Include(x => x.Items)
            .First(x => x.Id == invoiceId);
        var representative = invoice.Room!.RoomTenants.FirstOrDefault(x => x.Status == RoomTenantStatus.Active && x.IsRepresentative)?.Tenant?.FullName ?? "";
        var lines = new List<string>
        {
            $"Invoice {invoice.BillingMonth}",
            $"Property: {invoice.Room.Property?.Name}",
            $"Room: {invoice.Room.RoomName}",
            $"Representative: {representative}"
        };
        lines.AddRange(invoice.Items.Select(x => $"{x.ItemName}: {x.Amount:N0}"));
        lines.Add($"Total: {invoice.TotalAmount:N0}");
        lines.Add($"Paid: {invoice.PaidAmount:N0}");
        lines.Add($"Remaining: {invoice.RemainingAmount:N0}");
        return string.Join(Environment.NewLine, lines);
    }

    private static void EnsureRoomHasRepresentativeIfOccupied(RentalManagerDbContext db, int roomId)
    {
        var room = db.Rooms.Find(roomId) ?? throw new ValidationException("Không tìm thấy phòng.");
        if (room.Status != RoomStatus.Occupied)
        {
            return;
        }

        var hasRepresentative = db.RoomTenants.Any(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active && x.IsRepresentative);
        if (!hasRepresentative)
        {
            throw new ValidationException("Phòng này chưa có người đại diện. Vui lòng chọn người đại diện trước khi tạo hóa đơn.");
        }
    }

    private static void EnsureRoomHasActiveTenantForBillingMonth(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        if (!HasActiveTenantForBillingMonth(db, roomId, billingMonth))
        {
            throw new ValidationException("Không thể tạo hóa đơn vì phòng chưa có người thuê.");
        }
    }

    private static string? GetSkipReason(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        if (db.Invoices.Any(x => x.RoomId == roomId && x.BillingMonth == billingMonth))
        {
            return "Đã có hóa đơn tháng này";
        }

        if (!HasActiveTenantForBillingMonth(db, roomId, billingMonth))
        {
            return "Phòng chưa có người thuê";
        }

        var hasRepresentative = db.RoomTenants.Any(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active && x.IsRepresentative);
        if (!hasRepresentative)
        {
            return "Chưa có người đại diện/liên hệ chính";
        }

        var missingMeterReadingExists = db.RoomFeeConfigs
            .Include(x => x.FeeType)
            .Where(x => x.RoomId == roomId && x.Enabled && x.FeeType!.IsActive && x.CalculationType == CalculationType.Meter)
            .Any(config => !db.MeterReadings.Any(reading =>
                reading.RoomId == roomId &&
                reading.FeeTypeId == config.FeeTypeId &&
                reading.BillingMonth == billingMonth));

        if (!missingMeterReadingExists)
        {
            return null;
        }

        return "Thiếu chỉ số";
    }

    private static bool HasActiveTenantForBillingMonth(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        var monthStart = DateTime.TryParse($"{billingMonth}-01", out var parsedMonth)
            ? parsedMonth.Date
            : DateTime.Today.Date;
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return db.RoomTenants.Any(x =>
            x.RoomId == roomId &&
            x.Status == RoomTenantStatus.Active &&
            x.StartDate <= monthEnd &&
            (x.EndDate == null || x.EndDate >= monthStart));
    }

    private static void EnsureMeterReadingsPresent(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        var missingReadingExists = db.RoomFeeConfigs
            .Include(x => x.FeeType)
            .Where(x => x.RoomId == roomId && x.Enabled && x.FeeType!.IsActive && x.CalculationType == CalculationType.Meter)
            .Any(config => !db.MeterReadings.Any(reading =>
                reading.RoomId == roomId &&
                reading.FeeTypeId == config.FeeTypeId &&
                reading.BillingMonth == billingMonth));

        if (missingReadingExists)
        {
            throw new ValidationException("Phòng này còn thiếu chỉ số điện/nước cho kỳ hóa đơn đã chọn.");
        }
    }
}
