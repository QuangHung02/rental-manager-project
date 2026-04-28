using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class RoomFeeConfigService : CrudService<RoomFeeConfig>
{
    public override RoomFeeConfig Save(RoomFeeConfig entity)
    {
        NormalizeFields(entity);
        entity.Room = null;
        entity.FeeType = null;
        return base.Save(entity);
    }

    public void ToggleActive(int id)
    {
        using var db = DbContextFactory.Create();
        var config = db.RoomFeeConfigs.Include(x => x.FeeType).FirstOrDefault(x => x.Id == id) ?? throw new ValidationException("Không tìm thấy cấu hình phí đã chọn.");
        if (!config.Enabled && config.FeeType?.IsActive == false)
        {
            throw new ValidationException("Loại phí này đang ngừng dùng. Vui lòng bật lại trong tab Loại phí trước.");
        }

        config.Enabled = !config.Enabled;
        db.SaveChanges();
    }

    protected override IQueryable<RoomFeeConfig> Include(IQueryable<RoomFeeConfig> query)
    {
        return query.Include(x => x.Room).ThenInclude(x => x!.Property).Include(x => x.FeeType);
    }

    protected override void Validate(RoomFeeConfig entity)
    {
        if (entity.RoomId <= 0 || entity.FeeTypeId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng và loại phí.");
        }

        if (entity.CalculationType == CalculationType.Fixed && entity.FixedAmount is < 0)
        {
            throw new ValidationException("Số tiền cố định phải lớn hơn hoặc bằng 0.");
        }

        if (entity.CalculationType == CalculationType.Manual)
        {
            if (entity.FixedAmount is null)
            {
                throw new ValidationException("Vui lòng nhập số tiền cố định.");
            }

            if (entity.FixedAmount < 0)
            {
                throw new ValidationException("Số tiền cố định phải lớn hơn hoặc bằng 0.");
            }
        }

        var usesUnitPrice = entity.CalculationType is CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit;
        if (usesUnitPrice && entity.UnitPrice is < 0)
        {
            throw new ValidationException("Đơn giá phải lớn hơn hoặc bằng 0.");
        }

        if (entity.CalculationType == CalculationType.PerUnit && entity.Quantity is < 0)
        {
            throw new ValidationException("Số lượng phải lớn hơn hoặc bằng 0.");
        }

        using var db = DbContextFactory.Create();
        var feeType = db.FeeTypes.AsNoTracking().FirstOrDefault(x => x.Id == entity.FeeTypeId) ?? throw new ValidationException("Không tìm thấy loại phí đã chọn.");
        if (entity.Enabled && !feeType.IsActive)
        {
            throw new ValidationException("Loại phí này đang ngừng dùng. Vui lòng bật lại trong tab Loại phí trước.");
        }

        var duplicateConfig = db.RoomFeeConfigs
            .AsNoTracking()
            .FirstOrDefault(x =>
            x.Id != entity.Id &&
            x.RoomId == entity.RoomId &&
            x.FeeTypeId == entity.FeeTypeId);

        if (duplicateConfig is not null)
        {
            throw new ValidationException(duplicateConfig.Enabled
                ? "Phòng này đã có loại phí này đang áp dụng. Vui lòng bấm Sửa trên dòng tương ứng để chỉnh sửa."
                : "Loại phí này đã tồn tại nhưng đang ngừng áp dụng. Vui lòng chuyển bộ lọc sang Tất cả hoặc Ngừng áp dụng để bật lại.");
        }

        if (!entity.Enabled)
        {
            return;
        }

        if (entity.CalculationType == CalculationType.Meter && !feeType.IsActive)
        {
            throw new ValidationException("Loại phí này đang ngừng dùng. Vui lòng bật lại trong tab Loại phí trước.");
        }
    }

    private static void NormalizeFields(RoomFeeConfig entity)
    {
        switch (entity.CalculationType)
        {
            case CalculationType.Fixed:
                entity.UnitPrice = null;
                entity.Quantity = null;
                break;
            case CalculationType.Manual:
                entity.UnitPrice = null;
                entity.Quantity = null;
                break;
            case CalculationType.Meter:
            case CalculationType.PerPerson:
                entity.FixedAmount = null;
                entity.Quantity = null;
                break;
            case CalculationType.PerUnit:
                entity.FixedAmount = null;
                break;
        }
    }
}
