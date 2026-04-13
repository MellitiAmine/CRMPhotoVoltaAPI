namespace CrmPhotoVolta.Domain.App;

public class Quote : SocietyScopedEntity
{
    public Guid? LeadId { get; set; }
    public Lead? Lead { get; set; }

    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }

    public Guid? DealId { get; set; }
    public Deal? Deal { get; set; }

    public Guid? ProjectId { get; set; }
    public Project? Project { get; set; }

    public string QuoteNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;

    public string Currency { get; set; } = "TND";
    public decimal TotalAmount { get; set; }
    public DateOnly? ValidUntil { get; set; }

    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }

    public ICollection<QuoteItem> Items { get; set; } = new List<QuoteItem>();
}
