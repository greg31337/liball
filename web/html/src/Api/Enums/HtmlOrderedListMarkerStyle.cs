using System;
using System.Collections.Generic;

namespace html;
/// <summary>Selects the decimal, alphabetic, or Roman marker style inferred for an ordered list.</summary>
public enum HtmlOrderedListMarkerStyle
{
    /// <summary>Formats ordered-list markers with decimal digits.</summary>
    Decimal = 0,
    /// <summary>Formats ordered-list markers with lowercase Latin letters.</summary>
    LowerAlpha = 1,
    /// <summary>Formats ordered-list markers with uppercase Latin letters.</summary>
    UpperAlpha = 2,
    /// <summary>Formats ordered-list markers with lowercase Roman numerals.</summary>
    LowerRoman = 3,
    /// <summary>Formats ordered-list markers with uppercase Roman numerals.</summary>
    UpperRoman = 4
}
