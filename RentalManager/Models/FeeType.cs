using RentalManager.Enums;
using RentalManager.Helpers;

namespace RentalManager.Models;

public class FeeType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CalculationType DefaultCalculationType { get; set; } = CalculationType.Fixed;
    public string? DefaultUnit { get; set; }
    public decimal DefaultUnitPrice { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
    public string DisplayName => DisplayText.FeeName(Name);
    public string CalculationTypeText => DisplayText.For(DefaultCalculationType);
    public string SystemStatusText => IsSystem ? "Mặc định" : "Tùy chỉnh";
    public string ActiveStatusText => IsActive ? "Đang dùng" : "Ngừng dùng";
    public string ToggleActionText => IsActive ? "Ngừng" : "Bật lại";
}
