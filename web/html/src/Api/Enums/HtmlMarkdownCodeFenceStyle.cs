using System;

namespace html;
/// <summary>Selects adaptive, backtick, or tilde fenced-code-block syntax.</summary>
public enum HtmlMarkdownCodeFenceStyle
{
    /// <summary>Chooses the shortest valid fence for the content and info string.</summary>
    Adaptive = 0,
    /// <summary>Prefers a backtick fence and uses tildes when the info string forbids backticks.</summary>
    Backticks = 1,
    /// <summary>Uses a tilde fence.</summary>
    Tildes = 2
}
