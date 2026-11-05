using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlMarkdownConverter operations.</summary>
public class HtmlMarkdownConverterApi : IHtmlMarkdownConverterApi
{
    /// <summary>Executes the ConvertHtml operation.</summary>
    public HtmlMarkdownResult ConvertHtml(string html)
    {
        return HtmlMarkdownConverter.Convert(html);
    }

    /// <summary>Executes the ConvertHtmlWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertHtmlWithOptions(string html, IHtmlMarkdownOptions options, CancellationToken cancellationToken)
    {
        return HtmlMarkdownConverter.Convert(html, options, cancellationToken);
    }

    /// <summary>Executes the ConvertDocument operation.</summary>
    public HtmlMarkdownResult ConvertDocument(HtmlDocument document)
    {
        return HtmlMarkdownConverter.Convert(document);
    }

    /// <summary>Executes the ConvertDocumentWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertDocumentWithOptions(HtmlDocument document, IHtmlMarkdownOptions options, CancellationToken cancellationToken)
    {
        return HtmlMarkdownConverter.Convert(document, options, cancellationToken);
    }

    /// <summary>Executes the ConvertNode operation.</summary>
    public HtmlMarkdownResult ConvertNode(HtmlNode node)
    {
        return HtmlMarkdownConverter.Convert(node);
    }

    /// <summary>Executes the ConvertNodeWithOptions operation.</summary>
    public HtmlMarkdownResult ConvertNodeWithOptions(HtmlNode node, IHtmlMarkdownOptions options, CancellationToken cancellationToken)
    {
        return HtmlMarkdownConverter.Convert(node, options, cancellationToken);
    }
}
