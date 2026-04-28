# TODO_UX_INTERACTION_REFINEMENT

## Goal

The app is now functional, but the UI/UX still feels like a technical prototype.

This iteration should improve real user usability for a landlord who only uses the app 1-2 times per month.

The app should be easy to understand, easy to check, easy to edit, and safe from confusing actions.

Do not change the database schema unless absolutely necessary.  
Do not change the billing logic unless required by UI behavior.  
Focus on interaction design, input formatting, table actions, filters, and Vietnamese error handling.

---

## 1. Money Input Formatting

### Problem

Money input fields currently require users to type raw numbers like:

```text
3000000
```

This is hard to read.

### Required Behavior

For all money input fields, automatically format input with thousand separators while typing or when the field loses focus.

Examples:

```text
3000000 -> 3,000,000
150000 -> 150,000
3500 -> 3,500
```

Apply this to:

- Tiền phòng
- Đơn giá
- Cố định
- Số tiền thanh toán
- Any other money input fields

### Internal Rule

The displayed value may contain commas, but the internal value must still be parsed and saved as a numeric decimal.

Example:

```text
Displayed: 3,000,000
Saved value: 3000000
```

### Validation

If the user enters invalid characters, show this Vietnamese error message:

```text
Vui lòng nhập số tiền hợp lệ.
```

---

## 2. Month and Year Picker UX

### Problem

Billing month is currently typed manually as text:

```text
2026-04
```

This is easy to mistype.

### Required Behavior

Replace plain text billing month input with a controlled month/year selection UI.

If a real WPF month picker is not available, implement this simple solution:

- ComboBox for month: 01 to 12
- ComboBox for year: current year ± 5 years
- Store selected billing month as `YYYY-MM`

Apply this to:

- Top global billing month selector
- Dashboard filter
- Invoice filter
- Meter reading filter
- Payment filter where relevant

### Default

When the app opens:

- Select current month
- Select current year
- Do not require the user to manually type `YYYY-MM`

---

## 3. Dashboard Simplification

### Problem

Dashboard has too many sections and can feel crowded.

Large sections like:

- Hóa đơn chưa thu
- Phòng thiếu chỉ số

may not need to be large tables on the dashboard.

### Required Changes

Dashboard should focus on monthly overview.

Keep these cards:

- Dự kiến thu
- Đã thu
- Chưa thu
- Phòng đang thuê
- Phòng trống
- Hóa đơn chưa thanh toán

Remove or reduce the visual priority of:

- Hóa đơn chưa thu table
- Phòng thiếu chỉ số table

Instead, show a small warning summary if needed:

```text
Có X phòng thiếu chỉ số điện/nước trong tháng này.
```

Add an action button/link if needed:

```text
Xem phòng thiếu chỉ số
```

### Dashboard Main Tables

Dashboard should have at most 2 main tables:

1. Hóa đơn trong tháng
2. Thanh toán gần đây

Avoid showing too many tables at the same time.

---

## 4. Better Filters for List Screens

### Problem

Several tabs have weak filters. For example, the Room tab does not clearly allow filtering by property or room.

### Required Filters

#### Room Screen

Add clear filters:

- Nhà / khu trọ
- Trạng thái
- Tìm theo tên phòng
- Tìm theo người đại diện

#### Invoice Screen

Add clear filters:

- Tháng
- Năm
- Nhà / khu trọ
- Phòng
- Trạng thái hóa đơn
- Tìm theo người đại diện

#### Payment Screen

Add clear filters:

- Tháng
- Năm
- Nhà / khu trọ
- Phòng
- Phương thức
- Tìm theo người đại diện

#### Meter Reading Screen

Add clear filters:

- Tháng
- Năm
- Nhà / khu trọ
- Phòng
- Loại phí

#### Room Fee Screen

Keep filters but make them clearer:

- Nhà / khu trọ
- Phòng
- Loại phí
- Trạng thái áp dụng

### Filter Behavior

Filters should update the table after the user clicks:

```text
Lọc
```

Also provide:

```text
Xóa lọc
```

to reset filters.

---

## 5. Row-Level Actions in Tables

### Problem

Actions are currently mostly global buttons:

- Sửa dòng chọn
- Lưu thay đổi
- Ngưng sử dụng

This makes the user perform too many steps.

### Required Change

For tables that support editing, add row-level action buttons.

Each editable row should show actions where appropriate:

- Sửa
- Lưu
- Hủy
- Ngưng dùng / Ngưng áp dụng
- Xem chi tiết
- Thanh toán

### Desired Interaction

For editable list rows:

1. User clicks `Sửa` on that row.
2. That row becomes editable or its values are loaded into an edit panel clearly tied to that row.
3. The `Sửa` button changes to `Lưu`.
4. A `Hủy` button appears.
5. User clicks `Lưu` to save the row.
6. Table refreshes.
7. Show a Vietnamese success message.

### Apply Row-Level Actions To

- Nhà / Khu trọ
- Phòng
- Người thuê
- Loại phí
- Phí theo phòng
- Hóa đơn
- Thanh toán where applicable

### Important Rule

Do not make every table cell always editable. That is confusing.

Only allow edit mode after the user clicks `Sửa`.

---

## 6. Invoice Screen Simplification

### Problem

The invoice screen currently feels too complex and crowded.

### Required Redesign

Split the invoice screen into clearer sections:

1. Filter area
2. Invoice list
3. Selected invoice summary
4. Invoice detail items
5. Payment action area

### Invoice List Columns

Show only useful columns:

- Tháng
- Nhà / khu trọ
- Phòng
- Người đại diện
- Tổng tiền
- Đã thu
- Còn lại
- Trạng thái
- Hành động

Do not show unnecessary technical columns.

### Row Actions

Each invoice row should have buttons:

- Chi tiết
- Thanh toán
- Sao chép
- Hủy

If status is Draft:

- Show `Chốt hóa đơn`

If status is Paid:

- Disable payment button or show `Đã thanh toán`

### Payment UX

When the user clicks `Thanh toán`:

- Show selected invoice clearly.
- Default payment amount should be the remaining amount.
- User can edit amount.
- Payment method default should be `Tiền mặt`.
- Button text should be `Ghi nhận thanh toán`.

If invoice is already paid, show:

```text
Hóa đơn này đã được thanh toán đủ.
```

---

## 7. Vietnamese Error Handling

### Problem

Some caught errors still show English messages.

Example:

```text
Invoice already exists for this room and billing month.
```

### Required Change

All user-facing errors must be Vietnamese.

Examples:

```text
Invoice already exists for this room and billing month.
```

should become:

```text
Hóa đơn của phòng này trong tháng đã chọn đã tồn tại.
```

```text
Invalid amount.
```

should become:

```text
Số tiền không hợp lệ.
```

```text
Current reading must be greater than or equal to previous reading.
```

should become:

```text
Chỉ số mới phải lớn hơn hoặc bằng chỉ số cũ.
```

```text
Room is required.
```

should become:

```text
Vui lòng chọn phòng.
```

```text
Fee type is required.
```

should become:

```text
Vui lòng chọn loại phí.
```

```text
Payment amount must be greater than zero.
```

should become:

```text
Số tiền thanh toán phải lớn hơn 0.
```

### Required Work

Audit all service and ViewModel exception messages.

Create a centralized user-friendly message helper if useful, for example:

```text
UserMessageService
```

or:

```text
ErrorMessageMapper
```

Do not leak raw technical exception messages to the UI unless in debug mode.

---

## 8. Error Handling Quality Check

### Required Checks

Review and improve error handling for:

- Creating duplicate invoices
- Missing meter readings
- Invalid money input
- Invalid room/fee selection
- Invalid payment amount
- Trying to pay an already paid invoice
- Trying to edit or deactivate system fee types
- Backup and restore errors
- Database save errors

### UI Behavior

When an error happens:

- Show Vietnamese message.
- Do not crash the app.
- Keep user input if possible.
- Make it clear what the user should fix.

When an operation succeeds:

- Show a short Vietnamese success message.

Examples:

```text
Đã lưu thay đổi.
Đã tạo hóa đơn.
Đã ghi nhận thanh toán.
Đã tạo dữ liệu mẫu.
Đã sao lưu dữ liệu.
```

---

## 9. Terminology Consistency

Use consistent Vietnamese terms:

| English concept | Vietnamese display |
|---|---|
| Property | Nhà / khu trọ |
| Room | Phòng |
| Tenant | Người thuê |
| Representative tenant | Người đại diện |
| Fee Type | Loại phí |
| Room Fee Config | Phí theo phòng |
| Meter Reading | Chỉ số điện nước |
| Invoice | Hóa đơn |
| Payment | Thanh toán |
| Draft | Nháp |
| Issued | Đã chốt |
| Partial | Thanh toán một phần |
| Paid | Đã trả |
| Cancelled | Đã hủy |

Avoid mixing English labels like:

- Room Rent
- Electricity
- Water
- Parking
- Draft
- Paid
- True / False

All visible text should be Vietnamese.

---

## 10. Visual Usability

Without doing a full redesign, improve visual clarity:

- Add more spacing between sections.
- Make section titles clearer.
- Align form labels and inputs consistently.
- Keep table columns readable.
- Avoid overly wide empty areas where possible.
- Use consistent button order.
- Highlight selected invoice/room more clearly.
- Use status text clearly.

Do not focus on advanced styling or themes yet.

---

## 11. Build and Verification

After changes, run:

```powershell
dotnet build RentalManager.sln
```

Build must succeed with 0 errors.

Then run the app and manually verify:

1. Money inputs format as `3,000,000`.
2. Billing month can be selected without manually typing `YYYY-MM`.
3. Dashboard is simpler and easier to read.
4. Room screen has useful filters.
5. Invoice screen is easier to use.
6. Row-level edit actions work.
7. User-facing errors are Vietnamese.
8. Duplicate invoice error is shown in Vietnamese.
9. Payment flow handles already paid invoice cleanly.
10. Data remains saved after app restart.
