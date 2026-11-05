namespace template;

/// <summary>
/// Creates the library's native syntax profile and matching options.
/// </summary>
internal static class NativeProfile
{
    /// <summary>
    /// Creates a profile populated with this dialect's delimiters and runtime defaults.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = new TemplateProfile();
        profile.Name = "native";
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
        profile.StandaloneStatementTrimming = true;
        profile.CaseSensitiveNames = true;
        profile.AllowsComplexExpressions = true;
        profile.DefaultWhitespaceMode = TemplateWhitespaceMode.Preserve;
        profile.DefaultEscapingMode = TemplateEscapingMode.None;
        profile.AutoEscape = false;
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
