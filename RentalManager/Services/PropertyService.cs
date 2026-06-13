using RentalManager.Helpers;
using RentalManager.Models;
using RentalManager.Data;

namespace RentalManager.Services;

public class PropertyService : CrudService<Property>
{
    public void Delete(int id)
    {
        using var db = DbContextFactory.Create();
        var property = db.Properties.Find(id) ?? throw new ValidationException("Không tìm thấy nhà / khu trọ.");
        if (db.Rooms.Any(x => x.PropertyId == id))
        {
            throw new ValidationException("Không thể xóa nhà vì vẫn còn phòng thuộc nhà này.");
        }

        db.Properties.Remove(property);
        db.SaveChanges();
    }

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
