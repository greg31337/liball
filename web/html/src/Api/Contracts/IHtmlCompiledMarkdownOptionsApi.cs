using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlCompiledMarkdownOptions"/>.</summary>
public interface IHtmlCompiledMarkdownOptionsApi
{
    /// <summary>Executes the GetHeadingStyle operation.</summary>
    public HtmlMarkdownHeadingStyle GetHeadingStyle();
    /// <summary>Gets the delimiter used for emphasized text.</summary>
    public string GetEmphasisMarker();
    /// <summary>Gets the delimiter used for strongly emphasized text.</summary>
    public string GetStrongMarker();
    /// <summary>Gets the syntax used for hard line breaks.</summary>
    public HtmlMarkdownHardBreakStyle GetHardBreakStyle();
    /// <summary>Gets how fenced code block delimiters are selected.</summary>
    public HtmlMarkdownCodeFenceStyle GetCodeFenceStyle();
    /// <summary>Gets the Markdown marker emitted for thematic breaks.</summary>
    public string GetThematicBreakMarker();
    /// <summary>Executes the GetLinkStyle operation.</summary>
    public HtmlMarkdownLinkStyle GetLinkStyle();
    /// <summary>Gets the fallback used when a link or image destination is rejected.</summary>
    public HtmlMarkdownUnsafeLinkFallback GetUnsafeLinkFallback();
    /// <summary>Gets the base URL used to resolve links and images in string conversion.</summary>
    public string GetBaseUrl();
    /// <summary>Executes the GetUnknownElementAction operation.</summary>
    public HtmlMarkdownUnknownElementAction GetUnknownElementAction();
    /// <summary>Executes the GetOrderedListNumbering operation.</summary>
    public HtmlMarkdownOrderedListNumbering GetOrderedListNumbering();
    /// <summary>Executes the GetUnorderedListMarker operation.</summary>
    public string GetUnorderedListMarker();
    /// <summary>Executes the GetEnableTables operation.</summary>
    public bool GetEnableTables();
    /// <summary>Executes the GetEnableStrikethrough operation.</summary>
    public bool GetEnableStrikethrough();
    /// <summary>Executes the GetEnableTaskLists operation.</summary>
    public bool GetEnableTaskLists();
    /// <summary>Executes the GetEnableAutolinks operation.</summary>
    public bool GetEnableAutolinks();
    /// <summary>Executes the GetEnableDefinitionLists operation.</summary>
    public bool GetEnableDefinitionLists();
    /// <summary>Executes the GetEnableFootnotes operation.</summary>
    public bool GetEnableFootnotes();
    /// <summary>Executes the GetEnableSubscript operation.</summary>
    public bool GetEnableSubscript();
    /// <summary>Executes the GetEnableSuperscript operation.</summary>
    public bool GetEnableSuperscript();
    /// <summary>Executes the GetEnableTrustedRawHtml operation.</summary>
    public bool GetEnableTrustedRawHtml();
    /// <summary>Executes the GetNewLine operation.</summary>
    public string GetNewLine();
    /// <summary>Executes the GetTruncationMarker operation.</summary>
    public string GetTruncationMarker();
    /// <summary>Executes the GetRules operation.</summary>
    public HtmlCompiledMarkdownRule[] GetRules();
    /// <summary>Executes the FindRule operation.</summary>
    public HtmlCompiledMarkdownRule? FindRule(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken);
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
}
