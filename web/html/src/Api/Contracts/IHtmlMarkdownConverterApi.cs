using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlMarkdownConverter operations.</summary>
public interface IHtmlMarkdownConverterApi
{
    /// <summary>Executes the ConvertHtml operation.</summary>
    public HtmlMarkdownResult ConvertHtml(string html);
    /// <summary>Executes the ConvertHtmlWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertHtmlWithOptions(string html, IHtmlMarkdownOptions options, CancellationToken cancellationToken);
    /// <summary>Executes the ConvertDocument operation.</summary>
    public HtmlMarkdownResult ConvertDocument(HtmlDocument document);
    /// <summary>Executes the ConvertDocumentWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertDocumentWithOptions(HtmlDocument document, IHtmlMarkdownOptions options, CancellationToken cancellationToken);
    /// <summary>Executes the ConvertNode operation.</summary>
    public HtmlMarkdownResult ConvertNode(HtmlNode node);
    /// <summary>Executes the ConvertNodeWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertNodeWithOptions(HtmlNode node, IHtmlMarkdownOptions options, CancellationToken cancellationToken);
}
