# Fee / Room Fee / Meter Reading Logic Review

Baseline: this is a review only. No database schema, invoice calculation, or payment logic changes are included here.

## 1. Loại phí

### Current calculation types

- `Fixed`: invoice amount comes from `RoomFeeConfig.FixedAmount`; if it is empty, the current calculation falls back to the unit price.
- `Meter`: invoice amount comes from the matching monthly `MeterReading`; usage is `CurrentReading - PreviousReading`, amount is `UsageAmount * UnitPriceSnapshot`.
- `PerPerson`: invoice amount is active tenant count multiplied by `RoomFeeConfig.UnitPrice` or `FeeType.DefaultUnitPrice`.
- `PerUnit`: invoice amount is `RoomFeeConfig.Quantity` multiplied by `RoomFeeConfig.UnitPrice` or `FeeType.DefaultUnitPrice`.
- `Manual`: invoice amount uses `RoomFeeConfig.FixedAmount` and sets quantity to 1.

### Default fee types

Current seeded defaults are:

- Electricity: `Meter`, unit `kWh`, default unit price `3,500`.
- Water: `PerPerson`, unit `person`, default unit price `100,000`.
- Wifi: `Fixed`, unit `month`, default unit price `0`.
- Parking: `PerUnit`, unit `unit`, default unit price `150,000`.
- Garbage: `Fixed`, unit `month`, default unit price `0`.
- Other: `Manual`, no unit, default unit price `0`.

These defaults match the project spec and common landlord usage. The only usability issue is that English internal names are seeded and then translated for display by `DisplayText.FeeName`.

### Editing effects on old invoices

Old invoice items store snapshots of item name, calculation type, quantity, unit, unit price, and amount. Editing a fee type or a room fee config should not automatically rewrite old invoices that already exist.

Risk: draft invoice recreation can rebuild values from current fee/config data. This is expected, but users should understand that recreating a draft invoice uses the latest settings.

### System fee type edit/deactivate behavior

Current code allows system fee types to be edited and deactivated. The spec only says system fee types should not be hard deleted. This is technically allowed, but risky for normal users because disabling Electricity or changing its calculation type can break expected workflows.

Recommendation: keep system fee types visible but restrict risky edits later. Allow editing display-friendly values such as default unit price, but consider blocking name/calculation-type changes and show a Vietnamese message if the user tries to deactivate a system fee.

## 2. Phí theo phòng

### Current assignment model

Each room can have multiple `RoomFeeConfig` records. A config points to one room and one fee type, has its own calculation type, optional unit price, optional fixed amount, optional quantity, and enabled flag.

### Duplicate config risk

Current validation checks only:

- Room is selected.
- Fee type is selected.
- Unit price, fixed amount, and quantity are not negative.

It does not prevent duplicate enabled configs for the same room and fee type. This can charge the same fee twice on a generated invoice if two enabled configs exist.

Recommended future fix: prevent more than one enabled config per room and fee type. If historical disabled configs should remain, allow duplicates only when old ones are disabled.

### Enabled / disabled behavior

Invoice generation uses only enabled configs. Disabled configs remain visible in the app when filters are changed, which is good for history and auditability.

### Field usage by calculation type

- `Fixed`: uses `FixedAmount`; if empty, falls back to unit price.
- `Meter`: uses the monthly meter reading. `UnitPrice` is used when saving meter reading as the snapshot price; otherwise default unit price is used.
- `PerPerson`: uses active tenant count and unit price.
- `PerUnit`: uses quantity and unit price.
- `Manual`: uses fixed amount.

### UI clarity risk

The UI currently shows `Đơn giá`, `Cố định`, and `Số lượng` for every calculation type. This can confuse users because some fields are irrelevant depending on the type.

Recommended future UI behavior:

- `Fixed`: show `Cố định`, hide or de-emphasize `Đơn giá` and `Số lượng`.
- `Meter`: show `Đơn giá`, hide `Cố định` and `Số lượng`.
- `PerPerson`: show `Đơn giá`, hide `Cố định` and `Số lượng`.
- `PerUnit`: show `Đơn giá` and `Số lượng`, hide `Cố định`.
- `Manual`: show `Cố định`, hide `Đơn giá` and `Số lượng`.

## 3. Chỉ số điện nước

### Required only for meter-based fees

Readiness checks look at enabled room fee configs where `CalculationType == Meter`. This correctly limits missing-reading detection to meter-based fees.

Risk: the meter reading input screen currently lets the user choose any fee type, including non-meter fees. Saving a reading for Water or Wifi is possible if selected manually. That data may not be used by invoices, but it can confuse users.

Recommended future fix: filter the meter reading fee dropdown to meter-based fee types/configs for the selected room.

### Previous reading auto-fill

When saving a reading, `MainViewModel.AddMeterReading` fills `PreviousReading` from the latest earlier reading only if `PreviousReading == 0`.

This works for common cases, but has one edge case: if a valid previous reading is actually 0, the app cannot distinguish between "user typed 0" and "not filled yet." That is usually acceptable for early usage.

Recommended future UX improvement: auto-fill previous reading when room, fee type, or billing month changes, instead of waiting until save.

### Current reading validation

`MeterReadingService.Save` blocks `CurrentReading < PreviousReading` with Vietnamese validation:

`Chỉ số mới phải lớn hơn hoặc bằng chỉ số cũ.`

This is correct.

### Amount calculation

The service calculates:

- `UsageAmount = CurrentReading - PreviousReading`
- `UnitPriceSnapshot = RoomFeeConfig.UnitPrice ?? FeeType.DefaultUnitPrice`
- `Amount = UsageAmount * UnitPriceSnapshot`

This matches the spec.

Risk: the service selects the first room fee config for room and fee type, without requiring it to be enabled or meter-based. If duplicate configs exist, the wrong unit price could be used.

### Missing readings before invoice generation

`InvoiceService.GetReadiness` detects missing readings for enabled meter configs before generation and shows `Thiếu chỉ số`. `GenerateReady` only generates rooms marked `Đủ dữ liệu`.

Risk: direct single-room invoice generation still builds a draft invoice even if a meter reading is missing; the calculation item becomes amount 0 with English note `Missing meter reading`. This should be reviewed later, but not changed in this iteration because invoice calculation logic is out of scope.

## 4. Risks or Bugs to Test Manually

High priority:

- Create two enabled fee configs for the same room and same fee type, then generate a draft invoice. Check whether the fee is duplicated.
- Create a meter reading for a non-meter fee type and confirm whether it appears in screens or affects reports.
- Create duplicate room fee configs with different unit prices, then save a meter reading. Confirm which unit price becomes `UnitPriceSnapshot`.
- Generate a single-room invoice when a meter reading is missing. Confirm the invoice item amount and note.

Medium priority:

- Edit a fee type default unit price after an invoice exists. Confirm old invoice items keep their old values.
- Disable a room fee config, then generate the next invoice. Confirm the disabled config is excluded.
- Change the active tenant count for a room, then generate a per-person fee invoice.

Low priority:

- Edit a system fee type name/calculation type and confirm display translations still make sense.
- Save a reading where previous and current are equal. Confirm usage and amount are 0.

## 5. Recommended Improvements

High priority:

- Prevent duplicate enabled room fee configs for the same room and fee type.
- Filter meter reading fee choices to only meter-based enabled configs for the selected room.
- Use only enabled meter configs when calculating meter reading unit price.
- Replace the English invoice item note `Missing meter reading` with Vietnamese if this path remains allowed.

Medium priority:

- Hide irrelevant room fee fields based on calculation type.
- Auto-fill previous reading when the user changes room, fee type, or billing month.
- Add clearer Vietnamese validation for fee type name and default unit price.

Low priority:

- Add guardrails around editing or deactivating system fee types.
- Add explanatory labels for each calculation type.

## 6. Suggested Implementation Order

1. Add duplicate enabled room fee config validation.
2. Filter meter reading selection to room-specific meter configs.
3. Make meter reading unit-price lookup use enabled meter configs only.
4. Add dynamic field visibility for room fee config form.
5. Auto-fill previous reading before save.
6. Add system fee type guardrails.
7. Review direct single-room invoice behavior for missing meter readings in a later invoice-focused iteration.
