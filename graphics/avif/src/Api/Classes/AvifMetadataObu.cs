namespace avif;

/// <summary>Owns opaque bytes for an unknown AV1 metadata type.</summary>
public class AvifMetadataObu : IAvifMetadataObu
{
    AvifResultCode code;
    ulong metadataType;
    byte[] data;

    /// <summary>Creates an opaque metadata value for an unknown AV1 metadata type.</summary>
    /// <param name="metadataType">An unsigned type greater than the defined types one through five.</param>
    /// <param name="data">The opaque metadata bytes to copy.</param>
    public AvifMetadataObu(ulong metadataType, byte[]? data)
    {
        this.metadataType = metadataType;
        this.data = data == null
            ? System.Array.Empty<byte>() : (byte[])data.Clone();
        code = data == null || metadataType <= 5
            ? AvifResultCode.InvalidArgument : AvifResultCode.Success;
    }

    /// <summary>Returns the construction result.</summary>
    /// <returns>The stable result code.</returns>
    public AvifResultCode GetCode()
    {
        return code;
    }

    /// <summary>Returns the unsigned AV1 metadata type.</summary>
    /// <returns>The metadata type value.</returns>
    public ulong GetMetadataType()
    {
        return metadataType;
    }

    /// <summary>Returns an independent copy of the opaque metadata bytes.</summary>
    /// <returns>The bytes following the metadata type and preceding trailing bits.</returns>
    public byte[] GetData()
    {
        return code == AvifResultCode.Success
            ? (byte[])data.Clone() : System.Array.Empty<byte>();
    }

    internal byte[] GetEncodedObu()
    {
        if (code != AvifResultCode.Success)
        {
            return System.Array.Empty<byte>();
        }
        byte[] type = Av1ObuWriter.WriteLeb128(metadataType);
        if (type.Length > int.MaxValue - data.Length - 1)
        {
            return System.Array.Empty<byte>();
        }
        byte[] payload = new byte[type.Length + data.Length + 1];
        type.CopyTo(payload, 0);
        data.CopyTo(payload, type.Length);
        payload[payload.Length - 1] = 128;
        Av1WriteResult encoded = Av1ObuWriter.WriteObu(
            Av1ObuType.Metadata,
            payload,
            false,
            0,
            0);
        return encoded.Code == AvifResultCode.Success
            ? encoded.Data : System.Array.Empty<byte>();
    }
}
