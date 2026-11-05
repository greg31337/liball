using System;

namespace html;
/// <summary>Selects trailing-space or backslash syntax for Markdown hard line breaks.</summary>
public enum HtmlMarkdownHardBreakStyle
{
    /// <summary>Emits two trailing spaces before the line ending.</summary>
    TwoSpaces = 0,
    /// <summary>Emits a trailing backslash before the line ending.</summary>
    Backslash = 1
}
