using CrmPhotoVolta.Application.Crm.Techniciens;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Domain.Core;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>
/// EF Core–backed implementation of <see cref="ITechnicienService"/>.
/// </summary>
public sealed class TechnicienService : ITechnicienService
{
    private readonly AppDbContext _db;
    private readonly CoreDbContext _core;

    public TechnicienService(AppDbContext db, CoreDbContext core)
    {
        _db = db;
        _core = core;
    }

    public async Task<(IReadOnlyList<TechnicienListItemDto> Items, TechnicienPageMeta Meta)>
        ListAsync(Guid societyId, Guid actorUserId, TechnicienListQuery query, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var q = _db.TechnicienProfiles
            .Where(c => c.SocietyId == societyId)
            .Where(c =>
                (c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                || relatedUserIds.Contains(c.UserId));

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

        var total = await q.CountAsync(ct);
        var page  = Math.Max(1, query.Page);
        var size  = Math.Clamp(query.PageSize, 1, 100);
        var pages = (int)Math.Ceiling(total / (double)size);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        var profiles = await q
            .OrderByDescending(c => c.ScoreTotal)
            .ThenBy(c => c.LastName)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var items = profiles.ConvertAll(c => ToListDto(c, societyMemberIds));
        return (items, new TechnicienPageMeta(page, size, total, pages));
    }

    public async Task<TechnicienProfileDto> GetAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var c = await RequireAsync(societyId, relatedUserIds, id, ct);
        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(c, societyMemberIds);
    }

    public async Task<TechnicienStatsDto> GetStatsAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var all = await _db.TechnicienProfiles
            .Where(c => c.SocietyId == societyId)
            .Where(c =>
                (c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                || relatedUserIds.Contains(c.UserId))
            .ToListAsync(ct);

        if (all.Count == 0)
            return new TechnicienStatsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        return new TechnicienStatsDto(
            TotalCount:        all.Count,
            ActiveCount:       all.Count(c => c.Status == TechnicienStatuses.Active),
            OnLeaveCount:      all.Count(c => c.Status == TechnicienStatuses.OnLeave),
            TopPerformers:     all.Count(c => c.ScoreTier == TechnicienScoreTiers.Top),
            LowPerformers:     all.Count(c => c.ScoreTier == TechnicienScoreTiers.Low),
            TotalSalary:       all.Sum(c => c.Salary),
            AvgScore:          all.Average(c => c.ScoreTotal),
            AvgAttendancePct:  all.Average(c => c.AttendancePct),
            AvgOnTimeRate:     all.Average(c => c.KpiOnTimeRate),
            TotalHoursOnSite:  all.Sum(c => c.KpiHoursOnSite)
        );
    }

    public async Task<TechnicienProfileDto> CreateAsync(
        Guid societyId, Guid actorId, CreateTechnicienRequest request, CancellationToken ct = default)
    {
        var employeeId = string.IsNullOrWhiteSpace(request.EmployeeId)
            ? await GenerateEmployeeIdAsync(societyId, ct)
            : request.EmployeeId.Trim();

        Guid resolvedUserId;
        if (request.UserId.HasValue)
        {
            resolvedUserId = request.UserId.Value;
        }
        else
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUserId = await _core.Users
                .Where(u => u.Email == normalizedEmail && !u.IsDeleted)
                .Select(u => (Guid?)u.Id)
                .FirstOrDefaultAsync(ct);

            if (existingUserId.HasValue)
            {
                resolvedUserId = existingUserId.Value;
            }
            else
            {
                var newUser = new User
                {
                    Id           = Guid.NewGuid(),
                    CreatedAt    = DateTimeOffset.UtcNow,
                    CreatedById  = actorId,
                    Email        = normalizedEmail,
                    FullName     = $"{request.FirstName} {request.LastName}".Trim(),
                    Phone        = request.Phone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                    IsActive     = true,
                };
                _core.Users.Add(newUser);
                resolvedUserId = newUser.Id;
            }
        }

        var alreadyMember = await _core.UserSocieties
            .AnyAsync(us => us.UserId == resolvedUserId && us.SocietyId == societyId && !us.IsDeleted, ct);

        if (!alreadyMember)
        {
            var technicienRole = await _core.Roles
                .Where(r => r.SocietyId == societyId && r.RoleType == RoleType.Technicien && !r.IsDeleted)
                .FirstOrDefaultAsync(ct);

            if (technicienRole is null)
            {
                technicienRole = new Role
                {
                    Id          = Guid.NewGuid(),
                    CreatedAt   = DateTimeOffset.UtcNow,
                    CreatedById = actorId,
                    SocietyId   = societyId,
                    Name        = "Technicien",
                    RoleType    = RoleType.Technicien,
                };
                _core.Roles.Add(technicienRole);
            }

            _core.UserSocieties.Add(new UserSociety
            {
                Id          = Guid.NewGuid(),
                CreatedAt   = DateTimeOffset.UtcNow,
                CreatedById = actorId,
                UserId      = resolvedUserId,
                SocietyId   = societyId,
                RoleId      = technicienRole.Id,
            });

            await _core.SaveChangesAsync(ct);
        }

        var userAlreadyLinked = await _db.TechnicienProfiles
            .AnyAsync(c => c.SocietyId == societyId && c.UserId == resolvedUserId, ct);
        if (userAlreadyLinked)
            throw new AppException("TECHNICIEN_USER_ALREADY_EXISTS", "Ce compte utilisateur est déjà lié à un profil technicien.", 409);

        var profile = new TechnicienProfile
        {
            SocietyId   = societyId,
            CreatedById = actorId,
            CreatedAt   = DateTimeOffset.UtcNow,
            UserId      = resolvedUserId,
            EmployeeId  = employeeId,
            FirstName   = request.FirstName,
            LastName    = request.LastName,
            Email       = request.Email,
            Phone       = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Address     = request.Address,
            City        = request.City,
            Department  = request.Department,
            Position    = request.Position,
            ContractType = request.ContractType,
            WorkTime    = request.WorkTime,
            Salary      = request.Salary,
            StartDate   = request.StartDate,
            MonthlyTarget = request.MonthlyTarget,
            Status      = TechnicienStatuses.Active,
            EmergencyContactName     = request.EmergencyContactName,
            EmergencyContactPhone    = request.EmergencyContactPhone,
            EmergencyContactRelation = request.EmergencyContactRelation,
            ScoreTotal = 0, ScoreTier = TechnicienScoreTiers.Low, ScoreTrend = "stable",
            AttendanceTotalWorkingDays = 22, AttendanceExpectedHours = 160
        };

        _db.TechnicienProfiles.Add(profile);
        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task<TechnicienProfileDto> UpdateAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateTechnicienRequest request, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);

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

        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task<TechnicienProfileDto> UpdateKpisAndScoreAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateTechnicienKpisRequest kpis, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);

        profile.KpiInterventionsCompleted = kpis.InterventionsCompleted;
        profile.KpiSiteVisitsCompleted    = kpis.SiteVisitsCompleted;
        profile.KpiProjectsAssigned       = kpis.ProjectsAssigned;
        profile.KpiInstallationsCompleted = kpis.InstallationsCompleted;
        profile.KpiReportsSubmitted       = kpis.ReportsSubmitted;
        profile.KpiHoursOnSite            = kpis.HoursOnSite;
        profile.KpiOnTimeRate             = kpis.OnTimeRate;
        profile.KpiPenalties              = kpis.Penalties;

        ComputeAndApplyScore(profile);
        profile.ScoredAt  = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        return ToProfileDto(profile, societyMemberIds);
    }

    public async Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);
        profile.IsDeleted = true;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
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

    private async Task<TechnicienProfile> RequireAsync(
        Guid societyId,
        HashSet<Guid> relatedUserIds,
        Guid id,
        CancellationToken ct)
    {
        return await _db.TechnicienProfiles
            .FirstOrDefaultAsync(
                c => c.SocietyId == societyId
                    && c.Id == id
                    && ((c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                        || relatedUserIds.Contains(c.UserId)), ct)
            ?? throw new AppException("NOT_FOUND", $"Technicien profile {id} not found.", 404);
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
        var prefix = $"TECH-{year}-";

        var lastForYear = await _db.TechnicienProfiles
            .Where(c => c.SocietyId == societyId && c.EmployeeId.StartsWith(prefix))
            .OrderByDescending(c => c.EmployeeId)
            .Select(c => c.EmployeeId)
            .FirstOrDefaultAsync(ct);

        var next = 1;
        if (!string.IsNullOrWhiteSpace(lastForYear))
        {
            var lastPart = lastForYear.Split('-').LastOrDefault();
            if (int.TryParse(lastPart, out var n))
                next = n + 1;
        }

        return $"{prefix}{next:000}";
    }

    private static void ComputeAndApplyScore(TechnicienProfile p)
    {
        const double maxInt  = 40, maxVisit = 15, maxProj = 30, maxInst = 8;

        double interventions = Math.Min(20, (p.KpiInterventionsCompleted / maxInt)   * 20);
        double siteVisits    = Math.Min(20, (p.KpiSiteVisitsCompleted    / maxVisit) * 20);
        double projects      = Math.Min(15, (p.KpiProjectsAssigned       / maxProj)  * 15);
        double installations = Math.Min(25, (p.KpiInstallationsCompleted / maxInst)  * 25);
        double att           = (p.AttendancePct / 100.0) * 15;
        double pen           = Math.Max(-15, p.KpiPenalties * -5.0);

        int previousTotal = p.ScoreTotal;
        int newTotal = (int)Math.Clamp(Math.Round(interventions + siteVisits + projects + installations + att + pen), 0, 100);

        p.ScoreInterventions = Math.Round(interventions, 1);
        p.ScoreSiteVisits    = Math.Round(siteVisits,    1);
        p.ScoreProjects      = Math.Round(projects,      1);
        p.ScoreInstallations = Math.Round(installations, 1);
        p.ScoreAttendance    = Math.Round(att,           1);
        p.ScorePenalties     = pen;

        p.ScoreTotal = newTotal;
        p.ScoreTier  = newTotal >= 80 ? TechnicienScoreTiers.Top
                     : newTotal >= 65 ? TechnicienScoreTiers.Good
                     : newTotal >= 50 ? TechnicienScoreTiers.Average
                     : TechnicienScoreTiers.Low;

        int delta = newTotal - previousTotal;
        p.ScoreTrendValue = delta;
        p.ScoreTrend      = delta >  2 ? "up"
                          : delta < -1 ? "down"
                          : "stable";
    }

    private static TechnicienScoreDto BuildScore(TechnicienProfile c) => new(
        Total:      c.ScoreTotal,
        Tier:       c.ScoreTier,
        Trend:      c.ScoreTrend,
        TrendValue: c.ScoreTrendValue,
        Breakdown:  new(c.ScoreInterventions, c.ScoreSiteVisits, c.ScoreProjects,
                        c.ScoreInstallations, c.ScoreAttendance, c.ScorePenalties)
    );

    private static TechnicienKpisDto BuildKpis(TechnicienProfile c) => new(
        InterventionsCompleted: c.KpiInterventionsCompleted,
        SiteVisitsCompleted:    c.KpiSiteVisitsCompleted,
        ProjectsAssigned:       c.KpiProjectsAssigned,
        InstallationsCompleted: c.KpiInstallationsCompleted,
        ReportsSubmitted:       c.KpiReportsSubmitted,
        HoursOnSite:            c.KpiHoursOnSite,
        OnTimeRate:             c.KpiOnTimeRate,
        Penalties:              c.KpiPenalties
    );

    private static TechnicienAttendanceDto BuildAttendance(TechnicienProfile c) => new(
        PresentDays:       c.AttendancePresentDays,
        TotalWorkingDays:  c.AttendanceTotalWorkingDays,
        AbsentDays:        c.AttendanceAbsentDays,
        LateDays:          c.AttendanceLateDays,
        HoursWorked:       c.AttendanceHoursWorked,
        ExpectedHours:     c.AttendanceExpectedHours,
        AttendancePct:     c.AttendancePct
    );

    private static TechnicienListItemDto ToListDto(TechnicienProfile c, HashSet<Guid> societyMemberIds) => new(
        Id:            c.Id,
        UserId:        societyMemberIds.Contains(c.UserId) ? c.UserId : null,
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

    private static TechnicienProfileDto ToProfileDto(TechnicienProfile c, HashSet<Guid> societyMemberIds) => new(
        Id:            c.Id,
        UserId:        societyMemberIds.Contains(c.UserId) ? c.UserId : null,
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
