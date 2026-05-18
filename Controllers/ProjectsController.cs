using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Crm.Contracts;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Crm.Installations;
using CrmPhotoVolta.Application.Crm.Invoices;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CrmPhotoVoltaApis.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = AuthSchemes.TenantJwt)]
[Route("api/v1/projects")]
public sealed class ProjectsController : TenantCrmControllerBase
{
    private readonly IProjectService _projects;
    private readonly IProjectDetailService _detail;
    private readonly IProjectTimelineService _timeline;
    private readonly IProjectWorkflowService _workflow;
    private readonly IProjectDocumentService _documents;
    private readonly IInvoiceService _invoices;
    private readonly IInstallationWorkflowService _installations;
    private readonly ICurrentUser _currentUser;

    public ProjectsController(
        ITenantContext tenant,
        IProjectService projects,
        IProjectDetailService detail,
        IProjectTimelineService timeline,
        IProjectWorkflowService workflow,
        IProjectDocumentService documents,
        IInvoiceService invoices,
        IInstallationWorkflowService installations,
        ICurrentUser currentUser) : base(tenant)
    {
        _projects = projects;
        _detail = detail;
        _timeline = timeline;
        _workflow = workflow;
        _documents = documents;
        _invoices = invoices;
        _installations = installations;
        _currentUser = currentUser;
    }

    // ── CRUD ────────────────────────────────────────────────────────────────

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] PaginationQuery query,
        [FromQuery] Guid? clientId,
        CancellationToken cancellationToken)
    {
        var (items, meta) = await _projects.ListPagedAsync(RequireSociety(), clientId, query.ToRequest(), cancellationToken);
        return Ok(ApiResponse.OkPaged(items, meta));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var item = await _projects.GetAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpGet("{id:guid}/detail")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDetail(Guid id, CancellationToken cancellationToken)
    {
        var item = await _detail.GetDetailAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(item));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var created = await _projects.CreateAsync(RequireSociety(), request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequest request, CancellationToken cancellationToken)
    {
        var updated = await _projects.UpdateAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _projects.DeleteAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    // ── Existing sub-resources ───────────────────────────────────────────────

    [HttpGet("{id:guid}/overview")]
    public async Task<IActionResult> Overview(Guid id, CancellationToken cancellationToken)
    {
        var data = await _projects.GetOverviewAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpGet("{id:guid}/progress")]
    public async Task<IActionResult> Progress(Guid id, CancellationToken cancellationToken)
    {
        var data = await _projects.GetProgressAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(data));
    }

    [HttpPost("{id:guid}/assign-technician")]
    public async Task<IActionResult> AssignTechnician(Guid id, [FromBody] AssignProjectUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await _projects.AssignTechnicianAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/assign-manager")]
    public async Task<IActionResult> AssignManager(Guid id, [FromBody] AssignProjectUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await _projects.AssignManagerAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPost("{id:guid}/assign-commercial")]
    public async Task<IActionResult> AssignCommercial(Guid id, [FromBody] AssignProjectUserRequest request, CancellationToken cancellationToken)
    {
        var updated = await _projects.AssignCommercialAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    [HttpPatch("{id:guid}/progress")]
    public async Task<IActionResult> PatchProgress(Guid id, [FromBody] PatchProjectProgressRequest request, CancellationToken cancellationToken)
    {
        var updated = await _projects.UpdateProgressAsync(RequireSociety(), id, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── Workflow ─────────────────────────────────────────────────────────────

    [HttpPost("{id:guid}/change-status")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeProjectStatusRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var updated = await _workflow.ChangeStatusAsync(RequireSociety(), id, actorId, request, cancellationToken);
        return Ok(ApiResponse.Ok(updated));
    }

    // ── Timeline ─────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        var events = await _timeline.GetTimelineAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(events));
    }

    [HttpPost("{id:guid}/timeline")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddTimelineEvent(Guid id, [FromBody] AddTimelineEventRequest request, CancellationToken cancellationToken)
    {
        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        var ev = await _timeline.AddEventAsync(RequireSociety(), id, actorId, request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(ev));
    }

    // ── Documents ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/documents")]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken cancellationToken)
    {
        var docs = await _documents.ListAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(docs));
    }

    [HttpPost("{id:guid}/documents")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> UploadDocument(
        Guid id,
        [FromForm] ProjectDocumentType type,
        [FromForm] string? name,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return UnprocessableEntity(ApiResponse.Fail("VALIDATION_ERROR", "File is required.", null));

        var actorId = _currentUser.UserId ?? throw new AppException("UNAUTHORIZED", "Unauthorized.", 401);
        await using var stream = file.OpenReadStream();
        var doc = await _documents.UploadAsync(
            RequireSociety(),
            id,
            actorId,
            type,
            name,
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(doc));
    }

    [HttpDelete("{id:guid}/documents/{documentId:guid}")]
    public async Task<IActionResult> DeleteDocument(Guid id, Guid documentId, CancellationToken cancellationToken)
    {
        await _documents.DeleteAsync(RequireSociety(), id, documentId, cancellationToken);
        return Ok(ApiResponse.Ok(new { deleted = true }));
    }

    // ── Invoices (by project) ─────────────────────────────────────────────────

    [HttpGet("{id:guid}/invoices")]
    public async Task<IActionResult> GetInvoices(Guid id, CancellationToken cancellationToken)
    {
        var list = await _invoices.ListByProjectAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }

    [HttpGet("{id:guid}/financial-summary")]
    public async Task<IActionResult> FinancialSummary(Guid id, CancellationToken cancellationToken)
    {
        var summary = await _invoices.GetFinancialSummaryAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(summary));
    }

    // ── Installations ─────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/installations")]
    public async Task<IActionResult> GetInstallations(Guid id, CancellationToken cancellationToken)
    {
        var list = await _installations.ListByProjectAsync(RequireSociety(), id, cancellationToken);
        return Ok(ApiResponse.Ok(list));
    }
}
