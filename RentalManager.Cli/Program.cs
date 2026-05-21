using System;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;
using RentalManager.Services;

namespace RentalManager.Cli;

class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Ensure Database is initialized (applies schema if needed)
        DbContextFactory.EnsureDatabase();
        
        Console.Error.WriteLine($"[DEBUG] Database Path: {DbContextFactory.DatabasePath}");

        if (args.Length < 1)
        {
            PrintJson(new
            {
                success = false,
                code = "INVALID_ARGUMENT",
                message = "Chưa nhập lệnh.",
                nextAction = "Dùng một trong các lệnh: invoice create, invoice unpaid, meter add, payment add."
            });
            return;
        }

        var command = args[0].ToLowerInvariant();
        var subCommand = args.Length > 1 ? args[1].ToLowerInvariant() : string.Empty;

        try
        {
            if (command == "invoice" && subCommand == "create")
            {
                HandleInvoiceCreate(args);
            }
            else if (command == "invoice" && subCommand == "unpaid")
            {
                HandleInvoiceUnpaid(args);
            }
            else if (command == "meter" && subCommand == "add")
            {
                HandleMeterAdd(args);
            }
            else if (command == "payment" && subCommand == "add")
            {
                HandlePaymentAdd(args);
            }
            else if (command == "seed-test")
            {
                HandleSeedTestData();
            }
            else
            {
                PrintJson(new
                {
                    success = false,
                    code = "INVALID_ARGUMENT",
                    message = "Lệnh không được hỗ trợ.",
                    nextAction = "Dùng một trong các lệnh: invoice create, invoice unpaid, meter add, payment add."
                });
            }
        }
        catch (ValidationException ex)
        {
            string code = "VALIDATION_ERROR";
            string nextAction = "Kiểm tra lại tham số và thử lại.";
            object? details = null;

            if (ex.Message.Contains("thiếu chỉ số"))
            {
                code = "MISSING_METER_READING";
                var roomStr = GetArg(args, "room");
                var month = GetArg(args, "month");
                details = new { room = roomStr, billingMonth = month };
                nextAction = "Nhập chỉ số điện/nước cho tháng đang xử lý rồi tạo hóa đơn lại.";
            }
            else if (ex.Message.Contains("đã tồn tại"))
            {
                code = "INVOICE_ALREADY_EXISTS";
                nextAction = "Dùng hóa đơn hiện có hoặc chọn tháng/phòng khác.";
            }
            else if (ex.Message.Contains("Có nhiều phòng trùng tên"))
            {
                code = "AMBIGUOUS_ROOM";
                nextAction = "Thêm --property để xác định đúng nhà / khu trọ.";
            }
            else if (ex.Message.Contains("Không tìm thấy phòng"))
            {
                code = "ROOM_NOT_FOUND";
                nextAction = "Kiểm tra lại --property và --room.";
            }
            else if (ex.Message.Contains("Không tìm thấy nhà") || ex.Message.Contains("Property was not found"))
            {
                code = "PROPERTY_NOT_FOUND";
                nextAction = "Kiểm tra lại tên nhà / khu trọ.";
            }
            else if (ex.Message.Contains("Không tìm thấy loại phí"))
            {
                code = "FEE_NOT_FOUND";
                nextAction = "Kiểm tra lại --fee theo tên khoản phí đang dùng trong app.";
            }
            else if (ex.Message.Contains("Số tiền thanh toán") || ex.Message.Contains("thanh toán"))
            {
                code = "PAYMENT_ERROR";
                nextAction = "Kiểm tra invoiceId, số tiền còn lại và số tiền thanh toán.";
            }
            else if (ex.Message.Contains("Thiếu tham số") || ex.Message.Contains("Vui lòng"))
            {
                code = "INVALID_ARGUMENT";
            }

            PrintJson(new
            {
                success = false,
                code = code,
                message = ex.Message,
                nextAction = nextAction,
                details = details
            });
        }
        catch (Exception ex)
        {
            var code = ex is FormatException or OverflowException ? "INVALID_ARGUMENT" : "SYSTEM_ERROR";
            var nextAction = code == "INVALID_ARGUMENT"
                ? "Kiểm tra định dạng số, tháng và các tham số rồi thử lại."
                : "Kiểm tra log hoặc chạy lại lệnh sau khi xác nhận dữ liệu.";
            PrintJson(new
            {
                success = false,
                code = code,
                message = ex.Message,
                nextAction = nextAction
            });
        }
    }

    static string? GetArg(string[] args, string name)
    {
        var idx = Array.IndexOf(args, "--" + name);
        if (idx >= 0 && idx < args.Length - 1) return args[idx + 1];
        return null;
    }

    static void PrintJson(object data)
    {
        Console.WriteLine(JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }

    static Room FindRoom(string? propertyStr, string roomStr)
    {
        using var db = DbContextFactory.Create();
        var lowerRoomStr = roomStr?.ToLowerInvariant();
        var lowerPropertyStr = propertyStr?.ToLowerInvariant();
        
        var query = db.Rooms.Include(x => x.Property).AsEnumerable();
        
        List<Room> matchedRooms = new List<Room>();

        if (!string.IsNullOrEmpty(lowerPropertyStr))
        {
            query = query.Where(r => r.Property != null && r.Property.Name.ToLowerInvariant() == lowerPropertyStr);
            matchedRooms = query.Where(r => r.RoomName.ToLowerInvariant() == lowerRoomStr).ToList();
        }
        else
        {
            matchedRooms = query.Where(r => 
                r.RoomName.ToLowerInvariant() == lowerRoomStr || 
                (r.Property != null && (r.Property.Name + " - " + r.RoomName).ToLowerInvariant() == lowerRoomStr) ||
                (r.Property != null && (r.Property.Name + "-" + r.RoomName).ToLowerInvariant() == lowerRoomStr) ||
                r.DisplayNameWithProperty.ToLowerInvariant() == lowerRoomStr).ToList();
        }
        
        if (matchedRooms.Count > 1)
        {
            throw new ValidationException("Có nhiều phòng trùng tên. Vui lòng chỉ định nhà / khu trọ.");
        }
        if (matchedRooms.Count == 1)
        {
            return matchedRooms[0];
        }
        
        throw new ValidationException($"Không tìm thấy phòng: {propertyStr} {roomStr}");
    }

    static int FindFeeType(string name)
    {
        using var db = DbContextFactory.Create();
        var fee = db.FeeTypes.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        if (fee == null) throw new ValidationException($"Không tìm thấy loại phí: {name}");
        return fee.Id;
    }

    static void HandleInvoiceCreate(string[] args)
    {
        var propertyStr = GetArg(args, "property");
        var roomStr = GetArg(args, "room");
        var month = GetArg(args, "month");

        if (string.IsNullOrEmpty(roomStr) || string.IsNullOrEmpty(month))
            throw new ValidationException("Thiếu tham số --room hoặc --month");

        var room = FindRoom(propertyStr, roomStr);
        var invoiceService = new InvoiceService();
        
        var invoice = invoiceService.Generate(room.Id, month, recreate: false);
        
        PrintJson(new
        {
            success = true,
            code = "INVOICE_CREATED",
            message = "Đã tạo hóa đơn.",
            data = new
            {
                invoiceId = invoice.Id,
                room = room.DisplayNameWithProperty,
                billingMonth = invoice.BillingMonth,
                totalAmount = invoice.TotalAmount,
                remainingAmount = invoice.RemainingAmount
            }
        });
    }

    static void HandleInvoiceUnpaid(string[] args)
    {
        var month = GetArg(args, "month");
        using var db = DbContextFactory.Create();
        
        var query = db.Invoices
            .Include(x => x.Room)
            .ThenInclude(x => x!.Property)
            .AsQueryable();
            
        if (!string.IsNullOrEmpty(month))
        {
            query = query.Where(x => x.BillingMonth == month);
        }
        
        var unpaid = query
            .Where(x => (x.Status == InvoiceStatus.Issued || x.Status == InvoiceStatus.Partial) && x.RemainingAmount > 0)
            .ToList();
        var count = unpaid.Count;
        
        PrintJson(new
        {
            success = true,
            code = "OK",
            count = count,
            message = count == 0
                ? "Không có hóa đơn chưa thanh toán phù hợp."
                : $"Tìm thấy {count} hóa đơn chưa thanh toán.",
            data = unpaid.Select(x => new {
                invoiceId = x.Id,
                room = x.Room?.DisplayNameWithProperty,
                billingMonth = x.BillingMonth,
                totalAmount = x.TotalAmount,
                remainingAmount = x.RemainingAmount,
                status = DisplayText.For(x.Status)
            })
        });
    }

    static void HandleMeterAdd(string[] args)
    {
        var propertyStr = GetArg(args, "property");
        var roomStr = GetArg(args, "room");
        var feeStr = GetArg(args, "fee");
        var month = GetArg(args, "month");
        var currentStr = GetArg(args, "current");

        if (string.IsNullOrEmpty(roomStr) || string.IsNullOrEmpty(feeStr) || string.IsNullOrEmpty(month) || string.IsNullOrEmpty(currentStr))
            throw new ValidationException("Thiếu tham số bắt buộc (--room, --fee, --month, --current)");

        var room = FindRoom(propertyStr, roomStr);
        var feeTypeId = FindFeeType(feeStr);
        
        var meterService = new MeterReadingService();
        var previousReading = meterService.GetPreviousReading(room.Id, feeTypeId, month);
        
        var reading = new MeterReading
        {
            RoomId = room.Id,
            FeeTypeId = feeTypeId,
            BillingMonth = month,
            PreviousReading = previousReading,
            CurrentReading = decimal.Parse(currentStr)
        };
        
        var saved = meterService.Save(reading);
        
        PrintJson(new
        {
            success = true,
            code = "METER_READING_SAVED",
            message = "Đã cập nhật chỉ số.",
            data = new
            {
                room = room.DisplayNameWithProperty,
                fee = feeStr,
                billingMonth = month,
                previous = saved.PreviousReading,
                current = saved.CurrentReading
            }
        });
    }

    static void HandlePaymentAdd(string[] args)
    {
        var invoiceIdStr = GetArg(args, "invoice");
        var amountStr = GetArg(args, "amount");
        var methodStr = GetArg(args, "method");
        var noteStr = GetArg(args, "note");

        if (string.IsNullOrEmpty(invoiceIdStr) || string.IsNullOrEmpty(amountStr))
            throw new ValidationException("Thiếu tham số bắt buộc (--invoice, --amount)");

        var invoiceId = int.Parse(invoiceIdStr);
        var amount = decimal.Parse(amountStr);
        
        PaymentMethod method = PaymentMethod.Cash;
        if (!string.IsNullOrEmpty(methodStr))
        {
            if (Enum.TryParse(methodStr, true, out PaymentMethod parsedMethod))
            {
                method = parsedMethod;
            }
        }

        var paymentService = new PaymentService();
        var payment = paymentService.Record(invoiceId, amount, method, DateTime.Today, noteStr);
        
        PrintJson(new
        {
            success = true,
            code = "PAYMENT_RECORDED",
            message = "Đã ghi nhận thanh toán.",
            data = new
            {
                paymentId = payment.Id,
                invoiceId = invoiceId,
                amount = amount,
                method = method.ToString()
            }
        });
    }

    static void HandleSeedTestData()
    {
        using var db = DbContextFactory.Create();
        
        var property = new Property { Name = "Nha Test", Address = "Test Address", IsActive = true };
        db.Properties.Add(property);
        db.SaveChanges();

        var room = new Room { PropertyId = property.Id, RoomName = "Phong 101", Floor = "1", BaseRent = 3000000, Status = RoomStatus.Occupied };
        db.Rooms.Add(room);
        db.SaveChanges();

        var tenant = new Tenant { FullName = "Test Tenant", IdentityNumber = "123456789", Phone = "0123456789" };
        db.Tenants.Add(tenant);
        db.SaveChanges();
        
        var roomTenant = new RoomTenant { RoomId = room.Id, TenantId = tenant.Id, IsRepresentative = true, Status = RoomTenantStatus.Active, StartDate = DateTime.Today };
        db.RoomTenants.Add(roomTenant);
        db.SaveChanges();

        var feeType = new FeeType { Name = "Dien", DefaultCalculationType = CalculationType.Meter, DefaultUnitPrice = 3500, IsActive = true };
        db.FeeTypes.Add(feeType);
        db.SaveChanges();

        var roomFee = new RoomFeeConfig { RoomId = room.Id, FeeTypeId = feeType.Id, Enabled = true, CalculationType = CalculationType.Meter };
        db.RoomFeeConfigs.Add(roomFee);
        db.SaveChanges();
        
        // Seed a previous meter reading so we can add one for the current month
        var meterService = new MeterReadingService();
        var prevReading = new MeterReading
        {
            RoomId = room.Id,
            FeeTypeId = feeType.Id,
            BillingMonth = "2026-03",
            PreviousReading = 0,
            CurrentReading = 100,
            UsageAmount = 100,
            UnitPriceSnapshot = 3500,
            Amount = 350000
        };
        meterService.Save(prevReading);

        PrintJson(new { success = true, code = "SEED_CREATED", message = "Seed data created successfully." });
    }
}
