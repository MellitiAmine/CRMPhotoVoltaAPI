namespace CrmPhotoVolta.Domain.App;

public class Lead : SocietyScopedEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string Status { get; set; } = LeadStatuses.Nouveau;
    public Guid? AssignedToUserId { get; set; }

    /// <summary>Legacy aggregate rating (1–5); scoring satisfaction uses activity ratings first.</summary>
    public double AverageRating { get; set; }

    public double? MonthlyBillEur { get; set; }
    public double? EstimatedKw { get; set; }
    public double? MontantEstimé { get; set; }

    public bool BonusQuoteRequested { get; set; }
    public bool BonusBudgetConfirmed { get; set; }
    public bool BonusDecisionMaker { get; set; }
    public bool BonusFinancingInterest { get; set; }

    public double? Lvi { get; set; }
    public double? Sd { get; set; }
    public DateTimeOffset? ScoredAt { get; set; }

    public LeadTemperature Temperature { get; private set; } = LeadTemperature.Cold;
    public LeadPriority Priority { get; private set; } = LeadPriority.Low;

    public double? ScoreBreakdownInteraction { get; set; }
    public double? ScoreBreakdownIntention { get; set; }
    public double? ScoreBreakdownSatisfaction { get; set; }
    public double? ScoreBreakdownActivity { get; set; }
    public double? ScoreBreakdownPotential { get; set; }
    public double? ScoreBreakdownPenalties { get; set; }

    /// <summary>Manual tags added by commercial/admin users (stored as JSON array).</summary>
    public List<string> Tags { get; set; } = new();

    public ICollection<LeadActivity> Activities { get; set; } = new List<LeadActivity>();
    public ICollection<Deal> Deals { get; set; } = new List<Deal>();

    public void ApplyScoring(in LeadScoreSnapshot snapshot)
    {
        Lvi = snapshot.Lvi;
        Sd = snapshot.Sd;
        Temperature = snapshot.Temperature;
        Priority = snapshot.Priority;
        ScoredAt = DateTimeOffset.UtcNow;
        ScoreBreakdownInteraction = snapshot.Breakdown.Interaction;
        ScoreBreakdownIntention = snapshot.Breakdown.Intention;
        ScoreBreakdownSatisfaction = snapshot.Breakdown.Satisfaction;
        ScoreBreakdownActivity = snapshot.Breakdown.Activity;
        ScoreBreakdownPotential = snapshot.Breakdown.Potential;
        ScoreBreakdownPenalties = snapshot.Breakdown.Penalties;
    }
}
