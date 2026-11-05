using System;

namespace html;
/// <summary>Configures HTML processing behavior, limits, and output choices.</summary>
public class HtmlProcessingOptions : IHtmlProcessingOptions, IHtmlProcessingConfiguration
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
    internal HtmlTextOptions textOptions;
    internal HtmlMarkdownOptions markdownOptions;
    internal HtmlArticleOptions articleOptions;
    internal HtmlLimits limits;
    /// <summary>Creates processing options initialized for bounded rich-text sanitization.</summary>
    public HtmlProcessingOptions()
    {
        this.extractMetadata = false;
        this.extractArticle = false;
        this.sanitize = true;
        this.serializeHtml = true;
        this.convertText = false;
        this.convertMarkdown = false;
        this.limits = HtmlLimits.CreateUntrusted();
        this.parseOptions = HtmlParseOptions.CreateDefault();
        this.metadataOptions = new HtmlMetadataOptions();
        this.sanitizerPolicy = HtmlSanitizerPolicy.CreateRichText().Compile();
        this.serializerOptions = HtmlSerializerOptions.CreateCanonical();
        this.textOptions = HtmlTextOptions.CreateDefault();
        this.markdownOptions = HtmlMarkdownOptions.CreateCommonMark();
        this.articleOptions = HtmlArticleOptions.CreateDefault();
        this.ApplyLimits();
    }

    /// <summary>Creates pipeline settings for sanitizing and converting untrusted rich text.</summary>
    /// <returns>A new pipeline configuration with sanitization and HTML serialization enabled.</returns>
    internal static HtmlProcessingOptions CreateUntrustedRichText()
    {
        return new HtmlProcessingOptions();
    }

    /// <summary>Creates pipeline settings for article extraction.</summary>
    /// <returns>A new pipeline configuration that also extracts metadata, article content, text, and Markdown.</returns>
    internal static HtmlProcessingOptions CreateArticle()
    {
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.extractMetadata = true;
        options.extractArticle = true;
        options.convertText = true;
        options.convertMarkdown = true;
        options.markdownOptions = HtmlMarkdownOptions.CreateReadable();
        options.ApplyLimits();
        return options;
    }

    /// <summary>Creates plain-text settings suitable for search indexing.</summary>
    /// <returns>A new pipeline configuration that extracts metadata and normalized text without serializing HTML.</returns>
    internal static HtmlProcessingOptions CreateSearchIndex()
    {
        HtmlProcessingOptions options = new HtmlProcessingOptions();
        options.extractMetadata = true;
        options.serializeHtml = false;
        options.convertText = true;
        options.textOptions = HtmlTextOptions.CreateSearchIndex();
        options.ApplyLimits();
        return options;
    }

    /// <summary>Creates pipeline settings for bounded text and Markdown ingestion.</summary>
    /// <returns>A new bounded pipeline configuration that emits article, text, and Markdown data without serialized HTML.</returns>
    internal static HtmlProcessingOptions CreateLlmIngestion()
    {
        HtmlProcessingOptions options = CreateArticle();
        options.serializeHtml = false;
        options.textOptions.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
        options.textOptions.SetImageStyle(HtmlTextImageStyle.AltTextAndUrl);
        options.ApplyLimits();
        return options;
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent pipeline-options object containing copies of the current settings.</returns>
    public HtmlProcessingOptions Copy()
    {
        HtmlProcessingOptions copy = new HtmlProcessingOptions();
        copy.extractMetadata = this.extractMetadata;
        copy.extractArticle = this.extractArticle;
        copy.sanitize = this.sanitize;
        copy.serializeHtml = this.serializeHtml;
        copy.convertText = this.convertText;
        copy.convertMarkdown = this.convertMarkdown;
        copy.limits = this.limits.Copy();
        copy.parseOptions = this.parseOptions.Copy();
        copy.metadataOptions = this.metadataOptions.Copy();
        copy.sanitizerPolicy = CopyPolicy(this.sanitizerPolicy);
        copy.serializerOptions = this.serializerOptions.Copy();
        copy.textOptions = this.textOptions.Copy();
        copy.markdownOptions = this.markdownOptions.Copy();
        copy.articleOptions = this.articleOptions.Copy();
        copy.ApplyLimits();
        return copy;
    }

    internal void Validate()
    {
        if (this.parseOptions == null || this.metadataOptions == null
            || this.sanitizerPolicy == null
            || this.serializerOptions == null || this.textOptions == null
            || this.markdownOptions == null || this.articleOptions == null
            || this.limits == null)
        {
            throw new InvalidOperationException("Processing options contain a missing required value.");
        }

        this.limits.Validate();
        this.parseOptions.Validate();
        this.metadataOptions.Validate();
        this.serializerOptions.Validate();
        this.textOptions.Validate();
        this.markdownOptions.Validate();
        this.articleOptions.Validate();
        if (this.serializeHtml && !this.sanitize)
        {
            throw new InvalidOperationException("Renderable pipeline HTML requires sanitization.");
        }
    }

    /// <summary>Creates a validated immutable snapshot for concurrent reuse.</summary>
    /// <returns>An immutable compiled pipeline configuration.</returns>
    public HtmlCompiledProcessingOptions Compile()
    {
        this.Validate();
        HtmlParseOptions compiledParse = this.parseOptions.Copy();
        compiledParse.SetLimits(this.limits);
        HtmlMetadataOptions compiledMetadata = this.metadataOptions.Copy();
        compiledMetadata.SetLimits(this.limits);
        HtmlSerializerOptions compiledSerializer =
            this.serializerOptions.Copy();
        compiledSerializer.SetLimits(this.limits);
        HtmlTextOptions compiledText = this.textOptions.Copy();
        compiledText.SetLimits(this.limits);
        HtmlMarkdownOptions compiledMarkdown = this.markdownOptions.Copy();
        compiledMarkdown.SetLimits(this.limits);
        HtmlArticleOptions compiledArticle = this.articleOptions.Copy();
        compiledArticle.SetLimits(this.limits);
        return new HtmlCompiledProcessingOptions(this.extractMetadata, this.extractArticle, this.sanitize, this.serializeHtml, this.convertText, this.convertMarkdown, compiledParse, compiledMetadata, CopyPolicy(this.sanitizerPolicy), compiledSerializer, compiledText.Compile(), compiledMarkdown.Compile(), compiledArticle, this.limits);
    }

    /// <summary>Reports whether metadata extraction is enabled.</summary>
    /// <returns><c>true</c> when metadata extraction is enabled; otherwise, <c>false</c>.</returns>
    public bool GetExtractMetadata()
    {
        return this.extractMetadata;
    }

    /// <summary>Controls whether the pipeline extracts document metadata.</summary>
    /// <param name = "value"><c>true</c> to collect title, URL, author, image, and related document metadata.</param>
    public void SetExtractMetadata(bool value)
    {
        this.extractMetadata = value;
    }

    /// <summary>Reports whether article extraction is enabled.</summary>
    /// <returns><c>true</c> when article extraction is enabled; otherwise, <c>false</c>.</returns>
    public bool GetExtractArticle()
    {
        return this.extractArticle;
    }

    /// <summary>Controls whether the pipeline extracts readable article content.</summary>
    /// <param name = "value"><c>true</c> to score and extract the document's likely readable article content.</param>
    public void SetExtractArticle(bool value)
    {
        this.extractArticle = value;
    }

    /// <summary>Reports whether sanitization is enabled.</summary>
    /// <returns><c>true</c> when the sanitizer stage is enabled; otherwise, <c>false</c>.</returns>
    public bool GetSanitize()
    {
        return this.sanitize;
    }

    /// <summary>Enables or disables the sanitizer stage.</summary>
    /// <param name = "value"><c>true</c> to apply the configured sanitizer policy before renderable output is returned.</param>
    public void SetSanitize(bool value)
    {
        this.sanitize = value;
    }

    /// <summary>Reports whether serialized HTML output is enabled.</summary>
    /// <returns><c>true</c> when serialized HTML output is requested; otherwise, <c>false</c>.</returns>
    public bool GetSerializeHtml()
    {
        return this.serializeHtml;
    }

    /// <summary>Controls whether the pipeline produces serialized HTML output.</summary>
    /// <param name = "value"><c>true</c> to serialize the pipeline's final DOM as HTML.</param>
    public void SetSerializeHtml(bool value)
    {
        this.serializeHtml = value;
    }

    /// <summary>Reports whether plain-text output is enabled.</summary>
    /// <returns><c>true</c> when plain-text output is requested; otherwise, <c>false</c>.</returns>
    public bool GetConvertText()
    {
        return this.convertText;
    }

    /// <summary>Controls whether the pipeline produces plain-text output.</summary>
    /// <param name = "value"><c>true</c> to convert the pipeline's final DOM to plain text.</param>
    public void SetConvertText(bool value)
    {
        this.convertText = value;
    }

    /// <summary>Reports whether Markdown output is enabled.</summary>
    /// <returns><c>true</c> when Markdown output is requested; otherwise, <c>false</c>.</returns>
    public bool GetConvertMarkdown()
    {
        return this.convertMarkdown;
    }

    /// <summary>Controls whether the pipeline produces Markdown output.</summary>
    /// <param name = "value"><c>true</c> to convert the pipeline's final DOM to Markdown.</param>
    public void SetConvertMarkdown(bool value)
    {
        this.convertMarkdown = value;
    }

    /// <summary>Gets the parsing settings that the pipeline will apply before later stages.</summary>
    /// <returns>An independent copy whose changes do not affect this pipeline configuration.</returns>
    public HtmlParseOptions GetParseOptions()
    {
        return this.parseOptions.Copy();
    }

    /// <summary>Sets the parse options used by subsequent operations.</summary>
    /// <param name = "value">The parsing settings to validate and copy; the pipeline replaces their limits with its shared limits.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetParseOptions(HtmlParseOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.parseOptions = value.Copy();
        this.parseOptions.SetLimits(this.limits);
    }

    /// <summary>Gets the metadata settings used by the extraction stage.</summary>
    /// <returns>An independent mutable copy of the metadata settings.</returns>
    public HtmlMetadataOptions GetMetadataOptions()
    {
        return this.metadataOptions.Copy();
    }

    /// <summary>Sets the metadata settings used by subsequent pipeline runs.</summary>
    /// <param name = "value">The validated metadata settings to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetMetadataOptions(HtmlMetadataOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.metadataOptions = value.Copy();
        this.metadataOptions.SetLimits(this.limits);
    }

    /// <summary>Gets the compiled policy used when the sanitization stage is enabled.</summary>
    /// <returns>An independent immutable snapshot of the configured policy.</returns>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy()
    {
        return CopyPolicy(this.sanitizerPolicy);
    }

    /// <summary>Sets the sanitizer policy used by subsequent operations.</summary>
    /// <param name = "value">The immutable policy to copy for the sanitizer stage.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSanitizerPolicy(HtmlCompiledSanitizerPolicy value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.sanitizerPolicy = CopyPolicy(value);
    }

    /// <summary>Gets the settings used by the HTML serialization stage.</summary>
    /// <returns>An independent mutable serializer-options copy.</returns>
    public HtmlSerializerOptions GetSerializerOptions()
    {
        return this.serializerOptions.Copy();
    }

    /// <summary>Sets the serializer settings used by subsequent pipeline runs.</summary>
    /// <param name = "value">The validated serializer settings to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetSerializerOptions(HtmlSerializerOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.serializerOptions = value.Copy();
        this.serializerOptions.SetLimits(this.limits);
    }

    /// <summary>Gets the settings used when plain-text conversion is enabled.</summary>
    /// <returns>An independent copy whose changes do not affect this pipeline configuration.</returns>
    public HtmlTextOptions GetTextOptions()
    {
        return this.textOptions.Copy();
    }

    /// <summary>Sets the text options used by subsequent operations.</summary>
    /// <param name = "value">The plain-text settings to validate and copy; the pipeline replaces their limits with its shared limits.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetTextOptions(HtmlTextOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.textOptions = value.Copy();
        this.textOptions.SetLimits(this.limits);
    }

    /// <summary>Gets the settings used when Markdown conversion is enabled.</summary>
    /// <returns>An independent copy whose changes do not affect this pipeline configuration.</returns>
    public HtmlMarkdownOptions GetMarkdownOptions()
    {
        return this.markdownOptions.Copy();
    }

    /// <summary>Sets the Markdown options used by subsequent operations.</summary>
    /// <param name = "value">The Markdown settings to validate and copy; the pipeline replaces their limits with its shared limits.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetMarkdownOptions(HtmlMarkdownOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.markdownOptions = value.Copy();
        this.markdownOptions.SetLimits(this.limits);
    }

    /// <summary>Gets the settings used when readable-article extraction is enabled.</summary>
    /// <returns>An independent copy whose changes do not affect this pipeline configuration.</returns>
    public HtmlArticleOptions GetArticleOptions()
    {
        return this.articleOptions.Copy();
    }

    /// <summary>Sets the article options used by subsequent operations.</summary>
    /// <param name = "value">The article-extraction settings to validate and copy; the pipeline replaces their limits with its shared limits.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetArticleOptions(HtmlArticleOptions value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.articleOptions = value.Copy();
        this.articleOptions.SetLimits(this.limits);
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect this object.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the limits used by subsequent operations.</summary>
    /// <param name = "value">The validated shared limits to copy into the pipeline and each configured stage.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
        this.ApplyLimits();
    }

    internal void ApplyLimits()
    {
        this.parseOptions.SetLimits(this.limits);
        this.metadataOptions.SetLimits(this.limits);
        this.serializerOptions.SetLimits(this.limits);
        this.textOptions.SetLimits(this.limits);
        this.markdownOptions.SetLimits(this.limits);
        this.articleOptions.SetLimits(this.limits);
    }

    internal static HtmlCompiledSanitizerPolicy CopyPolicy(HtmlCompiledSanitizerPolicy value)
    {
        return value.CreatePolicy().Compile();
    }
}
