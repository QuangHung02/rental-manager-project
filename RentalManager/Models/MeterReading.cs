namespace RentalManager.Models;

public class MeterReading
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public int FeeTypeId { get; set; }
    public FeeType? FeeType { get; set; }
    public string BillingMonth { get; set; } = DateTime.Today.ToString("yyyy-MM");
    public decimal PreviousReading { get; set; }
    public decimal CurrentReading { get; set; }
    public decimal UsageAmount { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public string PropertyName => Room?.Property?.Name ?? string.Empty;
    public string RoomName => Room?.RoomName ?? string.Empty;
    public string FeeTypeName => FeeType?.Name ?? string.Empty;
}
