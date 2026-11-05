namespace docx;

/// <summary>
/// Describes an internal relationship whose resolved target part is absent from the package.
/// </summary>
public class DocxOrphanRelationship
{
    /// <summary>
    /// The package root marker or normalized part name from which the relationship originates.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// The relationship with the missing internal target.
    /// </summary>
    public DocxRelationship Relationship;
    /// <summary>
    /// The normalized target part name that is absent from the package.
    /// </summary>
    public string TargetPartName;

    /// <summary>
    /// Creates an orphan relationship description.
    /// </summary>
    /// <param name="sourcePartName">The package root marker or source package part name.</param>
    /// <param name="relationship">The relationship with the missing target.</param>
    /// <param name="targetPartName">The normalized missing target part name.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxOrphanRelationship(string sourcePartName, DocxRelationship relationship, string targetPartName)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (relationship == null)
        {
            throw new ArgumentNullException("relationship");
        }

        if (targetPartName == null)
        {
            throw new ArgumentNullException("targetPartName");
        }

        this.SourcePartName = sourcePartName;
        this.Relationship = relationship;
        this.TargetPartName = targetPartName;
    }
}
