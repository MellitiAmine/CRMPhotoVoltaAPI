namespace CrmPhotoVolta.Domain.App;

public class Invoice : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string Reference { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;

    public DateOnly InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }

    public decimal TotalHt { get; set; }
    public decimal TotalTva { get; set; }
    public decimal TotalTtc { get; set; }
    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount => TotalTtc - PaidAmount;

    public string? PdfUrl { get; set; }
    public string? Notes { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
