namespace RentalManager.DTOs;

public class DashboardSummary
{
    public decimal ExpectedRevenue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int OccupiedRoomCount { get; set; }
    public int VacantRoomCount { get; set; }
    public int MissingReadingCount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
}
