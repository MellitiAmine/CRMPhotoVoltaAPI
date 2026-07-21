using System.Text.Json;
using CrmPhotoVolta.Application.Crm.Commercials;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Domain.App;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Domain.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

public sealed class CommercialTimeEntryService : ICommercialTimeEntryService
{
    private readonly AppDbContext _db;
    private readonly CoreDbContext _core;

    private static readonly HrRules DefaultRules = new(
        WorkDayStart: new TimeOnly(8, 0),
        WorkDayEnd: new TimeOnly(17, 0),
        ExpectedHoursPerDay: 8,
        LateGraceMinutes: 15);

    public CommercialTimeEntryService(AppDbContext db, CoreDbContext core)
    {
        _db = db;
        _core = core;
    }

    public async Task<CommercialTimeMonthBundleDto> GetMonthAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);
        ValidateMonth(year, month);

        var entries = await QueryEntries(societyId, commercialProfileId, year, month)
            .OrderBy(e => e.WorkDate).ThenBy(e => e.Time)
            .ToListAsync(ct);

        var rules = await LoadHrRulesAsync(societyId, ct);
        var summary = await GetOrComputeSummaryAsync(societyId, commercialProfileId, year, month, entries, ct);
        var lateDates = ComputeLateDates(entries, rules);
        return new CommercialTimeMonthBundleDto(summary, entries.ConvertAll(MapEntry), lateDates);
    }

    public async Task<CommercialTimeEntryDto> CreateAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        CreateCommercialTimeEntryRequest request,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);
        var (workDate, punchType, time) = ParsePunch(request.WorkDate, request.PunchType, request.Time);

        var entry = new CommercialTimeEntry
        {
            SocietyId = societyId,
            CommercialProfileId = commercialProfileId,
            CreatedById = actorUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            WorkDate = workDate,
            PunchType = punchType,
            Time = time,
            Notes = request.Notes?.Trim()
        };

        _db.Set<CommercialTimeEntry>().Add(entry);
        await _db.SaveChangesAsync(ct);

        await RecomputeMonthInternalAsync(societyId, commercialProfileId, workDate.Year, workDate.Month, ct);

        return MapEntry(entry);
    }

    public async Task<CommercialTimeEntryDto> UpdateAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        Guid entryId,
        UpdateCommercialTimeEntryRequest request,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);

        var entry = await _db.Set<CommercialTimeEntry>()
            .FirstOrDefaultAsync(e => e.SocietyId == societyId
                && e.CommercialProfileId == commercialProfileId
                && e.Id == entryId, ct)
            ?? throw new AppException("NOT_FOUND", "Time entry not found.", 404);

        var prevYear = entry.WorkDate.Year;
        var prevMonth = entry.WorkDate.Month;

        if (request.WorkDate is not null || request.PunchType is not null || request.Time is not null)
        {
            var workDateStr = request.WorkDate ?? entry.WorkDate.ToString("yyyy-MM-dd");
            var punchTypeStr = request.PunchType ?? (entry.PunchType == CommercialPunchType.In ? "in" : "out");
            var timeStr = request.Time ?? entry.Time.ToString("HH:mm");
            var (workDate, punchType, time) = ParsePunch(workDateStr, punchTypeStr, timeStr);
            entry.WorkDate = workDate;
            entry.PunchType = punchType;
            entry.Time = time;
        }

        if (request.Notes is not null)
            entry.Notes = request.Notes.Trim();

        entry.UpdatedAt = DateTimeOffset.UtcNow;
        entry.UpdatedById = actorUserId;
        await _db.SaveChangesAsync(ct);

        await RecomputeMonthInternalAsync(societyId, commercialProfileId, entry.WorkDate.Year, entry.WorkDate.Month, ct);
        if (prevYear != entry.WorkDate.Year || prevMonth != entry.WorkDate.Month)
            await RecomputeMonthInternalAsync(societyId, commercialProfileId, prevYear, prevMonth, ct);

        return MapEntry(entry);
    }

    public async Task DeleteAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        Guid entryId,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);

        var entry = await _db.Set<CommercialTimeEntry>()
            .FirstOrDefaultAsync(e => e.SocietyId == societyId
                && e.CommercialProfileId == commercialProfileId
                && e.Id == entryId, ct)
            ?? throw new AppException("NOT_FOUND", "Time entry not found.", 404);

        var year = entry.WorkDate.Year;
        var month = entry.WorkDate.Month;
        entry.IsDeleted = true;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        entry.UpdatedById = actorUserId;
        await _db.SaveChangesAsync(ct);

        await RecomputeMonthInternalAsync(societyId, commercialProfileId, year, month, ct);
    }

    public async Task<CommercialAttendanceMonthDto> RecomputeMonthAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int year,
        int month,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);
        ValidateMonth(year, month);
        return await RecomputeMonthInternalAsync(societyId, commercialProfileId, year, month, ct);
    }

    public async Task<IReadOnlyList<CommercialAttendanceMonthDto>> ListMonthsAsync(
        Guid societyId,
        Guid actorUserId,
        Guid commercialProfileId,
        int? year,
        CancellationToken ct = default)
    {
        await EnsureAccessAsync(societyId, actorUserId, commercialProfileId, ct);

        var q = _db.Set<CommercialAttendanceMonth>().AsNoTracking()
            .Where(m => m.SocietyId == societyId && m.CommercialProfileId == commercialProfileId);

        if (year.HasValue)
            q = q.Where(m => m.Year == year.Value);

        var list = await q.OrderByDescending(m => m.Year).ThenByDescending(m => m.Month).ToListAsync(ct);
        return list.ConvertAll(MapMonth);
    }

    private async Task<CommercialAttendanceMonthDto> RecomputeMonthInternalAsync(
        Guid societyId,
        Guid commercialProfileId,
        int year,
        int month,
        CancellationToken ct)
    {
        var entries = await QueryEntries(societyId, commercialProfileId, year, month).ToListAsync(ct);
        return await GetOrComputeSummaryAsync(societyId, commercialProfileId, year, month, entries, ct, persist: true);
    }

    private async Task<CommercialAttendanceMonthDto> GetOrComputeSummaryAsync(
        Guid societyId,
        Guid commercialProfileId,
        int year,
        int month,
        List<CommercialTimeEntry> entries,
        CancellationToken ct,
        bool persist = false)
    {
        var rules = await LoadHrRulesAsync(societyId, ct);
        var stats = ComputeMonthStats(year, month, entries, rules);

        if (!persist)
        {
            var cached = await _db.Set<CommercialAttendanceMonth>().AsNoTracking()
                .FirstOrDefaultAsync(m => m.SocietyId == societyId
                    && m.CommercialProfileId == commercialProfileId
                    && m.Year == year
                    && m.Month == month, ct);

            return new CommercialAttendanceMonthDto(
                year,
                month,
                stats.PresentDays,
                stats.TotalWorkingDays,
                stats.AbsentDays,
                stats.LateDays,
                stats.HoursWorked,
                stats.ExpectedHours,
                stats.AttendancePct,
                cached?.ComputedAt ?? DateTimeOffset.UtcNow);
        }

        var existing = await _db.Set<CommercialAttendanceMonth>()
            .FirstOrDefaultAsync(m => m.SocietyId == societyId
                && m.CommercialProfileId == commercialProfileId
                && m.Year == year
                && m.Month == month, ct);

        if (existing is null)
        {
            existing = new CommercialAttendanceMonth
            {
                SocietyId = societyId,
                CommercialProfileId = commercialProfileId,
                Year = year,
                Month = month,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.Set<CommercialAttendanceMonth>().Add(existing);
        }

        existing.PresentDays = stats.PresentDays;
        existing.TotalWorkingDays = stats.TotalWorkingDays;
        existing.AbsentDays = stats.AbsentDays;
        existing.LateDays = stats.LateDays;
        existing.HoursWorked = stats.HoursWorked;
        existing.ExpectedHours = stats.ExpectedHours;
        existing.AttendancePct = stats.AttendancePct;
        existing.ComputedAt = DateTimeOffset.UtcNow;
        existing.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        var now = DateTime.UtcNow;
        if (year == now.Year && month == now.Month)
        {
            var profile = await _db.CommercialProfiles
                .FirstOrDefaultAsync(p => p.SocietyId == societyId && p.Id == commercialProfileId, ct);
            if (profile is not null)
            {
                profile.AttendancePresentDays = stats.PresentDays;
                profile.AttendanceTotalWorkingDays = stats.TotalWorkingDays;
                profile.AttendanceAbsentDays = stats.AbsentDays;
                profile.AttendanceLateDays = stats.LateDays;
                profile.AttendanceHoursWorked = stats.HoursWorked;
                profile.AttendanceExpectedHours = stats.ExpectedHours;
                profile.AttendancePct = stats.AttendancePct;
                CommercialProfileScoring.ComputeAndApplyScore(profile);
                profile.UpdatedAt = DateTimeOffset.UtcNow;
                await _db.SaveChangesAsync(ct);
            }
        }

        return MapMonth(existing);
    }

    private static MonthStats ComputeMonthStats(int year, int month, List<CommercialTimeEntry> entries, HrRules rules)
    {
        var workingDays = CountWeekdays(year, month);
        var byDate = entries.GroupBy(e => e.WorkDate).ToDictionary(g => g.Key, g => g.OrderBy(x => x.Time).ToList());

        var presentDays = 0;
        var lateDays = 0;
        double totalHours = 0;

        foreach (var (date, punches) in byDate)
        {
            if (!IsWeekday(date)) continue;
            var dayHours = ComputeDayHoursFromPunches(punches, rules);
            if (dayHours <= 0) continue;

            presentDays++;
            totalHours += dayHours;

            var firstIn = punches.FirstOrDefault(p => p.PunchType == CommercialPunchType.In);
            if (firstIn is not null)
            {
                var lateThreshold = rules.WorkDayStart.AddMinutes(rules.LateGraceMinutes);
                if (firstIn.Time > lateThreshold)
                    lateDays++;
            }
        }

        var absentDays = Math.Max(0, workingDays - presentDays);
        var expectedHours = workingDays * rules.ExpectedHoursPerDay;
        var attendancePct = workingDays > 0
            ? Math.Round(presentDays * 100.0 / workingDays, 1)
            : 0;

        return new MonthStats(presentDays, workingDays, absentDays, lateDays, Math.Round(totalHours, 2), expectedHours, attendancePct);
    }

    private static IReadOnlyList<string> ComputeLateDates(List<CommercialTimeEntry> entries, HrRules rules)
    {
        var lateDates = new List<string>();
        var lateThreshold = rules.WorkDayStart.AddMinutes(rules.LateGraceMinutes);

        var byDate = entries
            .Where(e => e.PunchType == CommercialPunchType.In)
            .GroupBy(e => e.WorkDate);

        foreach (var group in byDate)
        {
            if (!IsWeekday(group.Key)) continue;
            var firstIn = group.OrderBy(e => e.Time).First();
            if (firstIn.Time > lateThreshold)
                lateDates.Add(group.Key.ToString("yyyy-MM-dd"));
        }

        lateDates.Sort();
        return lateDates;
    }

    private static double ComputeDayHoursFromPunches(List<CommercialTimeEntry> punches, HrRules rules)
    {
        var sorted = punches.OrderBy(p => p.Time).ToList();
        double total = 0;
        TimeOnly? openIn = null;

        foreach (var punch in sorted)
        {
            if (punch.PunchType == CommercialPunchType.In)
            {
                openIn = punch.Time;
            }
            else if (punch.PunchType == CommercialPunchType.Out && openIn.HasValue)
            {
                var mins = punch.Time.ToTimeSpan() - openIn.Value.ToTimeSpan();
                if (mins.TotalMinutes > 0)
                    total += mins.TotalMinutes / 60.0;
                openIn = null;
            }
        }

        if (openIn.HasValue)
        {
            var mins = rules.WorkDayEnd.ToTimeSpan() - openIn.Value.ToTimeSpan();
            if (mins.TotalMinutes > 0)
                total += mins.TotalMinutes / 60.0;
        }

        return total;
    }

    private static int CountWeekdays(int year, int month)
    {
        var days = DateTime.DaysInMonth(year, month);
        var count = 0;
        for (var d = 1; d <= days; d++)
        {
            var dow = new DateOnly(year, month, d).DayOfWeek;
            if (dow is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                count++;
        }
        return count;
    }

    private static bool IsWeekday(DateOnly date) =>
        date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

    private IQueryable<CommercialTimeEntry> QueryEntries(Guid societyId, Guid profileId, int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1);
        return _db.Set<CommercialTimeEntry>().AsNoTracking()
            .Where(e => e.SocietyId == societyId
                && e.CommercialProfileId == profileId
                && e.WorkDate >= start
                && e.WorkDate < end);
    }

    private async Task<HrRules> LoadHrRulesAsync(Guid societyId, CancellationToken ct)
    {
        var row = await _db.SocietySettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.SocietyId == societyId, ct);
        if (row is null || string.IsNullOrWhiteSpace(row.DataJson))
            return DefaultRules;

        try
        {
            using var doc = JsonDocument.Parse(row.DataJson);
            if (!doc.RootElement.TryGetProperty("hr", out var hr))
                return DefaultRules;

            var start = hr.TryGetProperty("workDayStart", out var ws) ? ws.GetString() : "08:00";
            var end = hr.TryGetProperty("workDayEnd", out var we) ? we.GetString() : "17:00";
            var expected = hr.TryGetProperty("expectedHoursPerDay", out var eh) ? eh.GetDouble() : 8;
            var grace = hr.TryGetProperty("lateGraceMinutes", out var lg) ? lg.GetInt32() : 15;

            return new HrRules(
                ParseTime(start, DefaultRules.WorkDayStart),
                ParseTime(end, DefaultRules.WorkDayEnd),
                expected > 0 ? expected : 8,
                grace >= 0 ? grace : 15);
        }
        catch
        {
            return DefaultRules;
        }
    }

    private static TimeOnly ParseTime(string? value, TimeOnly fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;
        return TimeOnly.TryParse(value, out var t) ? t : fallback;
    }

    private async Task EnsureAccessAsync(Guid societyId, Guid actorUserId, Guid profileId, CancellationToken ct)
    {
        var viewAll = await CanViewAllCommercialsAsync(societyId, actorUserId, ct);
        if (viewAll)
        {
            var ok = await _db.CommercialProfiles.AnyAsync(
                p => p.SocietyId == societyId && p.Id == profileId, ct);
            if (!ok)
                throw new AppException("NOT_FOUND", "Commercial profile not found.", 404);
            return;
        }

        var related = await GetRelatedUserIdsAsync(societyId, actorUserId, ct);
        var allowed = await _db.CommercialProfiles.AnyAsync(
            p => p.SocietyId == societyId
                && p.Id == profileId
                && ((p.CreatedById.HasValue && related.Contains(p.CreatedById.Value))
                    || related.Contains(p.UserId)), ct);
        if (!allowed)
            throw new AppException("NOT_FOUND", "Commercial profile not found.", 404);
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

    private static void ValidateMonth(int year, int month)
    {
        if (year < 2000 || year > 2100 || month is < 1 or > 12)
            throw new AppException("VALIDATION_ERROR", "Invalid year or month.", 400);
    }

    private static (DateOnly WorkDate, CommercialPunchType PunchType, TimeOnly Time) ParsePunch(
        string workDate, string punchType, string time)
    {
        if (!DateOnly.TryParse(workDate, out var date))
            throw new AppException("VALIDATION_ERROR", "Invalid work date.", 400);

        var pt = punchType.Trim().ToLowerInvariant() switch
        {
            "in" or "entrée" or "entree" or "entry" => CommercialPunchType.In,
            "out" or "sortie" or "exit" => CommercialPunchType.Out,
            _ => throw new AppException("VALIDATION_ERROR", "Punch type must be 'in' or 'out'.", 400)
        };

        if (!TimeOnly.TryParse(time, out var t))
            throw new AppException("VALIDATION_ERROR", "Invalid time.", 400);

        return (date, pt, t);
    }

    private static string PunchTypeToApi(CommercialPunchType type) =>
        type == CommercialPunchType.In ? "in" : "out";

    private static CommercialTimeEntryDto MapEntry(CommercialTimeEntry e) => new(
        e.Id,
        e.CommercialProfileId,
        e.WorkDate.ToString("yyyy-MM-dd"),
        PunchTypeToApi(e.PunchType),
        e.Time.ToString("HH:mm"),
        0,
        e.Notes,
        e.CreatedAt);

    private static CommercialAttendanceMonthDto MapMonth(CommercialAttendanceMonth m) => new(
        m.Year,
        m.Month,
        m.PresentDays,
        m.TotalWorkingDays,
        m.AbsentDays,
        m.LateDays,
        m.HoursWorked,
        m.ExpectedHours,
        m.AttendancePct,
        m.ComputedAt);

    private sealed record HrRules(TimeOnly WorkDayStart, TimeOnly WorkDayEnd, double ExpectedHoursPerDay, int LateGraceMinutes);
    internal sealed record MonthStats(
        int PresentDays,
        int TotalWorkingDays,
        int AbsentDays,
        int LateDays,
        double HoursWorked,
        double ExpectedHours,
        double AttendancePct);
}
