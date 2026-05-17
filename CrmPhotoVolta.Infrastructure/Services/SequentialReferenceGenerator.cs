namespace CrmPhotoVolta.Infrastructure.Services;

/// <summary>Builds next reference like PREFIX-0001 from existing values (handles gaps and soft-deletes).</summary>
internal static class SequentialReferenceGenerator
{
    public static string Next(string prefix, IEnumerable<string> existing)
    {
        var max = 0;
        foreach (var value in existing)
        {
            if (string.IsNullOrEmpty(value) || !value.StartsWith(prefix, StringComparison.Ordinal))
                continue;

            var suffix = value[prefix.Length..];
            if (int.TryParse(suffix, out var seq))
                max = Math.Max(max, seq);
        }

        return $"{prefix}{(max + 1):D4}";
    }
}
