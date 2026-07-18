using KromicFlow.Application.Common;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Restrictions;

public sealed record SetUserRestrictionCommand(Guid AdminId, Guid UserId, bool LoginBlocked, bool AutomationBlocked, bool NotificationBlocked, string? Reason) : IRequest<Result>;
