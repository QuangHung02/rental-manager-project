using RentalManager.Enums;
using RentalManager.Helpers;

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
    public string PropertyName => Room?.PropertyName ?? string.Empty;
    public string RoomDisplayNameWithProperty => string.IsNullOrWhiteSpace(PropertyName) ? RoomName : $"{PropertyName} - {RoomName}";
    public string FeeTypeName => FeeType?.DisplayName ?? string.Empty;
    public string CalculationTypeText => DisplayText.For(CalculationType);
    public bool IsFeeTypeActive => FeeType?.IsActive != false;
    public bool IsEffectivelyActive => Enabled && IsFeeTypeActive;
    public string EnabledText => IsEffectivelyActive
        ? "Đang áp dụng"
        : Enabled && !IsFeeTypeActive
            ? "Không hiệu lực - Loại phí đã ngừng"
            : "Ngừng áp dụng";
    public string ToggleActionText => Enabled ? "Ngừng" : "Bật lại";
    public bool UsesDefaultPrice => CalculationType switch
    {
        CalculationType.Fixed => FixedAmount is null,
        CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit => UnitPrice is null,
        _ => false
    };
    public string AppliedPriceText => CalculationType switch
    {
        CalculationType.Fixed => $"{FormatMoney(FixedAmount ?? FeeType?.DefaultUnitPrice ?? 0)} / tháng {PriceSourceText}",
        CalculationType.Meter => $"{FormatMoney(UnitPrice ?? FeeType?.DefaultUnitPrice ?? 0)} / {DisplayUnit(FeeType?.DefaultUnit)} {PriceSourceText}",
        CalculationType.PerPerson => $"{FormatMoney(UnitPrice ?? FeeType?.DefaultUnitPrice ?? 0)} / người {PriceSourceText}",
        CalculationType.PerUnit => $"{FormatMoney(UnitPrice ?? FeeType?.DefaultUnitPrice ?? 0)} × {FormatQuantity(Quantity ?? 1)} {PriceSourceText}",
        CalculationType.Manual => $"{FormatMoney(FixedAmount ?? 0)} (riêng)",
        _ => string.Empty
    };
    private string PriceSourceText => UsesDefaultPrice ? "(mặc định)" : "(riêng)";

    private static string FormatMoney(decimal value) => value.ToString("N0");

    private static string FormatQuantity(decimal value) => value.ToString("N0");

    private static string DisplayUnit(string? unit) => unit switch
    {
        "person" => "người",
        "month" => "tháng",
        "unit" => "lần",
        null or "" => "đơn vị",
        _ => unit
    };
}
