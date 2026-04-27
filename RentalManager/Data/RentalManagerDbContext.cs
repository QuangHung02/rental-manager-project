using Microsoft.EntityFrameworkCore;
using RentalManager.Enums;
using RentalManager.Models;

namespace RentalManager.Data;

public class RentalManagerDbContext : DbContext
{
    public RentalManagerDbContext(DbContextOptions<RentalManagerDbContext> options) : base(options)
    {
    }

    public DbSet<Property> Properties => Set<Property>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RoomTenant> RoomTenants => Set<RoomTenant>();
    public DbSet<FeeType> FeeTypes => Set<FeeType>();
    public DbSet<RoomFeeConfig> RoomFeeConfigs => Set<RoomFeeConfig>();
    public DbSet<MeterReading> MeterReadings => Set<MeterReading>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Property>().Property(x => x.Name).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<Room>().Property(x => x.RoomName).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<Tenant>().Property(x => x.FullName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<FeeType>().Property(x => x.Name).HasMaxLength(80).IsRequired();
        modelBuilder.Entity<MeterReading>().Property(x => x.BillingMonth).HasMaxLength(7).IsRequired();
        modelBuilder.Entity<Invoice>().Property(x => x.BillingMonth).HasMaxLength(7).IsRequired();

        modelBuilder.Entity<Invoice>()
            .HasIndex(x => new { x.RoomId, x.BillingMonth })
            .IsUnique();

        modelBuilder.Entity<MeterReading>()
            .HasIndex(x => new { x.RoomId, x.FeeTypeId, x.BillingMonth })
            .IsUnique();

        modelBuilder.Entity<RoomFeeConfig>()
            .HasIndex(x => new { x.RoomId, x.FeeTypeId })
            .IsUnique();

        SeedFeeTypes(modelBuilder);
    }

    private static void SeedFeeTypes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FeeType>().HasData(
            new FeeType { Id = 1, Name = "Electricity", DefaultCalculationType = CalculationType.Meter, DefaultUnit = "kWh", DefaultUnitPrice = 3500, IsSystem = true, IsActive = true },
            new FeeType { Id = 2, Name = "Water", DefaultCalculationType = CalculationType.PerPerson, DefaultUnit = "person", DefaultUnitPrice = 100000, IsSystem = true, IsActive = true },
            new FeeType { Id = 3, Name = "Wifi", DefaultCalculationType = CalculationType.Fixed, DefaultUnit = "month", DefaultUnitPrice = 0, IsSystem = true, IsActive = true },
            new FeeType { Id = 4, Name = "Parking", DefaultCalculationType = CalculationType.PerUnit, DefaultUnit = "unit", DefaultUnitPrice = 150000, IsSystem = true, IsActive = true },
            new FeeType { Id = 5, Name = "Garbage", DefaultCalculationType = CalculationType.Fixed, DefaultUnit = "month", DefaultUnitPrice = 0, IsSystem = true, IsActive = true },
            new FeeType { Id = 6, Name = "Other", DefaultCalculationType = CalculationType.Manual, DefaultUnit = null, DefaultUnitPrice = 0, IsSystem = true, IsActive = true });
    }
}
