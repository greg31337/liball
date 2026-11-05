using System;
using System.Threading;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlMarkdownRule"/>.</summary>
public interface IHtmlMarkdownRule
{
    /// <summary>Executes the GetSelector operation.</summary>
    public string GetSelector();
    /// <summary>Executes the GetAction operation.</summary>
    public HtmlMarkdownRuleAction GetAction();
}
