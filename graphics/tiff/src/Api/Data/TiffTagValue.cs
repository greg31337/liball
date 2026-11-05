using System;

namespace tiff;

/// <summary>Stores one typed TIFF tag value in a representation suitable for reading, editing, or writing.</summary>
/// <remarks>The field matching <c>DataType</c> contains the semantic value. <c>RawBytes</c> is used when a value must be retained without interpreting it.</remarks>
public class TiffTagValue
{
    /// <summary>Identifies the TIFF tag.</summary>
    public int TagId;
    /// <summary>Specifies the TIFF field data type.</summary>
    public TiffDataType DataType;
    /// <summary>Contains the number of elements represented by the value.</summary>
    public long Count;
    /// <summary>Indicates whether the tag value is represented only by its uninterpreted encoded bytes.</summary>
    public bool IsRaw;
    /// <summary>Contains the unsigned values.</summary>
    public ulong[] UnsignedValues;
    /// <summary>Contains the signed values.</summary>
    public long[] SignedValues;
    /// <summary>Contains the rational values.</summary>
    public TiffRational[] RationalValues;
    /// <summary>Contains the floating values.</summary>
    public double[] FloatingValues;
    /// <summary>Contains the text.</summary>
    public string Text;
    /// <summary>Contains the text values.</summary>
    public string[] TextValues;
    /// <summary>Contains the original encoded bytes so the value can be preserved without semantic conversion.</summary>
    public byte[] RawBytes;

    /// <summary>Creates a new instance of the TIFF Tag Value class.</summary>
    public TiffTagValue()
    {
        TagId = 0;
        DataType = TiffDataType.Unknown;
        Count = 0;
        IsRaw = false;
        UnsignedValues = new ulong[0];
        SignedValues = new long[0];
        RationalValues = new TiffRational[0];
        FloatingValues = new double[0];
        Text = "";
        TextValues = new string[0];
        RawBytes = new byte[0];
    }

    /// <summary>Creates unsigned scalar with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateUnsignedScalar(int tagId, TiffDataType dataType, ulong value)
    {
        ulong[] values = new ulong[1];
        values[0] = value;
        return CreateUnsignedArray(tagId, dataType, values);
    }

    /// <summary>Creates unsigned array with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateUnsignedArray(int tagId, TiffDataType dataType, ulong[] values)
    {
        if (!IsUnsignedDataType(dataType))
        {
            throw new ArgumentException("Data type is not an unsigned integer TIFF type.");
        }

        TiffTagValue tagValue = CreateBase(tagId, dataType);
        tagValue.UnsignedValues = CopyUnsignedArray(values);
        tagValue.Count = tagValue.UnsignedValues.Length;
        return tagValue;
    }

    /// <summary>Creates signed scalar with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateSignedScalar(int tagId, TiffDataType dataType, long value)
    {
        long[] values = new long[1];
        values[0] = value;
        return CreateSignedArray(tagId, dataType, values);
    }

    /// <summary>Creates signed array with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateSignedArray(int tagId, TiffDataType dataType, long[] values)
    {
        if (!IsSignedDataType(dataType))
        {
            throw new ArgumentException("Data type is not a signed integer TIFF type.");
        }

        TiffTagValue tagValue = CreateBase(tagId, dataType);
        tagValue.SignedValues = CopySignedArray(values);
        tagValue.Count = tagValue.SignedValues.Length;
        return tagValue;
    }

    /// <summary>Creates ASCII with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="text">The text.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateAscii(int tagId, string text)
    {
        string[] values = new string[1];
        if (text == null)
        {
            values[0] = "";
        }
        else
        {
            values[0] = text;
        }

        return CreateAsciiArray(tagId, values);
    }

    /// <summary>Creates ASCII array with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="values">The values.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateAsciiArray(int tagId, string[] values)
    {
        TiffTagValue tagValue = CreateBase(tagId, TiffDataType.Ascii);
        tagValue.TextValues = TiffAscii.CopyStrings(values);
        tagValue.Text = TiffAscii.JoinStrings(tagValue.TextValues);
        tagValue.Count = TiffAscii.CalculateEncodedLength(tagValue.TextValues);
        return tagValue;
    }

    /// <summary>Creates rational scalar with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="numerator">The numerator.</param>
    /// <param name="denominator">The denominator.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateRationalScalar(int tagId, TiffDataType dataType, long numerator, long denominator)
    {
        TiffRational[] values = new TiffRational[1];
        values[0] = TiffRational.Create(numerator, denominator);
        return CreateRationalArray(tagId, dataType, values);
    }

    /// <summary>Creates rational array with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateRationalArray(int tagId, TiffDataType dataType, TiffRational[] values)
    {
        if (!IsRationalDataType(dataType))
        {
            throw new ArgumentException("Data type is not a rational TIFF type.");
        }

        TiffTagValue tagValue = CreateBase(tagId, dataType);
        tagValue.RationalValues = CopyRationalArray(values);
        tagValue.Count = tagValue.RationalValues.Length;
        return tagValue;
    }

    /// <summary>Creates floating scalar with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="value">The value.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateFloatingScalar(int tagId, TiffDataType dataType, double value)
    {
        double[] values = new double[1];
        values[0] = value;
        return CreateFloatingArray(tagId, dataType, values);
    }

    /// <summary>Creates floating array with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="values">The values.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateFloatingArray(int tagId, TiffDataType dataType, double[] values)
    {
        if (!IsFloatingDataType(dataType))
        {
            throw new ArgumentException("Data type is not a floating-point TIFF type.");
        }

        TiffTagValue tagValue = CreateBase(tagId, dataType);
        tagValue.FloatingValues = CopyFloatingArray(values);
        tagValue.Count = tagValue.FloatingValues.Length;
        return tagValue;
    }

    /// <summary>Creates raw with normalized defaults suitable for the requested operation.</summary>
    /// <param name="tagId">The numeric TIFF tag identifier.</param>
    /// <param name="dataType">The data type.</param>
    /// <param name="count">The count.</param>
    /// <param name="rawBytes">The raw bytes.</param>
    /// <returns>A new instance initialized with normalized defaults.</returns>
    internal static TiffTagValue CreateRaw(int tagId, TiffDataType dataType, long count, byte[] rawBytes)
    {
        if (!TiffDataTypeInfo.IsKnown(dataType))
        {
            throw new ArgumentException("Data type is not a known TIFF type.");
        }

        TiffMath.RequireNonNegative(count, "count");
        TiffTagValue tagValue = CreateBase(tagId, dataType);
        tagValue.Count = count;
        tagValue.IsRaw = true;
        tagValue.RawBytes = CopyByteArray(rawBytes);
        return tagValue;
    }

    /// <summary>Creates a detached copy whose mutable arrays and child models do not alias this instance.</summary>
    /// <returns>The resulting TIFF tag value.</returns>
    internal TiffTagValue Copy()
    {
        TiffTagValue copy = CreateBase(TagId, DataType);
        copy.Count = Count;
        copy.IsRaw = IsRaw;
        copy.UnsignedValues = CopyUnsignedArray(UnsignedValues);
        copy.SignedValues = CopySignedArray(SignedValues);
        copy.RationalValues = CopyRationalArray(RationalValues);
        copy.FloatingValues = CopyFloatingArray(FloatingValues);
        copy.Text = Text;
        copy.TextValues = TiffAscii.CopyStrings(TextValues);
        copy.RawBytes = CopyByteArray(RawBytes);
        return copy;
    }

    /// <summary>Determines whether is scalar under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsScalar()
    {
        return Count == 1;
    }

    /// <summary>Determines whether is ASCII under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsAscii()
    {
        return DataType == TiffDataType.Ascii && !IsRaw;
    }

    /// <summary>Determines whether is unsigned under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsUnsigned()
    {
        return IsUnsignedDataType(DataType) && !IsRaw;
    }

    /// <summary>Determines whether is signed under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsSigned()
    {
        return IsSignedDataType(DataType) && !IsRaw;
    }

    /// <summary>Determines whether is rational under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsRational()
    {
        return IsRationalDataType(DataType) && !IsRaw;
    }

    /// <summary>Determines whether is floating under the current TIFF data and policy.</summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsFloating()
    {
        return IsFloatingDataType(DataType) && !IsRaw;
    }

    /// <summary>Returns the number of available get value byte entries.</summary>
    /// <returns>The requested value or model.</returns>
    internal long GetValueByteCount()
    {
        return TiffDataTypeInfo.CalculateValueByteCount(DataType, Count);
    }

    /// <summary>Returns the number of available get raw byte entries.</summary>
    /// <returns>The requested value or model.</returns>
    internal int GetRawByteCount()
    {
        return RawBytes.Length;
    }

    /// <summary>Determines whether is unsigned data type under the current TIFF data and policy.</summary>
    /// <param name="dataType">The data type.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal static bool IsUnsignedDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Byte:
            case TiffDataType.Short:
            case TiffDataType.Long:
            case TiffDataType.Ifd:
            case TiffDataType.Long8:
            case TiffDataType.Ifd8:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Determines whether is signed data type under the current TIFF data and policy.</summary>
    /// <param name="dataType">The data type.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal static bool IsSignedDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.SByte:
            case TiffDataType.SShort:
            case TiffDataType.SLong:
            case TiffDataType.SLong8:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Determines whether is rational data type under the current TIFF data and policy.</summary>
    /// <param name="dataType">The data type.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal static bool IsRationalDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Rational:
            case TiffDataType.SRational:
                return true;
            default:
                return false;
        }
    }

    /// <summary>Determines whether is floating data type under the current TIFF data and policy.</summary>
    /// <param name="dataType">The data type.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal static bool IsFloatingDataType(TiffDataType dataType)
    {
        switch (dataType)
        {
            case TiffDataType.Float:
            case TiffDataType.Double:
                return true;
            default:
                return false;
        }
    }

    internal static TiffTagValue CreateBase(int tagId, TiffDataType dataType)
    {
        TiffTagValue tagValue = new TiffTagValue();
        tagValue.TagId = tagId;
        tagValue.DataType = dataType;
        return tagValue;
    }

    internal static ulong[] CopyUnsignedArray(ulong[] values)
    {
        if (values == null)
        {
            return new ulong[0];
        }

        ulong[] copy = new ulong[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static long[] CopySignedArray(long[] values)
    {
        if (values == null)
        {
            return new long[0];
        }

        long[] copy = new long[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static TiffRational[] CopyRationalArray(TiffRational[] values)
    {
        if (values == null)
        {
            return new TiffRational[0];
        }

        TiffRational[] copy = new TiffRational[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            if (values[index] == null)
            {
                copy[index] = new TiffRational();
            }
            else
            {
                copy[index] = values[index].Copy();
            }

            index = index + 1;
        }

        return copy;
    }

    internal static double[] CopyFloatingArray(double[] values)
    {
        if (values == null)
        {
            return new double[0];
        }

        double[] copy = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }

    internal static byte[] CopyByteArray(byte[] values)
    {
        if (values == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
