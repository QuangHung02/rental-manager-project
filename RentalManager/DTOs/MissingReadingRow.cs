namespace RentalManager.DTOs;

public class MissingReadingRow
{
    public string PropertyName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string FeeTypeName { get; set; } = string.Empty;
    public decimal PreviousReading { get; set; }
}
