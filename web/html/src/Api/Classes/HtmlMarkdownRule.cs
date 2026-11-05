using System;
using System.Threading;

namespace html;
/// <summary>Defines one selector rule for HTML-to-Markdown conversion.</summary>
public class HtmlMarkdownRule : IHtmlMarkdownRule
{
    internal string selector;
    internal HtmlMarkdownRuleAction action;
    internal HtmlMarkdownRule(string selectorValue, HtmlMarkdownRuleAction actionValue)
    {
        if (selectorValue == null)
        {
            throw new ArgumentNullException("selectorValue");
        }

        if (selectorValue.Length == 0)
        {
            throw new ArgumentException("A Markdown rule selector cannot be empty.", "selectorValue");
        }

        ValidateAction(actionValue);
        this.selector = selectorValue;
        this.action = actionValue;
    }

    /// <summary>Creates an instance initialized with the library defaults.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <returns>A new rule that applies the converter's default handling to matched elements.</returns>
    internal static HtmlMarkdownRule CreateDefault(string selector)
    {
        return new HtmlMarkdownRule(selector, HtmlMarkdownRuleAction.Default);
    }

    /// <summary>Creates a rule that keeps elements matched by the selector.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <returns>A new rule that retains matched elements through normal Markdown conversion.</returns>
    internal static HtmlMarkdownRule CreateKeep(string selector)
    {
        return new HtmlMarkdownRule(selector, HtmlMarkdownRuleAction.Keep);
    }

    /// <summary>Creates a rule that removes elements matched by the selector.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <returns>A new rule that omits matched elements and their content.</returns>
    internal static HtmlMarkdownRule CreateRemove(string selector)
    {
        return new HtmlMarkdownRule(selector, HtmlMarkdownRuleAction.Remove);
    }

    /// <summary>Gets the CSS selector text stored for later compilation.</summary>
    /// <returns>The non-empty CSS selector text.</returns>
    public string GetSelector()
    {
        return this.selector;
    }

    /// <summary>Gets the behavior applied when this rule matches.</summary>
    /// <returns>The configured rule action.</returns>
    public HtmlMarkdownRuleAction GetAction()
    {
        return this.action;
    }

    internal HtmlMarkdownRule Copy()
    {
        return new HtmlMarkdownRule(this.selector, this.action);
    }

    internal static void ValidateAction(HtmlMarkdownRuleAction value)
    {
        if ((int)value < (int)HtmlMarkdownRuleAction.Default || (int)value > (int)HtmlMarkdownRuleAction.Remove)
        {
            throw new ArgumentOutOfRangeException("actionValue", (int)value, "Unknown Markdown rule action.");
        }
    }
}
