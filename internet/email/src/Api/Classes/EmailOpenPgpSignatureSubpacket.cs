using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP signature subpacket.
/// </summary>
public class EmailOpenPgpSignatureSubpacket : IEmailOpenPgpSignatureSubpacketContract
{
    int typeIdentifier;
    bool critical;
    byte[] data;
    byte[] encodedBytes;
    /// <summary>
    /// Creates a new openPGP signature subpacket.
    /// </summary>
    /// <param name = "typeIdentifier">The type identifier used by the operation.</param>
    /// <param name = "critical">Whether to enable the critical behavior.</param>
    /// <param name = "data">The source bytes or data to process.</param>
    /// <param name = "encodedBytes">The source bytes or data to process.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpSignatureSubpacket(int typeIdentifier, bool critical, byte[] data, byte[] encodedBytes)
    {
        if (typeIdentifier < 0 || typeIdentifier > 127)
            throw new ArgumentOutOfRangeException("typeIdentifier");
        if (data == null || encodedBytes == null)
            throw new ArgumentNullException("data");
        this.typeIdentifier = typeIdentifier;
        this.critical = critical;
        this.data = new EmailOpenPgpArmorResultOperations().Copy(data);
        this.encodedBytes = new EmailOpenPgpArmorResultOperations().Copy(encodedBytes);
    }

    /// <summary>
    /// Gets the type identifier.
    /// </summary>
    /// <returns>The current type identifier.</returns>
    public int GetTypeIdentifier()
    {
        return typeIdentifier;
    }

    /// <summary>
    /// Determines whether the current value or state is critical.
    /// </summary>
    /// <returns><c>true</c> when the value or state is critical; otherwise, <c>false</c>.</returns>
    public bool IsCritical()
    {
        return critical;
    }

    /// <summary>
    /// Gets the data.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current data.</returns>
    public byte[] GetData()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(data);
    }

    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    public byte[] GetEncodedBytes()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(encodedBytes);
    }
}
