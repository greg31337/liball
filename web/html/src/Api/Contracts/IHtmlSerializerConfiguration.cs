using System;

namespace html;
/// <summary>Provides serializer settings that can be compiled into an immutable snapshot.</summary>
public interface IHtmlSerializerConfiguration
{
    /// <summary>Returns validated serializer settings ready for one or more operations.</summary>
    public HtmlCompiledSerializerOptions Compile();
}
