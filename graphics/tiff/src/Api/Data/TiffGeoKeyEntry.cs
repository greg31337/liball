namespace tiff;

/// <summary>Represents geographic key entry used by the public TIFF API.</summary>
public class TiffGeoKeyEntry
{
    /// <summary>Contains the key id.</summary>
    public int KeyId;
    /// <summary>Contains the TIFF tag location.</summary>
    public int TiffTagLocation;
    /// <summary>Contains the number of elements represented by the value.</summary>
    public int Count;
    /// <summary>Contains the value offset.</summary>
    public int ValueOffset;
    /// <summary>Indicates whether the key value was resolved from its declared storage location.</summary>
    public bool HasResolvedValue;
    /// <summary>Identifies the resolved value storage form.</summary>
    public TiffGeoKeyValueKind ValueKind;
    /// <summary>Contains resolved inline SHORT values represented as integers.</summary>
    public int[] IntegerValues;
    /// <summary>Contains resolved DOUBLE parameter values.</summary>
    public double[] DoubleValues;
    /// <summary>Contains the resolved ASCII value without its trailing pipe delimiter.</summary>
    public string AsciiValue;
    /// <summary>Indicates whether the key identifier is in the private GeoKey range.</summary>
    public bool IsPrivate;

    /// <summary>Creates a new instance of the TIFF geographic Key Entry class.</summary>
    public TiffGeoKeyEntry()
    {
        KeyId = 0;
        TiffTagLocation = 0;
        Count = 0;
        ValueOffset = 0;
        HasResolvedValue = false;
        ValueKind = TiffGeoKeyValueKind.None;
        IntegerValues = new int[0];
        DoubleValues = new double[0];
        AsciiValue = "";
        IsPrivate = false;
    }

    /// <summary>Creates a new instance of the TIFF geographic Key Entry class.</summary>
    /// <param name="keyId">The key id.</param>
    /// <param name="tiffTagLocation">The tiff tag location.</param>
    /// <param name="count">The count.</param>
    /// <param name="valueOffset">The value offset.</param>
    internal static TiffGeoKeyEntry Create(int keyId, int tiffTagLocation, int count, int valueOffset)
    {
        TiffGeoKeyEntry value = new TiffGeoKeyEntry();
        value.Initialize(keyId, tiffTagLocation, count, valueOffset);
        return value;
    }

    internal void Initialize(int keyId, int tiffTagLocation, int count, int valueOffset)
    {
        KeyId = keyId;
        TiffTagLocation = tiffTagLocation;
        Count = count;
        ValueOffset = valueOffset;
        IsPrivate = keyId >= 32768 && keyId <= 65535;
    }

    /// <summary>Determines whether is inline value under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsInlineValue()
    {
        return TiffTagLocation == 0;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF geographic key entry.</returns>
    internal TiffGeoKeyEntry Copy()
    {
        TiffGeoKeyEntry copy = TiffGeoKeyEntry.Create(KeyId, TiffTagLocation, Count, ValueOffset);
        copy.HasResolvedValue = HasResolvedValue;
        copy.ValueKind = ValueKind;
        copy.IntegerValues = CopyIntegers(IntegerValues);
        copy.DoubleValues = TiffGeoModelMetadata.CopyDoubleArray(DoubleValues);
        copy.AsciiValue = AsciiValue == null ? "" : AsciiValue;
        copy.IsPrivate = KeyId >= 32768 && KeyId <= 65535;
        return copy;
    }

    internal static int[] CopyIntegers(int[] values)
    {
        if (values == null)
        {
            return new int[0];
        }

        int[] copy = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
