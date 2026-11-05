using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Selects how ordered-list item numbers are emitted in Markdown.</summary>
public enum HtmlMarkdownOrderedListNumbering
{
    /// <summary>Preserves ordered-list start values and item numbering.</summary>
    Preserve = 0,
    /// <summary>Renumbers ordered-list items sequentially from their effective start.</summary>
    Sequential = 1,
    /// <summary>Emits every ordered-list item with marker <c>1.</c>.</summary>
    One = 2
}
