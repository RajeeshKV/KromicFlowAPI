using FluentValidation;
using MediatR;

namespace KromicFlow.Application.Common;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any()) return await next();
        var context = new ValidationContext<TRequest>(request);
        var failures = validators.SelectMany(x => x.Validate(context).Errors).Where(x => x is not null).ToList();
        if (failures.Count > 0) throw new ValidationException(failures);
        return await next();
    }
}
