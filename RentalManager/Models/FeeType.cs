using RentalManager.Enums;

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
}
