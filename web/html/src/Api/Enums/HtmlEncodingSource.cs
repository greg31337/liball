using System;

namespace html;
/// <summary>Identifies which byte-order mark, declaration, transport hint, or fallback selected an encoding.</summary>
public enum HtmlEncodingSource
{
    /// <summary>Indicates that the configured fallback encoding was used.</summary>
    Default = 0,
    /// <summary>Indicates that an HTML <c>meta</c> declaration selected the encoding.</summary>
    Meta = 1,
    /// <summary>Indicates that an XML-style declaration supplied the encoding hint.</summary>
    XmlDeclaration = 2,
    /// <summary>Indicates that a transport-layer label selected the encoding.</summary>
    Transport = 3,
    /// <summary>Indicates that a byte-order mark selected the encoding.</summary>
    Bom = 4,
    /// <summary>Indicates that the caller explicitly selected the encoding.</summary>
    Explicit = 5
}
