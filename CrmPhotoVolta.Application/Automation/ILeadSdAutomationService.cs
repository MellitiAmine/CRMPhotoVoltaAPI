using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Automation;

/// <summary>After scoring: decision → persist recommendation → route in-app notification.</summary>
public interface ILeadSdAutomationService
{
    Task ProcessAfterScoringAsync(Lead lead, LeadScoreSnapshot snapshot, CancellationToken cancellationToken = default);
}
