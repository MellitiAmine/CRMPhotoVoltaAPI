using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Application.Crm.Projects;

public sealed class AddTimelineEventRequest
{
    public ProjectTimelineEventType Type { get; init; } = ProjectTimelineEventType.Note;
    public string Description { get; init; } = string.Empty;
}

public interface IProjectTimelineService
{
    Task<IReadOnlyList<ProjectTimelineEventDto>> GetTimelineAsync(
        Guid societyId, Guid projectId, CancellationToken cancellationToken = default);

    Task<ProjectTimelineEventDto> AddEventAsync(
        Guid societyId, Guid projectId, Guid actorUserId,
        AddTimelineEventRequest request, CancellationToken cancellationToken = default);
}
