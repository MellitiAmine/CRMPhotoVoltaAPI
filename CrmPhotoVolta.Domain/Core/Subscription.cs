namespace CrmPhotoVolta.Domain.Core;

public class Subscription : Common.EntityBase
{
    public Guid SocietyId { get; set; }
    public Society Society { get; set; } = null!;

    public Guid PlanId { get; set; }
    public SubscriptionPlan Plan { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string Status { get; set; } = "Active";
}
