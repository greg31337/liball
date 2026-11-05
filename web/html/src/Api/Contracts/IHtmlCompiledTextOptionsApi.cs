using System;
using System.Collections.Generic;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlCompiledTextOptions"/>.</summary>
public interface IHtmlCompiledTextOptionsApi
{
    /// <summary>Executes the GetLinkStyle operation.</summary>
    public HtmlTextLinkStyle GetLinkStyle();
    /// <summary>Executes the GetImageStyle operation.</summary>
    public HtmlTextImageStyle GetImageStyle();
    /// <summary>Executes the GetTableStyle operation.</summary>
    public HtmlTextTableStyle GetTableStyle();
    /// <summary>Gets whether ordinary text collapses or preserves source whitespace.</summary>
    public HtmlTextWhitespaceMode GetWhitespaceMode();
    /// <summary>Gets how non-breaking spaces are represented.</summary>
    public HtmlTextNonBreakingSpaceMode GetNonBreakingSpaceMode();
    /// <summary>Executes the GetWrapColumn operation.</summary>
    public int GetWrapColumn();
    /// <summary>Executes the GetTruncationMarker operation.</summary>
    public string GetTruncationMarker();
    /// <summary>Executes the GetNewLine operation.</summary>
    public string GetNewLine();
    /// <summary>Executes the GetTrim operation.</summary>
    public bool GetTrim();
    /// <summary>Executes the GetMaximumBlankLines operation.</summary>
    public int GetMaximumBlankLines();
    /// <summary>Gets the number of line endings requested at a line boundary.</summary>
    public int GetLineBoundaryBreaks();
    /// <summary>Gets the number of line endings requested at a block boundary.</summary>
    public int GetBlockBoundaryBreaks();
    /// <summary>Executes the GetUnorderedListBullets operation.</summary>
    public string[] GetUnorderedListBullets();
    /// <summary>Executes the GetMaximumListDepth operation.</summary>
    public int GetMaximumListDepth();
    /// <summary>Executes the GetIncludeFormControls operation.</summary>
    public bool GetIncludeFormControls();
    /// <summary>Reports whether hidden and inert element content contributes to output.</summary>
    public bool GetIncludeHiddenContent();
    /// <summary>Gets the base URL used to resolve links and media in string conversion.</summary>
    public string GetBaseUrl();
    /// <summary>Executes the GetRules operation.</summary>
    public HtmlCompiledTextRule[] GetRules();
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
}
