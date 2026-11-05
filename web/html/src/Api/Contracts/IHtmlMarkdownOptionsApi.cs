using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlMarkdownOptions"/>.</summary>
public interface IHtmlMarkdownOptionsApi
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlMarkdownOptions Copy();
    /// <summary>Executes the CompileWithDiagnostics operation.</summary>
    public HtmlMarkdownCompileResult CompileWithDiagnostics();
    /// <summary>Executes the GetHeadingStyle operation.</summary>
    public HtmlMarkdownHeadingStyle GetHeadingStyle();
    /// <summary>Executes the SetHeadingStyle operation.</summary>
    public void SetHeadingStyle(HtmlMarkdownHeadingStyle value);
    /// <summary>Gets the delimiter used for emphasized text.</summary>
    public string GetEmphasisMarker();
    /// <summary>Sets the delimiter used for emphasized text.</summary>
    public void SetEmphasisMarker(string value);
    /// <summary>Gets the delimiter used for strongly emphasized text.</summary>
    public string GetStrongMarker();
    /// <summary>Sets the delimiter used for strongly emphasized text.</summary>
    public void SetStrongMarker(string value);
    /// <summary>Gets the syntax used for hard line breaks.</summary>
    public HtmlMarkdownHardBreakStyle GetHardBreakStyle();
    /// <summary>Sets the syntax used for hard line breaks.</summary>
    public void SetHardBreakStyle(HtmlMarkdownHardBreakStyle value);
    /// <summary>Gets how fenced code block delimiters are selected.</summary>
    public HtmlMarkdownCodeFenceStyle GetCodeFenceStyle();
    /// <summary>Sets how fenced code block delimiters are selected.</summary>
    public void SetCodeFenceStyle(HtmlMarkdownCodeFenceStyle value);
    /// <summary>Gets the Markdown marker emitted for thematic breaks.</summary>
    public string GetThematicBreakMarker();
    /// <summary>Sets the Markdown marker emitted for thematic breaks.</summary>
    public void SetThematicBreakMarker(string value);
    /// <summary>Executes the GetLinkStyle operation.</summary>
    public HtmlMarkdownLinkStyle GetLinkStyle();
    /// <summary>Executes the SetLinkStyle operation.</summary>
    public void SetLinkStyle(HtmlMarkdownLinkStyle value);
    /// <summary>Gets the fallback used when a link or image destination is rejected.</summary>
    public HtmlMarkdownUnsafeLinkFallback GetUnsafeLinkFallback();
    /// <summary>Sets the fallback used when a link or image destination is rejected.</summary>
    public void SetUnsafeLinkFallback(HtmlMarkdownUnsafeLinkFallback value);
    /// <summary>Gets the base URL used to resolve links and images in string conversion.</summary>
    public string GetBaseUrl();
    /// <summary>Sets the base URL used to resolve links and images in string conversion.</summary>
    public void SetBaseUrl(string value);
    /// <summary>Executes the GetUnknownElementAction operation.</summary>
    public HtmlMarkdownUnknownElementAction GetUnknownElementAction();
    /// <summary>Executes the SetUnknownElementAction operation.</summary>
    public void SetUnknownElementAction(HtmlMarkdownUnknownElementAction value);
    /// <summary>Executes the GetOrderedListNumbering operation.</summary>
    public HtmlMarkdownOrderedListNumbering GetOrderedListNumbering();
    /// <summary>Executes the SetOrderedListNumbering operation.</summary>
    public void SetOrderedListNumbering(HtmlMarkdownOrderedListNumbering value);
    /// <summary>Executes the GetUnorderedListMarker operation.</summary>
    public string GetUnorderedListMarker();
    /// <summary>Executes the SetUnorderedListMarker operation.</summary>
    public void SetUnorderedListMarker(string value);
    /// <summary>Executes the GetEnableTables operation.</summary>
    public bool GetEnableTables();
    /// <summary>Executes the SetEnableTables operation.</summary>
    public void SetEnableTables(bool value);
    /// <summary>Executes the GetEnableStrikethrough operation.</summary>
    public bool GetEnableStrikethrough();
    /// <summary>Executes the SetEnableStrikethrough operation.</summary>
    public void SetEnableStrikethrough(bool value);
    /// <summary>Executes the GetEnableTaskLists operation.</summary>
    public bool GetEnableTaskLists();
    /// <summary>Executes the SetEnableTaskLists operation.</summary>
    public void SetEnableTaskLists(bool value);
    /// <summary>Executes the GetEnableAutolinks operation.</summary>
    public bool GetEnableAutolinks();
    /// <summary>Executes the SetEnableAutolinks operation.</summary>
    public void SetEnableAutolinks(bool value);
    /// <summary>Executes the GetEnableDefinitionLists operation.</summary>
    public bool GetEnableDefinitionLists();
    /// <summary>Executes the SetEnableDefinitionLists operation.</summary>
    public void SetEnableDefinitionLists(bool value);
    /// <summary>Executes the GetEnableFootnotes operation.</summary>
    public bool GetEnableFootnotes();
    /// <summary>Executes the SetEnableFootnotes operation.</summary>
    public void SetEnableFootnotes(bool value);
    /// <summary>Executes the GetEnableSubscript operation.</summary>
    public bool GetEnableSubscript();
    /// <summary>Executes the SetEnableSubscript operation.</summary>
    public void SetEnableSubscript(bool value);
    /// <summary>Executes the GetEnableSuperscript operation.</summary>
    public bool GetEnableSuperscript();
    /// <summary>Executes the SetEnableSuperscript operation.</summary>
    public void SetEnableSuperscript(bool value);
    /// <summary>Executes the GetEnableTrustedRawHtml operation.</summary>
    public bool GetEnableTrustedRawHtml();
    /// <summary>Executes the SetEnableTrustedRawHtml operation.</summary>
    public void SetEnableTrustedRawHtml(bool value);
    /// <summary>Executes the GetNewLine operation.</summary>
    public string GetNewLine();
    /// <summary>Executes the SetNewLine operation.</summary>
    public void SetNewLine(string value);
    /// <summary>Executes the GetTruncationMarker operation.</summary>
    public string GetTruncationMarker();
    /// <summary>Executes the SetTruncationMarker operation.</summary>
    public void SetTruncationMarker(string value);
    /// <summary>Executes the AddRule operation.</summary>
    public void AddRule(HtmlMarkdownRule value);
    /// <summary>Executes the Keep operation.</summary>
    public void Keep(string selector);
    /// <summary>Executes the Remove operation.</summary>
    public void Remove(string selector);
    /// <summary>Executes the UseDefault operation.</summary>
    public void UseDefault(string selector);
    /// <summary>Executes the GetRules operation.</summary>
    public HtmlMarkdownRule[] GetRules();
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
