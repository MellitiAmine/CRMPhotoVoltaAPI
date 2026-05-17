namespace CrmPhotoVolta.Domain.App;

public class Payment : SocietyScopedEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateOnly PaidOn { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.BankTransfer;
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
