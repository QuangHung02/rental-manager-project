using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Models;

namespace RentalManager.Services;

public class DemoDataService
{
    public void Seed()
    {
        using var db = DbContextFactory.Create();
        ResetDemoData(db);

        var electricity = GetFeeType(db, "Electricity", CalculationType.Meter, "kWh", 3500);
        var water = GetFeeType(db, "Water", CalculationType.PerPerson, "person", 100000);
        var wifi = GetFeeType(db, "Wifi", CalculationType.Fixed, "month", 0);
        var parking = GetFeeType(db, "Parking", CalculationType.PerUnit, "unit", 150000);
        db.SaveChanges();

        var nhaA = GetProperty(db, "Nhà A", "123 Lê Lợi");
        var nhaB = GetProperty(db, "Nhà B", "45 Nguyễn Trãi");
        db.SaveChanges();

        var p101 = GetRoom(db, nhaA, "Phòng 101", 3000000, RoomStatus.Occupied);
        var p102 = GetRoom(db, nhaA, "Phòng 102", 3200000, RoomStatus.Occupied);
        GetRoom(db, nhaA, "Phòng 103", 2800000, RoomStatus.Vacant);
        var p201 = GetRoom(db, nhaB, "Phòng 201", 3500000, RoomStatus.Occupied);
        GetRoom(db, nhaB, "Phòng 202", 3000000, RoomStatus.Maintenance);
        db.SaveChanges();

        var an = GetTenant(db, "Nguyễn Văn An", "0901000001");
        var binh = GetTenant(db, "Trần Thị Bình", "0901000002");
        var cuong = GetTenant(db, "Lê Hoàng Cường", "0901000003");
        var duy = GetTenant(db, "Phạm Minh Duy", "0901000004");
        var ha = GetTenant(db, "Vũ Thu Hà", "0901000005");
        var lam = GetTenant(db, "Đỗ Minh Lâm", "0901000006");
        db.SaveChanges();

        Assign(db, p101, an, true);
        Assign(db, p101, binh, false);
        Assign(db, p102, cuong, true);
        Assign(db, p201, duy, true);
        EndedAssignment(db, p102, ha);
        db.SaveChanges();

        RefreshTenantStatuses(db);
        lam.Status = TenantStatus.Unassigned;
        db.SaveChanges();

        AddOccupiedRoomConfigs(db, p101, electricity, water, wifi, parking, 2);
        AddOccupiedRoomConfigs(db, p102, electricity, water, wifi, parking, 1);
        AddOccupiedRoomConfigs(db, p201, electricity, water, wifi, parking, 1);
        db.SaveChanges();

        AddReading(db, p101, electricity, "2026-04", 100, 160, 3500);
        AddReading(db, p102, electricity, "2026-04", 80, 120, 3500);
        AddReading(db, p201, electricity, "2026-04", 200, 260, 3500);
        db.SaveChanges();
    }

    private static void ResetDemoData(RentalManagerDbContext db)
    {
        db.Payments.RemoveRange(db.Payments);
        db.InvoiceItems.RemoveRange(db.InvoiceItems);
        db.Invoices.RemoveRange(db.Invoices);
        db.MeterReadings.RemoveRange(db.MeterReadings);
        db.RoomFeeConfigs.RemoveRange(db.RoomFeeConfigs);
        db.RoomTenants.RemoveRange(db.RoomTenants);
        db.Tenants.RemoveRange(db.Tenants);
        db.Rooms.RemoveRange(db.Rooms);
        db.Properties.RemoveRange(db.Properties);
        db.FeeTypes.RemoveRange(db.FeeTypes);
        db.SaveChanges();
    }

    private static FeeType GetFeeType(RentalManagerDbContext db, string name, CalculationType type, string unit, decimal price)
    {
        var feeType = db.FeeTypes.FirstOrDefault(x => x.Name == name);
        if (feeType is not null)
        {
            return feeType;
        }

        feeType = new FeeType { Name = name, DefaultCalculationType = type, DefaultUnit = unit, DefaultUnitPrice = price, IsSystem = true, IsActive = true };
        db.FeeTypes.Add(feeType);
        return feeType;
    }

    private static Property GetProperty(RentalManagerDbContext db, string name, string address)
    {
        var property = db.Properties.FirstOrDefault(x => x.Name == name);
        if (property is not null)
        {
            property.Address = address;
            property.IsActive = true;
            return property;
        }

        property = new Property { Name = name, Address = address, IsActive = true };
        db.Properties.Add(property);
        return property;
    }

    private static Room GetRoom(RentalManagerDbContext db, Property property, string name, decimal rent, RoomStatus status)
    {
        var room = db.Rooms.FirstOrDefault(x => x.PropertyId == property.Id && x.RoomName == name);
        if (room is not null)
        {
            room.BaseRent = rent;
            room.Status = status;
            return room;
        }

        room = new Room { Property = property, RoomName = name, BaseRent = rent, Status = status };
        db.Rooms.Add(room);
        return room;
    }

    private static Tenant GetTenant(RentalManagerDbContext db, string fullName, string phone)
    {
        var tenant = db.Tenants.FirstOrDefault(x => x.FullName == fullName);
        if (tenant is not null)
        {
            tenant.Phone = phone;
            return tenant;
        }

        tenant = new Tenant { FullName = fullName, Phone = phone };
        db.Tenants.Add(tenant);
        return tenant;
    }

    private static void Assign(RentalManagerDbContext db, Room room, Tenant tenant, bool representative)
    {
        var assignment = db.RoomTenants.FirstOrDefault(x => x.RoomId == room.Id && x.TenantId == tenant.Id && x.Status == RoomTenantStatus.Active);
        if (assignment is null)
        {
            assignment = new RoomTenant { Room = room, Tenant = tenant, Status = RoomTenantStatus.Active, StartDate = new DateTime(2026, 4, 1) };
            db.RoomTenants.Add(assignment);
        }

        assignment.IsRepresentative = representative;
        tenant.Status = TenantStatus.Renting;
    }

    private static void EndedAssignment(RentalManagerDbContext db, Room room, Tenant tenant)
    {
        var assignment = db.RoomTenants.FirstOrDefault(x => x.RoomId == room.Id && x.TenantId == tenant.Id && x.Status == RoomTenantStatus.Ended);
        if (assignment is null)
        {
            assignment = new RoomTenant
            {
                Room = room,
                Tenant = tenant,
                Status = RoomTenantStatus.Ended,
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 3, 31)
            };
            db.RoomTenants.Add(assignment);
        }

        assignment.IsRepresentative = false;
        assignment.EndDate ??= new DateTime(2026, 3, 31);
        tenant.Status = TenantStatus.Former;
    }

    private static void RefreshTenantStatuses(RentalManagerDbContext db)
    {
        foreach (var tenant in db.Tenants.Include(x => x.RoomTenants))
        {
            tenant.Status = tenant.RoomTenants.Any(x => x.Status == RoomTenantStatus.Active)
                ? TenantStatus.Renting
                : tenant.RoomTenants.Any(x => x.Status == RoomTenantStatus.Ended)
                    ? TenantStatus.Former
                    : TenantStatus.Unassigned;
        }
    }

    private static void AddOccupiedRoomConfigs(RentalManagerDbContext db, Room room, FeeType electricity, FeeType water, FeeType wifi, FeeType parking, decimal parkingQuantity)
    {
        AddConfig(db, room, electricity, CalculationType.Meter, 3500, null, null);
        AddConfig(db, room, water, CalculationType.PerPerson, 100000, null, null);
        AddConfig(db, room, wifi, CalculationType.Fixed, null, 100000, null);
        AddConfig(db, room, parking, CalculationType.PerUnit, 150000, null, parkingQuantity);
    }

    private static void AddConfig(RentalManagerDbContext db, Room room, FeeType feeType, CalculationType type, decimal? unitPrice, decimal? fixedAmount, decimal? quantity)
    {
        var config = db.RoomFeeConfigs.FirstOrDefault(x => x.RoomId == room.Id && x.FeeTypeId == feeType.Id);
        if (config is null)
        {
            config = new RoomFeeConfig { Room = room, FeeType = feeType };
            db.RoomFeeConfigs.Add(config);
        }

        config.CalculationType = type;
        config.UnitPrice = unitPrice;
        config.FixedAmount = fixedAmount;
        config.Quantity = quantity;
        config.Enabled = true;
    }

    private static void AddReading(RentalManagerDbContext db, Room room, FeeType feeType, string month, decimal previous, decimal current, decimal unitPrice)
    {
        var reading = db.MeterReadings.FirstOrDefault(x => x.RoomId == room.Id && x.FeeTypeId == feeType.Id && x.BillingMonth == month);
        if (reading is null)
        {
            reading = new MeterReading { Room = room, FeeType = feeType, BillingMonth = month };
            db.MeterReadings.Add(reading);
        }

        reading.PreviousReading = previous;
        reading.CurrentReading = current;
        reading.UsageAmount = current - previous;
        reading.UnitPriceSnapshot = unitPrice;
        reading.Amount = reading.UsageAmount * unitPrice;
    }
}
