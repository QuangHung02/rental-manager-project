# UX Improvement Plan - Rental Manager

## Audit Summary

The app has the correct business foundation for an Excel replacement, but the current WPF UI still feels like a developer-facing data editor. The main usability issues are dense tabs, crowded forms, unclear primary actions, limited search, and invoice/payment workflows that are not yet guided enough for a landlord doing monthly billing.

The next improvements should keep the app simple and table-heavy, but make each screen answer: "What do I need to do next this month?"

## Improvement Backlog

| Priority | Screen | Current UI Problem | Proposed Improvement | Risk |
|---|---|---|---|---|
| High | Main layout | All workflows are flat tabs with equal visual weight, so monthly billing tasks are mixed with setup screens. | Keep tabs for now, but reorder them by landlord workflow: Tổng quan, Phòng, Chỉ số điện nước, Hóa đơn, Thanh toán, Người thuê, Phí theo phòng, Loại phí, Nhà / Khu trọ, Cài đặt. Add a short page title and primary action area per tab. | Safe |
| High | Main layout | Forms and tables compete for space; on smaller windows, forms wrap into long rows and reduce table visibility. | Use a two-zone pattern: compact filter/action bar at top, table fills center, edit form in a clearly labeled collapsible/side panel or fixed-height section. | Medium |
| High | Dashboard | Dashboard cards show totals, but do not clearly separate "this month needs attention" from historical invoice data. | Add three focused tables: Hóa đơn chưa thu, Phòng thiếu chỉ số, Thanh toán gần đây. Keep invoice history secondary. | Safe |
| High | Dashboard | Property filter has no obvious "all properties" option and range behavior may be unclear. | Add explicit "Tất cả nhà / khu trọ" option and show a small label for the active period, e.g. "Đang xem: 2026-04". | Safe |
| High | Dashboard | Missing meter readings is only a number, not actionable. | Make missing readings table show property, room, fee type, previous reading if available, and a button/path to enter readings. | Medium |
| High | Tables | Numeric columns are readable but not consistently aligned as money/quantity fields. | Right-align VND and quantity columns; keep text columns left aligned; use consistent widths for month/status/money. | Safe |
| High | Tables | Long tables lack search boxes, so the user must visually scan like a raw spreadsheet. | Add search fields to Rooms, Tenants, Invoices, Payments, and Room Fees. Search by room, tenant, phone, property, invoice month. | Medium |
| High | Forms | Create and edit share the same input area, but the current mode is not obvious. | Add form mode text: "Đang thêm mới" or "Đang sửa: Phòng 101". Clear the mode after save/refresh. | Safe |
| High | CRUD workflow | Buttons are present, but there are too many peer actions with no primary/secondary grouping. | Group buttons into primary action first: "Thêm mới" or "Lưu thay đổi"; then secondary actions "Sửa dòng chọn", "Ngừng sử dụng", "Làm mới". Disable save until editing an existing row. | Medium |
| High | Invoice workflow | Invoice generation is a single row action, but monthly workflow usually needs "generate for all rooms that are ready". | Add invoice readiness view: rooms with readings complete, rooms missing readings, invoices already generated. Then expose "Tạo hóa đơn cho các phòng đủ dữ liệu". | Medium |
| High | Invoice workflow | Invoice detail is not visible, so the landlord cannot easily inspect line items before issuing/copying. | Add invoice detail panel below or beside invoice table: room/property/tenant, invoice items, payments, totals. | Medium |
| High | Payment workflow | Payment entry is attached to the invoice list, but selected invoice context can be missed. | Show selected invoice summary above payment form: room, tenant, total, paid, remaining. Disable payment button until an invoice is selected. | Safe |
| High | Payment workflow | Overpayment validation exists in service logic, but the UI does not help the user enter exact remaining payment. | Add "Điền số còn lại" button and show remaining amount prominently. | Safe |
| Medium | Properties | Properties are setup data and do not need daily prominence. | Move after operational tabs; keep simple table plus form. | Safe |
| Medium | Rooms | Room screen lacks filters despite spec requiring property/status/search filters. | Add property filter, status filter, and search by room or representative tenant. | Medium |
| Medium | Tenants | Tenant assignment table shares the screen with tenant list, but there is no clear current room context. | Add assignment section title and filters by property/room/status. Consider moving assignment management into Room Detail later. | Medium |
| Medium | Room Fees | Fee config requires domain understanding, but all fields are shown at once. | Dynamically emphasize fields by calculation type: Fixed uses fixed amount; Meter uses unit price; PerPerson uses unit price; PerUnit uses quantity and unit price; Manual uses fixed amount. | Medium |
| Medium | Meter Readings | Meter readings screen is close to Excel, but it lacks missing-only and property/fee filters. | Add filters: month, property, fee type, missing only. Add autofill previous readings action. | Medium |
| Medium | Payments | Payment history has no filters. | Add billing month, property, method, and search filters. | Medium |
| Medium | Visual hierarchy | Header and group boxes are functional but not enough to distinguish setup vs monthly work. | Use restrained section headings, consistent top action bars, and smaller secondary buttons. No advanced styling yet. | Safe |
| Medium | Status display | Some boolean values can still appear as raw true/false, especially representative assignment. | Replace boolean table text with "Có/Không" or "Đại diện/Người ở cùng". | Safe |
| Low | Settings | Backup/restore/demo data are mixed without warnings or grouping. | Split into "Dữ liệu", "Sao lưu", and "Dữ liệu mẫu"; keep restore warning. | Safe |
| Low | Tables | Tables do not persist sort/filter choices. | Later: remember last selected filters and column widths. | Risky |

## What To Hide From Normal Users

- Raw technical IDs: `Id`, `RoomId`, `TenantId`, `FeeTypeId`, `InvoiceId`.
- EF navigation objects and collection counts.
- English enum names: `Occupied`, `Vacant`, `Draft`, `Issued`, `Partial`, `Paid`, `Fixed`, `Meter`.
- Editable boolean table checkboxes for system/internal states such as `IsSystem`, `IsActive`, `Enabled`, unless the screen has an explicit save flow.
- `CreatedAt` and `UpdatedAt` on normal screens.
- System fee internals unless the user is in Fee Type setup.
- Inactive/deactivated records by default; show them only with an "Hiện mục ngừng sử dụng" filter.

## Suggested Implementation Order

1. **Table Readability Pass**
   - Right-align money/quantity columns.
   - Replace remaining true/false displays with Vietnamese text.
   - Hide inactive records by default where appropriate.
   - Safe and immediately improves confidence.

2. **Search And Filters Pass**
   - Add filters to Rooms, Meter Readings, Invoices, Payments, and Room Fees.
   - Add explicit "Tất cả" filter options.
   - Medium risk because it touches view-model filtering logic.

3. **Dashboard Actionability Pass**
   - Add unpaid invoices, missing readings, and recent payments tables.
   - Keep monthly summary as the main dashboard default.
   - Medium risk but high workflow value.

4. **CRUD Mode Clarity Pass**
   - Add form mode labels.
   - Disable invalid actions where possible.
   - Make "Thêm mới" and "Lưu thay đổi" visually distinct.
   - Medium risk because command state must stay accurate.

5. **Invoice Detail And Payment Context Pass**
   - Add selected invoice detail panel.
   - Show invoice items and payment history.
   - Add "Điền số còn lại" for payments.
   - Medium risk because it needs richer invoice loading.

6. **Meter Reading Workflow Pass**
   - Add missing-only filter.
   - Add previous reading autofill action.
   - Add "generate invoices for ready rooms" path.
   - Medium risk because it touches billing readiness behavior.

7. **Main Layout Reorder Pass**
   - Reorder tabs by landlord workflow.
   - Move setup screens later.
   - Keep the layout simple; do not introduce a custom navigation framework yet.
   - Safe if only XAML order changes.

## Notes For Future Design

Do not start with colors, icons, or advanced styling. The app should first become a dependable operational workbook: clear filters, readable tables, predictable create/edit/save flows, and guided monthly billing. Once the workflow feels solid, visual polish can be added conservatively.
