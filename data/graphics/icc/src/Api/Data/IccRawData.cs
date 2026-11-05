namespace icc;

/// <summary>
/// Represents a losslessly retained encoded tag payload.
/// </summary>
public class IccRawData
{
    /// <summary>
    /// Gets or sets the payload type signature when at least four bytes are available.
    /// </summary>
    public uint TypeSignature;

    /// <summary>
    /// Gets or sets whether a complete type signature was available.
    /// </summary>
    public bool HasTypeSignature;

    /// <summary>
    /// Gets or sets the complete encoded payload, including its type signature and reserved bytes.
    /// </summary>
    public byte[] Bytes;

    /// <summary>
    /// Initializes an empty raw payload.
    /// </summary>
    public IccRawData()
    {
        TypeSignature = 0;
        HasTypeSignature = false;
        Bytes = new byte[0];
    }
}
