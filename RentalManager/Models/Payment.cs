using RentalManager.Enums;
using RentalManager.Helpers;

namespace RentalManager.Models;

public class Payment
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string BillingMonth => Invoice?.BillingMonth ?? string.Empty;
    public string RoomName => Invoice?.Room?.RoomName ?? string.Empty;
    public string PropertyName => Invoice?.Room?.Property?.Name ?? string.Empty;
    public string MethodText => DisplayText.For(Method);
}
