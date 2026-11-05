namespace pptx;

/// <summary>
/// Represents package cleanup result data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxPackageCleanupResult
{
    /// <summary>
    /// Stores the diagnostics value associated with this object.
    /// </summary>
    public PptxResult Diagnostics;
    /// <summary>
    /// Stores the relationships removed value associated with this object.
    /// </summary>
    public int RelationshipsRemoved;
    /// <summary>
    /// Stores the unused parts removed value associated with this object.
    /// </summary>
    public int UnusedPartsRemoved;
    /// <summary>
    /// Stores the duplicate media parts removed value associated with this object.
    /// </summary>
    public int DuplicateMediaPartsRemoved;
    /// <summary>
    /// Stores the media relationships retargeted value associated with this object.
    /// </summary>
    public int MediaRelationshipsRetargeted;
    /// <summary>
    /// Stores the content type overrides removed value associated with this object.
    /// </summary>
    public int ContentTypeOverridesRemoved;
    /// <summary>
    /// Indicates whether the raw entries compacted setting is enabled.
    /// </summary>
    public bool RawEntriesCompacted;

    /// <summary>
    /// Initializes a new package cleanup result with safe defaults or the supplied values.
    /// </summary>
    public PptxPackageCleanupResult()
    {
        Diagnostics = new PptxResult();
        RelationshipsRemoved = 0;
        UnusedPartsRemoved = 0;
        DuplicateMediaPartsRemoved = 0;
        MediaRelationshipsRetargeted = 0;
        ContentTypeOverridesRemoved = 0;
        RawEntriesCompacted = false;
    }
    internal bool Succeeded()
    {
        return Diagnostics.Succeeded();
    }
    internal bool Failed()
    {
        return Diagnostics.Failed();
    }
    internal int ChangeCount()
    {
        int count = RelationshipsRemoved + UnusedPartsRemoved + DuplicateMediaPartsRemoved + MediaRelationshipsRetargeted + ContentTypeOverridesRemoved;
        if (RawEntriesCompacted)
        {
            count = count + 1;
        }

        return count;
    }
}

