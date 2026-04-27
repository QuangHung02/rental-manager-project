using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.DTOs;
using RentalManager.Enums;

namespace RentalManager.Services;

public class DashboardService
{
    public DashboardSummary GetSummary(string billingMonth, int? propertyId = null)
    {
        using var db = DbContextFactory.Create();
        var invoices = db.Invoices.Include(x => x.Room).Where(x => x.BillingMonth == billingMonth);
        var rooms = db.Rooms.AsQueryable();

        if (propertyId is > 0)
        {
            invoices = invoices.Where(x => x.Room!.PropertyId == propertyId);
            rooms = rooms.Where(x => x.PropertyId == propertyId);
        }

        var meterConfigs = db.RoomFeeConfigs.Where(x => x.Enabled && x.CalculationType == CalculationType.Meter);
        if (propertyId is > 0)
        {
            meterConfigs = meterConfigs.Include(x => x.Room).Where(x => x.Room!.PropertyId == propertyId);
        }

        var invoiceList = invoices.AsNoTracking().ToList();
        var missing = meterConfigs
            .AsNoTracking()
            .ToList()
            .Count(x => !db.MeterReadings.Any(r => r.RoomId == x.RoomId && r.FeeTypeId == x.FeeTypeId && r.BillingMonth == billingMonth));

        return new DashboardSummary
        {
            ExpectedRevenue = invoiceList.Sum(x => x.TotalAmount),
            CollectedAmount = invoiceList.Sum(x => x.PaidAmount),
            UnpaidAmount = invoiceList.Sum(x => x.RemainingAmount),
            OccupiedRoomCount = rooms.Count(x => x.Status == RoomStatus.Occupied),
            VacantRoomCount = rooms.Count(x => x.Status == RoomStatus.Vacant),
            MissingReadingCount = missing,
            UnpaidInvoiceCount = invoiceList.Count(x => x.Status == InvoiceStatus.Issued || x.Status == InvoiceStatus.Partial)
        };
    }
}
