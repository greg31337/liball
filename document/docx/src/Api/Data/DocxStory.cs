namespace docx;

/// <summary>
/// Describes one relationship-discovered WordprocessingML text story.
/// </summary>
public class DocxStory
{
    /// <summary>
    /// The kind of story.
    /// </summary>
    public DocxStoryType Type;
    /// <summary>
    /// The package root or part from which the story relationship originates.
    /// </summary>
    public string SourcePartName;
    /// <summary>
    /// The normalized package part name containing the story.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The relationship identifier that discovers the story.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// The parsed text-bearing story content.
    /// </summary>
    public IDocxTextContent Content;

    /// <summary>
    /// Creates a story descriptor.
    /// </summary>
    /// <param name="type">The kind of story.</param>
    /// <param name="sourcePartName">The package root or source part name.</param>
    /// <param name="partName">The story package part name.</param>
    /// <param name="relationshipId">The relationship identifier.</param>
    /// <param name="content">The parsed story content.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxStory(
        DocxStoryType type,
        string sourcePartName,
        string partName,
        string relationshipId,
        IDocxTextContent content)
    {
        if (sourcePartName == null)
        {
            throw new ArgumentNullException("sourcePartName");
        }

        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (content == null)
        {
            throw new ArgumentNullException("content");
        }

        this.Type = type;
        this.SourcePartName = sourcePartName;
        this.PartName = partName;
        this.RelationshipId = relationshipId;
        this.Content = content;
    }
}
