using KromicFlow.Domain.Entities;
using MediatR;

namespace KromicFlow.Application.Features.Admin.Plans;

public sealed record GetPlansQuery : IRequest<List<Plan>>;

