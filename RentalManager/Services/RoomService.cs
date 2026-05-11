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
        entity.RoomName = entity.RoomName.Trim();
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
        var tenantIds = activeAssignments.Select(x => x.TenantId).Distinct().ToList();

        foreach (var assignment in activeAssignments)
        {
            assignment.Status = RoomTenantStatus.Ended;
            assignment.EndDate = DateTime.Today;
            assignment.IsRepresentative = false;
        }

        room.Status = RoomStatus.Vacant;
        room.UpdatedAt = DateTime.Now;
        db.SaveChanges();

        foreach (var tenantId in tenantIds)
        {
            RecalculateTenantStatus(db, tenantId);
        }

        db.SaveChanges();
        return activeAssignments.Count;
    }

    private static void RecalculateTenantStatus(RentalManagerDbContext db, int tenantId)
    {
        var tenant = db.Tenants.Find(tenantId);
        if (tenant is null)
        {
            return;
        }

        tenant.Status = db.RoomTenants.Any(x => x.TenantId == tenantId && x.Status == RoomTenantStatus.Active)
            ? TenantStatus.Renting
            : db.RoomTenants.Any(x => x.TenantId == tenantId && x.Status == RoomTenantStatus.Ended)
                ? TenantStatus.Former
                : TenantStatus.Unassigned;
        tenant.UpdatedAt = DateTime.Now;
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
            throw new ValidationException("Tên phòng là bắt buộc.");
        }

        if (entity.BaseRent < 0)
        {
            throw new ValidationException("Tiền phòng phải lớn hơn hoặc bằng 0.");
        }

        using var db = DbContextFactory.Create();
        var normalizedRoomName = entity.RoomName.Trim();
        var duplicateExists = db.Rooms
            .AsNoTracking()
            .Where(x => x.PropertyId == entity.PropertyId)
            .Select(x => new { x.Id, x.RoomName })
            .AsEnumerable()
            .Any(x =>
                (entity.Id == 0 || x.Id != entity.Id) &&
                string.Equals(x.RoomName.Trim(), normalizedRoomName, StringComparison.OrdinalIgnoreCase));

        if (duplicateExists)
        {
            throw new ValidationException("Phòng này đã tồn tại trong nhà / khu trọ đã chọn.");
        }
    }
}
