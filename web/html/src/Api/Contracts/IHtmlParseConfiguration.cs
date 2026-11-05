using System;

namespace html;
/// <summary>Provides parse settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlParseConfiguration
{
    /// <summary>Returns validated parse settings ready for one or more operations.</summary>
    public HtmlCompiledParseOptions Compile();
}
