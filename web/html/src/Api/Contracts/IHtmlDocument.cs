using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlDocument"/>.</summary>
public interface IHtmlDocument
{
    /// <summary>Executes the GetRoot operation.</summary>
    public HtmlNode GetRoot();
    /// <summary>Executes the GetDocumentElement operation.</summary>
    public HtmlNode? GetDocumentElement();
    /// <summary>Executes the GetHead operation.</summary>
    public HtmlNode? GetHead();
    /// <summary>Executes the GetBody operation.</summary>
    public HtmlNode? GetBody();
    /// <summary>Executes the GetTitle operation.</summary>
    public string GetTitle();
    /// <summary>Executes the GetUrl operation.</summary>
    public string GetUrl();
    /// <summary>Executes the GetResponseUrl operation.</summary>
    public string GetResponseUrl();
    /// <summary>Executes the GetBaseUrl operation.</summary>
    public string GetBaseUrl();
    /// <summary>Executes the GetContentLanguage operation.</summary>
    public string GetContentLanguage();
    /// <summary>Executes the GetLanguage operation.</summary>
    public string GetLanguage();
    /// <summary>Executes the GetDirection operation.</summary>
    public string GetDirection();
    /// <summary>Executes the GetDetectedEncoding operation.</summary>
    public Encoding? GetDetectedEncoding();
    /// <summary>Executes the GetDetectedEncodingLabel operation.</summary>
    public string GetDetectedEncodingLabel();
    /// <summary>Executes the GetEncodingDetection operation.</summary>
    public HtmlEncodingDetectionResult? GetEncodingDetection();
    /// <summary>Executes the GetMutationVersion operation.</summary>
    public long GetMutationVersion();
    /// <summary>Executes the GetDocumentType operation.</summary>
    public HtmlNode? GetDocumentType();
    /// <summary>Executes the GetBaseElement operation.</summary>
    public HtmlNode? GetBaseElement();
    /// <summary>Executes the GetEffectiveBaseUrl operation.</summary>
    public HtmlUrlResult GetEffectiveBaseUrl();
    /// <summary>Executes the FindLinks operation.</summary>
    public HtmlNode[] FindLinks();
    /// <summary>Executes the FindLinksWithLimits operation.</summary>
    public HtmlNode[] FindLinksWithLimits(int maximumResults, int maximumVisits);
    /// <summary>Executes the FindMedia operation.</summary>
    public HtmlNode[] FindMedia();
    /// <summary>Executes the FindMediaWithLimits operation.</summary>
    public HtmlNode[] FindMediaWithLimits(int maximumResults, int maximumVisits);
    /// <summary>Executes the GetMode operation.</summary>
    public HtmlDocumentMode GetMode();
    /// <summary>Executes the GetDiagnostics operation.</summary>
    public HtmlDiagnostic[] GetDiagnostics();
    /// <summary>Executes the FindById operation.</summary>
    public HtmlNode? FindById(string id);
    /// <summary>Executes the FindByTag operation.</summary>
    public HtmlNode[] FindByTag(string tagName);
    /// <summary>Executes the FindByClass operation.</summary>
    public HtmlNode[] FindByClass(string className);
    /// <summary>Executes the QuerySelector operation.</summary>
    public HtmlNode? QuerySelector(string selector);
    /// <summary>Executes the QuerySelectorAll operation.</summary>
    public HtmlNode[] QuerySelectorAll(string selector);
    /// <summary>Executes the FindByName operation.</summary>
    public HtmlNode[] FindByName(string name);
    /// <summary>Executes the FindByNameWithLimits operation.</summary>
    public HtmlNode[] FindByNameWithLimits(string name, int maximumResults, int maximumVisits);
    /// <summary>Executes the FindByAttribute operation.</summary>
    public HtmlNode[] FindByAttribute(string attributeName);
    /// <summary>Executes the FindByAttributeWithLimits operation.</summary>
    public HtmlNode[] FindByAttributeWithLimits(string attributeName, int maximumResults, int maximumVisits);
    /// <summary>Executes the FindByAttributeValue operation.</summary>
    public HtmlNode[] FindByAttributeValue(string attributeName, string value);
    /// <summary>Executes the FindByAttributeValueWithLimits operation.</summary>
    public HtmlNode[] FindByAttributeValueWithLimits(string attributeName, string value, int maximumResults, int maximumVisits);
    /// <summary>Executes the FindFirst operation.</summary>
    public HtmlNode? FindFirst(Func<HtmlNode, bool> predicate);
    /// <summary>Executes the FindFirstWithLimit operation.</summary>
    public HtmlNode? FindFirstWithLimit(Func<HtmlNode, bool> predicate, int maximumVisits);
    /// <summary>Executes the FindAll operation.</summary>
    public HtmlNode[] FindAll(Func<HtmlNode, bool> predicate);
    /// <summary>Executes the FindAllWithLimits operation.</summary>
    public HtmlNode[] FindAllWithLimits(Func<HtmlNode, bool> predicate, int maximumResults, int maximumVisits);
    /// <summary>Executes the CreateElement operation.</summary>
    public HtmlNode CreateElement(string name);
    /// <summary>Executes the CreateElementInNamespace operation.</summary>
    public HtmlNode CreateElementInNamespace(string namespaceUri, string qualifiedName);
    /// <summary>Executes the CreateText operation.</summary>
    public HtmlNode CreateText(string text);
    /// <summary>Executes the CreateComment operation.</summary>
    public HtmlNode CreateComment(string text);
    /// <summary>Executes the CreateFragment operation.</summary>
    public HtmlNode CreateFragment();
    /// <summary>Executes the CreateAttribute operation.</summary>
    public HtmlAttribute CreateAttribute(string name, string value);
    /// <summary>Executes the CreateAttributeInNamespace operation.</summary>
    public HtmlAttribute CreateAttributeInNamespace(string namespaceUri, string qualifiedName, string value);
    /// <summary>Executes the CreateDocumentType operation.</summary>
    public HtmlNode CreateDocumentType(string? name, string? publicIdentifier, string? systemIdentifier, bool forceQuirks);
    /// <summary>Executes the Import operation.</summary>
    public HtmlNode Import(HtmlNode node, bool deep);
    /// <summary>Executes the ImportShallow operation.</summary>
    public HtmlNode ImportShallow(HtmlNode node);
    /// <summary>Executes the ImportDeep operation.</summary>
    public HtmlNode ImportDeep(HtmlNode node);
    /// <summary>Executes the Freeze operation.</summary>
    public void Freeze();
    /// <summary>Executes the IsFrozen operation.</summary>
    public bool IsFrozen();
}
