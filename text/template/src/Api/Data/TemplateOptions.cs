using System;
using System.Globalization;

namespace template;

/// <summary>
/// Collects syntax, escaping, strictness, whitespace, culture, caching, and runtime-limit settings used during parsing and rendering.
/// </summary>
public class TemplateOptions
{
    /// <summary>
    /// Selects the native or compatibility profile used to preprocess and interpret template source.
    /// </summary>
    public string SyntaxProfile;
    /// <summary>
    /// Enables all individual strictness switches for rendering.
    /// </summary>
    public bool StrictMode;
    /// <summary>
    /// Causes missing values to fail instead of rendering as empty text.
    /// </summary>
    public bool StrictMissingValues;
    /// <summary>
    /// Causes invalid conversions and incompatible operations to fail instead of returning a missing value.
    /// </summary>
    public bool StrictTypeConversions;
    /// <summary>
    /// Causes calls to unregistered functions, filters, tests, or helpers to fail.
    /// </summary>
    public bool StrictUnknownFunctions;
    /// <summary>
    /// Causes denied member access to fail instead of returning a missing value.
    /// </summary>
    public bool StrictUnsafeMembers;
    /// <summary>
    /// Selects render-time newline and whitespace normalization.
    /// </summary>
    public TemplateWhitespaceMode WhitespaceMode;
    /// <summary>
    /// Removes one final newline sequence from completed rendered output when enabled.
    /// </summary>
    public bool TrimFinalNewline;
    /// <summary>
    /// Prefixes later lines of a multiline value with the indentation preceding its output tag.
    /// </summary>
    public bool AutoIndentMultilineValues;
    /// <summary>
    /// Selects the encoder used for automatically escaped output expressions.
    /// </summary>
    public TemplateEscapingMode EscapingMode;
    /// <summary>
    /// Applies <c>EscapingMode</c> to ordinary output unless a value is marked safe or output is explicitly raw.
    /// </summary>
    public bool AutoEscape;
    /// <summary>
    /// Supplies an optional custom encoder for automatically escaped output; <c>null</c> selects the built-in format encoders.
    /// </summary>
    public ITemplateOutputEncoder? OutputEncoder;
    /// <summary>
    /// Selects pragmatic or strict compatibility behavior for the active syntax profile.
    /// </summary>
    public TemplateCompatibilityMode CompatibilityMode;
    /// <summary>
    /// Selects how conditions interpret missing, null, scalar, and collection values.
    /// </summary>
    public TemplateTruthinessMode TruthinessMode;
    /// <summary>
    /// Supplies number, date, comparison, and formatting conventions during evaluation.
    /// </summary>
    public CultureInfo Culture;
    /// <summary>
    /// Selects the precedence used for unqualified local, current-context, and root-context names.
    /// </summary>
    public TemplateNameLookupOrder NameLookupOrder;
    /// <summary>
    /// Specifies the maximum allowed template length.
    /// </summary>
    public int MaximumTemplateLength;
    /// <summary>
    /// Specifies the maximum allowed output length.
    /// </summary>
    public int MaximumOutputLength;
    /// <summary>
    /// Specifies the maximum allowed include depth.
    /// </summary>
    public int MaximumIncludeDepth;
    /// <summary>
    /// Specifies the maximum allowed loop iterations.
    /// </summary>
    public int MaximumLoopIterations;
    /// <summary>
    /// Specifies the maximum allowed expression depth.
    /// </summary>
    public int MaximumExpressionDepth;
    /// <summary>
    /// Specifies the maximum allowed recursive macro invocation depth.
    /// </summary>
    public int MaximumRecursionDepth;
    /// <summary>
    /// Specifies the maximum allowed template count.
    /// </summary>
    public int MaximumTemplateCount;
    /// <summary>
    /// Specifies the maximum elapsed render time in milliseconds; zero disables the time limit.
    /// </summary>
    public int MaximumRenderMilliseconds;

    /// <summary>
    /// Initializes a new template options and validates any supplied values.
    /// </summary>
    public TemplateOptions()
    {
        SyntaxProfile = "native";
        StrictMode = false;
        StrictMissingValues = false;
        StrictTypeConversions = false;
        StrictUnknownFunctions = false;
        StrictUnsafeMembers = false;
        WhitespaceMode = TemplateWhitespaceMode.Preserve;
        TrimFinalNewline = false;
        AutoIndentMultilineValues = false;
        EscapingMode = TemplateEscapingMode.None;
        AutoEscape = false;
        OutputEncoder = null;
        CompatibilityMode = TemplateCompatibilityMode.Pragmatic;
        TruthinessMode = TemplateTruthinessMode.ProfileDefault;
        Culture = CultureInfo.InvariantCulture;
        NameLookupOrder = TemplateNameLookupOrder.LocalCurrentRoot;
        MaximumTemplateLength = 1048576;
        MaximumOutputLength = 16777216;
        MaximumIncludeDepth = 32;
        MaximumLoopIterations = 100000;
        MaximumExpressionDepth = 64;
        MaximumRecursionDepth = 64;
        MaximumTemplateCount = 1024;
        MaximumRenderMilliseconds = 0;
    }

    /// <summary>
    /// Creates options populated with the same native defaults as the parameterless constructor.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal static TemplateOptions CreateDefault()
    {
        return new TemplateOptions();
    }

    /// <summary>
    /// Creates an independent copy so later field changes do not affect the original instance.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal TemplateOptions Clone()
    {
        TemplateOptions clone = new TemplateOptions();
        clone.SyntaxProfile = SyntaxProfile;
        clone.StrictMode = StrictMode;
        clone.StrictMissingValues = StrictMissingValues;
        clone.StrictTypeConversions = StrictTypeConversions;
        clone.StrictUnknownFunctions = StrictUnknownFunctions;
        clone.StrictUnsafeMembers = StrictUnsafeMembers;
        clone.WhitespaceMode = WhitespaceMode;
        clone.TrimFinalNewline = TrimFinalNewline;
        clone.AutoIndentMultilineValues = AutoIndentMultilineValues;
        clone.EscapingMode = EscapingMode;
        clone.AutoEscape = AutoEscape;
        clone.OutputEncoder = OutputEncoder;
        clone.CompatibilityMode = CompatibilityMode;
        clone.TruthinessMode = TruthinessMode;
        clone.Culture = Culture;
        clone.NameLookupOrder = NameLookupOrder;
        clone.MaximumTemplateLength = MaximumTemplateLength;
        clone.MaximumOutputLength = MaximumOutputLength;
        clone.MaximumIncludeDepth = MaximumIncludeDepth;
        clone.MaximumLoopIterations = MaximumLoopIterations;
        clone.MaximumExpressionDepth = MaximumExpressionDepth;
        clone.MaximumRecursionDepth = MaximumRecursionDepth;
        clone.MaximumTemplateCount = MaximumTemplateCount;
        clone.MaximumRenderMilliseconds = MaximumRenderMilliseconds;
        return clone;
    }

    /// <summary>
    /// Sets every individual strictness switch to the supplied value.
    /// </summary>
    /// <param name="enabled">Whether all strict behaviors should be enabled.</param>
    internal void SetStrictMode(bool enabled)
    {
        StrictMode = enabled;
        StrictMissingValues = enabled;
        StrictTypeConversions = enabled;
        StrictUnknownFunctions = enabled;
        StrictUnsafeMembers = enabled;
    }

    /// <summary>
    /// Checks the current settings for missing or contradictory values and throws when they cannot be used safely.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void Validate()
    {
        if (string.IsNullOrEmpty(SyntaxProfile))
        {
            throw new ArgumentException("Syntax profile is required.", "SyntaxProfile");
        }

        if (Culture == null)
        {
            throw new ArgumentNullException("Culture");
        }

        if (NameLookupOrder != TemplateNameLookupOrder.LocalCurrentRoot && NameLookupOrder != TemplateNameLookupOrder.CurrentLocalRoot && NameLookupOrder != TemplateNameLookupOrder.RootLocalCurrent)
        {
            throw new ArgumentOutOfRangeException("NameLookupOrder");
        }

        RequirePositive(MaximumTemplateLength, "MaximumTemplateLength");
        RequirePositive(MaximumOutputLength, "MaximumOutputLength");
        RequireNonNegative(MaximumIncludeDepth, "MaximumIncludeDepth");
        RequireNonNegative(MaximumLoopIterations, "MaximumLoopIterations");
        RequirePositive(MaximumExpressionDepth, "MaximumExpressionDepth");
        RequirePositive(MaximumRecursionDepth, "MaximumRecursionDepth");
        RequirePositive(MaximumTemplateCount, "MaximumTemplateCount");
        RequireNonNegative(MaximumRenderMilliseconds, "MaximumRenderMilliseconds");
        if (CompatibilityMode != TemplateCompatibilityMode.Pragmatic && CompatibilityMode != TemplateCompatibilityMode.Strict)
        {
            throw new ArgumentOutOfRangeException("CompatibilityMode");
        }
    }

    internal void RequirePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName);
        }
    }

    internal void RequireNonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(fieldName);
        }
    }
}
