using System;
using System.Globalization;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlTextConverter operations.</summary>
public class HtmlTextConverterApi : IHtmlTextConverterApi
{
    /// <summary>Executes the ConvertHtml operation.</summary>
    public HtmlTextResult ConvertHtml(string html)
    {
        return HtmlTextConverter.Convert(html);
    }

    /// <summary>Executes the ConvertHtmlWithOptions operation.</summary>
    public HtmlTextResult ConvertHtmlWithOptions(string html, IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        return HtmlTextConverter.Convert(html, options, cancellationToken);
    }

    /// <summary>Executes the ConvertDocument operation.</summary>
    public HtmlTextResult ConvertDocument(HtmlDocument document)
    {
        return HtmlTextConverter.Convert(document);
    }

    /// <summary>Executes the ConvertDocumentWithOptions operation.</summary>
    public HtmlTextResult ConvertDocumentWithOptions(HtmlDocument document, IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        return HtmlTextConverter.Convert(document, options, cancellationToken);
    }

    /// <summary>Executes the ConvertNode operation.</summary>
    public HtmlTextResult ConvertNode(HtmlNode node)
    {
        return HtmlTextConverter.Convert(node);
    }

    /// <summary>Executes the ConvertNodeWithOptions operation.</summary>
    public HtmlTextResult ConvertNodeWithOptions(HtmlNode node, IHtmlTextOptions options, CancellationToken cancellationToken)
    {
        return HtmlTextConverter.Convert(node, options, cancellationToken);
    }
}
