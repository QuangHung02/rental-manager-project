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
            throw new ValidationException("Vui lòng chọn phòng và người thuê trước khi phân phòng.");
        }

        using var db = DbContextFactory.Create();
        entity.Status = RoomTenantStatus.Active;
        entity.EndDate = null;
        EnsureTenantHasNoOtherActiveAssignment(db, entity.TenantId, entity.Id);
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

        SetTenantStatus(db, entity.TenantId, TenantStatus.Renting);
        db.SaveChanges();
        return entity;
    }

    public void EndAssignment(int assignmentId, DateTime? endDate = null)
    {
        using var db = DbContextFactory.Create();
        var assignment = db.RoomTenants.Find(assignmentId) ?? throw new ValidationException("Không tìm thấy lượt thuê.");
        if (assignment.Status == RoomTenantStatus.Ended)
        {
            throw new ValidationException("Lượt thuê này đã kết thúc.");
        }

        var roomId = assignment.RoomId;
        var tenantId = assignment.TenantId;
        assignment.Status = RoomTenantStatus.Ended;
        assignment.EndDate = endDate ?? DateTime.Today;
        assignment.IsRepresentative = false;
        db.SaveChanges();
        RecalculateRoomStatus(db, roomId);
        RecalculateTenantStatus(db, tenantId);
        db.SaveChanges();
    }

    public void ChangeRoom(int assignmentId, int targetRoomId, DateTime? moveDate = null, bool isRepresentative = false)
    {
        if (targetRoomId <= 0)
        {
            throw new ValidationException("Vui lòng chọn phòng mới.");
        }

        using var db = DbContextFactory.Create();
        var oldAssignment = db.RoomTenants.Find(assignmentId) ?? throw new ValidationException("Không tìm thấy lượt thuê.");
        if (oldAssignment.Status == RoomTenantStatus.Ended)
        {
            throw new ValidationException("Lượt thuê này đã kết thúc.");
        }

        if (oldAssignment.RoomId == targetRoomId)
        {
            throw new ValidationException("Phòng mới phải khác phòng hiện tại.");
        }

        var effectiveDate = moveDate ?? DateTime.Today;
        var oldRoomId = oldAssignment.RoomId;
        var tenantId = oldAssignment.TenantId;
        EnsureTenantHasNoOtherActiveAssignment(db, oldAssignment.TenantId, oldAssignment.Id);
        oldAssignment.Status = RoomTenantStatus.Ended;
        oldAssignment.EndDate = effectiveDate;
        oldAssignment.IsRepresentative = false;

        if (isRepresentative)
        {
            ClearRepresentatives(db, targetRoomId);
        }

        db.RoomTenants.Add(new RoomTenant
        {
            RoomId = targetRoomId,
            TenantId = oldAssignment.TenantId,
            IsRepresentative = isRepresentative,
            StartDate = effectiveDate,
            Status = RoomTenantStatus.Active
        });

        db.SaveChanges();
        RecalculateRoomStatus(db, oldRoomId);
        SetRoomStatus(db, targetRoomId, RoomStatus.Occupied);
        SetTenantStatus(db, tenantId, TenantStatus.Renting);
        db.SaveChanges();
    }

    public void SetRepresentative(int assignmentId)
    {
        using var db = DbContextFactory.Create();
        var assignment = db.RoomTenants.Find(assignmentId) ?? throw new ValidationException("Không tìm thấy lượt thuê.");
        if (assignment.Status != RoomTenantStatus.Active)
        {
            throw new ValidationException("Chỉ có thể đặt đại diện cho người đang thuê.");
        }

        ClearRepresentatives(db, assignment.RoomId);
        assignment.IsRepresentative = true;
        SetRoomStatus(db, assignment.RoomId, RoomStatus.Occupied);
        db.SaveChanges();
    }

    private static void ClearRepresentatives(RentalManagerDbContext db, int roomId)
    {
        var current = db.RoomTenants.Where(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active && x.IsRepresentative);
        foreach (var assignment in current)
        {
            assignment.IsRepresentative = false;
        }
    }

    private static void RecalculateRoomStatus(RentalManagerDbContext db, int roomId)
    {
        var hasActiveTenant = db.RoomTenants.Any(x => x.RoomId == roomId && x.Status == RoomTenantStatus.Active);
        SetRoomStatus(db, roomId, hasActiveTenant ? RoomStatus.Occupied : RoomStatus.Vacant);
    }

    private static void SetRoomStatus(RentalManagerDbContext db, int roomId, RoomStatus status)
    {
        var room = db.Rooms.Find(roomId);
        if (room is null)
        {
            return;
        }

        room.Status = status;
        room.UpdatedAt = DateTime.Now;
    }

    private static void RecalculateTenantStatus(RentalManagerDbContext db, int tenantId)
    {
        var status = db.RoomTenants.Any(x => x.TenantId == tenantId && x.Status == RoomTenantStatus.Active)
            ? TenantStatus.Renting
            : db.RoomTenants.Any(x => x.TenantId == tenantId && x.Status == RoomTenantStatus.Ended)
                ? TenantStatus.Former
                : TenantStatus.Unassigned;

        SetTenantStatus(db, tenantId, status);
    }

    private static void SetTenantStatus(RentalManagerDbContext db, int tenantId, TenantStatus status)
    {
        var tenant = db.Tenants.Find(tenantId);
        if (tenant is null)
        {
            return;
        }

        tenant.Status = status;
        tenant.UpdatedAt = DateTime.Now;
    }

    private static void EnsureTenantHasNoOtherActiveAssignment(RentalManagerDbContext db, int tenantId, int currentAssignmentId)
    {
        var hasOtherActiveAssignment = db.RoomTenants.Any(x =>
            x.TenantId == tenantId &&
            x.Status == RoomTenantStatus.Active &&
            x.Id != currentAssignmentId);

        if (hasOtherActiveAssignment)
        {
            throw new ValidationException("Người thuê này đang được gán vào một phòng khác. Vui lòng chuyển phòng thay vì gán mới.");
        }
    }
}
