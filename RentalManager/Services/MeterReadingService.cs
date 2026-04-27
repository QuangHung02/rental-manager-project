using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class MeterReadingService : CrudService<MeterReading>
{
    protected override IQueryable<MeterReading> Include(IQueryable<MeterReading> query)
    {
        return query.Include(x => x.Room).ThenInclude(x => x!.Property).Include(x => x.FeeType);
    }

    public override MeterReading Save(MeterReading entity)
    {
        using var db = DbContextFactory.Create();
        var feeType = db.FeeTypes.Find(entity.FeeTypeId) ?? throw new ValidationException("Fee type was not found.");
        var config = db.RoomFeeConfigs.FirstOrDefault(x => x.RoomId == entity.RoomId && x.FeeTypeId == entity.FeeTypeId);
        var unitPrice = config?.UnitPrice ?? feeType.DefaultUnitPrice;

        if (entity.CurrentReading < entity.PreviousReading)
        {
            throw new ValidationException("Current reading must be greater than or equal to previous reading.");
        }

        entity.UnitPriceSnapshot = unitPrice;
        entity.UsageAmount = entity.CurrentReading - entity.PreviousReading;
        entity.Amount = entity.UsageAmount * unitPrice;

        if (entity.Id == 0)
        {
            db.MeterReadings.Add(entity);
        }
        else
        {
            db.MeterReadings.Update(entity);
        }

        db.SaveChanges();
        return entity;
    }

    public decimal GetPreviousReading(int roomId, int feeTypeId, string billingMonth)
    {
        using var db = DbContextFactory.Create();
        return db.MeterReadings
            .Where(x => x.RoomId == roomId && x.FeeTypeId == feeTypeId && string.Compare(x.BillingMonth, billingMonth, StringComparison.Ordinal) < 0)
            .OrderByDescending(x => x.BillingMonth)
            .Select(x => x.CurrentReading)
            .FirstOrDefault();
    }
}
