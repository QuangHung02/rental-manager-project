using Microsoft.EntityFrameworkCore;
using RentalManager.Data;
using RentalManager.Enums;
using RentalManager.Helpers;
using RentalManager.Models;

namespace RentalManager.Services;

public class PaymentService
{
    public List<Payment> GetAll()
    {
        using var db = DbContextFactory.Create();
        return db.Payments.Include(x => x.Invoice).ThenInclude(x => x!.Room).ThenInclude(x => x!.Property)
            .AsNoTracking()
            .OrderByDescending(x => x.PaymentDate)
            .ToList();
    }

    public Payment Record(int invoiceId, decimal amount, PaymentMethod method, DateTime paymentDate, string? note = null, bool allowOverpay = false)
    {
        if (amount <= 0)
        {
            throw new ValidationException("Payment amount must be greater than 0.");
        }

        using var db = DbContextFactory.Create();
        var invoice = db.Invoices.Include(x => x.Payments).FirstOrDefault(x => x.Id == invoiceId) ?? throw new ValidationException("Invoice was not found.");
        if (!allowOverpay && amount > invoice.RemainingAmount)
        {
            throw new ValidationException("Payment amount exceeds remaining amount.");
        }

        var payment = new Payment { InvoiceId = invoiceId, Amount = amount, Method = method, PaymentDate = paymentDate, Note = note };
        db.Payments.Add(payment);
        db.SaveChanges();
        RecalculateInvoice(db, invoiceId);
        return payment;
    }

    public void RecalculateInvoice(RentalManagerDbContext db, int invoiceId)
    {
        var invoice = db.Invoices.Include(x => x.Payments).First(x => x.Id == invoiceId);
        invoice.PaidAmount = invoice.Payments.Sum(x => x.Amount);
        invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
        if (invoice.Status != InvoiceStatus.Cancelled)
        {
            if (invoice.PaidAmount <= 0)
            {
                invoice.Status = InvoiceStatus.Issued;
                invoice.PaidDate = null;
            }
            else if (invoice.PaidAmount < invoice.TotalAmount)
            {
                invoice.Status = InvoiceStatus.Partial;
                invoice.PaidDate = null;
            }
            else
            {
                invoice.Status = InvoiceStatus.Paid;
                invoice.PaidDate = invoice.Payments.Max(x => x.PaymentDate);
            }
        }

        invoice.UpdatedAt = DateTime.Now;
        db.SaveChanges();
    }
}
