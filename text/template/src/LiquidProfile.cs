namespace template;

/// <summary>
/// Creates a Liquid-compatible syntax profile and matching options.
/// </summary>
internal static class LiquidProfile
{
    /// <summary>
    /// Creates a profile populated with this dialect's delimiters and runtime defaults.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public static TemplateProfile Create()
    {
        TemplateProfile profile = NativeProfile.Create();
        profile.Name = "liquid";
        profile.OutputStartDelimiter = "{{";
        profile.OutputEndDelimiter = "}}";
        profile.StatementStartDelimiter = "{%";
        profile.StatementEndDelimiter = "%}";
        profile.CommentStartDelimiter = "{% comment";
        profile.CommentEndDelimiter = "endcomment %}";
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
        profile.ExpressionPrecedence = TemplateExpressionPrecedenceMode.LiquidRightToLeft;
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
