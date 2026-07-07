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

    public CommercialService(AppDbContext db, CoreDbContext core)
    {
        _db = db;
        _core = core;
    }

    // ── Queries ───────────────────────────────────────────────────────────

    public async Task<(IReadOnlyList<CommercialListItemDto> Items, CommercialPageMeta Meta)>
        ListAsync(Guid societyId, Guid actorUserId, CommercialListQuery query, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var q = _db.CommercialProfiles
            .Where(c => c.SocietyId == societyId)
            .Where(c =>
                (c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                || relatedUserIds.Contains(c.UserId));

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim().ToLower();
            var matchingUserIds = await _core.Users.AsNoTracking()
                .Where(u => !u.IsDeleted && (
                    u.FullName.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s) ||
                    (u.Phone != null && u.Phone.ToLower().Contains(s))))
                .Select(u => u.Id)
                .ToListAsync(ct);

            q = q.Where(c =>
                matchingUserIds.Contains(c.UserId) ||
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
            .ThenBy(c => c.EmployeeId)
            .Skip((page - 1) * size)
            .Take(size)
            .ToListAsync(ct);

        var users = await LoadUsersByIdsAsync(profiles.Select(c => c.UserId), ct);
        var items = profiles.ConvertAll(c => ToListDto(c, users.GetValueOrDefault(c.UserId), societyMemberIds));

        return (items, new CommercialPageMeta(page, size, total, pages));
    }

    public async Task<CommercialProfileDto> GetAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var c = await RequireAsync(societyId, relatedUserIds, id, ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        var user = await LoadUserAsync(c.UserId, ct);
        return ToProfileDto(c, user, societyMemberIds);
    }

    public async Task<CommercialStatsDto> GetStatsAsync(Guid societyId, Guid actorUserId, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var all = await _db.CommercialProfiles
            .Where(c => c.SocietyId == societyId)
            .Where(c =>
                (c.CreatedById.HasValue && relatedUserIds.Contains(c.CreatedById.Value))
                || relatedUserIds.Contains(c.UserId))
            .ToListAsync(ct);

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

    // ── Mutations ─────────────────────────────────────────────────────────

    public async Task<CommercialProfileDto> CreateAsync(
        Guid societyId, Guid actorId, CreateCommercialRequest request, CancellationToken ct = default)
    {
        var employeeId = string.IsNullOrWhiteSpace(request.EmployeeId)
            ? await GenerateEmployeeIdAsync(societyId, ct)
            : request.EmployeeId.Trim();

        // ── Step 1: resolve / create the User account ────────────────────────
        Guid resolvedUserId;
        User linkedUser;

        if (request.UserId.HasValue)
        {
            linkedUser = await _core.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId.Value && !u.IsDeleted, ct)
                ?? throw new AppException("USER_NOT_FOUND", "Compte utilisateur introuvable.", 404);
            resolvedUserId = linkedUser.Id;
        }
        else
        {
            var normalizedEmail = request.Email.Trim().ToLowerInvariant();
            var existingUser = await _core.Users
                .FirstOrDefaultAsync(u => u.Email == normalizedEmail && !u.IsDeleted, ct);

            if (existingUser is null)
            {
                linkedUser = new User
                {
                    Id           = Guid.NewGuid(),
                    CreatedAt    = DateTimeOffset.UtcNow,
                    CreatedById  = actorId,
                    Email        = normalizedEmail,
                    FullName     = BuildFullName(request.FirstName, request.LastName),
                    Phone        = request.Phone,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("ChangeMe123!"),
                    IsActive     = true,
                };
                _core.Users.Add(linkedUser);
            }
            else
            {
                linkedUser = existingUser;
            }

            resolvedUserId = linkedUser.Id;
        }

        ApplyUserIdentity(linkedUser, request.FirstName, request.LastName, request.Phone);
        if (!request.UserId.HasValue)
            linkedUser.Email = request.Email.Trim().ToLowerInvariant();
        linkedUser.UpdatedAt = DateTimeOffset.UtcNow;

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
        }

        await _core.SaveChangesAsync(ct);

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
        return ToProfileDto(profile, linkedUser, societyMemberIds);
    }

    public async Task<CommercialProfileDto> UpdateAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateCommercialRequest request, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);

        var user = await _core.Users
            .FirstOrDefaultAsync(u => u.Id == profile.UserId && !u.IsDeleted, ct)
            ?? throw new AppException("USER_NOT_FOUND", "Compte utilisateur introuvable.", 404);

        if (request.FirstName is not null || request.LastName is not null || request.Phone is not null)
        {
            var identity = SplitFullName(user.FullName);
            ApplyUserIdentity(
                user,
                request.FirstName ?? identity.FirstName,
                request.LastName  ?? identity.LastName,
                request.Phone     ?? user.Phone);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            await _core.SaveChangesAsync(ct);
        }

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
        return ToProfileDto(profile, user, societyMemberIds);
    }

    public async Task<CommercialProfileDto> UpdateKpisAndScoreAsync(
        Guid societyId, Guid actorUserId, Guid id, UpdateCommercialKpisRequest kpis, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);

        profile.KpiActivitiesCreated    = kpis.ActivitiesCreated;
        profile.KpiMeetingsParticipated = kpis.MeetingsParticipated;
        profile.KpiLeadsAssigned        = kpis.LeadsAssigned;
        profile.KpiDealsWon             = kpis.DealsWon;
        profile.KpiQuotesGenerated      = kpis.QuotesGenerated;
        profile.KpiRevenueGenerated     = kpis.RevenueGenerated;
        profile.KpiConversionRate       = kpis.ConversionRate;
        profile.KpiPenalties            = kpis.Penalties;

        // Recompute score
        ComputeAndApplyScore(profile);
        profile.ScoredAt  = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var societyMemberIds = await GetSocietyMemberUserIdsAsync(societyId, ct);
        var user = await LoadUserAsync(profile.UserId, ct);
        return ToProfileDto(profile, user, societyMemberIds);
    }

    public async Task DeleteAsync(Guid societyId, Guid actorUserId, Guid id, CancellationToken ct = default)
    {
        var relatedUserIds = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var profile = await RequireAsync(societyId, relatedUserIds, id, ct);
        profile.IsDeleted = true;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    // ── Private helpers ───────────────────────────────────────────────────

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
        HashSet<Guid> relatedUserIds,
        Guid id,
        CancellationToken ct)
    {
        return await _db.CommercialProfiles
            .FirstOrDefaultAsync(
                c => c.SocietyId == societyId
                    && c.Id == id
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

    private async Task<User?> LoadUserAsync(Guid userId, CancellationToken ct) =>
        await _core.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, ct);

    private async Task<IReadOnlyDictionary<Guid, User>> LoadUsersByIdsAsync(
        IEnumerable<Guid> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
            return new Dictionary<Guid, User>();

        return await _core.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && !u.IsDeleted)
            .ToDictionaryAsync(u => u.Id, ct);
    }

    private static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var trimmed = fullName.Trim();
        if (trimmed.Length == 0)
            return ("", "");

        var space = trimmed.IndexOf(' ');
        return space < 0
            ? (trimmed, "")
            : (trimmed[..space], trimmed[(space + 1)..].Trim());
    }

    private static string BuildFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();

    private static void ApplyUserIdentity(User user, string firstName, string lastName, string? phone)
    {
        user.FullName = BuildFullName(firstName, lastName);
        user.Phone    = phone;
    }

    private static CommercialUserIdentity GetIdentity(User? user)
    {
        if (user is null)
            return new CommercialUserIdentity("", "", "", null);

        var (firstName, lastName) = SplitFullName(user.FullName);
        return new CommercialUserIdentity(firstName, lastName, user.Email, user.Phone);
    }

    private sealed record CommercialUserIdentity(
        string FirstName,
        string LastName,
        string Email,
        string? Phone);

    /// <summary>
    /// Scoring algorithm (mirrors the Angular service logic).
    /// Max contribution per dimension:
    ///   Activities  : 20 pts (benchmark = 40 / month)
    ///   Meetings    : 20 pts (benchmark = 15 / month)
    ///   Leads       : 15 pts (benchmark = 30 leads)
    ///   Deals       : 25 pts (benchmark = 8 won)
    ///   Attendance  : 15 pts (= attendance% × 0.15)
    ///   Penalties   : -5 per penalty (min -15)
    /// Total clipped to [0, 100].
    /// </summary>
    private static void ComputeAndApplyScore(CommercialProfile p)
    {
        const double maxAct  = 40, maxMeet = 15, maxLead = 30, maxDeal = 8;

        double act  = Math.Min(20, (p.KpiActivitiesCreated    / maxAct)  * 20);
        double meet = Math.Min(20, (p.KpiMeetingsParticipated / maxMeet) * 20);
        double lead = Math.Min(15, (p.KpiLeadsAssigned        / maxLead) * 15);
        double deal = Math.Min(25, (p.KpiDealsWon             / maxDeal) * 25);
        double att  = (p.AttendancePct / 100.0) * 15;
        double pen  = Math.Max(-15, p.KpiPenalties * -5.0);

        int previousTotal = p.ScoreTotal;
        int newTotal = (int)Math.Clamp(Math.Round(act + meet + lead + deal + att + pen), 0, 100);

        p.ScoreActivities = Math.Round(act,  1);
        p.ScoreMeetings   = Math.Round(meet, 1);
        p.ScoreLeads      = Math.Round(lead, 1);
        p.ScoreDeals      = Math.Round(deal, 1);
        p.ScoreAttendance = Math.Round(att,  1);
        p.ScorePenalties  = pen;

        p.ScoreTotal = newTotal;
        p.ScoreTier  = newTotal >= 80 ? CommercialScoreTiers.Top
                     : newTotal >= 65 ? CommercialScoreTiers.Good
                     : newTotal >= 50 ? CommercialScoreTiers.Average
                     : CommercialScoreTiers.Low;

        int delta = newTotal - previousTotal;
        p.ScoreTrendValue = delta;
        p.ScoreTrend      = delta >  2 ? "up"
                          : delta < -1 ? "down"
                          : "stable";
    }

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

    private static CommercialListItemDto ToListDto(
        CommercialProfile c, User? user, HashSet<Guid> societyMemberIds)
    {
        var identity = GetIdentity(user);
        return new(
            Id:            c.Id,
            UserId:        societyMemberIds.Contains(c.UserId) ? c.UserId : null,
            EmployeeId:    c.EmployeeId,
            FirstName:     identity.FirstName,
            LastName:      identity.LastName,
            Email:         identity.Email,
            Phone:         identity.Phone,
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
    }

    private static CommercialProfileDto ToProfileDto(
        CommercialProfile c, User? user, HashSet<Guid> societyMemberIds)
    {
        var identity = GetIdentity(user);
        return new(
            Id:            c.Id,
            UserId:        societyMemberIds.Contains(c.UserId) ? c.UserId : null,
            EmployeeId:    c.EmployeeId,
            FirstName:     identity.FirstName,
            LastName:      identity.LastName,
            Email:         identity.Email,
            Phone:         identity.Phone,
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
}
