namespace icc;

/// <summary>
/// Represents the 16-byte ICC Profile ID and whether a non-zero ID was supplied.
/// </summary>
public class IccProfileId
{
    /// <summary>
    /// Gets or sets the 16 bytes encoded in the Profile ID header field.
    /// </summary>
    public byte[] Bytes;

    /// <summary>
    /// Gets or sets whether the encoded Profile ID is present rather than all zero.
    /// </summary>
    public bool IsPresent;

    /// <summary>
    /// Initializes an absent, zero-filled Profile ID.
    /// </summary>
    public IccProfileId()
    {
        Bytes = new byte[16];
        IsPresent = false;
    }
}
