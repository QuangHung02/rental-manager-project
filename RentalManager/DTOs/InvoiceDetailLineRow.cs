namespace RentalManager.DTOs;

public class InvoiceDetailLineRow
{
    public string ItemName { get; set; } = string.Empty;
    public string CalculationTypeText { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public bool IsMeter { get; set; }
    public decimal? PreviousReading { get; set; }
    public decimal? CurrentReading { get; set; }
    public string? MeterEvidenceNote { get; set; }
    public bool HasMeterEvidenceNote => !string.IsNullOrWhiteSpace(MeterEvidenceNote);
}
