namespace asn1;

/// <summary>
/// Contains a minimally sized bit-string payload and its exact bit length.
/// </summary>
/// <remarks>
/// Bytes contains exactly enough octets for BitLength. Unused low-order bits in the final octet are zero.
/// </remarks>
public class AsnBitStringValue
{
    /// <summary>
    /// Contains bits in most-significant-bit-first octet order.
    /// </summary>
    public byte[] Bytes = [];

    /// <summary>
    /// Contains the non-negative number of meaningful bits.
    /// </summary>
    public int BitLength;

    internal bool IsValid()
    {
        if (this.Bytes == null || this.BitLength < 0)
        {
            return false;
        }

        int requiredByteCount = this.BitLength / 8;
        int remainder = this.BitLength % 8;
        if (remainder != 0)
        {
            requiredByteCount = requiredByteCount + 1;
        }

        if (this.Bytes.Length != requiredByteCount)
        {
            return false;
        }

        if (remainder == 0)
        {
            return true;
        }

        int unusedBitCount = 8 - remainder;
        int unusedBitMask = (1 << unusedBitCount) - 1;
        return (this.Bytes[this.Bytes.Length - 1] & unusedBitMask) == 0;
    }
}
