using System;

namespace html;
/// <summary>Indicates whether the detected encoding is tentative or fixed by an authoritative signal.</summary>
public enum HtmlEncodingConfidence
{
    /// <summary>Marks an encoding selected from a signal that later input may override.</summary>
    Tentative = 0,
    /// <summary>Marks an encoding selected from an authoritative signal.</summary>
    Certain = 1
}
