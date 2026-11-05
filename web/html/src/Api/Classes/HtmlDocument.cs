using System;
using System.Text;

namespace html;
/// <summary>Owns a parsed HTML tree, its source metadata, diagnostics, and document-wide search index.</summary>
/// <remarks>A mutable document is a single-writer object and must not be read while it is being changed. Complete all mutations and call <see cref = "Freeze"/> before sharing the document with concurrent readers.</remarks>
public class HtmlDocument : IHtmlDocument
{
    internal HtmlNode root;
    internal string url;
    internal string baseUrl;
    internal string contentLanguage;
    internal Encoding? detectedEncoding;
    internal string detectedEncodingLabel;
    internal HtmlEncodingDetectionResult? encodingDetection;
    internal HtmlDocumentMode mode;
    internal HtmlDiagnostic[] diagnostics;
    internal bool frozen;
    internal long mutationVersion;
    internal HtmlDocumentIndex? searchIndex;
    internal int searchIndexBuildCount;
    internal object searchIndexLock;
    internal HtmlDocument(string url, string baseUrl, HtmlDocumentMode mode, HtmlDiagnostic[] diagnostics, HtmlSourceSpan sourceSpan)
    {
        if (url == null)
        {
            throw new ArgumentNullException("url");
        }

        if (baseUrl == null)
        {
            throw new ArgumentNullException("baseUrl");
        }

        ValidateMode(mode);
        ValidateDiagnostics(diagnostics);
        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        this.url = url;
        this.baseUrl = baseUrl;
        this.contentLanguage = "";
        this.detectedEncoding = null;
        this.detectedEncodingLabel = "";
        this.encodingDetection = null;
        this.mode = mode;
        this.diagnostics = CopyDiagnostics(diagnostics);
        this.frozen = false;
        this.mutationVersion = 0;
        this.searchIndex = null;
        this.searchIndexBuildCount = 0;
        this.searchIndexLock = new object ();
        this.root = HtmlNode.CreateDocumentStorage(this, sourceSpan);
    }

    /// <summary>Gets the document node that owns every top-level node in this tree.</summary>
    /// <returns>The document node; this is distinct from the document element returned by <see cref = "GetDocumentElement"/>.</returns>
    public HtmlNode GetRoot()
    {
        return this.root;
    }

    /// <summary>Gets the document element.</summary>
    /// <returns>The document element. Returns null when the document has no element child.</returns>
    public HtmlNode? GetDocumentElement()
    {
        HtmlNode[] children = this.root.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].GetKindStorage() == HtmlNodeKind.Element)
            {
                return children[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Gets the first HTML <c>head</c> child of the document element.</summary>
    /// <returns>The HTML head element. Returns null when the document has no head element.</returns>
    public HtmlNode? GetHead()
    {
        HtmlNode? element = this.GetDocumentElement();
        if (element == null)
        {
            return null;
        }

        return FindHtmlChild(element, "head");
    }

    /// <summary>Gets the first HTML <c>body</c> child of the document element.</summary>
    /// <returns>The HTML body element. Returns null when the document has no body element.</returns>
    public HtmlNode? GetBody()
    {
        HtmlNode? element = this.GetDocumentElement();
        if (element == null)
        {
            return null;
        }

        return FindHtmlChild(element, "body");
    }

    /// <summary>Concatenates the direct text-node children of the first HTML <c>title</c> element in the document head.</summary>
    /// <returns>The title text without whitespace normalization, or an empty string when no title is present.</returns>
    public string GetTitle()
    {
        HtmlNode? head = this.GetHead();
        if (head == null)
        {
            return "";
        }

        HtmlNode? title = FindHtmlChild(head, "title");
        if (title == null)
        {
            return "";
        }

        HtmlNode[] children = title.GetChildrenStorage();
        string result = "";
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].GetKindStorage() == HtmlNodeKind.Text)
            {
                result = result + children[index].GetDataStorage();
            }

            index = index + 1;
        }

        return result;
    }

    /// <summary>Gets the document URL recorded when the input was parsed.</summary>
    /// <returns>The caller-supplied document URL, or an empty string when none was supplied.</returns>
    public string GetUrl()
    {
        return this.url;
    }

    /// <summary>Gets the response URL recorded when the input was parsed.</summary>
    /// <returns>The caller-supplied response URL, or an empty string when none was supplied.</returns>
    public string GetResponseUrl()
    {
        return this.url;
    }

    /// <summary>Gets the fallback base URL recorded when the input was parsed.</summary>
    /// <returns>The caller-supplied base URL, or the document URL when no separate base was supplied.</returns>
    public string GetBaseUrl()
    {
        return this.baseUrl;
    }

    /// <summary>Gets the content language supplied with the parsed response.</summary>
    /// <returns>The caller-supplied content-language value, or an empty string when none was supplied.</returns>
    public string GetContentLanguage()
    {
        return this.contentLanguage;
    }

    /// <summary>Gets the effective document language from markup or response metadata.</summary>
    /// <returns>The nearest document-element language value, the response content language, or an empty string when neither is available.</returns>
    public string GetLanguage()
    {
        HtmlNode? element = this.GetDocumentElement();
        return element == null ? this.contentLanguage : element.GetLanguage();
    }

    /// <summary>Gets the effective document text direction.</summary>
    /// <returns><c>ltr</c> or <c>rtl</c>, including resolution of <c>dir=auto</c>.</returns>
    public string GetDirection()
    {
        HtmlNode? element = this.GetDocumentElement();
        return element == null ? "ltr" : element.GetDirection();
    }

    /// <summary>Gets the .NET encoding instance used to decode byte input.</summary>
    /// <returns>The detected encoding. Returns null for character input and programmatically created documents.</returns>
    public Encoding? GetDetectedEncoding()
    {
        return this.detectedEncoding == null ? null : (Encoding)this.detectedEncoding.Clone();
    }

    /// <summary>Gets the canonical decoder label recorded for byte input.</summary>
    /// <returns>The label, or an empty string for character input and programmatically created documents.</returns>
    public string GetDetectedEncodingLabel()
    {
        return this.detectedEncodingLabel;
    }

    /// <summary>Gets how byte input selected its decoder.</summary>
    /// <returns>The encoding-detection result. Returns null for character input and programmatically created documents.</returns>
    public HtmlEncodingDetectionResult? GetEncodingDetection()
    {
        return this.encodingDetection;
    }

    /// <summary>Gets the document-wide mutation version.</summary>
    /// <returns>A monotonically increasing version that changes after each effective tree or attribute mutation.</returns>
    public long GetMutationVersion()
    {
        return this.mutationVersion;
    }

    /// <summary>Gets the document type child.</summary>
    /// <returns>The document type, or null when this document has none.</returns>
    public HtmlNode? GetDocumentType()
    {
        HtmlNode[] children = this.root.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].GetKindStorage() == HtmlNodeKind.DocumentType)
            {
                return children[index];
            }

            index = index + 1;
        }

        return null;
    }

    /// <summary>Finds the first HTML <c>base</c> element with an <c>href</c> attribute in document order.</summary>
    /// <returns>The base element. Returns null when the document contains no qualifying element.</returns>
    public HtmlNode? GetBaseElement()
    {
        return HtmlDomUrls.GetBaseElement(this);
    }

    /// <summary>Resolves the first qualifying HTML <c>base</c> element against the document's fallback base URL.</summary>
    /// <returns>A URL result containing the resolved base, or the normalized fallback when no usable base element exists.</returns>
    public HtmlUrlResult GetEffectiveBaseUrl()
    {
        return HtmlDomUrls.GetEffectiveBaseUrl(this);
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in document order.</summary>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindLinks()
    {
        return HtmlDomUrls.FindLinks(this.root);
    }

    /// <summary>Finds HTML, SVG, and MathML link elements with URL-bearing attributes in document order.</summary>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindLinksWithLimits(int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindLinks(this.root, maximumResults, maximumVisits);
    }

    /// <summary>Finds media elements that contain a single-URL or <c>srcset</c> attribute in document order.</summary>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindMedia()
    {
        return HtmlDomUrls.FindMedia(this.root);
    }

    /// <summary>Finds media elements that contain a single-URL or <c>srcset</c> attribute in document order.</summary>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindMediaWithLimits(int maximumResults, int maximumVisits)
    {
        return HtmlDomUrls.FindMedia(this.root, maximumResults, maximumVisits);
    }

    /// <summary>Gets the quirks mode selected from the parsed document type.</summary>
    /// <returns>No quirks, limited quirks, or quirks mode.</returns>
    public HtmlDocumentMode GetMode()
    {
        return this.mode;
    }

    /// <summary>Gets the bounded parser diagnostics associated with this document.</summary>
    /// <returns>A snapshot of the diagnostics; the array is empty when there are none.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlDiagnostic[] GetDiagnostics()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    /// <summary>Finds the element whose ID exactly matches the supplied value.</summary>
    /// <param name = "id">The exact <c>id</c> attribute value to match.</param>
    /// <returns>The matching element. Returns null when no element has the requested ID.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public HtmlNode? FindById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        return this.GetSearchIndexStorage().FindById(id);
    }

    /// <summary>Finds descendant elements with the supplied tag name in document order.</summary>
    /// <param name = "tagName">The element tag name to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public HtmlNode[] FindByTag(string tagName)
    {
        if (tagName == null)
        {
            throw new ArgumentNullException("tagName");
        }

        return this.GetSearchIndexStorage().FindByTag(tagName);
    }

    /// <summary>Finds descendant elements containing the supplied class token in document order.</summary>
    /// <param name = "className">The single class token to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByClass(string className)
    {
        HtmlTreeMutation.ValidateClassToken(className);
        return this.GetSearchIndexStorage().FindByClass(className);
    }

    /// <summary>Evaluates the selector against the selected subtree.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate against this document.</param>
    /// <returns>The first matching node. Returns null when the selector matches no node.</returns>
    public HtmlNode? QuerySelector(string selector)
    {
        return HtmlSelectors.CompileOrThrowStorage(selector).FindFirst(this.root);
    }

    /// <summary>Returns every node in the subtree that matches the selector.</summary>
    /// <param name = "selector">The CSS selector text to compile and evaluate against this document.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] QuerySelectorAll(string selector)
    {
        return HtmlSelectors.CompileOrThrowStorage(selector).FindAll(this.root);
    }

    /// <summary>Finds elements whose <c>name</c> attribute has the supplied value.</summary>
    /// <param name = "name">The exact <c>name</c> attribute value to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByName(string name)
    {
        return this.root.FindByName(name);
    }

    /// <summary>Finds elements whose <c>name</c> attribute has the supplied value.</summary>
    /// <param name = "name">The exact <c>name</c> attribute value to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByNameWithLimits(string name, int maximumResults, int maximumVisits)
    {
        return this.root.FindByNameWithLimits(name, maximumResults, maximumVisits);
    }

    /// <summary>Finds elements that contain the named attribute.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttribute(string attributeName)
    {
        return this.root.FindByAttribute(attributeName);
    }

    /// <summary>Finds elements that contain the named attribute.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeWithLimits(string attributeName, int maximumResults, int maximumVisits)
    {
        return this.root.FindByAttributeWithLimits(attributeName, maximumResults, maximumVisits);
    }

    /// <summary>Finds elements whose named attribute has the supplied value.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "value">The exact attribute value to match.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeValue(string attributeName, string value)
    {
        return this.root.FindByAttributeValue(attributeName, value);
    }

    /// <summary>Finds elements whose named attribute has the supplied value.</summary>
    /// <param name = "attributeName">The attribute name to find.</param>
    /// <param name = "value">The exact attribute value to match.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindByAttributeValueWithLimits(string attributeName, string value, int maximumResults, int maximumVisits)
    {
        return this.root.FindByAttributeValueWithLimits(attributeName, value, maximumResults, maximumVisits);
    }

    /// <summary>Finds the first node accepted by the predicate in document order.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <returns>The first accepted node. Returns null when the predicate accepts no node.</returns>
    public HtmlNode? FindFirst(Func<HtmlNode, bool> predicate)
    {
        return this.root.FindFirst(predicate);
    }

    /// <summary>Finds the first node accepted by the predicate in document order.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>The first accepted node. Returns null when the predicate accepts no node.</returns>
    public HtmlNode? FindFirstWithLimit(Func<HtmlNode, bool> predicate, int maximumVisits)
    {
        return this.root.FindFirstWithLimit(predicate, maximumVisits);
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAll(Func<HtmlNode, bool> predicate)
    {
        return this.root.FindAll(predicate);
    }

    /// <summary>Finds matching nodes in document order within the configured limits.</summary>
    /// <param name = "predicate">The callback that accepts matching nodes.</param>
    /// <param name = "maximumResults">The maximum number of matching entries to return.</param>
    /// <param name = "maximumVisits">The maximum number of nodes or traversal entries the operation may examine.</param>
    /// <returns>A newly allocated array containing the results in the documented order; the array is empty when no values are produced.</returns>
    /// <remarks>The caller owns the returned array and may change it without affecting library state.</remarks>
    public HtmlNode[] FindAllWithLimits(Func<HtmlNode, bool> predicate, int maximumResults, int maximumVisits)
    {
        return this.root.FindAllWithLimits(predicate, maximumResults, maximumVisits);
    }

    /// <summary>Creates a detached element owned by this document.</summary>
    /// <param name = "name">The unqualified element name to store.</param>
    /// <returns>A detached element owned by this document.</returns>
    public HtmlNode CreateElement(string name)
    {
        ValidateSimpleName(name, "name");
        string folded = FoldAscii(name);
        bool template = folded == "template";
        return HtmlNode.CreateElementStorage(this, HtmlNamespace.Html, HtmlNamespaceData.GetUri(HtmlNamespace.Html), folded, folded, "", new HtmlAttribute[0], template, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates a detached element owned by this document.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "qualifiedName">The namespace-qualified name.</param>
    /// <returns>A detached namespaced element owned by this document.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public HtmlNode CreateElementInNamespace(string namespaceUri, string qualifiedName)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        ValidateQualifiedName(qualifiedName);
        int colon = qualifiedName.IndexOf(':');
        string prefix = colon < 0 ? "" : qualifiedName.Substring(0, colon);
        string localName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1);
        ValidateNamespaceName(namespaceUri, qualifiedName, prefix);
        HtmlNamespace namespaceKind = HtmlNamespaceData.GetKind(namespaceUri);
        string name = qualifiedName;
        if (namespaceKind == HtmlNamespace.Html)
        {
            name = FoldAscii(qualifiedName);
            localName = FoldAscii(localName);
            prefix = FoldAscii(prefix);
        }

        bool template = namespaceKind == HtmlNamespace.Html && localName == "template";
        return HtmlNode.CreateElementStorage(this, namespaceKind, namespaceUri, name, localName, prefix, new HtmlAttribute[0], template, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates a detached text node owned by this document.</summary>
    /// <param name = "text">The literal character data stored in the new node.</param>
    /// <returns>A detached text node owned by this document.</returns>
    public HtmlNode CreateText(string text)
    {
        return HtmlNode.CreateTextStorage(this, text, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates a detached comment node owned by this document.</summary>
    /// <param name = "text">The literal character data stored in the new node.</param>
    /// <returns>A detached comment node owned by this document.</returns>
    public HtmlNode CreateComment(string text)
    {
        return HtmlNode.CreateCommentStorage(this, text, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates an empty document fragment owned by this document.</summary>
    /// <returns>An empty document fragment owned by this document.</returns>
    public HtmlNode CreateFragment()
    {
        return HtmlNode.CreateFragmentStorage(this, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates an unnamespaced attribute for later use on an element.</summary>
    /// <param name = "name">The attribute name.</param>
    /// <param name = "value">The attribute value.</param>
    /// <returns>A generated attribute with an unknown source location.</returns>
    public HtmlAttribute CreateAttribute(string name, string value)
    {
        HtmlTreeMutation.ValidateAttributeName(name);
        return HtmlAttribute.Create(name, value,
            HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates a namespace-aware attribute for later use on an element.</summary>
    /// <param name = "namespaceUri">The namespace URI, or an empty string for no namespace.</param>
    /// <param name = "qualifiedName">The qualified attribute name.</param>
    /// <param name = "value">The attribute value.</param>
    /// <returns>A generated attribute with the supplied expanded name.</returns>
    public HtmlAttribute CreateAttributeInNamespace(string namespaceUri,
        string qualifiedName, string value)
    {
        return HtmlTreeMutation.CreateNamespacedAttribute(namespaceUri,
            qualifiedName, value, HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Creates a detached document type owned by this document.</summary>
    /// <param name = "name">The document type name, or null when the name is absent.</param>
    /// <param name = "publicIdentifier">The public identifier, or null when it is absent.</param>
    /// <param name = "systemIdentifier">The system identifier, or null when it is absent.</param>
    /// <param name = "forceQuirks">Whether the document type carries the parser force-quirks flag.</param>
    /// <returns>A detached document type owned by this document.</returns>
    public HtmlNode CreateDocumentType(string? name, string? publicIdentifier, string? systemIdentifier, bool forceQuirks)
    {
        return HtmlNode.CreateDocumentTypeStorage(this, name != null,
            name ?? "", publicIdentifier != null, publicIdentifier ?? "",
            systemIdentifier != null, systemIdentifier ?? "", forceQuirks,
            HtmlSourceSpan.CreateUnknown());
    }

    /// <summary>Copies the supplied node into this document with new ownership.</summary>
    /// <param name = "node">The root of the node or subtree processed by the operation.</param>
    /// <param name = "deep">Whether descendant nodes are copied recursively.</param>
    /// <returns>The resulting HTML node.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref = "InvalidOperationException">Thrown when the object state does not permit this operation.</exception>
    public HtmlNode Import(HtmlNode node, bool deep)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (node.GetKindStorage() == HtmlNodeKind.Document)
        {
            throw new InvalidOperationException("A document node cannot be imported into another document.");
        }

        return HtmlTreeClone.Clone(node, this, deep);
    }

    /// <summary>Imports only the supplied node into this document.</summary>
    /// <param name = "node">The non-document node to copy.</param>
    /// <returns>A detached shallow copy owned by this document.</returns>
    public HtmlNode ImportShallow(HtmlNode node)
    {
        return this.Import(node, false);
    }

    /// <summary>Imports the supplied node and its descendants into this document.</summary>
    /// <param name = "node">The non-document subtree to copy.</param>
    /// <returns>A detached deep copy owned by this document.</returns>
    public HtmlNode ImportDeep(HtmlNode node)
    {
        return this.Import(node, true);
    }

    /// <summary>Makes the document and its current tree immutable for subsequent callers.</summary>
    /// <remarks>Call this method after the single writer has completed all mutations and before publishing the document to concurrent readers. Calling it more than once has no additional effect.</remarks>
    public void Freeze()
    {
        this.frozen = true;
    }

    /// <summary>Reports whether this document rejects further tree and attribute mutation.</summary>
    /// <returns><c>true</c> when tree mutation is disabled; otherwise, <c>false</c>.</returns>
    public bool IsFrozen()
    {
        return this.frozen;
    }

    internal HtmlNode GetRootStorage()
    {
        return this.root;
    }

    internal string GetUrlStorage()
    {
        return this.url;
    }

    internal string GetBaseUrlStorage()
    {
        return this.baseUrl;
    }

    internal void SetContentLanguageStorage(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.contentLanguage = value;
    }

    internal void SetEncodingMetadataStorage(Encoding? encoding, string label, HtmlEncodingDetectionResult? detection)
    {
        if (label == null)
        {
            throw new ArgumentNullException("label");
        }

        this.detectedEncoding = encoding == null ? null : (Encoding)encoding.Clone();
        this.detectedEncodingLabel = label;
        this.encodingDetection = detection;
    }

    internal HtmlDocumentMode GetModeStorage()
    {
        return this.mode;
    }

    internal void SetModeStorage(HtmlDocumentMode value)
    {
        ValidateMode(value);
        this.mode = value;
    }

    internal HtmlDiagnostic[] GetDiagnosticsStorage()
    {
        return CopyDiagnostics(this.diagnostics);
    }

    internal void SetDiagnosticsStorage(HtmlDiagnostic[] value)
    {
        ValidateDiagnostics(value);
        this.diagnostics = CopyDiagnostics(value);
    }

    internal bool IsFrozenStorage()
    {
        return this.frozen;
    }

    internal long GetMutationVersionStorage()
    {
        return this.mutationVersion;
    }

    internal bool HasCurrentSearchIndexStorage()
    {
        return this.searchIndex != null && this.searchIndex.GetMutationVersion() == this.mutationVersion;
    }

    internal int GetSearchIndexBuildCountStorage()
    {
        return this.searchIndexBuildCount;
    }

    internal HtmlDocumentIndex GetSearchIndexStorage()
    {
        lock (this.searchIndexLock)
        {
            if (!this.HasCurrentSearchIndexStorage())
            {
                this.searchIndex = HtmlDocumentIndex.Create(this.root, this.mutationVersion);
                this.searchIndexBuildCount = this.searchIndexBuildCount + 1;
            }

            HtmlDocumentIndex? result = this.searchIndex;
            if (result == null)
            {
                throw new InvalidOperationException("Building a document index must produce an index.");
            }

            return result;
        }
    }

    internal void EnsureMutationAllowedStorage()
    {
        if (this.frozen)
        {
            throw new InvalidOperationException("A frozen document cannot be mutated.");
        }

        if (this.mutationVersion == long.MaxValue)
        {
            throw new InvalidOperationException("The document mutation version is exhausted.");
        }
    }

    internal void RecordMutationStorage()
    {
        this.EnsureMutationAllowedStorage();
        this.mutationVersion = this.mutationVersion + 1;
    }

    internal HtmlNode CreateFragmentStorage(HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateFragmentStorage(this, sourceSpan);
    }

    internal HtmlNode CreateDocumentTypeStorage(bool hasName, string name, bool hasPublicIdentifier, string publicIdentifier, bool hasSystemIdentifier, string systemIdentifier, bool forceQuirks, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateDocumentTypeStorage(this, hasName, name, hasPublicIdentifier, publicIdentifier, hasSystemIdentifier, systemIdentifier, forceQuirks, sourceSpan);
    }

    internal HtmlNode CreateElementStorage(HtmlNamespace namespaceKind, string namespaceUri, string name, string localName, string prefix, HtmlAttribute[] attributes, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateElementStorage(this, namespaceKind, namespaceUri, name, localName, prefix, attributes, false, sourceSpan);
    }

    internal HtmlNode CreateTemplateStorage(HtmlAttribute[] attributes, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateElementStorage(this, HtmlNamespace.Html, HtmlNamespaceData.GetUri(HtmlNamespace.Html), "template", "template", "", attributes, true, sourceSpan);
    }

    internal HtmlNode CreateTextStorage(string data, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateTextStorage(this, data, sourceSpan);
    }

    internal HtmlNode CreateCommentStorage(string data, HtmlSourceSpan sourceSpan)
    {
        return HtmlNode.CreateCommentStorage(this, data, sourceSpan);
    }

    internal HtmlNode CreateElementWithSourceSpanStorage(string namespaceUri, string qualifiedName, HtmlSourceSpan sourceSpan)
    {
        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        ValidateQualifiedName(qualifiedName);
        if (sourceSpan == null)
        {
            throw new ArgumentNullException("sourceSpan");
        }

        int colon = qualifiedName.IndexOf(':');
        string prefix = colon < 0 ? "" : qualifiedName.Substring(0, colon);
        string localName = colon < 0 ? qualifiedName : qualifiedName.Substring(colon + 1);
        ValidateNamespaceName(namespaceUri, qualifiedName, prefix);
        HtmlNamespace namespaceKind = HtmlNamespaceData.GetKind(namespaceUri);
        string name = qualifiedName;
        if (namespaceKind == HtmlNamespace.Html)
        {
            name = FoldAscii(qualifiedName);
            localName = FoldAscii(localName);
            prefix = FoldAscii(prefix);
        }

        bool template = namespaceKind == HtmlNamespace.Html && localName == "template";
        return HtmlNode.CreateElementStorage(this, namespaceKind, namespaceUri,
            name, localName, prefix, new HtmlAttribute[0], template, sourceSpan);
    }

    internal static void ValidateMode(HtmlDocumentMode mode)
    {
        if ((int)mode < (int)HtmlDocumentMode.NoQuirks || (int)mode > (int)HtmlDocumentMode.Quirks)
        {
            throw new ArgumentOutOfRangeException("mode", (int)mode, "Unknown HTML document mode.");
        }
    }

    internal static HtmlNode? FindHtmlChild(HtmlNode parent, string localName)
    {
        HtmlNode[] children = parent.GetChildrenStorage();
        int index = 0;
        while (index < children.Length)
        {
            HtmlNode child = children[index];
            if (child.GetKindStorage() == HtmlNodeKind.Element && child.GetNamespaceStorage() == HtmlNamespace.Html && child.GetLocalNameStorage() == localName)
            {
                return child;
            }

            index = index + 1;
        }

        return null;
    }

    internal static void ValidateQualifiedName(string qualifiedName)
    {
        if (qualifiedName == null)
        {
            throw new ArgumentNullException("qualifiedName");
        }

        if (qualifiedName.Length == 0)
        {
            throw new ArgumentException("A qualified element name cannot be empty.", "qualifiedName");
        }

        int firstColon = qualifiedName.IndexOf(':');
        if (firstColon == 0 || firstColon == qualifiedName.Length - 1 || firstColon != qualifiedName.LastIndexOf(':'))
        {
            throw new ArgumentException("A qualified element name has an invalid prefix separator.", "qualifiedName");
        }

        if (firstColon < 0)
        {
            ValidateSimpleName(qualifiedName, "qualifiedName");
            return;
        }

        ValidateSimpleName(qualifiedName.Substring(0, firstColon), "qualifiedName");
        ValidateSimpleName(qualifiedName.Substring(firstColon + 1), "qualifiedName");
    }

    internal static void ValidateSimpleName(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (value.Length == 0)
        {
            throw new ArgumentException("An element name cannot be empty.", parameterName);
        }

        int index = 0;
        while (index < value.Length)
        {
            char current = value[index];
            bool allowed;
            if (index == 0)
            {
                allowed = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current == '_' || current == ':' || current >= 128;
            }
            else
            {
                allowed = current >= 'A' && current <= 'Z' || current >= 'a' && current <= 'z' || current >= '0' && current <= '9' || current == '_' || current == ':' || current == '-' || current == '.' || current >= 128;
            }

            if (!allowed || UnicodeTools.IsLowSurrogate(current))
            {
                throw new ArgumentException("An element name contains an invalid character.", parameterName);
            }

            if (UnicodeTools.IsHighSurrogate(current))
            {
                if (index + 1 >= value.Length || !UnicodeTools.IsLowSurrogate(value[index + 1]))
                {
                    throw new ArgumentException("An element name contains an invalid Unicode scalar.", parameterName);
                }

                index = index + 1;
            }

            index = index + 1;
        }
    }

    internal static void ValidateNamespaceName(string namespaceUri, string qualifiedName, string prefix)
    {
        if (prefix.Length > 0 && namespaceUri.Length == 0)
        {
            throw new ArgumentException("A prefixed element name requires a namespace URI.", "namespaceUri");
        }

        if (prefix == "xml" && namespaceUri != HtmlNamespaceData.GetUri(HtmlNamespace.Xml))
        {
            throw new ArgumentException("The xml prefix requires the XML namespace URI.", "namespaceUri");
        }

        bool xmlnsName = qualifiedName == "xmlns" || prefix == "xmlns";
        bool xmlnsNamespace = namespaceUri == HtmlNamespaceData.GetUri(HtmlNamespace.Xmlns);
        if (xmlnsName != xmlnsNamespace)
        {
            throw new ArgumentException("The xmlns name and namespace URI must be used together.", "namespaceUri");
        }
    }

    internal static string FoldAscii(string value)
    {
        char[] result = new char[value.Length];
        int index = 0;
        while (index < value.Length)
        {
            result[index] = Ascii.ToLower(value[index]);
            index = index + 1;
        }

        return new string (result);
    }

    internal static void ValidateDiagnostics(HtmlDiagnostic[] diagnostics)
    {
        if (diagnostics == null)
        {
            throw new ArgumentNullException("diagnostics");
        }

        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index] == null)
            {
                throw new ArgumentException("A document diagnostic cannot be null.", "diagnostics");
            }

            index = index + 1;
        }
    }

    internal static HtmlDiagnostic[] CopyDiagnostics(HtmlDiagnostic[] values)
    {
        HtmlDiagnostic[] result = new HtmlDiagnostic[values.Length];
        Array.Copy(values, result, values.Length);
        return result;
    }
}
