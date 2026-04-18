using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Scoring;

public interface ILeadScoringService
{
    LeadScoreSnapshot Calculate(Lead lead, IReadOnlyList<LeadActivity> activities);
}
