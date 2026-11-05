using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Selects whether character-reference decoding follows text-state or attribute-value ambiguity rules.</summary>
public enum HtmlEntityContext
{
    /// <summary>Applies character-reference rules for HTML text content.</summary>
    Text = 0,
    /// <summary>Applies character-reference rules for an HTML attribute value.</summary>
    Attribute = 1
}
