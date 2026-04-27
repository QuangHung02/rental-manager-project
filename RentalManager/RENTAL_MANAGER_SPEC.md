# Rental Manager Desktop App - Development Specification

## 1. Project Identity

**Project name:** Rental Manager Desktop App  
**App type:** Windows desktop application  
**Primary user:** Landlord only  
**Purpose:** Replace Excel for managing rental rooms, monthly costs, utility readings, invoices, payments, and dashboard summaries.

This is not a web app. This is not a multi-user system. The app should run locally on Windows by opening an `.exe` file.

---

## 2. Product Goal

Build a local Windows desktop app that helps a landlord manage:

- Multiple rental properties
- Multiple rooms inside each property
- Tenants assigned to each room
- One representative tenant per room
- Room-specific rent and utility fee configuration
- Monthly electricity/water readings
- Monthly invoice generation
- Payment tracking
- Dashboard summaries
- Backup and restore of local data

The app should behave like an upgraded Excel file: table-heavy, filterable, searchable, but with structured data and automatic calculation.

---

## 3. Technical Direction

Use:

- .NET WPF
- C#
- SQLite
- Entity Framework Core
- MVVM pattern

The app should store all data in a local SQLite database file.

No SQL Server.  
No backend server.  
No cloud dependency.  
No tenant login.  
No internet requirement.

---

## 4. User and Actor Model

### Landlord

The landlord is the only system user.

The landlord can:

- Create properties
- Create rooms
- Add tenants
- Assign tenants to rooms
- Mark one tenant as room representative
- Configure rent and fees
- Enter meter readings
- Generate invoices
- Record payments
- View dashboard
- Export/copy invoice data
- Backup/restore database

### Tenant

Tenant is not a system actor.

Tenant does not:

- Log in
- Use the app
- Submit data
- View invoices inside the app

Tenant is only stored as data so the landlord can track responsibility and invoice recipients.

---

## 5. Core Business Structure

The main business structure is:

```text
Property
└── Room
    ├── Tenants
    ├── Room fee configuration
    ├── Monthly meter readings
    ├── Monthly invoices
    └── Payments
```

A property has many rooms.  
A room belongs to one property.  
A room can have many tenants.  
A room should have one active representative tenant.  
A room has its own base rent.  
A room can have its own fee configuration.

---

## 6. Key Design Rule: Flexible Fees Without Dynamic Columns

Do not design the database like this:

```text
water_fee
wifi_fee
parking_fee
garbage_fee
extra_fee_1
extra_fee_2
```

This makes the system rigid and hard to extend.

Instead, use this model:

```text
FeeType
RoomFeeConfig
InvoiceItem
```

Meaning:

- `FeeType` defines what kind of fee exists.
- `RoomFeeConfig` defines how a fee applies to a specific room.
- `InvoiceItem` stores the final calculated fee line inside an invoice.

This gives Excel-like flexibility while keeping the database structured.

---

## 7. Fee Calculation Types

The app must support these fee calculation types.

### Fixed

Used for fixed monthly fees.

Examples:

- Wifi: 100,000/month
- Garbage: 30,000/month

Formula:

```text
Amount = FixedAmount
```

### Meter

Used for fees based on meter readings.

Examples:

- Electricity
- Water, if calculated by meter

Formula:

```text
Usage = CurrentReading - PreviousReading
Amount = Usage * UnitPrice
```

### PerPerson

Used for fees based on active tenant count.

Example:

- Water: 100,000/person
- Room has 2 active tenants
- Amount = 2 * 100,000

Formula:

```text
Amount = ActiveTenantCount * UnitPrice
```

### PerUnit

Used for fees based on quantity.

Examples:

- Parking: 150,000/motorbike
- Pet fee: 50,000/pet

Formula:

```text
Amount = Quantity * UnitPrice
```

### Manual

Used for one-time or irregular fees.

Examples:

- Repair fee
- Penalty
- Extra charge
- Special discount adjustment

The landlord manually enters the amount.

---

## 8. Main Data Model

### Property

Represents a rental building or house.

Fields:

- Id
- Name
- Address
- Note
- IsActive
- CreatedAt
- UpdatedAt

Rules:

- Property name is required.
- A property can have many rooms.
- Do not hard delete a property that already has rooms or invoices.
- Use `IsActive = false` instead.

---

### Room

Represents a rental room.

Fields:

- Id
- PropertyId
- RoomName
- Floor
- BaseRent
- Status
- Note
- CreatedAt
- UpdatedAt

Room statuses:

- Vacant
- Occupied
- Maintenance
- Inactive

Rules:

- Room name is required.
- Each room belongs to one property.
- Each room has its own base rent.
- Base rent must be greater than or equal to 0.
- Old invoices must not change when base rent changes later.

---

### Tenant

Represents a tenant record.

Fields:

- Id
- FullName
- Phone
- Email
- IdentityNumber
- Note
- CreatedAt
- UpdatedAt

Rules:

- Tenant full name is required.
- Tenant is not a login user.
- Tenant can be assigned to rooms through `RoomTenant`.

---

### RoomTenant

Represents tenant assignment to a room.

Fields:

- Id
- RoomId
- TenantId
- IsRepresentative
- StartDate
- EndDate
- Status

Statuses:

- Active
- Ended

Rules:

- A room can have multiple active tenants.
- A room should have only one active representative tenant.
- The representative tenant is used for invoice display.
- When a tenant moves out, set status to `Ended` and set `EndDate`.

---

### FeeType

Represents a fee category.

Fields:

- Id
- Name
- DefaultCalculationType
- DefaultUnit
- DefaultUnitPrice
- IsSystem
- IsActive

Default seed fee types:

- Electricity
- Water
- Wifi
- Parking
- Garbage
- Other

Rules:

- System fee types should not be hard deleted.
- User can create custom fee types.
- Fee types are not database columns.

---

### RoomFeeConfig

Defines how a fee applies to one room.

Fields:

- Id
- RoomId
- FeeTypeId
- CalculationType
- UnitPrice
- FixedAmount
- Quantity
- Enabled
- Note

Rules:

- Each room can have different fee settings.
- If `UnitPrice` is empty, use `FeeType.DefaultUnitPrice`.
- If `Enabled = false`, exclude it from invoice generation.
- Fixed fee uses `FixedAmount`.
- Meter fee uses meter reading.
- PerPerson fee uses active tenant count.
- PerUnit fee uses `Quantity`.

---

### MeterReading

Stores monthly readings for meter-based fees.

Fields:

- Id
- RoomId
- FeeTypeId
- BillingMonth
- PreviousReading
- CurrentReading
- UsageAmount
- UnitPriceSnapshot
- Amount
- Note

Rules:

- Used only for meter-based fees.
- Billing month format: `YYYY-MM`.
- Current reading must be greater than or equal to previous reading.
- Usage amount = current reading - previous reading.
- Amount = usage amount * unit price snapshot.
- Previous reading can be auto-filled from previous month’s current reading.

---

### Invoice

Represents a monthly invoice for one room.

Fields:

- Id
- RoomId
- BillingMonth
- Status
- Subtotal
- Discount
- ExtraAmount
- TotalAmount
- PaidAmount
- RemainingAmount
- IssuedDate
- PaidDate
- Note
- CreatedAt
- UpdatedAt

Invoice statuses:

- Draft
- Issued
- Partial
- Paid
- Cancelled

Rules:

- One room should have only one main invoice per billing month.
- Invoice should be created as Draft first.
- When confirmed, invoice becomes Issued.
- Issued invoices should not silently change when fee settings change later.
- Old invoices must preserve historical values.

---

### InvoiceItem

Represents one line inside an invoice.

Fields:

- Id
- InvoiceId
- FeeTypeId
- ItemName
- CalculationType
- Quantity
- Unit
- UnitPrice
- Amount
- Note

Rules:

- Base rent always becomes an invoice item named `Room Rent`.
- Each enabled room fee config becomes an invoice item.
- Invoice item must store snapshot values.
- Old invoice items should not change when room fee config changes later.

---

### Payment

Represents a payment for an invoice.

Fields:

- Id
- InvoiceId
- Amount
- PaymentDate
- Method
- Note
- CreatedAt

Payment methods:

- Cash
- BankTransfer
- Momo
- Other

Rules:

- One invoice can have multiple payments.
- Payment amount must be greater than 0.
- PaidAmount = sum of payments for the invoice.
- RemainingAmount = TotalAmount - PaidAmount.

---

## 9. Invoice Generation Logic

Input:

```text
RoomId
BillingMonth
```

Process:

1. Load room.
2. Load property.
3. Load representative tenant.
4. Load enabled room fee configurations.
5. Create invoice with `Draft` status.
6. Add base rent as first invoice item.
7. For each enabled room fee config, calculate fee based on calculation type.
8. Save invoice items.
9. Calculate subtotal.
10. Apply discount.
11. Apply extra amount.
12. Calculate total amount.
13. Save invoice.

Formula:

```text
Subtotal = Sum(InvoiceItem.Amount)
TotalAmount = Subtotal - Discount + ExtraAmount
PaidAmount = Sum(Payment.Amount)
RemainingAmount = TotalAmount - PaidAmount
```

Calculation details:

```text
Fixed:
Amount = FixedAmount

Meter:
Usage = CurrentReading - PreviousReading
UnitPrice = RoomFeeConfig.UnitPrice or FeeType.DefaultUnitPrice
Amount = Usage * UnitPrice

PerPerson:
Amount = ActiveTenantCount * UnitPrice

PerUnit:
Amount = Quantity * UnitPrice

Manual:
Landlord manually enters amount
```

Important rule:

Invoice generation must create snapshot invoice items.  
Do not make old invoices depend on live room fee configuration.

---

## 10. Payment Status Logic

When a payment is recorded:

1. Save payment.
2. Recalculate invoice paid amount.
3. Recalculate invoice remaining amount.
4. Update invoice status.

Status rules:

```text
If invoice is Cancelled:
    Do not auto-update status.

If PaidAmount = 0:
    Status = Issued

If PaidAmount > 0 and PaidAmount < TotalAmount:
    Status = Partial

If PaidAmount >= TotalAmount:
    Status = Paid
    PaidDate = latest payment date
```

---

## 11. UI Requirements

The UI should be optimized for management work.

It should use:

- Tables
- Filters
- Search boxes
- Sortable columns
- Monthly views
- Property filters
- Status filters
- Simple forms
- Dashboard cards

The app should feel like an upgraded Excel workbook.

---

## 12. Main Screens

### Dashboard

Purpose:

Show monthly summary.

Filters:

- Billing month
- Property

Cards:

- Expected revenue
- Collected amount
- Unpaid amount
- Occupied rooms
- Vacant rooms
- Missing meter readings
- Unpaid invoices

Tables:

- Recent payments
- Unpaid invoices
- Rooms missing readings

---

### Properties

Purpose:

Manage rental buildings/houses.

Main actions:

- Add property
- Edit property
- Deactivate property
- View rooms

---

### Rooms

Purpose:

Manage rooms.

Filters:

- Property
- Room status
- Search by room name or tenant name

Main columns:

- Property
- Room name
- Floor
- Base rent
- Representative tenant
- Status

Main actions:

- Add room
- Edit room
- Open room detail
- Configure fees

---

### Room Detail

Purpose:

Manage one room.

Sections:

- Basic room information
- Current tenants
- Representative tenant
- Fee configuration
- Invoice history
- Payment history

---

### Tenants

Purpose:

Manage tenant records.

Actions:

- Add tenant
- Edit tenant
- Assign tenant to room
- End tenant assignment
- Set representative tenant

---

### Fee Settings

Purpose:

Manage global fee types.

Actions:

- Add custom fee type
- Edit fee type
- Deactivate fee type

---

### Room Fee Configuration

Purpose:

Configure how fees apply to each room.

Main columns:

- Room
- Fee type
- Calculation type
- Unit price
- Fixed amount
- Quantity
- Enabled
- Note

---

### Meter Readings

Purpose:

Enter monthly electricity/water readings.

This screen should look similar to an Excel table.

Filters:

- Billing month
- Property
- Fee type
- Missing only

Main columns:

- Property
- Room
- Fee type
- Previous reading
- Current reading
- Usage amount
- Unit price
- Amount
- Note

Actions:

- Auto-fill previous readings
- Save readings
- Generate invoices

---

### Invoices

Purpose:

View and manage monthly invoices.

Filters:

- Billing month
- Property
- Invoice status
- Search by room or tenant

Main columns:

- Billing month
- Property
- Room
- Representative tenant
- Total amount
- Paid amount
- Remaining amount
- Status

Actions:

- Generate invoice
- Generate all invoices for selected month
- View invoice
- Issue invoice
- Record payment
- Copy invoice text
- Cancel invoice

---

### Invoice Detail

Purpose:

View full invoice detail.

Sections:

- Property information
- Room information
- Representative tenant
- Invoice items
- Payment history
- Total summary
- Notes

Actions:

- Issue invoice
- Record payment
- Copy invoice text
- Export PDF later
- Cancel invoice

---

### Payments

Purpose:

Track payment history.

Filters:

- Billing month
- Property
- Payment method
- Search by room or tenant

---

### Settings

Purpose:

Manage app-level settings.

Features:

- Show database location
- Backup database
- Restore database
- Default billing month
- Currency format
- App theme

---

## 13. Dashboard Logic

Dashboard should calculate:

```text
ExpectedRevenue = Sum(TotalAmount of invoices in selected month)
CollectedAmount = Sum(PaidAmount of invoices in selected month)
UnpaidAmount = Sum(RemainingAmount of invoices in selected month)
OccupiedRoomCount = Count rooms where Status = Occupied
VacantRoomCount = Count rooms where Status = Vacant
UnpaidInvoiceCount = Count invoices where Status = Issued or Partial
MissingReadingCount = Count rooms that use Meter fee but do not have MeterReading for selected month
```

---

## 14. Validation Rules

Implement these validations:

- Property name is required.
- Room name is required.
- Base rent must be greater than or equal to 0.
- Tenant full name is required.
- Only one active representative tenant per room.
- Current reading must be greater than or equal to previous reading.
- Unit price must be greater than or equal to 0.
- Fixed amount must be greater than or equal to 0.
- Quantity must be greater than or equal to 0.
- Invoice cannot be generated twice for the same room and billing month unless user confirms recreate.
- Payment amount must be greater than 0.
- Payment amount should not exceed remaining amount unless user confirms.
- Issued or paid invoice should not be silently changed by fee config updates.

---

## 15. Backup and Restore

Because data is local, backup is required.

Features:

- Show current database file path.
- Backup SQLite database file to selected folder.
- Restore SQLite database file from selected backup.
- Warn user before restoring database.

Suggested backup file name:

```text
rental-manager-backup-YYYY-MM-DD.sqlite
```

---

## 16. Architecture

Use MVVM.

Suggested folders inside WPF project:

```text
Models/
Enums/
Data/
Services/
Repositories/
ViewModels/
Views/
DTOs/
Helpers/
Resources/
```

Rules:

- Do not put business logic in XAML code-behind.
- Do not put invoice calculation in UI.
- Use services for business logic.
- Use DbContext/repositories/services for data access.
- Keep UI simple first.
- Prioritize correct data model and billing logic before styling.

---

## 17. Required Services

Implement these services:

- PropertyService
- RoomService
- TenantService
- RoomTenantService
- FeeTypeService
- RoomFeeConfigService
- MeterReadingService
- InvoiceCalculationService
- InvoiceService
- PaymentService
- DashboardService
- BackupService

Most important services:

- InvoiceCalculationService
- InvoiceService
- PaymentService
- MeterReadingService
- DashboardService

---

## 18. Development Order

Build in this order:

1. Project setup
2. Folder structure
3. Enums
4. Models
5. DbContext
6. SQLite setup
7. Seed default fee types
8. Property CRUD
9. Room CRUD
10. Tenant CRUD
11. RoomTenant assignment
12. FeeType management
13. RoomFeeConfig management
14. MeterReading screen
15. InvoiceCalculationService
16. Invoice generation
17. Invoice list screen
18. Invoice detail screen
19. Payment recording
20. Dashboard
21. Backup and restore
22. Copy invoice text
23. Export features later
24. Reports later
25. Tests later

---

## 19. Version 1 Non-Goals

Do not build these in version 1:

- Tenant login
- Landlord account login
- Cloud sync
- Online payment
- Mobile app
- Multi-user roles
- Permission system
- Automatic email sending
- Automatic Zalo sending
- AI features
- Complex accounting module
- Tax module
- Contract management module

---

## 20. Initial Implementation Expectation

The first usable version should allow the landlord to:

1. Add a property.
2. Add rooms under that property.
3. Add tenants.
4. Assign tenants to rooms.
5. Mark one tenant as representative.
6. Configure room fees.
7. Enter monthly meter readings.
8. Generate invoice for a room.
9. View invoice details.
10. Record payment.
11. See dashboard summary.
12. Backup the SQLite database.

The first version does not need beautiful UI.  
It must have correct data structure and correct billing logic.
