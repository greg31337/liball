namespace odf;

/// <summary>
/// Creates new package and flat OpenDocument document skeletons.
/// </summary>
public interface IOdfDocumentFactory
{
    /// <summary>
    /// Creates a minimal package document for a document or template kind.
    /// </summary>
    /// <param name="kind">The document or template kind to create.</param>
    /// <returns>The new editable package document.</returns>
    /// <exception cref="OdfDocumentException">
    /// Thrown when the kind is not a recognized document kind.
    /// </exception>
    public OdfDocument CreatePackageDocument(OdfDocumentKind kind);
    /// <summary>
    /// Creates a minimal flat document for a document or template kind.
    /// </summary>
    /// <param name="kind">The document or template kind to create.</param>
    /// <returns>The new editable flat document.</returns>
    /// <exception cref="OdfDocumentException">
    /// Thrown when the kind is not a recognized document kind.
    /// </exception>
    public OdfDocument CreateFlatDocument(OdfDocumentKind kind);
}
