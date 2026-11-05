using System;

namespace html;
/// <summary>Contains extracted article metadata and content produced by readability processing.</summary>
public class HtmlArticle : IHtmlArticle
{
    internal string? title;
    internal string? byline;
    internal string? excerpt;
    internal string? siteName;
    internal string? language;
    internal string? direction;
    internal string? publishedTimeText;
    internal string? canonicalUrl;
    internal string? imageUrl;
    internal HtmlNode content;
    internal string? sanitizedHtml;
    internal string? text;
    internal string? markdown;
    internal int length;
    internal bool contentSanitized;
    internal HtmlArticle(string? titleValue, string? bylineValue, string? excerptValue, string? siteNameValue, string? languageValue, string? directionValue, string? publishedTimeTextValue, string? canonicalUrlValue, string? imageUrlValue, HtmlNode contentValue, string? sanitizedHtmlValue, string? textValue, string? markdownValue, int lengthValue)
    {
        if (contentValue == null)
        {
            throw new ArgumentNullException("contentValue");
        }

        if (lengthValue < 0)
        {
            throw new ArgumentOutOfRangeException("lengthValue");
        }

        this.title = titleValue;
        this.byline = bylineValue;
        this.excerpt = excerptValue;
        this.siteName = siteNameValue;
        this.language = languageValue;
        this.direction = directionValue;
        this.publishedTimeText = publishedTimeTextValue;
        this.canonicalUrl = canonicalUrlValue;
        this.imageUrl = imageUrlValue;
        this.content = contentValue;
        this.sanitizedHtml = sanitizedHtmlValue;
        this.text = textValue;
        this.markdown = markdownValue;
        this.length = lengthValue;
        this.contentSanitized = false;
    }

    /// <summary>Gets the extracted article title.</summary>
    /// <returns>The title. Returns null when it is unavailable.</returns>
    public string? GetTitle()
    {
        return this.title;
    }

    /// <summary>Gets the extracted article byline.</summary>
    /// <returns>The byline. Returns null when it is unavailable.</returns>
    public string? GetByline()
    {
        return this.byline;
    }

    /// <summary>Gets the extracted article excerpt.</summary>
    /// <returns>The excerpt. Returns null when it is unavailable.</returns>
    public string? GetExcerpt()
    {
        return this.excerpt;
    }

    /// <summary>Gets the extracted site name.</summary>
    /// <returns>The site name. Returns null when it is unavailable.</returns>
    public string? GetSiteName()
    {
        return this.siteName;
    }

    /// <summary>Gets the extracted language tag.</summary>
    /// <returns>The language. Returns null when it is unavailable.</returns>
    public string? GetLanguage()
    {
        return this.language;
    }

    /// <summary>Gets the extracted text direction.</summary>
    /// <returns>The direction. Returns null when it is unavailable.</returns>
    public string? GetDirection()
    {
        return this.direction;
    }

    /// <summary>Gets the publication time as source text.</summary>
    /// <returns>The publication time text. Returns null when it is
    /// unavailable.</returns>
    public string? GetPublishedTimeText()
    {
        return this.publishedTimeText;
    }

    /// <summary>Gets the extracted canonical URL.</summary>
    /// <returns>The canonical URL. Returns null when it is unavailable.
    /// </returns>
    public string? GetCanonicalUrl()
    {
        return this.canonicalUrl;
    }

    /// <summary>Gets the representative image URL.</summary>
    /// <returns>The image URL. Returns null when it is unavailable.</returns>
    public string? GetImageUrl()
    {
        return this.imageUrl;
    }

    /// <summary>Gets the extracted article content clone.</summary>
    /// <remarks>Content returned by extraction-only APIs is not sanitized.
    /// Check <see cref = "IsContentSanitized"/> before treating this node as
    /// renderable content.</remarks>
    /// <returns>The content.</returns>
    public HtmlNode GetContent()
    {
        return this.content;
    }

    /// <summary>Gets renderable HTML produced by a sanitizing pipeline.</summary>
    /// <returns>The sanitized HTML. Returns null when sanitized HTML
    /// serialization was not requested or did not complete.</returns>
    public string? GetSanitizedHtml()
    {
        return this.sanitizedHtml;
    }

    /// <summary>Gets converted plain text.</summary>
    /// <returns>The text. Returns null when text conversion was not
    /// requested.</returns>
    public string? GetText()
    {
        return this.text;
    }

    /// <summary>Gets converted Markdown.</summary>
    /// <returns>The Markdown. Returns null when Markdown conversion was not
    /// requested.</returns>
    public string? GetMarkdown()
    {
        return this.markdown;
    }

    /// <summary>Gets the normalized character length used by article scoring.</summary>
    /// <returns>The extracted article-content length in UTF-16 code units.</returns>
    public int GetLength()
    {
        return this.length;
    }

    /// <summary>Reports whether the article content came from the complete sanitizer pipeline.</summary>
    /// <returns><c>true</c> when article content was produced by the complete sanitizer; otherwise, <c>false</c>.</returns>
    public bool IsContentSanitized()
    {
        return this.contentSanitized;
    }

    internal HtmlArticle CreateProcessed(HtmlNode contentValue, string? sanitizedHtmlValue, string? textValue, string? markdownValue)
    {
        HtmlArticle article = new HtmlArticle(this.title, this.byline, this.excerpt, this.siteName, this.language, this.direction, this.publishedTimeText, this.canonicalUrl, this.imageUrl, contentValue, sanitizedHtmlValue, textValue, markdownValue, this.length);
        article.contentSanitized = true;
        return article;
    }
}
