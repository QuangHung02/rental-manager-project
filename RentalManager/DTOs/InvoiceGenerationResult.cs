namespace RentalManager.DTOs;

public class InvoiceGenerationResult
{
    public int CreatedCount { get; set; }
    public List<InvoiceGenerationSkipRow> SkippedRooms { get; set; } = new();

    public string SummaryText
    {
        get
        {
            var skippedCount = SkippedRooms.Count;
            if (skippedCount == 0)
            {
                return $"Đã tạo {CreatedCount} hóa đơn. Không có phòng bị bỏ qua.";
            }

            return $"Đã tạo {CreatedCount} hóa đơn. Bỏ qua {skippedCount} phòng.";
        }
    }
}

public class InvoiceGenerationSkipRow
{
    public string PropertyName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
