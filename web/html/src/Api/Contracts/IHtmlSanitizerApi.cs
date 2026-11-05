using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlSanitizer operations.</summary>
public interface IHtmlSanitizerApi
{
    /// <summary>Executes the SanitizeHtml operation.</summary>
    public HtmlSanitizeResult SanitizeHtml(string html, HtmlCompiledSanitizerPolicy policy, IHtmlParseConfiguration options, CancellationToken cancellationToken);
    /// <summary>Sanitizes a document into a new document without changing the input.</summary>
    public HtmlSanitizeResult SanitizeDocument(HtmlDocument document, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Sanitizes a fragment into a new fragment without changing the input.</summary>
    public HtmlSanitizeResult SanitizeFragment(HtmlNode fragment, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Sanitizes a subtree into a new subtree without changing the input.</summary>
    public HtmlSanitizeResult SanitizeSubtree(HtmlNode subtree, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Sanitizes a document by changing the supplied document.</summary>
    public HtmlSanitizeResult SanitizeDocumentInPlace(HtmlDocument document, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Sanitizes a fragment by changing the supplied fragment.</summary>
    public HtmlSanitizeResult SanitizeFragmentInPlace(HtmlNode fragment, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Sanitizes a subtree by changing the supplied subtree.</summary>
    public HtmlSanitizeResult SanitizeSubtreeInPlace(HtmlNode subtree, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the SanitizeNode operation.</summary>
    public HtmlSanitizeResult SanitizeNode(HtmlNode node, HtmlCompiledSanitizerPolicy policy, CancellationToken cancellationToken);
    /// <summary>Executes the SanitizeNodeWithLimits operation.</summary>
    public HtmlSanitizeResult SanitizeNodeWithLimits(HtmlNode node, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the SanitizeInPlace operation.</summary>
    public HtmlSanitizeResult SanitizeInPlace(HtmlNode node, HtmlCompiledSanitizerPolicy policy, CancellationToken cancellationToken);
    /// <summary>Executes the SanitizeInPlaceWithLimits operation.</summary>
    public HtmlSanitizeResult SanitizeInPlaceWithLimits(HtmlNode node, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken);
}
