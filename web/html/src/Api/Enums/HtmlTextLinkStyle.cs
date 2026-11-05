using System;
using System.Collections.Generic;

namespace html;
/// <summary>Selects whether plain-text links emit their label, destination, both, or no content.</summary>
public enum HtmlTextLinkStyle
{
    /// <summary>Emits link text without its destination URL.</summary>
    TextOnly = 0,
    /// <summary>Emits link text followed by its destination URL.</summary>
    TextAndUrl = 1,
    /// <summary>Collects link destinations into numbered footnotes.</summary>
    Footnotes = 2,
    /// <summary>Emits only the link destination URL.</summary>
    UrlOnly = 3
}
