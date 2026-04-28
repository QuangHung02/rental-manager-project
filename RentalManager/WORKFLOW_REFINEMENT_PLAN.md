# Workflow Refinement Plan

## Scope

This plan keeps the current database model unchanged. The existing `RoomTenant` table already supports rental history with `StartDate`, `EndDate`, `Status`, and `IsRepresentative`, so the next workflow improvement should be service/UI behavior rather than schema work.

Do not change billing logic in this iteration. The goal is to make the app match how a landlord actually works: create rooms, configure fees, keep tenant profiles, assign people to rooms, end assignments when they leave, and preserve history.

## Correct Tab Order

Recommended tab order for normal use:

1. `Tổng quan`
2. `Nhà / Khu trọ`
3. `Phòng`
4. `Phí theo phòng`
5. `Người thuê`
6. `Thuê phòng / Phân phòng`
7. `Chỉ số điện nước`
8. `Hóa đơn`
9. `Thanh toán`
10. `Loại phí`
11. `Cài đặt`

Reasoning:

- `Tổng quan` stays first because returning landlords usually want the monthly status first.
- Setup screens should follow the real data dependency: property, room, fee config, tenant profile, room assignment.
- Monthly operation screens should come after the setup flow: meter readings, invoices, payments.
- `Loại phí` is setup/admin data and should not be in the main monthly path once default fee types exist.
- `Cài đặt` remains last.

## Correct Landlord Workflow

The app should guide the landlord through this sequence:

1. Create a property in `Nhà / Khu trọ`.
2. Create rooms under that property in `Phòng`.
3. Configure rent and room-level fees in `Phí theo phòng`.
4. Create tenant profiles in `Người thuê`.
5. Assign tenants to rooms in `Thuê phòng / Phân phòng`.
6. Mark exactly one active tenant as the representative for each occupied room.
7. Enter monthly meter readings.
8. Generate invoices.
9. Record payments.
10. Review results on the dashboard.

Tenant profile creation and tenant-room assignment should be separate mental steps. A tenant profile is a person record. A room assignment is a rental period.

## Room Status Logic

Only two room statuses should be visible in normal UI:

- `Đang trống` for `Vacant`
- `Đang cho thuê` for `Occupied`

`Maintenance` and `Inactive` can remain in the enum but should be hidden from normal room status selectors. They should only appear through explicit advanced/deactivation actions if needed.

Room status rules:

- When a new active room assignment is created, set the room to `Occupied`.
- When an active assignment is ended, recalculate the room status.
- If the room has at least one active tenant after the assignment ends, keep the room `Occupied`.
- If the room has no active tenants after the assignment ends, set the room to `Vacant`.
- Room list filters should default to showing `Vacant` and `Occupied`, not inactive/internal statuses.

Representative rules:

- A room should have only one active representative tenant.
- If a new active representative is chosen, clear representative status from other active tenants in the same room.
- If the representative moves out while other active tenants remain, the UI should require selecting a new representative or provide a clear `Đặt làm đại diện` row action.

## Tenant Move-Out Logic

Moving out must end the assignment, not delete the tenant.

Required behavior:

- Add a row-level action on active assignments: `Kết thúc thuê`.
- When clicked, ask for or default the `Ngày kết thúc`.
- Set `RoomTenant.Status = Ended`.
- Set `RoomTenant.EndDate`.
- Set `RoomTenant.IsRepresentative = false`.
- Recalculate the room status from remaining active assignments.
- Keep the tenant profile unchanged.
- Keep the ended assignment visible in history filters.

The tenant should remain searchable because the landlord may need phone number, old invoice context, or rental history later.

## Tenant Room-Change Logic

Changing rooms should be modeled as two assignment events:

1. End the old active room assignment.
2. Create a new active room assignment for the new room.

Required behavior:

- Add a row-level action on active assignments: `Chuyển phòng`.
- The action opens a focused form with:
  - current tenant
  - current room
  - target room
  - move date
  - representative option for the new room
- End the old assignment with `EndDate`.
- Create a new `RoomTenant` record with `StartDate` and `Status = Active`.
- Recalculate status for the old room.
- Set the new room to `Occupied`.
- Preserve the old assignment in history.

The UI should not edit the old assignment into a new room, because that would erase room history.

## Tenant Records: Hidden, Archived, Or Deleted

Tenant records should not be hard deleted in normal use.

Recommended behavior without schema changes:

- Keep all tenant profiles.
- Default tenant list filter should show `Đang thuê` and `Chưa phân phòng`.
- Provide filters:
  - `Tất cả`
  - `Đang thuê`
  - `Chưa phân phòng`
  - `Đã từng thuê`
- `Đã từng thuê` means the tenant has ended assignments and no active assignment.
- Avoid using the word archive unless a future schema field is added. For now, this is filter behavior, not stored archive state.

## Automatic Filters

Filters should help the landlord stay in the current workflow without constant manual cleanup.

Recommended defaults:

- `Phòng`: show active rooms with visible statuses `Đang trống` and `Đang cho thuê`.
- `Phí theo phòng`: default to `Đang áp dụng`.
- `Người thuê`: default to `Đang thuê` plus `Chưa phân phòng`.
- `Thuê phòng / Phân phòng`: default to active assignments only.
- History views: show ended assignments only when the user selects `Lịch sử` or `Đã kết thúc`.
- `Chỉ số điện nước`, `Hóa đơn`, and `Thanh toán`: default to the selected billing month/year.

Filter behavior:

- Search text can update automatically while typing.
- Dropdown filters can update automatically after selection.
- Keep `Lọc` and `Xóa lọc` buttons where the screen already uses them, but the table should not require a refresh button for obvious filter changes.
- When the landlord selects a property, room dropdowns should narrow to rooms in that property where practical.

## Screen Layout Pattern

Each workflow screen should separate three areas clearly:

1. Filter area
   - At the top.
   - Contains search, property, room, status, month/year filters.
   - Does not contain create/edit fields.

2. Add-new or edit area
   - Below filters or in a right-side panel.
   - Has a clear mode label: `Đang thêm mới` or `Đang sửa`.
   - Used only for creating/editing records.

3. Table/list area
   - Main screen space.
   - Shows readable business columns.
   - Includes row-level action buttons.

This separation is especially important for `Người thuê`, because tenant profile fields and room assignment fields are currently too close together.

## Row-Level Edit Buttons

Tables should not be always editable.

Recommended row actions:

- `Sửa`: loads the row into the edit area.
- `Lưu`: only appears in inline edit mode if inline editing is used.
- `Hủy`: exits edit mode without saving.
- `Ngừng dùng`: for property, room, or fee type deactivation.
- `Ngừng áp dụng`: for room fee config.
- `Kết thúc thuê`: for active room assignments.
- `Chuyển phòng`: for active tenant assignments.
- `Đặt đại diện`: for active non-representative tenants in occupied rooms.
- `Chi tiết`, `Thanh toán`, `Sao chép`, `Hủy`: for invoices.

Preferred near-term pattern:

- Keep the existing edit panel pattern.
- Row `Sửa` selects the row and fills the edit panel.
- Save is done through the panel button `Lưu thay đổi`.
- Assignment-specific actions should be row-level because they represent workflow events, not normal field editing.

## Screens That Need Changes

### Main Layout

- Reorder tabs to match the landlord workflow.
- Add a separate `Thuê phòng / Phân phòng` tab or clearly separate this section from tenant profile management.

### Nhà / Khu Trọ

- Mostly keep current CRUD behavior.
- Ensure properties are created before rooms in navigation.

### Phòng

- Show only `Đang trống` and `Đang cho thuê` in normal status UI.
- Add visible columns for representative tenant and active tenant count.
- Add actions: `Cấu hình phí`, `Gán người thuê`, and later `Xem lịch sử`.

### Phí Theo Phòng

- Position before tenant assignment in the setup workflow.
- Keep fee config scoped by property and room.
- Default filters should show enabled configs.

### Người Thuê

- Treat this as tenant profile management only.
- Add filters for `Đang thuê`, `Chưa phân phòng`, `Đã từng thuê`, and `Tất cả`.
- Do not delete tenant profiles in normal UI.

### Thuê Phòng / Phân Phòng

- New or separated workflow screen.
- Show active assignments by default.
- Support assigning tenants to rooms.
- Support setting representative tenant.
- Support move-out.
- Support room change.
- Support assignment history.

### Chỉ Số Điện Nước

- Default to selected month/year and active occupied rooms with meter fees.
- Keep monthly billing flow separate from tenant assignment flow.

### Hóa Đơn

- Continue to use active representative tenant for invoice display.
- Invoice generation should rely on current active assignments for tenant count and representative.
- Historical invoices should keep their snapshot data.

### Thanh Toán

- No data model changes needed.
- Keep payment workflow tied to invoices, not directly to tenant records.

### Loại Phí

- Keep as setup/admin data.
- Place later in navigation because landlords should not edit fee types during normal monthly work.

## Implementation Order

1. Reorder tabs and rename/separate the assignment workflow.
2. Add assignment-focused view state and filters: active assignments, ended assignments, all assignments.
3. Add service methods for ending an assignment and recalculating room status.
4. Add `Kết thúc thuê` row action for active assignments.
5. Add representative row action and enforce one active representative per room.
6. Add `Chuyển phòng` flow that ends the old assignment and creates a new one.
7. Update tenant filters so tenant profiles are hidden by workflow state instead of deleted.
8. Update room filters and status display so only `Vacant` and `Occupied` are normal UI statuses.
9. Split each affected screen into filter area, edit/add area, and table area.
10. Verify workflow scenarios manually:
    - add property, room, fee config, tenant, assignment
    - set representative
    - move one tenant out while another stays
    - move representative out and choose a new representative
    - move tenant to another room
    - confirm old room becomes vacant when no active tenants remain
    - confirm tenant history remains visible

## Risk Notes

- Safe: tab ordering, labels, filters, visible status options, hiding delete actions.
- Medium: move-out and room-change actions because they change assignment state and room status together.
- Medium: representative handling because the UI must avoid leaving an occupied room without a representative.
- Risky if rushed: changing invoice generation behavior. Avoid this unless a workflow bug is discovered, because invoice snapshots and billing logic should remain stable.
