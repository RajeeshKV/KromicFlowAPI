namespace KromicFlow.Application.Options;

public sealed class PlatformOptions
{
    public string TermsVersion { get; set; } = "2026-07-18";
    public string DefaultPlanCode { get; set; } = "free";
    public string AdminBootstrapKey { get; set; } = string.Empty;
}
