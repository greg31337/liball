using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlDomUrls operations.</summary>
public interface IHtmlDomUrlsApi
{
    /// <summary>Executes the GetAttributeKind operation.</summary>
    public HtmlUrlAttributeKind GetAttributeKind(HtmlNode element, HtmlAttribute attribute);
    /// <summary>Classifies a no-namespace attribute name in an element context.</summary>
    public HtmlUrlAttributeKind GetNamedAttributeKind(HtmlNode element, string attributeName);
    /// <summary>Executes the GetBaseElement operation.</summary>
    public HtmlNode? GetBaseElement(HtmlDocument document);
    /// <summary>Executes the GetEffectiveBaseUrl operation.</summary>
    public HtmlUrlResult GetEffectiveBaseUrl(HtmlDocument document);
    /// <summary>Executes the GetEffectiveBaseUrlWithLimits operation.</summary>
    public HtmlUrlResult GetEffectiveBaseUrlWithLimits(HtmlDocument document, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the ResolveAttribute operation.</summary>
    public HtmlUrlResult ResolveAttribute(HtmlNode element, HtmlAttribute attribute);
    /// <summary>Executes the ResolveAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveAttributeWithLimits(HtmlNode element, HtmlAttribute attribute, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the ResolveNamedAttribute operation.</summary>
    public HtmlUrlResult ResolveNamedAttribute(HtmlNode element, string attributeName);
    /// <summary>Executes the ResolveNamedAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveNamedAttributeWithLimits(HtmlNode element, string attributeName, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the ResolveNamespacedAttribute operation.</summary>
    public HtmlUrlResult ResolveNamespacedAttribute(HtmlNode element, string namespaceUri, string localName);
    /// <summary>Executes the ResolveNamespacedAttributeWithLimits operation.</summary>
    public HtmlUrlResult ResolveNamespacedAttributeWithLimits(HtmlNode element, string namespaceUri, string localName, HtmlLimits limits, CancellationToken cancellationToken);
    /// <summary>Executes the FindLinks operation.</summary>
    public HtmlNode[] FindLinks(HtmlNode root);
    /// <summary>Executes the FindLinksWithLimits operation.</summary>
    public HtmlNode[] FindLinksWithLimits(HtmlNode root, int maximumResults, int maximumVisits);
    /// <summary>Executes the FindMedia operation.</summary>
    public HtmlNode[] FindMedia(HtmlNode root);
    /// <summary>Executes the FindMediaWithLimits operation.</summary>
    public HtmlNode[] FindMediaWithLimits(HtmlNode root, int maximumResults, int maximumVisits);
}
