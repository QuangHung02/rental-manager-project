using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class TenantService : CrudService<Tenant>
{
    public override Tenant Save(Tenant entity)
    {
        Validate(entity);
        using var db = DbContextFactory.Create();
        if (entity.Id == 0)
        {
            entity.Status = TenantStatus.Unassigned;
            entity.CreatedAt = DateTime.Now;
            entity.UpdatedAt = DateTime.Now;
            db.Tenants.Add(entity);
            db.SaveChanges();
            return entity;
        }

        var tenant = db.Tenants.Find(entity.Id) ?? throw new ValidationException("Không tìm thấy người thuê.");
        tenant.FullName = entity.FullName;
        tenant.Phone = entity.Phone;
        tenant.Email = entity.Email;
        tenant.IdentityNumber = entity.IdentityNumber;
        tenant.Note = entity.Note;
        tenant.UpdatedAt = DateTime.Now;
        db.SaveChanges();
        entity.Status = tenant.Status;
        return entity;
    }

    public void SyncStatusesFromAssignments()
    {
        using var db = DbContextFactory.Create();
        var tenants = db.Tenants.Include(x => x.RoomTenants).ToList();
        foreach (var tenant in tenants)
        {
            var status = GetStatusFromAssignments(tenant.RoomTenants);
            if (tenant.Status != status)
            {
                tenant.Status = status;
                tenant.UpdatedAt = DateTime.Now;
            }
        }

        db.SaveChanges();
    }

    protected override void Validate(Tenant entity)
    {
        if (string.IsNullOrWhiteSpace(entity.FullName))
        {
            throw new ValidationException("Tenant full name is required.");
        }
    }

    private static TenantStatus GetStatusFromAssignments(IEnumerable<RoomTenant> assignments)
    {
        if (assignments.Any(x => x.Status == RoomTenantStatus.Active))
        {
            return TenantStatus.Renting;
        }

        return assignments.Any(x => x.Status == RoomTenantStatus.Ended)
            ? TenantStatus.Former
            : TenantStatus.Unassigned;
    }
}
