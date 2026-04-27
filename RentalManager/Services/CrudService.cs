using Microsoft.EntityFrameworkCore;
using RentalManager.Data;

namespace RentalManager.Services;

public abstract class CrudService<T> where T : class
{
    public virtual List<T> GetAll()
    {
        using var db = DbContextFactory.Create();
        return Include(db.Set<T>()).AsNoTracking().ToList();
    }

    public virtual T Save(T entity)
    {
        Validate(entity);
        using var db = DbContextFactory.Create();
        var id = (int)(typeof(T).GetProperty("Id")?.GetValue(entity) ?? 0);
        if (id == 0)
        {
            SetTimestamp(entity, "CreatedAt");
            SetTimestamp(entity, "UpdatedAt");
            db.Set<T>().Add(entity);
        }
        else
        {
            SetTimestamp(entity, "UpdatedAt");
            db.Set<T>().Update(entity);
        }

        db.SaveChanges();
        return entity;
    }

    protected virtual IQueryable<T> Include(IQueryable<T> query) => query;

    protected virtual void Validate(T entity)
    {
    }

    private static void SetTimestamp(T entity, string propertyName)
    {
        var property = typeof(T).GetProperty(propertyName);
        if (property?.PropertyType == typeof(DateTime))
        {
            property.SetValue(entity, DateTime.Now);
        }
    }
}
