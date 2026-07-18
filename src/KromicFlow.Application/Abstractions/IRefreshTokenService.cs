namespace KromicFlow.Application.Abstractions;

public interface IRefreshTokenService
{
    string CreateToken();
    string Hash(string token);
}
