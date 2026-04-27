using RentalManager.Enums;

namespace RentalManager.Models;

public class Invoice
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public string BillingMonth { get; set; } = DateTime.Today.ToString("yyyy-MM");
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal ExtraAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime IssuedDate { get; set; } = DateTime.Today;
    public DateTime? PaidDate { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public string PropertyName => Room?.Property?.Name ?? string.Empty;
    public string RoomName => Room?.RoomName ?? string.Empty;
    public string RepresentativeTenantName => Room?.RoomTenants.FirstOrDefault(x => x.IsRepresentative && x.Status == Enums.RoomTenantStatus.Active)?.Tenant?.FullName ?? string.Empty;
}
