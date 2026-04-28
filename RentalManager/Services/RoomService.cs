using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class RoomService : CrudService<Room>
{
    public override Room Save(Room entity)
    {
        entity.Property = null;
        entity.RoomTenants.Clear();
        return base.Save(entity);
    }

    public void Deactivate(int id)
    {
        using var db = DbContextFactory.Create();
        var room = db.Rooms.Find(id) ?? throw new ValidationException("Room was not found.");
        room.Status = RoomStatus.Inactive;
        room.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }

    public int Checkout(int id)
    {
        using var db = DbContextFactory.Create();
        var room = db.Rooms.Find(id) ?? throw new ValidationException("Không tìm thấy phòng.");
        var activeAssignments = db.RoomTenants
            .Where(x => x.RoomId == id && x.Status == RoomTenantStatus.Active)
            .ToList();

        foreach (var assignment in activeAssignments)
        {
            assignment.Status = RoomTenantStatus.Ended;
            assignment.EndDate = DateTime.Today;
            assignment.IsRepresentative = false;
        }

        room.Status = RoomStatus.Vacant;
        room.UpdatedAt = DateTime.Now;
        db.SaveChanges();
        return activeAssignments.Count;
    }

    protected override IQueryable<Room> Include(IQueryable<Room> query)
    {
        return query.Include(x => x.Property).Include(x => x.RoomTenants).ThenInclude(x => x.Tenant);
    }

    protected override void Validate(Room entity)
    {
        if (entity.PropertyId <= 0)
        {
            throw new ValidationException("Room must belong to a property.");
        }

        if (string.IsNullOrWhiteSpace(entity.RoomName))
        {
            throw new ValidationException("Room name is required.");
        }

        if (entity.BaseRent < 0)
        {
            throw new ValidationException("Base rent must be greater than or equal to 0.");
        }
    }
}
