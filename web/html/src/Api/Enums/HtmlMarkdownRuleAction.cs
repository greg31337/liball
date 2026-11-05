using System;
using System.Threading;

namespace html;
/// <summary>Selects how a matched element is handled during HTML-to-Markdown conversion.</summary>
public enum HtmlMarkdownRuleAction
{
    /// <summary>Uses the converter normal behavior for matched elements.</summary>
    Default = 0,
    /// <summary>Keeps matched elements as trusted raw HTML.</summary>
    Keep = 1,
    /// <summary>Removes matched elements and their descendants from Markdown output.</summary>
    Remove = 2
}
