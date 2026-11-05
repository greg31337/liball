namespace odf;

/// <summary>Creates, queries, updates, resolves, and removes annotations.</summary>
public interface IOdfAnnotationEditor
{
    /// <summary>Gets annotations in document order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>An independent array of live annotation models.</returns>
    public OdfAnnotation[] GetAnnotations(OdfDocument document);
    /// <summary>Finds one uniquely named annotation.</summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The exact annotation name.</param>
    /// <returns>The unique annotation, or <c>null</c>.</returns>
    public OdfAnnotation? FindAnnotation(
        OdfDocument document,
        string name);
    /// <summary>Creates a point or selected-range annotation.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="cursor">The point or selection to annotate.</param>
    /// <param name="annotation">The annotation metadata and content.</param>
    /// <returns>The created annotation.</returns>
    public OdfAnnotation CreateAnnotation(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfAnnotation annotation);
    /// <summary>Creates a reply adjacent to its parent annotation.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="parent">The parent annotation.</param>
    /// <param name="reply">The reply metadata and content.</param>
    /// <returns>The created reply.</returns>
    public OdfAnnotation CreateAnnotationReply(
        OdfDocument document,
        OdfAnnotation parent,
        OdfAnnotation reply);
    /// <summary>Replaces editable metadata while retaining anchor and thread.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="annotation">The annotation to update.</param>
    /// <param name="replacement">The replacement metadata and content.</param>
    /// <returns>The updated annotation.</returns>
    public OdfAnnotation UpdateAnnotation(
        OdfDocument document,
        OdfAnnotation annotation,
        OdfAnnotation replacement);
    /// <summary>Marks an annotation resolved.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="annotation">The annotation to resolve.</param>
    /// <returns>The resolved annotation.</returns>
    public OdfAnnotation ResolveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation);
    /// <summary>Marks an annotation open.</summary>
    /// <param name="document">The containing document.</param>
    /// <param name="annotation">The annotation to reopen.</param>
    /// <returns>The reopened annotation.</returns>
    public OdfAnnotation ReopenAnnotation(
        OdfDocument document,
        OdfAnnotation annotation);
    /// <summary>Removes one annotation and promotes surviving replies.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="annotation">The annotation to remove.</param>
    /// <returns>Whether the annotation was present.</returns>
    public bool RemoveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation);
    /// <summary>Removes an annotation and every transitive reply.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="annotation">The thread root or subtree root.</param>
    /// <returns>The number of removed annotations.</returns>
    public int RemoveAnnotationThread(
        OdfDocument document,
        OdfAnnotation annotation);
}
