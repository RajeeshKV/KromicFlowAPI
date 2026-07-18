using FluentValidation;

namespace KromicFlow.Application.Features.Automations.CreateAutomation;

internal sealed class CreateAutomationCommandValidator : AbstractValidator<CreateAutomationCommand>
{
    public CreateAutomationCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(160);
        RuleFor(x => x.InstagramAccountId).NotEmpty();
        RuleFor(x => x.CooldownSeconds).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PublicReply).MaximumLength(2000);
        RuleFor(x => x.PrivateReply).MaximumLength(2000);
    }
}
