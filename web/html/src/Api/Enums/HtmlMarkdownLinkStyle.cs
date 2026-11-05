using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Selects inline or reference-style Markdown links.</summary>
public enum HtmlMarkdownLinkStyle
{
    /// <summary>Emits link destinations beside their link text.</summary>
    Inline = 0,
    /// <summary>Emits full reference links and definitions.</summary>
    Reference = 1,
    /// <summary>Emits collapsed reference links when the label can identify the definition.</summary>
    CollapsedReference = 2
}
