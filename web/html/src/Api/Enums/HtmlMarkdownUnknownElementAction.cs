using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Selects how HTML elements without a conversion rule are handled.</summary>
public enum HtmlMarkdownUnknownElementAction
{
    /// <summary>Drops an unknown element's tags while retaining converted child content.</summary>
    Unwrap = 0,
    /// <summary>Drops an unknown element together with its descendants.</summary>
    Drop = 1,
    /// <summary>Emits an unknown element as raw HTML; use only with trusted input.</summary>
    PreserveHtml = 2,
    /// <summary>Emits an unknown element's markup as escaped text.</summary>
    EscapeHtml = 3,
    /// <summary>Emits only the text content of an unknown element.</summary>
    Text = 4,
    /// <summary>Stops conversion with a diagnostic when an unknown element is encountered.</summary>
    Error = 5
}
