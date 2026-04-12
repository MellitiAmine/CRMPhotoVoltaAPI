using CrmPhotoVolta.Domain.Common;

namespace CrmPhotoVolta.Domain.App;

public abstract class SocietyScopedEntity : EntityBase
{
    public Guid SocietyId { get; set; }
}
