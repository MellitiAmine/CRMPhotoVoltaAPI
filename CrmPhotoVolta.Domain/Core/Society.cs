namespace CrmPhotoVolta.Domain.Core;

public class Society : Common.EntityBase
{
    public string Name { get; set; } = string.Empty;
    public Guid? SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<UserSociety> UserSocieties { get; set; } = new List<UserSociety>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
