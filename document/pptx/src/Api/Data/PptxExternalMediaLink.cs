namespace pptx;

/// <summary>
/// Describes one externally linked audio or video relationship found during an audit.
/// </summary>
public class PptxExternalMediaLink
{
    /// <summary>
    /// Stores the slide part containing the relationship.
    /// </summary>
    public string SlidePartName;
    /// <summary>
    /// Stores the relationship identifier.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the audio or video media type.
    /// </summary>
    public string MediaType;
    /// <summary>
    /// Stores the external target.
    /// </summary>
    public string Target;
    /// <summary>
    /// Indicates whether the target uses HTTPS.
    /// </summary>
    public bool Secure;
    /// <summary>
    /// Indicates whether a non-empty target is present.
    /// </summary>
    public bool TargetPresent;

    /// <summary>
    /// Initializes an external-media link with safe defaults.
    /// </summary>
    public PptxExternalMediaLink()
    {
        SlidePartName = "";
        RelationshipId = "";
        MediaType = PptxMediaType.Unknown;
        Target = "";
        Secure = false;
        TargetPresent = false;
    }
}
