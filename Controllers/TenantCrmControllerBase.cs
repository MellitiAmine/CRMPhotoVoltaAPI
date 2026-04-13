using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

public abstract class TenantCrmControllerBase : ControllerBase
{
    private readonly ITenantContext _tenant;

    protected TenantCrmControllerBase(ITenantContext tenant)
    {
        _tenant = tenant;
    }

    protected Guid RequireSociety()
    {
        try
        {
            return _tenant.SocietyId;
        }
        catch (InvalidOperationException)
        {
            throw new AppException("TENANT_REQUIRED", "Society context is required (JWT claim society_id).", StatusCodes.Status403Forbidden);
        }
    }
}
