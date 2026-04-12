using CrmPhotoVolta.Application.Platform.Auth.Dtos;
using FluentValidation;

namespace CrmPhotoVolta.Application.Platform.Auth.Validators;

public sealed class PlatformLoginRequestValidator : AbstractValidator<PlatformLoginRequest>
{
    public PlatformLoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
