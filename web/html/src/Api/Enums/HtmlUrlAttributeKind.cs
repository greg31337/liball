using System;
using System.Threading;

namespace html;
/// <summary>Classifies the value grammar used by a URL-bearing HTML attribute.</summary>
public enum HtmlUrlAttributeKind
{
    /// <summary>Indicates that no option or value applies.</summary>
    None = 0,
    /// <summary>The attribute contains one URL reference.</summary>
    Single = 1,
    /// <summary>The attribute contains a space-separated URL list.</summary>
    SpaceSeparated = 2,
    /// <summary>The attribute contains an HTML <c>srcset</c> candidate list.</summary>
    SourceSet = 3,
    /// <summary>The attribute contains an HTML refresh delay and optional URL.</summary>
    Refresh = 4
}
