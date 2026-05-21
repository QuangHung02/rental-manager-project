namespace RentalManager.DTOs;

public class DashboardMonthlySummary
{
    public string BillingMonth { get; set; } = string.Empty;
    public decimal ExpectedRevenue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int UnpaidInvoiceCount { get; set; }
}
