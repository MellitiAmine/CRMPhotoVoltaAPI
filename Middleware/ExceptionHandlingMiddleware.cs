using System.Net;
using System.Text.Json;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Exceptions;

namespace CrmPhotoVoltaApis.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            await WriteJsonAsync(context, ex.StatusCode, ApiResponse.Fail(ex.Code, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteJsonAsync(
                context,
                (int)HttpStatusCode.InternalServerError,
                ApiResponse.Fail("INTERNAL_SERVER_ERROR", "Something went wrong"));
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
