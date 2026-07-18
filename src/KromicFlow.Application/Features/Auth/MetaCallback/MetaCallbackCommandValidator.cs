using FluentValidation;

namespace KromicFlow.Application.Features.Auth.MetaCallback;

internal sealed class MetaCallbackCommandValidator : AbstractValidator<MetaCallbackCommand>
{
    public MetaCallbackCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty();
        RuleFor(x => x.RedirectUri).NotEmpty();
    }
}
