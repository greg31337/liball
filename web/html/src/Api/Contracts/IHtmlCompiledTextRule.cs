using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlCompiledTextRule"/>.</summary>
public interface IHtmlCompiledTextRule
{
    /// <summary>Executes the GetSelectorSource operation.</summary>
    public string GetSelectorSource();
    /// <summary>Executes the GetSelector operation.</summary>
    public HtmlSelector GetSelector();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlTextRuleAction GetAction();
    /// <summary>Executes the GetReplacement operation.</summary>
    public string GetReplacement();
    /// <summary>Executes the Matches operation.</summary>
    public bool Matches(HtmlNode node, HtmlNode scope, System.Threading.CancellationToken cancellationToken);
}
