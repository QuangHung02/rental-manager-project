using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class RoomFeeConfigService : CrudService<RoomFeeConfig>
{
    public override RoomFeeConfig Save(RoomFeeConfig entity)
    {
        entity.Room = null;
        entity.FeeType = null;
        return base.Save(entity);
    }

    public void Disable(int id)
    {
        using var db = DbContextFactory.Create();
        var config = db.RoomFeeConfigs.Find(id) ?? throw new ValidationException("Room fee config was not found.");
        config.Enabled = false;
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
            throw new ValidationException("Choose a room and fee type.");
        }

        if (entity.UnitPrice is < 0 || entity.FixedAmount is < 0 || entity.Quantity is < 0)
        {
            throw new ValidationException("Amounts, unit prices, and quantities must be greater than or equal to 0.");
        }
    }
}
