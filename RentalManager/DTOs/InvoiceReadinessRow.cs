namespace RentalManager.DTOs;

public class InvoiceReadinessRow
{
    public int RoomId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string StatusText { get; set; } = string.Empty;
}
