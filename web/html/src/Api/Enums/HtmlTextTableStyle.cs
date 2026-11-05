using System;
using System.Collections.Generic;

namespace html;
/// <summary>Selects reading-order, tab-separated, aligned-grid, or omitted output for tables.</summary>
public enum HtmlTextTableStyle
{
    /// <summary>Emits table cell text in document reading order.</summary>
    ReadingOrder = 0,
    /// <summary>Emits rows as tab-separated values.</summary>
    Tsv = 1,
    /// <summary>Emits a fixed-width text grid when it fits configured limits.</summary>
    Grid = 2,
    /// <summary>Omits the applicable image or table output.</summary>
    Omit = 3
}
