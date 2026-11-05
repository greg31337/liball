using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Contains validated HTML-to-Markdown settings ready for repeated conversion.</summary>
public class HtmlCompiledMarkdownOptions : IHtmlMarkdownOptions, IHtmlCompiledMarkdownOptionsApi
{
    internal HtmlMarkdownHeadingStyle headingStyle;
    internal string emphasisMarker;
    internal string strongMarker;
    internal HtmlMarkdownHardBreakStyle hardBreakStyle;
    internal HtmlMarkdownCodeFenceStyle codeFenceStyle;
    internal string thematicBreakMarker;
    internal HtmlMarkdownLinkStyle linkStyle;
    internal HtmlMarkdownUnsafeLinkFallback unsafeLinkFallback;
    internal string baseUrl;
    internal HtmlMarkdownUnknownElementAction unknownElementAction;
    internal HtmlMarkdownOrderedListNumbering orderedListNumbering;
    internal string unorderedListMarker;
    internal bool enableTables;
    internal bool enableStrikethrough;
    internal bool enableTaskLists;
    internal bool enableAutolinks;
    internal bool enableDefinitionLists;
    internal bool enableFootnotes;
    internal bool enableSubscript;
    internal bool enableSuperscript;
    internal bool enableTrustedRawHtml;
    internal string newLine;
    internal string truncationMarker;
    internal HtmlCompiledMarkdownRule[] rules;
    internal HtmlLimits limits;
    internal HtmlCompiledMarkdownOptions(
        HtmlMarkdownHeadingStyle headingStyleValue,
        string emphasisMarkerValue, string strongMarkerValue,
        HtmlMarkdownHardBreakStyle hardBreakStyleValue,
        HtmlMarkdownCodeFenceStyle codeFenceStyleValue,
        string thematicBreakMarkerValue,
        HtmlMarkdownLinkStyle linkStyleValue,
        HtmlMarkdownUnsafeLinkFallback unsafeLinkFallbackValue,
        string baseUrlValue,
        HtmlMarkdownUnknownElementAction unknownElementActionValue,
        HtmlMarkdownOrderedListNumbering orderedListNumberingValue,
        string unorderedListMarkerValue, bool enableTablesValue,
        bool enableStrikethroughValue, bool enableTaskListsValue,
        bool enableAutolinksValue, bool enableDefinitionListsValue,
        bool enableFootnotesValue, bool enableSubscriptValue,
        bool enableSuperscriptValue, bool enableTrustedRawHtmlValue,
        string newLineValue, string truncationMarkerValue,
        HtmlCompiledMarkdownRule[] rulesValue, HtmlLimits limitsValue)
    {
        ValidateHeadingStyle(headingStyleValue);
        ValidateEmphasisMarker(emphasisMarkerValue);
        ValidateStrongMarker(strongMarkerValue);
        ValidateHardBreakStyle(hardBreakStyleValue);
        ValidateCodeFenceStyle(codeFenceStyleValue);
        ValidateThematicBreakMarker(thematicBreakMarkerValue);
        ValidateLinkStyle(linkStyleValue);
        ValidateUnsafeLinkFallback(unsafeLinkFallbackValue);
        ValidateUnknownElementAction(unknownElementActionValue);
        ValidateOrderedListNumbering(orderedListNumberingValue);
        ValidateUnorderedListMarker(unorderedListMarkerValue);
        if (newLineValue == null)
        {
            throw new ArgumentNullException("newLineValue");
        }

        if (newLineValue.Length == 0)
        {
            throw new ArgumentException("The compiled Markdown line ending cannot be empty.", "newLineValue");
        }

        if (truncationMarkerValue == null)
        {
            throw new ArgumentNullException("truncationMarkerValue");
        }

        if (baseUrlValue == null)
        {
            throw new ArgumentNullException("baseUrlValue");
        }

        if (limitsValue == null)
        {
            throw new ArgumentNullException("limitsValue");
        }

        limitsValue.Validate();
        this.headingStyle = headingStyleValue;
        this.emphasisMarker = emphasisMarkerValue;
        this.strongMarker = strongMarkerValue;
        this.hardBreakStyle = hardBreakStyleValue;
        this.codeFenceStyle = codeFenceStyleValue;
        this.thematicBreakMarker = thematicBreakMarkerValue;
        this.linkStyle = linkStyleValue;
        this.unsafeLinkFallback = unsafeLinkFallbackValue;
        this.baseUrl = baseUrlValue;
        this.unknownElementAction = unknownElementActionValue;
        this.orderedListNumbering = orderedListNumberingValue;
        this.unorderedListMarker = unorderedListMarkerValue;
        this.enableTables = enableTablesValue;
        this.enableStrikethrough = enableStrikethroughValue;
        this.enableTaskLists = enableTaskListsValue;
        this.enableAutolinks = enableAutolinksValue;
        this.enableDefinitionLists = enableDefinitionListsValue;
        this.enableFootnotes = enableFootnotesValue;
        this.enableSubscript = enableSubscriptValue;
        this.enableSuperscript = enableSuperscriptValue;
        this.enableTrustedRawHtml = enableTrustedRawHtmlValue;
        this.newLine = newLineValue;
        this.truncationMarker = truncationMarkerValue;
        this.rules = CopyRules(rulesValue);
        this.limits = limitsValue.Copy();
    }

    /// <summary>Returns this already compiled immutable configuration.</summary>
    /// <returns>This compiled configuration.</returns>
    public HtmlCompiledMarkdownOptions Compile()
    {
        return this;
    }

    /// <summary>Gets whether supported headings use leading number signs or Setext underlines.</summary>
    /// <returns>The compiled heading rendering style.</returns>
    public HtmlMarkdownHeadingStyle GetHeadingStyle()
    {
        return this.headingStyle;
    }

    /// <summary>Gets the delimiter used for emphasized text.</summary>
    /// <returns>The compiled <c>*</c> or <c>_</c> delimiter.</returns>
    public string GetEmphasisMarker()
    {
        return this.emphasisMarker;
    }

    /// <summary>Gets the delimiter used for strongly emphasized text.</summary>
    /// <returns>The compiled <c>**</c> or <c>__</c> delimiter.</returns>
    public string GetStrongMarker()
    {
        return this.strongMarker;
    }

    /// <summary>Gets the syntax used for hard line breaks.</summary>
    /// <returns>The compiled hard-break style.</returns>
    public HtmlMarkdownHardBreakStyle GetHardBreakStyle()
    {
        return this.hardBreakStyle;
    }

    /// <summary>Gets how fenced code block delimiters are selected.</summary>
    /// <returns>The compiled code-fence style.</returns>
    public HtmlMarkdownCodeFenceStyle GetCodeFenceStyle()
    {
        return this.codeFenceStyle;
    }

    /// <summary>Gets the Markdown marker emitted for thematic breaks.</summary>
    /// <returns>The compiled thematic-break marker.</returns>
    public string GetThematicBreakMarker()
    {
        return this.thematicBreakMarker;
    }

    /// <summary>Gets whether links use inline destinations, collected references, or plain text.</summary>
    /// <returns>The compiled link rendering style.</returns>
    public HtmlMarkdownLinkStyle GetLinkStyle()
    {
        return this.linkStyle;
    }

    /// <summary>Gets the fallback used when a link or image destination is rejected.</summary>
    /// <returns>The compiled safe fallback.</returns>
    public HtmlMarkdownUnsafeLinkFallback GetUnsafeLinkFallback()
    {
        return this.unsafeLinkFallback;
    }

    /// <summary>Gets the base URL used to resolve links and images in string conversion.</summary>
    /// <returns>The compiled base URL, or an empty string when none is configured.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Gets how elements without a matching built-in or caller rule are handled.</summary>
    /// <returns>The compiled fallback action for unknown elements.</returns>
    public HtmlMarkdownUnknownElementAction GetUnknownElementAction()
    {
        return this.unknownElementAction;
    }

    /// <summary>Gets whether ordered-list markers preserve HTML numbering or use <c>1</c> for every item.</summary>
    /// <returns>The compiled ordered-list numbering mode.</returns>
    public HtmlMarkdownOrderedListNumbering GetOrderedListNumbering()
    {
        return this.orderedListNumbering;
    }

    /// <summary>Gets the single Markdown marker placed before unordered-list items.</summary>
    /// <returns>The compiled <c>-</c>, <c>+</c>, or <c>*</c> marker.</returns>
    public string GetUnorderedListMarker()
    {
        return this.unorderedListMarker;
    }

    /// <summary>Reports whether GitHub-style table conversion is enabled.</summary>
    /// <returns><c>true</c> when table conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTables()
    {
        return this.enableTables;
    }

    /// <summary>Reports whether strikethrough is enabled.</summary>
    /// <returns><c>true</c> when strikethrough conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableStrikethrough()
    {
        return this.enableStrikethrough;
    }

    /// <summary>Reports whether GitHub-style task-list conversion is enabled.</summary>
    /// <returns><c>true</c> when task-list conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTaskLists()
    {
        return this.enableTaskLists;
    }

    /// <summary>Reports whether automatic link conversion is enabled.</summary>
    /// <returns><c>true</c> when automatic link conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableAutolinks()
    {
        return this.enableAutolinks;
    }

    /// <summary>Reports whether definition-list conversion is enabled.</summary>
    /// <returns><c>true</c> when definition-list conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableDefinitionLists()
    {
        return this.enableDefinitionLists;
    }

    /// <summary>Reports whether footnote conversion is enabled.</summary>
    /// <returns><c>true</c> when footnote conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableFootnotes()
    {
        return this.enableFootnotes;
    }

    /// <summary>Reports whether subscript conversion is enabled.</summary>
    /// <returns><c>true</c> when subscript conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableSubscript()
    {
        return this.enableSubscript;
    }

    /// <summary>Reports whether superscript conversion is enabled.</summary>
    /// <returns><c>true</c> when superscript conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableSuperscript()
    {
        return this.enableSuperscript;
    }

    /// <summary>Reports whether trusted raw HTML is enabled.</summary>
    /// <returns><c>true</c> when trusted raw HTML output is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTrustedRawHtml()
    {
        return this.enableTrustedRawHtml;
    }

    /// <summary>Gets the line-ending sequence inserted between Markdown output lines.</summary>
    /// <returns>The compiled non-empty line ending.</returns>
    public string GetNewLine()
    {
        return this.newLine;
    }

    /// <summary>Gets the text appended when conversion cannot emit complete Markdown within its output limit.</summary>
    /// <returns>The compiled truncation marker; an empty string suppresses a visible marker.</returns>
    public string GetTruncationMarker()
    {
        return this.truncationMarker;
    }

    /// <summary>Gets the configured selector rules in evaluation order.</summary>
    /// <returns>A snapshot of the rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlCompiledMarkdownRule[] GetRules()
    {
        return CopyRules(this.rules);
    }

    /// <summary>Finds the last configured selector rule that matches the node.</summary>
    /// <param name = "node">The root of the node or subtree processed by the operation.</param>
    /// <param name = "scope">The root that defines selector scope and traversal bounds.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The stored compiled rule. Returns null when no configured rule matches.</returns>
    /// <remarks>Rules are evaluated in configuration order and later matching rules override earlier ones.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public HtmlCompiledMarkdownRule? FindRule(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (scope == null)
        {
            throw new ArgumentNullException("scope");
        }

        HtmlCompiledMarkdownRule? matched = null;
        int index = 0;
        while (index < this.rules.Length)
        {
            if (this.rules[index].Matches(node, scope, cancellationToken))
            {
                matched = this.rules[index];
            }

            index = index + 1;
        }

        return matched;
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    internal static HtmlCompiledMarkdownRule[] CopyRules(HtmlCompiledMarkdownRule[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("rulesValue");
        }

        HtmlCompiledMarkdownRule[] result = new HtmlCompiledMarkdownRule[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("Compiled Markdown rules cannot contain null.", "rulesValue");
            }

            result[index] = values[index];
            index = index + 1;
        }

        return result;
    }

    internal static void ValidateHeadingStyle(HtmlMarkdownHeadingStyle value)
    {
        if ((int)value < (int)HtmlMarkdownHeadingStyle.Atx || (int)value > (int)HtmlMarkdownHeadingStyle.Setext)
        {
            throw new ArgumentOutOfRangeException("headingStyleValue", (int)value, "Unknown compiled Markdown heading style.");
        }
    }

    internal static void ValidateEmphasisMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("emphasisMarkerValue");
        }

        if (value != "*" && value != "_")
        {
            throw new ArgumentException(
                "A compiled Markdown emphasis marker must be * or _.",
                "emphasisMarkerValue");
        }
    }

    internal static void ValidateStrongMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("strongMarkerValue");
        }

        if (value != "**" && value != "__")
        {
            throw new ArgumentException(
                "A compiled Markdown strong marker must be ** or __.",
                "strongMarkerValue");
        }
    }

    internal static void ValidateHardBreakStyle(
        HtmlMarkdownHardBreakStyle value)
    {
        if ((int)value < (int)HtmlMarkdownHardBreakStyle.TwoSpaces
            || (int)value > (int)HtmlMarkdownHardBreakStyle.Backslash)
        {
            throw new ArgumentOutOfRangeException("hardBreakStyleValue",
                (int)value, "Unknown compiled Markdown hard-break style.");
        }
    }

    internal static void ValidateCodeFenceStyle(
        HtmlMarkdownCodeFenceStyle value)
    {
        if ((int)value < (int)HtmlMarkdownCodeFenceStyle.Adaptive
            || (int)value > (int)HtmlMarkdownCodeFenceStyle.Tildes)
        {
            throw new ArgumentOutOfRangeException("codeFenceStyleValue",
                (int)value, "Unknown compiled Markdown code-fence style.");
        }
    }

    internal static void ValidateThematicBreakMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("thematicBreakMarkerValue");
        }

        if (value != "---" && value != "***" && value != "___")
        {
            throw new ArgumentException(
                "A compiled Markdown thematic-break marker must be ---, ***, or ___.",
                "thematicBreakMarkerValue");
        }
    }

    internal static void ValidateLinkStyle(HtmlMarkdownLinkStyle value)
    {
        if ((int)value < (int)HtmlMarkdownLinkStyle.Inline || (int)value > (int)HtmlMarkdownLinkStyle.CollapsedReference)
        {
            throw new ArgumentOutOfRangeException("linkStyleValue", (int)value, "Unknown compiled Markdown link style.");
        }
    }

    internal static void ValidateUnsafeLinkFallback(
        HtmlMarkdownUnsafeLinkFallback value)
    {
        if ((int)value < (int)HtmlMarkdownUnsafeLinkFallback.Text
            || (int)value > (int)HtmlMarkdownUnsafeLinkFallback.Omit)
        {
            throw new ArgumentOutOfRangeException(
                "unsafeLinkFallbackValue", (int)value,
                "Unknown compiled Markdown unsafe-link fallback.");
        }
    }

    internal static void ValidateUnknownElementAction(HtmlMarkdownUnknownElementAction value)
    {
        if ((int)value < (int)HtmlMarkdownUnknownElementAction.Unwrap || (int)value > (int)HtmlMarkdownUnknownElementAction.Error)
        {
            throw new ArgumentOutOfRangeException("unknownElementActionValue", (int)value, "Unknown compiled Markdown element action.");
        }
    }

    internal static void ValidateOrderedListNumbering(HtmlMarkdownOrderedListNumbering value)
    {
        if ((int)value < (int)HtmlMarkdownOrderedListNumbering.Preserve || (int)value > (int)HtmlMarkdownOrderedListNumbering.One)
        {
            throw new ArgumentOutOfRangeException("orderedListNumberingValue", (int)value, "Unknown compiled Markdown ordered-list numbering choice.");
        }
    }

    internal static void ValidateUnorderedListMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("unorderedListMarkerValue");
        }

        if (value != "-" && value != "+" && value != "*")
        {
            throw new ArgumentException("A compiled Markdown unordered-list marker must be -, +, or *.", "unorderedListMarkerValue");
        }
    }
}
