namespace KromicFlow.Domain.Entities;

public sealed class TermsAcceptance : Entity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public string TermsVersion { get; set; } = string.Empty;
    public DateTime AcceptedUtc { get; set; } = DateTime.UtcNow;
    public string? IPAddress { get; set; }
}

