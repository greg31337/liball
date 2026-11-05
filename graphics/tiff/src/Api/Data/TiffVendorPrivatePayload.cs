namespace tiff;

/// <summary>Preserves one opaque vendor-private TIFF tag payload.</summary>
public class TiffVendorPrivatePayload
{
    /// <summary>Identifies the vendor-private TIFF tag.</summary>
    public int TagId;
    /// <summary>Specifies the stored TIFF field type.</summary>
    public TiffDataType DataType;
    /// <summary>Contains the stored element count.</summary>
    public long Count;
    /// <summary>Contains the exact encoded payload bytes.</summary>
    public byte[] Bytes;

    /// <summary>Creates an empty private payload.</summary>
    public TiffVendorPrivatePayload()
    {
        TagId = 0;
        DataType = TiffDataType.Unknown;
        Count = 0;
        Bytes = new byte[0];
    }

    internal static TiffVendorPrivatePayload Create(int tagId, TiffDataType dataType, long count, byte[] bytes)
    {
        TiffVendorPrivatePayload payload = new TiffVendorPrivatePayload();
        payload.TagId = tagId;
        payload.DataType = dataType;
        payload.Count = count;
        payload.Bytes = TiffTagValue.CopyByteArray(bytes);
        return payload;
    }

    internal TiffVendorPrivatePayload Copy()
    {
        return TiffVendorPrivatePayload.Create(TagId, DataType, Count, Bytes);
    }
}
