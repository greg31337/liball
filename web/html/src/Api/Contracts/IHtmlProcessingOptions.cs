using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlProcessingOptions"/>.</summary>
public interface IHtmlProcessingOptions
{
    /// <summary>Executes the Copy operation.</summary>
    public HtmlProcessingOptions Copy();
    /// <summary>Executes the GetExtractMetadata operation.</summary>
    public bool GetExtractMetadata();
    /// <summary>Executes the SetExtractMetadata operation.</summary>
    public void SetExtractMetadata(bool value);
    /// <summary>Executes the GetExtractArticle operation.</summary>
    public bool GetExtractArticle();
    /// <summary>Executes the SetExtractArticle operation.</summary>
    public void SetExtractArticle(bool value);
    /// <summary>Executes the GetSanitize operation.</summary>
    public bool GetSanitize();
    /// <summary>Executes the SetSanitize operation.</summary>
    public void SetSanitize(bool value);
    /// <summary>Executes the GetSerializeHtml operation.</summary>
    public bool GetSerializeHtml();
    /// <summary>Executes the SetSerializeHtml operation.</summary>
    public void SetSerializeHtml(bool value);
    /// <summary>Executes the GetConvertText operation.</summary>
    public bool GetConvertText();
    /// <summary>Executes the SetConvertText operation.</summary>
    public void SetConvertText(bool value);
    /// <summary>Executes the GetConvertMarkdown operation.</summary>
    public bool GetConvertMarkdown();
    /// <summary>Executes the SetConvertMarkdown operation.</summary>
    public void SetConvertMarkdown(bool value);
    /// <summary>Executes the GetParseOptions operation.</summary>
    public HtmlParseOptions GetParseOptions();
    /// <summary>Executes the SetParseOptions operation.</summary>
    public void SetParseOptions(HtmlParseOptions value);
    /// <summary>Gets the metadata extraction settings.</summary>
    public HtmlMetadataOptions GetMetadataOptions();
    /// <summary>Sets the metadata extraction settings.</summary>
    public void SetMetadataOptions(HtmlMetadataOptions value);
    /// <summary>Executes the GetSanitizerPolicy operation.</summary>
    public HtmlCompiledSanitizerPolicy GetSanitizerPolicy();
    /// <summary>Executes the SetSanitizerPolicy operation.</summary>
    public void SetSanitizerPolicy(HtmlCompiledSanitizerPolicy value);
    /// <summary>Gets the HTML serializer settings.</summary>
    public HtmlSerializerOptions GetSerializerOptions();
    /// <summary>Sets the HTML serializer settings.</summary>
    public void SetSerializerOptions(HtmlSerializerOptions value);
    /// <summary>Executes the GetTextOptions operation.</summary>
    public HtmlTextOptions GetTextOptions();
    /// <summary>Executes the SetTextOptions operation.</summary>
    public void SetTextOptions(HtmlTextOptions value);
    /// <summary>Executes the GetMarkdownOptions operation.</summary>
    public HtmlMarkdownOptions GetMarkdownOptions();
    /// <summary>Executes the SetMarkdownOptions operation.</summary>
    public void SetMarkdownOptions(HtmlMarkdownOptions value);
    /// <summary>Executes the GetArticleOptions operation.</summary>
    public HtmlArticleOptions GetArticleOptions();
    /// <summary>Executes the SetArticleOptions operation.</summary>
    public void SetArticleOptions(HtmlArticleOptions value);
    /// <summary>Executes the GetLimits operation.</summary>
    public HtmlLimits GetLimits();
    /// <summary>Executes the SetLimits operation.</summary>
    public void SetLimits(HtmlLimits value);
}
