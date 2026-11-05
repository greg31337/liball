using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlTextRule"/>.</summary>
public interface IHtmlTextRule
{
    /// <summary>Executes the GetSelector operation.</summary>
    public string GetSelector();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlTextRuleAction GetAction();
    /// <summary>Executes the GetReplacement operation.</summary>
    public string GetReplacement();
}
