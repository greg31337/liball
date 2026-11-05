namespace tiff;

/// <summary>Represents GDAL ASCII value used by the public TIFF API.</summary>
public class TiffGdalAsciiValue
{
    /// <summary>Indicates whether the value is present.</summary>
    public bool HasValue;
    /// <summary>Contains the text.</summary>
    public string Text;
    /// <summary>Contains the values.</summary>
    public string[] Values;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;

    /// <summary>Creates a new instance of the TIFF GDAL ASCII Value class.</summary>
    public TiffGdalAsciiValue()
    {
        HasValue = false;
        Text = "";
        Values = new string[0];
        RawBytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF GDAL ASCII Value class.</summary>
    /// <param name="hasValue">The has value.</param>
    /// <param name="text">The text.</param>
    /// <param name="values">The values.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    internal static TiffGdalAsciiValue Create(bool hasValue, string text, string[] values, byte[] rawBytes)
    {
        TiffGdalAsciiValue value = new TiffGdalAsciiValue();
        value.Initialize(hasValue, text, values, rawBytes);
        return value;
    }

    internal void Initialize(bool hasValue, string text, string[] values, byte[] rawBytes)
    {
        HasValue = hasValue;
        Text = NormalizeString(text);
        Values = TiffAscii.CopyStrings(values);
        RawBytes = TiffTagValue.CopyByteArray(rawBytes);
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF GDAL ASCII value.</returns>
    internal TiffGdalAsciiValue Copy()
    {
        return TiffGdalAsciiValue.Create(HasValue, Text, Values, RawBytes);
    }

    internal static string NormalizeString(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}
