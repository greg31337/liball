namespace tiff;

/// <summary>Represents XMP packet used by the public TIFF API.</summary>
public class TiffXmpPacket
{
    /// <summary>Indicates whether the XMP is present.</summary>
    public bool HasXmp;
    /// <summary>Specifies the TIFF field data type.</summary>
    public TiffDataType DataType;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;
    /// <summary>Contains the text.</summary>
    public string Text;

    /// <summary>Creates a new instance of the TIFF XMP Packet class.</summary>
    public TiffXmpPacket()
    {
        HasXmp = false;
        DataType = TiffDataType.Byte;
        Bytes = new byte[0];
        Text = "";
    }

    /// <summary>Creates a new instance of the TIFF XMP Packet class.</summary>
    /// <param name="hasXmp">The has XMP.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="bytes">The bytes.</param>
    /// <param name="text">The text.</param>
    internal static TiffXmpPacket Create(bool hasXmp, TiffDataType dataType, byte[] bytes, string text)
    {
        TiffXmpPacket value = new TiffXmpPacket();
        value.Initialize(hasXmp, dataType, bytes, text);
        return value;
    }

    internal void Initialize(bool hasXmp, TiffDataType dataType, byte[] bytes, string text)
    {
        HasXmp = hasXmp;
        DataType = dataType;
        Bytes = TiffTagValue.CopyByteArray(bytes);
        if (text == null)
        {
            Text = "";
        }
        else
        {
            Text = text;
        }
    }

    /// <summary>Returns the number of available byte entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int ByteCount()
    {
        return Bytes.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF XMP packet.</returns>
    internal TiffXmpPacket Copy()
    {
        return TiffXmpPacket.Create(HasXmp, DataType, Bytes, Text);
    }
}
