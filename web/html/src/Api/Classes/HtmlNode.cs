using System;
using System.Text;

namespace html;
/// <summary>Represents a document, fragment, element, text, comment, or document-type node in an HTML tree.</summary>
public class HtmlNode : IHtmlNode
{
    internal HtmlNodeKind kind;
    internal HtmlDocument ownerDocument;
    internal HtmlNode? parent;
    internal HtmlNode? firstChild;
    internal HtmlNode? lastChild;
    internal HtmlNode? previousSibling;
    internal HtmlNode? nextSibling;
    internal HtmlNode[] children;
    internal int childCount;
    internal HtmlNamespace namespaceKind;
    internal string namespaceUri;
    internal string name;
    internal string localName;
    internal string prefix;
    internal string data;
    internal HtmlAttribute[] attributes;
    internal int attributeCount;
    internal bool hasDoctypeName;
    internal bool hasPublicIdentifier;
    internal string publicIdentifier;
    internal bool hasSystemIdentifier;
    internal string systemIdentifier;
    internal bool forceQuirks;
    internal HtmlNode? templateContent;
    internal HtmlNode? templateHost;
    internal bool scriptInert;
    internal HtmlSourceSpan sourceSpan;
    internal bool sourceBacked;
    internal long ownTextCacheVersion;
    internal string ownTextCache;
    internal long textContentCacheVersion;
    internal string textContentCache;
    internal long normalizedTextContentCacheVersion;
    internal string normalizedTextContentCache;
    internal HtmlNode(HtmlNodeKind kind, HtmlDocument ownerDocument, HtmlSourceSpan sourceSpan)
    {
        this.kind = kind;
        this.ownerDocument = ownerDocument;
        this.parent = null;
        this.firstChild = null;
        this.lastChild = null;
        this.previousSibling = null;
        this.nextSibling = null;
        this.children = new HtmlNode[0];
        this.childCount = 0;
        this.namespaceKind = HtmlNamespace.None;
        this.namespaceUri = "";
        this.name = "";
        this.localName = "";
        this.prefix = "";
        this.data = "";
        this.attributes = new HtmlAttribute[0];
        this.attributeCount = 0;
        this.hasDoctypeName = false;
        this.hasPublicIdentifier = false;
        this.publicIdentifier = "";
        this.hasSystemIdentifier = false;
        this.systemIdentifier = "";
        this.forceQuirks = false;
        this.templateContent = null;
        this.templateHost = null;
        this.scriptInert = false;
        this.sourceSpan = sourceSpan;
        this.sourceBacked = sourceSpan.IsKnown();
        this.ownTextCacheVersion = -1;
        this.ownTextCache = "";
        this.textContentCacheVersion = -1;
        this.textContentCache = "";
        this.normalizedTextContentCacheVersion = -1;
        this.normalizedTextContentCache = "";
    }

    internal static HtmlNode CreateDocumentStorage(HtmlDocument ownerDocument, HtmlSourceSpan sourceSpan)
    {
        ValidateOwner(ownerDocument);
        ValidateSpan(sourceSpan);
        return new HtmlNode(HtmlNodeKind.Document, ownerDocument, sourceSpan);
    }

    internal static HtmlNode CreateFragmentStorage(HtmlDocument ownerDocument, HtmlSourceSpan sourceSpan)
    {
        ValidateOwner(ownerDocument);
        ValidateSpan(sourceSpan);
        return new HtmlNode(HtmlNodeKind.Fragment, ownerDocument, sourceSpan);
    }

    internal static HtmlNode CreateDocumentTypeStorage(HtmlDocument ownerDocument, bool hasName, string name, bool hasPublicIdentifier, string publicIdentifier, bool hasSystemIdentifier, string systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        ValidateOwner(ownerDocument);
        ValidateOptionalValue(hasName, name, "name");
        ValidateOptionalValue(hasPublicIdentifier, publicIdentifier, "publicIdentifier");
        ValidateOptionalValue(hasSystemIdentifier, systemIdentifier, "systemIdentifier");
        ValidateSpan(sourceSpan);
        HtmlNode result = new HtmlNode(HtmlNodeKind.DocumentType, ownerDocument, sourceSpan);
        result.hasDoctypeName = hasName;
        result.name = name;
        result.localName = name;
        result.hasPublicIdentifier = hasPublicIdentifier;
        result.publicIdentifier = publicIdentifier;
        result.hasSystemIdentifier = hasSystemIdentifier;
        result.systemIdentifier = systemIdentifier;
        result.forceQuirks = forceQuirks;
        return result;
    }

    internal static HtmlNode CreateElementStorage(HtmlDocument ownerDocument, HtmlNamespace namespaceKind, string namespaceUri, string name, string localName, string prefix, HtmlAttribute[] attributes, bool createTemplateContent, HtmlSourceSpan sourceSpan)
    {
        ValidateOwner(ownerDocument);
        ValidateNamespace(namespaceKind, namespaceUri);
        ValidateName(name, "name");
        ValidateName(localName, "localName");
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        ValidateAttributes(attributes);
        ValidateSpan(sourceSpan);
        if (createTemplateContent && (namespaceKind != HtmlNamespace.Html || localName != "template"))
        {
            throw new ArgumentException("Only an HTML template element can own template content.", "createTemplateContent");
        }

        HtmlNode result = new HtmlNode(HtmlNodeKind.Element, ownerDocument, sourceSpan);
        result.namespaceKind = namespaceKind;
        result.namespaceUri = namespaceUri;
        result.name = name;
        result.localName = localName;
        result.prefix = prefix;
        result.attributes = CopyAttributes(attributes);
        result.attributeCount = attributes.Length;
        result.scriptInert = (namespaceKind == HtmlNamespace.Html || namespaceKind == HtmlNamespace.Svg) && localName == "script";
        if (createTemplateContent)
        {
            HtmlNode content = CreateFragmentStorage(ownerDocument, HtmlSourceSpan.CreateUnknown());
            result.templateContent = content;
            content.templateHost = result;
        }

        return result;
    }

    internal static HtmlNode CreateTextStorage(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan)
    {
        return CreateDataStorage(HtmlNodeKind.Text, ownerDocument, data, sourceSpan);
    }

    internal static HtmlNode CreateCommentStorage(HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan)
    {
        return CreateDataStorage(HtmlNodeKind.Comment, ownerDocument, data, sourceSpan);
    }

    internal HtmlNodeKind GetKindStorage()
    {
        return this.kind;
    }

    /// <summary>Gets which concrete DOM node form this tagged node represents.</summary>
    /// <returns>Document, fragment, document type, element, text, or comment.</returns>
    public HtmlNodeKind GetKind()
    {
        return this.kind;
    }

    /// <summary>Gets the owner document.</summary>
    /// <returns>The owning document. Returns null when this node is the document root.</returns>
    public HtmlDocument? GetOwnerDocument()
    {
        if (this.kind == HtmlNodeKind.Document)
        {
            return null;
        }

        return this.ownerDocument;
    }

    /// <summary>Gets the parent.</summary>
    /// <returns>The parent node. Returns null when this node is detached or is a tree root.</returns>
    public HtmlNode? GetParent()
    {
        return this.parent;
    }

    /// <summary>Gets the first child.</summary>
    /// <returns>The first child. Returns null when this node has no children.</returns>
    public HtmlNode? GetFirstChild()
    {
        return this.firstChild;
    }

    /// <summary>Gets the last child.</summary>
    /// <returns>The last child. Returns null when this node has no children.</returns>
    public HtmlNode? GetLastChild()
    {
        return this.lastChild;
    }

    /// <summary>Gets the previous sibling.</summary>
    /// <returns>The previous sibling. Returns null when this node is the first child or is detached.</returns>
    public HtmlNode? GetPreviousSibling()
    {
        return this.previousSibling;
    }

    /// <summary>Gets the next sibling.</summary>
    /// <returns>The next sibling. Returns null when this node is the last child or is detached.</returns>
    public HtmlNode? GetNextSibling()
    {
        return this.nextSibling;
    }

    /// <summary>Gets the children.</summary>
    /// <returns>A snapshot of the children; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetChildren()
    {
        return this.GetChildrenStorage();
    }

    /// <summary>Gets the child elements.</summary>
    /// <returns>A snapshot of the child elements; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetChildElements()
    {
        int count = 0;
        int index = 0;
        while (index < this.childCount)
        {
            if (this.children[index].kind == HtmlNodeKind.Element)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        HtmlNode[] result = new HtmlNode[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.childCount)
        {
            HtmlNode child = this.children[index];
            if (child.kind == HtmlNodeKind.Element)
            {
                result[outputIndex] = child;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the first element child.</summary>
    /// <returns>The first child whose kind is element, or null when there is none.</returns>
    public HtmlNode? GetFirstElementChild()
    {
        HtmlNode? current = this.firstChild;
        while (current != null && current.kind != HtmlNodeKind.Element)
        {
            current = current.nextSibling;
        }

        return current;
    }

    /// <summary>Gets the last element child.</summary>
    /// <returns>The last child whose kind is element, or null when there is none.</returns>
    public HtmlNode? GetLastElementChild()
    {
        HtmlNode? current = this.lastChild;
        while (current != null && current.kind != HtmlNodeKind.Element)
        {
            current = current.previousSibling;
        }

        return current;
    }

    /// <summary>Gets the siblings.</summary>
    /// <returns>A snapshot of the siblings; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetSiblings()
    {
        if (this.parent == null)
        {
            return new HtmlNode[0];
        }

        HtmlNode[] result = new HtmlNode[this.parent.childCount - 1];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < this.parent.childCount)
        {
            HtmlNode sibling = this.parent.children[sourceIndex];
            if (sibling != this)
            {
                result[outputIndex] = sibling;
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        return result;
    }

    /// <summary>Gets the element siblings.</summary>
    /// <returns>A snapshot of the element siblings; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetElementSiblings()
    {
        if (this.parent == null)
        {
            return new HtmlNode[0];
        }

        int count = 0;
        int index = 0;
        while (index < this.parent.childCount)
        {
            HtmlNode sibling = this.parent.children[index];
            if (sibling != this && sibling.kind == HtmlNodeKind.Element)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        HtmlNode[] result = new HtmlNode[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.parent.childCount)
        {
            HtmlNode sibling = this.parent.children[index];
            if (sibling != this && sibling.kind == HtmlNodeKind.Element)
            {
                result[outputIndex] = sibling;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Finds the nearest preceding sibling whose kind is <see cref = "HtmlNodeKind.Element"/>.</summary>
    /// <returns>The preceding element sibling. Returns null when no preceding element sibling exists.</returns>
    public HtmlNode? GetPreviousElementSibling()
    {
        HtmlNode? current = this.previousSibling;
        while (current != null && current.kind != HtmlNodeKind.Element)
        {
            current = current.previousSibling;
        }

        return current;
    }

    /// <summary>Finds the nearest following sibling whose kind is <see cref = "HtmlNodeKind.Element"/>.</summary>
    /// <returns>The following element sibling. Returns null when no following element sibling exists.</returns>
    public HtmlNode? GetNextElementSibling()
    {
        HtmlNode? current = this.nextSibling;
        while (current != null && current.kind != HtmlNodeKind.Element)
        {
            current = current.nextSibling;
        }

        return current;
    }

    /// <summary>Gets the ancestors.</summary>
    /// <returns>A snapshot of the ancestors; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetAncestors()
    {
        int count = 0;
        HtmlNode? current = this.parent;
        while (current != null)
        {
            count = count + 1;
            current = current.parent;
        }

        HtmlNode[] result = new HtmlNode[count];
        current = this.parent;
        int index = 0;
        while (current != null)
        {
            result[index] = current;
            index = index + 1;
            current = current.parent;
        }

        return result;
    }

    /// <summary>Gets the descendants.</summary>
    /// <returns>A snapshot of the descendants; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] GetDescendants()
    {
        return HtmlTreeWalker.GetDescendants(this);
    }

    /// <summary>Finds the first descendant element with the supplied ID.</summary>
    /// <param name = "id">The exact <c>id</c> attribute value to match.</param>
    /// <returns>The matching element. Returns null when no descendant has the requested ID.</returns>
    public HtmlNode? FindById(string id)
    {
        return this.FindByIdWithLimit(id, HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds the first descendant element with the supplied ID.</summary>
    /// <param name = "id">The exact <c>id</c> attribute value to match.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The matching element. Returns null when no descendant has the requested ID.</returns>
    public HtmlNode? FindByIdWithLimit(string id, int maximumVisits)
    {
        return HtmlTreeSearch.FindById(this, id, maximumVisits);
    }

    /// <summary>Finds descendant elements with the supplied tag name in document order.</summary>
    /// <param name = "tagName">The element tag name to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByTag(string tagName)
    {
        return this.FindByTagWithLimits(tagName, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds descendant elements with the supplied tag name in document order.</summary>
    /// <param name = "tagName">The element tag name to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByTagWithLimits(string tagName, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindByTag(this, tagName, maximumResults, maximumVisits);
    }

    /// <summary>Finds descendant elements containing the supplied class token in document order.</summary>
    /// <param name = "className">The single class token to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByClass(string className)
    {
        return this.FindByClassWithLimits(className, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds descendant elements containing the supplied class token in document order.</summary>
    /// <param name = "className">The single class token to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByClassWithLimits(string className, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindByClass(this, className, maximumResults, maximumVisits);
    }

    /// <summary>Finds descendant elements whose <c>name</c> attribute has the supplied value.</summary>
    /// <param name = "name">The exact <c>name</c> attribute value to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByName(string name)
    {
        return this.FindByNameWithLimits(name, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds descendant elements whose <c>name</c> attribute has the supplied value.</summary>
    /// <param name = "name">The exact <c>name</c> attribute value to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByNameWithLimits(string name, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindByName(this, name, maximumResults, maximumVisits);
    }

    /// <summary>Finds descendant elements that contain the named attribute.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttribute(string attributeName)
    {
        return this.FindByAttributeWithLimits(attributeName, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds descendant elements that contain the named attribute.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeWithLimits(string attributeName, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindByAttribute(this, attributeName, maximumResults, maximumVisits);
    }

    /// <summary>Finds descendant elements whose named attribute has the supplied value.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "value">The exact attribute value to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeValue(string attributeName, string value)
    {
        return this.FindByAttributeValueWithLimits(attributeName, value, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds descendant elements whose named attribute has the supplied value.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "value">The exact attribute value to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeValueWithLimits(string attributeName, string value, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindByAttribute(this, attributeName, value, maximumResults, maximumVisits);
    }

    /// <summary>Finds the first node accepted by the predicate in document order.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <returns>The first accepted node. Returns null when the predicate accepts no node.</returns>
    public HtmlNode? FindFirst(Func<HtmlNode, bool> predicate)
    {
        return this.FindFirstWithLimit(predicate, HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds the first node accepted by the predicate in document order.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The first accepted node. Returns null when the predicate accepts no node.</returns>
    public HtmlNode? FindFirstWithLimit(Func<HtmlNode, bool> predicate, int maximumVisits)
    {
        return HtmlTreeSearch.FindFirst(this, predicate, maximumVisits);
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAll(Func<HtmlNode, bool> predicate)
    {
        return this.FindAllWithLimits(predicate, HtmlTreeSearch.GetDefaultMaximumResults(), HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAllWithLimits(Func<HtmlNode, bool> predicate, int maximumResults, int maximumVisits)
    {
        return HtmlTreeSearch.FindAll(this, predicate, maximumResults, maximumVisits);
    }

    /// <summary>Finds the nearest inclusive ancestor that satisfies the predicate or selector.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <returns>The nearest accepted node, including this node. Returns null when no inclusive ancestor is accepted.</returns>
    public HtmlNode? Closest(Func<HtmlNode, bool> predicate)
    {
        return this.ClosestWithLimit(predicate, HtmlTreeSearch.GetDefaultMaximumVisits());
    }

    /// <summary>Finds the nearest inclusive ancestor that satisfies the predicate or selector.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The nearest accepted node, including this node. Returns null when no inclusive ancestor is accepted.</returns>
    public HtmlNode? ClosestWithLimit(Func<HtmlNode, bool> predicate, int maximumVisits)
    {
        return HtmlTreeSearch.Closest(this, predicate, maximumVisits);
    }

    /// <summary>Tests whether this node matches the supplied CSS selector.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate.</param>
    /// <returns><c>true</c> when the supplied value matches; otherwise, <c>false</c>.</returns>
    public bool Matches(string selector)
    {
        return HtmlSelectors.CompileOrThrowStorage(selector).Matches(this, this);
    }

    /// <summary>Finds the nearest inclusive ancestor that satisfies the predicate or selector.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate against this node and its ancestors.</param>
    /// <returns>The nearest matching node, including this node. Returns null when no inclusive ancestor matches.</returns>
    public HtmlNode? ClosestSelector(string selector)
    {
        HtmlSelector compiled = HtmlSelectors.CompileOrThrowStorage(selector);
        return HtmlSelectorQuery.Closest(compiled, this, HtmlLimits.CreateDefault(), System.Threading.CancellationToken.None);
    }

    /// <summary>Evaluates the selector against the selected subtree.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate against this subtree.</param>
    /// <returns>The first matching node. Returns null when the selector matches no node.</returns>
    public HtmlNode? QuerySelector(string selector)
    {
        return HtmlSelectors.CompileOrThrowStorage(selector).FindFirst(this);
    }

    /// <summary>Returns every node in the subtree that matches the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate against this subtree.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] QuerySelectorAll(string selector)
    {
        return HtmlSelectors.CompileOrThrowStorage(selector).FindAll(this);
    }

    /// <summary>Walks parent links to find the topmost node in this connected tree.</summary>
    /// <returns>The document, fragment, or detached subtree root containing this node.</returns>
    public HtmlNode GetRoot()
    {
        HtmlNode result = this;
        while (result.parent != null)
        {
            result = result.parent;
        }

        return result;
    }

    /// <summary>Counts this node's parent links up to its current tree root.</summary>
    /// <returns><c>0</c> for a root node; otherwise, the number of ancestors.</returns>
    public int GetDepth()
    {
        int result = 0;
        HtmlNode? current = this.parent;
        while (current != null)
        {
            result = result + 1;
            current = current.parent;
        }

        return result;
    }

    /// <summary>Determines whether this node is an ancestor of or identical to the supplied node.</summary>
    /// <param name = "other">The value to compare with this instance.</param>
    /// <returns><c>true</c> when this subtree contains the supplied node; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public bool Contains(HtmlNode other)
    {
        if (other == null)
        {
            throw new ArgumentNullException("other");
        }

        HtmlNode? current = other;
        while (current != null)
        {
            if (current == this)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    /// <summary>Walks the subtree in depth-first order and invokes the visitor on entry and exit.</summary>
    /// <param name = "visitor">The callback invoked for each visited node or traversal phase; returning <c>false</c> stops the walk.</param>
    /// <returns>The walk status and number of nodes entered or visited.</returns>
    public HtmlTreeWalkResult WalkDepthFirst(Func<HtmlNode, HtmlTreeVisitPhase, int, bool> visitor)
    {
        return this.WalkDepthFirstWithLimit(visitor, HtmlLimits.CreateDefault().GetMaximumVisits());
    }

    /// <summary>Walks the subtree in depth-first order and invokes the visitor on entry and exit.</summary>
    /// <param name = "visitor">The callback invoked for each visited node or traversal phase; returning <c>false</c> stops the walk.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The walk status and number of nodes entered or visited.</returns>
    public HtmlTreeWalkResult WalkDepthFirstWithLimit(Func<HtmlNode, HtmlTreeVisitPhase, int, bool> visitor, int maximumVisits)
    {
        return HtmlTreeWalker.WalkDepthFirst(this, visitor, maximumVisits);
    }

    /// <summary>Walks the subtree in breadth-first order and invokes the visitor once per node.</summary>
    /// <param name = "visitor">The callback invoked for each visited node or traversal phase; returning <c>false</c> stops the walk.</param>
    /// <returns>The walk status and number of nodes entered or visited.</returns>
    public HtmlTreeWalkResult WalkBreadthFirst(Func<HtmlNode, int, bool> visitor)
    {
        return this.WalkBreadthFirstWithLimit(visitor, HtmlLimits.CreateDefault().GetMaximumVisits());
    }

    /// <summary>Walks the subtree in breadth-first order and invokes the visitor once per node.</summary>
    /// <param name = "visitor">The callback invoked for each visited node or traversal phase; returning <c>false</c> stops the walk.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The walk status and number of nodes entered or visited.</returns>
    public HtmlTreeWalkResult WalkBreadthFirstWithLimit(Func<HtmlNode, int, bool> visitor, int maximumVisits)
    {
        return HtmlTreeWalker.WalkBreadthFirst(this, visitor, maximumVisits);
    }

    /// <summary>Gets the DOM-style name for this node.</summary>
    /// <returns>The qualified element or document-type name, or a <c>#document</c>, <c>#document-fragment</c>, <c>#text</c>, or <c>#comment</c> marker for other node kinds.</returns>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public string GetName()
    {
        switch (this.kind)
        {
            case HtmlNodeKind.Document:
                return "#document";
            case HtmlNodeKind.Fragment:
                return "#document-fragment";
            case HtmlNodeKind.DocumentType:
            case HtmlNodeKind.Element:
                return this.name;
            case HtmlNodeKind.Text:
                return "#text";
            case HtmlNodeKind.Comment:
                return "#comment";
            default:
                throw new InvalidOperationException("Unknown stored node kind.");
        }
    }

    /// <summary>Gets the namespace-local element name.</summary>
    /// <returns>The local element name; non-element nodes return an empty string.</returns>
    public string GetLocalName()
    {
        return this.localName;
    }

    /// <summary>Gets the namespace URI for an element node.</summary>
    /// <returns>The element namespace URI; non-element nodes return an empty string.</returns>
    public string GetNamespaceUri()
    {
        return this.namespaceUri;
    }

    /// <summary>Gets the namespace prefix stored on an element node.</summary>
    /// <returns>The prefix, or an empty string when the node has no prefix.</returns>
    public string GetPrefix()
    {
        return this.prefix;
    }

    /// <summary>Gets the character data stored by text and comment nodes.</summary>
    /// <returns>The stored data; node kinds without character data return an empty string.</returns>
    public string GetData()
    {
        return this.data;
    }

    /// <summary>Gets the effective language inherited by this node.</summary>
    /// <returns>The nearest <c>xml:lang</c> or <c>lang</c> value, the response content language, or an empty string when none is available.</returns>
    public string GetLanguage()
    {
        if (this.kind == HtmlNodeKind.Document)
        {
            return this.ownerDocument.GetLanguage();
        }

        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(),
            System.Threading.CancellationToken.None);
        string? language = HtmlSelectorMarkupMatcher.GetContentLanguage(this,
            budget);
        return language == null
            ? this.ownerDocument.GetContentLanguage() : language;
    }

    /// <summary>Gets the effective text direction inherited or computed for this node.</summary>
    /// <returns><c>ltr</c> or <c>rtl</c>, including resolution of <c>dir=auto</c>.</returns>
    public string GetDirection()
    {
        if (this.kind == HtmlNodeKind.Document)
        {
            return this.ownerDocument.GetDirection();
        }

        HtmlBudget budget = new HtmlBudget(HtmlLimits.CreateDefault(),
            System.Threading.CancellationToken.None);
        int direction = HtmlSelectorMarkupMatcher.GetDirection(this, budget);
        return direction == 0 ? "ltr" : "rtl";
    }

    /// <summary>Reports whether a document-type name is present.</summary>
    /// <returns><c>true</c> only for a document type whose name was present.</returns>
    public bool HasDoctypeName()
    {
        return this.kind == HtmlNodeKind.DocumentType && this.hasDoctypeName;
    }

    /// <summary>Gets the document-type name.</summary>
    /// <returns>The name when <see cref = "HasDoctypeName"/> is true; otherwise, an empty string.</returns>
    public string GetDoctypeName()
    {
        return this.kind == HtmlNodeKind.DocumentType ? this.name : "";
    }

    /// <summary>Reports whether a document type has a public identifier.</summary>
    /// <returns><c>true</c> only for a document type whose public identifier was present.</returns>
    public bool HasPublicIdentifier()
    {
        return this.kind == HtmlNodeKind.DocumentType
            && this.hasPublicIdentifier;
    }

    /// <summary>Gets the document-type public identifier.</summary>
    /// <returns>The public identifier when <see cref = "HasPublicIdentifier"/> is true; otherwise, an empty string.</returns>
    public string GetPublicIdentifier()
    {
        return this.kind == HtmlNodeKind.DocumentType
            ? this.publicIdentifier : "";
    }

    /// <summary>Reports whether a document type has a system identifier.</summary>
    /// <returns><c>true</c> only for a document type whose system identifier was present.</returns>
    public bool HasSystemIdentifier()
    {
        return this.kind == HtmlNodeKind.DocumentType
            && this.hasSystemIdentifier;
    }

    /// <summary>Gets the document-type system identifier.</summary>
    /// <returns>The system identifier when <see cref = "HasSystemIdentifier"/> is true; otherwise, an empty string.</returns>
    public string GetSystemIdentifier()
    {
        return this.kind == HtmlNodeKind.DocumentType
            ? this.systemIdentifier : "";
    }

    /// <summary>Reports whether a document type carries the parser force-quirks flag.</summary>
    /// <returns><c>true</c> only for a force-quirks document type.</returns>
    public bool IsForceQuirks()
    {
        return this.kind == HtmlNodeKind.DocumentType && this.forceQuirks;
    }

    /// <summary>Concatenates text-node data from this node and all descendants in tree order.</summary>
    /// <returns>The unnormalized descendant text; comments contribute data only when this node itself is a comment, and unsupported node kinds return an empty string.</returns>
    public string GetTextContent()
    {
        long version = this.ownerDocument.GetMutationVersionStorage();
        if (this.textContentCacheVersion == version)
        {
            return this.textContentCache;
        }

        string result = HtmlTextContent.GetDescendantText(this);
        this.textContentCache = result;
        this.textContentCacheVersion = version;
        return result;
    }

    /// <summary>Concatenates only the direct text-node children of this node.</summary>
    /// <returns>The unnormalized direct-child text; a text node returns its own data and unsupported node kinds return an empty string.</returns>
    public string GetOwnText()
    {
        long version = this.ownerDocument.GetMutationVersionStorage();
        if (this.ownTextCacheVersion == version)
        {
            return this.ownTextCache;
        }

        string result = HtmlTextContent.GetOwnText(this);
        this.ownTextCache = result;
        this.ownTextCacheVersion = version;
        return result;
    }

    /// <summary>Serializes this node's children using the default canonical HTML settings.</summary>
    /// <returns>The serialized child markup, or an empty string when the node has no serializable children.</returns>
    public string GetInnerHtml()
    {
        return HtmlSerializer.SerializeChildren(this);
    }

    /// <summary>Serializes this node and its descendants using the default canonical HTML settings.</summary>
    /// <returns>The serialized markup for the complete subtree.</returns>
    public string GetOuterHtml()
    {
        return HtmlSerializer.Serialize(this);
    }

    /// <summary>Returns descendant text with Unicode whitespace runs collapsed for simple display or matching.</summary>
    /// <returns>The descendant text with leading and trailing whitespace removed and each internal whitespace run replaced by one ASCII space.</returns>
    public string GetNormalizedTextContent()
    {
        long version = this.ownerDocument.GetMutationVersionStorage();
        if (this.normalizedTextContentCacheVersion == version)
        {
            return this.normalizedTextContentCache;
        }

        string result = HtmlTextContent.Normalize(this.GetTextContent());
        this.normalizedTextContentCache = result;
        this.normalizedTextContentCacheVersion = version;
        return result;
    }

    /// <summary>Gets this element's attributes in DOM order.</summary>
    /// <returns>A snapshot of the attributes; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlAttribute[] GetAttributes()
    {
        return this.GetAttributesStorage();
    }

    /// <summary>Gets the decoded value of an unnamespaced attribute.</summary>
    /// <param name = "name">The unnamespaced attribute name to find.</param>
    /// <returns>The attribute value. Returns null when the named attribute is absent.</returns>
    public string? GetAttribute(string name)
    {
        string lookupName = HtmlTreeMutation.NormalizeAttributeLookupName(this, name);
        int index = this.FindAttributeByName(lookupName);
        return index < 0 ? null : this.attributes[index].GetValue();
    }

    /// <summary>Gets the decoded value of an attribute identified by namespace URI and local name.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns>The attribute value. Returns null when the namespaced attribute is absent.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public string? GetAttributeInNamespace(string namespaceUri, string localName)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        int index = this.FindAttributeByNamespace(namespaceUri, localName);
        return index < 0 ? null : this.attributes[index].GetValue();
    }

    /// <summary>Reports whether this element contains an attribute with the supplied expanded name.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns><c>true</c> when the expanded name is present; otherwise, <c>false</c>.</returns>
    public bool HasAttributeInNamespace(string namespaceUri, string localName)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return this.FindAttributeByNamespace(namespaceUri, localName) >= 0;
    }

    /// <summary>Gets the space separated tokens.</summary>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns>A snapshot of the space separated tokens; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetSpaceSeparatedTokens(string attributeName)
    {
        string? value = this.GetAttribute(attributeName);
        return value == null ? new string[0] : HtmlTokenLists.ParseSpaceSeparated(value);
    }

    /// <summary>Gets the comma separated tokens.</summary>
    /// <param name = "attributeName">The attribute name.</param>
    /// <returns>A snapshot of the comma separated tokens; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public string[] GetCommaSeparatedTokens(string attributeName)
    {
        string? value = this.GetAttribute(attributeName);
        return value == null ? new string[0] : HtmlTokenLists.ParseCommaSeparated(value);
    }

    /// <summary>Resolves an unnamespaced URL-bearing attribute against this node's document base URL.</summary>
    /// <param name = "attributeName">The attribute name to read using HTML attribute-name matching rules.</param>
    /// <returns>The resolved URL and classification flags, or a failed result with diagnostics when the attribute is absent, does not contain one URL, or cannot be resolved.</returns>
    public HtmlUrlResult GetAbsoluteUrl(string attributeName)
    {
        return HtmlDomUrls.ResolveAttribute(this, attributeName);
    }

    /// <summary>Resolves an unnamespaced URL-bearing attribute against this node's document base URL within explicit limits.</summary>
    /// <param name = "attributeName">The attribute name to read using HTML attribute-name matching rules.</param>
    /// <param name = "limits">The limits copied and enforced for this operation.</param>
    /// <param name = "cancellationToken">The token checked while the operation is running.</param>
    /// <returns>The resolved URL and classification flags, or a failed result whose diagnostics and limit flag explain why resolution did not complete.</returns>
    public HtmlUrlResult GetAbsoluteUrlWithLimits(string attributeName, HtmlLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return HtmlDomUrls.ResolveAttribute(this, attributeName, limits, cancellationToken);
    }

    /// <summary>Resolves a namespaced URL-bearing attribute against this node's document base URL.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns>The resolved URL and classification flags, or a failed result with diagnostics when the attribute is absent, does not contain one URL, or cannot be resolved.</returns>
    public HtmlUrlResult GetAbsoluteUrlInNamespace(string namespaceUri, string localName)
    {
        return HtmlDomUrls.ResolveAttribute(this, namespaceUri, localName);
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in this subtree.</summary>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindLinks()
    {
        return HtmlDomUrls.FindLinks(this);
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in this subtree.</summary>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindLinksWithLimits(int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindLinks(this, maximumResults, maximumVisits);
    }

    /// <summary>Finds media elements with a single-URL or <c>srcset</c> attribute in this subtree.</summary>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindMedia()
    {
        return HtmlDomUrls.FindMedia(this);
    }

    /// <summary>Finds media elements with a single-URL or <c>srcset</c> attribute in this subtree.</summary>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindMediaWithLimits(int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindMedia(this, maximumResults, maximumVisits);
    }

    /// <summary>Reports whether this element contains the named unnamespaced attribute.</summary>
    /// <param name = "name">The unnamespaced attribute name to test.</param>
    /// <returns><c>true</c> when the named attribute is present; otherwise, <c>false</c>.</returns>
    public bool HasAttribute(string name)
    {
        string lookupName = HtmlTreeMutation.NormalizeAttributeLookupName(this, name);
        return this.FindAttributeByName(lookupName) >= 0;
    }

    /// <summary>Adds or replaces an unnamespaced attribute on this element.</summary>
    /// <param name = "name">The unnamespaced attribute name to add or replace.</param>
    /// <param name = "value">The replacement attribute value.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetAttribute(string name, string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        string normalizedName = HtmlTreeMutation.NormalizeAttributeName(this, name);
        this.EnsureElementMutation();
        int index = this.FindAttributeByName(normalizedName);
        if (index >= 0 && this.attributes[index].GetValue() == value)
        {
            return;
        }

        if (index >= 0)
        {
            HtmlAttribute current = this.attributes[index];
            HtmlAttribute replacement = HtmlAttribute.CreateNamespaced(current.GetName(), current.GetLocalName(), current.GetPrefix(), current.GetNamespaceUri(), value, HtmlSourceSpan.CreateUnknown());
            this.ReplaceAttribute(index, replacement);
        }
        else
        {
            HtmlAttribute added = HtmlAttribute.Create(normalizedName, value, HtmlSourceSpan.CreateUnknown());
            this.AppendAttribute(added);
        }

        this.RecordNodeMutation();
    }

    /// <summary>Removes the attribute when it is present.</summary>
    /// <param name = "name">The unnamespaced attribute name to remove.</param>
    /// <returns><c>true</c> when the named attribute is present; otherwise, <c>false</c>.</returns>
    public bool RemoveAttribute(string name)
    {
        string lookupName = HtmlTreeMutation.NormalizeAttributeLookupName(this, name);
        this.EnsureElementMutation();
        int index = this.FindAttributeByName(lookupName);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAttributeAt(index);
        this.RecordNodeMutation();
        return true;
    }

    /// <summary>Adds or replaces an attribute identified by namespace and qualified name.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "qualifiedName">The qualified attribute name.</param>
    /// <param name = "value">The replacement value.</param>
    public void SetAttributeInNamespace(string namespaceUri,
        string qualifiedName, string value)
    {
        HtmlAttribute replacement =
            HtmlTreeMutation.CreateNamespacedAttribute(namespaceUri,
                qualifiedName, value, HtmlSourceSpan.CreateUnknown());
        this.EnsureElementMutation();
        int index = this.FindAttributeByNamespace(
            replacement.GetNamespaceUri(), replacement.GetLocalName());
        if (index >= 0)
        {
            HtmlAttribute current = this.attributes[index];
            if (current.GetName() == replacement.GetName()
                && current.GetValue() == replacement.GetValue())
            {
                return;
            }

            this.ReplaceAttribute(index, replacement);
        }
        else
        {
            this.AppendAttribute(replacement);
        }

        this.RecordNodeMutation();
    }

    /// <summary>Removes an attribute identified by namespace URI and local name.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "localName">The namespace-local name.</param>
    /// <returns><c>true</c> when an attribute was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveAttributeInNamespace(string namespaceUri,
        string localName)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        this.EnsureElementMutation();
        int index = this.FindAttributeByNamespace(namespaceUri, localName);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAttributeAt(index);
        this.RecordNodeMutation();
        return true;
    }

    /// <summary>Adds one class token to this element unless it is already present.</summary>
    /// <param name = "className">The single class token to match.</param>
    public void AddClass(string className)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        this.EnsureElementMutation();
        int index = this.FindAttributeByName("class");
        if (index < 0)
        {
            this.AppendAttribute(HtmlAttribute.Create("class", className, HtmlSourceSpan.CreateUnknown()));
            this.RecordNodeMutation();
            return;
        }

        string value = this.attributes[index].GetValue();
        if (HtmlTreeMutation.ContainsClassToken(value, className))
        {
            return;
        }

        this.ReplaceAttributeValue(index, HtmlTreeMutation.AddClassToken(value, className));
        this.RecordNodeMutation();
    }

    /// <summary>Removes the class when it is present.</summary>
    /// <param name = "className">The single class token to match.</param>
    public void RemoveClass(string className)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        this.EnsureElementMutation();
        int index = this.FindAttributeByName("class");
        if (index < 0)
        {
            return;
        }

        string value = this.attributes[index].GetValue();
        if (!HtmlTreeMutation.ContainsClassToken(value, className))
        {
            return;
        }

        this.ReplaceAttributeValue(index, HtmlTreeMutation.RemoveClassToken(value, className));
        this.RecordNodeMutation();
    }

    /// <summary>Reports whether this element's <c>class</c> attribute contains the requested token.</summary>
    /// <param name = "className">The single class token to match.</param>
    /// <returns><c>true</c> when the element contains the requested class token; otherwise, <c>false</c>.</returns>
    public bool HasClass(string className)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        int index = this.FindAttributeByName("class");
        return index >= 0 && HtmlTreeMutation.ContainsClassToken(this.attributes[index].GetValue(), className);
    }

    /// <summary>Gets the distinct class tokens in their stored order.</summary>
    /// <returns>A normalized snapshot of this element's class tokens.</returns>
    public string[] GetClassTokens()
    {
        string? value = this.GetAttribute("class");
        return value == null ? new string[0]
            : HtmlTreeMutation.ParseClassTokens(value, "").ToArray();
    }

    /// <summary>Adds or removes a class token according to an explicit state.</summary>
    /// <param name = "className">The single class token.</param>
    /// <param name = "present">Whether the token must be present after the call.</param>
    /// <returns>The requested final state.</returns>
    public bool ToggleClass(string className, bool present)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        this.EnsureElementMutation();
        bool found = this.HasClass(className);
        if (found != present)
        {
            if (present)
            {
                this.AddClass(className);
            }
            else
            {
                this.RemoveClass(className);
            }
        }

        return present;
    }

    /// <summary>Replaces one class token while preserving token order.</summary>
    /// <param name = "oldClassName">The token to replace.</param>
    /// <param name = "newClassName">The replacement token.</param>
    /// <returns><c>true</c> when the old token was present; otherwise, <c>false</c>.</returns>
    public bool ReplaceClass(string oldClassName, string newClassName)
    {
        HtmlTreeMutation.ValidateClassToken(oldClassName);
        HtmlTreeMutation.ValidateClassToken(newClassName);
        this.EnsureElementMutation();
        int index = this.FindAttributeByName("class");
        if (index < 0)
        {
            return false;
        }

        string value = this.attributes[index].GetValue();
        if (!HtmlTreeMutation.ContainsClassToken(value, oldClassName))
        {
            return false;
        }

        if (oldClassName == newClassName)
        {
            return true;
        }

        this.ReplaceAttributeValue(index,
            HtmlTreeMutation.ReplaceClassToken(value, oldClassName,
                newClassName));
        this.RecordNodeMutation();
        return true;
    }

    /// <summary>Gets a value from this element's <c>data-*</c> attribute map.</summary>
    /// <param name = "name">The camel-cased dataset name.</param>
    /// <returns>The value, or null when the corresponding attribute is absent.</returns>
    public string? GetDatasetValue(string name)
    {
        return this.GetAttribute(
            HtmlTreeMutation.DatasetNameToAttribute(name));
    }

    /// <summary>Reports whether this element's dataset contains a name.</summary>
    /// <param name = "name">The camel-cased dataset name.</param>
    /// <returns><c>true</c> when the corresponding attribute is present; otherwise, <c>false</c>.</returns>
    public bool HasDatasetValue(string name)
    {
        return this.GetDatasetValue(name) != null;
    }

    /// <summary>Adds or replaces a value in this element's dataset.</summary>
    /// <param name = "name">The camel-cased dataset name.</param>
    /// <param name = "value">The value to store.</param>
    public void SetDatasetValue(string name, string value)
    {
        this.SetAttribute(HtmlTreeMutation.DatasetNameToAttribute(name),
            value);
    }

    /// <summary>Removes a value from this element's dataset.</summary>
    /// <param name = "name">The camel-cased dataset name.</param>
    /// <returns><c>true</c> when a value was removed; otherwise, <c>false</c>.</returns>
    public bool RemoveDatasetValue(string name)
    {
        return this.RemoveAttribute(
            HtmlTreeMutation.DatasetNameToAttribute(name));
    }

    /// <summary>Gets dataset names in attribute order.</summary>
    /// <returns>A snapshot of camel-cased dataset names.</returns>
    public string[] GetDatasetNames()
    {
        int count = 0;
        int index = 0;
        while (index < this.attributeCount)
        {
            HtmlAttribute attribute = this.attributes[index];
            if (attribute.GetNamespaceUri().Length == 0
                && HtmlTreeMutation.AttributeToDatasetName(
                    attribute.GetLocalName()) != null)
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] result = new string[count];
        index = 0;
        int outputIndex = 0;
        while (index < this.attributeCount)
        {
            HtmlAttribute attribute = this.attributes[index];
            string? name = attribute.GetNamespaceUri().Length == 0
                ? HtmlTreeMutation.AttributeToDatasetName(
                    attribute.GetLocalName()) : null;
            if (name != null)
            {
                result[outputIndex] = name;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Replaces the data stored by a text or comment node.</summary>
    /// <param name = "value">The replacement text or comment data.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public void SetData(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (this.kind != HtmlNodeKind.Text && this.kind != HtmlNodeKind.Comment)
        {
            throw new InvalidOperationException("Only text and comment nodes contain mutable character data.");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.data == value)
        {
            return;
        }

        this.data = value;
        this.RecordNodeMutation();
    }

    /// <summary>Gets the input range from which this node was created.</summary>
    /// <returns>The half-open source span, or an explicitly unknown span for generated, programmatically created, or untracked nodes.</returns>
    public HtmlSourceSpan GetSourceSpan()
    {
        return this.sourceSpan;
    }

    /// <summary>Reports whether this node has known source coordinates.</summary>
    /// <returns><c>true</c> when <see cref = "GetSourceSpan"/> is known; otherwise, <c>false</c>.</returns>
    public bool HasSourceLocation()
    {
        return this.sourceSpan.IsKnown();
    }

    /// <summary>Reports whether this node represents source input rather than generated tree structure.</summary>
    /// <returns><c>true</c> for source-backed nodes, including when source tracking was disabled; otherwise, <c>false</c>.</returns>
    public bool IsSourceBacked()
    {
        return this.sourceBacked;
    }

    /// <summary>Reports whether this node was generated or created programmatically.</summary>
    /// <returns><c>true</c> when the node is not source-backed; otherwise, <c>false</c>.</returns>
    public bool IsGenerated()
    {
        return !this.sourceBacked;
    }

    /// <summary>Gets the current mutation version of the owning document.</summary>
    /// <returns>The document-wide mutation version.</returns>
    public long GetMutationVersion()
    {
        return this.ownerDocument.GetMutationVersionStorage();
    }

    /// <summary>Gets the template content.</summary>
    /// <returns>The template contents fragment. Returns null when this is not an HTML template element.</returns>
    public HtmlNode? GetTemplateContent()
    {
        return this.templateContent;
    }

    /// <summary>Reports whether script content under this node is marked as non-executable.</summary>
    /// <returns><c>true</c> when the script is marked as non-executable; otherwise, <c>false</c>.</returns>
    public bool IsScriptInert()
    {
        return this.scriptInert;
    }

    /// <summary>Renames this element while preserving its namespace.</summary>
    /// <param name = "qualifiedName">The replacement qualified name.</param>
    public void Rename(string qualifiedName)
    {
        this.RenameInNamespace(this.namespaceUri, qualifiedName);
    }

    /// <summary>Renames this element and replaces its namespace.</summary>
    /// <param name = "namespaceUri">The replacement namespace URI.</param>
    /// <param name = "qualifiedName">The replacement qualified name.</param>
    public void RenameInNamespace(string namespaceUri, string qualifiedName)
    {
        this.EnsureElementMutation();
        HtmlNode shape = this.ownerDocument.CreateElementWithSourceSpanStorage(
            namespaceUri, qualifiedName, HtmlSourceSpan.CreateUnknown());
        if (this.namespaceKind == shape.namespaceKind
            && this.namespaceUri == shape.namespaceUri
            && this.name == shape.name && this.localName == shape.localName
            && this.prefix == shape.prefix)
        {
            return;
        }

        bool wasTemplate = this.templateContent != null;
        bool willBeTemplate = shape.templateContent != null;
        if (wasTemplate && !willBeTemplate)
        {
            HtmlNode? oldContent = this.templateContent;
            if (oldContent == null)
            {
                throw new InvalidOperationException(
                    "Template content storage is inconsistent.");
            }

            HtmlNode[] values = oldContent.GetChildrenStorage();
            oldContent.CommitChildren(new HtmlNode[0]);
            oldContent.templateHost = null;
            this.templateContent = null;
            this.CommitChildren(values);
        }
        else if (!wasTemplate && willBeTemplate)
        {
            HtmlNode? newContent = shape.templateContent;
            if (newContent == null)
            {
                throw new InvalidOperationException(
                    "Template content creation is inconsistent.");
            }

            HtmlNode[] values = this.GetChildrenStorage();
            this.CommitChildren(new HtmlNode[0]);
            newContent.templateHost = this;
            shape.templateContent = null;
            newContent.CommitChildren(values);
            this.templateContent = newContent;
        }

        this.namespaceKind = shape.namespaceKind;
        this.namespaceUri = shape.namespaceUri;
        this.name = shape.name;
        this.localName = shape.localName;
        this.prefix = shape.prefix;
        this.scriptInert = (this.namespaceKind == HtmlNamespace.Html
            || this.namespaceKind == HtmlNamespace.Svg)
            && this.localName == "script";
        this.RecordNodeMutation();
    }

    /// <summary>Creates an owned copy of the node, optionally including its descendants.</summary>
    /// <param name = "deep">Whether descendant nodes are copied recursively.</param>
    /// <returns>The resulting HTML node.</returns>
    public HtmlNode Clone(bool deep)
    {
        return HtmlTreeClone.Clone(this, this.ownerDocument, deep);
    }

    /// <summary>Creates an owned copy of this node without descendants.</summary>
    /// <returns>A detached shallow copy.</returns>
    public HtmlNode CloneShallow()
    {
        return HtmlTreeClone.Clone(this, this.ownerDocument, false);
    }

    /// <summary>Creates an owned copy of this node and all descendants.</summary>
    /// <returns>A detached deep copy.</returns>
    public HtmlNode CloneDeep()
    {
        return HtmlTreeClone.Clone(this, this.ownerDocument, true);
    }

    /// <summary>Moves a node to the end of this node's child list.</summary>
    /// <param name = "child">The node to append, or a fragment whose children are appended in order.</param>
    /// <remarks>An attached node is detached from its current parent before insertion. A fragment itself is not inserted; its children are moved and the fragment becomes empty. Template elements receive children in their template-content fragment.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the child belongs to a different document.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or the insertion would violate node-kind, document-shape, or tree-cycle rules.</exception>
    public void Append(HtmlNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.InsertAt(child, target.childCount);
    }

    /// <summary>Moves a node to the start of this node's child list.</summary>
    /// <param name = "child">The node to prepend, or a fragment whose children are prepended in order.</param>
    /// <remarks>An attached node is detached from its current parent before insertion. A fragment itself is not inserted; its children are moved and the fragment becomes empty. Template elements receive children in their template-content fragment.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the child belongs to a different document.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or the insertion would violate node-kind, document-shape, or tree-cycle rules.</exception>
    public void Prepend(HtmlNode child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.InsertAt(child, 0);
    }

    /// <summary>Moves a node immediately before an existing child.</summary>
    /// <param name = "child">The node to insert, or a fragment whose children are inserted in order.</param>
    /// <param name = "referenceChild">The existing child that determines the insertion position.</param>
    /// <remarks>The reference must belong to this node's effective child container. An attached node is detached from its current parent; a fragment contributes its children and becomes empty.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the reference is not a child or the inserted node belongs to a different document.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or the insertion would violate node-kind, document-shape, or tree-cycle rules.</exception>
    public void InsertBefore(HtmlNode child, HtmlNode referenceChild)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (referenceChild == null)
        {
            throw new ArgumentNullException("referenceChild");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.ownerDocument.EnsureMutationAllowedStorage();
        HtmlTreeMutation.ValidateContainer(target);
        int index = target.IndexOfChild(referenceChild);
        if (index < 0)
        {
            throw new ArgumentException("The reference node is not a child of this node.", "referenceChild");
        }

        target.InsertAt(child, index);
    }

    /// <summary>Moves a node immediately after an existing child.</summary>
    /// <param name = "child">The node to insert, or a fragment whose children are inserted in order.</param>
    /// <param name = "referenceChild">The existing child that determines the insertion position.</param>
    /// <remarks>The reference must belong to this node's effective child container. An attached node is detached from its current parent; a fragment contributes its children and becomes empty.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the reference is not a child or the inserted node belongs to a different document.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or the insertion would violate node-kind, document-shape, or tree-cycle rules.</exception>
    public void InsertAfter(HtmlNode child, HtmlNode referenceChild)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (referenceChild == null)
        {
            throw new ArgumentNullException("referenceChild");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.ownerDocument.EnsureMutationAllowedStorage();
        HtmlTreeMutation.ValidateContainer(target);
        int index = target.IndexOfChild(referenceChild);
        if (index < 0)
        {
            throw new ArgumentException("The reference node is not a child of this node.", "referenceChild");
        }

        target.InsertAt(child, index + 1);
    }

    /// <summary>Replaces this node in its parent with another node or fragment contents.</summary>
    /// <param name = "replacement">The node to move into this position, or a fragment whose children replace this node.</param>
    /// <remarks>If this node is detached, or is the same object as <c>replacement</c>, the method makes no change. An attached replacement is moved from its former parent. A fragment becomes empty after its children are moved.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "ArgumentException">Thrown when the replacement belongs to a different document.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or replacement would violate node-kind, document-shape, or tree-cycle rules.</exception>
    public void ReplaceWith(HtmlNode replacement)
    {
        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        HtmlNode? targetParent = this.parent;
        if (targetParent == null || replacement == this)
        {
            return;
        }

        HtmlTreeMutation.ValidateInsertion(targetParent, replacement);
        if (replacement.kind == HtmlNodeKind.Fragment)
        {
            this.ReplaceWithFragment(targetParent, replacement);
            return;
        }

        bool alreadySibling = replacement.parent == targetParent;
        int resultCount = alreadySibling ? targetParent.childCount - 1 : targetParent.childCount;
        HtmlNode[] result = new HtmlNode[resultCount];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < targetParent.childCount)
        {
            HtmlNode current = targetParent.children[sourceIndex];
            if (current == this)
            {
                result[outputIndex] = replacement;
                outputIndex = outputIndex + 1;
            }
            else if (current != replacement)
            {
                result[outputIndex] = current;
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        HtmlTreeMutation.ValidateChildren(targetParent, result);
        HtmlNode? replacementParent = replacement.parent;
        if (replacementParent != null && replacementParent != targetParent)
        {
            replacementParent.RemoveChildDirect(replacement);
        }

        targetParent.CommitChildren(result);
        if (replacementParent != null && replacementParent != targetParent)
        {
            replacementParent.InvalidateChildList();
        }

        targetParent.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    /// <summary>Replaces one direct child with a node or fragment contents.</summary>
    /// <param name = "replacement">The replacement node or fragment.</param>
    /// <param name = "child">The direct child to replace.</param>
    /// <returns>The detached child that was replaced.</returns>
    public HtmlNode ReplaceChild(HtmlNode replacement, HtmlNode child)
    {
        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.ownerDocument.EnsureMutationAllowedStorage();
        HtmlTreeMutation.ValidateContainer(target);
        if (child.parent != target)
        {
            throw new ArgumentException(
                "The replaced node is not a child of this node.", "child");
        }

        child.ReplaceWith(replacement);
        return child;
    }

    /// <summary>Wraps this element with another element.</summary>
    /// <param name = "wrapper">The element that will become this node's parent.</param>
    /// <returns>The supplied wrapper.</returns>
    public HtmlNode Wrap(HtmlNode wrapper)
    {
        if (wrapper == null)
        {
            throw new ArgumentNullException("wrapper");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.kind != HtmlNodeKind.Element
            || wrapper.kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException(
                "Wrapping requires two element nodes.");
        }

        if (wrapper.ownerDocument != this.ownerDocument)
        {
            throw new ArgumentException(
                "A wrapper belongs to a different document.", "wrapper");
        }

        HtmlNode wrapperTarget = HtmlTreeMutation.GetMutationTarget(wrapper);
        HtmlTreeMutation.ValidateInsertion(wrapperTarget, this);
        HtmlNode? targetParent = this.parent;
        if (targetParent != null)
        {
            HtmlTreeMutation.ValidateInsertion(targetParent, wrapper);
            this.ReplaceWith(wrapper);
        }

        wrapper.Append(this);
        return wrapper;
    }

    /// <summary>Detaches this node and its subtree from the parent tree.</summary>
    /// <remarks>A detached node is left unchanged.</remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen.</exception>
    public void Remove()
    {
        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.parent != null)
        {
            HtmlNode oldParent = this.parent;
            oldParent.RemoveChildDirect(this);
            oldParent.InvalidateChildList();
            this.ownerDocument.RecordMutationStorage();
        }
    }

    /// <summary>Detaches this node and returns it.</summary>
    /// <returns>This node after detachment.</returns>
    public HtmlNode Detach()
    {
        this.Remove();
        return this;
    }

    /// <summary>Removes this element while moving its children into its former position.</summary>
    /// <remarks>A detached node is left unchanged. For a template element, the children of its template-content fragment are promoted.</remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or promoting the children would violate node-kind or document-shape rules.</exception>
    public void Unwrap()
    {
        this.ownerDocument.EnsureMutationAllowedStorage();
        HtmlNode? targetParent = this.parent;
        if (targetParent == null)
        {
            return;
        }

        HtmlNode content = HtmlTreeMutation.GetMutationTarget(this);
        HtmlNode[] promoted = content.GetChildrenStorage();
        HtmlNode[] result = new HtmlNode[targetParent.childCount - 1 + promoted.Length];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < targetParent.childCount)
        {
            HtmlNode current = targetParent.children[sourceIndex];
            if (current == this)
            {
                int promotedIndex = 0;
                while (promotedIndex < promoted.Length)
                {
                    result[outputIndex] = promoted[promotedIndex];
                    outputIndex = outputIndex + 1;
                    promotedIndex = promotedIndex + 1;
                }
            }
            else
            {
                result[outputIndex] = current;
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        HtmlTreeMutation.ValidateChildren(targetParent, result);
        content.CommitChildren(new HtmlNode[0]);
        targetParent.CommitChildren(result);
        content.InvalidateChildList();
        if (targetParent != content)
        {
            targetParent.InvalidateChildList();
        }

        this.ownerDocument.RecordMutationStorage();
    }

    /// <summary>Removes every child from this node.</summary>
    /// <remarks>For a template element, this empties its template-content fragment.</remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen or this node kind cannot contain children.</exception>
    public void Empty()
    {
        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        target.ownerDocument.EnsureMutationAllowedStorage();
        HtmlTreeMutation.ValidateContainer(target);
        if (target.childCount == 0)
        {
            return;
        }

        target.CommitChildren(new HtmlNode[0]);
        target.InvalidateChildList();
        target.ownerDocument.RecordMutationStorage();
    }

    /// <summary>Replaces this node's character data or descendant content with one text value.</summary>
    /// <param name = "value">The replacement character data. An empty value removes all children from a container.</param>
    /// <remarks>Text and comment nodes receive the value directly. Elements and fragments replace their children with one text node, while template elements replace template content. Document and document-type nodes are left unchanged.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen.</exception>
    public void SetTextContent(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.kind == HtmlNodeKind.Document || this.kind == HtmlNodeKind.DocumentType)
        {
            return;
        }

        if (this.kind == HtmlNodeKind.Text || this.kind == HtmlNodeKind.Comment)
        {
            if (this.data == value)
            {
                return;
            }

            this.data = value;
            this.RecordNodeMutation();
            return;
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        HtmlTreeMutation.ValidateContainer(target);
        if (value.Length == 0 && target.childCount == 0)
        {
            return;
        }

        HtmlNode[] result;
        if (value.Length == 0)
        {
            result = new HtmlNode[0];
        }
        else
        {
            result = new HtmlNode[]
            {
                CreateTextStorage(this.ownerDocument, value, HtmlSourceSpan.CreateUnknown())
            };
        }

        HtmlTreeMutation.ValidateChildren(target, result);
        target.CommitChildren(result);
        target.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    /// <summary>Parses HTML and atomically replaces this element or fragment's children.</summary>
    /// <param name = "html">The fragment markup to parse.</param>
    public void SetInnerHtml(string html)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.kind != HtmlNodeKind.Element
            && this.kind != HtmlNodeKind.Fragment)
        {
            throw new InvalidOperationException(
                "Only elements and fragments can receive inner HTML.");
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        HtmlNode context = this.GetFragmentContextStorage();
        HtmlNode fragment = this.ParseHtmlFragmentStorage(html, context);
        HtmlNode[] values = fragment.GetChildrenStorage();
        HtmlTreeMutation.ValidateChildren(target, values);
        if (target.childCount == 0 && values.Length == 0)
        {
            return;
        }

        fragment.CommitChildren(new HtmlNode[0]);
        target.CommitChildren(values);
        fragment.InvalidateChildList();
        target.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    /// <summary>Parses HTML and atomically replaces this attached element.</summary>
    /// <param name = "html">The fragment markup to parse.</param>
    public void SetOuterHtml(string html)
    {
        if (html == null)
        {
            throw new ArgumentNullException("html");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException(
                "Only an element can receive outer HTML.");
        }

        HtmlNode? targetParent = this.parent;
        if (targetParent == null)
        {
            return;
        }

        if (targetParent.kind == HtmlNodeKind.Document)
        {
            throw new InvalidOperationException(
                "The document element cannot be replaced through outer HTML.");
        }

        HtmlNode context = targetParent.GetFragmentContextStorage();
        HtmlNode fragment = this.ParseHtmlFragmentStorage(html, context);
        this.ReplaceWith(fragment);
    }

    /// <summary>Removes empty text nodes and merges adjacent text nodes throughout this subtree.</summary>
    /// <remarks>Container descendants are processed recursively, including template content. Text, comment, and document-type nodes are unchanged.</remarks>
    /// <exception cref = "InvalidOperationException">Thrown when the owning document is frozen.</exception>
    public void Normalize()
    {
        this.ownerDocument.EnsureMutationAllowedStorage();
        if (this.NormalizeDescendantText())
        {
            this.ownerDocument.RecordMutationStorage();
        }
    }

    internal HtmlNode GetFragmentContextStorage()
    {
        if (this.kind == HtmlNodeKind.Element)
        {
            return this;
        }

        if (this.kind == HtmlNodeKind.Fragment
            && this.templateHost != null)
        {
            return this.templateHost;
        }

        if (this.kind == HtmlNodeKind.Fragment)
        {
            return this.ownerDocument.CreateElement("body");
        }

        throw new InvalidOperationException(
            "This node cannot provide an HTML fragment context.");
    }

    internal HtmlNode ParseHtmlFragmentStorage(string html, HtmlNode context)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        HtmlParser parser = HtmlParser.CreateFragment(options, context,
            System.Threading.CancellationToken.None);
        parser.Feed(html.AsSpan(), true);
        HtmlParseResult result = parser.Complete();
        if (!result.IsComplete())
        {
            throw new InvalidOperationException(
                "The HTML fragment could not be parsed completely.");
        }

        HtmlNode? fragment = result.GetFragment();
        if (fragment == null)
        {
            throw new InvalidOperationException(
                "Fragment parsing did not produce a fragment.");
        }

        return fragment;
    }

    internal HtmlDocument GetOwnerDocumentStorage()
    {
        return this.ownerDocument;
    }

    internal HtmlNode? GetParentStorage()
    {
        return this.parent;
    }

    internal HtmlNode? GetFirstChildStorage()
    {
        return this.firstChild;
    }

    internal HtmlNode? GetLastChildStorage()
    {
        return this.lastChild;
    }

    internal HtmlNode? GetPreviousSiblingStorage()
    {
        return this.previousSibling;
    }

    internal HtmlNode? GetNextSiblingStorage()
    {
        return this.nextSibling;
    }

    internal HtmlNode[] GetChildrenStorage()
    {
        HtmlNode[] result = new HtmlNode[this.childCount];
        Array.Copy(this.children, result, this.childCount);
        return result;
    }

    internal HtmlNamespace GetNamespaceStorage()
    {
        return this.namespaceKind;
    }

    internal string GetNamespaceUriStorage()
    {
        return this.namespaceUri;
    }

    internal string GetNameStorage()
    {
        return this.name;
    }

    internal string GetLocalNameStorage()
    {
        return this.localName;
    }

    internal string GetPrefixStorage()
    {
        return this.prefix;
    }

    internal string GetDataStorage()
    {
        return this.data;
    }

    internal HtmlAttribute[] GetAttributesStorage()
    {
        HtmlAttribute[] result = new HtmlAttribute[this.attributeCount];
        Array.Copy(this.attributes, result, this.attributeCount);
        return result;
    }

    internal void ReplaceAttributesStorage(HtmlAttribute[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        this.EnsureElementMutation();
        HtmlAttribute[] replacement = new HtmlAttribute[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            HtmlAttribute attribute = values[index];
            if (attribute == null)
            {
                throw new ArgumentException("An element attribute cannot be null.", "values");
            }

            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                HtmlAttribute previous = replacement[duplicateIndex];
                if (previous.GetNamespaceUri() == attribute.GetNamespaceUri() && previous.GetLocalName() == attribute.GetLocalName())
                {
                    throw new ArgumentException("Element attributes must have unique expanded names.", "values");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            replacement[index] = attribute;
            index = index + 1;
        }

        this.attributes = replacement;
        this.attributeCount = replacement.Length;
        this.RecordNodeMutation();
    }

    internal void MergeAttributesStorage(HtmlAttribute[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (this.kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException("Only an element can receive parser attributes.");
        }

        int valueIndex = 0;
        while (valueIndex < values.Length)
        {
            HtmlAttribute value = values[valueIndex];
            if (value == null)
            {
                throw new ArgumentException("A parser attribute cannot be null.", "values");
            }

            bool found = false;
            int attributeIndex = 0;
            while (attributeIndex < this.attributeCount && !found)
            {
                HtmlAttribute current = this.attributes[attributeIndex];
                found = current.GetNamespaceUri() == value.GetNamespaceUri() && current.GetLocalName() == value.GetLocalName();
                attributeIndex = attributeIndex + 1;
            }

            if (!found)
            {
                this.AppendAttribute(value);
            }

            valueIndex = valueIndex + 1;
        }
    }

    internal bool HasDoctypeNameStorage()
    {
        return this.hasDoctypeName;
    }

    internal bool HasPublicIdentifierStorage()
    {
        return this.hasPublicIdentifier;
    }

    internal string GetPublicIdentifierStorage()
    {
        return this.publicIdentifier;
    }

    internal bool HasSystemIdentifierStorage()
    {
        return this.hasSystemIdentifier;
    }

    internal string GetSystemIdentifierStorage()
    {
        return this.systemIdentifier;
    }

    internal bool IsForceQuirksStorage()
    {
        return this.forceQuirks;
    }

    internal HtmlNode? GetTemplateContentStorage()
    {
        return this.templateContent;
    }

    internal HtmlNode? GetTemplateHostStorage()
    {
        return this.templateHost;
    }

    internal bool IsScriptInertStorage()
    {
        return this.scriptInert;
    }

    internal HtmlSourceSpan GetSourceSpanStorage()
    {
        return this.sourceSpan;
    }

    internal bool IsSourceBackedStorage()
    {
        return this.sourceBacked;
    }

    internal void SetSourceBackedStorage(bool value)
    {
        this.sourceBacked = value;
    }

    internal void InitializeChildrenStorage(HtmlNode[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (this.kind != HtmlNodeKind.Document && this.kind != HtmlNodeKind.Fragment && this.kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException("This node kind cannot contain child nodes.");
        }

        if (this.childCount != 0)
        {
            throw new InvalidOperationException("Child storage has already been initialized.");
        }

        if (this.templateContent != null && values.Length != 0)
        {
            throw new InvalidOperationException("Template children must be stored in the template content fragment.");
        }

        HtmlNode[] owned = new HtmlNode[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            HtmlNode child = values[index];
            if (child == null)
            {
                throw new ArgumentException("A child node cannot be null.", "values");
            }

            if (child == this || child.kind == HtmlNodeKind.Document)
            {
                throw new ArgumentException("A node cannot contain itself or a document node.", "values");
            }

            if (child.parent != null)
            {
                throw new ArgumentException("A child node is already parented.", "values");
            }

            if (child.ownerDocument != this.ownerDocument)
            {
                throw new ArgumentException("A child node belongs to a different document.", "values");
            }

            int duplicateIndex = 0;
            while (duplicateIndex < index)
            {
                if (owned[duplicateIndex] == child)
                {
                    throw new ArgumentException("A child node cannot occur more than once.", "values");
                }

                duplicateIndex = duplicateIndex + 1;
            }

            owned[index] = child;
            index = index + 1;
        }

        HtmlTreeMutation.ValidateChildren(this, owned);
        this.CommitChildren(owned);
        this.InvalidateTextCacheChain();
    }

    internal void InsertChildStorage(HtmlNode child, HtmlNode? beforeChild)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        HtmlTreeMutation.ValidateInsertion(this, child);
        if (this.templateContent != null)
        {
            throw new InvalidOperationException("Template children must be inserted into the template content fragment.");
        }

        if (child.parent != null)
        {
            throw new ArgumentException("A parser-inserted child must not already have a parent.", "child");
        }

        int insertionIndex = this.childCount;
        if (beforeChild != null)
        {
            insertionIndex = this.IndexOfChild(beforeChild);
            if (insertionIndex < 0)
            {
                throw new ArgumentException("The insertion reference is not a child of this node.", "beforeChild");
            }
        }

        HtmlNode[] result = new HtmlNode[this.childCount + 1];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (outputIndex < result.Length)
        {
            if (outputIndex == insertionIndex)
            {
                result[outputIndex] = child;
            }
            else
            {
                result[outputIndex] = this.children[sourceIndex];
                sourceIndex = sourceIndex + 1;
            }

            outputIndex = outputIndex + 1;
        }

        HtmlTreeMutation.ValidateChildren(this, result);
        this.CommitChildren(result);
        this.InvalidateTextCacheChain();
    }

    internal void AppendTextDataStorage(string value, HtmlSourceSpan valueSpan)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (valueSpan == null)
        {
            throw new ArgumentNullException("valueSpan");
        }

        if (this.kind != HtmlNodeKind.Text)
        {
            throw new InvalidOperationException("Only a text node can receive parser character data.");
        }

        if (value.Length == 0)
        {
            return;
        }

        this.data = this.data + value;
        this.sourceSpan = MergeSourceSpans(this.sourceSpan, valueSpan);
        this.InvalidateTextCacheChain();
    }

    internal void DetachStorage()
    {
        if (this.parent == null)
        {
            return;
        }

        HtmlNode oldParent = this.parent;
        oldParent.RemoveChildDirect(this);
        oldParent.InvalidateChildList();
    }

    internal void ReparentChildStorage(HtmlNode child, HtmlNode? beforeChild)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (child.parent != null)
        {
            HtmlNode oldParent = child.parent;
            oldParent.RemoveChildDirect(child);
            oldParent.InvalidateChildList();
        }

        this.InsertChildStorage(child, beforeChild);
    }

    internal void InsertAt(HtmlNode child, int index)
    {
        this.ownerDocument.EnsureMutationAllowedStorage();
        HtmlTreeMutation.ValidateInsertion(this, child);
        if (index < 0 || index > this.childCount)
        {
            throw new ArgumentOutOfRangeException("index", index, "A child insertion index must be within the child list.");
        }

        if (child.kind == HtmlNodeKind.Fragment)
        {
            this.InsertFragmentAt(child, index);
            return;
        }

        int oldIndex = child.parent == this ? this.IndexOfChild(child) : -1;
        int adjustedIndex = index;
        if (oldIndex >= 0 && oldIndex < adjustedIndex)
        {
            adjustedIndex = adjustedIndex - 1;
        }

        int resultCount = oldIndex >= 0 ? this.childCount : this.childCount + 1;
        HtmlNode[] result = new HtmlNode[resultCount];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (outputIndex < result.Length)
        {
            if (outputIndex == adjustedIndex)
            {
                result[outputIndex] = child;
                outputIndex = outputIndex + 1;
            }
            else
            {
                while (sourceIndex < this.childCount && this.children[sourceIndex] == child)
                {
                    sourceIndex = sourceIndex + 1;
                }

                result[outputIndex] = this.children[sourceIndex];
                sourceIndex = sourceIndex + 1;
                outputIndex = outputIndex + 1;
            }
        }

        HtmlTreeMutation.ValidateChildren(this, result);
        if (this.AreChildrenSame(result))
        {
            return;
        }

        HtmlNode? oldParent = child.parent;
        if (oldParent != null && oldParent != this)
        {
            oldParent.RemoveChildDirect(child);
        }

        this.CommitChildren(result);
        if (oldParent != null && oldParent != this)
        {
            oldParent.InvalidateChildList();
        }

        this.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    internal void InsertFragmentAt(HtmlNode fragment, int index)
    {
        if (fragment.childCount == 0)
        {
            return;
        }

        HtmlNode[] result = new HtmlNode[this.childCount + fragment.childCount];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < index)
        {
            result[outputIndex] = this.children[sourceIndex];
            sourceIndex = sourceIndex + 1;
            outputIndex = outputIndex + 1;
        }

        int fragmentIndex = 0;
        while (fragmentIndex < fragment.childCount)
        {
            result[outputIndex] = fragment.children[fragmentIndex];
            fragmentIndex = fragmentIndex + 1;
            outputIndex = outputIndex + 1;
        }

        while (sourceIndex < this.childCount)
        {
            result[outputIndex] = this.children[sourceIndex];
            sourceIndex = sourceIndex + 1;
            outputIndex = outputIndex + 1;
        }

        HtmlTreeMutation.ValidateChildren(this, result);
        fragment.CommitChildren(new HtmlNode[0]);
        this.CommitChildren(result);
        fragment.InvalidateChildList();
        this.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    internal void ReplaceWithFragment(HtmlNode targetParent, HtmlNode fragment)
    {
        HtmlNode[] result = new HtmlNode[targetParent.childCount - 1 + fragment.childCount];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < targetParent.childCount)
        {
            HtmlNode current = targetParent.children[sourceIndex];
            if (current == this)
            {
                int fragmentIndex = 0;
                while (fragmentIndex < fragment.childCount)
                {
                    result[outputIndex] = fragment.children[fragmentIndex];
                    fragmentIndex = fragmentIndex + 1;
                    outputIndex = outputIndex + 1;
                }
            }
            else
            {
                result[outputIndex] = current;
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        HtmlTreeMutation.ValidateChildren(targetParent, result);
        fragment.CommitChildren(new HtmlNode[0]);
        targetParent.CommitChildren(result);
        fragment.InvalidateChildList();
        targetParent.InvalidateChildList();
        this.ownerDocument.RecordMutationStorage();
    }

    internal int IndexOfChild(HtmlNode child)
    {
        int index = 0;
        while (index < this.childCount)
        {
            if (this.children[index] == child)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void RemoveChildDirect(HtmlNode child)
    {
        int removedIndex = this.IndexOfChild(child);
        if (removedIndex < 0)
        {
            throw new InvalidOperationException("The stored parent and child relationships are inconsistent.");
        }

        HtmlNode[] result = new HtmlNode[this.childCount - 1];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < this.childCount)
        {
            if (sourceIndex != removedIndex)
            {
                result[outputIndex] = this.children[sourceIndex];
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        this.CommitChildren(result);
    }

    internal void CommitChildren(HtmlNode[] values)
    {
        int index = 0;
        while (index < this.childCount)
        {
            HtmlNode child = this.children[index];
            if (child.parent == this)
            {
                child.parent = null;
                child.previousSibling = null;
                child.nextSibling = null;
            }

            index = index + 1;
        }

        this.children = values;
        this.childCount = values.Length;
        this.firstChild = values.Length == 0 ? null : values[0];
        this.lastChild = values.Length == 0 ? null : values[values.Length - 1];
        index = 0;
        while (index < values.Length)
        {
            HtmlNode child = values[index];
            child.parent = this;
            child.previousSibling = index == 0 ? null : values[index - 1];
            child.nextSibling = index + 1 == values.Length ? null : values[index + 1];
            index = index + 1;
        }
    }

    internal int FindAttributeByName(string attributeName)
    {
        if (this.kind != HtmlNodeKind.Element)
        {
            return -1;
        }

        int index = 0;
        while (index < this.attributeCount)
        {
            string candidate = this.attributes[index].GetName();
            bool matches = this.namespaceKind == HtmlNamespace.Html ? Ascii.EqualsIgnoreCase(candidate.AsSpan(), attributeName.AsSpan()) : candidate == attributeName;
            if (matches)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAttributeByNamespace(string attributeNamespace, string attributeLocalName)
    {
        if (this.kind != HtmlNodeKind.Element)
        {
            return -1;
        }

        int index = 0;
        while (index < this.attributeCount)
        {
            HtmlAttribute attribute = this.attributes[index];
            if (attribute.GetNamespaceUri() == attributeNamespace && attribute.GetLocalName() == attributeLocalName)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void EnsureElementMutation()
    {
        if (this.kind != HtmlNodeKind.Element)
        {
            throw new InvalidOperationException("Only element nodes can mutate attributes.");
        }

        this.ownerDocument.EnsureMutationAllowedStorage();
    }

    internal void AppendAttribute(HtmlAttribute attribute)
    {
        HtmlAttribute[] result = new HtmlAttribute[this.attributeCount + 1];
        Array.Copy(this.attributes, result, this.attributeCount);
        result[this.attributeCount] = attribute;
        this.attributes = result;
        this.attributeCount = result.Length;
    }

    internal void ReplaceAttribute(int index, HtmlAttribute replacement)
    {
        this.attributes[index] = replacement;
    }

    internal void ReplaceAttributeValue(int index, string value)
    {
        HtmlAttribute current = this.attributes[index];
        this.ReplaceAttribute(index, HtmlAttribute.CreateNamespaced(current.GetName(), current.GetLocalName(), current.GetPrefix(), current.GetNamespaceUri(), value, HtmlSourceSpan.CreateUnknown()));
    }

    internal void RemoveAttributeAt(int removedIndex)
    {
        HtmlAttribute[] result = new HtmlAttribute[this.attributeCount - 1];
        int sourceIndex = 0;
        int outputIndex = 0;
        while (sourceIndex < this.attributeCount)
        {
            if (sourceIndex != removedIndex)
            {
                result[outputIndex] = this.attributes[sourceIndex];
                outputIndex = outputIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        this.attributes = result;
        this.attributeCount = result.Length;
    }

    internal void RecordNodeMutation()
    {
        this.sourceSpan = HtmlSourceSpan.CreateUnknown();
        this.ownerDocument.RecordMutationStorage();
    }

    internal void InvalidateChildList()
    {
        this.sourceSpan = HtmlSourceSpan.CreateUnknown();
        if (this.templateHost != null)
        {
            this.templateHost.sourceSpan = HtmlSourceSpan.CreateUnknown();
        }
    }

    internal void InvalidateTextCacheChain()
    {
        HtmlNode? current = this;
        while (current != null)
        {
            current.ownTextCacheVersion = -1;
            current.ownTextCache = "";
            current.textContentCacheVersion = -1;
            current.textContentCache = "";
            current.normalizedTextContentCacheVersion = -1;
            current.normalizedTextContentCache = "";
            if (current.templateHost != null)
            {
                current = current.templateHost;
            }
            else
            {
                current = current.parent;
            }
        }
    }

    internal bool AreChildrenSame(HtmlNode[] values)
    {
        if (values.Length != this.childCount)
        {
            return false;
        }

        int index = 0;
        while (index < values.Length)
        {
            if (values[index] != this.children[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool NormalizeDescendantText()
    {
        HtmlNodeKind currentKind = this.kind;
        if (currentKind != HtmlNodeKind.Document && currentKind != HtmlNodeKind.Fragment && currentKind != HtmlNodeKind.Element)
        {
            return false;
        }

        HtmlNode target = HtmlTreeMutation.GetMutationTarget(this);
        bool changed = target.NormalizeDirectText();
        int index = 0;
        while (index < target.childCount)
        {
            HtmlNode child = target.children[index];
            if (child.kind == HtmlNodeKind.Element && child.NormalizeDescendantText())
            {
                changed = true;
            }

            index = index + 1;
        }

        return changed;
    }

    internal bool NormalizeDirectText()
    {
        HtmlNode[] retained = new HtmlNode[this.childCount];
        string[] textValues = new string[this.childCount];
        StringBuilder[] textBuilders = new StringBuilder[this.childCount];
        int sourceIndex = 0;
        int outputCount = 0;
        bool changed = false;
        while (sourceIndex < this.childCount)
        {
            HtmlNode child = this.children[sourceIndex];
            if (child.kind == HtmlNodeKind.Text)
            {
                if (child.data.Length == 0)
                {
                    changed = true;
                }
                else if (outputCount > 0 && retained[outputCount - 1].kind == HtmlNodeKind.Text)
                {
                    StringBuilder builder = textBuilders[outputCount - 1];
                    if (builder == null)
                    {
                        builder = new StringBuilder(textValues[outputCount - 1]);
                        textBuilders[outputCount - 1] = builder;
                    }

                    builder.Append(child.data);
                    changed = true;
                }
                else
                {
                    retained[outputCount] = child;
                    textValues[outputCount] = child.data;
                    outputCount = outputCount + 1;
                }
            }
            else
            {
                retained[outputCount] = child;
                outputCount = outputCount + 1;
            }

            sourceIndex = sourceIndex + 1;
        }

        if (!changed)
        {
            return false;
        }

        HtmlNode[] result = new HtmlNode[outputCount];
        int index = 0;
        while (index < outputCount)
        {
            HtmlNode child = retained[index];
            result[index] = child;
            if (textBuilders[index] != null)
            {
                textValues[index] = textBuilders[index].ToString();
            }

            if (child.kind == HtmlNodeKind.Text && child.data != textValues[index])
            {
                child.data = textValues[index];
                child.sourceSpan = HtmlSourceSpan.CreateUnknown();
            }

            index = index + 1;
        }

        this.CommitChildren(result);
        this.InvalidateChildList();
        return true;
    }

    internal static HtmlNode CreateDataStorage(HtmlNodeKind kind, HtmlDocument ownerDocument, string data, HtmlSourceSpan sourceSpan)
    {
        ValidateOwner(ownerDocument);
        if (data == null)
        {
            throw new ArgumentNullException("data");
        }

        ValidateSpan(sourceSpan);
        HtmlNode result = new HtmlNode(kind, ownerDocument, sourceSpan);
        result.data = data;
        return result;
    }

    internal static HtmlSourceSpan MergeSourceSpans(HtmlSourceSpan first, HtmlSourceSpan second)
    {
        if (!first.IsKnown() || !second.IsKnown())
        {
            return HtmlSourceSpan.CreateUnknown();
        }

        HtmlSourcePosition start = first.GetStart();
        HtmlSourcePosition end = second.GetEnd();
        if (end.GetNormalizedCharacterOffset() < start.GetNormalizedCharacterOffset() || end.GetCharacterOffset() < start.GetCharacterOffset() || end.GetLine() < start.GetLine() || end.GetLine() == start.GetLine() && end.GetColumn() < start.GetColumn() || start.HasByteOffset() != end.HasByteOffset() || start.HasByteOffset() && end.GetByteOffset() < start.GetByteOffset())
        {
            return HtmlSourceSpan.CreateUnknown();
        }

        return HtmlSourceSpan.Create(start, end);
    }

    internal static void ValidateOwner(HtmlDocument ownerDocument)
    {
        if (ownerDocument == null)
        {
            throw new ArgumentNullException("ownerDocument");
        }
    }

    internal static void ValidateName(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("A node name cannot be empty.", parameterName);
        }
    }

    internal static void ValidateOptionalValue(bool present, string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (!present && value.Length != 0)
        {
            throw new ArgumentException("A missing value must use empty owned storage.", parameterName);
        }
    }

    internal static void ValidateNamespace(HtmlNamespace namespaceKind, string namespaceUri)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if ((int)namespaceKind < (int)HtmlNamespace.None || (int)namespaceKind > (int)HtmlNamespace.Other)
        {
            throw new ArgumentOutOfRangeException("namespaceKind", (int)namespaceKind, "Unknown HTML namespace.");
        }

        string knownUri = HtmlNamespaceData.GetUri(namespaceKind);
        if (namespaceKind == HtmlNamespace.Other)
        {
            if (namespaceUri.Length == 0)
            {
                throw new ArgumentException("A custom namespace requires a namespace URI.", "namespaceUri");
            }

            return;
        }

        if (namespaceUri != knownUri)
        {
            throw new ArgumentException("The namespace URI does not match the namespace kind.", "namespaceUri");
        }
    }

    internal static void ValidateAttributes(HtmlAttribute[] attributes)
    {
        if (attributes == null)
        {
            throw new ArgumentNullException("attributes");
        }

        int index = 0;
        while (index < attributes.Length)
        {
            if (attributes[index] == null)
            {
                throw new ArgumentException("An element attribute cannot be null.", "attributes");
            }

            index = index + 1;
        }
    }

    internal static HtmlAttribute[] CopyAttributes(HtmlAttribute[] values)
    {
        HtmlAttribute[] result = new HtmlAttribute[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }

    internal static void ValidateSpan(HtmlSourceSpan sourceSpan)
    {
        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }
    }
}
