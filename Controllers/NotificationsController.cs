using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/notifications")]
public sealed class NotificationsController : TenantCrmControllerBase
{
    private readonly INotificationService _notifications;
    private readonly ICurrentUser _currentUser;

    public NotificationsController(ITenantContext tenant, INotificationService notifications, ICurrentUser currentUser) : base(tenant)
    {
        _notifications = notifications;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PaginationQuery query, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var (items, meta) = await _notifications.ListPagedAsync(RequireSociety(), userId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpPost("read")]
    public async Task<IActionResult> Read([FromBody] MarkNotificationsReadRequest request, CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await _notifications.MarkReadBatchAsync(RequireSociety(), userId, request, cancellationToken);
        return Ok(ApiResponse.Ok(new { read = true }));
    }
}
