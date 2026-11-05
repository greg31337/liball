using System;
using System.Collections.Generic;

namespace html;
/// <summary>Classifies a parsed HTML dimension as a number, percentage, or length.</summary>
public enum HtmlDimensionUnit
{
    /// <summary>Interprets the dimension as a length value.</summary>
    Length = 0,
    /// <summary>Interprets the dimension as a percentage.</summary>
    Percentage = 1
}
