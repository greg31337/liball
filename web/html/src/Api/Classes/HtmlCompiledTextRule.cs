using System;

namespace html;
/// <summary>Contains one validated selector rule used during plain-text conversion.</summary>
public class HtmlCompiledTextRule : IHtmlCompiledTextRule
{
    internal string selectorSource;
    internal HtmlSelector selector;
    internal HtmlTextRuleAction action;
    internal string replacement;
    internal HtmlCompiledTextRule(string selectorSourceValue, HtmlSelector? selectorValue, HtmlTextRuleAction actionValue, string replacementValue)
    {
        if (selectorSourceValue == null)
        {
            throw new ArgumentNullException("selectorSourceValue");
        }

        if (selectorSourceValue.Length == 0)
        {
            throw new ArgumentException("A compiled text rule selector cannot be empty.", "selectorSourceValue");
        }

        if (selectorValue == null)
        {
            throw new ArgumentNullException("selectorValue");
        }

        if (replacementValue == null)
        {
            throw new ArgumentNullException("replacementValue");
        }

        if ((int)actionValue < (int)HtmlTextRuleAction.Include || (int)actionValue > (int)HtmlTextRuleAction.Replace)
        {
            throw new ArgumentOutOfRangeException("actionValue", (int)actionValue, "Unknown text conversion rule action.");
        }

        if (actionValue != HtmlTextRuleAction.Replace && replacementValue.Length != 0)
        {
            throw new ArgumentException("Only a compiled replace rule can contain replacement text.", "replacementValue");
        }

        this.selectorSource = selectorSourceValue;
        this.selector = selectorValue;
        this.action = actionValue;
        this.replacement = replacementValue;
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
    public HtmlTextRuleAction GetAction()
    {
        return this.action;
    }

    /// <summary>Gets the literal text emitted by a replacement rule.</summary>
    /// <returns>The replacement text, or an empty string when the action is not <see cref = "HtmlTextRuleAction.Replace"/>.</returns>
    public string GetReplacement()
    {
        return this.replacement;
    }

    /// <summary>Tests whether the supplied node matches this compiled text rule.</summary>
    /// <param name = "node">The node to test.</param>
    /// <param name = "scope">The root that defines selector scope and traversal bounds.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns><c>true</c> when the supplied value matches; otherwise, <c>false</c>.</returns>
    public bool Matches(HtmlNode node, HtmlNode scope, System.Threading.CancellationToken cancellationToken)
    {
        return this.selector.MatchesWithCancellation(node, scope, cancellationToken);
    }
}
