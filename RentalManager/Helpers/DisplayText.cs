using RentalManager.Enums;

namespace RentalManager.Helpers;

public static class DisplayText
{
    public static string For(RoomStatus status) => status switch
    {
        RoomStatus.Occupied => "Đang cho thuê",
        RoomStatus.Vacant => "Đang trống",
        RoomStatus.Maintenance => "Bảo trì",
        RoomStatus.Inactive => "Ngừng sử dụng",
        _ => status.ToString()
    };

    public static string For(RoomTenantStatus status) => status switch
    {
        RoomTenantStatus.Active => "Đang ở",
        RoomTenantStatus.Ended => "Đã kết thúc",
        _ => status.ToString()
    };

    public static string For(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Draft => "Nháp",
        InvoiceStatus.Issued => "Đã chốt",
        InvoiceStatus.Partial => "Trả một phần",
        InvoiceStatus.Paid => "Đã trả",
        InvoiceStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    public static string For(CalculationType type) => type switch
    {
        CalculationType.Fixed => "Cố định",
        CalculationType.Meter => "Theo chỉ số",
        CalculationType.PerPerson => "Theo người",
        CalculationType.PerUnit => "Theo số lượng",
        CalculationType.Manual => "Nhập tay",
        _ => type.ToString()
    };

    public static string For(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Tiền mặt",
        PaymentMethod.BankTransfer => "Chuyển khoản",
        PaymentMethod.Momo => "Momo",
        PaymentMethod.Other => "Khác",
        _ => method.ToString()
    };

    public static string FeeName(string name) => name switch
    {
        "Electricity" => "Điện",
        "Water" => "Nước",
        "Parking" => "Giữ xe",
        "Garbage" => "Rác",
        "Other" => "Khác",
        _ => name
    };
}
