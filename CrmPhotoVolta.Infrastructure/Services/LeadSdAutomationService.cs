using CrmPhotoVolta.Application.Automation;
using CrmPhotoVolta.Application.Scoring;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class LeadSdAutomationService : ILeadSdAutomationService
{
    private readonly AppDbContext _app;
    private readonly ILeadScoringNotificationSink _notificationSink;

    public LeadSdAutomationService(AppDbContext app, ILeadScoringNotificationSink notificationSink)
    {
        _app = app;
        _notificationSink = notificationSink;
    }

    public async Task ProcessAfterScoringAsync(Lead lead, LeadScoreSnapshot snapshot, CancellationToken cancellationToken = default)
    {
        var action = LeadSdDecisionEngine.ResolveAction(snapshot.Sd);
        var title = LeadSdRecommendationText.BuildTitle(action);
        var body = LeadSdRecommendationText.BuildBody(lead, snapshot.Sd, action);
        var phone = string.IsNullOrWhiteSpace(lead.Phone) ? string.Empty : lead.Phone.Trim();

        var row = new WhatsAppRecommendation
        {
            SocietyId = lead.SocietyId,
            LeadId = lead.Id,
            PhoneNumber = phone,
            Message = body,
            ActionType = action,
            Sd = snapshot.Sd,
            Priority = snapshot.Priority,
            Temperature = snapshot.Temperature,
            IsSent = false,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _app.WhatsAppRecommendations.Add(row);
        await _app.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _notificationSink
            .RouteCommercialOrAdminAsync(lead.SocietyId, lead.AssignedToUserId, title, body, cancellationToken)
            .ConfigureAwait(false);
    }
}
