using RentalManager.Data;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class FeeTypeService : CrudService<FeeType>
{
    public override FeeType Save(FeeType entity)
    {
        if (entity.Id == 0)
        {
            entity.IsSystem = false;
        }

        return base.Save(entity);
    }

    public void Deactivate(int id)
    {
        using var db = DbContextFactory.Create();
        var feeType = db.FeeTypes.Find(id) ?? throw new ValidationException("Fee type was not found.");
        feeType.IsActive = false;
        db.SaveChanges();
    }

    protected override void Validate(FeeType entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw new ValidationException("Fee type name is required.");
        }

        if (entity.DefaultUnitPrice < 0)
        {
            throw new ValidationException("Default unit price must be greater than or equal to 0.");
        }
    }
}
