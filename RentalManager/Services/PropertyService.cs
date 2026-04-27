using RentalManager.Helpers;
using RentalManager.Models;
using RentalManager.Data;

namespace RentalManager.Services;

public class PropertyService : CrudService<Property>
{
    public void Deactivate(int id)
    {
        using var db = DbContextFactory.Create();
        var property = db.Properties.Find(id) ?? throw new ValidationException("Property was not found.");
        property.IsActive = false;
        property.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }

    protected override void Validate(Property entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Name))
        {
            throw new ValidationException("Property name is required.");
        }
    }
}
