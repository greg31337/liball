namespace odf;

/// <summary>Creates, queries, updates, resolves, and removes annotations.</summary>
public class OdfAnnotationEditor : IOdfAnnotationEditor
{
    /// <summary>Initializes an annotation editor.</summary>
    public OdfAnnotationEditor()
    {
    }

    /// <summary>Gets annotations in document order.</summary>
    public OdfAnnotation[] GetAnnotations(OdfDocument document)
    {
        return AnnotationEditingModel.GetAnnotations(document);
    }

    /// <summary>Finds one uniquely named annotation.</summary>
    public OdfAnnotation? FindAnnotation(
        OdfDocument document,
        string name)
    {
        return AnnotationEditingModel.FindAnnotation(document, name);
    }

    /// <summary>Creates a point or selected-range annotation.</summary>
    public OdfAnnotation CreateAnnotation(
        OdfDocument document,
        OdfTextCursor cursor,
        OdfAnnotation annotation)
    {
        return AnnotationEditingModel.CreateAnnotation(
            document, cursor, annotation);
    }

    /// <summary>Creates a reply adjacent to its parent annotation.</summary>
    public OdfAnnotation CreateAnnotationReply(
        OdfDocument document,
        OdfAnnotation parent,
        OdfAnnotation reply)
    {
        return AnnotationEditingModel.CreateReply(
            document, parent, reply);
    }

    /// <summary>Replaces editable metadata while retaining anchor and thread.</summary>
    public OdfAnnotation UpdateAnnotation(
        OdfDocument document,
        OdfAnnotation annotation,
        OdfAnnotation replacement)
    {
        return AnnotationEditingModel.UpdateAnnotation(
            document, annotation, replacement);
    }

    /// <summary>Marks an annotation resolved.</summary>
    public OdfAnnotation ResolveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        return AnnotationEditingModel.ResolveAnnotation(
            document, annotation);
    }

    /// <summary>Marks an annotation open.</summary>
    public OdfAnnotation ReopenAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        return AnnotationEditingModel.ReopenAnnotation(
            document, annotation);
    }

    /// <summary>Removes one annotation and promotes surviving replies.</summary>
    public bool RemoveAnnotation(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        return AnnotationEditingModel.RemoveAnnotation(
            document, annotation);
    }

    /// <summary>Removes an annotation and every transitive reply.</summary>
    public int RemoveAnnotationThread(
        OdfDocument document,
        OdfAnnotation annotation)
    {
        return AnnotationEditingModel.RemoveThread(
            document, annotation);
    }
}
