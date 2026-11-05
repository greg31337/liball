using System;
using System.Collections.Generic;

namespace html;
/// <summary>Provides plain-text conversion settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlTextOptions
{
    /// <summary>Returns validated plain-text settings ready for one or more conversions.</summary>
    /// <returns>An immutable compiled configuration.</returns>
    public HtmlCompiledTextOptions Compile();
}
