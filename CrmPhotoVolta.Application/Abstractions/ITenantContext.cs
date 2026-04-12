namespace CrmPhotoVolta.Application.Abstractions;

public interface ITenantContext
{
    Guid? CurrentSocietyId { get; }
    void SetCurrentSociety(Guid societyId);
}
