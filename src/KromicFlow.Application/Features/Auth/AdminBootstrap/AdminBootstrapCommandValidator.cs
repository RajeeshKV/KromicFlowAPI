using FluentValidation;

namespace KromicFlow.Application.Features.Auth.AdminBootstrap;

internal sealed class AdminBootstrapCommandValidator : AbstractValidator<AdminBootstrapCommand>
{
    public AdminBootstrapCommandValidator()
    {
        RuleFor(x => x.BootstrapKey).NotEmpty();
        RuleFor(x => x.Username).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(12);
    }
}
