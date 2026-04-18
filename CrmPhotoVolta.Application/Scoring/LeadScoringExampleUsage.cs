using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Scoring;

public static class LeadScoringExampleUsage
{
    public static LeadScoreSnapshot Example()
    {
        ILeadScoringService engine = new LeadScoringService();

        var lead = new Lead
        {
            Name = "Demo",
            MonthlyBillEur = 400,
            EstimatedKw = 6,
            BonusQuoteRequested = true,
            BonusDecisionMaker = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var activities = new List<LeadActivity>
        {
            new()
            {
                Type = LeadActivityType.Call,
                Rating = 4,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-6)
            },
            new()
            {
                Type = LeadActivityType.QuoteRequest,
                Rating = 5,
                CreatedAt = DateTimeOffset.UtcNow.AddHours(-2)
            }
        };

        return engine.Calculate(lead, activities);
    }
}
