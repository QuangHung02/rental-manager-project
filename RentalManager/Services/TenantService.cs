using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class TenantService : CrudService<Tenant>
{
    protected override void Validate(Tenant entity)
    {
        if (string.IsNullOrWhiteSpace(entity.FullName))
        {
            throw new ValidationException("Tenant full name is required.");
        }
    }
}
