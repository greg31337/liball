using System;

namespace html;
/// <summary>Selects whether ordinary HTML text collapses or preserves source whitespace.</summary>
public enum HtmlTextWhitespaceMode
{
    /// <summary>Collapses runs of HTML whitespace to one separator.</summary>
    Collapse = 0,
    /// <summary>Preserves ordinary source whitespace while normalizing line endings.</summary>
    Preserve = 1
}
