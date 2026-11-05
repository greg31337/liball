using System;

namespace html;
/// <summary>Exposes immutable compiled serializer settings.</summary>
public interface IHtmlCompiledSerializerOptionsApi
{
    /// <summary>Gets the name serialization mode.</summary>
    public HtmlSerializerNameMode GetNameMode();
    /// <summary>Gets the attribute quote mode.</summary>
    public HtmlSerializerQuoteMode GetQuoteMode();
    /// <summary>Reports whether non-ASCII characters are escaped.</summary>
    public bool GetAsciiOnly();
    /// <summary>Reports whether attributes are sorted.</summary>
    public bool GetSortAttributes();
    /// <summary>Reports whether foreign elements use self-closing syntax.</summary>
    public bool GetSelfCloseForeignElements();
    /// <summary>Reports whether pretty printing is enabled.</summary>
    public bool GetPretty();
    /// <summary>Reports whether optional tags are omitted.</summary>
    public bool GetOmitOptionalTags();
    /// <summary>Gets the indentation text.</summary>
    public string GetIndentation();
    /// <summary>Gets the line-ending text.</summary>
    public string GetNewLine();
    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits();
}
