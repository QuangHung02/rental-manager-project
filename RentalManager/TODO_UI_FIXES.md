# TODO_UI_FIXES - Usability and Business Flow Improvements

## Current Status

The app foundation works. Demo data exists. The main issue now is usability.

The UI currently has several forms with blank input boxes, unclear fields, technical statuses, and limited dashboard filtering. This iteration should improve clarity and align the app closer to real landlord usage.

Do not change the core database model unless required. Focus on UI clarity, filtering, and workflow behavior.

---

## 1. Dashboard Filter Improvements

### Current Problem

The Dashboard currently shows summary data for the selected billing month, but the filter is too limited and not user-friendly.

### Required Changes

Add dashboard filters:

- Billing month
- Billing year
- Quick range selector

Quick range options:

- Tháng hiện tại
- 3 tháng gần nhất
- 6 tháng gần nhất
- Năm hiện tại
- Tùy chọn tháng

Default behavior:

- When the app opens, Dashboard should default to the current billing month.
- Do not default to all-time totals.
- Dashboard should prioritize monthly summary, not lifetime summary.

### Dashboard Calculation Rules

If filter is one month:

- Show totals for that month only.

If filter is 3 months / 6 months:

- Show aggregated totals for that selected range.
- The invoice table should show invoices from that range.

If filter is current year:

- Show totals from January to December of selected year.

### UI Labels

Use Vietnamese labels:

- Tháng tính tiền
- Năm
- Khoảng thời gian
- Tháng hiện tại
- 3 tháng gần nhất
- 6 tháng gần nhất
- Năm hiện tại
- Tùy chọn tháng

---

## 2. Add Clear Labels for All Input Fields

### Current Problem

Many screens show blank input boxes without labels, so the user does not know what each field means.

### Required Change

Every input field must have a visible label above or beside it.

Do not rely on placeholder text only. Use clear labels.

---

## 3. Properties Screen Improvements

### Current Problem

The Properties screen has 3 blank input boxes without clear labels.

It also shows `Đang dùng`, but the user cannot clearly interact with it and it is not necessary for the main workflow.

### Required Changes

Rename screen group title:

- `Thông tin nhà / khu trọ`

Input fields should be clearly labeled:

- Tên nhà / khu trọ
- Địa chỉ
- Ghi chú

Buttons:

- Thêm mới
- Lưu thay đổi
- Sửa dòng chọn
- Ngưng sử dụng
- Làm mới

Table columns:

- Tên nhà / khu trọ
- Địa chỉ
- Ghi chú

Hide or remove from visible table:

- Raw Id
- IsActive / Đang dùng checkbox, unless it is shown as readable text

If status is needed, show it as text:

- Đang dùng
- Ngưng sử dụng

Do not show an editable checkbox in the table for active status.

### Behavior

- When user selects a row and clicks `Sửa dòng chọn`, populate form fields.
- User edits fields.
- User clicks `Lưu thay đổi`.
- Changes are saved and table refreshes.

---

## 4. Room Screen Improvements

### Current Problem

Room status currently has too many values, including Maintenance. For this app's current scope, the landlord mainly needs to know if a room is rented or empty.

### Required Changes

For visible UI, only use two main statuses:

- Đang cho thuê
- Đang trống

Internal enum can remain unchanged if needed, but UI should simplify it.

Mapping:

- Occupied → Đang cho thuê
- Vacant → Đang trống
- Maintenance and Inactive should not be primary options in the normal room form for now.

Input fields must have labels:

- Nhà / khu trọ
- Tên phòng
- Tầng
- Tiền phòng
- Trạng thái

Buttons:

- Thêm mới
- Lưu thay đổi
- Sửa dòng chọn
- Ngưng sử dụng
- Làm mới

Table columns:

- Nhà / khu trọ
- Phòng
- Tầng
- Tiền phòng
- Người đại diện
- Trạng thái
- Ghi chú

Status display should be Vietnamese:

- Occupied → Đang cho thuê
- Vacant → Đang trống

Do not show English enum names in the UI.

---

## 5. Fee Type Screen Improvements

### Current Problem

The Fee Type screen shows default fee types, but it is unclear how to edit/update existing rows. Input boxes also do not have labels.

### Required Changes

Input fields must have labels:

- Tên loại phí
- Cách tính
- Đơn vị
- Đơn giá mặc định

Buttons:

- Thêm mới
- Sửa dòng chọn
- Lưu thay đổi
- Ngưng sử dụng
- Làm mới

Table columns:

- Tên phí
- Cách tính
- Đơn vị
- Đơn giá
- Loại hệ thống
- Trạng thái

Show system status as text:

- Mặc định
- Tùy chỉnh

Show active status as text:

- Đang dùng
- Ngưng dùng

Do not show editable checkboxes for `IsSystem` or `IsActive`.

### Required Behavior

- User selects an existing fee type.
- User clicks `Sửa dòng chọn`.
- Form fields are populated.
- User edits name, calculation type, unit, or default price.
- User clicks `Lưu thay đổi`.
- Updated data is saved and table refreshes.

### Validation

- Fee name is required.
- Default price cannot be negative.
- System fee types cannot be hard deleted.
- Changing default price must not affect existing invoice items.

### Vietnamese Fee Names

Default seeded fee names should be displayed in Vietnamese:

- Electricity → Điện
- Water → Nước
- Wifi → Wifi
- Parking → Giữ xe
- Garbage → Rác
- Other → Khác

Calculation type display should be Vietnamese:

- Fixed → Cố định
- Meter → Theo chỉ số
- PerPerson → Theo người
- PerUnit → Theo số lượng
- Manual → Nhập tay

---

## 6. Room Fee Configuration Screen Improvements

### Required Input Labels

Add labels for all fields:

- Nhà / khu trọ
- Phòng
- Loại phí
- Cách tính
- Đơn giá
- Số tiền cố định
- Số lượng
- Đang áp dụng
- Ghi chú

Buttons:

- Thêm mới
- Sửa dòng chọn
- Lưu thay đổi
- Ngưng áp dụng
- Làm mới

Table columns:

- Nhà / khu trọ
- Phòng
- Loại phí
- Cách tính
- Đơn giá
- Số tiền cố định
- Số lượng
- Trạng thái
- Ghi chú

Status display:

- Đang áp dụng
- Ngưng áp dụng

Do not show raw IDs.

---

## 7. General UI Display Rules

Apply these rules across all screens:

- Hide raw technical IDs from tables unless needed for debugging.
- Do not show English enum values in the UI.
- Do not show editable boolean checkboxes in tables unless there is a clear save flow.
- Use Vietnamese display text for statuses and calculation types.
- Add labels to every input field.
- Use VND formatting with thousand separators.
- Keep forms simple and readable.
- Avoid unexplained blank input boxes.
- Avoid technical column names like `IsActive`, `IsSystem`, `RoomId`, `FeeTypeId`.

---

## 8. Save/Edit Interaction Pattern

Use the same interaction pattern for all CRUD screens:

1. User selects a row in the table.
2. User clicks `Sửa dòng chọn`.
3. App fills the form with selected row data.
4. User edits form fields.
5. User clicks `Lưu thay đổi`.
6. App validates data.
7. App saves changes.
8. App refreshes table.
9. App shows status message.

For creating new data:

1. User fills empty form.
2. User clicks `Thêm mới`.
3. App validates data.
4. App creates new record.
5. App clears form.
6. App refreshes table.

---

## 9. Build Verification

After implementing these changes:

```powershell
dotnet build RentalManager.sln
Build must succeed with 0 errors.

Then run:

dotnet run

Manually verify:

Dashboard defaults to current month.
Dashboard can filter by 3 months, 6 months, and year.
All input fields have labels.
Property edit flow is clear.
Room status shows only Vietnamese rented/empty options.
Fee type edit/update flow works.
Room fee config edit/update flow works.