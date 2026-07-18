using KromicFlow.Infrastructure.Services;

namespace KromicFlow.Tests.Application;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_ReturnsTrue_ForMatchingPassword()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("StrongPassword123!");

        Assert.True(hasher.Verify("StrongPassword123!", hash));
        Assert.False(hasher.Verify("WrongPassword123!", hash));
    }
}
