using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlSanitizer operations.</summary>
public class HtmlSanitizerApi : IHtmlSanitizerApi
{
    /// <summary>Executes the SanitizeHtml operation.</summary>
    public HtmlSanitizeResult SanitizeHtml(string html, HtmlCompiledSanitizerPolicy policy, IHtmlParseConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlSanitizer.Sanitize(html, policy,
            HtmlApi.CreateParseOptions(options), cancellationToken);
    }

    /// <summary>Sanitizes a document into a new document without changing the input.</summary>
    /// <param name = "document">The document to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked during the operation.</param>
    /// <returns>A fail-closed result containing the cloned sanitized document and canonical HTML when successful.</returns>
    public HtmlSanitizeResult SanitizeDocument(HtmlDocument document, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateDocument(document);
        return HtmlSanitizer.Sanitize(document.GetRoot(), policy, limits,
            cancellationToken);
    }

    /// <summary>Sanitizes a fragment into a new fragment without changing the input.</summary>
    /// <param name = "fragment">The fragment node to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked during the operation.</param>
    /// <returns>A fail-closed result containing the cloned sanitized fragment and canonical HTML when successful.</returns>
    public HtmlSanitizeResult SanitizeFragment(HtmlNode fragment, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateFragment(fragment);
        return HtmlSanitizer.Sanitize(fragment, policy, limits,
            cancellationToken);
    }

    /// <summary>Sanitizes a subtree into a new subtree without changing the input.</summary>
    /// <param name = "subtree">The non-document, non-fragment subtree root to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked during the operation.</param>
    /// <returns>A fail-closed result containing the cloned sanitized subtree and canonical outer HTML when successful.</returns>
    public HtmlSanitizeResult SanitizeSubtree(HtmlNode subtree, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateSubtree(subtree);
        return HtmlSanitizer.Sanitize(subtree, policy, limits,
            cancellationToken);
    }

    /// <summary>Sanitizes a document by changing the supplied document.</summary>
    /// <param name = "document">The mutable document to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked before mutation and during the operation.</param>
    /// <returns>A fail-closed result whose document is the supplied document when successful.</returns>
    public HtmlSanitizeResult SanitizeDocumentInPlace(HtmlDocument document, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateDocument(document);
        return HtmlSanitizer.SanitizeInPlace(document.GetRoot(), policy,
            limits, cancellationToken);
    }

    /// <summary>Sanitizes a fragment by changing the supplied fragment.</summary>
    /// <param name = "fragment">The mutable fragment node to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked before mutation and during the operation.</param>
    /// <returns>A fail-closed result whose node is the supplied fragment when successful.</returns>
    public HtmlSanitizeResult SanitizeFragmentInPlace(HtmlNode fragment, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateFragment(fragment);
        return HtmlSanitizer.SanitizeInPlace(fragment, policy, limits,
            cancellationToken);
    }

    /// <summary>Sanitizes a subtree by changing the supplied subtree.</summary>
    /// <param name = "subtree">The mutable non-document, non-fragment subtree root to sanitize.</param>
    /// <param name = "policy">The immutable compiled policy to apply.</param>
    /// <param name = "limits">The resource limits copied for this operation.</param>
    /// <param name = "cancellationToken">The token checked before mutation and during the operation.</param>
    /// <returns>A fail-closed result whose node is the supplied subtree when successful.</returns>
    public HtmlSanitizeResult SanitizeSubtreeInPlace(HtmlNode subtree, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        ValidateSubtree(subtree);
        return HtmlSanitizer.SanitizeInPlace(subtree, policy, limits,
            cancellationToken);
    }

    /// <summary>Executes the SanitizeNode operation.</summary>
    public HtmlSanitizeResult SanitizeNode(HtmlNode node, HtmlCompiledSanitizerPolicy policy, CancellationToken cancellationToken)
    {
        return HtmlSanitizer.Sanitize(node, policy, cancellationToken);
    }

    /// <summary>Executes the SanitizeNodeWithLimits operation.</summary>
    public HtmlSanitizeResult SanitizeNodeWithLimits(HtmlNode node, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlSanitizer.Sanitize(node, policy, limits, cancellationToken);
    }

    /// <summary>Executes the SanitizeInPlace operation.</summary>
    public HtmlSanitizeResult SanitizeInPlace(HtmlNode node, HtmlCompiledSanitizerPolicy policy, CancellationToken cancellationToken)
    {
        return HtmlSanitizer.SanitizeInPlace(node, policy, cancellationToken);
    }

    /// <summary>Executes the SanitizeInPlaceWithLimits operation.</summary>
    public HtmlSanitizeResult SanitizeInPlaceWithLimits(HtmlNode node, HtmlCompiledSanitizerPolicy policy, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlSanitizer.SanitizeInPlace(node, policy, limits, cancellationToken);
    }

    internal static void ValidateDocument(HtmlDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateFragment(HtmlNode fragment)
    {
        if (fragment == null)
        {
            throw new ArgumentNullException("fragment");
        }

        if (fragment.GetKind() != HtmlNodeKind.Fragment)
        {
            throw new ArgumentException(
                "A fragment workflow requires a fragment node.",
                "fragment");
        }
    }

    internal static void ValidateSubtree(HtmlNode subtree)
    {
        if (subtree == null)
        {
            throw new ArgumentNullException("subtree");
        }

        HtmlNodeKind kind = subtree.GetKind();
        if (kind == HtmlNodeKind.Document || kind == HtmlNodeKind.Fragment)
        {
            throw new ArgumentException(
                "A subtree workflow requires a non-document, non-fragment node.",
                "subtree");
        }
    }
}
