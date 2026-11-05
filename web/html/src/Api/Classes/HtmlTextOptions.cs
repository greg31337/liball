using System;
using System.Collections.Generic;

namespace html;
/// <summary>Configures readable plain-text conversion, formatting choices, selector rules, and resource limits.</summary>
public class HtmlTextOptions : IHtmlTextOptions, IHtmlTextOptionsApi
{
    internal HtmlTextLinkStyle linkStyle;
    internal HtmlTextImageStyle imageStyle;
    internal HtmlTextTableStyle tableStyle;
    internal HtmlTextWhitespaceMode whitespaceMode;
    internal HtmlTextNonBreakingSpaceMode nonBreakingSpaceMode;
    internal int wrapColumn;
    internal string truncationMarker;
    internal string newLine;
    internal bool trim;
    internal int maximumBlankLines;
    internal int lineBoundaryBreaks;
    internal int blockBoundaryBreaks;
    internal string[] unorderedListBullets;
    internal int maximumListDepth;
    internal bool includeFormControls;
    internal bool includeHiddenContent;
    internal string baseUrl;
    internal List<HtmlTextRule> rules;
    internal HtmlLimits limits;
    /// <summary>Creates plain-text conversion options initialized to the library defaults.</summary>
    public HtmlTextOptions()
    {
        this.linkStyle = HtmlTextLinkStyle.TextOnly;
        this.imageStyle = HtmlTextImageStyle.AltText;
        this.tableStyle = HtmlTextTableStyle.ReadingOrder;
        this.whitespaceMode = HtmlTextWhitespaceMode.Collapse;
        this.nonBreakingSpaceMode = HtmlTextNonBreakingSpaceMode.Preserve;
        this.wrapColumn = 0;
        this.truncationMarker = "\u2026";
        this.newLine = "\n";
        this.trim = true;
        this.maximumBlankLines = 1;
        this.lineBoundaryBreaks = 1;
        this.blockBoundaryBreaks = 2;
        this.unorderedListBullets = new string[]
        {
            "-",
            "*",
            "•"
        };
        this.maximumListDepth = 16;
        this.includeFormControls = false;
        this.includeHiddenContent = false;
        this.baseUrl = "";
        this.rules = new List<HtmlTextRule>();
        this.limits = HtmlLimits.CreateDefault();
        this.AddDefaultExclusions();
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <returns>A new independently mutable text-options object initialized with the library defaults.</returns>
    internal static HtmlTextOptions CreateDefault()
    {
        return new HtmlTextOptions();
    }

    /// <summary>Creates plain-text settings suitable for search indexing.</summary>
    /// <returns>A new text-options object configured for compact, normalized search-index text.</returns>
    internal static HtmlTextOptions CreateSearchIndex()
    {
        HtmlTextOptions options = new HtmlTextOptions();
        options.imageStyle = HtmlTextImageStyle.Omit;
        options.tableStyle = HtmlTextTableStyle.ReadingOrder;
        options.AddExcludedSelector("nav, aside, form");
        return options;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent text-options object containing copies of the current settings and rules.</returns>
    public HtmlTextOptions Copy()
    {
        HtmlTextOptions copy = new HtmlTextOptions();
        copy.linkStyle = this.linkStyle;
        copy.imageStyle = this.imageStyle;
        copy.tableStyle = this.tableStyle;
        copy.whitespaceMode = this.whitespaceMode;
        copy.nonBreakingSpaceMode = this.nonBreakingSpaceMode;
        copy.wrapColumn = this.wrapColumn;
        copy.truncationMarker = this.truncationMarker;
        copy.newLine = this.newLine;
        copy.trim = this.trim;
        copy.maximumBlankLines = this.maximumBlankLines;
        copy.lineBoundaryBreaks = this.lineBoundaryBreaks;
        copy.blockBoundaryBreaks = this.blockBoundaryBreaks;
        copy.unorderedListBullets = CopyBullets(this.unorderedListBullets);
        copy.maximumListDepth = this.maximumListDepth;
        copy.includeFormControls = this.includeFormControls;
        copy.includeHiddenContent = this.includeHiddenContent;
        copy.baseUrl = this.baseUrl;
        copy.rules.Clear();
        int index = 0;
        while (index < this.rules.Count)
        {
            copy.rules.Add(this.rules[index].Copy());
            index = index + 1;
        }

        copy.limits = this.limits.Copy();
        return copy;
    }

    internal void Validate()
    {
        ValidateLinkStyle(this.linkStyle);
        ValidateImageStyle(this.imageStyle);
        ValidateTableStyle(this.tableStyle);
        ValidateWhitespaceMode(this.whitespaceMode);
        ValidateNonBreakingSpaceMode(this.nonBreakingSpaceMode);
        if (this.wrapColumn < 0)
        {
            throw new InvalidOperationException("The text wrap column cannot be negative.");
        }

        if (this.truncationMarker == null)
        {
            throw new InvalidOperationException("The text truncation marker cannot be null.");
        }

        if (this.newLine == null || this.newLine.Length == 0)
        {
            throw new InvalidOperationException("The text line ending cannot be empty.");
        }

        if (this.maximumBlankLines < 0)
        {
            throw new InvalidOperationException("The maximum blank-line count cannot be negative.");
        }

        ValidateBoundaryBreaks(this.lineBoundaryBreaks,
            "lineBoundaryBreaks");
        ValidateBoundaryBreaks(this.blockBoundaryBreaks,
            "blockBoundaryBreaks");

        if (this.baseUrl == null)
        {
            throw new InvalidOperationException(
                "The text base URL cannot be null.");
        }

        ValidateBullets(this.unorderedListBullets);
        if (this.maximumListDepth < 1)
        {
            throw new InvalidOperationException("The maximum text list depth must be positive.");
        }

        if (this.rules == null || this.limits == null)
        {
            throw new InvalidOperationException("Text options contain a missing required value.");
        }

        this.limits.Validate();
        int index = 0;
        while (index < this.rules.Count)
        {
            if (this.rules[index] == null)
            {
                throw new InvalidOperationException("Text options contain a null conversion rule.");
            }

            index = index + 1;
        }
    }

    /// <summary>Validates the current settings and returns an immutable configuration for repeated use.</summary>
    /// <returns>A validated compiled configuration that can be reused safely.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public HtmlCompiledTextOptions Compile()
    {
        this.Validate();
        HtmlCompiledTextRule[] compiled = new HtmlCompiledTextRule[this.rules.Count];
        int index = 0;
        while (index < this.rules.Count)
        {
            HtmlTextRule rule = this.rules[index];
            HtmlSelectorCompileResult result = HtmlSelectors.Compile(rule.GetSelector(), this.limits);
            if (!result.IsSuccess())
            {
                HtmlSelectorCompileError[] errors = result.GetErrors();
                string message = errors.Length == 0 ? "A text conversion rule selector is invalid." : errors[0].GetMessage();
                throw new ArgumentException(message, "selector");
            }

            compiled[index] = new HtmlCompiledTextRule(rule.GetSelector(), result.GetSelector(), rule.GetAction(), rule.GetReplacement());
            index = index + 1;
        }

        return new HtmlCompiledTextOptions(this.linkStyle, this.imageStyle,
            this.tableStyle, this.whitespaceMode,
            this.nonBreakingSpaceMode, this.wrapColumn,
            this.truncationMarker, this.newLine, this.trim,
            this.maximumBlankLines, this.lineBoundaryBreaks,
            this.blockBoundaryBreaks, this.unorderedListBullets,
            this.maximumListDepth, this.includeFormControls,
            this.includeHiddenContent, this.baseUrl, compiled, this.limits);
    }

    /// <summary>Gets how link text and destinations are represented in plain-text output.</summary>
    /// <returns>The configured link rendering style.</returns>
    public HtmlTextLinkStyle GetLinkStyle()
    {
        return this.linkStyle;
    }

    /// <summary>Sets the link style used by subsequent operations.</summary>
    /// <param name = "value">The link rendering style to validate and use.</param>
    public void SetLinkStyle(HtmlTextLinkStyle value)
    {
        ValidateLinkStyle(value);
        this.linkStyle = value;
    }

    /// <summary>Gets how images contribute alternative text, source URLs, or placeholders.</summary>
    /// <returns>The configured image rendering style.</returns>
    public HtmlTextImageStyle GetImageStyle()
    {
        return this.imageStyle;
    }

    /// <summary>Sets the image style used by subsequent operations.</summary>
    /// <param name = "value">The image rendering style to validate and use.</param>
    public void SetImageStyle(HtmlTextImageStyle value)
    {
        ValidateImageStyle(value);
        this.imageStyle = value;
    }

    /// <summary>Gets whether tables use reading order, a tab-separated grid, an aligned grid, or no output.</summary>
    /// <returns>The configured table rendering style.</returns>
    public HtmlTextTableStyle GetTableStyle()
    {
        return this.tableStyle;
    }

    /// <summary>Sets the table style used by subsequent operations.</summary>
    /// <param name = "value">The table rendering style to validate and use.</param>
    public void SetTableStyle(HtmlTextTableStyle value)
    {
        ValidateTableStyle(value);
        this.tableStyle = value;
    }

    /// <summary>Gets whether ordinary text collapses or preserves source whitespace.</summary>
    /// <returns>The configured ordinary-whitespace mode.</returns>
    public HtmlTextWhitespaceMode GetWhitespaceMode()
    {
        return this.whitespaceMode;
    }

    /// <summary>Sets how ordinary source whitespace is represented.</summary>
    /// <param name = "value">The whitespace mode to validate and use.</param>
    public void SetWhitespaceMode(HtmlTextWhitespaceMode value)
    {
        ValidateWhitespaceMode(value);
        this.whitespaceMode = value;
    }

    /// <summary>Gets how non-breaking spaces are represented.</summary>
    /// <returns>The configured non-breaking-space mode.</returns>
    public HtmlTextNonBreakingSpaceMode GetNonBreakingSpaceMode()
    {
        return this.nonBreakingSpaceMode;
    }

    /// <summary>Sets how non-breaking spaces are represented.</summary>
    /// <param name = "value">The non-breaking-space mode to validate and use.</param>
    public void SetNonBreakingSpaceMode(HtmlTextNonBreakingSpaceMode value)
    {
        ValidateNonBreakingSpaceMode(value);
        this.nonBreakingSpaceMode = value;
    }

    /// <summary>Gets the target column at which ordinary text is wrapped.</summary>
    /// <returns>The nonnegative wrap column; <c>0</c> disables column wrapping.</returns>
    public int GetWrapColumn()
    {
        return this.wrapColumn;
    }

    /// <summary>Sets the wrap column used by subsequent operations.</summary>
    /// <param name = "value">The nonnegative target column, or <c>0</c> to disable wrapping.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetWrapColumn(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The text wrap column cannot be negative.");
        }

        this.wrapColumn = value;
    }

    /// <summary>Gets the text appended when conversion cannot emit the complete result within its output limit.</summary>
    /// <returns>The configured truncation marker; an empty string suppresses a visible marker.</returns>
    public string GetTruncationMarker()
    {
        return this.truncationMarker;
    }

    /// <summary>Sets the truncation marker used by subsequent operations.</summary>
    /// <param name = "value">The marker to copy; an empty string suppresses a visible marker.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTruncationMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.truncationMarker = value;
    }

    /// <summary>Gets the line-ending sequence inserted between plain-text output lines.</summary>
    /// <returns>The configured non-empty line ending.</returns>
    public string GetNewLine()
    {
        return this.newLine;
    }

    /// <summary>Sets the new line used by subsequent operations.</summary>
    /// <param name = "value">The non-empty line-ending sequence to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public void SetNewLine(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("The text line ending cannot be empty.", "value");
        }

        this.newLine = value;
    }

    /// <summary>Reports whether completed output is stripped of leading and trailing whitespace.</summary>
    /// <returns><c>true</c> when leading and trailing output whitespace is removed; otherwise, <c>false</c>.</returns>
    public bool GetTrim()
    {
        return this.trim;
    }

    /// <summary>Controls whether leading and trailing output whitespace is removed.</summary>
    /// <param name = "value"><c>true</c> to remove leading and trailing whitespace from the completed output.</param>
    public void SetTrim(bool value)
    {
        this.trim = value;
    }

    /// <summary>Gets how many consecutive empty lines may remain after whitespace compaction.</summary>
    /// <returns>The nonnegative maximum blank-line count.</returns>
    public int GetMaximumBlankLines()
    {
        return this.maximumBlankLines;
    }

    /// <summary>Sets the maximum blank lines used by subsequent operations.</summary>
    /// <param name = "value">The nonnegative maximum number of consecutive blank lines.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMaximumBlankLines(int value)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException("value", value, "The maximum blank-line count cannot be negative.");
        }

        this.maximumBlankLines = value;
    }

    /// <summary>Gets the number of line endings requested at a line boundary.</summary>
    /// <returns>The nonnegative configured line-boundary count.</returns>
    public int GetLineBoundaryBreaks()
    {
        return this.lineBoundaryBreaks;
    }

    /// <summary>Sets the number of line endings requested at a line boundary.</summary>
    /// <param name = "value">A nonnegative count.</param>
    public void SetLineBoundaryBreaks(int value)
    {
        ValidateBoundaryBreaks(value, "value");
        this.lineBoundaryBreaks = value;
    }

    /// <summary>Gets the number of line endings requested at a block boundary.</summary>
    /// <returns>The nonnegative configured block-boundary count.</returns>
    public int GetBlockBoundaryBreaks()
    {
        return this.blockBoundaryBreaks;
    }

    /// <summary>Sets the number of line endings requested at a block boundary.</summary>
    /// <param name = "value">A nonnegative count.</param>
    public void SetBlockBoundaryBreaks(int value)
    {
        ValidateBoundaryBreaks(value, "value");
        this.blockBoundaryBreaks = value;
    }

    /// <summary>Gets the bullet strings selected by zero-based list nesting depth.</summary>
    /// <returns>A snapshot of the unordered list bullets; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetUnorderedListBullets()
    {
        return CopyBullets(this.unorderedListBullets);
    }

    /// <summary>Sets the unordered list bullets used by subsequent operations.</summary>
    /// <param name = "value">A nonempty array of non-null bullet strings, copied so later caller changes have no effect.</param>
    public void SetUnorderedListBullets(string[] value)
    {
        ValidateBullets(value);
        this.unorderedListBullets = CopyBullets(value);
    }

    /// <summary>Gets the deepest list nesting level that receives structured list formatting.</summary>
    /// <returns>The positive maximum list depth.</returns>
    public int GetMaximumListDepth()
    {
        return this.maximumListDepth;
    }

    /// <summary>Sets the maximum list depth used by subsequent operations.</summary>
    /// <param name = "value">The positive maximum list nesting depth.</param>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when an argument is outside its supported range.</exception>
    public void SetMaximumListDepth(int value)
    {
        if (value < 1)
        {
            throw new ArgumentOutOfRangeException("value", value, "The maximum text list depth must be positive.");
        }

        this.maximumListDepth = value;
    }

    /// <summary>Reports whether form-control values contribute to plain-text output.</summary>
    /// <returns><c>true</c> when form-control values contribute to output; otherwise, <c>false</c>.</returns>
    public bool GetIncludeFormControls()
    {
        return this.includeFormControls;
    }

    /// <summary>Controls whether form-control values contribute to plain-text output.</summary>
    /// <param name = "value"><c>true</c> to include useful values from supported form controls; otherwise, <c>false</c>.</param>
    public void SetIncludeFormControls(bool value)
    {
        this.includeFormControls = value;
    }

    /// <summary>Reports whether hidden and inert element content contributes to output.</summary>
    /// <returns><c>true</c> when hidden and inert content is included; otherwise, <c>false</c>.</returns>
    public bool GetIncludeHiddenContent()
    {
        return this.includeHiddenContent;
    }

    /// <summary>Controls whether hidden and inert element content contributes to output.</summary>
    /// <param name = "value"><c>true</c> to include hidden and inert content.</param>
    public void SetIncludeHiddenContent(bool value)
    {
        this.includeHiddenContent = value;
    }

    /// <summary>Gets the base URL used to resolve links and media in string conversion.</summary>
    /// <returns>The configured base URL, or an empty string when none is configured.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Sets the base URL used to resolve links and media in string conversion.</summary>
    /// <param name = "value">The base URL to copy; an empty string disables explicit base resolution.</param>
    public void SetBaseUrl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.baseUrl = value;
    }

    /// <summary>Adds a copied selector rule to the end of this text-conversion rule list.</summary>
    /// <param name = "value">The rule to copy and append.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddRule(HtmlTextRule value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.rules.Add(value.Copy());
    }

    /// <summary>Adds a rule that includes content matched by the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    public void AddIncludedSelector(string selector)
    {
        this.AddRule(HtmlTextRule.CreateInclude(selector));
    }

    /// <summary>Adds a rule that omits content matched by the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    public void AddExcludedSelector(string selector)
    {
        this.AddRule(HtmlTextRule.CreateExclude(selector));
    }

    /// <summary>Adds a rule that replaces content matched by the selector with fixed text.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    /// <param name = "replacement">The text emitted in place of a matched node.</param>
    public void AddReplacementSelector(string selector, string replacement)
    {
        this.AddRule(HtmlTextRule.CreateReplace(selector, replacement));
    }

    /// <summary>Gets the configured selector rules in evaluation order.</summary>
    /// <returns>A snapshot of the rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlTextRule[] GetRules()
    {
        HtmlTextRule[] result = new HtmlTextRule[this.rules.Count];
        int index = 0;
        while (index < this.rules.Count)
        {
            result[index] = this.rules[index].Copy();
            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the limits used by subsequent operations.</summary>
    /// <param name = "value">The validated limits to copy; later changes to the supplied object do not affect these options.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
    }

    internal void AddDefaultExclusions()
    {
        this.rules.Add(HtmlTextRule.CreateExclude("script, style, template, head, noscript"));
    }

    internal static string[] CopyBullets(string[] values)
    {
        string[] result = new string[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static void ValidateBullets(string[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("value");
        }

        if (values.Length == 0)
        {
            throw new ArgumentException("At least one unordered-list bullet is required.", "value");
        }

        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value == null || value.Length == 0)
            {
                throw new ArgumentException("Unordered-list bullets cannot be null or empty.", "value");
            }

            int characterIndex = 0;
            while (characterIndex < value.Length)
            {
                if (Ascii.IsHtmlWhitespace(value[characterIndex]))
                {
                    throw new ArgumentException("Unordered-list bullets cannot contain HTML whitespace.", "value");
                }

                characterIndex = characterIndex + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateLinkStyle(HtmlTextLinkStyle value)
    {
        if ((int)value < (int)HtmlTextLinkStyle.TextOnly || (int)value > (int)HtmlTextLinkStyle.UrlOnly)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown text link style.");
        }
    }

    internal static void ValidateImageStyle(HtmlTextImageStyle value)
    {
        if ((int)value < (int)HtmlTextImageStyle.AltText || (int)value > (int)HtmlTextImageStyle.Omit)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown text image style.");
        }
    }

    internal static void ValidateTableStyle(HtmlTextTableStyle value)
    {
        if ((int)value < (int)HtmlTextTableStyle.ReadingOrder || (int)value > (int)HtmlTextTableStyle.Omit)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown text table style.");
        }
    }

    internal static void ValidateWhitespaceMode(HtmlTextWhitespaceMode value)
    {
        if ((int)value < (int)HtmlTextWhitespaceMode.Collapse
            || (int)value > (int)HtmlTextWhitespaceMode.Preserve)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown text whitespace mode.");
        }
    }

    internal static void ValidateNonBreakingSpaceMode(
        HtmlTextNonBreakingSpaceMode value)
    {
        if ((int)value < (int)HtmlTextNonBreakingSpaceMode.Preserve
            || (int)value
                > (int)HtmlTextNonBreakingSpaceMode.ConvertToSpace)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown text non-breaking-space mode.");
        }
    }

    internal static void ValidateBoundaryBreaks(int value,
        string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value,
                "A text boundary line-break count cannot be negative.");
        }
    }
}
