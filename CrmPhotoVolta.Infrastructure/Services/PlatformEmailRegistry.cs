using CrmPhotoVolta.Infrastructure.Data.Platform;
using Microsoft.EntityFrameworkCore;

namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>Platform operators live in <c>platform.PlatformUsers</c>; they must not be mixed into tenant user lists.</summary>
internal static class PlatformEmailRegistry
{
    public static async Task<HashSet<string>> LoadLowercaseEmailsAsync(PlatformDbContext db, CancellationToken cancellationToken)
    {
        var list = await db.PlatformUsers.AsNoTracking()
            .Select(x => x.Email.ToLower())
            .ToListAsync(cancellationToken);
        return list.ToHashSet(StringComparer.Ordinal);
    }

    public static bool Contains(HashSet<string> lowercaseEmails, string email) =>
        lowercaseEmails.Contains(email.Trim().ToLowerInvariant());
}
