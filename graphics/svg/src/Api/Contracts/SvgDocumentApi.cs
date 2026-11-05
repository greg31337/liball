namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgDocumentApi"/>.</summary>
public interface ISvgDocumentApi
{
    /// <summary>Creates a canvas with positive viewport dimensions and a matching view box.</summary>
    /// <param name="width">The positive viewport width.</param>
    /// <param name="height">The positive viewport height.</param>
    /// <returns>A new canvas, including a sticky error for invalid dimensions.</returns>
    SvgCanvas CreateCanvasWithViewport(double width, double height);
    /// <summary>Creates a paint-definition builder for an editable document builder.</summary>
    /// <param name="builder">The destination document builder.</param>
    /// <returns>A paint builder positioned at the builder's current element.</returns>
    SvgPaintBuilder CreatePaintBuilderWithBuilder(SvgBuilder builder);
    /// <summary>Creates a filter builder for an editable document builder.</summary>
    /// <param name="builder">The destination document builder.</param>
    /// <returns>A filter builder positioned at the builder's current element.</returns>
    SvgFilterBuilder CreateFilterBuilderWithBuilder(SvgBuilder builder);
    /// <summary>Creates a sprite builder using caller-owned mutable options.</summary>
    /// <param name="options">The bounded sprite construction options.</param>
    /// <returns>A new sprite builder.</returns>
    SvgSpriteBuilder CreateSpriteBuilderWithOptions(SvgSpriteBuilderOptions options);
    /// <summary>Creates an empty editable document.</summary>
    /// <returns>A new clean document.</returns>
    SvgDocument CreateDocument();
    /// <summary>Begins one exclusive edit transaction for a document.</summary>
    /// <param name="document">The document whose mutations are recorded.</param>
    /// <returns>An active change set that can be committed or rolled back.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> is <c>null</c>.</exception>
    /// <exception cref = "System.InvalidOperationException">Thrown when the document already has an active change set.</exception>
    SvgChangeSet BeginChangeSet(SvgDocument document);
    /// <summary>Creates a deterministic structural patch from one document to another.</summary>
    /// <param name="before">The expected source document.</param>
    /// <param name="after">The desired target document.</param>
    /// <returns>A newly allocated preconditioned patch.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when an argument is <c>null</c>.</exception>
    SvgPatch Diff(SvgDocument before, SvgDocument after);
    /// <summary>Applies a structural patch atomically to a document.</summary>
    /// <param name="document">The document to mutate.</param>
    /// <param name="patch">The preconditioned patch.</param>
    /// <returns>The application result and inverse patch after success.</returns>
    SvgPatchResult ApplyPatch(SvgDocument document, SvgPatch? patch);
    /// <summary>Validates a structural patch against a document without mutating it.</summary>
    /// <param name="document">The document whose current state is checked.</param>
    /// <param name="patch">The preconditioned patch.</param>
    /// <returns>The validation result.</returns>
    SvgPatchResult ValidatePatch(SvgDocument document, SvgPatch? patch);
    /// <summary>Copies a node into a target document without attaching or changing either tree.</summary>
    /// <param name="document">The document that will own the imported copy.</param>
    /// <param name="node">The non-document node to copy.</param>
    /// <param name="deep">Whether descendants are copied recursively.</param>
    /// <returns>A detached generated node owned by the target document.</returns>
    /// <exception cref = "System.ArgumentNullException">Thrown when <c>document</c> or <c>node</c> is <c>null</c>.</exception>
    /// <exception cref = "System.ArgumentException">Thrown when a document node is supplied.</exception>
    SvgNode ImportNode(SvgDocument document, SvgNode node, bool deep);
    /// <summary>Detaches a node from its current parent and changes its owning document.</summary>
    /// <param name="document">The destination document.</param>
    /// <param name="node">The non-document node to adopt.</param>
    /// <returns>The mutation result containing the detached adopted node.</returns>
    SvgMutationResult AdoptNode(SvgDocument document, SvgNode node);
    /// <summary>Creates a detached namespace-aware element.</summary>
    /// <param name="qualifiedName">The qualified element name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local element name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <returns>A new detached element.</returns>
    SvgNode CreateElement(string qualifiedName, string prefix, string localName, string namespaceUri);
    /// <summary>Creates a detached text node.</summary>
    /// <param name="value">The text value.</param>
    /// <returns>A new detached text node.</returns>
    SvgNode CreateText(string value);
    /// <summary>Creates a detached CDATA node.</summary>
    /// <param name="value">The CDATA value.</param>
    /// <returns>A new detached CDATA node.</returns>
    SvgNode CreateCData(string value);
    /// <summary>Creates a detached comment node.</summary>
    /// <param name="value">The comment value.</param>
    /// <returns>A new detached comment node.</returns>
    SvgNode CreateComment(string value);
    /// <summary>Creates a detached processing instruction node.</summary>
    /// <param name="target">The processing instruction target.</param>
    /// <param name="value">The instruction value.</param>
    /// <returns>A new detached processing instruction.</returns>
    SvgNode CreateProcessingInstruction(string target, string value);
    /// <summary>Creates a detached document type node.</summary>
    /// <param name="name">The document type name.</param>
    /// <param name="value">The retained declaration value.</param>
    /// <returns>A new detached document type node.</returns>
    SvgNode CreateDoctype(string name, string value);
    /// <summary>Creates a detached namespace-aware attribute.</summary>
    /// <param name="qualifiedName">The qualified attribute name.</param>
    /// <param name="prefix">The namespace prefix, or an empty string.</param>
    /// <param name="localName">The local attribute name.</param>
    /// <param name="namespaceUri">The namespace URI, or an empty string.</param>
    /// <param name="value">The attribute value.</param>
    /// <returns>A new detached attribute.</returns>
    SvgAttribute CreateAttribute(string qualifiedName, string prefix, string localName, string namespaceUri, string value);
}
