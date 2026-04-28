# Fee / Room Fee / Meter Reading Logic Review

Baseline: this review tracks fee and meter behavior. Invoice calculation and payment logic are intentionally unchanged in the current iteration.

## Status Update

Fixed in this iteration:

- Prevent duplicate enabled `RoomFeeConfig` records for the same room and fee type.
- Keep old disabled room fee configs visible for history/audit.
- Filter the meter reading fee selector to only enabled meter-based configs for the selected room.
- Save meter readings using only enabled meter-based room fee configs for unit price lookup.
- Block meter reading save when the selected room does not have an enabled meter-based config for that fee.
- Show room choices as `Nhà / khu trọ - Phòng` in the Room Fee Config screen.
- Hide irrelevant Room Fee Config amount fields based on calculation type.
- Auto-fill previous meter reading when room, meter fee, or billing month changes, while keeping it editable.
- Upsert meter readings by room, fee type, and billing month to avoid duplicates.
- Block invoice generation when an enabled meter-based fee is missing a reading for the billing period.
- Room Fee Config filter and form room dropdowns are now property-scoped.
- Meter Reading form now loads an existing same-period reading into the form for editing.
- Room Fee Config status filtering now uses only the visible status selector. `Tất cả` includes enabled and disabled records.
- Room Fee Config table was compacted to show room, fee type, calculation type, applied price, status, and actions.
- Room Fee Config pricing now supports `Dùng giá mặc định` without schema changes. Null `UnitPrice`/`FixedAmount` means follow `FeeType.DefaultUnitPrice`; non-null means custom room price.

Still pending:

- Add system fee type edit/deactivate guardrails.
- Add more descriptive inline help for calculation types if needed.

## 1. Loại phí

### Current calculation types

- `Fixed`: invoice amount comes from `RoomFeeConfig.FixedAmount`; if it is empty, the current calculation falls back to the unit price.
- `Meter`: invoice amount comes from the matching monthly `MeterReading`; usage is `CurrentReading - PreviousReading`, amount is `UsageAmount * UnitPriceSnapshot`.
- `PerPerson`: invoice amount is active tenant count multiplied by `RoomFeeConfig.UnitPrice` or `FeeType.DefaultUnitPrice`.
- `PerUnit`: invoice amount is `RoomFeeConfig.Quantity` multiplied by `RoomFeeConfig.UnitPrice` or `FeeType.DefaultUnitPrice`.
- `Manual`: invoice amount uses `RoomFeeConfig.FixedAmount` and sets quantity to 1.

### Default fee types

- Electricity: `Meter`, unit `kWh`, default unit price `3,500`.
- Water: `PerPerson`, unit `person`, default unit price `100,000`.
- Wifi: `Fixed`, unit `month`, default unit price `0`.
- Parking: `PerUnit`, unit `unit`, default unit price `150,000`.
- Garbage: `Fixed`, unit `month`, default unit price `0`.
- Other: `Manual`, no unit, default unit price `0`.

These defaults match the project spec and common landlord usage.

### Editing effects on old invoices

Old invoice items store snapshots of item name, calculation type, quantity, unit, unit price, and amount. Editing a fee type or a room fee config should not automatically rewrite old invoices that already exist.

Risk: draft invoice recreation can rebuild values from current fee/config data. This is expected, but users should understand that recreating a draft invoice uses the latest settings.

### System fee type edit/deactivate behavior

Current code allows system fee types to be edited and deactivated. The spec only says system fee types should not be hard deleted. This is technically allowed, but risky for normal users because disabling Electricity or changing its calculation type can break expected workflows.

Recommendation: keep system fee types visible but restrict risky edits later. Allow editing display-friendly values such as default unit price, but consider blocking name/calculation-type changes and show a Vietnamese message if the user tries to deactivate a system fee.

## 2. Phí theo phòng

### Current assignment model

Each room can have multiple `RoomFeeConfig` records. A config points to one room and one fee type, has its own calculation type, optional unit price, optional fixed amount, optional quantity, and enabled flag.

### Duplicate config handling

Fixed: the app now prevents more than one enabled config for the same room and fee type.

Allowed: old disabled configs remain visible and can coexist for audit/history.

Validation message:

`Phòng này đã có cấu hình phí đang áp dụng cho loại phí đã chọn.`

### Enabled / disabled behavior

Invoice generation uses only enabled configs. Disabled configs remain visible in the app when filters are changed, which is good for history and auditability.

### Field usage by calculation type

- `Fixed`: uses `FixedAmount`; if empty, falls back to unit price.
- `Meter`: uses the monthly meter reading. `UnitPrice` is used when saving meter reading as the snapshot price; otherwise default unit price is used.
- `PerPerson`: uses active tenant count and unit price.
- `PerUnit`: uses quantity and unit price.
- `Manual`: uses fixed amount.

### UI clarity

Fixed: the Room Fee Config form now shows only fields relevant to the selected calculation type.

Fixed: room selection is property-first. The filter room dropdown follows the selected property, and the add/edit form requires a property before selecting a room.

Fixed: `Tất cả` no longer applies a hidden enabled-only filter. Disabled configs are visible when the selected status allows them.

Fixed: Room Fee Config can now explicitly follow the FeeType default price again. Existing non-null prices remain custom prices for backward compatibility.

Current UI behavior:

- `Fixed`: show `Cố định`.
- `Meter`: show `Đơn giá`, hide `Cố định` and `Số lượng`.
- `PerPerson`: show `Đơn giá`, hide `Cố định` and `Số lượng`.
- `PerUnit`: show `Đơn giá` and `Số lượng`.
- `Manual`: show `Cố định`.

Validation now follows the selected calculation type, and hidden fields are normalized before saving so they do not affect later calculations.

## 3. Chỉ số điện nước

### Required only for meter-based fees

Fixed: the meter reading input fee selector now shows only active fee types that have an enabled meter-based room fee config for the selected room.

This prevents users from entering readings for Water, Wifi, or other non-meter fees unless those fees are explicitly configured as meter-based for that room.

### Previous reading auto-fill

Fixed: previous reading is now auto-filled when the user changes room, meter fee, or billing month/year. The value remains editable manually.

Fixed: if a reading already exists for the selected room, fee type, and billing month, the form loads that existing reading for editing instead of starting a new one. If no same-period reading exists, only `PreviousReading` is filled from the latest earlier reading and `CurrentReading` stays empty/default.

### Current reading validation

`MeterReadingService.Save` blocks `CurrentReading < PreviousReading` with Vietnamese validation:

`Chỉ số mới phải lớn hơn hoặc bằng chỉ số cũ.`

This is correct.

### Amount calculation

Fixed: the service now calculates meter readings using only enabled meter-based room fee configs.

The service calculates:

- `UsageAmount = CurrentReading - PreviousReading`
- `UnitPriceSnapshot = enabled meter RoomFeeConfig.UnitPrice ?? FeeType.DefaultUnitPrice`
- `Amount = UsageAmount * UnitPriceSnapshot`

If no enabled meter-based config exists, saving is blocked with:

`Vui lòng chọn loại phí theo chỉ số đang áp dụng cho phòng.`

### Missing readings before invoice generation

`InvoiceService.GetReadiness` detects missing readings for enabled meter configs before generation and shows `Thiếu chỉ số`. `GenerateReady` only generates rooms marked `Đủ dữ liệu`.

Fixed: direct invoice generation now blocks missing meter readings instead of creating a zero-amount invoice item.

Validation message:

`Phòng này còn thiếu chỉ số điện/nước cho kỳ hóa đơn đã chọn.`

## 4. Risks or Bugs to Test Manually

High priority now fixed, but should be verified:

- Create two enabled fee configs for the same room and same fee type. The second enabled config should be blocked.
- Disable the old config, then create a new enabled config for the same room and fee type. This should be allowed.
- Select a room without enabled meter configs in the meter reading screen. The fee dropdown should be empty.
- Select a room with an enabled meter config. Only that meter fee should appear.
- Save a meter reading and confirm `UnitPriceSnapshot` comes from the enabled meter config.
- Save a reading twice for the same room, fee type, and month. Confirm the existing reading is updated instead of duplicated.
- Generate an invoice for a room that is missing a required meter reading. Confirm invoice generation is blocked with Vietnamese validation.
- In Room Fee Config, choose a property and confirm the room dropdown only shows rooms from that property.
- In Room Fee Config, set status to `Tất cả` and confirm disabled configs are visible.
- Try adding a fee that already exists as disabled and confirm the app tells the user to switch filters and edit/reactivate the existing row.
- In Room Fee Config, check `Dùng giá mặc định` and confirm the applied price column shows `(mặc định)`.
- Uncheck `Dùng giá mặc định`, enter a custom price, and confirm the applied price column shows `(riêng)`.
- In Meter Reading, select a room + fee + month with an existing reading and confirm previous/current/note load into the form.

Still pending:

- Edit a fee type default unit price after an invoice exists. Confirm old invoice items keep their old values.
- Disable a room fee config, then generate the next invoice. Confirm the disabled config is excluded.
- Change the active tenant count for a room, then generate a per-person fee invoice.
- Edit a system fee type name/calculation type and confirm display translations still make sense.
- Save a reading where previous and current are equal. Confirm usage and amount are 0.

## 5. Recommended Improvements

Completed:

- Prevent duplicate enabled room fee configs for the same room and fee type.
- Filter meter reading fee choices to only meter-based enabled configs for the selected room.
- Use only enabled meter configs when calculating meter reading unit price.
- Show contextual room names in Room Fee Config dropdowns/tables.
- Hide irrelevant Room Fee Config fields by calculation type.
- Auto-fill previous meter reading before save.
- Upsert meter readings by room, fee type, and billing month.
- Block invoice generation when a required meter reading is missing.

Pending medium priority:

- Add clearer Vietnamese validation for fee type name and default unit price.

Pending low priority:

- Add guardrails around editing or deactivating system fee types.
- Add explanatory labels for each calculation type.

## 6. Suggested Implementation Order

Completed:

1. Add duplicate enabled room fee config validation.
2. Filter meter reading selection to room-specific meter configs.
3. Make meter reading unit-price lookup use enabled meter configs only.
4. Review direct single-room invoice behavior for missing meter readings.
5. Add dynamic field visibility for room fee config form.
6. Auto-fill previous reading before save.
7. Upsert duplicate meter readings by room, fee type, and month.

Next:

8. Add system fee type guardrails.
9. Add clearer fee type validation/help text.
