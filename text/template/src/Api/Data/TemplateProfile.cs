using System;

namespace template;

/// <summary>
/// Describes a template dialect by its delimiters, whitespace behavior, expression rules, and default escaping behavior.
/// </summary>
public class TemplateProfile
{
    /// <summary>
    /// Stores the logical name used to identify this value.
    /// </summary>
    public string Name;
    /// <summary>
    /// Specifies the output start delimiter recognized by the profile.
    /// </summary>
    public string OutputStartDelimiter;
    /// <summary>
    /// Specifies the output end delimiter recognized by the profile.
    /// </summary>
    public string OutputEndDelimiter;
    /// <summary>
    /// Specifies the statement start delimiter recognized by the profile.
    /// </summary>
    public string StatementStartDelimiter;
    /// <summary>
    /// Specifies the statement end delimiter recognized by the profile.
    /// </summary>
    public string StatementEndDelimiter;
    /// <summary>
    /// Specifies the comment start delimiter recognized by the profile.
    /// </summary>
    public string CommentStartDelimiter;
    /// <summary>
    /// Specifies the comment end delimiter recognized by the profile.
    /// </summary>
    public string CommentEndDelimiter;
    /// <summary>
    /// Specifies the raw block start keyword for this profile.
    /// </summary>
    public string RawBlockStartKeyword;
    /// <summary>
    /// Specifies the raw block end keyword for this profile.
    /// </summary>
    public string RawBlockEndKeyword;
    /// <summary>
    /// Controls whether trim markers on template delimiters remove adjacent whitespace.
    /// </summary>
    public bool SupportsWhitespaceTrim;
    /// <summary>
    /// Specifies the left trim marker for this profile.
    /// </summary>
    public char LeftTrimMarker;
    /// <summary>
    /// Specifies the right trim marker for this profile.
    /// </summary>
    public char RightTrimMarker;
    /// <summary>
    /// Specifies the non-greedy left trim marker for this profile.
    /// </summary>
    public char NonGreedyLeftTrimMarker;
    /// <summary>
    /// Specifies the non-greedy right trim marker for this profile.
    /// </summary>
    public char NonGreedyRightTrimMarker;
    /// <summary>
    /// Removes the whole line around a statement-only tag when the profile supports standalone trimming.
    /// </summary>
    public bool StandaloneStatementTrimming;
    /// <summary>
    /// Controls whether identifiers and registered names use case-sensitive matching.
    /// </summary>
    public bool CaseSensitiveNames;
    /// <summary>
    /// Controls whether operators, calls, collection literals, and other non-path expressions are accepted.
    /// </summary>
    public bool AllowsComplexExpressions;
    /// <summary>
    /// Specifies the default whitespace mode.
    /// </summary>
    public TemplateWhitespaceMode DefaultWhitespaceMode;
    /// <summary>
    /// Specifies the default escaping mode.
    /// </summary>
    public TemplateEscapingMode DefaultEscapingMode;
    /// <summary>
    /// Enables automatic encoding of ordinary output expressions for this profile.
    /// </summary>
    public bool AutoEscape;
    /// <summary>
    /// Specifies the verbatim block start keyword for this profile.
    /// </summary>
    public string VerbatimBlockStartKeyword;
    /// <summary>
    /// Specifies the verbatim block end keyword for this profile.
    /// </summary>
    public string VerbatimBlockEndKeyword;
    /// <summary>
    /// Selects strict or pragmatic behavior when options are created from this profile.
    /// </summary>
    public TemplateCompatibilityMode CompatibilityMode;
    /// <summary>
    /// Selects the binary-operator precedence and association rules used while parsing this profile.
    /// </summary>
    public TemplateExpressionPrecedenceMode ExpressionPrecedence;

    /// <summary>
    /// Initializes a new template profile and validates any supplied values.
    /// </summary>
    public TemplateProfile()
    {
        Name = "";
        OutputStartDelimiter = "";
        OutputEndDelimiter = "";
        StatementStartDelimiter = "";
        StatementEndDelimiter = "";
        CommentStartDelimiter = "";
        CommentEndDelimiter = "";
        RawBlockStartKeyword = "";
        RawBlockEndKeyword = "";
        SupportsWhitespaceTrim = false;
        LeftTrimMarker = '-';
        RightTrimMarker = '-';
        NonGreedyLeftTrimMarker = '~';
        NonGreedyRightTrimMarker = '~';
        StandaloneStatementTrimming = false;
        CaseSensitiveNames = true;
        AllowsComplexExpressions = true;
        DefaultWhitespaceMode = TemplateWhitespaceMode.Preserve;
        DefaultEscapingMode = TemplateEscapingMode.None;
        AutoEscape = false;
        VerbatimBlockStartKeyword = "verbatim";
        VerbatimBlockEndKeyword = "endverbatim";
        CompatibilityMode = TemplateCompatibilityMode.Pragmatic;
        ExpressionPrecedence = TemplateExpressionPrecedenceMode.Standard;
    }

    /// <summary>
    /// Creates an independent copy so later field changes do not affect the original instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal TemplateProfile Clone()
    {
        TemplateProfile clone = new TemplateProfile();
        clone.Name = Name;
        clone.OutputStartDelimiter = OutputStartDelimiter;
        clone.OutputEndDelimiter = OutputEndDelimiter;
        clone.StatementStartDelimiter = StatementStartDelimiter;
        clone.StatementEndDelimiter = StatementEndDelimiter;
        clone.CommentStartDelimiter = CommentStartDelimiter;
        clone.CommentEndDelimiter = CommentEndDelimiter;
        clone.RawBlockStartKeyword = RawBlockStartKeyword;
        clone.RawBlockEndKeyword = RawBlockEndKeyword;
        clone.SupportsWhitespaceTrim = SupportsWhitespaceTrim;
        clone.LeftTrimMarker = LeftTrimMarker;
        clone.RightTrimMarker = RightTrimMarker;
        clone.NonGreedyLeftTrimMarker = NonGreedyLeftTrimMarker;
        clone.NonGreedyRightTrimMarker = NonGreedyRightTrimMarker;
        clone.StandaloneStatementTrimming = StandaloneStatementTrimming;
        clone.CaseSensitiveNames = CaseSensitiveNames;
        clone.AllowsComplexExpressions = AllowsComplexExpressions;
        clone.DefaultWhitespaceMode = DefaultWhitespaceMode;
        clone.DefaultEscapingMode = DefaultEscapingMode;
        clone.AutoEscape = AutoEscape;
        clone.VerbatimBlockStartKeyword = VerbatimBlockStartKeyword;
        clone.VerbatimBlockEndKeyword = VerbatimBlockEndKeyword;
        clone.CompatibilityMode = CompatibilityMode;
        clone.ExpressionPrecedence = ExpressionPrecedence;
        return clone;
    }

    /// <summary>
    /// Creates template options with this dialect's defaults applied.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal TemplateOptions CreateOptions()
    {
        TemplateOptions options = new TemplateOptions();
        ApplyTo(options);
        return options;
    }

    /// <summary>
    /// Copies this configuration into the supplied target after validation.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void ApplyTo(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        Validate();

        options.SyntaxProfile = Name;
        options.WhitespaceMode = DefaultWhitespaceMode;
        options.EscapingMode = DefaultEscapingMode;
        options.AutoEscape = AutoEscape;
        options.CompatibilityMode = CompatibilityMode;
    }

    /// <summary>
    /// Checks the current settings for missing or contradictory values and throws when they cannot be used safely.
    /// </summary>
    internal void Validate()
    {
        RequireText(Name, "Name");
        RequireText(OutputStartDelimiter, "OutputStartDelimiter");
        RequireText(OutputEndDelimiter, "OutputEndDelimiter");
        RequireText(StatementStartDelimiter, "StatementStartDelimiter");
        RequireText(StatementEndDelimiter, "StatementEndDelimiter");
        RequireText(CommentStartDelimiter, "CommentStartDelimiter");
        RequireText(CommentEndDelimiter, "CommentEndDelimiter");
        RequireText(RawBlockStartKeyword, "RawBlockStartKeyword");
        RequireText(RawBlockEndKeyword, "RawBlockEndKeyword");
        RequireText(VerbatimBlockStartKeyword, "VerbatimBlockStartKeyword");
        RequireText(VerbatimBlockEndKeyword, "VerbatimBlockEndKeyword");
        if (CompatibilityMode != TemplateCompatibilityMode.Pragmatic && CompatibilityMode != TemplateCompatibilityMode.Strict)
        {
            throw new ArgumentOutOfRangeException("CompatibilityMode");
        }

        if (ExpressionPrecedence != TemplateExpressionPrecedenceMode.Standard && ExpressionPrecedence != TemplateExpressionPrecedenceMode.LiquidRightToLeft)
        {
            throw new ArgumentOutOfRangeException("ExpressionPrecedence");
        }
    }

    internal void RequireText(string value, string fieldName)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new ArgumentException("Profile field is required.", fieldName);
        }
    }
}
