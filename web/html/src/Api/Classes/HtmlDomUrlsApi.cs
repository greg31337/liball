using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlDomUrls operations.</summary>
public class HtmlDomUrlsApi : IHtmlDomUrlsApi
{
    /// <summary>Executes the GetAttributeKind operation.</summary>
    public HtmlUrlAttributeKind GetAttributeKind(HtmlNode element, HtmlAttribute attribute)
    {
        return HtmlDomUrls.GetAttributeKind(element, attribute);
    }

    /// <summary>Classifies a no-namespace attribute name in an element context.</summary>
    public HtmlUrlAttributeKind GetNamedAttributeKind(HtmlNode element,
        string attributeName)
    {
        return HtmlDomUrls.GetNamedAttributeKind(element, attributeName);
    }

    /// <summary>Executes the GetBaseElement operation.</summary>
    public HtmlNode? GetBaseElement(HtmlDocument document)
    {
        return HtmlDomUrls.GetBaseElement(document);
    }

    /// <summary>Executes the GetEffectiveBaseUrl operation.</summary>
    public HtmlUrlResult GetEffectiveBaseUrl(HtmlDocument document)
    {
        return HtmlDomUrls.GetEffectiveBaseUrl(document);
    }

    /// <summary>Executes the GetEffectiveBaseUrlWithLimits operation.</summary>
    public HtmlUrlResult GetEffectiveBaseUrlWithLimits(HtmlDocument document, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlDomUrls.GetEffectiveBaseUrl(document, limits, cancellationToken);
    }

    /// <summary>Executes the ResolveAttribute operation.</summary>
    public HtmlUrlResult ResolveAttribute(HtmlNode element, HtmlAttribute attribute)
    {
        return HtmlDomUrls.ResolveAttribute(element, attribute);
    }

    /// <summary>Executes the ResolveAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveAttributeWithLimits(HtmlNode element, HtmlAttribute attribute, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlDomUrls.ResolveAttribute(element, attribute, limits, cancellationToken);
    }

    /// <summary>Executes the ResolveNamedAttribute operation.</summary>
    public HtmlUrlResult ResolveNamedAttribute(HtmlNode element, string attributeName)
    {
        return HtmlDomUrls.ResolveAttribute(element, attributeName);
    }

    /// <summary>Executes the ResolveNamedAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveNamedAttributeWithLimits(HtmlNode element, string attributeName, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlDomUrls.ResolveAttribute(element, attributeName, limits, cancellationToken);
    }

    /// <summary>Executes the ResolveNamespacedAttribute operation.</summary>
    public HtmlUrlResult ResolveNamespacedAttribute(HtmlNode element, string namespaceUri, string localName)
    {
        return HtmlDomUrls.ResolveAttribute(element, namespaceUri, localName);
    }

    /// <summary>Executes the ResolveNamespacedAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveNamespacedAttributeWithLimits(HtmlNode element, string namespaceUri, string localName, HtmlLimits limits, CancellationToken cancellationToken)
    {
        return HtmlDomUrls.ResolveAttribute(element, namespaceUri, localName, limits, cancellationToken);
    }

    /// <summary>Executes the FindLinks operation.</summary>
    public HtmlNode[] FindLinks(HtmlNode root)
    {
        return HtmlDomUrls.FindLinks(root);
    }

    /// <summary>Executes the FindLinksWithLimits operation.</summary>
    public HtmlNode[] FindLinksWithLimits(HtmlNode root, int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindLinks(root, maximumResults, maximumVisits);
    }

    /// <summary>Executes the FindMedia operation.</summary>
    public HtmlNode[] FindMedia(HtmlNode root)
    {
        return HtmlDomUrls.FindMedia(root);
    }

    /// <summary>Executes the FindMediaWithLimits operation.</summary>
    public HtmlNode[] FindMediaWithLimits(HtmlNode root, int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindMedia(root, maximumResults, maximumVisits);
    }
}
