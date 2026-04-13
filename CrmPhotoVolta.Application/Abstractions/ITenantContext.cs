namespace CrmPhotoVolta.Application.Abstractions;

public interface ITenantContext
{
    Guid SocietyId { get; }
    Guid? CurrentSocietyId { get; }
    void SetCurrentSociety(Guid societyId);
}
