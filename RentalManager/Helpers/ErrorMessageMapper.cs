using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace RentalManager.Helpers;

public static class ErrorMessageMapper
{
    public static string ToUserMessage(Exception exception)
    {
        var message = exception.InnerException?.Message ?? exception.Message;

        if (message.Contains("Invoice already exists", StringComparison.OrdinalIgnoreCase)
            || message.Contains("UNIQUE constraint failed: Invoices.RoomId, Invoices.BillingMonth", StringComparison.OrdinalIgnoreCase))
        {
            return "Hóa đơn của phòng này trong tháng đã chọn đã tồn tại.";
        }

        if (message.Contains("Payment amount must be greater", StringComparison.OrdinalIgnoreCase))
        {
            return "Số tiền thanh toán phải lớn hơn 0.";
        }

        if (message.Contains("Payment amount exceeds", StringComparison.OrdinalIgnoreCase))
        {
            return "Số tiền thanh toán vượt quá số tiền còn lại.";
        }

        if (message.Contains("Current reading must be greater", StringComparison.OrdinalIgnoreCase))
        {
            return "Chỉ số mới phải lớn hơn hoặc bằng chỉ số cũ.";
        }

        if (message.Contains("Choose a room and fee type", StringComparison.OrdinalIgnoreCase))
        {
            return "Vui lòng chọn phòng và loại phí.";
        }

        if (message.Contains("Value could not be converted", StringComparison.OrdinalIgnoreCase))
        {
            return "Vui lòng chọn giá trị hợp lệ.";
        }

        if (message.Contains("Input string was not in a correct format", StringComparison.OrdinalIgnoreCase)
            || message.Contains("The input string", StringComparison.OrdinalIgnoreCase)
            || message.Contains("is not a valid value", StringComparison.OrdinalIgnoreCase))
        {
            return "Dữ liệu nhập không đúng định dạng.";
        }

        if (message.Contains("Room was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy phòng đã chọn.";
        }

        if (message.Contains("Property was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy nhà / khu trọ đã chọn.";
        }

        if (message.Contains("Fee type was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy loại phí đã chọn.";
        }

        if (message.Contains("Property name is required", StringComparison.OrdinalIgnoreCase))
        {
            return "Tên nhà / khu trọ là bắt buộc.";
        }

        if (message.Contains("Room must belong to a property", StringComparison.OrdinalIgnoreCase))
        {
            return "Vui lòng chọn nhà / khu trọ cho phòng.";
        }

        if (message.Contains("Tenant full name is required", StringComparison.OrdinalIgnoreCase))
        {
            return "Họ tên người thuê là bắt buộc.";
        }

        if (message.Contains("Fee type name is required", StringComparison.OrdinalIgnoreCase))
        {
            return "Tên khoản phí là bắt buộc.";
        }

        if (message.Contains("Default unit price must be greater than or equal to 0", StringComparison.OrdinalIgnoreCase))
        {
            return "Đơn giá mặc định phải lớn hơn hoặc bằng 0.";
        }

        if (message.Contains("Invoice was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy hóa đơn đã chọn.";
        }

        if (message.Contains("database is locked", StringComparison.OrdinalIgnoreCase))
        {
            return "Dữ liệu đang được sử dụng. Vui lòng đóng thao tác khác rồi thử lại.";
        }

        if (message.Contains("UNIQUE constraint failed: RoomFeeConfigs.RoomId, RoomFeeConfigs.FeeTypeId", StringComparison.OrdinalIgnoreCase))
        {
            return "Loại phí này đã tồn tại cho phòng đã chọn. Vui lòng chuyển bộ lọc sang Tất cả hoặc Ngừng áp dụng để chỉnh sửa cấu hình hiện có.";
        }

        if (exception is DbUpdateException or SqliteException)
        {
            return "Không thể lưu dữ liệu. Vui lòng kiểm tra thông tin và thử lại.";
        }

        return string.IsNullOrWhiteSpace(message) ? "Đã xảy ra lỗi. Vui lòng thử lại." : message;
    }
}
