using System;

namespace html;
/// <summary>Selects safe fallback output when a link or image destination is rejected.</summary>
public enum HtmlMarkdownUnsafeLinkFallback
{
    /// <summary>Emits the escaped link label or image alternative text without a destination.</summary>
    Text = 0,
    /// <summary>Omits the rejected link or image entirely.</summary>
    Omit = 1
}
