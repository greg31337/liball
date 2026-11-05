using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Configures HTML-to-Markdown conversion, element rules, output style, and resource limits.</summary>
public class HtmlMarkdownOptions : IHtmlMarkdownOptions, IHtmlMarkdownOptionsApi
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
    internal List<HtmlMarkdownRule> rules;
    internal HtmlLimits limits;
    /// <summary>Creates Markdown options initialized for CommonMark-compatible output.</summary>
    public HtmlMarkdownOptions()
    {
        this.headingStyle = HtmlMarkdownHeadingStyle.Atx;
        this.emphasisMarker = "*";
        this.strongMarker = "**";
        this.hardBreakStyle = HtmlMarkdownHardBreakStyle.TwoSpaces;
        this.codeFenceStyle = HtmlMarkdownCodeFenceStyle.Adaptive;
        this.thematicBreakMarker = "---";
        this.linkStyle = HtmlMarkdownLinkStyle.Inline;
        this.unsafeLinkFallback = HtmlMarkdownUnsafeLinkFallback.Text;
        this.baseUrl = "";
        this.unknownElementAction = HtmlMarkdownUnknownElementAction.Unwrap;
        this.orderedListNumbering = HtmlMarkdownOrderedListNumbering.Preserve;
        this.unorderedListMarker = "-";
        this.enableTables = false;
        this.enableStrikethrough = false;
        this.enableTaskLists = false;
        this.enableAutolinks = false;
        this.enableDefinitionLists = false;
        this.enableFootnotes = false;
        this.enableSubscript = false;
        this.enableSuperscript = false;
        this.enableTrustedRawHtml = false;
        this.newLine = "\n";
        this.truncationMarker = "\u2026";
        this.rules = new List<HtmlMarkdownRule>();
        this.limits = HtmlLimits.CreateDefault();
    }

    /// <summary>Creates settings for CommonMark-compatible output.</summary>
    /// <returns>A new independently mutable options object for CommonMark-compatible output.</returns>
    internal static HtmlMarkdownOptions CreateCommonMark()
    {
        return new HtmlMarkdownOptions();
    }

    /// <summary>Creates settings for GitHub Flavored Markdown output.</summary>
    /// <returns>A new options object with supported GitHub Flavored Markdown features enabled.</returns>
    internal static HtmlMarkdownOptions CreateGitHubFlavored()
    {
        HtmlMarkdownOptions options = new HtmlMarkdownOptions();
        options.enableTables = true;
        options.enableStrikethrough = true;
        options.enableTaskLists = true;
        options.enableAutolinks = true;
        return options;
    }

    /// <summary>Creates settings that favor readable Markdown over source preservation.</summary>
    /// <returns>A new options object favoring readable output and destination-preserving links.</returns>
    internal static HtmlMarkdownOptions CreateReadable()
    {
        HtmlMarkdownOptions options = CreateGitHubFlavored();
        options.linkStyle = HtmlMarkdownLinkStyle.Reference;
        options.enableDefinitionLists = true;
        options.enableFootnotes = true;
        options.enableSubscript = true;
        options.enableSuperscript = true;
        return options;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent Markdown-options object containing copies of the current settings and rules.</returns>
    public HtmlMarkdownOptions Copy()
    {
        HtmlMarkdownOptions copy = new HtmlMarkdownOptions();
        copy.headingStyle = this.headingStyle;
        copy.emphasisMarker = this.emphasisMarker;
        copy.strongMarker = this.strongMarker;
        copy.hardBreakStyle = this.hardBreakStyle;
        copy.codeFenceStyle = this.codeFenceStyle;
        copy.thematicBreakMarker = this.thematicBreakMarker;
        copy.linkStyle = this.linkStyle;
        copy.unsafeLinkFallback = this.unsafeLinkFallback;
        copy.baseUrl = this.baseUrl;
        copy.unknownElementAction = this.unknownElementAction;
        copy.orderedListNumbering = this.orderedListNumbering;
        copy.unorderedListMarker = this.unorderedListMarker;
        copy.enableTables = this.enableTables;
        copy.enableStrikethrough = this.enableStrikethrough;
        copy.enableTaskLists = this.enableTaskLists;
        copy.enableAutolinks = this.enableAutolinks;
        copy.enableDefinitionLists = this.enableDefinitionLists;
        copy.enableFootnotes = this.enableFootnotes;
        copy.enableSubscript = this.enableSubscript;
        copy.enableSuperscript = this.enableSuperscript;
        copy.enableTrustedRawHtml = this.enableTrustedRawHtml;
        copy.newLine = this.newLine;
        copy.truncationMarker = this.truncationMarker;
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
        ValidateHeadingStyle(this.headingStyle);
        ValidateEmphasisMarker(this.emphasisMarker);
        ValidateStrongMarker(this.strongMarker);
        ValidateHardBreakStyle(this.hardBreakStyle);
        ValidateCodeFenceStyle(this.codeFenceStyle);
        ValidateThematicBreakMarker(this.thematicBreakMarker);
        ValidateLinkStyle(this.linkStyle);
        ValidateUnsafeLinkFallback(this.unsafeLinkFallback);
        ValidateUnknownElementAction(this.unknownElementAction);
        ValidateOrderedListNumbering(this.orderedListNumbering);
        ValidateUnorderedListMarker(this.unorderedListMarker);
        if (this.newLine == null || this.newLine.Length == 0)
        {
            throw new InvalidOperationException("The Markdown line ending cannot be empty.");
        }

        if (this.truncationMarker == null || this.baseUrl == null
            || this.rules == null || this.limits == null)
        {
            throw new InvalidOperationException("Markdown options contain a missing required value.");
        }

        this.limits.Validate();
        int index = 0;
        while (index < this.rules.Count)
        {
            if (this.rules[index] == null)
            {
                throw new InvalidOperationException("Markdown options contain a null rule.");
            }

            index = index + 1;
        }
    }

    /// <summary>Validates the current settings and returns an immutable configuration for repeated use.</summary>
    /// <returns>A validated compiled configuration that can be reused safely.</returns>
    /// <exception cref = "ArgumentException">Thrown when an argument is inconsistent or malformed.</exception>
    public HtmlCompiledMarkdownOptions Compile()
    {
        HtmlMarkdownCompileResult result = this.CompileWithDiagnostics();
        if (!result.IsSuccess())
        {
            HtmlDiagnostic[] diagnostics = result.GetDiagnostics();
            string message = diagnostics.Length == 0 ? "Markdown options could not be compiled." : diagnostics[0].GetMessage();
            throw new ArgumentException(message, "rules");
        }

        HtmlCompiledMarkdownOptions? options = result.GetOptions();
        if (options == null)
        {
            throw new InvalidOperationException("Successful Markdown compilation must produce options.");
        }

        return options;
    }

    /// <summary>Validates and compiles the with diagnostics for repeated use.</summary>
    /// <returns>The compilation status, compiled options when valid, and diagnostics.</returns>
    public HtmlMarkdownCompileResult CompileWithDiagnostics()
    {
        this.Validate();
        HtmlBudget budget = new HtmlBudget(this.limits, CancellationToken.None);
        HtmlDiagnosticCollector diagnostics = new HtmlDiagnosticCollector(budget);
        List<HtmlCompiledMarkdownRule> compiled = new List<HtmlCompiledMarkdownRule>();
        bool success = true;
        if (!this.enableTrustedRawHtml && this.unknownElementAction == HtmlMarkdownUnknownElementAction.PreserveHtml)
        {
            diagnostics.AddDetails(HtmlDiagnosticCode.MarkdownInvalidConfiguration, HtmlDiagnosticSeverity.Error, "Preserving unknown HTML requires trusted raw HTML output.", HtmlSourceSpan.CreateUnknown());
            success = false;
        }

        int index = 0;
        while (index < this.rules.Count)
        {
            if (!budget.TryConsume(HtmlLimitKind.Visits, 1))
            {
                diagnostics.AddDetails(HtmlDiagnosticCode.MarkdownConversionLimitExceeded, HtmlDiagnosticSeverity.Error, "The Markdown rule compilation limit was exceeded.", HtmlSourceSpan.CreateUnknown());
                success = false;
                break;
            }

            if (!this.enableTrustedRawHtml && this.rules[index].GetAction() == HtmlMarkdownRuleAction.Keep)
            {
                diagnostics.AddDetails(HtmlDiagnosticCode.MarkdownInvalidConfiguration, HtmlDiagnosticSeverity.Error, "A keep rule requires trusted raw HTML output.", HtmlSourceSpan.CreateUnknown());
                success = false;
            }

            HtmlSelectorCompileResult result = HtmlSelectors.Compile(this.rules[index].GetSelector(), this.limits);
            if (!result.IsSuccess())
            {
                AddRuleDiagnostics(diagnostics, result.GetDiagnostics());
                success = false;
            }
            else
            {
                compiled.Add(new HtmlCompiledMarkdownRule(this.rules[index].GetSelector(), result.GetSelector(), this.rules[index].GetAction()));
            }

            index = index + 1;
        }

        HtmlCompiledMarkdownOptions? options = null;
        if (success)
        {
            options = new HtmlCompiledMarkdownOptions(this.headingStyle,
                this.emphasisMarker, this.strongMarker,
                this.hardBreakStyle, this.codeFenceStyle,
                this.thematicBreakMarker, this.linkStyle,
                this.unsafeLinkFallback, this.baseUrl,
                this.unknownElementAction, this.orderedListNumbering,
                this.unorderedListMarker, this.enableTables,
                this.enableStrikethrough, this.enableTaskLists,
                this.enableAutolinks, this.enableDefinitionLists,
                this.enableFootnotes, this.enableSubscript,
                this.enableSuperscript, this.enableTrustedRawHtml,
                this.newLine, this.truncationMarker, compiled.ToArray(),
                this.limits);
        }

        return new HtmlMarkdownCompileResult(options, diagnostics.GetDiagnostics(), diagnostics.GetTotalCount(), diagnostics.GetDroppedCount());
    }

    /// <summary>Gets whether supported headings use leading number signs or Setext underlines.</summary>
    /// <returns>The configured heading rendering style.</returns>
    public HtmlMarkdownHeadingStyle GetHeadingStyle()
    {
        return this.headingStyle;
    }

    /// <summary>Sets the heading style used by subsequent operations.</summary>
    /// <param name = "value">The heading style to validate and use.</param>
    public void SetHeadingStyle(HtmlMarkdownHeadingStyle value)
    {
        ValidateHeadingStyle(value);
        this.headingStyle = value;
    }

    /// <summary>Gets the delimiter used for emphasized text.</summary>
    /// <returns><c>*</c> or <c>_</c>.</returns>
    public string GetEmphasisMarker()
    {
        return this.emphasisMarker;
    }

    /// <summary>Sets the delimiter used for emphasized text.</summary>
    /// <param name = "value"><c>*</c> or <c>_</c>.</param>
    public void SetEmphasisMarker(string value)
    {
        ValidateEmphasisMarker(value);
        this.emphasisMarker = value;
    }

    /// <summary>Gets the delimiter used for strongly emphasized text.</summary>
    /// <returns><c>**</c> or <c>__</c>.</returns>
    public string GetStrongMarker()
    {
        return this.strongMarker;
    }

    /// <summary>Sets the delimiter used for strongly emphasized text.</summary>
    /// <param name = "value"><c>**</c> or <c>__</c>.</param>
    public void SetStrongMarker(string value)
    {
        ValidateStrongMarker(value);
        this.strongMarker = value;
    }

    /// <summary>Gets the syntax used for hard line breaks.</summary>
    /// <returns>The configured hard-break style.</returns>
    public HtmlMarkdownHardBreakStyle GetHardBreakStyle()
    {
        return this.hardBreakStyle;
    }

    /// <summary>Sets the syntax used for hard line breaks.</summary>
    /// <param name = "value">The hard-break style to validate and use.</param>
    public void SetHardBreakStyle(HtmlMarkdownHardBreakStyle value)
    {
        ValidateHardBreakStyle(value);
        this.hardBreakStyle = value;
    }

    /// <summary>Gets how fenced code block delimiters are selected.</summary>
    /// <returns>The configured code-fence style.</returns>
    public HtmlMarkdownCodeFenceStyle GetCodeFenceStyle()
    {
        return this.codeFenceStyle;
    }

    /// <summary>Sets how fenced code block delimiters are selected.</summary>
    /// <param name = "value">The code-fence style to validate and use.</param>
    public void SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle value)
    {
        ValidateCodeFenceStyle(value);
        this.codeFenceStyle = value;
    }

    /// <summary>Gets the Markdown marker emitted for thematic breaks.</summary>
    /// <returns><c>---</c>, <c>***</c>, or <c>___</c>.</returns>
    public string GetThematicBreakMarker()
    {
        return this.thematicBreakMarker;
    }

    /// <summary>Sets the Markdown marker emitted for thematic breaks.</summary>
    /// <param name = "value"><c>---</c>, <c>***</c>, or <c>___</c>.</param>
    public void SetThematicBreakMarker(string value)
    {
        ValidateThematicBreakMarker(value);
        this.thematicBreakMarker = value;
    }

    /// <summary>Gets whether links use inline destinations, collected references, or plain text.</summary>
    /// <returns>The configured link rendering style.</returns>
    public HtmlMarkdownLinkStyle GetLinkStyle()
    {
        return this.linkStyle;
    }

    /// <summary>Sets the link style used by subsequent operations.</summary>
    /// <param name = "value">The link rendering style to validate and use.</param>
    public void SetLinkStyle(HtmlMarkdownLinkStyle value)
    {
        ValidateLinkStyle(value);
        this.linkStyle = value;
    }

    /// <summary>Gets the fallback used when a link or image destination is rejected.</summary>
    /// <returns>The configured safe fallback.</returns>
    public HtmlMarkdownUnsafeLinkFallback GetUnsafeLinkFallback()
    {
        return this.unsafeLinkFallback;
    }

    /// <summary>Sets the fallback used when a link or image destination is rejected.</summary>
    /// <param name = "value">The safe fallback to validate and use.</param>
    public void SetUnsafeLinkFallback(
        HtmlMarkdownUnsafeLinkFallback value)
    {
        ValidateUnsafeLinkFallback(value);
        this.unsafeLinkFallback = value;
    }

    /// <summary>Gets the base URL used to resolve links and images in string conversion.</summary>
    /// <returns>The configured base URL, or an empty string when none is configured.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Sets the base URL used to resolve links and images in string conversion.</summary>
    /// <param name = "value">The base URL to copy; an empty string disables explicit base resolution.</param>
    public void SetBaseUrl(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.baseUrl = value;
    }

    /// <summary>Gets how elements without a matching built-in or caller rule are handled.</summary>
    /// <returns>The configured unwrap, drop, preserve, escape, text, or diagnostic action.</returns>
    public HtmlMarkdownUnknownElementAction GetUnknownElementAction()
    {
        return this.unknownElementAction;
    }

    /// <summary>Sets the unknown element action used by subsequent operations.</summary>
    /// <param name = "value">The fallback action to validate and apply to unknown elements.</param>
    public void SetUnknownElementAction(HtmlMarkdownUnknownElementAction value)
    {
        ValidateUnknownElementAction(value);
        this.unknownElementAction = value;
    }

    /// <summary>Gets whether ordered-list markers preserve HTML numbering or use <c>1</c> for every item.</summary>
    /// <returns>The configured ordered-list numbering mode.</returns>
    public HtmlMarkdownOrderedListNumbering GetOrderedListNumbering()
    {
        return this.orderedListNumbering;
    }

    /// <summary>Sets the ordered list numbering used by subsequent operations.</summary>
    /// <param name = "value">The ordered-list numbering mode to validate and use.</param>
    public void SetOrderedListNumbering(HtmlMarkdownOrderedListNumbering value)
    {
        ValidateOrderedListNumbering(value);
        this.orderedListNumbering = value;
    }

    /// <summary>Gets the single Markdown marker placed before unordered-list items.</summary>
    /// <returns><c>-</c>, <c>+</c>, or <c>*</c>.</returns>
    public string GetUnorderedListMarker()
    {
        return this.unorderedListMarker;
    }

    /// <summary>Sets the unordered list marker used by subsequent operations.</summary>
    /// <param name = "value">One of <c>-</c>, <c>+</c>, or <c>*</c>.</param>
    public void SetUnorderedListMarker(string value)
    {
        ValidateUnorderedListMarker(value);
        this.unorderedListMarker = value;
    }

    /// <summary>Reports whether GitHub-style table conversion is enabled.</summary>
    /// <returns><c>true</c> when table conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTables()
    {
        return this.enableTables;
    }

    /// <summary>Controls whether HTML tables are converted to GitHub-style Markdown tables.</summary>
    /// <param name = "value"><c>true</c> to emit supported tables using GitHub-style pipe-table syntax.</param>
    public void SetEnableTables(bool value)
    {
        this.enableTables = value;
    }

    /// <summary>Reports whether strikethrough is enabled.</summary>
    /// <returns><c>true</c> when strikethrough conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableStrikethrough()
    {
        return this.enableStrikethrough;
    }

    /// <summary>Controls whether deleted text is emitted using Markdown strikethrough syntax.</summary>
    /// <param name = "value"><c>true</c> to represent deleted text with double-tilde delimiters.</param>
    public void SetEnableStrikethrough(bool value)
    {
        this.enableStrikethrough = value;
    }

    /// <summary>Reports whether GitHub-style task-list conversion is enabled.</summary>
    /// <returns><c>true</c> when task-list conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTaskLists()
    {
        return this.enableTaskLists;
    }

    /// <summary>Controls whether checkbox list items are emitted using GitHub-style task-list markers.</summary>
    /// <param name = "value"><c>true</c> to emit checkbox list items with GitHub-style task markers.</param>
    public void SetEnableTaskLists(bool value)
    {
        this.enableTaskLists = value;
    }

    /// <summary>Reports whether automatic link conversion is enabled.</summary>
    /// <returns><c>true</c> when automatic link conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableAutolinks()
    {
        return this.enableAutolinks;
    }

    /// <summary>Controls whether suitable absolute URLs are emitted using Markdown autolink syntax.</summary>
    /// <param name = "value"><c>true</c> to use angle-bracket autolinks for suitable absolute URLs.</param>
    public void SetEnableAutolinks(bool value)
    {
        this.enableAutolinks = value;
    }

    /// <summary>Reports whether definition-list conversion is enabled.</summary>
    /// <returns><c>true</c> when definition-list conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableDefinitionLists()
    {
        return this.enableDefinitionLists;
    }

    /// <summary>Controls whether HTML definition lists use the configured Markdown definition-list extension.</summary>
    /// <param name = "value"><c>true</c> to emit definition lists using the converter's extension syntax.</param>
    public void SetEnableDefinitionLists(bool value)
    {
        this.enableDefinitionLists = value;
    }

    /// <summary>Reports whether footnote conversion is enabled.</summary>
    /// <returns><c>true</c> when footnote conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableFootnotes()
    {
        return this.enableFootnotes;
    }

    /// <summary>Controls whether recognized footnote markup is emitted using Markdown footnote syntax.</summary>
    /// <param name = "value"><c>true</c> to emit recognized footnote references and definitions using extension syntax.</param>
    public void SetEnableFootnotes(bool value)
    {
        this.enableFootnotes = value;
    }

    /// <summary>Reports whether subscript conversion is enabled.</summary>
    /// <returns><c>true</c> when subscript conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableSubscript()
    {
        return this.enableSubscript;
    }

    /// <summary>Controls whether subscript elements use the configured Markdown subscript extension.</summary>
    /// <param name = "value"><c>true</c> to emit supported subscript elements using extension syntax.</param>
    public void SetEnableSubscript(bool value)
    {
        this.enableSubscript = value;
    }

    /// <summary>Reports whether superscript conversion is enabled.</summary>
    /// <returns><c>true</c> when superscript conversion is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableSuperscript()
    {
        return this.enableSuperscript;
    }

    /// <summary>Controls whether superscript elements use the configured Markdown superscript extension.</summary>
    /// <param name = "value"><c>true</c> to emit supported superscript elements using extension syntax.</param>
    public void SetEnableSuperscript(bool value)
    {
        this.enableSuperscript = value;
    }

    /// <summary>Reports whether trusted raw HTML is enabled.</summary>
    /// <returns><c>true</c> when trusted raw HTML output is enabled; otherwise, <c>false</c>.</returns>
    public bool GetEnableTrustedRawHtml()
    {
        return this.enableTrustedRawHtml;
    }

    /// <summary>Controls whether conversion rules may copy raw HTML into Markdown output.</summary>
    /// <param name = "value"><c>true</c> only when the input and eventual Markdown renderer are trusted; otherwise, <c>false</c>.</param>
    /// <remarks>Enabling this option bypasses escaping for rule actions that retain raw markup. It does not sanitize HTML and must not be enabled for attacker-controlled input.</remarks>
    public void SetEnableTrustedRawHtml(bool value)
    {
        this.enableTrustedRawHtml = value;
    }

    /// <summary>Gets the line-ending sequence inserted between Markdown output lines.</summary>
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
            throw new ArgumentException("The Markdown line ending cannot be empty.", "value");
        }

        this.newLine = value;
    }

    /// <summary>Gets the text appended when conversion cannot emit complete Markdown within its output limit.</summary>
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

    /// <summary>Adds a copied selector rule to the end of this Markdown-conversion rule list.</summary>
    /// <param name = "value">The rule to copy and append.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddRule(HtmlMarkdownRule value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.rules.Add(value.Copy());
    }

    /// <summary>Adds a rule that preserves elements matched by the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    public void Keep(string selector)
    {
        this.AddRule(HtmlMarkdownRule.CreateKeep(selector));
    }

    /// <summary>Adds a rule that removes elements matched by the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    public void Remove(string selector)
    {
        this.AddRule(HtmlMarkdownRule.CreateRemove(selector));
    }

    /// <summary>Adds a rule that applies the converter's default handling to matched elements.</summary>
    /// <param name = "selector">The CSS selector text to compile when the options are compiled.</param>
    public void UseDefault(string selector)
    {
        this.AddRule(HtmlMarkdownRule.CreateDefault(selector));
    }

    /// <summary>Gets the configured selector rules in evaluation order.</summary>
    /// <returns>A snapshot of the rules; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlMarkdownRule[] GetRules()
    {
        HtmlMarkdownRule[] result = new HtmlMarkdownRule[this.rules.Count];
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

    internal static void AddRuleDiagnostics(HtmlDiagnosticCollector collector, HtmlDiagnostic[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            HtmlDiagnostic diagnostic = values[index];
            collector.Add(HtmlDiagnostic.Create(HtmlDiagnosticCode.MarkdownInvalidRule, diagnostic.GetSeverity(), diagnostic.GetMessage(), diagnostic.GetSpan()));
            index = index + 1;
        }
    }

    internal static void ValidateHeadingStyle(HtmlMarkdownHeadingStyle value)
    {
        if ((int)value < (int)HtmlMarkdownHeadingStyle.Atx || (int)value > (int)HtmlMarkdownHeadingStyle.Setext)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown Markdown heading style.");
        }
    }

    internal static void ValidateEmphasisMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "*" && value != "_")
        {
            throw new ArgumentException(
                "A Markdown emphasis marker must be * or _.", "value");
        }
    }

    internal static void ValidateStrongMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "**" && value != "__")
        {
            throw new ArgumentException(
                "A Markdown strong marker must be ** or __.", "value");
        }
    }

    internal static void ValidateHardBreakStyle(
        HtmlMarkdownHardBreakStyle value)
    {
        if ((int)value < (int)HtmlMarkdownHardBreakStyle.TwoSpaces
            || (int)value > (int)HtmlMarkdownHardBreakStyle.Backslash)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown Markdown hard-break style.");
        }
    }

    internal static void ValidateCodeFenceStyle(
        HtmlMarkdownCodeFenceStyle value)
    {
        if ((int)value < (int)HtmlMarkdownCodeFenceStyle.Adaptive
            || (int)value > (int)HtmlMarkdownCodeFenceStyle.Tildes)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown Markdown code-fence style.");
        }
    }

    internal static void ValidateThematicBreakMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "---" && value != "***" && value != "___")
        {
            throw new ArgumentException(
                "A Markdown thematic-break marker must be ---, ***, or ___.",
                "value");
        }
    }

    internal static void ValidateLinkStyle(HtmlMarkdownLinkStyle value)
    {
        if ((int)value < (int)HtmlMarkdownLinkStyle.Inline || (int)value > (int)HtmlMarkdownLinkStyle.CollapsedReference)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown Markdown link style.");
        }
    }

    internal static void ValidateUnsafeLinkFallback(
        HtmlMarkdownUnsafeLinkFallback value)
    {
        if ((int)value < (int)HtmlMarkdownUnsafeLinkFallback.Text
            || (int)value > (int)HtmlMarkdownUnsafeLinkFallback.Omit)
        {
            throw new ArgumentOutOfRangeException("value", (int)value,
                "Unknown Markdown unsafe-link fallback.");
        }
    }

    internal static void ValidateUnknownElementAction(HtmlMarkdownUnknownElementAction value)
    {
        if ((int)value < (int)HtmlMarkdownUnknownElementAction.Unwrap || (int)value > (int)HtmlMarkdownUnknownElementAction.Error)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown Markdown element action.");
        }
    }

    internal static void ValidateOrderedListNumbering(HtmlMarkdownOrderedListNumbering value)
    {
        if ((int)value < (int)HtmlMarkdownOrderedListNumbering.Preserve || (int)value > (int)HtmlMarkdownOrderedListNumbering.One)
        {
            throw new ArgumentOutOfRangeException("value", (int)value, "Unknown Markdown ordered-list numbering choice.");
        }
    }

    internal static void ValidateUnorderedListMarker(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "-" && value != "+" && value != "*")
        {
            throw new ArgumentException("A Markdown unordered-list marker must be -, +, or *.", "value");
        }
    }
}
