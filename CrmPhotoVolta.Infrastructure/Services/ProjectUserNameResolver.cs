using CrmPhotoVolta.Infrastructure.Data.Core;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

internal static class ProjectUserNameResolver
{
    public static async Task<Dictionary<Guid, string>> LoadNamesAsync(
        CoreDbContext core,
        IEnumerable<Guid?> userIds,
        CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return new Dictionary<Guid, string>();

        return await core.Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, cancellationToken);
    }

    public static string? Resolve(IReadOnlyDictionary<Guid, string> names, Guid? userId) =>
        userId.HasValue && names.TryGetValue(userId.Value, out var n) ? n : null;
}
