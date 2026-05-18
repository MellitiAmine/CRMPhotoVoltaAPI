using System.Text.Json;
using System.Text.Json.Serialization;
using CrmPhotoVolta.Application;
using CrmPhotoVolta.Application.Common;
using CrmPhotoVolta.Application.Storage;
using CrmPhotoVolta.Infrastructure;
using CrmPhotoVolta.Infrastructure.Data.App;
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
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
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
    c.TagActionsBy(api =>
    {
        var controller = api.ActionDescriptor.RouteValues.TryGetValue("controller", out var value)
            ? value
            : null;
        return controller is null
            ? Array.Empty<string>()
            : new[] { TranslateSwaggerTag(controller) };
    });

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

static string TranslateSwaggerTag(string controller) =>
    controller switch
    {
        "Auth" => "Auth (Authentification partenaire)",
        "PlatformAuth" => "PlatformAuth (Authentification plateforme)",
        "Users" => "Users (Utilisateurs)",
        "Subscriptions" => "Subscriptions (Abonnements partenaire)",
        "PlatformSocieties" => "PlatformSocieties (Societes plateforme)",
        "PlatformSubscriptions" => "PlatformSubscriptions (Abonnements plateforme)",
        "PlatformSubscriptionPlans" => "PlatformSubscriptionPlans (Plans d'abonnement)",
        "Permissions" => "Permissions (Droits)",
        "Roles" => "Roles (Roles)",
        "RolesCatalog" => "RolesCatalog (Catalogue de roles)",
        "Leads" => "Leads (Prospects)",
        "Clients" => "Clients (Clients)",
        "Deals" => "Deals (Affaires commerciales)",
        "Projects" => "Projects (Projets)",
        "Quotes" => "Quotes (Devis)",
        "Items" => "Items (Catalogue articles)",
        "QuoteItems" => "QuoteItems (Lignes de devis)",
        "Pipeline" => "Pipeline (Etapes du pipeline)",
        "Dashboard" => "Dashboard (Tableau de bord)",
        "Installations" => "Installations (Interventions terrain)",
        "Me" => "Me (Espace technicien)",
        "Calendar" => "Calendar (Calendrier)",
        "Notifications" => "Notifications (Notifications)",
        "Documents" => "Documents (Documents)",
        "Reports" => "Reports (Rapports)",
        "Settings" => "Settings (Parametres)",
        _ => controller
    };

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var coreDb = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
    var appDb = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();

    try
    {
        var coreReachable = await coreDb.Database.CanConnectAsync();
        var appReachable = await appDb.Database.CanConnectAsync();
        var platformReachable = await platformDb.Database.CanConnectAsync();

        if (!coreReachable || !appReachable || !platformReachable)
        {
            app.Logger.LogWarning(
                "Database check failed (Core: {CoreReachable}, App: {AppReachable}, Platform: {PlatformReachable}). " +
                "Skipping startup seeding and continuing without crashing.",
                coreReachable,
                appReachable,
                platformReachable);
        }
        else
        {
            await coreDb.Database.MigrateAsync(CancellationToken.None);
            await appDb.Database.MigrateAsync(CancellationToken.None);
            await platformDb.Database.MigrateAsync(CancellationToken.None);
            app.Logger.LogInformation("PostgreSQL migrations applied for Core, App, and Platform schemas.");

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
        app.Logger.LogError(ex, "Database is unavailable at startup. Skipping DB init and continuing.");
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
// Official .NET images set DOTNET_RUNNING_IN_CONTAINER=true; HTTPS redirect breaks plain HTTP in Docker.
if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") != "true")
    app.UseHttpsRedirection();
// }

var fileStorage = app.Configuration.GetSection(FileStorageOptions.SectionName).Get<FileStorageOptions>()
    ?? new FileStorageOptions();
var webRootPath = Path.Combine(app.Environment.ContentRootPath, fileStorage.WebRootPath);
var filesRoot = Path.Combine(webRootPath, "files");
Directory.CreateDirectory(filesRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(filesRoot),
    RequestPath = fileStorage.PublicPathPrefix.TrimEnd('/')
});

app.UseCors();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
