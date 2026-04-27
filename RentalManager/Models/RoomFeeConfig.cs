using RentalManager.Enums;

namespace RentalManager.Models;

public class RoomFeeConfig
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public Room? Room { get; set; }
    public int FeeTypeId { get; set; }
    public FeeType? FeeType { get; set; }
    public CalculationType CalculationType { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? FixedAmount { get; set; }
    public decimal? Quantity { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Note { get; set; }
    public string RoomName => Room?.RoomName ?? string.Empty;
    public string FeeTypeName => FeeType?.Name ?? string.Empty;
}
