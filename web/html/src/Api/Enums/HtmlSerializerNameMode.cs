using System;

namespace html;
/// <summary>Selects whether serialization canonicalizes HTML names or preserves their stored spelling.</summary>
public enum HtmlSerializerNameMode
{
    /// <summary>Normalizes element and attribute names for canonical HTML output.</summary>
    Canonical = 0,
    /// <summary>Preserves the stored spelling of names where serialization permits it.</summary>
    Preserve = 1
}
