using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Selects ATX or Setext Markdown heading syntax.</summary>
public enum HtmlMarkdownHeadingStyle
{
    /// <summary>Emits headings with leading hash markers.</summary>
    Atx = 0,
    /// <summary>Emits level-one and level-two headings with underline markers.</summary>
    Setext = 1
}
