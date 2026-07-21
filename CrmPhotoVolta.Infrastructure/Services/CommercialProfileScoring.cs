using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>Shared performance score computation for commercial profiles.</summary>
internal static class CommercialProfileScoring
{
    /// <summary>
    /// Scoring algorithm (mirrors the Angular service logic).
    /// Max contribution per dimension:
    ///   Activities  : 20 pts (benchmark = 40 / month)
    ///   Meetings    : 20 pts (benchmark = 15 / month)
    ///   Leads       : 15 pts (benchmark = 30 leads)
    ///   Deals       : 25 pts (benchmark = 8 won)
    ///   Attendance  : 15 pts (= attendance% × 0.15)
    ///   Penalties   : -5 per penalty (min -15)
    /// Total clipped to [0, 100].
    /// </summary>
    public static void ComputeAndApplyScore(CommercialProfile p)
    {
        const double maxAct = 40, maxMeet = 15, maxLead = 30, maxDeal = 8;

        double act = Math.Min(20, (p.KpiActivitiesCreated / maxAct) * 20);
        double meet = Math.Min(20, (p.KpiMeetingsParticipated / maxMeet) * 20);
        double lead = Math.Min(15, (p.KpiLeadsAssigned / maxLead) * 15);
        double deal = Math.Min(25, (p.KpiDealsWon / maxDeal) * 25);
        double att = (p.AttendancePct / 100.0) * 15;
        double pen = Math.Max(-15, p.KpiPenalties * -5.0);

        int previousTotal = p.ScoreTotal;
        int newTotal = (int)Math.Clamp(Math.Round(act + meet + lead + deal + att + pen), 0, 100);

        p.ScoreActivities = Math.Round(act, 1);
        p.ScoreMeetings = Math.Round(meet, 1);
        p.ScoreLeads = Math.Round(lead, 1);
        p.ScoreDeals = Math.Round(deal, 1);
        p.ScoreAttendance = Math.Round(att, 1);
        p.ScorePenalties = pen;

        p.ScoreTotal = newTotal;
        p.ScoreTier = newTotal >= 80 ? CommercialScoreTiers.Top
            : newTotal >= 65 ? CommercialScoreTiers.Good
            : newTotal >= 50 ? CommercialScoreTiers.Average
            : CommercialScoreTiers.Low;

        int delta = newTotal - previousTotal;
        p.ScoreTrendValue = delta;
        p.ScoreTrend = delta > 2 ? "up"
            : delta < -1 ? "down"
            : "stable";
    }
}
