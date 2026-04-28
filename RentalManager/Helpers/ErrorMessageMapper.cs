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

        if (message.Contains("Room was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy phòng đã chọn.";
        }

        if (message.Contains("Fee type was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy loại phí đã chọn.";
        }

        if (message.Contains("Invoice was not found", StringComparison.OrdinalIgnoreCase))
        {
            return "Không tìm thấy hóa đơn đã chọn.";
        }

        if (message.Contains("database is locked", StringComparison.OrdinalIgnoreCase))
        {
            return "Dữ liệu đang được sử dụng. Vui lòng đóng thao tác khác rồi thử lại.";
        }

        if (exception is DbUpdateException or SqliteException)
        {
            return "Không thể lưu dữ liệu. Vui lòng kiểm tra thông tin và thử lại.";
        }

        return string.IsNullOrWhiteSpace(message) ? "Đã xảy ra lỗi. Vui lòng thử lại." : message;
    }
}
