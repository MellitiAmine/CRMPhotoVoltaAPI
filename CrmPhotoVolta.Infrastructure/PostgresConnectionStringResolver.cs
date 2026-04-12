using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CrmPhotoVolta.Infrastructure;

/// <summary>
/// Resolves PostgreSQL connection string for Render/Supabase (<c>DATABASE_URL</c>) or standard .NET config keys.
/// </summary>
public static class PostgresConnectionStringResolver
{
    public static string Resolve(IConfiguration configuration)
    {
        var databaseUrl = configuration["DATABASE_URL"];
        if (!string.IsNullOrWhiteSpace(databaseUrl))
            return FromDatabaseUrl(databaseUrl);

        var defaultConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(defaultConnection))
            return defaultConnection;

        var legacy = configuration.GetConnectionString("crmPhotoVoltaDatabase");
        if (!string.IsNullOrWhiteSpace(legacy))
            return legacy;

        throw new InvalidOperationException(
            "PostgreSQL is not configured. Set DATABASE_URL, or ConnectionStrings__DefaultConnection, or ConnectionStrings__crmPhotoVoltaDatabase.");
    }

    public static string FromDatabaseUrl(string databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            throw new ArgumentException("DATABASE_URL is empty.", nameof(databaseUrl));

        var uri = new Uri(databaseUrl);
        if (!string.Equals(uri.Scheme, "postgres", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, "postgresql", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"DATABASE_URL must use postgres:// or postgresql:// scheme (got '{uri.Scheme}').");
        }

        var userInfo = string.IsNullOrEmpty(uri.UserInfo)
            ? Array.Empty<string>()
            : uri.UserInfo.Split(':', 2);
        var user = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : "";
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";

        var database = string.IsNullOrEmpty(uri.AbsolutePath) || uri.AbsolutePath == "/"
            ? "postgres"
            : Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/'));

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Username = user,
            Password = password,
            Database = database
        };

        if (!string.IsNullOrEmpty(uri.Query))
        {
            var query = uri.Query.TrimStart('?');
            foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var eq = segment.IndexOf('=');
                var key = Uri.UnescapeDataString(eq >= 0 ? segment[..eq] : segment);
                var value = eq >= 0 ? Uri.UnescapeDataString(segment[(eq + 1)..]) : "";
                if (string.Equals(key, "sslmode", StringComparison.OrdinalIgnoreCase)
                    && Enum.TryParse<SslMode>(value, ignoreCase: true, out var ssl))
                {
                    builder.SslMode = ssl;
                }
            }
        }

        return builder.ConnectionString;
    }
}
