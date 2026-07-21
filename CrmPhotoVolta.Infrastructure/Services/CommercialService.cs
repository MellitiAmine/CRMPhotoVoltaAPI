using CrmPhotoVolta.Application.Crm.Commercials;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>
/// EF Core–backed implementation of <see cref="ICommercialService"/>.
/// All queries are scoped to the authenticated society via global query filters.
/// </summary>
public sealed class CommercialService : ICommercialService
{
    private readonly AppDbContext _db;
    private readonly CoreDbContext _core;
    private readonly ICommercialKpiSyncService _kpiSync;

    public CommercialService(AppDbContext db, CoreDbContext core, ICommercialKpiSyncService kpiSync)
    {
        _db = db;
        _core = core;
        _kpiSync = kpiSync;
    }

    // ── Queries ───────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<CommercialListItemDto> Items, CommercialPageMeta Meta)>
        ListAsync(Guid societyId, Guid actorUserId, CommercialListQuery query, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);

        var q = _db.CommercialProfiles.Where(c => c.SocietyId == societyId);
        q = ApplyVisibilityFilter(q, viewAll, relatedUserIds);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            q = q.Where(c =>
                c.FirstName.ToLower().Contains(s) ||
                c.LastName.ToLower().Contains(s) ||
                c.Email.ToLower().Contains(s) ||
                c.EmployeeId.ToLower().Contains(s) ||
                c.Position.ToLower().Contains(s) ||
                c.Department.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
            q = q.Where(c => c.Status == query.Status);

        if (!string.IsNullOrWhiteSpace(query.ContractType))
            q = q.Where(c => c.ContractType == query.ContractType);

        if (!string.IsNullOrWhiteSpace(query.Department))
            q = q.Where(c => c.Department == query.Department);

        if (!string.IsNullOrWhiteSpace(query.ScoreTier))
            q = q.Where(c => c.ScoreTier == query.ScoreTier);

        var total   = await q.CountAsync(ct);
        var page    = Math.Max(1, query.Page);
        var size    = Math.Clamp(query.PageSize, 1, 100);
        var pages   = (int)Math.Ceiling(total / (double)size);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        var profiles = await q
            .OrderByDescending(c => c.ScoreTotal)
            .ThenBy(c => c.LastName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = profiles.ConvertAll(c => ToListDto(c, societyMemberIds));

        return (items, new CommercialPageMeta(page, size, total, pages));
    }

    public async Task<CommercialProfileDto> GetAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var c = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        await _kpiSync.SyncForProfileAsync(societyId, id, ct);
        c = await _db.CommercialProfiles.FirstAsync(x => x.SocietyId == societyId && x.Id == id, ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(c, societyMemberIds);
    }

    public async Task<CommercialStatsDto> GetStatsAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);

        var q = _db.CommercialProfiles.Where(c => c.SocietyId == societyId);
        q = ApplyVisibilityFilter(q, viewAll, relatedUserIds);
        var all = await q.ToListAsync(ct);

        if (all.Count == 0)
            return new CommercialStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new CommercialStatsDto(
            TotalCount:       all.Count,
            ActiveCount:      all.Count(c => c.Status == CommercialStatuses.Active),
            OnLeaveCount:     all.Count(c => c.Status == CommercialStatuses.OnLeave),
            TopPerformers:    all.Count(c => c.ScoreTier == CommercialScoreTiers.Top),
            LowPerformers:    all.Count(c => c.ScoreTier == CommercialScoreTiers.Low),
            TotalSalary:      all.Sum(c => c.Salary),
            AvgScore:         all.Average(c => c.ScoreTotal),
            AvgAttendancePct: all.Average(c => c.AttendancePct),
            AvgConversionRate:all.Average(c => c.KpiConversionRate),
            TotalRevenue:     all.Sum(c => c.KpiRevenueGenerated)
        );
    }

    public async Task<CommercialProfileDto> GetMeAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default)
    {
        var profile = await _db.CommercialProfiles
            .FirstOrDefaultAsync(c => c.SocietyId == societyId && c.UserId == actorUserId, ct)
            ?? throw new AppException("NOT_FOUND", "Aucun profil commercial lié à ce compte.", 404);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    // ── Mutations ─────────────────────────────────────────────────────────

    public async Task<CreateCommercialResultDto> CreateAsync(
        Guid societyId, Guid actorId, CreateCommercialRequest request, CancellationToken ct = default)
    {
        var employeeId = string.IsNullOrWhiteSpace(request.EmployeeId)
            ? await GenerateEmployeeIdAsync(societyId, ct)
            : request.EmployeeId.Trim();

        var initialPassword = ResolveInitialPassword(request.Password);

        // ── Step 1: resolve / create the User account ────────────────────────
        Guid resolvedUserId;
        var isNewUser = false;
        if (request.UserId.HasValue)
        {
            resolvedUserId = request.UserId.Value;
        }
        else
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await _core.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, ct);

            if (existingUser is not null)
            {
                resolvedUserId = existingUser.Id;
                if (!string.IsNullOrWhiteSpace(request.Password))
                {
                    existingUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword);
                    existingUser.UpdatedAt = DateTimeOffset.UtcNow;
                }
            }
            else
            {
                isNewUser = true;
                var newUser = new User
                {
                    Id           = Guid.NewGuid(),
                    CreatedAt    = DateTimeOffset.UtcNow,
                    CreatedById  = actorId,
                    Email        = normalizedEmail,
                    FullName     = $"{request.FirstName} {request.LastName}".Trim(),
                    Phone        = request.Phone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(initialPassword),
                    IsActive     = true,
                };
                _core.Users.Add(newUser);
                resolvedUserId = newUser.Id;
            }
        }

        // ── Step 2: ensure UserSociety (Commercial role) ─────────────────────
        var alreadyMember = await _core.UserSocieties
            .AnyAsync(us => us.UserId == resolvedUserId && us.SocietyId == societyId && !us.IsDeleted, ct);

        if (!alreadyMember)
        {
            // Find or auto-create the Commercial role for this society.
            var commercialRole = await _core.Roles
                .Where(r => r.SocietyId == societyId && r.RoleType == RoleType.Commercial && !r.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (commercialRole is null)
            {
                commercialRole = new Role
                {
                    Id        = Guid.NewGuid(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedById = actorId,
                    SocietyId = societyId,
                    Name      = "Commercial",
                    RoleType  = RoleType.Commercial,
                };
                _core.Roles.Add(commercialRole);
            }

            _core.UserSocieties.Add(new UserSociety
            {
                Id        = Guid.NewGuid(),
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedById = actorId,
                UserId    = resolvedUserId,
                SocietyId = societyId,
                RoleId    = commercialRole.Id,
            });

            // Save user-account changes to the core DB before inserting the profile.
            await _core.SaveChangesAsync(ct);
        }

        // ── Step 3: guard against duplicate profile for the same user ────────
        var userAlreadyLinked = await _db.CommercialProfiles
            .AnyAsync(c => c.SocietyId == societyId && c.UserId == resolvedUserId, ct);
        if (userAlreadyLinked)
            throw new AppException("COMMERCIAL_USER_ALREADY_EXISTS", "Ce compte utilisateur est déjà lié à un profil commercial.", 409);

        // ── Step 4: persist the commercial profile ───────────────────────────
        var profile = new CommercialProfile
        {
            SocietyId  = societyId,
            CreatedById = actorId,
            CreatedAt  = DateTimeOffset.UtcNow,
            UserId     = resolvedUserId,
            EmployeeId = employeeId,
            FirstName  = request.FirstName,
            LastName   = request.LastName,
            Email      = request.Email,
            Phone      = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Address    = request.Address,
            City       = request.City,
            Department = request.Department,
            Position   = request.Position,
            ContractType = request.ContractType,
            WorkTime   = request.WorkTime,
            Salary     = request.Salary,
            StartDate  = request.StartDate,
            MonthlyTarget = request.MonthlyTarget,
            Status     = CommercialStatuses.Active,
            EmergencyContactName     = request.EmergencyContactName,
            EmergencyContactPhone    = request.EmergencyContactPhone,
            EmergencyContactRelation = request.EmergencyContactRelation,
            ScoreTotal = 0, ScoreTier = CommercialScoreTiers.Low, ScoreTrend = "stable",
            AttendanceTotalWorkingDays = 22, AttendanceExpectedHours = 160
        };

        _db.CommercialProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        var dto = ToProfileDto(profile, societyMemberIds);
        var passwordToReturn = isNewUser || !string.IsNullOrWhiteSpace(request.Password)
            ? initialPassword
            : null;
        return new CreateCommercialResultDto(dto, passwordToReturn);
    }

    public async Task<CommercialProfileDto> UpdateAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateCommercialRequest request, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        if (request.FirstName  is { } fn) profile.FirstName  = fn;
        if (request.LastName   is { } ln) profile.LastName   = ln;
        if (request.Phone      is { } ph) profile.Phone      = ph;
        if (request.DateOfBirth is { } db) profile.DateOfBirth = db;
        if (request.Address    is { } ad) profile.Address    = ad;
        if (request.City       is { } cy) profile.City       = cy;
        if (request.Department is { } dp) profile.Department = dp;
        if (request.Position   is { } po) profile.Position   = po;
        if (request.ContractType is { } ct2) profile.ContractType = ct2;
        if (request.WorkTime   is { } wt) profile.WorkTime   = wt;
        if (request.Salary     is { } sa) profile.Salary     = sa;
        if (request.Status     is { } st) profile.Status     = st;
        if (request.MonthlyTarget is { } mt) profile.MonthlyTarget = mt;
        if (request.EmergencyContactName     is { } ecn) profile.EmergencyContactName     = ecn;
        if (request.EmergencyContactPhone    is { } ecp) profile.EmergencyContactPhone    = ecp;
        if (request.EmergencyContactRelation is { } ecr) profile.EmergencyContactRelation = ecr;

        profile.UpdatedAt   = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task<CommercialProfileDto> UpdateKpisAndScoreAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateCommercialKpisRequest kpis, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        profile.KpiActivitiesCreated    = kpis.ActivitiesCreated;
        profile.KpiMeetingsParticipated = kpis.MeetingsParticipated;
        profile.KpiLeadsAssigned        = kpis.LeadsAssigned;
        profile.KpiDealsWon             = kpis.DealsWon;
        profile.KpiQuotesGenerated      = kpis.QuotesGenerated;
        profile.KpiRevenueGenerated     = kpis.RevenueGenerated;
        profile.KpiConversionRate       = kpis.ConversionRate;
        profile.KpiPenalties            = kpis.Penalties;

        // Recompute score
        CommercialProfileScoring.ComputeAndApplyScore(profile);
        profile.ScoredAt  = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);
        profile.IsDeleted = true;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<CommercialProfileDto> UpdateAttendanceAsync(
        Guid societyId,
        Guid actorUserId,
        Guid id,
        UpdateCommercialAttendanceRequest request,
        CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        profile.AttendancePresentDays = Math.Max(0, request.PresentDays);
        profile.AttendanceTotalWorkingDays = Math.Max(1, request.TotalWorkingDays);
        profile.AttendanceAbsentDays = Math.Max(0, request.AbsentDays);
        profile.AttendanceLateDays = Math.Max(0, request.LateDays);
        profile.AttendanceHoursWorked = Math.Max(0, request.HoursWorked);
        profile.AttendanceExpectedHours = Math.Max(1, request.ExpectedHours);
        profile.AttendancePct = profile.AttendanceTotalWorkingDays > 0
            ? Math.Round(profile.AttendancePresentDays * 100.0 / profile.AttendanceTotalWorkingDays, 1)
            : 0;

        CommercialProfileScoring.ComputeAndApplyScore(profile);
        profile.ScoredAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task<CommercialProfileDto> SyncKpisAsync(
        Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        await _kpiSync.SyncForProfileAsync(societyId, id, ct);
        var profile = await _db.CommercialProfiles
            .FirstAsync(c => c.SocietyId == societyId && c.Id == id, ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task UpdateAccountAsync(
        Guid societyId,
        Guid actorUserId,
        Guid id,
        UpdateCommercialAccountRequest request,
        CancellationToken ct = default)
    {
        var hasEmail = !string.IsNullOrWhiteSpace(request.Email);
        var hasPassword = !string.IsNullOrWhiteSpace(request.NewPassword);
        if (!hasEmail && !hasPassword)
            throw new AppException("VALIDATION_ERROR", "Email or password is required.", 400);

        if (hasPassword && request.NewPassword!.Trim().Length < 8)
            throw new AppException("VALIDATION_ERROR", "Password must be at least 8 characters.", 400);

        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        var relatedUserIds = viewAll
            ? null
            : await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, viewAll, relatedUserIds, id, ct);

        var user = await _core.Users.FirstOrDefaultAsync(u => u.Id == profile.UserId && !u.IsDeleted, ct)
            ?? throw new AppException("NOT_FOUND", "User account not found.", 404);

        if (hasEmail)
        {
            var normalizedEmail = request.Email!.Trim().ToLowerInvariant();
            var emailTaken = await _core.Users.AnyAsync(
                u => u.Email == normalizedEmail && u.Id != user.Id && !u.IsDeleted, ct);
            if (emailTaken)
                throw new AppException("EMAIL_ALREADY_EXISTS", "Cet e-mail est déjà utilisé.", 409);

            user.Email = normalizedEmail;
            user.FullName = $"{profile.FirstName} {profile.LastName}".Trim();
            profile.Email = request.Email!.Trim();
        }

        if (hasPassword)
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword!.Trim());

        user.UpdatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _core.SaveChangesAsync(ct);
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static string ResolveInitialPassword(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var p = requested.Trim();
            if (p.Length < 8)
                throw new AppException("VALIDATION_ERROR", "Password must be at least 8 characters.", 400);
            return p;
        }

        return $"Pv{Guid.NewGuid():N}"[..12];
    }

    private static IQueryable<CommercialProfile> ApplyVisibilityFilter(
        IQueryable<CommercialProfile> q,
        bool viewAll,
        HashSet<Guid>? relatedUserIds)
    {
        if (viewAll || relatedUserIds is null)
            return q;

        return q.Where(c =>
            (c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
            || relatedUserIds.Contains(c.UserId));
    }

    private async Task<bool> CanViewAllCommercialsAsync(Guid societyId, Guid actorUserId, CancellationToken ct)
    {
        var roleTypes = await (
            from us in _core.UserSocieties.AsNoTracking()
            join r in _core.Roles.AsNoTracking() on us.RoleId equals r.Id
            where us.SocietyId == societyId
                && us.UserId == actorUserId
                && !us.IsDeleted
                && !r.IsDeleted
            select r.RoleType
        ).ToListAsync(ct);

        return roleTypes.Any(rt => rt is RoleType.Admin or RoleType.Manager);
    }

    private async Task<HashSet<Guid>> GetSocietyMemberUserIdsAsync(Guid societyId, CancellationToken ct)
    {
        var list = await (
            from us in _core.UserSocieties.AsNoTracking()
            join u in _core.Users.AsNoTracking() on us.UserId equals u.Id
            where us.SocietyId == societyId && !us.IsDeleted && !u.IsDeleted
            select us.UserId
        ).ToListAsync(ct);

        return list.ToHashSet();
    }

    private async Task<CommercialProfile> RequireAsync(
        Guid societyId,
        bool viewAll,
        HashSet<Guid>? relatedUserIds,
        Guid id,
        CancellationToken ct)
    {
        if (viewAll)
        {
            return await _db.CommercialProfiles
                .FirstOrDefaultAsync(c => c.SocietyId == societyId && c.Id == id, ct)
                ?? throw new AppException("NOT_FOUND", $"Commercial profile {id} not found.", 404);
        }

        return await _db.CommercialProfiles
            .FirstOrDefaultAsync(
                c => c.SocietyId == societyId
                    && c.Id == id
                    && relatedUserIds != null
                    && ((c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                        || relatedUserIds.Contains(c.UserId)), ct)
            ?? throw new AppException("NOT_FOUND", $"Commercial profile {id} not found.", 404);
    }

    private async Task<HashSet<Guid>> GetRelatedUserIdsAsync(Guid societyId, Guid actorUserId, CancellationToken ct)
    {
        var members = await _core.UserSocieties
            .Where(x => x.SocietyId == societyId && !x.IsDeleted)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(ct);
        var memberSet = members.ToHashSet();

        var related = new HashSet<Guid> { actorUserId };
        if (!memberSet.Contains(actorUserId))
            return related;

        var actorCreatorId = await _core.Users
            .Where(u => u.Id == actorUserId && !u.IsDeleted)
            .Select(u => u.CreatedById)
            .FirstOrDefaultAsync(ct);

        if (actorCreatorId.HasValue && memberSet.Contains(actorCreatorId.Value))
            related.Add(actorCreatorId.Value);

        var creatorCandidates = new List<Guid> { actorUserId };
        if (actorCreatorId.HasValue)
            creatorCandidates.Add(actorCreatorId.Value);

        var subordinateIds = await _core.Users
            .Where(u => !u.IsDeleted && u.CreatedById.HasValue && creatorCandidates.Contains(u.CreatedById.Value))
            .Select(u => u.Id)
            .ToListAsync(ct);

        foreach (var uid in subordinateIds)
        {
            if (memberSet.Contains(uid))
                related.Add(uid);
        }

        return related;
    }

    private async Task<string> GenerateEmployeeIdAsync(Guid societyId, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"EMP-{year}-";

        var lastForYear = await _db.CommercialProfiles
            .Where(c => c.SocietyId == societyId && c.EmployeeId.StartsWith(prefix))
            .OrderByDescending(c => c.EmployeeId)
            .Select(c => c.EmployeeId)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastForYear))
        {
            var lastPart = lastForYear.Split('-').LastOrDefault();
            if (int.TryParse(lastPart, out var n))
            {
                next = n + 1;
            }
        }

        return $"{prefix}{next:000}";
    }

    /// <summary>
    /// Scoring algorithm — see <see cref="CommercialProfileScoring"/>.
    /// </summary>
    private static void ComputeAndApplyScore(CommercialProfile p) =>
        CommercialProfileScoring.ComputeAndApplyScore(p);

    // ── Projection helpers ────────────────────────────────────────────────

    private static CommercialScoreDto BuildScore(CommercialProfile c) => new(
        Total:      c.ScoreTotal,
        Tier:       c.ScoreTier,
        Trend:      c.ScoreTrend,
        TrendValue: c.ScoreTrendValue,
        Breakdown:  new(c.ScoreActivities, c.ScoreMeetings, c.ScoreLeads,
                        c.ScoreDeals, c.ScoreAttendance, c.ScorePenalties)
    );

    private static CommercialKpisDto BuildKpis(CommercialProfile c) => new(
        ActivitiesCreated:    c.KpiActivitiesCreated,
        MeetingsParticipated: c.KpiMeetingsParticipated,
        LeadsAssigned:        c.KpiLeadsAssigned,
        DealsWon:             c.KpiDealsWon,
        QuotesGenerated:      c.KpiQuotesGenerated,
        RevenueGenerated:     c.KpiRevenueGenerated,
        ConversionRate:       c.KpiConversionRate,
        Penalties:            c.KpiPenalties
    );

    private static CommercialAttendanceDto BuildAttendance(CommercialProfile c) => new(
        PresentDays:       c.AttendancePresentDays,
        TotalWorkingDays:  c.AttendanceTotalWorkingDays,
        AbsentDays:        c.AttendanceAbsentDays,
        LateDays:          c.AttendanceLateDays,
        HoursWorked:       c.AttendanceHoursWorked,
        ExpectedHours:     c.AttendanceExpectedHours,
        AttendancePct:     c.AttendancePct
    );

    private static CommercialListItemDto ToListDto(CommercialProfile c, HashSet<Guid> societyMemberIds) => new(
        Id:            c.Id,
        UserId:        c.UserId,
        EmployeeId:    c.EmployeeId,
        FirstName:     c.FirstName,
        LastName:      c.LastName,
        Email:         c.Email,
        Phone:         c.Phone,
        Department:    c.Department,
        Position:      c.Position,
        ContractType:  c.ContractType,
        WorkTime:      c.WorkTime,
        Status:        c.Status,
        StartDate:     c.StartDate,
        Salary:        c.Salary,
        MonthlyTarget: c.MonthlyTarget,
        Score:         BuildScore(c),
        Kpis:          BuildKpis(c),
        Attendance:    BuildAttendance(c)
    );

    private static CommercialProfileDto ToProfileDto(CommercialProfile c, HashSet<Guid> societyMemberIds) => new(
        Id:            c.Id,
        UserId:        c.UserId,
        EmployeeId:    c.EmployeeId,
        FirstName:     c.FirstName,
        LastName:      c.LastName,
        Email:         c.Email,
        Phone:         c.Phone,
        AvatarUrl:     c.AvatarUrl,
        DateOfBirth:   c.DateOfBirth,
        Address:       c.Address,
        City:          c.City,
        Department:    c.Department,
        Position:      c.Position,
        ContractType:  c.ContractType,
        WorkTime:      c.WorkTime,
        Salary:        c.Salary,
        Status:        c.Status,
        StartDate:     c.StartDate,
        MonthlyTarget: c.MonthlyTarget,
        Score:         BuildScore(c),
        Kpis:          BuildKpis(c),
        Attendance:    BuildAttendance(c),
        EmergencyContact: new(c.EmergencyContactName, c.EmergencyContactPhone, c.EmergencyContactRelation),
        CreatedAt:     c.CreatedAt,
        UpdatedAt:     c.UpdatedAt
    );
}
