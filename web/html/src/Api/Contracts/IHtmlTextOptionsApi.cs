using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTextOptions"/>.</summary>
public interface IHtmlTextOptionsApi
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlTextOptions Copy();
    /// <summary>Executes the GetLinkStyle operation.</summary>
    public HtmlTextLinkStyle GetLinkStyle();
    /// <summary>Executes the SetLinkStyle operation.</summary>
    public void SetLinkStyle(HtmlTextLinkStyle value);
    /// <summary>Executes the GetImageStyle operation.</summary>
    public HtmlTextImageStyle GetImageStyle();
    /// <summary>Executes the SetImageStyle operation.</summary>
    public void SetImageStyle(HtmlTextImageStyle value);
    /// <summary>Executes the GetTableStyle operation.</summary>
    public HtmlTextTableStyle GetTableStyle();
    /// <summary>Executes the SetTableStyle operation.</summary>
    public void SetTableStyle(HtmlTextTableStyle value);
    /// <summary>Gets whether ordinary text collapses or preserves source whitespace.</summary>
    public HtmlTextWhitespaceMode GetWhitespaceMode();
    /// <summary>Sets how ordinary source whitespace is represented.</summary>
    public void SetWhitespaceMode(HtmlTextWhitespaceMode value);
    /// <summary>Gets how non-breaking spaces are represented.</summary>
    public HtmlTextNonBreakingSpaceMode GetNonBreakingSpaceMode();
    /// <summary>Sets how non-breaking spaces are represented.</summary>
    public void SetNonBreakingSpaceMode(HtmlTextNonBreakingSpaceMode value);
    /// <summary>Executes the GetWrapColumn operation.</summary>
    public int GetWrapColumn();
    /// <summary>Executes the SetWrapColumn operation.</summary>
    public void SetWrapColumn(int value);
    /// <summary>Executes the GetTruncationMarker operation.</summary>
    public string GetTruncationMarker();
    /// <summary>Executes the SetTruncationMarker operation.</summary>
    public void SetTruncationMarker(string value);
    /// <summary>Executes the GetNewLine operation.</summary>
    public string GetNewLine();
    /// <summary>Executes the SetNewLine operation.</summary>
    public void SetNewLine(string value);
    /// <summary>Executes the GetTrim operation.</summary>
    public bool GetTrim();
    /// <summary>Executes the SetTrim operation.</summary>
    public void SetTrim(bool value);
    /// <summary>Executes the GetMaximumBlankLines operation.</summary>
    public int GetMaximumBlankLines();
    /// <summary>Executes the SetMaximumBlankLines operation.</summary>
    public void SetMaximumBlankLines(int value);
    /// <summary>Gets the number of line endings requested at a line boundary.</summary>
    public int GetLineBoundaryBreaks();
    /// <summary>Sets the number of line endings requested at a line boundary.</summary>
    public void SetLineBoundaryBreaks(int value);
    /// <summary>Gets the number of line endings requested at a block boundary.</summary>
    public int GetBlockBoundaryBreaks();
    /// <summary>Sets the number of line endings requested at a block boundary.</summary>
    public void SetBlockBoundaryBreaks(int value);
    /// <summary>Executes the GetUnorderedListBullets operation.</summary>
    public string[] GetUnorderedListBullets();
    /// <summary>Executes the SetUnorderedListBullets operation.</summary>
    public void SetUnorderedListBullets(string[] value);
    /// <summary>Executes the GetMaximumListDepth operation.</summary>
    public int GetMaximumListDepth();
    /// <summary>Executes the SetMaximumListDepth operation.</summary>
    public void SetMaximumListDepth(int value);
    /// <summary>Executes the GetIncludeFormControls operation.</summary>
    public bool GetIncludeFormControls();
    /// <summary>Executes the SetIncludeFormControls operation.</summary>
    public void SetIncludeFormControls(bool value);
    /// <summary>Reports whether hidden and inert element content contributes to output.</summary>
    public bool GetIncludeHiddenContent();
    /// <summary>Controls whether hidden and inert element content contributes to output.</summary>
    public void SetIncludeHiddenContent(bool value);
    /// <summary>Gets the base URL used to resolve links and media in string conversion.</summary>
    public string GetBaseUrl();
    /// <summary>Sets the base URL used to resolve links and media in string conversion.</summary>
    public void SetBaseUrl(string value);
    /// <summary>Executes the AddRule operation.</summary>
    public void AddRule(HtmlTextRule value);
    /// <summary>Executes the AddIncludedSelector operation.</summary>
    public void AddIncludedSelector(string selector);
    /// <summary>Executes the AddExcludedSelector operation.</summary>
    public void AddExcludedSelector(string selector);
    /// <summary>Executes the AddReplacementSelector operation.</summary>
    public void AddReplacementSelector(string selector, string replacement);
    /// <summary>Executes the GetRules operation.</summary>
    public HtmlTextRule[] GetRules();
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
