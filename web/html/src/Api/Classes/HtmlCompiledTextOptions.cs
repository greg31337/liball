using System;
using System.Collections.Generic;

namespace html;
/// <summary>Contains validated plain-text conversion settings ready for repeated use.</summary>
public class HtmlCompiledTextOptions : IHtmlTextOptions, IHtmlCompiledTextOptionsApi
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
    internal HtmlCompiledTextRule[] rules;
    internal HtmlLimits limits;
    internal HtmlCompiledTextOptions(HtmlTextLinkStyle linkStyleValue,
        HtmlTextImageStyle imageStyleValue,
        HtmlTextTableStyle tableStyleValue,
        HtmlTextWhitespaceMode whitespaceModeValue,
        HtmlTextNonBreakingSpaceMode nonBreakingSpaceModeValue,
        int wrapColumnValue, string truncationMarkerValue,
        string newLineValue, bool trimValue, int maximumBlankLinesValue,
        int lineBoundaryBreaksValue, int blockBoundaryBreaksValue,
        string[] unorderedListBulletsValue, int maximumListDepthValue,
        bool includeFormControlsValue, bool includeHiddenContentValue,
        string baseUrlValue, HtmlCompiledTextRule[] rulesValue,
        HtmlLimits limitsValue)
    {
        ValidateLinkStyle(linkStyleValue);
        ValidateImageStyle(imageStyleValue);
        ValidateTableStyle(tableStyleValue);
        ValidateWhitespaceMode(whitespaceModeValue);
        ValidateNonBreakingSpaceMode(nonBreakingSpaceModeValue);
        if (wrapColumnValue < 0)
        {
            throw new ArgumentOutOfRangeException("wrapColumnValue", wrapColumnValue, "The text wrap column cannot be negative.");
        }

        if (truncationMarkerValue == null)
        {
            throw new ArgumentNullException("truncationMarkerValue");
        }

        if (newLineValue == null)
        {
            throw new ArgumentNullException("newLineValue");
        }

        if (newLineValue.Length == 0)
        {
            throw new ArgumentException("The text line ending cannot be empty.", "newLineValue");
        }

        if (maximumBlankLinesValue < 0)
        {
            throw new ArgumentOutOfRangeException("maximumBlankLinesValue", maximumBlankLinesValue, "The maximum blank-line count cannot be negative.");
        }

        ValidateBoundaryBreaks(lineBoundaryBreaksValue,
            "lineBoundaryBreaksValue");
        ValidateBoundaryBreaks(blockBoundaryBreaksValue,
            "blockBoundaryBreaksValue");

        if (baseUrlValue == null)
        {
            throw new ArgumentNullException("baseUrlValue");
        }

        ValidateBullets(unorderedListBulletsValue);
        if (maximumListDepthValue < 1)
        {
            throw new ArgumentOutOfRangeException("maximumListDepthValue", maximumListDepthValue, "The maximum text list depth must be positive.");
        }

        if (limitsValue == null)
        {
            throw new ArgumentNullException("limitsValue");
        }

        limitsValue.Validate();
        this.linkStyle = linkStyleValue;
        this.imageStyle = imageStyleValue;
        this.tableStyle = tableStyleValue;
        this.whitespaceMode = whitespaceModeValue;
        this.nonBreakingSpaceMode = nonBreakingSpaceModeValue;
        this.wrapColumn = wrapColumnValue;
        this.truncationMarker = truncationMarkerValue;
        this.newLine = newLineValue;
        this.trim = trimValue;
        this.maximumBlankLines = maximumBlankLinesValue;
        this.lineBoundaryBreaks = lineBoundaryBreaksValue;
        this.blockBoundaryBreaks = blockBoundaryBreaksValue;
        this.unorderedListBullets = CopyBullets(unorderedListBulletsValue);
        this.maximumListDepth = maximumListDepthValue;
        this.includeFormControls = includeFormControlsValue;
        this.includeHiddenContent = includeHiddenContentValue;
        this.baseUrl = baseUrlValue;
        this.rules = CopyRules(rulesValue);
        this.limits = limitsValue.Copy();
    }

    /// <summary>Returns this already compiled immutable configuration.</summary>
    /// <returns>This compiled configuration.</returns>
    public HtmlCompiledTextOptions Compile()
    {
        return this;
    }

    /// <summary>Gets how link text and destinations are represented in plain-text output.</summary>
    /// <returns>The compiled link rendering style.</returns>
    public HtmlTextLinkStyle GetLinkStyle()
    {
        return this.linkStyle;
    }

    /// <summary>Gets how images contribute alternative text, source URLs, or placeholders.</summary>
    /// <returns>The compiled image rendering style.</returns>
    public HtmlTextImageStyle GetImageStyle()
    {
        return this.imageStyle;
    }

    /// <summary>Gets whether tables use reading order, a tab-separated grid, an aligned grid, or no output.</summary>
    /// <returns>The compiled table rendering style.</returns>
    public HtmlTextTableStyle GetTableStyle()
    {
        return this.tableStyle;
    }

    /// <summary>Gets whether ordinary text collapses or preserves source whitespace.</summary>
    /// <returns>The compiled ordinary-whitespace mode.</returns>
    public HtmlTextWhitespaceMode GetWhitespaceMode()
    {
        return this.whitespaceMode;
    }

    /// <summary>Gets how non-breaking spaces are represented.</summary>
    /// <returns>The compiled non-breaking-space mode.</returns>
    public HtmlTextNonBreakingSpaceMode GetNonBreakingSpaceMode()
    {
        return this.nonBreakingSpaceMode;
    }

    /// <summary>Gets the target column at which ordinary text is wrapped.</summary>
    /// <returns>The nonnegative compiled wrap column; <c>0</c> disables column wrapping.</returns>
    public int GetWrapColumn()
    {
        return this.wrapColumn;
    }

    /// <summary>Gets the text appended when conversion cannot emit the complete result within its output limit.</summary>
    /// <returns>The compiled truncation marker; an empty string suppresses a visible marker.</returns>
    public string GetTruncationMarker()
    {
        return this.truncationMarker;
    }

    /// <summary>Gets the line-ending sequence inserted between plain-text output lines.</summary>
    /// <returns>The compiled non-empty line ending.</returns>
    public string GetNewLine()
    {
        return this.newLine;
    }

    /// <summary>Reports whether compiled conversion strips leading and trailing output whitespace.</summary>
    /// <returns><c>true</c> when leading and trailing output whitespace is removed; otherwise, <c>false</c>.</returns>
    public bool GetTrim()
    {
        return this.trim;
    }

    /// <summary>Gets how many consecutive empty lines may remain after whitespace compaction.</summary>
    /// <returns>The nonnegative compiled maximum blank-line count.</returns>
    public int GetMaximumBlankLines()
    {
        return this.maximumBlankLines;
    }

    /// <summary>Gets the number of line endings requested at a line boundary.</summary>
    /// <returns>The nonnegative compiled line-boundary count.</returns>
    public int GetLineBoundaryBreaks()
    {
        return this.lineBoundaryBreaks;
    }

    /// <summary>Gets the number of line endings requested at a block boundary.</summary>
    /// <returns>The nonnegative compiled block-boundary count.</returns>
    public int GetBlockBoundaryBreaks()
    {
        return this.blockBoundaryBreaks;
    }

    /// <summary>Gets the compiled bullet strings selected by zero-based list nesting depth.</summary>
    /// <returns>A snapshot of the unordered list bullets; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetUnorderedListBullets()
    {
        return CopyBullets(this.unorderedListBullets);
    }

    /// <summary>Gets the deepest list nesting level that receives structured list formatting.</summary>
    /// <returns>The positive compiled maximum list depth.</returns>
    public int GetMaximumListDepth()
    {
        return this.maximumListDepth;
    }

    /// <summary>Reports whether form-control values contribute to plain-text output.</summary>
    /// <returns><c>true</c> when form-control values contribute to output; otherwise, <c>false</c>.</returns>
    public bool GetIncludeFormControls()
    {
        return this.includeFormControls;
    }

    /// <summary>Reports whether hidden and inert element content contributes to output.</summary>
    /// <returns><c>true</c> when hidden and inert content is included; otherwise, <c>false</c>.</returns>
    public bool GetIncludeHiddenContent()
    {
        return this.includeHiddenContent;
    }

    /// <summary>Gets the base URL used to resolve links and media in string conversion.</summary>
    /// <returns>The compiled base URL, or an empty string when none is configured.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Gets the configured selector rules in evaluation order.</summary>
    /// <returns>A snapshot of the rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlCompiledTextRule[] GetRules()
    {
        return CopyRules(this.rules);
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    internal static HtmlCompiledTextRule[] CopyRules(HtmlCompiledTextRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("rulesValue");
        }

        HtmlCompiledTextRule[] result = new HtmlCompiledTextRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Compiled text rules cannot contain null.", "rulesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
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
            throw new ArgumentNullException("unorderedListBulletsValue");
        }

        if (values.Length == 0)
        {
            throw new ArgumentException("At least one unordered-list bullet is required.", "unorderedListBulletsValue");
        }

        int index = 0;
        while (index < values.Length)
        {
            string value = values[index];
            if (value == null || value.Length == 0)
            {
                throw new ArgumentException("Unordered-list bullets cannot be null or empty.", "unorderedListBulletsValue");
            }

            int characterIndex = 0;
            while (characterIndex < value.Length)
            {
                if (Ascii.IsHtmlWhitespace(value[characterIndex]))
                {
                    throw new ArgumentException("Unordered-list bullets cannot contain HTML whitespace.", "unorderedListBulletsValue");
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
            throw new ArgumentOutOfRangeException("linkStyleValue", (int)value, "Unknown text link style.");
        }
    }

    internal static void ValidateImageStyle(HtmlTextImageStyle value)
    {
        if ((int)value < (int)HtmlTextImageStyle.AltText || (int)value > (int)HtmlTextImageStyle.Omit)
        {
            throw new ArgumentOutOfRangeException("imageStyleValue", (int)value, "Unknown text image style.");
        }
    }

    internal static void ValidateTableStyle(HtmlTextTableStyle value)
    {
        if ((int)value < (int)HtmlTextTableStyle.ReadingOrder || (int)value > (int)HtmlTextTableStyle.Omit)
        {
            throw new ArgumentOutOfRangeException("tableStyleValue", (int)value, "Unknown text table style.");
        }
    }

    internal static void ValidateWhitespaceMode(HtmlTextWhitespaceMode value)
    {
        if ((int)value < (int)HtmlTextWhitespaceMode.Collapse
            || (int)value > (int)HtmlTextWhitespaceMode.Preserve)
        {
            throw new ArgumentOutOfRangeException("whitespaceModeValue",
                (int)value, "Unknown text whitespace mode.");
        }
    }

    internal static void ValidateNonBreakingSpaceMode(
        HtmlTextNonBreakingSpaceMode value)
    {
        if ((int)value < (int)HtmlTextNonBreakingSpaceMode.Preserve
            || (int)value
                > (int)HtmlTextNonBreakingSpaceMode.ConvertToSpace)
        {
            throw new ArgumentOutOfRangeException(
                "nonBreakingSpaceModeValue", (int)value,
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
