using System;

namespace html;
/// <summary>Exposes immutable compiled pipeline settings.</summary>
public interface IHtmlCompiledProcessingOptionsApi
{
    /// <summary>Reports whether metadata extraction is enabled.</summary>
    public bool GetExtractMetadata();
    /// <summary>Reports whether article extraction is enabled.</summary>
    public bool GetExtractArticle();
    /// <summary>Reports whether sanitization is enabled.</summary>
    public bool GetSanitize();
    /// <summary>Reports whether HTML serialization is enabled.</summary>
    public bool GetSerializeHtml();
    /// <summary>Reports whether plain-text conversion is enabled.</summary>
    public bool GetConvertText();
    /// <summary>Reports whether Markdown conversion is enabled.</summary>
    public bool GetConvertMarkdown();
    /// <summary>Gets a defensive parse-options snapshot.</summary>
    public HtmlParseOptions GetParseOptions();
    /// <summary>Gets a defensive metadata-options snapshot.</summary>
    public HtmlMetadataOptions GetMetadataOptions();
    /// <summary>Gets an immutable sanitizer-policy snapshot.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy();
    /// <summary>Gets a defensive serializer-options snapshot.</summary>
    public HtmlSerializerOptions GetSerializerOptions();
    /// <summary>Gets an immutable text-options snapshot.</summary>
    public HtmlCompiledTextOptions GetTextOptions();
    /// <summary>Gets an immutable Markdown-options snapshot.</summary>
    public HtmlCompiledMarkdownOptions? GetMarkdownOptions();
    /// <summary>Gets a defensive article-options snapshot.</summary>
    public HtmlArticleOptions GetArticleOptions();
    /// <summary>Gets a defensive copy of the shared limits.</summary>
    public HtmlLimits GetLimits();
}
