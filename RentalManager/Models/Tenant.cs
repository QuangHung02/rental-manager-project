using RentalManager.Enums;
using RentalManager.Helpers;

namespace RentalManager.Models;

public class Tenant
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? IdentityNumber { get; set; }
    public TenantStatus Status { get; set; } = TenantStatus.Unassigned;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<RoomTenant> RoomTenants { get; set; } = new List<RoomTenant>();
    public string StatusText => DisplayText.For(Status);
    public string AssignmentDisplayText
    {
        get
        {
            var details = new[] { Phone, IdentityNumber }
                .Where(x => !string.IsNullOrWhiteSpace(x));
            var suffix = string.Join(" - ", details);
            return string.IsNullOrWhiteSpace(suffix) ? FullName : $"{FullName} - {suffix}";
        }
    }
}
