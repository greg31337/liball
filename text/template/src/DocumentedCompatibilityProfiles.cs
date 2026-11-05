namespace template;

/// <summary>
/// Creates a Scriban documented-compatibility profile.
/// </summary>
internal static class ScribanProfile
{
    /// <summary>
    /// Creates the profile.
    /// </summary>
    /// <returns>The resulting profile.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.Name = "scriban";
        profile.OutputStartDelimiter = "{{";
        profile.OutputEndDelimiter = "}}";
        profile.StatementStartDelimiter = "{{";
        profile.StatementEndDelimiter = "}}";
        profile.CommentStartDelimiter = "{{##";
        profile.CommentEndDelimiter = "##}}";
        profile.StandaloneStatementTrimming = false;
        return profile;
    }
}

/// <summary>
/// Creates a Nunjucks documented-compatibility profile.
/// </summary>
internal static class NunjucksProfile
{
    /// <summary>
    /// Creates the profile.
    /// </summary>
    /// <returns>The resulting profile.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = JinjaProfile.Create();
        profile.Name = "nunjucks";
        profile.DefaultEscapingMode = TemplateEscapingMode.None;
        profile.AutoEscape = false;
        return profile;
    }
}

/// <summary>
/// Creates a Go text/template documented-compatibility profile.
/// </summary>
internal static class GoTextTemplateProfile
{
    /// <summary>
    /// Creates the profile.
    /// </summary>
    /// <returns>The resulting profile.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.Name = "go-text-template";
        profile.OutputStartDelimiter = "{{";
        profile.OutputEndDelimiter = "}}";
        profile.StatementStartDelimiter = "{{";
        profile.StatementEndDelimiter = "}}";
        profile.CommentStartDelimiter = "{{/*";
        profile.CommentEndDelimiter = "*/}}";
        profile.StandaloneStatementTrimming = false;
        return profile;
    }
}

/// <summary>
/// Creates a Razor-inspired documented-compatibility profile.
/// </summary>
internal static class RazorInspiredProfile
{
    /// <summary>
    /// Creates the profile.
    /// </summary>
    /// <returns>The resulting profile.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.Name = "razor-inspired";
        profile.OutputStartDelimiter = "@(";
        profile.OutputEndDelimiter = ")";
        profile.StatementStartDelimiter = "@{";
        profile.StatementEndDelimiter = "}";
        profile.CommentStartDelimiter = "@*";
        profile.CommentEndDelimiter = "*@";
        profile.SupportsWhitespaceTrim = false;
        profile.StandaloneStatementTrimming = false;
        profile.DefaultEscapingMode = TemplateEscapingMode.Html;
        profile.AutoEscape = true;
        return profile;
    }
}
