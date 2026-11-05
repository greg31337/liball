using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a TNEF property.
/// </summary>
public class EmailTnefProperty : IEmailTnefPropertyContract
{
    int typeCode;
    int propertyId;
    EmailTnefNamedPropertyKind namedKind;
    string? namedPropertySet;
    long namedNumericId;
    string? namedStringId;
    List<byte[]> values;
    List<string?> textValues;
    List<long> integerValues;
    List<bool> hasIntegerValues;
    byte[] rawEncoding;
    EmailRange sourceRange;
    bool complete;
    /// <summary>
    /// Creates a new TNEF property.
    /// </summary>
    /// <param name = "typeCode">The type code used by the operation.</param>
    /// <param name = "propertyId">The property ID used by the operation.</param>
    /// <param name = "namedKind">The named kind used by the operation.</param>
    /// <param name = "namedPropertySet">The named property set used by the operation.</param>
    /// <param name = "namedNumericId">The named numeric ID used by the operation.</param>
    /// <param name = "namedStringId">The named string ID used by the operation.</param>
    /// <param name = "values">The values used by the operation.</param>
    /// <param name = "textValues">The text values used by the operation.</param>
    /// <param name = "integerValues">The integer values used by the operation.</param>
    /// <param name = "hasIntegerValues">The has integer values used by the operation.</param>
    /// <param name = "rawEncoding">The raw encoding used by the operation.</param>
    /// <param name = "sourceRange">The source range used by the operation.</param>
    /// <param name = "complete">Whether to enable the complete behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailTnefProperty(int typeCode, int propertyId, EmailTnefNamedPropertyKind namedKind, string? namedPropertySet, long namedNumericId, string? namedStringId, List<byte[]> values, List<string?> textValues, List<long> integerValues, List<bool> hasIntegerValues, byte[] rawEncoding, EmailRange sourceRange, bool complete)
    {
        if (typeCode < 0 || typeCode > 65535)
            throw new ArgumentOutOfRangeException("typeCode");
        if (propertyId < 0 || propertyId > 65535)
            throw new ArgumentOutOfRangeException("propertyId");
        if (namedKind < EmailTnefNamedPropertyKind.None || namedKind > EmailTnefNamedPropertyKind.String)
            throw new ArgumentOutOfRangeException("namedKind");
        if (values == null)
            throw new ArgumentNullException("values");
        if (textValues == null)
            throw new ArgumentNullException("textValues");
        if (integerValues == null)
            throw new ArgumentNullException("integerValues");
        if (hasIntegerValues == null)
            throw new ArgumentNullException("hasIntegerValues");
        if (values.Count != textValues.Count || values.Count != integerValues.Count || values.Count != hasIntegerValues.Count)
            throw new ArgumentException("TNEF property value projections must have equal counts.", "values");
        if (rawEncoding == null)
            throw new ArgumentNullException("rawEncoding");
        if (sourceRange == null)
            throw new ArgumentNullException("sourceRange");
        this.typeCode = typeCode;
        this.propertyId = propertyId;
        this.namedKind = namedKind;
        this.namedPropertySet = namedPropertySet;
        this.namedNumericId = namedNumericId;
        this.namedStringId = namedStringId;
        this.values = CopyValues(values);
        this.textValues = new List<string?>(textValues);
        this.integerValues = new List<long>(integerValues);
        this.hasIntegerValues = new List<bool>(hasIntegerValues);
        this.rawEncoding = Copy(rawEncoding);
        this.sourceRange = sourceRange;
        this.complete = complete;
    }

    /// <summary>
    /// Gets the type code.
    /// </summary>
    /// <returns>The current type code.</returns>
    public int GetTypeCode()
    {
        return typeCode;
    }

    /// <summary>
    /// Gets the property ID.
    /// </summary>
    /// <returns>The current property ID.</returns>
    public int GetPropertyId()
    {
        return propertyId;
    }

    /// <summary>
    /// Gets the tag.
    /// </summary>
    /// <returns>The current tag.</returns>
    public uint GetTag()
    {
        return (uint)(propertyId & 65535) << 16 | (uint)(typeCode & 65535);
    }

    /// <summary>
    /// Determines whether the current value or state is multi valued.
    /// </summary>
    /// <returns><c>true</c> when the value or state is multi valued; otherwise, <c>false</c>.</returns>
    public bool IsMultiValued()
    {
        return (typeCode & 4096) != 0;
    }

    /// <summary>
    /// Determines whether the current value or state is named.
    /// </summary>
    /// <returns><c>true</c> when the value or state is named; otherwise, <c>false</c>.</returns>
    public bool IsNamed()
    {
        return namedKind != EmailTnefNamedPropertyKind.None;
    }

    /// <summary>
    /// Gets the named kind.
    /// </summary>
    /// <returns>The current named kind.</returns>
    public EmailTnefNamedPropertyKind GetNamedKind()
    {
        return namedKind;
    }

    /// <summary>
    /// Gets the named property set.
    /// </summary>
    /// <returns>The current named property set.</returns>
    public string? GetNamedPropertySet()
    {
        return namedPropertySet;
    }

    /// <summary>
    /// Gets the named numeric ID.
    /// </summary>
    /// <returns>The current named numeric ID.</returns>
    public long GetNamedNumericId()
    {
        return namedNumericId;
    }

    /// <summary>
    /// Gets the named string ID.
    /// </summary>
    /// <returns>The current named string ID.</returns>
    public string? GetNamedStringId()
    {
        return namedStringId;
    }

    /// <summary>
    /// Gets the value count.
    /// </summary>
    /// <returns>The current value count.</returns>
    public int GetValueCount()
    {
        return values.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public byte[] GetValueAt(int index)
    {
        if (index < 0 || index >= values.Count)
            throw new ArgumentOutOfRangeException("index");
        return Copy(values[index]);
    }

    /// <summary>
    /// Determines whether the text value at is present or available.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns><c>true</c> when the text value at is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public bool HasTextValueAt(int index)
    {
        if (index < 0 || index >= textValues.Count)
            throw new ArgumentOutOfRangeException("index");
        return textValues[index] != null;
    }

    /// <summary>
    /// Gets the text value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested text value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public string? GetTextValueAt(int index)
    {
        if (index < 0 || index >= textValues.Count)
            throw new ArgumentOutOfRangeException("index");
        return textValues[index];
    }

    /// <summary>
    /// Determines whether the integer value at is present or available.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns><c>true</c> when the integer value at is available; otherwise, <c>false</c>.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public bool HasIntegerValueAt(int index)
    {
        if (index < 0 || index >= hasIntegerValues.Count)
            throw new ArgumentOutOfRangeException("index");
        return hasIntegerValues[index];
    }

    /// <summary>
    /// Gets the integer value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested integer value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public long GetIntegerValueAt(int index)
    {
        if (index < 0 || index >= integerValues.Count)
            throw new ArgumentOutOfRangeException("index");
        return integerValues[index];
    }

    /// <summary>
    /// Determines whether the text value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the text value is available; otherwise, <c>false</c>.</returns>
    public bool HasTextValue()
    {
        return values.Count > 0 && textValues[0] != null;
    }

    /// <summary>
    /// Gets the text value.
    /// </summary>
    /// <returns>The current text value.</returns>
    public string? GetTextValue()
    {
        return values.Count == 0 ? null : textValues[0];
    }

    /// <summary>
    /// Determines whether the integer value is present or available.
    /// </summary>
    /// <returns><c>true</c> when the integer value is available; otherwise, <c>false</c>.</returns>
    public bool HasIntegerValue()
    {
        return values.Count > 0 && hasIntegerValues[0];
    }

    /// <summary>
    /// Gets the integer value.
    /// </summary>
    /// <returns>The current integer value.</returns>
    public long GetIntegerValue()
    {
        return values.Count == 0 ? 0 : integerValues[0];
    }

    /// <summary>
    /// Gets the raw encoding.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current raw encoding.</returns>
    public byte[] GetRawEncoding()
    {
        return Copy(rawEncoding);
    }

    /// <summary>
    /// Gets the source range.
    /// </summary>
    /// <returns>The current source range.</returns>
    public EmailRange GetSourceRange()
    {
        return sourceRange;
    }

    /// <summary>
    /// Determines whether the current value or state is complete.
    /// </summary>
    /// <returns><c>true</c> when the value or state is complete; otherwise, <c>false</c>.</returns>
    public bool IsComplete()
    {
        return complete;
    }

    static List<byte[]> CopyValues(List<byte[]> source)
    {
        List<byte[]> result = new List<byte[]>();
        int index = 0;
        while (index < source.Count)
        {
            if (source[index] == null)
                throw new ArgumentException("TNEF property values cannot contain null.", "source");
            result.Add(Copy(source[index]));
            index++;
        }

        return result;
    }

    static byte[] Copy(byte[] source)
    {
        byte[] result = new byte[source.Length];
        source.CopyTo(result, 0);
        return result;
    }
}
