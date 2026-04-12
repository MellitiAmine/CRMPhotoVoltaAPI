using System.Text;
using CrmPhotoVolta.Application.Abstractions;
using CrmPhotoVolta.Application.Auth;
using CrmPhotoVolta.Application.Crm.Calendar;
using CrmPhotoVolta.Application.Crm.Clients;
using CrmPhotoVolta.Application.Crm.Dashboard;
using CrmPhotoVolta.Application.Crm.Deals;
using CrmPhotoVolta.Application.Crm.Documents;
using CrmPhotoVolta.Application.Crm.Installations;
using CrmPhotoVolta.Application.Crm.Leads;
using CrmPhotoVolta.Application.Crm.Me;
using CrmPhotoVolta.Application.Crm.Notifications;
using CrmPhotoVolta.Application.Crm.Pipeline;
using CrmPhotoVolta.Application.Crm.Projects;
using CrmPhotoVolta.Application.Crm.Quotes;
using CrmPhotoVolta.Application.Crm.Reports;
using CrmPhotoVolta.Application.Crm.Settings;
using CrmPhotoVolta.Application.Permissions;
using CrmPhotoVolta.Application.Platform;
using CrmPhotoVolta.Application.Platform.Auth;
using CrmPhotoVolta.Application.Platform.Societies;
using CrmPhotoVolta.Application.Platform.Subscriptions;
using CrmPhotoVolta.Application.Roles;
using CrmPhotoVolta.Application.Societies;
using CrmPhotoVolta.Application.Subscriptions;
using CrmPhotoVolta.Application.Users;
using CrmPhotoVolta.Infrastructure.Auth;
using CrmPhotoVolta.Infrastructure.Data.App;
using CrmPhotoVolta.Infrastructure.Data.Core;
using CrmPhotoVolta.Infrastructure.Data.Platform;
using CrmPhotoVolta.Infrastructure.Identity;
using CrmPhotoVolta.Infrastructure.Services;
using CrmPhotoVolta.Infrastructure.Seeding;
using CrmPhotoVolta.Infrastructure.Tenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CrmPhotoVolta.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var coreConnection = PostgresConnectionStringResolver.Resolve(configuration);

        services.AddDbContext<CoreDbContext>(options =>
        {
            options.UseNpgsql(coreConnection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "core"));
        });

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseNpgsql(coreConnection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "app"));
        });

        services.AddDbContext<PlatformDbContext>(options =>
        {
            options.UseNpgsql(coreConnection, npgsql =>
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "platform"));
        });

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<PlatformJwtOptions>(configuration.GetSection(PlatformJwtOptions.SectionName));
        services.Configure<PlatformSeedOptions>(configuration.GetSection(PlatformSeedOptions.SectionName));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPlatformJwtTokenService, PlatformJwtTokenService>();

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, HttpTenantContext>();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();
        services.AddScoped<IPlatformCurrentUser, HttpPlatformCurrentUser>();

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPlatformAuthService, PlatformAuthService>();
        services.AddScoped<ISocietyService, SocietyService>();
        services.AddScoped<IPlatformSocietyService, PlatformSocietyService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPlatformSubscriptionPlanService, PlatformSubscriptionPlanService>();
        services.AddScoped<IPlatformSubscriptionAdminService, PlatformSubscriptionAdminService>();

        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IDealService, DealService>();

        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IQuoteService, QuoteService>();
        services.AddScoped<IPipelineStageService, PipelineStageService>();
        services.AddScoped<IInstallationWorkflowService, InstallationWorkflowService>();
        services.AddScoped<IMeWorkspaceService, MeWorkspaceService>();
        services.AddScoped<ICalendarQueryService, CalendarQueryService>();
        services.AddScoped<INotificationService, CrmNotificationService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<ISocietySettingsService, SocietySettingsService>();

        var tenantJwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
            ?? throw new InvalidOperationException("Jwt configuration section is missing.");

        var platformJwt = configuration.GetSection(PlatformJwtOptions.SectionName).Get<PlatformJwtOptions>()
            ?? throw new InvalidOperationException("PlatformJwt configuration section is missing.");

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = AuthSchemes.TenantJwt;
                options.DefaultChallengeScheme = AuthSchemes.TenantJwt;
            })
            .AddJwtBearer(AuthSchemes.TenantJwt, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = tenantJwt.Issuer,
                    ValidAudience = tenantJwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tenantJwt.SigningKey))
                };
            })
            .AddJwtBearer(AuthSchemes.PlatformJwt, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ValidIssuer = platformJwt.Issuer,
                    ValidAudience = platformJwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(platformJwt.SigningKey))
                };
            });

        services.AddAuthorization();

        return services;
    }
}
