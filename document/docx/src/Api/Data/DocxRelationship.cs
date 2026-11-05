namespace docx;

/// <summary>
/// Describes an internal or external OPC relationship.
/// </summary>
public class DocxRelationship
{
    /// <summary>
    /// The relationship identifier, which must be unique within its relationship set.
    /// </summary>
    public string Id;
    /// <summary>
    /// The type value stored in the document model.
    /// </summary>
    public string Type;
    /// <summary>
    /// The target value stored in the document model.
    /// </summary>
    public string Target;
    /// <summary>
    /// The target mode value stored in the document model.
    /// </summary>
    public string TargetMode;
    /// <summary>
    /// Whether the is external behavior is enabled.
    /// </summary>
    public bool IsExternal;
    internal List<DocxRelationshipAttribute> UnknownAttributes;

    /// <summary>
    /// Creates a relationship instance.
    /// </summary>
    /// <param name="id">The id used by the operation.</param>
    /// <param name="type">The type used by the operation.</param>
    /// <param name="target">The target used by the operation.</param>
    /// <param name="targetMode">The target mode used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty, malformed, or not supported.</exception>
    public DocxRelationship(string id, string type, string target, string targetMode)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            throw new ArgumentException("Relationship id is required.", "id");
        }

        this.Id = id;
        this.Type = "";
        this.Target = "";
        this.TargetMode = "";
        this.IsExternal = false;
        this.UnknownAttributes = new List<DocxRelationshipAttribute>();

        Update(type, target, targetMode);
    }

    internal void Update(string type, string target, string targetMode)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (targetMode == null)
        {
            throw new ArgumentNullException("targetMode");
        }

        if (type == "")
        {
            throw new ArgumentException("Relationship type is required.", "type");
        }

        if (target == "")
        {
            throw new ArgumentException("Relationship target is required.", "target");
        }

        this.Type = type;
        this.Target = target;
        this.TargetMode = targetMode;
        this.IsExternal = string.Equals(targetMode, "External", StringComparison.OrdinalIgnoreCase);
    }

    internal void AddUnknownAttribute(DocxRelationshipAttribute attribute)
    {
        if (attribute == null)
        {
            throw new ArgumentNullException("attribute");
        }

        this.UnknownAttributes.Add(attribute);
    }

    internal DocxRelationship CreateDiagnosticCopy()
    {
        DocxRelationship copy = new DocxRelationship(this.Id, this.Type, this.Target, this.TargetMode);

        foreach (DocxRelationshipAttribute attribute in this.UnknownAttributes)
        {
            copy.AddUnknownAttribute(new DocxRelationshipAttribute(
                attribute.Prefix,
                attribute.LocalName,
                attribute.NamespaceUri,
                attribute.Value));
        }

        return copy;
    }

    internal int UnknownAttributeCount()
    {
        return this.UnknownAttributes.Count;
    }

    internal string ResolveTargetPartName(string sourcePartName)
    {
        if (this.IsExternal)
        {
            throw new InvalidOperationException("External relationship targets cannot be resolved to package part names.");
        }

        return DocxRelationshipSet.ResolveTargetPartName(sourcePartName, this.Target);
    }
}
