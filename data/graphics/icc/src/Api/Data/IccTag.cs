namespace icc;

/// <summary>
/// Represents one profile tag, its original directory entry, data, padding, and decode outcome.
/// </summary>
public class IccTag
{
    internal TagDecodeState? DecodeState;

    /// <summary>
    /// Gets or sets the tag signature.
    /// </summary>
    public uint Signature;

    /// <summary>
    /// Gets or sets the original tag-table entry.
    /// </summary>
    public IccTagTableEntry TableEntry;

    /// <summary>
    /// Gets or sets the decoded or raw tag data.
    /// </summary>
    public IccTagData Data;

    /// <summary>
    /// Gets or sets the original bytes between the payload end and the next four-byte boundary.
    /// </summary>
    public byte[] PaddingBytes;

    /// <summary>
    /// Gets or sets whether tag decoding has been attempted.
    /// </summary>
    public bool IsDecodeAttempted;

    /// <summary>
    /// Gets or sets the stable outcome of tag decoding.
    /// </summary>
    public IccOperationResult DecodeResult;

    /// <summary>
    /// Initializes an unassigned tag with independent child data.
    /// </summary>
    public IccTag()
    {
        DecodeState = null;
        Signature = 0;
        TableEntry = new IccTagTableEntry();
        Data = new IccTagData();
        PaddingBytes = new byte[0];
        IsDecodeAttempted = false;
        DecodeResult = new IccOperationResult();
    }
}
