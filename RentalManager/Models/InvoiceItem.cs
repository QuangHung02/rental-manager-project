using RentalManager.Enums;

namespace RentalManager.Models;

public class InvoiceItem
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public int? FeeTypeId { get; set; }
    public FeeType? FeeType { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public CalculationType CalculationType { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
