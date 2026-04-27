using Microsoft.EntityFrameworkCore;
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
            .FirstOrDefault(x => x.Id == roomId) ?? throw new ValidationException("Room was not found.");

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
                    ItemName = "Room Rent",
                    CalculationType = CalculationType.Fixed,
                    Quantity = 1,
                    Unit = "month",
                    UnitPrice = room.BaseRent,
                    Amount = room.BaseRent
                }
            }
        };

        var activeTenantCount = db.RoomTenants.Count(x => x.RoomId == room.Id && x.Status == RoomTenantStatus.Active);
        foreach (var config in room.FeeConfigs.Where(x => x.Enabled))
        {
            var feeType = config.FeeType ?? throw new ValidationException("Fee type was not found.");
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
        var unitPrice = config.UnitPrice ?? feeType.DefaultUnitPrice;
        var quantity = config.Quantity ?? 1;
        var amount = 0m;
        var note = config.Note;

        switch (config.CalculationType)
        {
            case CalculationType.Fixed:
                quantity = 1;
                unitPrice = config.FixedAmount ?? unitPrice;
                amount = config.FixedAmount ?? unitPrice;
                break;
            case CalculationType.Meter:
                var reading = db.MeterReadings.FirstOrDefault(x => x.RoomId == roomId && x.FeeTypeId == feeType.Id && x.BillingMonth == billingMonth);
                if (reading is null)
                {
                    quantity = 0;
                    amount = 0;
                    note = "Missing meter reading";
                }
                else
                {
                    quantity = reading.UsageAmount;
                    unitPrice = reading.UnitPriceSnapshot;
                    amount = reading.Amount;
                }
                break;
            case CalculationType.PerPerson:
                quantity = activeTenantCount;
                amount = quantity * unitPrice;
                break;
            case CalculationType.PerUnit:
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
}
