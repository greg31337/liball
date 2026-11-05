namespace template;

/// <summary>
/// Creates a Jinja-compatible syntax profile and matching options.
/// </summary>
internal static class JinjaProfile
{
    /// <summary>
    /// Creates a profile populated with this dialect's delimiters and runtime defaults.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.Name = "jinja";
        profile.OutputStartDelimiter = "{{";
        profile.OutputEndDelimiter = "}}";
        profile.StatementStartDelimiter = "{%";
        profile.StatementEndDelimiter = "%}";
        profile.CommentStartDelimiter = "{#";
        profile.CommentEndDelimiter = "#}";
        profile.RawBlockStartKeyword = "raw";
        profile.RawBlockEndKeyword = "endraw";
        profile.SupportsWhitespaceTrim = true;
        profile.LeftTrimMarker = '-';
        profile.RightTrimMarker = '-';
        profile.StandaloneStatementTrimming = false;
        profile.CaseSensitiveNames = true;
        profile.AllowsComplexExpressions = true;
        profile.DefaultWhitespaceMode = TemplateWhitespaceMode.Preserve;
        profile.DefaultEscapingMode = TemplateEscapingMode.Html;
        profile.AutoEscape = true;
        return profile;
    }

    /// <summary>
    /// Creates template options with this dialect's defaults applied.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateOptions CreateOptions()
    {
        TemplateProfile profile = Create();
        return profile.CreateOptions();
    }
}

/// <summary>
/// Creates a Django-template-compatible profile with Django-specific conversion semantics.
/// </summary>
internal static class DjangoProfile
{
    /// <summary>
    /// Creates a profile populated with this dialect's delimiters and runtime defaults.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = JinjaProfile.Create();
        profile.Name = "django";
        return profile;
    }

    /// <summary>
    /// Creates template options with this dialect's defaults applied.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateOptions CreateOptions()
    {
        TemplateProfile profile = Create();
        return profile.CreateOptions();
    }
}
