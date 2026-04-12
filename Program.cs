using System.Text.Json;
using System.Text.Json.Serialization;
using CrmPhotoVolta.Application;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Maintenance;
using CrmPhotoVolta.Infrastructure;
using CrmPhotoVoltaApis.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsProduction())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
}

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var details = context.ModelState
                .Where(x => x.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    x => x.Key,
                    x => x.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value" : e.ErrorMessage).ToArray());

            var body = ApiResponse.Fail("VALIDATION_ERROR", "Invalid input data", details);
            return new UnprocessableEntityObjectResult(body);
        };
    });

builder.Services.AddFluentValidationAutoValidation();

// Swagger in all environments (same as Development). Re-enable the guard for Production-only docs if needed.
// if (builder.Environment.IsDevelopment())
// {
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "CRM PhotoVolta API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description =
            "JWT from tenant login POST /api/v1/auth/login (audience CrmPhotoVoltaClients, claim society_id) " +
            "or platform login POST /api/v1/platform/auth/login (audience CrmPhotoVoltaPlatform). " +
            "Use: Authorization: Bearer {token}",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = JwtBearerDefaults.AuthenticationScheme,
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
// }

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    var maintenance = scope.ServiceProvider.GetRequiredService<IDatabaseMaintenanceService>();
    // Migrations: POST /api/v1/maintenance/migrate-and-seed with X-Maintenance-Secret, or dotnet ef database update.
    await maintenance.RunStartupConnectivityAndSeedAsync(logger, CancellationToken.None);
}

app.MapGet("/api/health", () => Results.Json(new { status = "ok" }))
    .ExcludeFromDescription();

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger UI + HTTPS redirect in all environments (same as Development). Comment the guard to match prod to dev.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
// }

var uploadsRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    RequestPath = "/uploads"
});

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
