namespace tiff;

/// <summary>Represents geographic ASCII params used by the public TIFF API.</summary>
public class TiffGeoAsciiParams
{
    /// <summary>Indicates whether the ASCII params is present.</summary>
    public bool HasAsciiParams;
    /// <summary>Contains the text.</summary>
    public string Text;
    /// <summary>Contains the values.</summary>
    public string[] Values;
    /// <summary>Contains the resulting bytes in their documented storage or pixel order.</summary>
    public byte[] Bytes;

    /// <summary>Creates a new instance of the TIFF geographic ASCII Params class.</summary>
    public TiffGeoAsciiParams()
    {
        HasAsciiParams = false;
        Text = "";
        Values = new string[0];
        Bytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF geographic ASCII Params class.</summary>
    /// <param name="hasAsciiParams">The has ASCII params.</param>
    /// <param name="text">The text.</param>
    /// <param name="values">The values.</param>
    /// <param name="bytes">The bytes.</param>
    internal static TiffGeoAsciiParams Create(bool hasAsciiParams, string text, string[] values, byte[] bytes)
    {
        TiffGeoAsciiParams value = new TiffGeoAsciiParams();
        value.Initialize(hasAsciiParams, text, values, bytes);
        return value;
    }

    internal void Initialize(bool hasAsciiParams, string text, string[] values, byte[] bytes)
    {
        HasAsciiParams = hasAsciiParams;
        Text = TiffGeoParameterMetadata.NormalizeString(text);
        Values = TiffAscii.CopyStrings(values);
        Bytes = TiffTagValue.CopyByteArray(bytes);
    }
}
