using System.Runtime.CompilerServices;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Scoring;

public sealed class LeadScoringService : ILeadScoringService
{
    private const double MaxScore = 100.0;

    private const double WInteraction = 0.25;
    private const double WIntention = 0.30;
    private const double WSatisfaction = 0.15;
    private const double WActivity = 0.10;
    private const double WPotential = 0.20;
    private const double SdSatisfactionFactor = 0.5;

    private const double PtsCall = 10.0;
    private const double PtsWhatsApp = 5.0;
    private const double PtsSms = 5.0;
    private const double PtsMeeting = 10.0;
    private const double PtsTechnicalVisit = 15.0;

    private const double PtsInfo = 5.0;
    private const double PtsQuote = 20.0;
    private const double PtsNegotiation = 15.0;
    private const double PtsStrongBuying = 25.0;

    private const double SatisfactionRatingScale = 20.0;

    private const double PotentialBillFactor = 0.30;
    private const double PotentialKwFactor = 5.0;
    private const double BonusQuote = 25.0;
    private const double BonusBudget = 15.0;
    private const double BonusDecisionMaker = 10.0;
    private const double BonusFinancing = 10.0;

    private const double PenaltyTier3 = 3.0;
    private const double PenaltyTier7 = 7.0;
    private const double PenaltyTier14 = 14.0;
    private const double PenaltyTier21 = 21.0;
    private const double PenaltyAmt3 = 5.0;
    private const double PenaltyAmt7 = 10.0;
    private const double PenaltyAmt14 = 20.0;
    private const double PenaltyAmt21 = 30.0;

    private const double RecencyHours24 = 24.0;
    private const double RecencyDays3 = 3.0;
    private const double RecencyDays7 = 7.0;

    public LeadScoreSnapshot Calculate(Lead lead, IReadOnlyList<LeadActivity> activities)
    {
        var interactionRaw = 0.0;
        var intentionRaw = 0.0;
        var ratingWeightedSum = 0.0;
        var ratingWeightSum = 0.0;
        var quoteFromActivity = false;
        DateTimeOffset? lastActivityAt = null;

        var count = activities.Count;
        for (var i = 0; i < count; i++)
        {
            var a = activities[i];
            var t = a.Type;

            AccumulateInteractionAndIntention(t, ref interactionRaw, ref intentionRaw);

            if (t == LeadActivityType.QuoteRequest)
                quoteFromActivity = true;

            if (a.Rating is int rv && rv >= 1 && rv <= 5)
            {
                var rw = RatingWeight(t);
                if (rw > 0.0)
                {
                    ratingWeightedSum += rv * rw;
                    ratingWeightSum += rw;
                }
            }

            var at = a.CreatedAt;
            if (lastActivityAt is null || at > lastActivityAt.Value)
                lastActivityAt = at;
        }

        var sInteraction = ClampZeroTo100(interactionRaw);
        var sIntention = ClampZeroTo100(intentionRaw);

        var avgRating = ratingWeightSum > 0.0 ? ratingWeightedSum / ratingWeightSum : 0.0;
        var sSatisfaction = ClampZeroTo100(avgRating * SatisfactionRatingScale);

        var now = DateTimeOffset.UtcNow;
        double sActivity;
        double sPenalties;

        if (lastActivityAt is { } lastAct)
        {
            var elapsed = now - lastAct;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            sActivity = ComputeActivityRecency(elapsed);
            sPenalties = ComputePenalties(elapsed);
        }
        else
        {
            sActivity = 0.0;
            var anchor = lead.UpdatedAt ?? lead.CreatedAt;
            var elapsed = now - anchor;
            if (elapsed < TimeSpan.Zero) elapsed = TimeSpan.Zero;
            sPenalties = ComputePenalties(elapsed);
        }

        var bonuses = new PotentialBonusesState(
            lead.BonusQuoteRequested || quoteFromActivity,
            lead.BonusBudgetConfirmed,
            lead.BonusDecisionMaker,
            lead.BonusFinancingInterest);

        var sPotential = ComputePotential(lead.MonthlyBillEur ?? 0.0, lead.EstimatedKw ?? 0.0, in bonuses);

        var lvi = (WInteraction * sInteraction)
            + (WIntention * sIntention)
            + (WSatisfaction * sSatisfaction)
            + (WActivity * sActivity)
            + (WPotential * sPotential)
            - sPenalties;

        lvi = ClampZeroTo100(lvi);
        var sd = ClampZeroTo100(lvi + SdSatisfactionFactor * sSatisfaction);

        var temperature = MapTemperature(sd);
        var priority = MapPriority(sd);

        return new LeadScoreSnapshot
        {
            Lvi = lvi,
            Sd = sd,
            Temperature = temperature,
            Priority = priority,
            Breakdown = new LeadScoreBreakdown
            {
                Interaction = sInteraction,
                Intention = sIntention,
                Satisfaction = sSatisfaction,
                Activity = sActivity,
                Potential = sPotential,
                Penalties = sPenalties
            }
        };
    }

    private readonly struct PotentialBonusesState
    {
        public readonly bool QuoteRequested;
        public readonly bool BudgetConfirmed;
        public readonly bool DecisionMaker;
        public readonly bool FinancingInterest;

        public PotentialBonusesState(bool quote, bool budget, bool dm, bool fin)
        {
            QuoteRequested = quote;
            BudgetConfirmed = budget;
            DecisionMaker = dm;
            FinancingInterest = fin;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AccumulateInteractionAndIntention(
        LeadActivityType t,
        ref double interactionRaw,
        ref double intentionRaw)
    {
        switch (t)
        {
            case LeadActivityType.Call:
                interactionRaw += PtsCall;
                return;
            case LeadActivityType.WhatsApp:
                interactionRaw += PtsWhatsApp;
                return;
            case LeadActivityType.Sms:
                interactionRaw += PtsSms;
                return;
            case LeadActivityType.MeetingScheduled:
                interactionRaw += PtsMeeting;
                return;
            case LeadActivityType.TechnicalVisit:
                interactionRaw += PtsTechnicalVisit;
                return;
            case LeadActivityType.InfoRequest:
                intentionRaw += PtsInfo;
                return;
            case LeadActivityType.QuoteRequest:
                intentionRaw += PtsQuote;
                return;
            case LeadActivityType.Negotiation:
                intentionRaw += PtsNegotiation;
                return;
            case LeadActivityType.StrongBuyingSignal:
                intentionRaw += PtsStrongBuying;
                return;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double RatingWeight(LeadActivityType t)
    {
        switch (t)
        {
            case LeadActivityType.Call: return PtsCall;
            case LeadActivityType.WhatsApp: return PtsWhatsApp;
            case LeadActivityType.Sms: return PtsSms;
            case LeadActivityType.MeetingScheduled: return PtsMeeting;
            case LeadActivityType.TechnicalVisit: return PtsTechnicalVisit;
            case LeadActivityType.InfoRequest: return PtsInfo;
            case LeadActivityType.QuoteRequest: return PtsQuote;
            case LeadActivityType.Negotiation: return PtsNegotiation;
            case LeadActivityType.StrongBuyingSignal: return PtsStrongBuying;
            default: return 0.0;
        }
    }

    private static double ComputeActivityRecency(TimeSpan elapsed)
    {
        var hours = elapsed.TotalHours;
        if (hours < RecencyHours24)
            return 100.0;

        var days = elapsed.TotalDays;
        if (days <= RecencyDays3)
            return 70.0;
        if (days <= RecencyDays7)
            return 40.0;
        return 0.0;
    }

    private static double ComputePotential(double monthlyBill, double estimatedKw, in PotentialBonusesState b)
    {
        var bill = monthlyBill < 0.0 ? 0.0 : monthlyBill;
        var kw = estimatedKw < 0.0 ? 0.0 : estimatedKw;

        var raw = (bill * PotentialBillFactor) + (kw * PotentialKwFactor);
        if (b.QuoteRequested) raw += BonusQuote;
        if (b.BudgetConfirmed) raw += BonusBudget;
        if (b.DecisionMaker) raw += BonusDecisionMaker;
        if (b.FinancingInterest) raw += BonusFinancing;

        return ClampZeroTo100(raw);
    }

    private static double ComputePenalties(TimeSpan elapsed)
    {
        var days = elapsed.TotalDays;
        if (days < 0.0) days = 0.0;

        if (days <= PenaltyTier3) return 0.0;
        if (days <= PenaltyTier7) return PenaltyAmt3;
        if (days <= PenaltyTier14) return PenaltyAmt7;
        if (days <= PenaltyTier21) return PenaltyAmt14;
        return PenaltyAmt21;
    }

    private static LeadTemperature MapTemperature(double sd)
    {
        if (sd >= 85.0) return LeadTemperature.Hot;
        if (sd >= 70.0) return LeadTemperature.High;
        if (sd >= 50.0) return LeadTemperature.Medium;
        if (sd >= 20.0) return LeadTemperature.Low;
        return LeadTemperature.Cold;
    }

    private static LeadPriority MapPriority(double sd)
    {
        if (sd >= 85.0) return LeadPriority.Urgent;
        if (sd >= 60.0) return LeadPriority.High;
        return LeadPriority.Low;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static double ClampZeroTo100(double value)
    {
        if (value < 0.0) return 0.0;
        return value > MaxScore ? MaxScore : value;
    }
}
