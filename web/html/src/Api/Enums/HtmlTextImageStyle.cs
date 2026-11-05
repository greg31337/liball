using System;
using System.Collections.Generic;

namespace html;
/// <summary>Selects whether images emit alternative text, source URLs, both, or no content.</summary>
public enum HtmlTextImageStyle
{
    /// <summary>Emits an image's alternative text.</summary>
    AltText = 0,
    /// <summary>Emits image alternative text followed by its source URL.</summary>
    AltTextAndUrl = 1,
    /// <summary>Emits only the link destination URL.</summary>
    UrlOnly = 2,
    /// <summary>Emits a readable placeholder for each image.</summary>
    Placeholder = 3,
    /// <summary>Omits the applicable image or table output.</summary>
    Omit = 4
}
