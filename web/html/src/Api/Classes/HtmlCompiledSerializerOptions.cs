using System;

namespace html;
/// <summary>Contains a validated immutable snapshot of serializer settings.</summary>
public class HtmlCompiledSerializerOptions : IHtmlSerializerConfiguration, IHtmlCompiledSerializerOptionsApi
{
    internal HtmlSerializerOptions options;

    internal HtmlCompiledSerializerOptions(HtmlSerializerOptions optionsValue)
    {
        if (optionsValue == null)
        {
            throw new ArgumentNullException("optionsValue");
        }

        optionsValue.Validate();
        this.options = optionsValue.Copy();
    }

    /// <summary>Returns this validated immutable configuration.</summary>
    public HtmlCompiledSerializerOptions Compile()
    {
        return this;
    }

    /// <summary>Gets the name serialization mode.</summary>
    public HtmlSerializerNameMode GetNameMode()
    {
        return this.options.GetNameMode();
    }

    /// <summary>Gets the attribute quote mode.</summary>
    public HtmlSerializerQuoteMode GetQuoteMode()
    {
        return this.options.GetQuoteMode();
    }

    /// <summary>Reports whether non-ASCII characters are escaped.</summary>
    public bool GetAsciiOnly()
    {
        return this.options.GetAsciiOnly();
    }

    /// <summary>Reports whether attributes are sorted.</summary>
    public bool GetSortAttributes()
    {
        return this.options.GetSortAttributes();
    }

    /// <summary>Reports whether foreign elements use self-closing syntax.</summary>
    public bool GetSelfCloseForeignElements()
    {
        return this.options.GetSelfCloseForeignElements();
    }

    /// <summary>Reports whether pretty printing is enabled.</summary>
    public bool GetPretty()
    {
        return this.options.GetPretty();
    }

    /// <summary>Reports whether optional tags are omitted.</summary>
    public bool GetOmitOptionalTags()
    {
        return this.options.GetOmitOptionalTags();
    }

    /// <summary>Gets the indentation text.</summary>
    public string GetIndentation()
    {
        return this.options.GetIndentation();
    }

    /// <summary>Gets the line-ending text.</summary>
    public string GetNewLine()
    {
        return this.options.GetNewLine();
    }

    /// <summary>Gets a defensive copy of the resource limits.</summary>
    public HtmlLimits GetLimits()
    {
        return this.options.GetLimits();
    }

    internal HtmlSerializerOptions CreateOptions()
    {
        return this.options.Copy();
    }
}
