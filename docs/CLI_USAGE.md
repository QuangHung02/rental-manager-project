# RentalManager CLI Usage

RentalManager CLI (`RentalManager.Cli.exe`) cho phép các công cụ tự động hóa hoặc AI agent tương tác với phần mềm quản lý nhà trọ RentalManager thông qua dòng lệnh.
Tất cả các lệnh đều trả về kết quả dưới định dạng JSON, giúp các hệ thống khác dễ dàng đọc hiểu.

## Cấu trúc chung

Cú pháp lệnh cơ bản:
```bash
RentalManager.Cli.exe <command> <sub-command> [options]
```

## Lệnh hiện có

- `invoice create`: tạo hóa đơn cho một phòng trong tháng.
- `invoice unpaid`: xem hóa đơn đã chốt/chưa thanh toán đủ.
- `meter add`: ghi nhận hoặc cập nhật chỉ số điện/nước.
- `payment add`: ghi nhận thanh toán cho hóa đơn.
- `seed-test`: tạo dữ liệu mẫu phục vụ kiểm thử/dev.

## 1. Ghi nhận chỉ số điện / nước (meter add)

Ghi nhận chỉ số điện/nước mới cho một phòng trong tháng nhất định. Hệ thống tự động tìm chỉ số của tháng trước, tính mức tiêu thụ và lưu lại.

**Lệnh:**
```bash
RentalManager.Cli.exe meter add --property "Nhà A" --room "Phòng 202" --fee "Điện" --month "2026-04" --current 160
```

**JSON Kết quả (Thành công):**
```json
{
  "success": true,
  "code": "METER_READING_SAVED",
  "message": "Đã cập nhật chỉ số.",
  "data": {
    "room": "Nhà A - Phòng 202",
    "fee": "Điện",
    "billingMonth": "2026-04",
    "previous": 120,
    "current": 160
  }
}
```

## 2. Tạo hóa đơn (invoice create)

Tạo hóa đơn cho phòng. Nếu phát hiện thiếu chỉ số, hệ thống sẽ trả về lỗi chi tiết dạng JSON.

**Lệnh:**
```bash
RentalManager.Cli.exe invoice create --property "Nhà A" --room "Phòng 202" --month "2026-04"
```

**JSON Kết quả (Thành công):**
```json
{
  "success": true,
  "code": "INVOICE_CREATED",
  "message": "Đã tạo hóa đơn.",
  "data": {
    "invoiceId": 12,
    "room": "Nhà A - Phòng 202",
    "billingMonth": "2026-04",
    "totalAmount": 3550000,
    "remainingAmount": 3550000
  }
}
```

**JSON Lỗi thiếu chỉ số (Thất bại):**
```json
{
  "success": false,
  "code": "MISSING_METER_READING",
  "message": "Phòng này còn thiếu chỉ số điện/nước cho kỳ hóa đơn đã chọn.",
  "details": {
    "room": "Nhà A - Phòng 202",
    "billingMonth": "2026-04"
  }
}
```

## 3. Xem danh sách hóa đơn chưa thu (invoice unpaid)

Liệt kê danh sách các hóa đơn chưa được thanh toán hoặc mới thanh toán một phần.

**Lệnh:**
```bash
RentalManager.Cli.exe invoice unpaid --month "2026-04"
```

**JSON Kết quả:**
```json
{
  "success": true,
  "code": "OK",
  "count": 1,
  "message": "Tìm thấy 1 hóa đơn chưa thanh toán.",
  "data": [
    {
      "invoiceId": 12,
      "room": "Nhà A - Phòng 202",
      "billingMonth": "2026-04",
      "totalAmount": 3550000,
      "remainingAmount": 3550000,
      "status": "Đã chốt"
    }
  ]
}
```

**JSON khi không có hóa đơn phù hợp:**
```json
{
  "success": true,
  "code": "OK",
  "count": 0,
  "message": "Không có hóa đơn chưa thanh toán phù hợp.",
  "data": []
}
```

## 4. Ghi nhận thanh toán (payment add)

Ghi nhận một khoản thanh toán cho hóa đơn (Dùng mã `invoiceId` được trả về từ các lệnh trên).

**Lệnh:**
```bash
RentalManager.Cli.exe payment add --invoice 12 --amount 3550000 --method "BankTransfer" --note "Chuyển khoản"
```

**Tham số `--method` hỗ trợ:**
- `Cash` (Tiền mặt)
- `BankTransfer` (Chuyển khoản)
- `Momo`
- `Other` (Khác)

**JSON Kết quả:**
```json
{
  "success": true,
  "code": "PAYMENT_RECORDED",
  "message": "Đã ghi nhận thanh toán.",
  "data": {
    "paymentId": 5,
    "invoiceId": 12,
    "amount": 3550000,
    "method": "BankTransfer"
  }
}
```

## Ví dụ lỗi thường gặp

**Không tìm thấy phòng:**
```json
{
  "success": false,
  "code": "ROOM_NOT_FOUND",
  "message": "Không tìm thấy phòng: Nhà A Phòng 999",
  "nextAction": "Kiểm tra lại --property và --room.",
  "details": null
}
```

**Hóa đơn đã tồn tại:**
```json
{
  "success": false,
  "code": "INVOICE_ALREADY_EXISTS",
  "message": "Hóa đơn của phòng này trong tháng đã chọn đã tồn tại.",
  "nextAction": "Dùng hóa đơn hiện có hoặc chọn tháng/phòng khác.",
  "details": null
}
```

**Thiếu chỉ số điện/nước:**
```json
{
  "success": false,
  "code": "MISSING_METER_READING",
  "message": "Phòng này còn thiếu chỉ số điện/nước cho kỳ hóa đơn đã chọn.",
  "nextAction": "Nhập chỉ số điện/nước cho tháng đang xử lý rồi tạo hóa đơn lại.",
  "details": {
    "room": "Phòng 202",
    "billingMonth": "2026-04"
  }
}
```

**Số tiền thanh toán không hợp lệ:**
```json
{
  "success": false,
  "code": "PAYMENT_ERROR",
  "message": "Số tiền thanh toán phải lớn hơn 0.",
  "nextAction": "Kiểm tra invoiceId, số tiền còn lại và số tiền thanh toán.",
  "details": null
}
```
