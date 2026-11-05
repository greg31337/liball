using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlCompiledMarkdownRule"/>.</summary>
public interface IHtmlCompiledMarkdownRule
{
    /// <summary>Executes the GetSelectorSource operation.</summary>
    public string GetSelectorSource();
    /// <summary>Executes the GetSelector operation.</summary>
    public HtmlSelector GetSelector();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlMarkdownRuleAction GetAction();
    /// <summary>Executes the Matches operation.</summary>
    public bool Matches(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken);
}
