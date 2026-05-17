using System.Net;
using System.Text.Json;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;
using CrmPhotoVolta.Infrastructure.Data;

namespace CrmPhotoVoltaApis.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteJsonAsync(
                context,
                ex.StatusCode,
                ApiResponse.Fail(ex.Code, ex.Message, ex.Details));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            var mapped = EfExceptionTranslator.TryMap(ex);
            if (mapped is not null)
            {
                await WriteJsonAsync(
                    context,
                    mapped.StatusCode,
                    ApiResponse.Fail(mapped.Code, mapped.Message, mapped.Details));
                return;
            }

            var message = _environment.IsDevelopment()
                ? ex.Message
                : "Something went wrong";

            object? details = _environment.IsDevelopment()
                ? new { type = ex.GetType().Name, ex.Message, inner = ex.InnerException?.Message }
                : null;

            await WriteJsonAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                ApiResponse.Fail("INTERNAL_SERVER_ERROR", message, details));
        }
    }

    private static async Task WriteJsonAsync(HttpContext context, int statusCode, ApiResponse body)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsync(JsonSerializer.Serialize(body, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        }));
    }
}
