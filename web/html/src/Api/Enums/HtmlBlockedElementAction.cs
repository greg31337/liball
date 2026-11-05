using System;

namespace html;
/// <summary>Selects how the sanitizer handles an explicitly blocked element.</summary>
public enum HtmlBlockedElementAction
{
    /// <summary>Removes the matched element and all of its descendants.</summary>
    DropSubtree = 0,
    /// <summary>Removes the matched element but retains its descendants in place.</summary>
    Unwrap = 1,
    /// <summary>Replaces the matched element subtree with its text content.</summary>
    ReplaceWithText = 2,
    /// <summary>Serializes the matched markup and retains it as text.</summary>
    EscapeAsText = 3
}
