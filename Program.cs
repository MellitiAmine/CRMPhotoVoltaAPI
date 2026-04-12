using System.Text.Json;
using System.Text.Json.Serialization;
using CrmPhotoVolta.Application;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Infrastructure;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Infrastructure.Data.Platform;
using CrmPhotoVolta.Infrastructure.Seeding;
using CrmPhotoVoltaApis.Middleware;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

    // Migrations are not applied at runtime — run manually per context, e.g.:
    // dotnet ef database update --project CrmPhotoVolta.Infrastructure --context CoreDbContext --startup-project CrmPhotoVoltaApis
    // (repeat for AppDbContext, PlatformDbContext)
    try
    {
        var canConnect = await coreDb.Database.CanConnectAsync(CancellationToken.None);
        if (!canConnect)
        {
            logger.LogWarning(
                "PostgreSQL is not reachable (CanConnectAsync=false). API will start; apply migrations and fix connectivity, then restart.");
        }
        else
        {
            logger.LogInformation("PostgreSQL connectivity check succeeded.");

            await DatabaseSeeder.EnsureSeedAsync(coreDb, CancellationToken.None);

            await PlatformDatabaseSeeder.EnsureSeedAsync(platformDb, CancellationToken.None);

            var platformSeed = scope.ServiceProvider.GetRequiredService<IOptions<PlatformSeedOptions>>().Value;
            if (platformSeed.Enabled)
            {
                await PlatformDatabaseSeeder.EnsureSuperAdminUserAsync(
                    platformDb,
                    platformSeed.PlatformAdminEmail,
                    platformSeed.PlatformAdminPassword,
                    CancellationToken.None);
            }

            await PlatformDemoSeeder.RemoveLegacyTenantUserMatchingPlatformEmailAsync(
                coreDb,
                platformSeed.PlatformAdminEmail,
                CancellationToken.None);

            await PlatformDemoSeeder.SeedAsync(coreDb, platformSeed, CancellationToken.None);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex,
            "Database connectivity check or seeding failed. API will start; ensure DB is reachable and migrations are applied (`dotnet ef database update`), then restart.");
    }
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
