using CrmPhotoVolta.Application.Auth.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CrmPhotoVolta.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(LoginRequestValidator).Assembly);
        return services;
    }
}
