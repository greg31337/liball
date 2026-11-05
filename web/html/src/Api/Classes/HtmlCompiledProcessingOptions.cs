using System;

namespace html;
/// <summary>Contains a validated immutable snapshot of pipeline settings.</summary>
public class HtmlCompiledProcessingOptions : IHtmlProcessingConfiguration, IHtmlCompiledProcessingOptionsApi
{
    internal bool extractMetadata;
    internal bool extractArticle;
    internal bool sanitize;
    internal bool serializeHtml;
    internal bool convertText;
    internal bool convertMarkdown;
    internal HtmlParseOptions parseOptions;
    internal HtmlMetadataOptions metadataOptions;
    internal HtmlCompiledSanitizerPolicy sanitizerPolicy;
    internal HtmlSerializerOptions serializerOptions;
    internal HtmlCompiledTextOptions textOptions;
    internal HtmlCompiledMarkdownOptions? markdownOptions;
    internal HtmlArticleOptions articleOptions;
    internal HtmlLimits limits;

    internal HtmlCompiledProcessingOptions(bool extractMetadataValue,
        bool extractArticleValue, bool sanitizeValue,
        bool serializeHtmlValue, bool convertTextValue,
        bool convertMarkdownValue, HtmlParseOptions parseOptionsValue,
        HtmlMetadataOptions metadataOptionsValue,
        HtmlCompiledSanitizerPolicy sanitizerPolicyValue,
        HtmlSerializerOptions serializerOptionsValue,
        HtmlCompiledTextOptions textOptionsValue,
        HtmlCompiledMarkdownOptions? markdownOptionsValue,
        HtmlArticleOptions articleOptionsValue, HtmlLimits limitsValue)
    {
        this.extractMetadata = extractMetadataValue;
        this.extractArticle = extractArticleValue;
        this.sanitize = sanitizeValue;
        this.serializeHtml = serializeHtmlValue;
        this.convertText = convertTextValue;
        this.convertMarkdown = convertMarkdownValue;
        this.parseOptions = parseOptionsValue.Copy();
        this.metadataOptions = metadataOptionsValue.Copy();
        this.sanitizerPolicy = sanitizerPolicyValue.CreatePolicy().Compile();
        this.serializerOptions = serializerOptionsValue.Copy();
        this.textOptions = textOptionsValue;
        this.markdownOptions = markdownOptionsValue;
        this.articleOptions = articleOptionsValue.Copy();
        this.limits = limitsValue.Copy();
    }

    /// <summary>Returns this validated immutable configuration.</summary>
    public HtmlCompiledProcessingOptions Compile()
    {
        return this;
    }

    /// <summary>Reports whether metadata extraction is enabled.</summary>
    public bool GetExtractMetadata()
    {
        return this.extractMetadata;
    }

    /// <summary>Reports whether article extraction is enabled.</summary>
    public bool GetExtractArticle()
    {
        return this.extractArticle;
    }

    /// <summary>Reports whether sanitization is enabled.</summary>
    public bool GetSanitize()
    {
        return this.sanitize;
    }

    /// <summary>Reports whether HTML serialization is enabled.</summary>
    public bool GetSerializeHtml()
    {
        return this.serializeHtml;
    }

    /// <summary>Reports whether plain-text conversion is enabled.</summary>
    public bool GetConvertText()
    {
        return this.convertText;
    }

    /// <summary>Reports whether Markdown conversion is enabled.</summary>
    public bool GetConvertMarkdown()
    {
        return this.convertMarkdown;
    }

    /// <summary>Gets a defensive parse-options snapshot.</summary>
    public HtmlParseOptions GetParseOptions()
    {
        return this.parseOptions.Copy();
    }

    /// <summary>Gets an immutable sanitizer-policy snapshot.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy()
    {
        return this.sanitizerPolicy.CreatePolicy().Compile();
    }

    /// <summary>Gets a defensive metadata-options snapshot.</summary>
    public HtmlMetadataOptions GetMetadataOptions()
    {
        return this.metadataOptions.Copy();
    }

    /// <summary>Gets a defensive serializer-options snapshot.</summary>
    public HtmlSerializerOptions GetSerializerOptions()
    {
        return this.serializerOptions.Copy();
    }

    /// <summary>Gets an immutable text-options snapshot.</summary>
    public HtmlCompiledTextOptions GetTextOptions()
    {
        return this.textOptions;
    }

    /// <summary>Gets an immutable Markdown-options snapshot.</summary>
    public HtmlCompiledMarkdownOptions? GetMarkdownOptions()
    {
        return this.markdownOptions;
    }

    /// <summary>Gets a defensive article-options snapshot.</summary>
    public HtmlArticleOptions GetArticleOptions()
    {
        return this.articleOptions.Copy();
    }

    /// <summary>Gets a defensive copy of the shared limits.</summary>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }
}
