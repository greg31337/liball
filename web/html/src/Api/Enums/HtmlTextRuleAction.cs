using System;

namespace html;
/// <summary>Selects how a matched element is handled during plain-text conversion.</summary>
public enum HtmlTextRuleAction
{
    /// <summary>Includes matched content using normal text-conversion rules.</summary>
    Include = 0,
    /// <summary>Excludes matched content and its descendants.</summary>
    Exclude = 1,
    /// <summary>Replaces matched content with the rule replacement text.</summary>
    Replace = 2
}
