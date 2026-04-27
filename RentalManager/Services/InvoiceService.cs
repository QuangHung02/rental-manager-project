using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class InvoiceService
{
    private readonly InvoiceCalculationService _calculationService = new();

    public List<Invoice> GetAll()
    {
        using var db = DbContextFactory.Create();
        return db.Invoices
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.Room).ThenInclude(x => x!.RoomTenants).ThenInclude(x => x.Tenant)
            .Include(x => x.Items)
            .Include(x => x.Payments)
            .AsNoTracking()
            .OrderByDescending(x => x.BillingMonth)
            .ThenBy(x => x.Room!.RoomName)
            .ToList();
    }

    public Invoice Generate(int roomId, string billingMonth, bool recreate = false)
    {
        using var db = DbContextFactory.Create();
        var existing = db.Invoices.Include(x => x.Items).FirstOrDefault(x => x.RoomId == roomId && x.BillingMonth == billingMonth);
        if (existing is not null)
        {
            if (!recreate)
            {
                throw new ValidationException("Invoice already exists for this room and billing month.");
            }

            if (existing.Status is InvoiceStatus.Issued or InvoiceStatus.Partial or InvoiceStatus.Paid)
            {
                throw new ValidationException("Issued or paid invoices cannot be silently recreated.");
            }

            db.InvoiceItems.RemoveRange(existing.Items);
            db.Invoices.Remove(existing);
            db.SaveChanges();
        }

        var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
        db.Invoices.Add(invoice);
        db.SaveChanges();
        return invoice;
    }

    public void GenerateAll(string billingMonth)
    {
        using var db = DbContextFactory.Create();
        var roomIds = db.Rooms.Where(x => x.Status != RoomStatus.Inactive).Select(x => x.Id).ToList();
        foreach (var roomId in roomIds)
        {
            if (!db.Invoices.Any(x => x.RoomId == roomId && x.BillingMonth == billingMonth))
            {
                var invoice = _calculationService.BuildDraftInvoice(roomId, billingMonth);
                db.Invoices.Add(invoice);
            }
        }

        db.SaveChanges();
    }

    public void Issue(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices.Find(invoiceId) ?? throw new ValidationException("Invoice was not found.");
        if (invoice.Status == InvoiceStatus.Draft)
        {
            invoice.Status = InvoiceStatus.Issued;
            invoice.IssuedDate = DateTime.Today;
            invoice.UpdatedAt = DateTime.Now;
            db.SaveChanges();
        }
    }

    public void Cancel(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices.Find(invoiceId) ?? throw new ValidationException("Invoice was not found.");
        invoice.Status = InvoiceStatus.Cancelled;
        invoice.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }

    public string CopyText(int invoiceId)
    {
        using var db = DbContextFactory.Create();
        var invoice = db.Invoices
            .Include(x => x.Room).ThenInclude(x => x!.Property)
            .Include(x => x.Room).ThenInclude(x => x!.RoomTenants).ThenInclude(x => x.Tenant)
            .Include(x => x.Items)
            .First(x => x.Id == invoiceId);
        var representative = invoice.Room!.RoomTenants.FirstOrDefault(x => x.Status == RoomTenantStatus.Active && x.IsRepresentative)?.Tenant?.FullName ?? "";
        var lines = new List<string>
        {
            $"Invoice {invoice.BillingMonth}",
            $"Property: {invoice.Room.Property?.Name}",
            $"Room: {invoice.Room.RoomName}",
            $"Representative: {representative}"
        };
        lines.AddRange(invoice.Items.Select(x => $"{x.ItemName}: {x.Amount:N0}"));
        lines.Add($"Total: {invoice.TotalAmount:N0}");
        lines.Add($"Paid: {invoice.PaidAmount:N0}");
        lines.Add($"Remaining: {invoice.RemainingAmount:N0}");
        return string.Join(Environment.NewLine, lines);
    }
}
