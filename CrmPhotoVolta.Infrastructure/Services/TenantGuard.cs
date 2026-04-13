using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;

namespace CrmPhotoVolta.Infrastructure.Services;

internal static class TenantGuard
{
    public static void EnsureSameTenant(SocietyScopedEntity entity, Guid societyId)
    {
        if (entity.SocietyId != societyId)
            throw new AppException("FORBIDDEN", "Cross-tenant access is forbidden.", 403);
    }
}

