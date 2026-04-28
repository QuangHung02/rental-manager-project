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

        EnsureRoomHasRepresentativeIfOccupied(db, roomId);
        EnsureMeterReadingsPresent(db, roomId, billingMonth);
        var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    public void GenerateAll(string billingMonth)
    {
        using var db = DbContextFactory.Create();
        var roomIds = db.Rooms.Where(x => x.Status != RoomStatus.Inactive).Select(x => x.Id).ToList();
        foreach (var roomId in roomIds)
        {
            if (!db.Invoices.Any(x => x.RoomId == roomId && x.BillingMonth == billingMonth))
            {
                EnsureRoomHasRepresentativeIfOccupied(db, roomId);
                EnsureMeterReadingsPresent(db, roomId, billingMonth);
                var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
                db.Invoices.Add(invoice);
            }
        }

        db.SaveChanges();
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
            var hasInvoice = db.Invoices.Any(x => x.RoomId == room.Id && x.BillingMonth == billingMonth);
            var missingRepresentative = room.Status == RoomStatus.Occupied && !db.RoomTenants.Any(x => x.RoomId == room.Id && x.Status == RoomTenantStatus.Active && x.IsRepresentative);
            var meterConfigs = db.RoomFeeConfigs.Where(x => x.RoomId == room.Id && x.Enabled && x.CalculationType == CalculationType.Meter).ToList();
            var missingReading = meterConfigs.Any(config => !db.MeterReadings.Any(r => r.RoomId == room.Id && r.FeeTypeId == config.FeeTypeId && r.BillingMonth == billingMonth));
            var status = hasInvoice ? "Đã có hóa đơn" : missingReading ? "Thiếu chỉ số" : "Đủ dữ liệu";

            status = hasInvoice ? "Đã có hóa đơn" : missingRepresentative ? "Chưa có người đại diện" : missingReading ? "Thiếu chỉ số" : "Đủ dữ liệu";

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
        foreach (var roomId in readyRoomIds)
        {
            var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
            db.Invoices.Add(invoice);
        }

        db.SaveChanges();
        return readyRoomIds.Count;
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

    private static void EnsureMeterReadingsPresent(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        var missingReadingExists = db.RoomFeeConfigs
            .Where(x => x.RoomId == roomId && x.Enabled && x.CalculationType == CalculationType.Meter)
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
