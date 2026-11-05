using System;

namespace html;
/// <summary>Provides article settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlArticleConfiguration
{
    /// <summary>Returns validated article settings ready for one or more operations.</summary>
    public HtmlCompiledArticleOptions Compile();
}
