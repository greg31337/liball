using System;
using System.Threading;

namespace html;
/// <summary>Contains one validated selector rule used during HTML-to-Markdown conversion.</summary>
public class HtmlCompiledMarkdownRule : IHtmlCompiledMarkdownRule
{
    internal string selectorSource;
    internal HtmlSelector selector;
    internal HtmlMarkdownRuleAction action;
    internal HtmlCompiledMarkdownRule(string selectorSourceValue, HtmlSelector? selectorValue, HtmlMarkdownRuleAction actionValue)
    {
        if (selectorSourceValue == null)
        {
            throw new ArgumentNullException("selectorSourceValue");
        }

        if (selectorSourceValue.Length == 0)
        {
            throw new ArgumentException("A compiled Markdown selector cannot be empty.", "selectorSourceValue");
        }

        if (selectorValue == null)
        {
            throw new ArgumentNullException("selectorValue");
        }

        ValidateAction(actionValue);
        this.selectorSource = selectorSourceValue;
        this.selector = selectorValue;
        this.action = actionValue;
    }

    /// <summary>Gets the original CSS selector text used to compile this rule.</summary>
    /// <returns>The non-empty selector source retained for inspection and diagnostics.</returns>
    public string GetSelectorSource()
    {
        return this.selectorSource;
    }

    /// <summary>Gets the reusable selector that determines whether this rule matches a node.</summary>
    /// <returns>The successfully compiled selector.</returns>
    public HtmlSelector GetSelector()
    {
        return this.selector;
    }

    /// <summary>Gets the behavior applied when this rule matches.</summary>
    /// <returns>The configured rule action.</returns>
    public HtmlMarkdownRuleAction GetAction()
    {
        return this.action;
    }

    /// <summary>Tests whether the supplied node matches this compiled Markdown rule.</summary>
    /// <param name = "node">The node to test.</param>
    /// <param name = "scope">The root that defines selector scope and traversal bounds.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns><c>true</c> when the supplied value matches; otherwise, <c>false</c>.</returns>
    public bool Matches(HtmlNode node, HtmlNode scope, CancellationToken cancellationToken)
    {
        return this.selector.MatchesWithCancellation(node, scope, cancellationToken);
    }

    internal static void ValidateAction(HtmlMarkdownRuleAction value)
    {
        if ((int)value < (int)HtmlMarkdownRuleAction.Default || (int)value > (int)HtmlMarkdownRuleAction.Remove)
        {
            throw new ArgumentOutOfRangeException("actionValue", (int)value, "Unknown compiled Markdown rule action.");
        }
    }
}
