namespace image;

/// <summary>Stores one metadata value in a syntax-independent, explicitly typed form.</summary>
/// <remarks><see cref="Kind"/> selects the payload field to read. Other payload fields retain their defaults and should be ignored.</remarks>
public class MetadataValue
{
    /// <summary>The payload kind and the field that contains the active value.</summary>
    public MetadataValueKind Kind;
    /// <summary>The payload for <see cref="MetadataValueKind.Integer"/> values.</summary>
    public long IntegerValue;
    /// <summary>The payload for <see cref="MetadataValueKind.UnsignedInteger"/> values.</summary>
    public ulong UnsignedIntegerValue;
    /// <summary>The numerator for rational and signed-rational values.</summary>
    public long Numerator;
    /// <summary>The denominator for rational and signed-rational values.</summary>
    public long Denominator;
    /// <summary>The payload for <see cref="MetadataValueKind.Double"/> values.</summary>
    public double DoubleValue;
    /// <summary>The payload for <see cref="MetadataValueKind.IntegerArray"/> values.</summary>
    public long[] IntegerValues;
    /// <summary>The payload for <see cref="MetadataValueKind.UnsignedIntegerArray"/> values.</summary>
    public ulong[] UnsignedIntegerValues;
    /// <summary>The numerators for rational-array and signed-rational-array values.</summary>
    public long[] Numerators;
    /// <summary>The denominators for rational-array and signed-rational-array values.</summary>
    public long[] Denominators;
    /// <summary>The payload for <see cref="MetadataValueKind.DoubleArray"/> values.</summary>
    public double[] DoubleValues;
    /// <summary>The payload for string, date-text, and XML-text values.</summary>
    public string Text;
    /// <summary>The payload for <see cref="MetadataValueKind.StringList"/> values.</summary>
    public string[] TextValues;
    /// <summary>The payload for <see cref="MetadataValueKind.ByteArray"/> values.</summary>
    public byte[] Bytes;
    /// <summary>The payload for <see cref="MetadataValueKind.DirectoryReference"/> values, or null when no directory is assigned.</summary>
    public MetadataDirectory? DirectoryValue;
    /// <summary>The payload for <see cref="MetadataValueKind.Boolean"/> values.</summary>
    public bool BooleanValue;

    /// <summary>Creates an empty value with initialized text, list, and byte-array fields.</summary>
    public MetadataValue()
    {
        Kind = MetadataValueKind.Empty;
        IntegerValue = 0;
        UnsignedIntegerValue = 0;
        Numerator = 0;
        Denominator = 1;
        DoubleValue = 0;
        IntegerValues = new long[0];
        UnsignedIntegerValues = new ulong[0];
        Numerators = new long[0];
        Denominators = new long[0];
        DoubleValues = new double[0];
        Text = "";
        TextValues = new string[0];
        Bytes = new byte[0];
        DirectoryValue = null;
        BooleanValue = false;
    }

    /// <summary>Creates a signed integer metadata value.</summary>
    /// <param name="value">The integer payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.Integer"/>.</returns>
    internal static MetadataValue FromInteger(long value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.Integer;
        result.IntegerValue = value;
        return result;
    }

    /// <summary>Creates an unsigned integer metadata value.</summary>
    /// <param name="value">The unsigned integer payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.UnsignedInteger"/>.</returns>
    internal static MetadataValue FromUnsignedInteger(ulong value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.UnsignedInteger;
        result.UnsignedIntegerValue = value;
        return result;
    }

    /// <summary>Creates a rational metadata value without performing division.</summary>
    /// <remarks>The numerator and denominator are preserved exactly; a zero denominator is permitted so malformed source data can still be represented.</remarks>
    /// <param name="numerator">The fraction numerator.</param>
    /// <param name="denominator">The fraction denominator.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.Rational"/>.</returns>
    internal static MetadataValue FromRational(long numerator, long denominator)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.Rational;
        result.Numerator = numerator;
        result.Denominator = denominator;
        return result;
    }

    /// <summary>Creates a signed rational metadata value without performing division.</summary>
    /// <remarks>The numerator and denominator are preserved exactly; a zero denominator is permitted so malformed source data can still be represented.</remarks>
    /// <param name="numerator">The signed fraction numerator.</param>
    /// <param name="denominator">The signed fraction denominator.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.SignedRational"/>.</returns>
    internal static MetadataValue FromSignedRational(long numerator, long denominator)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.SignedRational;
        result.Numerator = numerator;
        result.Denominator = denominator;
        return result;
    }

    /// <summary>Creates a double-precision floating-point metadata value.</summary>
    /// <param name="value">The floating-point payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.Double"/>.</returns>
    internal static MetadataValue FromDouble(double value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.Double;
        result.DoubleValue = value;
        return result;
    }

    internal static MetadataValue FromIntegerArray(long[] value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.IntegerArray;
        result.IntegerValues = value == null ? new long[0] : value;
        return result;
    }

    internal static MetadataValue FromUnsignedIntegerArray(ulong[] value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.UnsignedIntegerArray;
        result.UnsignedIntegerValues = value == null ? new ulong[0] : value;
        return result;
    }

    internal static MetadataValue FromRationalArray(long[] numerators, long[] denominators)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.RationalArray;
        result.Numerators = numerators == null ? new long[0] : numerators;
        result.Denominators = denominators == null ? new long[0] : denominators;
        return result;
    }

    internal static MetadataValue FromSignedRationalArray(long[] numerators, long[] denominators)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.SignedRationalArray;
        result.Numerators = numerators == null ? new long[0] : numerators;
        result.Denominators = denominators == null ? new long[0] : denominators;
        return result;
    }

    internal static MetadataValue FromDoubleArray(double[] value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.DoubleArray;
        result.DoubleValues = value == null ? new double[0] : value;
        return result;
    }

    /// <summary>Creates a plain text metadata value.</summary>
    /// <param name="value">The text payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.String"/>.</returns>
    internal static MetadataValue FromString(string value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.String;
        result.Text = value;
        return result;
    }

    internal static MetadataValue FromStringList(string[] value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.StringList;
        result.TextValues = value == null ? new string[0] : value;
        return result;
    }

    /// <summary>Creates a date-text value while preserving the source representation.</summary>
    /// <param name="value">The date or date-time text exactly as decoded.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.DateText"/>.</returns>
    internal static MetadataValue FromDateText(string value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.DateText;
        result.Text = value;
        return result;
    }

    /// <summary>Creates an XML-text value while preserving the decoded packet text.</summary>
    /// <param name="value">The XML text payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.XmlText"/>.</returns>
    internal static MetadataValue FromXmlText(string value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.XmlText;
        result.Text = value;
        return result;
    }

    /// <summary>Creates a byte-array metadata value.</summary>
    /// <remarks>The supplied array is referenced directly and is not copied.</remarks>
    /// <param name="value">The binary payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.ByteArray"/>.</returns>
    internal static MetadataValue FromBytes(byte[] value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.ByteArray;
        result.Bytes = value;
        return result;
    }

    /// <summary>Creates a reference to a nested metadata directory.</summary>
    /// <param name="value">The nested directory.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.DirectoryReference"/>.</returns>
    internal static MetadataValue FromDirectory(MetadataDirectory value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.DirectoryReference;
        result.DirectoryValue = value;
        return result;
    }

    /// <summary>Creates a Boolean metadata value.</summary>
    /// <param name="value">The Boolean payload.</param>
    /// <returns>A new value whose kind is <see cref="MetadataValueKind.Boolean"/>.</returns>
    internal static MetadataValue FromBoolean(bool value)
    {
        MetadataValue result = new MetadataValue();
        result.Kind = MetadataValueKind.Boolean;
        result.BooleanValue = value;
        return result;
    }

    internal void AddTextValue(string value)
    {
        int length = TextValues == null ? 0 : TextValues.Length;
        string[] result = new string[length + 1];
        int index = 0;
        while (TextValues != null && index < length)
        {
            result[index] = TextValues[index];
            index = index + 1;
        }

        result[length] = value;
        TextValues = result;
    }
}
