# Next Tasks - Rental Manager App

## Current Status

The app can run successfully. The first WPF + SQLite foundation is working.

Now improve the app for real manual testing and landlord usability.

## 1. Add Vietnamese UI

Convert visible UI labels to Vietnamese.

Examples:

- Rental Manager → Quản lý nhà trọ
- Billing month → Tháng tính tiền
- Refresh → Làm mới
- Dashboard → Tổng quan
- Properties → Nhà / Khu trọ
- Rooms → Phòng
- Tenants → Người thuê
- Fee Settings → Loại phí
- Room Fees → Phí theo phòng
- Meter Readings → Chỉ số điện nước
- Invoices → Hóa đơn
- Payments → Thanh toán
- Settings → Cài đặt

Dashboard labels:

- Expected → Dự kiến thu
- Collected → Đã thu
- Unpaid → Chưa thu
- Occupied → Phòng đang thuê
- Vacant → Phòng trống
- Missing readings → Thiếu chỉ số
- Unpaid invoices → Hóa đơn chưa trả

Entity/field labels should also be Vietnamese where visible in the UI.
Keep code/class/entity names in English.

## 2. Add Demo Data Seeding for Testing

Add a button in Settings:

- "Tạo dữ liệu mẫu"

When clicked, seed realistic demo data into SQLite for manual testing.

Demo data should include:

### Properties

- Nhà A - 123 Lê Lợi
- Nhà B - 45 Nguyễn Trãi

### Rooms

Nhà A:

- Phòng 101 - BaseRent 3,000,000 - Occupied
- Phòng 102 - BaseRent 3,200,000 - Occupied
- Phòng 103 - BaseRent 2,800,000 - Vacant

Nhà B:

- Phòng 201 - BaseRent 3,500,000 - Occupied
- Phòng 202 - BaseRent 3,000,000 - Maintenance

### Tenants

- Nguyễn Văn An - 0901000001
- Trần Thị Bình - 0901000002
- Lê Hoàng Cường - 0901000003
- Phạm Minh Duy - 0901000004

### Room Tenant Assignments

- Phòng 101: Nguyễn Văn An as representative, Trần Thị Bình as normal tenant
- Phòng 102: Lê Hoàng Cường as representative
- Phòng 201: Phạm Minh Duy as representative

### Fee Configs

For occupied rooms, create fee configs:

- Electricity: Meter, 3,500 VND/kWh
- Water: PerPerson, 100,000 VND/person
- Wifi: Fixed, 100,000 VND/month
- Parking: PerUnit, 150,000 VND/unit

Room-specific parking quantity:

- Phòng 101: 2
- Phòng 102: 1
- Phòng 201: 1

### Meter Readings

For billing month `2026-04`:

- Phòng 101 electricity: previous 100, current 160
- Phòng 102 electricity: previous 80, current 120
- Phòng 201 electricity: previous 200, current 260

Expected electricity amount:

- Phòng 101: 60 * 3500 = 210,000
- Phòng 102: 40 * 3500 = 140,000
- Phòng 201: 60 * 3500 = 210,000

## 3. Add Clear CRUD Actions

Current tables are too raw. Add clear action buttons for each main tab.

For each tab, add proper buttons:

- Thêm
- Sửa
- Lưu
- Xóa / Ngưng sử dụng
- Làm mới

Do not rely only on direct DataGrid editing unless changes are clearly saved.

## 4. Fee Settings Improvements

The Fee Settings tab must support:

- Add new fee type
- Edit selected fee type
- Save changes
- Deactivate selected fee type
- Prevent hard delete of system fee types
- Show validation message if name is empty
- Show validation message if price is negative

System fee types should not be deleted, but they can be edited carefully if needed.

Important: changing FeeType default price should not update old InvoiceItems.

## 5. Room Fee Config Improvements

The Room Fees tab must allow the landlord to configure fees per room.

Required fields:

- Room
- Fee Type
- Calculation Type
- Unit Price
- Fixed Amount
- Quantity
- Enabled
- Note

Add filters:

- Property
- Room
- Fee Type
- Enabled only

Add buttons:

- Add fee config
- Edit selected config
- Save config
- Disable config

## 6. Manual Testing Flow

After demo data is seeded, the app should allow this test flow:

1. Open Dashboard.
2. See demo rooms and summary data.
3. Open Fee Settings and confirm default fee types exist.
4. Open Rooms and see demo rooms.
5. Open Tenants and see demo tenants.
6. Open Room Fees and see fee configs per room.
7. Open Meter Readings and see April 2026 readings.
8. Generate invoices for April 2026.
9. Open Invoices and confirm invoice totals.
10. Record partial payment.
11. Confirm invoice status becomes Partial.
12. Record full remaining payment.
13. Confirm invoice status becomes Paid.
14. Close and reopen app.
15. Confirm data persists.

## 7. Improve Visual Usability

Keep the UI simple, but make it easier to read:

- Use Vietnamese labels.
- Format VND amounts with thousand separators.
- Make tables fill available space.
- Hide technical columns like raw Id unless needed.
- Show room name and property name instead of only RoomId.
- Show representative tenant name in invoice list.
- Add basic spacing and alignment.

Do not focus on advanced styling yet. Prioritize usability and correct workflow.

## 8. Build Verification

After changes, run:

```powershell
dotnet build RentalManager.sln