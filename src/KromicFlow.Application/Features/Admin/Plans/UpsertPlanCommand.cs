using KromicFlow.Application.Common;
using KromicFlow.Application.DTOs.Admin;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Plans;

public sealed record UpsertPlanCommand(
    Guid AdminId,
    string Code,
    string Name,
    bool IsActive,
    bool IsDefault,
    int MaxInstagramAccounts,
    int MaxAutomations,
    int MonthlyAutomationRuns,
    int MonthlyEmails,
    int MonthlyPushNotifications,
    int PriceInrPaise = 0,
    string BillingPeriod = "monthly",
    string? RazorpayPlanId = null
) : IRequest<Result<PlanDto>>;
