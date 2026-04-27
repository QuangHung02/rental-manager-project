using RentalManager.Enums;

namespace RentalManager.Models;

public class RoomTenant
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public int TenantId { get; set; }
    public Tenant? Tenant { get; set; }
    public bool IsRepresentative { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime? EndDate { get; set; }
    public RoomTenantStatus Status { get; set; } = RoomTenantStatus.Active;
    public string RoomName => Room?.RoomName ?? string.Empty;
    public string PropertyName => Room?.Property?.Name ?? string.Empty;
    public string TenantName => Tenant?.FullName ?? string.Empty;
}
