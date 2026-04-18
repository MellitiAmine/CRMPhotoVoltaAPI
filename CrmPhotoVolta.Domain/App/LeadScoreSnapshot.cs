namespace CrmPhotoVolta.Domain.App;

public readonly struct LeadScoreBreakdown
{
    public double Interaction { get; init; }
    public double Intention { get; init; }
    public double Satisfaction { get; init; }
    public double Activity { get; init; }
    public double Potential { get; init; }
    public double Penalties { get; init; }
}

public readonly struct LeadScoreSnapshot
{
    public double Lvi { get; init; }
    public double Sd { get; init; }
    public LeadTemperature Temperature { get; init; }
    public LeadPriority Priority { get; init; }
    public LeadScoreBreakdown Breakdown { get; init; }
}
