namespace pptx;

/// <summary>
/// Describes one compatibility-relevant feature occurrence in a package.
/// </summary>
public class PptxCompatibilityFeature
{
    /// <summary>Stores the stable feature name.</summary>
    public string Name;
    /// <summary>Stores the compatibility classification.</summary>
    public PptxCompatibilityLevel Level;
    /// <summary>Stores the related package part name, or an empty string.</summary>
    public string PartName;
    /// <summary>Stores the related relationship identifier, or an empty string.</summary>
    public string RelationshipId;
    /// <summary>Stores a concise explanation of the classification.</summary>
    public string Details;

    /// <summary>
    /// Initializes an empty compatibility feature.
    /// </summary>
    public PptxCompatibilityFeature()
    {
        Name = "";
        Level = PptxCompatibilityLevel.PreservedOnly;
        PartName = "";
        RelationshipId = "";
        Details = "";
    }
}
