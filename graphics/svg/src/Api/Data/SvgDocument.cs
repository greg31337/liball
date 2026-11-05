namespace svg;

/// <summary>Represents an editable SVG document as later XML stages populate it.</summary>
public class SvgDocument
{
    /// <summary>The effective base URI.</summary>
    public string BaseUri;
    /// <summary>The effective media type supplied by the load source.</summary>
    public string ContentType;
    /// <summary>The processing mode selected for this document.</summary>
    public SvgProcessingMode ProcessingMode;
    /// <summary>Whether the document element is an SVG fragment rather than an <c>svg</c> root.</summary>
    public bool IsFragment;
    /// <summary>The document edit version.</summary>
    public long Version;
    /// <summary>Whether the document has changes after source construction.</summary>
    public bool IsDirty;
    /// <summary>The document diagnostics.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;
    /// <summary>The tagged document node containing top-level children.</summary>
    public SvgNode DocumentNode;
    /// <summary>The document element, or <c>null</c> before XML construction.</summary>
    public SvgNode? DocumentElement;
    /// <summary>The original source resource, or <c>null</c> when unavailable.</summary>
    public SvgSourceUri? Source;
    /// <summary>The original decoded source snapshot, or <c>null</c> when unavailable.</summary>
    public string? SourceText;
    /// <summary>The complete original source span, or <c>null</c> when unavailable.</summary>
    public SvgSourceSpan? SourceSpan;
    /// <summary>The deterministic lazily refreshed node indexes.</summary>
    public SvgDocumentIndex Index;
    /// <summary>The live deterministic source map for nodes and attributes.</summary>
    public SvgSourceMap SourceMap;
    internal SvgChangeSet? ActiveChangeSet;

    /// <summary>Creates an empty document shell for the XML construction stage.</summary>
    public SvgDocument()
    {
        BaseUri = "";
        ContentType = "";
        ProcessingMode = SvgProcessingMode.SecureStatic;
        IsFragment = false;
        Version = 0;
        IsDirty = false;
        Diagnostics = new System.Collections.Generic.List<SvgDiagnostic>();
        DocumentNode = new SvgNode(SvgNodeKind.Document, "", "", "", "", "");
        DocumentNode.OwnerDocument = this;
        DocumentElement = null;
        Source = null;
        SourceText = null;
        SourceSpan = null;
        Index = new SvgDocumentIndex(this);
        SourceMap = new SvgSourceMap(this);
        ActiveChangeSet = null;
    }

    /// <summary>Marks the document changed and returns its next monotonic version.</summary>
    /// <returns>The incremented document version.</returns>
    /// <exception cref="System.OverflowException">Thrown when the version cannot be incremented.</exception>
    internal long MarkDirty()
    {
        Version = Version + 1;
        IsDirty = true;
        DocumentNode.IsDirty = true;
        DocumentNode.Version = Version;
        DocumentNode.MarkSourceRewritten();
        Index.Invalidate();
        return Version;
    }

    /// <summary>Validates this document against strict SVG 2 structural defaults.</summary>
    /// <returns>The completed or failed validation result.</returns>
    internal SvgValidationResult Validate()
    {
        return new SvgValidator().ValidateWithDocument(this);
    }

    /// <summary>Validates this document against an explicit profile and bounds.</summary>
    /// <param name="options">The profile, catalogs, and traversal limits.</param>
    /// <returns>The completed or failed validation result.</returns>
    internal SvgValidationResult ValidateWithOptions(SvgValidationOptions options)
    {
        return new SvgValidator().ValidateWithDocumentAndOptions(this, options);
    }

    /// <summary>Creates an independent secure-static sanitized copy.</summary>
    /// <returns>The sanitized document and bounded removal summary.</returns>
    internal SvgSanitizationResult Sanitize()
    {
        return new SvgSanitizer().SanitizeWithDocument(this);
    }

    /// <summary>Creates an independent sanitized copy using explicit bounds and resource policy.</summary>
    /// <param name="options">The sanitization bounds and resource policy.</param>
    /// <returns>The sanitized document and bounded removal summary.</returns>
    internal SvgSanitizationResult SanitizeWithOptions(SvgSanitizationOptions options)
    {
        return new SvgSanitizer().SanitizeWithDocumentAndOptions(this, options);
    }

    /// <summary>Analyzes the features dynamically used by this document.</summary>
    /// <returns>The bounded policy-aware feature analysis.</returns>
    internal SvgFeatureAnalysisResult AnalyzeFeatures()
    {
        return new SvgDocumentFeatureAnalyzer().AnalyzeWithDocument(this);
    }

    /// <summary>Analyzes dynamically used features with explicit bounds, policy, and host capabilities.</summary>
    /// <param name="options">The feature-analysis bounds, policy, and host capabilities.</param>
    /// <returns>The bounded policy-aware feature analysis.</returns>
    internal SvgFeatureAnalysisResult AnalyzeFeaturesWithOptions(SvgFeatureAnalysisOptions options)
    {
        return new SvgDocumentFeatureAnalyzer().AnalyzeWithDocumentAndOptions(this, options);
    }

    /// <summary>Processes this document with an empty default job list.</summary>
    /// <returns>The completed processing result.</returns>
    internal SvgProcessResult Process()
    {
        return new SvgDocumentProcessor().ProcessWithDocument(this);
    }

    /// <summary>Processes this document using ordered jobs and bounded convergence.</summary>
    /// <param name="options">The jobs, bounds, callbacks, and dry-run behavior.</param>
    /// <returns>The completed or atomically rolled-back processing result.</returns>
    internal SvgProcessResult ProcessWithOptions(SvgProcessOptions options)
    {
        return new SvgDocumentProcessor().ProcessWithDocumentAndOptions(this, options);
    }

    /// <summary>Attaches one newly constructed child without recording an edit.</summary>
    /// <param name="parent">The document or element parent.</param>
    /// <param name="child">The unattached non-document child.</param>
    internal void AttachInitialChild(SvgNode parent, SvgNode child)
    {
        if (parent == null) throw new System.ArgumentNullException("parent");
        if (child == null) throw new System.ArgumentNullException("child");
        if (!object.ReferenceEquals(parent.OwnerDocument, this)) throw new System.ArgumentException("The parent must belong to this document.", "parent");
        if (parent.Kind != SvgNodeKind.Document && parent.Kind != SvgNodeKind.Element) throw new System.ArgumentException("Only document and element nodes can contain children.", "parent");
        if (child.Kind == SvgNodeKind.Document) throw new System.ArgumentException("A document node cannot be attached as a child.", "child");
        if (child.Parent != null || child.OwnerDocument != null) throw new System.ArgumentException("The child is already attached.", "child");
        parent.Children.Add(child);
        child.Parent = parent;
        child.OwnerDocument = this;
        child.Version = Version;
        if (parent.Kind == SvgNodeKind.Document && child.Kind == SvgNodeKind.Element && DocumentElement == null) DocumentElement = child;
    }

    /// <summary>Attaches one newly constructed attribute without recording an edit.</summary>
    /// <param name="element">The owning element.</param>
    /// <param name="attribute">The unattached attribute.</param>
    internal void AttachInitialAttribute(SvgNode element, SvgAttribute attribute)
    {
        if (element == null) throw new System.ArgumentNullException("element");
        if (attribute == null) throw new System.ArgumentNullException("attribute");
        if (!object.ReferenceEquals(element.OwnerDocument, this) || element.Kind != SvgNodeKind.Element) throw new System.ArgumentException("The owner must be an element in this document.", "element");
        if (attribute.OwnerElement != null || attribute.OwnerDocument != null) throw new System.ArgumentException("The attribute is already attached.", "attribute");
        element.Attributes.Add(attribute);
        attribute.OwnerElement = element;
        attribute.OwnerDocument = this;
        attribute.Version = Version;
    }
}
