namespace KromicFlow.Application.Abstractions;

/// <summary>
/// Service for rendering emails using templates or inline HTML
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Gets the template ID for a given template type
    /// </summary>
    int? GetTemplateId(EmailTemplateType templateType);

    /// <summary>
    /// Renders email subject using the given template parameters
    /// </summary>
    string RenderSubject(EmailTemplateType templateType, Dictionary<string, string> parameters);

    /// <summary>
    /// Renders email HTML body using the given template parameters
    /// </summary>
    string RenderBody(EmailTemplateType templateType, Dictionary<string, string> parameters);

    /// <summary>
    /// Checks if templates are enabled
    /// </summary>
    bool AreTemplatesEnabled();
}
