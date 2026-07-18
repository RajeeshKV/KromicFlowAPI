using KromicFlow.Application.DTOs.Auth;
using KromicFlow.Domain.Entities;

namespace KromicFlow.Application.Abstractions;

public interface IJwtTokenService
{
    AuthTokenDto CreateUserToken(User user, Session session);
    AuthTokenDto CreateAdminToken(AdminUser admin, Session session);
}

