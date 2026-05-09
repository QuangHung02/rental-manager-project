using RentalManager.Enums;
using RentalManager.Helpers;

namespace RentalManager.Models;

public class Room
{
    public int Id { get; set; }
    public int PropertyId { get; set; }
    public Property? Property { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public string? Floor { get; set; }
    public decimal BaseRent { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Vacant;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<RoomTenant> RoomTenants { get; set; } = new List<RoomTenant>();
    public ICollection<RoomFeeConfig> FeeConfigs { get; set; } = new List<RoomFeeConfig>();
    public ICollection<MeterReading> MeterReadings { get; set; } = new List<MeterReading>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public string PropertyName => Property?.Name ?? string.Empty;
    public string DisplayNameWithProperty => string.IsNullOrWhiteSpace(PropertyName) ? RoomName : $"{PropertyName} - {RoomName}";
    public string RepresentativeTenantName => RoomTenants.FirstOrDefault(x => x.IsRepresentative && x.Status == Enums.RoomTenantStatus.Active)?.Tenant?.FullName ?? string.Empty;
    public string StatusText => DisplayText.For(Status);
    public bool IsOccupied => Status == RoomStatus.Occupied;
}
