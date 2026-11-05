using System;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlArticle"/>.</summary>
public interface IHtmlArticle
{
    /// <summary>Executes the GetTitle operation.</summary>
    public string? GetTitle();
    /// <summary>Executes the GetByline operation.</summary>
    public string? GetByline();
    /// <summary>Executes the GetExcerpt operation.</summary>
    public string? GetExcerpt();
    /// <summary>Executes the GetSiteName operation.</summary>
    public string? GetSiteName();
    /// <summary>Executes the GetLanguage operation.</summary>
    public string? GetLanguage();
    /// <summary>Executes the GetDirection operation.</summary>
    public string? GetDirection();
    /// <summary>Executes the GetPublishedTimeText operation.</summary>
    public string? GetPublishedTimeText();
    /// <summary>Executes the GetCanonicalUrl operation.</summary>
    public string? GetCanonicalUrl();
    /// <summary>Executes the GetImageUrl operation.</summary>
    public string? GetImageUrl();
    /// <summary>Executes the GetContent operation.</summary>
    public HtmlNode GetContent();
    /// <summary>Executes the GetSanitizedHtml operation.</summary>
    public string? GetSanitizedHtml();
    /// <summary>Executes the GetText operation.</summary>
    public string? GetText();
    /// <summary>Executes the GetMarkdown operation.</summary>
    public string? GetMarkdown();
    /// <summary>Executes the GetLength operation.</summary>
    public int GetLength();
    /// <summary>Executes the IsContentSanitized operation.</summary>
    public bool IsContentSanitized();
}
