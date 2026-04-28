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
    public bool UsesDefaultPrice => FeeType is not null && CalculationType == FeeType.DefaultCalculationType && CalculationType switch
    {
        CalculationType.Fixed => FixedAmount is null,
        CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit => UnitPrice is null,
        _ => false
    };
    public string AppliedPriceText => MissingRequiredCustomPrice ? "Chưa nhập giá riêng" : CalculationType switch
    {
        CalculationType.Fixed => $"{FormatMoney(GetAppliedFixedAmount())} / tháng {PriceSourceText}",
        CalculationType.Meter => $"{FormatMoney(GetAppliedUnitPrice())} / {DisplayUnit(FeeType?.DefaultUnit)} {PriceSourceText}",
        CalculationType.PerPerson => $"{FormatMoney(GetAppliedUnitPrice())} / người {PriceSourceText}",
        CalculationType.PerUnit => $"{FormatMoney(GetAppliedUnitPrice())} × {FormatQuantity(Quantity ?? 1)} {PriceSourceText}",
        CalculationType.Manual => $"{FormatMoney(FixedAmount ?? 0)} (riêng)",
        _ => string.Empty
    };
    private string PriceSourceText => UsesDefaultPrice ? "(mặc định)" : "(riêng)";
    private bool MissingRequiredCustomPrice => !UsesDefaultPrice && CalculationType switch
    {
        CalculationType.Fixed => FixedAmount is null,
        CalculationType.Meter or CalculationType.PerPerson or CalculationType.PerUnit => UnitPrice is null,
        CalculationType.Manual => FixedAmount is null,
        _ => false
    };

    private decimal GetAppliedFixedAmount()
    {
        if (FixedAmount is not null)
        {
            return FixedAmount.Value;
        }

        return UsesDefaultPrice ? FeeType?.DefaultUnitPrice ?? 0 : 0;
    }

    private decimal GetAppliedUnitPrice()
    {
        if (UnitPrice is not null)
        {
            return UnitPrice.Value;
        }

        return UsesDefaultPrice ? FeeType?.DefaultUnitPrice ?? 0 : 0;
    }

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
