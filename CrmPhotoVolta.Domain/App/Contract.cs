namespace CrmPhotoVolta.Domain.App;

public class Contract : SocietyScopedEntity
{
    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public Guid ClientId { get; set; }
    public Client Client { get; set; } = null!;

    public string Reference { get; set; } = string.Empty;
    public ContractType Type { get; set; } = ContractType.Installation;
    public ContractStatus Status { get; set; } = ContractStatus.Draft;

    public DateTimeOffset? SignedAt { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public decimal TotalAmount { get; set; }
    public string? Notes { get; set; }
    public string? PdfUrl { get; set; }
}
