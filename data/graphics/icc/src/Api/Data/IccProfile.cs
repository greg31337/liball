namespace icc;

/// <summary>
/// Represents an ICC profile envelope, original directory, decoded tags, and preserved source bytes.
/// </summary>
public class IccProfile
{
    /// <summary>
    /// Gets or sets the profile header.
    /// </summary>
    public IccProfileHeader Header;

    /// <summary>
    /// Gets or sets the tag-table entries in their original order.
    /// </summary>
    public IccTagTableEntry[] TagTableEntries;

    /// <summary>
    /// Gets or sets the profile tags in their original order.
    /// </summary>
    public IccTag[] Tags;

    /// <summary>
    /// Gets or sets ordered non-fatal diagnostics retained while reading the profile.
    /// </summary>
    public IccValidationIssue[] ValidationIssues;

    /// <summary>
    /// Gets or sets the complete original declared profile bytes.
    /// </summary>
    public byte[] OriginalBytes;

    /// <summary>
    /// Gets or sets bytes following the declared profile when the read mode retains them.
    /// </summary>
    public byte[] TrailingBytes;

    /// <summary>
    /// Gets or sets the profile location within its containing input.
    /// </summary>
    public IccByteLocation Location;

    /// <summary>
    /// Gets or sets the non-throwing read or probe outcome associated with the profile.
    /// </summary>
    public IccOperationResult ReadResult;

    /// <summary>
    /// Initializes an empty profile with independent child data and buffers.
    /// </summary>
    public IccProfile()
    {
        Header = new IccProfileHeader();
        TagTableEntries = new IccTagTableEntry[0];
        Tags = new IccTag[0];
        ValidationIssues = new IccValidationIssue[0];
        OriginalBytes = new byte[0];
        TrailingBytes = new byte[0];
        Location = new IccByteLocation();
        ReadResult = new IccOperationResult();
    }
}
