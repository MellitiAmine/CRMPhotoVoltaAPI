using CrmPhotoVolta.Domain.Core;

namespace CrmPhotoVolta.Infrastructure.Data;

public static class SubscriptionPeriodCalculator
{
    /// <summary>End date for a subscription row starting at <paramref name="start"/> under <paramref name="plan"/>.</summary>
    public static DateOnly ComputeEndDate(DateOnly start, SubscriptionPlan plan)
    {
        var months = plan.TrialDurationMonths ?? plan.BillingPeriodMonths;
        if (months <= 0)
            months = 12;

        return start.AddMonths(months);
    }
}
