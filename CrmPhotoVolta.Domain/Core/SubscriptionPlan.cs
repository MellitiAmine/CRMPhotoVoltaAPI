namespace CrmPhotoVolta.Domain.Core;

public class SubscriptionPlan : Common.EntityBase
{
    /// <summary>Stable key for seeding and API (e.g. FREE_TRIAL_3M, STANDARD_100_M).</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "TND";

    /// <summary>Monthly (or per-cycle) price. Trial plans are typically 0.</summary>
    public decimal Price { get; set; }

    /// <summary>When set, new subscriptions run for this many months (e.g. free trial = 3).</summary>
    public int? TrialDurationMonths { get; set; }

    /// <summary>Billing cycle length in months when not a fixed trial (e.g. 1 = monthly).</summary>
    public int BillingPeriodMonths { get; set; } = 1;

    public int MaxUsers { get; set; }
    public int MaxProjects { get; set; }

    public ICollection<Society> Societies { get; set; } = new List<Society>();
    public ICollection<Subscription> Subscriptions { get; set; } = new List<Subscription>();
}
