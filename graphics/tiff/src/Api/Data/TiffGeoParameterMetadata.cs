namespace tiff;

/// <summary>Represents geographic parameter metadata extracted from or prepared for a TIFF file.</summary>
public class TiffGeoParameterMetadata
{
    /// <summary>Indicates whether the double params is present.</summary>
    public bool HasDoubleParams;
    /// <summary>Indicates whether the ASCII params is present.</summary>
    public bool HasAsciiParams;
    /// <summary>Contains the double params.</summary>
    public double[] DoubleParams;
    /// <summary>Contains the ASCII text.</summary>
    public string AsciiText;
    /// <summary>Contains the ASCII values.</summary>
    public string[] AsciiValues;
    /// <summary>Contains the ASCII bytes.</summary>
    public byte[] AsciiBytes;

    /// <summary>Creates a new instance of the TIFF geographic Parameter Metadata class.</summary>
    public TiffGeoParameterMetadata()
    {
        HasDoubleParams = false;
        HasAsciiParams = false;
        DoubleParams = new double[0];
        AsciiText = "";
        AsciiValues = new string[0];
        AsciiBytes = new byte[0];
    }

    /// <summary>Creates a new instance of the TIFF geographic Parameter Metadata class.</summary>
    /// <param name="hasDoubleParams">The has double params.</param>
    /// <param name="doubleParams">The double params.</param>
    /// <param name="hasAsciiParams">The has ASCII params.</param>
    /// <param name="asciiText">The ascii text.</param>
    /// <param name="asciiValues">The ascii values.</param>
    /// <param name="asciiBytes">The ascii bytes.</param>
    internal static TiffGeoParameterMetadata Create(bool hasDoubleParams, double[] doubleParams, bool hasAsciiParams, string asciiText, string[] asciiValues, byte[] asciiBytes)
    {
        TiffGeoParameterMetadata value = new TiffGeoParameterMetadata();
        value.Initialize(hasDoubleParams, doubleParams, hasAsciiParams, asciiText, asciiValues, asciiBytes);
        return value;
    }

    internal void Initialize(bool hasDoubleParams, double[] doubleParams, bool hasAsciiParams, string asciiText, string[] asciiValues, byte[] asciiBytes)
    {
        HasDoubleParams = hasDoubleParams;
        HasAsciiParams = hasAsciiParams;
        DoubleParams = TiffGeoModelMetadata.CopyDoubleArray(doubleParams);
        AsciiText = NormalizeString(asciiText);
        AsciiValues = TiffAscii.CopyStrings(asciiValues);
        AsciiBytes = TiffTagValue.CopyByteArray(asciiBytes);
    }

    /// <summary>Returns the number of available double entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int DoubleCount()
    {
        return DoubleParams.Length;
    }

    /// <summary>Returns the number of available ASCII value entries.</summary>
    /// <returns>The resulting int.</returns>
    internal int AsciiValueCount()
    {
        return AsciiValues.Length;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic parameter metadata.</returns>
    internal TiffGeoParameterMetadata Copy()
    {
        return TiffGeoParameterMetadata.Create(HasDoubleParams, DoubleParams, HasAsciiParams, AsciiText, AsciiValues, AsciiBytes);
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
