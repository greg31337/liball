namespace tiff;

/// <summary>Preserves one unrecognized ImageJ private metadata block.</summary>
public class TiffImageJPrivateMetadataBlock
{
    /// <summary>Contains the four-byte ImageJ metadata type code.</summary>
    public uint TypeCode;
    /// <summary>Contains the zero-based item index within the type.</summary>
    public int ItemIndex;
    /// <summary>Contains the uninterpreted block bytes.</summary>
    public byte[] Data;

    /// <summary>Creates an empty ImageJ private metadata block.</summary>
    public TiffImageJPrivateMetadataBlock()
    {
        TypeCode = 0;
        ItemIndex = 0;
        Data = new byte[0];
    }

    internal static TiffImageJPrivateMetadataBlock Create(uint typeCode, int itemIndex, byte[] data)
    {
        TiffImageJPrivateMetadataBlock value = new TiffImageJPrivateMetadataBlock();
        value.TypeCode = typeCode;
        value.ItemIndex = itemIndex;
        value.Data = TiffTagValue.CopyByteArray(data);
        return value;
    }

    internal TiffImageJPrivateMetadataBlock Copy()
    {
        return TiffImageJPrivateMetadataBlock.Create(TypeCode, ItemIndex, Data);
    }
}
