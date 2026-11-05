using System;

namespace html;
/// <summary>Selects how non-breaking-space characters are represented in plain text.</summary>
public enum HtmlTextNonBreakingSpaceMode
{
    /// <summary>Preserves each non-breaking-space character.</summary>
    Preserve = 0,
    /// <summary>Converts each non-breaking-space character to an ordinary space.</summary>
    ConvertToSpace = 1
}
