using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
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
        if (entity.RoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng.");
        }

        if (entity.FeeTypeId <= 0)
        {
            throw new ValidationException("Vui lòng chọn loại phí.");
        }

        var feeType = db.FeeTypes.Find(entity.FeeTypeId) ?? throw new ValidationException("Không tìm thấy loại phí đã chọn.");
        var config = db.RoomFeeConfigs.FirstOrDefault(x =>
            x.RoomId == entity.RoomId &&
            x.FeeTypeId == entity.FeeTypeId &&
            x.Enabled &&
            x.CalculationType == CalculationType.Meter);

        if (config is null)
        {
            throw new ValidationException("Vui lòng chọn loại phí theo chỉ số đang áp dụng cho phòng.");
        }

        var unitPrice = config.UnitPrice ?? feeType.DefaultUnitPrice;

        if (entity.CurrentReading < entity.PreviousReading)
        {
            throw new ValidationException("Chỉ số mới phải lớn hơn hoặc bằng chỉ số cũ.");
        }

        entity.UnitPriceSnapshot = unitPrice;
        entity.UsageAmount = entity.CurrentReading - entity.PreviousReading;
        entity.Amount = entity.UsageAmount * unitPrice;

        var existing = entity.Id == 0
            ? db.MeterReadings.FirstOrDefault(x =>
                x.RoomId == entity.RoomId &&
                x.FeeTypeId == entity.FeeTypeId &&
                x.BillingMonth == entity.BillingMonth)
            : db.MeterReadings.Find(entity.Id);

        if (existing is null)
        {
            db.MeterReadings.Add(entity);
        }
        else
        {
            existing.RoomId = entity.RoomId;
            existing.FeeTypeId = entity.FeeTypeId;
            existing.BillingMonth = entity.BillingMonth;
            existing.PreviousReading = entity.PreviousReading;
            existing.CurrentReading = entity.CurrentReading;
            existing.UsageAmount = entity.UsageAmount;
            existing.UnitPriceSnapshot = entity.UnitPriceSnapshot;
            existing.Amount = entity.Amount;
            existing.Note = entity.Note;
        }

        db.SaveChanges();
        return existing ?? entity;
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
