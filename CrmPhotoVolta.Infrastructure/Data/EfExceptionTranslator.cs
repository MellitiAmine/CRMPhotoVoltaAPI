using CrmPhotoVolta.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CrmPhotoVolta.Infrastructure.Data;

/// <summary>Maps EF / PostgreSQL errors to <see cref="AppException"/> for API responses.</summary>
public static class EfExceptionTranslator
{
    public static AppException? TryMap(Exception exception)
    {
        var ex = exception;
        while (ex is not null)
        {
            if (ex is PostgresException pg)
                return MapPostgres(pg);

            if (ex is DbUpdateConcurrencyException)
                return new AppException(
                    "CONCURRENCY_CONFLICT",
                    "The record was modified or deleted by another process. Refresh and try again.",
                    409);

            if (ex is DbUpdateException db && db.InnerException is not null)
            {
                ex = db.InnerException;
                continue;
            }

            ex = ex.InnerException;
        }

        return null;
    }

    private static AppException MapPostgres(PostgresException pg) => pg.SqlState switch
    {
        PostgresErrorCodes.UniqueViolation =>
            new AppException(
                "DUPLICATE_ENTRY",
                UniqueViolationMessage(pg),
                409),

        PostgresErrorCodes.ForeignKeyViolation =>
            new AppException(
                "REFERENCE_ERROR",
                "A related record was not found or is invalid.",
                400),

        PostgresErrorCodes.NotNullViolation =>
            new AppException(
                "VALIDATION_ERROR",
                NotNullViolationMessage(pg),
                400),

        PostgresErrorCodes.CheckViolation =>
            new AppException(
                "VALIDATION_ERROR",
                "A value failed a database constraint.",
                400),

        _ =>
            new AppException(
                "DATABASE_ERROR",
                pg.MessageText,
                400)
    };

    private static string UniqueViolationMessage(PostgresException pg)
    {
        if (pg.ConstraintName?.Contains("QuoteNumber", StringComparison.OrdinalIgnoreCase) == true
            || pg.ConstraintName?.Contains("Quotes_SocietyId", StringComparison.OrdinalIgnoreCase) == true)
            return "A quote with this number already exists for this society.";

        if (pg.ConstraintName?.Contains("Invoices_SocietyId_Reference", StringComparison.OrdinalIgnoreCase) == true)
            return "An invoice with this reference already exists.";

        return "A record with the same unique value already exists.";
    }

    private static string NotNullViolationMessage(PostgresException pg)
    {
        var column = pg.ColumnName;
        return string.IsNullOrWhiteSpace(column)
            ? "A required field is missing."
            : $"Required field '{column}' is missing.";
    }
}
