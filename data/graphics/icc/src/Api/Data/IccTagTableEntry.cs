namespace icc;

/// <summary>
/// Represents one original tag-table directory entry and its shared-payload identity.
/// </summary>
public class IccTagTableEntry
{
    /// <summary>
    /// Gets or sets the tag signature.
    /// </summary>
    public uint Signature;

    /// <summary>
    /// Gets or sets the validated absolute byte offset of the tag payload.
    /// </summary>
    public long Offset;

    /// <summary>
    /// Gets or sets the validated tag payload size in bytes.
    /// </summary>
    public int Size;

    /// <summary>
    /// Gets or sets the entry index in the original tag table.
    /// </summary>
    public int OriginalIndex;

    /// <summary>
    /// Gets or sets a shared-payload identity, or <c>-1</c> when the payload is not shared.
    /// </summary>
    public int SharedDataId;

    /// <summary>
    /// Initializes an unassigned tag-table entry.
    /// </summary>
    public IccTagTableEntry()
    {
        Signature = 0;
        Offset = 0;
        Size = 0;
        OriginalIndex = -1;
        SharedDataId = -1;
    }
}
