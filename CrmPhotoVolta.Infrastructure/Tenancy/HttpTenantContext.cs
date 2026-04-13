using CrmPhotoVolta.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace CrmPhotoVolta.Infrastructure.Tenancy;

public sealed class HttpTenantContext : ITenantContext
{
    private const string SocietyKey = "tenant.society_id";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpTenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid SocietyId =>
        CurrentSocietyId ?? throw new InvalidOperationException("Society context is required for this request.");

    public Guid? CurrentSocietyId
    {
        get
        {
            var http = _httpContextAccessor.HttpContext;
            if (http?.Items.TryGetValue(SocietyKey, out var value) == true && value is Guid g)
                return g;
            return null;
        }
    }

    public void SetCurrentSociety(Guid societyId)
    {
        var http = _httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No HTTP context.");
        http.Items[SocietyKey] = societyId;
    }
}
