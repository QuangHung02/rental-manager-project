using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class RoomTenantService : CrudService<RoomTenant>
{
    protected override IQueryable<RoomTenant> Include(IQueryable<RoomTenant> query)
    {
        return query.Include(x => x.Room).ThenInclude(x => x!.Property).Include(x => x.Tenant);
    }

    public override RoomTenant Save(RoomTenant entity)
    {
        if (entity.RoomId <= 0 || entity.TenantId <= 0)
        {
            throw new ValidationException("Choose a room and tenant before assigning.");
        }

        using var db = DbContextFactory.Create();
        if (entity.IsRepresentative && entity.Status == RoomTenantStatus.Active)
        {
            var current = db.RoomTenants.Where(x => x.RoomId == entity.RoomId && x.Status == RoomTenantStatus.Active && x.IsRepresentative && x.Id != entity.Id);
            foreach (var assignment in current)
            {
                assignment.IsRepresentative = false;
            }
        }

        if (entity.Id == 0)
        {
            db.RoomTenants.Add(entity);
        }
        else
        {
            db.RoomTenants.Update(entity);
        }

        var room = db.Rooms.Find(entity.RoomId);
        if (room is not null && entity.Status == RoomTenantStatus.Active)
        {
            room.Status = RoomStatus.Occupied;
        }

        db.SaveChanges();
        return entity;
    }

    public void EndAssignment(RoomTenant entity)
    {
        entity.Status = RoomTenantStatus.Ended;
        entity.EndDate = DateTime.Today;
        entity.IsRepresentative = false;
        Save(entity);
    }
}
