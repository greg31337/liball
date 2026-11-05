namespace docx;

/// <summary>
/// Appends, splits, and imports document content and dependent resources.
/// </summary>
public interface IDocxAssemblyManagement
{
    /// <summary>Reports non-fatal conflicts between two documents.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The conflicts in discovery order.</returns>
    List<DocxAssemblyConflict> GetAssemblyConflicts(DocxDocument target, DocxDocument source);

    /// <summary>Appends source body content and imports its dependent resources.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    DocxAssemblyResult AppendDocument(DocxDocument target, DocxDocument source);

    /// <summary>Splits a document before one body block and returns the trailing document.</summary>
    /// <param name="document">The document whose leading blocks remain in place.</param>
    /// <param name="blockIndex">The first block moved to the returned document.</param>
    /// <returns>A document containing the trailing blocks and preserved package resources.</returns>
    DocxDocument SplitDocumentAtBlock(DocxDocument document, int blockIndex);

    /// <summary>Appends one source section and imports its dependent resources.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <param name="sectionIndex">The zero-based source section index.</param>
    /// <returns>The assembly result.</returns>
    DocxAssemblyResult ImportDocumentSection(DocxDocument target, DocxDocument source, int sectionIndex);

    /// <summary>Imports all non-conflicting source styles.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    DocxAssemblyResult ImportDocumentStyles(DocxDocument target, DocxDocument source);

    /// <summary>Imports and remaps source numbering definitions and instances.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    DocxAssemblyResult ImportDocumentNumbering(DocxDocument target, DocxDocument source);

    /// <summary>Imports source media and creates target main-document relationships.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    DocxAssemblyResult ImportDocumentMedia(DocxDocument target, DocxDocument source);
}
