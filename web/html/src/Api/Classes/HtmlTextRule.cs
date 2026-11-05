using System;

namespace html;
/// <summary>Defines one selector rule for plain-text conversion.</summary>
public class HtmlTextRule : IHtmlTextRule
{
    internal string selector;
    internal HtmlTextRuleAction action;
    internal string replacement;
    internal HtmlTextRule(string selectorValue, HtmlTextRuleAction actionValue, string replacementValue)
    {
        if (selectorValue == null)
        {
            throw new ArgumentNullException("selectorValue");
        }

        if (selectorValue.Length == 0)
        {
            throw new ArgumentException("A text conversion rule selector cannot be empty.", "selectorValue");
        }

        ValidateAction(actionValue);
        if (replacementValue == null)
        {
            throw new ArgumentNullException("replacementValue");
        }

        if (actionValue != HtmlTextRuleAction.Replace && replacementValue.Length != 0)
        {
            throw new ArgumentException("Only a replace rule can contain replacement text.", "replacementValue");
        }

        this.selector = selectorValue;
        this.action = actionValue;
        this.replacement = replacementValue;
    }

    /// <summary>Creates a rule that includes matched content.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <returns>A new rule that includes content selected by <c>selector</c>.</returns>
    internal static HtmlTextRule CreateInclude(string selector)
    {
        return new HtmlTextRule(selector, HtmlTextRuleAction.Include, "");
    }

    /// <summary>Creates a rule that excludes matched content.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <returns>A new rule that omits content selected by <c>selector</c>.</returns>
    internal static HtmlTextRule CreateExclude(string selector)
    {
        return new HtmlTextRule(selector, HtmlTextRuleAction.Exclude, "");
    }

    /// <summary>Creates a rule that replaces matched content with supplied text.</summary>
    /// <param name = "selector">The non-empty CSS selector text stored until the containing options are compiled.</param>
    /// <param name = "replacement">The text emitted in place of a matched node.</param>
    /// <returns>A new rule that emits <c>replacement</c> instead of matched content.</returns>
    internal static HtmlTextRule CreateReplace(string selector, string replacement)
    {
        return new HtmlTextRule(selector, HtmlTextRuleAction.Replace, replacement);
    }

    /// <summary>Gets the CSS selector text stored for later compilation.</summary>
    /// <returns>The non-empty CSS selector text.</returns>
    public string GetSelector()
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

    internal HtmlTextRule Copy()
    {
        return new HtmlTextRule(this.selector, this.action, this.replacement);
    }

    internal static void ValidateAction(HtmlTextRuleAction value)
    {
        if ((int)value < (int)HtmlTextRuleAction.Include || (int)value > (int)HtmlTextRuleAction.Replace)
        {
            throw new ArgumentOutOfRangeException("actionValue", (int)value, "Unknown text conversion rule action.");
        }
    }
}
