using System;
using System.Text;

namespace html;
/// <summary>Defines the public instance operations exposed by <see cref = "HtmlNode"/>.</summary>
public interface IHtmlNode
{
    /// <summary>Executes the GetKind operation.</summary>
    public HtmlNodeKind GetKind();
    /// <summary>Executes the GetOwnerDocument operation.</summary>
    public HtmlDocument? GetOwnerDocument();
    /// <summary>Executes the GetParent operation.</summary>
    public HtmlNode? GetParent();
    /// <summary>Executes the GetFirstChild operation.</summary>
    public HtmlNode? GetFirstChild();
    /// <summary>Executes the GetLastChild operation.</summary>
    public HtmlNode? GetLastChild();
    /// <summary>Executes the GetPreviousSibling operation.</summary>
    public HtmlNode? GetPreviousSibling();
    /// <summary>Executes the GetNextSibling operation.</summary>
    public HtmlNode? GetNextSibling();
    /// <summary>Executes the GetChildren operation.</summary>
    public HtmlNode[] GetChildren();
    /// <summary>Executes the GetChildElements operation.</summary>
    public HtmlNode[] GetChildElements();
    /// <summary>Executes the GetFirstElementChild operation.</summary>
    public HtmlNode? GetFirstElementChild();
    /// <summary>Executes the GetLastElementChild operation.</summary>
    public HtmlNode? GetLastElementChild();
    /// <summary>Executes the GetSiblings operation.</summary>
    public HtmlNode[] GetSiblings();
    /// <summary>Executes the GetElementSiblings operation.</summary>
    public HtmlNode[] GetElementSiblings();
    /// <summary>Executes the GetPreviousElementSibling operation.</summary>
    public HtmlNode? GetPreviousElementSibling();
    /// <summary>Executes the GetNextElementSibling operation.</summary>
    public HtmlNode? GetNextElementSibling();
    /// <summary>Executes the GetAncestors operation.</summary>
    public HtmlNode[] GetAncestors();
    /// <summary>Executes the GetDescendants operation.</summary>
    public HtmlNode[] GetDescendants();
    /// <summary>Executes the FindById operation.</summary>
    public HtmlNode? FindById(string id);
    /// <summary>Executes the FindByIdWithLimit operation.</summary>
    public HtmlNode? FindByIdWithLimit(string id, int maximumVisits);
    /// <summary>Executes the FindByTag operation.</summary>
    public HtmlNode[] FindByTag(string tagName);
    /// <summary>Executes the FindByTagWithLimits operation.</summary>
    public HtmlNode[] FindByTagWithLimits(string tagName, int maximumResults, int maximumVisits);
    /// <summary>Executes the FindByClass operation.</summary>
    public HtmlNode[] FindByClass(string className);
    /// <summary>Executes the FindByClassWithLimits operation.</summary>
    public HtmlNode[] FindByClassWithLimits(string className, int maximumResults, int maximumVisits);
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
    /// <summary>Executes the Closest operation.</summary>
    public HtmlNode? Closest(Func<HtmlNode, bool> predicate);
    /// <summary>Executes the ClosestWithLimit operation.</summary>
    public HtmlNode? ClosestWithLimit(Func<HtmlNode, bool> predicate, int maximumVisits);
    /// <summary>Executes the Matches operation.</summary>
    public bool Matches(string selector);
    /// <summary>Executes the ClosestSelector operation.</summary>
    public HtmlNode? ClosestSelector(string selector);
    /// <summary>Executes the QuerySelector operation.</summary>
    public HtmlNode? QuerySelector(string selector);
    /// <summary>Executes the QuerySelectorAll operation.</summary>
    public HtmlNode[] QuerySelectorAll(string selector);
    /// <summary>Executes the GetRoot operation.</summary>
    public HtmlNode GetRoot();
    /// <summary>Executes the GetDepth operation.</summary>
    public int GetDepth();
    /// <summary>Executes the Contains operation.</summary>
    public bool Contains(HtmlNode other);
    /// <summary>Executes the WalkDepthFirst operation.</summary>
    public HtmlTreeWalkResult WalkDepthFirst(Func<HtmlNode, HtmlTreeVisitPhase, int, bool> visitor);
    /// <summary>Executes the WalkDepthFirstWithLimit operation.</summary>
    public HtmlTreeWalkResult WalkDepthFirstWithLimit(Func<HtmlNode, HtmlTreeVisitPhase, int, bool> visitor, int maximumVisits);
    /// <summary>Executes the WalkBreadthFirst operation.</summary>
    public HtmlTreeWalkResult WalkBreadthFirst(Func<HtmlNode, int, bool> visitor);
    /// <summary>Executes the WalkBreadthFirstWithLimit operation.</summary>
    public HtmlTreeWalkResult WalkBreadthFirstWithLimit(Func<HtmlNode, int, bool> visitor, int maximumVisits);
    /// <summary>Executes the GetName operation.</summary>
    public string GetName();
    /// <summary>Executes the GetLocalName operation.</summary>
    public string GetLocalName();
    /// <summary>Executes the GetNamespaceUri operation.</summary>
    public string GetNamespaceUri();
    /// <summary>Executes the GetPrefix operation.</summary>
    public string GetPrefix();
    /// <summary>Executes the GetData operation.</summary>
    public string GetData();
    /// <summary>Executes the GetLanguage operation.</summary>
    public string GetLanguage();
    /// <summary>Executes the GetDirection operation.</summary>
    public string GetDirection();
    /// <summary>Executes the HasDoctypeName operation.</summary>
    public bool HasDoctypeName();
    /// <summary>Executes the GetDoctypeName operation.</summary>
    public string GetDoctypeName();
    /// <summary>Executes the HasPublicIdentifier operation.</summary>
    public bool HasPublicIdentifier();
    /// <summary>Executes the GetPublicIdentifier operation.</summary>
    public string GetPublicIdentifier();
    /// <summary>Executes the HasSystemIdentifier operation.</summary>
    public bool HasSystemIdentifier();
    /// <summary>Executes the GetSystemIdentifier operation.</summary>
    public string GetSystemIdentifier();
    /// <summary>Executes the IsForceQuirks operation.</summary>
    public bool IsForceQuirks();
    /// <summary>Executes the GetTextContent operation.</summary>
    public string GetTextContent();
    /// <summary>Executes the GetOwnText operation.</summary>
    public string GetOwnText();
    /// <summary>Executes the GetInnerHtml operation.</summary>
    public string GetInnerHtml();
    /// <summary>Executes the GetOuterHtml operation.</summary>
    public string GetOuterHtml();
    /// <summary>Executes the GetNormalizedTextContent operation.</summary>
    public string GetNormalizedTextContent();
    /// <summary>Executes the GetAttributes operation.</summary>
    public HtmlAttribute[] GetAttributes();
    /// <summary>Executes the GetAttribute operation.</summary>
    public string? GetAttribute(string name);
    /// <summary>Executes the GetAttributeInNamespace operation.</summary>
    public string? GetAttributeInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the HasAttributeInNamespace operation.</summary>
    public bool HasAttributeInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the GetSpaceSeparatedTokens operation.</summary>
    public string[] GetSpaceSeparatedTokens(string attributeName);
    /// <summary>Executes the GetCommaSeparatedTokens operation.</summary>
    public string[] GetCommaSeparatedTokens(string attributeName);
    /// <summary>Executes the GetAbsoluteUrl operation.</summary>
    public HtmlUrlResult GetAbsoluteUrl(string attributeName);
    /// <summary>Executes the GetAbsoluteUrlWithLimits operation.</summary>
    public HtmlUrlResult GetAbsoluteUrlWithLimits(string attributeName, HtmlLimits limits, System.Threading.CancellationToken cancellationToken);
    /// <summary>Executes the GetAbsoluteUrlInNamespace operation.</summary>
    public HtmlUrlResult GetAbsoluteUrlInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the FindLinks operation.</summary>
    public HtmlNode[] FindLinks();
    /// <summary>Executes the FindLinksWithLimits operation.</summary>
    public HtmlNode[] FindLinksWithLimits(int maximumResults, int maximumVisits);
    /// <summary>Executes the FindMedia operation.</summary>
    public HtmlNode[] FindMedia();
    /// <summary>Executes the FindMediaWithLimits operation.</summary>
    public HtmlNode[] FindMediaWithLimits(int maximumResults, int maximumVisits);
    /// <summary>Executes the HasAttribute operation.</summary>
    public bool HasAttribute(string name);
    /// <summary>Executes the SetAttribute operation.</summary>
    public void SetAttribute(string name, string value);
    /// <summary>Executes the RemoveAttribute operation.</summary>
    public bool RemoveAttribute(string name);
    /// <summary>Executes the SetAttributeInNamespace operation.</summary>
    public void SetAttributeInNamespace(string namespaceUri, string qualifiedName, string value);
    /// <summary>Executes the RemoveAttributeInNamespace operation.</summary>
    public bool RemoveAttributeInNamespace(string namespaceUri, string localName);
    /// <summary>Executes the AddClass operation.</summary>
    public void AddClass(string className);
    /// <summary>Executes the RemoveClass operation.</summary>
    public void RemoveClass(string className);
    /// <summary>Executes the HasClass operation.</summary>
    public bool HasClass(string className);
    /// <summary>Executes the GetClassTokens operation.</summary>
    public string[] GetClassTokens();
    /// <summary>Executes the ToggleClass operation.</summary>
    public bool ToggleClass(string className, bool present);
    /// <summary>Executes the ReplaceClass operation.</summary>
    public bool ReplaceClass(string oldClassName, string newClassName);
    /// <summary>Executes the GetDatasetValue operation.</summary>
    public string? GetDatasetValue(string name);
    /// <summary>Executes the HasDatasetValue operation.</summary>
    public bool HasDatasetValue(string name);
    /// <summary>Executes the SetDatasetValue operation.</summary>
    public void SetDatasetValue(string name, string value);
    /// <summary>Executes the RemoveDatasetValue operation.</summary>
    public bool RemoveDatasetValue(string name);
    /// <summary>Executes the GetDatasetNames operation.</summary>
    public string[] GetDatasetNames();
    /// <summary>Executes the SetData operation.</summary>
    public void SetData(string value);
    /// <summary>Executes the GetSourceSpan operation.</summary>
    public HtmlSourceSpan GetSourceSpan();
    /// <summary>Executes the HasSourceLocation operation.</summary>
    public bool HasSourceLocation();
    /// <summary>Executes the IsSourceBacked operation.</summary>
    public bool IsSourceBacked();
    /// <summary>Executes the IsGenerated operation.</summary>
    public bool IsGenerated();
    /// <summary>Executes the GetMutationVersion operation.</summary>
    public long GetMutationVersion();
    /// <summary>Executes the GetTemplateContent operation.</summary>
    public HtmlNode? GetTemplateContent();
    /// <summary>Executes the IsScriptInert operation.</summary>
    public bool IsScriptInert();
    /// <summary>Executes the Rename operation.</summary>
    public void Rename(string qualifiedName);
    /// <summary>Executes the RenameInNamespace operation.</summary>
    public void RenameInNamespace(string namespaceUri, string qualifiedName);
    /// <summary>Executes the Clone operation.</summary>
    public HtmlNode Clone(bool deep);
    /// <summary>Executes the CloneShallow operation.</summary>
    public HtmlNode CloneShallow();
    /// <summary>Executes the CloneDeep operation.</summary>
    public HtmlNode CloneDeep();
    /// <summary>Executes the Append operation.</summary>
    public void Append(HtmlNode child);
    /// <summary>Executes the Prepend operation.</summary>
    public void Prepend(HtmlNode child);
    /// <summary>Executes the InsertBefore operation.</summary>
    public void InsertBefore(HtmlNode child, HtmlNode referenceChild);
    /// <summary>Executes the InsertAfter operation.</summary>
    public void InsertAfter(HtmlNode child, HtmlNode referenceChild);
    /// <summary>Executes the ReplaceWith operation.</summary>
    public void ReplaceWith(HtmlNode replacement);
    /// <summary>Executes the ReplaceChild operation.</summary>
    public HtmlNode ReplaceChild(HtmlNode replacement, HtmlNode child);
    /// <summary>Executes the Wrap operation.</summary>
    public HtmlNode Wrap(HtmlNode wrapper);
    /// <summary>Executes the Remove operation.</summary>
    public void Remove();
    /// <summary>Executes the Detach operation.</summary>
    public HtmlNode Detach();
    /// <summary>Executes the Unwrap operation.</summary>
    public void Unwrap();
    /// <summary>Executes the Empty operation.</summary>
    public void Empty();
    /// <summary>Executes the SetTextContent operation.</summary>
    public void SetTextContent(string value);
    /// <summary>Executes the SetInnerHtml operation.</summary>
    public void SetInnerHtml(string html);
    /// <summary>Executes the SetOuterHtml operation.</summary>
    public void SetOuterHtml(string html);
    /// <summary>Executes the Normalize operation.</summary>
    public void Normalize();
}
