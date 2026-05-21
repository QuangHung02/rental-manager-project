using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.DTOs;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class DashboardService
{
    public DashboardSummary GetSummary(string billingMonth, int? propertyId = null)
    {
        return GetSummary(billingMonth, billingMonth, propertyId);
    }

    public DashboardSummary GetSummary(string startBillingMonth, string endBillingMonth, int? propertyId = null)
    {
        using var db = DbContextFactory.Create();
        var invoices = db.Invoices
            .Include(x => x.Room)
            .Where(x => string.Compare(x.BillingMonth, startBillingMonth) >= 0 && string.Compare(x.BillingMonth, endBillingMonth) <= 0);
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
            .Count(x => !db.MeterReadings.Any(r => r.RoomId == x.RoomId && r.FeeTypeId == x.FeeTypeId && string.Compare(r.BillingMonth, startBillingMonth) >= 0 && string.Compare(r.BillingMonth, endBillingMonth) <= 0));

        return new DashboardSummary
        {
            ExpectedRevenue = invoiceList.Sum(x => x.TotalAmount),
            CollectedAmount = invoiceList.Sum(x => x.PaidAmount),
            UnpaidAmount = invoiceList.Sum(x => x.RemainingAmount),
            OccupiedRoomCount = rooms.Count(x => x.Status == RoomStatus.Occupied),
            VacantRoomCount = rooms.Count(x => x.Status == RoomStatus.Vacant),
            MissingReadingCount = missing,
            UnpaidInvoiceCount = invoiceList.Count(x => x.RemainingAmount > 0 && x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled)
        };
    }

    public List<Models.Invoice> GetInvoices(string startBillingMonth, string endBillingMonth, int? propertyId = null)
    {
        using var db = DbContextFactory.Create();
        var invoices = db.Invoices
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.Room).ThenInclude(x => x!.RoomTenants).ThenInclude(x => x.Tenant)
            .Where(x => string.Compare(x.BillingMonth, startBillingMonth) >= 0 && string.Compare(x.BillingMonth, endBillingMonth) <= 0);

        if (propertyId is > 0)
        {
            invoices = invoices.Where(x => x.Room!.PropertyId == propertyId);
        }

        return invoices.AsNoTracking()
            .OrderByDescending(x => x.BillingMonth)
            .ThenBy(x => x.Room!.RoomName)
            .ToList();
    }

    public List<DashboardMonthlySummary> GetMonthlySummaries(int year, int? propertyId = null)
    {
        var startBillingMonth = $"{year:0000}-01";
        var endBillingMonth = $"{year:0000}-12";
        using var db = DbContextFactory.Create();
        var invoices = db.Invoices
            .Include(x => x.Room)
            .Where(x => string.Compare(x.BillingMonth, startBillingMonth) >= 0 && string.Compare(x.BillingMonth, endBillingMonth) <= 0);

        if (propertyId is > 0)
        {
            invoices = invoices.Where(x => x.Room!.PropertyId == propertyId);
        }

        var grouped = invoices.AsNoTracking()
            .ToList()
            .GroupBy(x => x.BillingMonth)
            .ToDictionary(x => x.Key, x => x.ToList());

        return Enumerable.Range(1, 12)
            .Select(month =>
            {
                var billingMonth = $"{year:0000}-{month:00}";
                grouped.TryGetValue(billingMonth, out var rows);
                rows ??= new List<Invoice>();
                return new DashboardMonthlySummary
                {
                    BillingMonth = billingMonth,
                    ExpectedRevenue = rows.Sum(x => x.TotalAmount),
                    CollectedAmount = rows.Sum(x => x.PaidAmount),
                    UnpaidAmount = rows.Sum(x => x.RemainingAmount),
                    UnpaidInvoiceCount = rows.Count(x => x.RemainingAmount > 0 && x.Status != InvoiceStatus.Paid && x.Status != InvoiceStatus.Cancelled)
                };
            })
            .ToList();
    }

    public List<Invoice> GetUnpaidInvoices(string startBillingMonth, string endBillingMonth, int? propertyId = null)
    {
        return GetInvoices(startBillingMonth, endBillingMonth, propertyId)
            .Where(x => x.Status is InvoiceStatus.Issued or InvoiceStatus.Partial && x.RemainingAmount > 0)
            .ToList();
    }

    public List<Payment> GetRecentPayments(string startBillingMonth, string endBillingMonth, int? propertyId = null)
    {
        using var db = DbContextFactory.Create();
        var payments = db.Payments
            .Include(x => x.Invoice).ThenInclude(x => x!.Room).ThenInclude(x => x!.Property)
            .Where(x => string.Compare(x.Invoice!.BillingMonth, startBillingMonth) >= 0 && string.Compare(x.Invoice!.BillingMonth, endBillingMonth) <= 0);

        if (propertyId is > 0)
        {
            payments = payments.Where(x => x.Invoice!.Room!.PropertyId == propertyId);
        }

        return payments.AsNoTracking()
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Take(10)
            .ToList();
    }

    public List<MissingReadingRow> GetMissingReadings(string billingMonth, int? propertyId = null)
    {
        using var db = DbContextFactory.Create();
        var configs = db.RoomFeeConfigs
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.FeeType)
            .Where(x => x.Enabled && x.CalculationType == CalculationType.Meter);

        if (propertyId is > 0)
        {
            configs = configs.Where(x => x.Room!.PropertyId == propertyId);
        }

        return configs
            .AsNoTracking()
            .ToList()
            .Where(x => !db.MeterReadings.Any(r => r.RoomId == x.RoomId && r.FeeTypeId == x.FeeTypeId && r.BillingMonth == billingMonth))
            .Select(x => new MissingReadingRow
            {
                PropertyName = x.Room?.Property?.Name ?? string.Empty,
                RoomName = x.Room?.RoomName ?? string.Empty,
                FeeTypeName = x.FeeType is null ? string.Empty : DisplayText.FeeName(x.FeeType.Name),
                PreviousReading = db.MeterReadings
                    .Where(r => r.RoomId == x.RoomId && r.FeeTypeId == x.FeeTypeId && string.Compare(r.BillingMonth, billingMonth) < 0)
                    .OrderByDescending(r => r.BillingMonth)
                    .Select(r => r.CurrentReading)
                    .FirstOrDefault()
            })
            .OrderBy(x => x.PropertyName)
            .ThenBy(x => x.RoomName)
            .ToList();
    }
}
