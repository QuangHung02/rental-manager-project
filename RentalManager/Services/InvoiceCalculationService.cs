using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class InvoiceCalculationService
{
    public Invoice BuildDraftInvoice(int roomId, string billingMonth, decimal discount = 0, decimal extraAmount = 0)
    {
        using var db = DbContextFactory.Create();
        var room = db.Rooms
            .Include(x => x.Property)
            .Include(x => x.RoomTenants)
            .Include(x => x.FeeConfigs)
            .ThenInclude(x => x.FeeType)
            .FirstOrDefault(x => x.Id == roomId) ?? throw new ValidationException("Không tìm thấy phòng.");

        var invoice = new Invoice
        {
            RoomId = room.Id,
            BillingMonth = billingMonth,
            Status = InvoiceStatus.Draft,
            Discount = discount,
            ExtraAmount = extraAmount,
            IssuedDate = DateTime.Today,
            Items = new List<InvoiceItem>
            {
                new()
                {
                    ItemName = "Tiền phòng",
                    CalculationType = CalculationType.Fixed,
                    Quantity = 1,
                    Unit = "tháng",
                    UnitPrice = room.BaseRent,
                    Amount = room.BaseRent
                }
            }
        };

        var activeTenantCount = CountTenantsForBillingMonth(db, room.Id, billingMonth);
        foreach (var config in room.FeeConfigs.Where(x => x.Enabled && x.FeeType?.IsActive == true))
        {
            var feeType = config.FeeType ?? throw new ValidationException("Không tìm thấy loại phí.");
            LogInvoiceRoomFeeConfig(room, config, feeType);
            var item = BuildFeeItem(db, room.Id, billingMonth, activeTenantCount, config, feeType);
            invoice.Items.Add(item);
        }

        Recalculate(invoice);
        return invoice;
    }

    public void Recalculate(Invoice invoice)
    {
        invoice.Subtotal = invoice.Items.Sum(x => x.Amount);
        invoice.TotalAmount = invoice.Subtotal - invoice.Discount + invoice.ExtraAmount;
        invoice.PaidAmount = invoice.Payments.Sum(x => x.Amount);
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
        invoice.UpdatedAt = DateTime.Now;
    }

    private static InvoiceItem BuildFeeItem(RentalManagerDbContext db, int roomId, string billingMonth, int activeTenantCount, RoomFeeConfig config, FeeType feeType)
    {
        var unitPrice = 0m;
        var quantity = config.Quantity ?? 1;
        var amount = 0m;
        var note = config.Note;

        switch (config.CalculationType)
        {
            case CalculationType.Fixed:
                quantity = 1;
                unitPrice = GetFixedAmount(config, feeType, roomId);
                amount = unitPrice;
                break;
            case CalculationType.Meter:
                var reading = db.MeterReadings.FirstOrDefault(x => x.RoomId == roomId && x.FeeTypeId == feeType.Id && x.BillingMonth == billingMonth);
                if (reading is null)
                {
                    throw new ValidationException("Phòng này còn thiếu chỉ số điện/nước cho kỳ hóa đơn đã chọn.");
                }

                quantity = reading.UsageAmount;
                unitPrice = reading.UnitPriceSnapshot;
                amount = reading.Amount;
                break;
            case CalculationType.PerPerson:
                unitPrice = GetUnitPrice(config, feeType, roomId);
                quantity = activeTenantCount;
                amount = quantity * unitPrice;
                break;
            case CalculationType.PerUnit:
                unitPrice = GetUnitPrice(config, feeType, roomId);
                amount = quantity * unitPrice;
                break;
            case CalculationType.Manual:
                amount = config.FixedAmount ?? 0;
                quantity = 1;
                unitPrice = amount;
                break;
        }

        return new InvoiceItem
        {
            FeeTypeId = feeType.Id,
            ItemName = feeType.Name,
            CalculationType = config.CalculationType,
            Quantity = quantity,
            Unit = feeType.DefaultUnit,
            UnitPrice = unitPrice,
            Amount = amount,
            Note = note
        };
    }

    private static decimal GetUnitPrice(RoomFeeConfig config, FeeType feeType, int roomId)
    {
        if (config.UnitPrice is not null)
        {
            return config.UnitPrice.Value;
        }

        if (config.CalculationType != feeType.DefaultCalculationType)
        {
            throw new ValidationException(BuildInvalidDefaultPriceMessage(config, feeType, roomId));
        }

        return feeType.DefaultUnitPrice;
    }

    private static decimal GetFixedAmount(RoomFeeConfig config, FeeType feeType, int roomId)
    {
        if (config.FixedAmount is not null)
        {
            return config.FixedAmount.Value;
        }

        if (config.CalculationType != feeType.DefaultCalculationType)
        {
            throw new ValidationException(BuildInvalidDefaultPriceMessage(config, feeType, roomId));
        }

        return feeType.DefaultUnitPrice;
    }

    private static string BuildInvalidDefaultPriceMessage(RoomFeeConfig config, FeeType feeType, int roomId)
    {
        var roomName = config.Room?.RoomName;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            using var db = DbContextFactory.Create();
            roomName = db.Rooms
                .AsNoTracking()
                .Where(x => x.Id == roomId)
                .Select(x => x.RoomName)
                .FirstOrDefault();
        }

        return $"{roomName ?? "Phòng"} - {feeType.DisplayName}: Không thể dùng giá mặc định khi cách tính khác với loại phí gốc. Vui lòng nhập giá riêng.";
    }

    private static void LogInvoiceRoomFeeConfig(Room room, RoomFeeConfig config, FeeType feeType)
    {
        var usesDefaultPrice = config.CalculationType != CalculationType.Manual &&
                               config.CalculationType == feeType.DefaultCalculationType &&
                               config.CalculationType switch
                               {
                                   CalculationType.Fixed => config.FixedAmount is null,
                                   CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit => config.UnitPrice is null,
                                   _ => false
                               };

        Debug.WriteLine(
            "Invoice RoomFeeConfig: " +
            $"RoomFeeConfigId={config.Id}; " +
            $"RoomId={config.RoomId}; " +
            $"Room={room.Property?.Name} - {room.RoomName}; " +
            $"FeeTypeId={config.FeeTypeId}; " +
            $"FeeType={feeType.DisplayName}; " +
            $"FeeTypeCalculationType={feeType.DefaultCalculationType}; " +
            $"RoomFeeConfigCalculationType={config.CalculationType}; " +
            $"UseDefaultPrice={usesDefaultPrice}; " +
            $"UnitPrice={config.UnitPrice?.ToString() ?? "null"}; " +
            $"FixedAmount={config.FixedAmount?.ToString() ?? "null"}; " +
            $"Quantity={config.Quantity?.ToString() ?? "null"}; " +
            $"RoomFeeConfigIsEnabled={config.Enabled}; " +
            $"FeeTypeIsEnabled={feeType.IsActive}; " +
            $"EffectiveStatus={config.Enabled && feeType.IsActive}");
    }

    private static int CountTenantsForBillingMonth(RentalManagerDbContext db, int roomId, string billingMonth)
    {
        var monthStart = DateTime.TryParse($"{billingMonth}-01", out var parsedMonth)
            ? parsedMonth.Date
            : DateTime.Today.Date;
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        return db.RoomTenants.Count(x =>
            x.RoomId == roomId &&
            x.StartDate <= monthEnd &&
            (x.EndDate == null || x.EndDate >= monthStart));
    }
}
