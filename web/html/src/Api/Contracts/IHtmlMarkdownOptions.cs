using System;
using System.Collections.Generic;
using System.Threading;

namespace html;
/// <summary>Provides Markdown conversion settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlMarkdownOptions
{
    /// <summary>Returns validated Markdown settings ready for one or more conversions.</summary>
    /// <returns>An immutable compiled configuration.</returns>
    public HtmlCompiledMarkdownOptions Compile();
}
