using System;

namespace html;
/// <summary>Provides pipeline settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlProcessingConfiguration
{
    /// <summary>Returns validated pipeline settings ready for one or more operations.</summary>
    public HtmlCompiledProcessingOptions Compile();
}
