namespace odf;

/// <summary>
/// Provides indexed cross-part queries over materialized OpenDocument content.
/// </summary>
public class OdfQueryService : IOdfQueryService
{
    /// <summary>
    /// Initializes a query service.
    /// </summary>
    public OdfQueryService()
    {
    }

    /// <summary>
    /// Finds literal values across text, attributes, and indexed typed models.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="value">The non-empty literal value.</param>
    /// <param name="matchCase">Whether matching is case-sensitive.</param>
    /// <returns>The matches in indexed document order.</returns>
    public OdfSearchMatch[] QueryLiteral(
        OdfDocument document,
        string value,
        bool matchCase)
    {
        return QueryServiceModel.QueryLiteral(
            document,
            value,
            matchCase);
    }

    /// <summary>
    /// Finds regular-expression matches across text, attributes, and indexed typed models.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="pattern">The non-empty regular-expression pattern.</param>
    /// <param name="matchCase">Whether matching is case-sensitive.</param>
    /// <returns>The matches in indexed document order.</returns>
    public OdfSearchMatch[] QueryRegularExpression(
        OdfDocument document,
        string pattern,
        bool matchCase)
    {
        return QueryServiceModel.QueryRegularExpression(
            document,
            pattern,
            matchCase);
    }

    /// <summary>
    /// Finds elements with an expanded XML name across all materialized parts.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="name">The expanded element name.</param>
    /// <returns>The matching element nodes.</returns>
    public OdfSearchMatch[] QueryNodes(
        OdfDocument document,
        OdfQualifiedName name)
    {
        return QueryServiceModel.QueryNodes(document, name);
    }

    /// <summary>
    /// Finds values of one expanded XML attribute name.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="fieldName">The expanded attribute name.</param>
    /// <param name="value">The value fragment, or an empty string for every value.</param>
    /// <param name="matchCase">Whether matching is case-sensitive.</param>
    /// <returns>The matching attribute values.</returns>
    public OdfSearchMatch[] QueryField(
        OdfDocument document,
        OdfQualifiedName fieldName,
        string value,
        bool matchCase)
    {
        return QueryServiceModel.QueryField(
            document,
            fieldName,
            value,
            matchCase);
    }

    /// <summary>
    /// Finds XML fields and typed resources that refer to an exact target.
    /// </summary>
    /// <param name="document">The document to search.</param>
    /// <param name="target">The non-empty reference target.</param>
    /// <returns>The matching references.</returns>
    public OdfSearchMatch[] QueryReferences(
        OdfDocument document,
        string target)
    {
        return QueryServiceModel.QueryReferences(document, target);
    }
}
