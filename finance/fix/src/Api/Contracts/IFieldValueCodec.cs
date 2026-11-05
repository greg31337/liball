using System;

namespace fix;

/// <summary>Parses and formats individual FIX field values without retaining caller spans.</summary>
public interface IFieldValueCodec
{
    /// <summary>Parses one raw field value according to its FIX datatype.</summary>
    /// <param name="source">The field value bytes without tag, equals sign, or delimiter.</param>
    /// <param name="tag">The field tag associated with the value.</param>
    /// <param name="fieldType">The FIX datatype to parse.</param>
    /// <param name="options">The decode limits and text encoding.</param>
    /// <returns>The explicit field parse outcome.</returns>
    FieldParseResult DecodeFieldValue(ReadOnlySpan<byte> source, int tag, FixFieldType fieldType, DecodeOptions options);
    /// <summary>Formats one field value into caller-owned storage.</summary>
    /// <param name="field">The field carrying the value bytes.</param>
    /// <param name="fieldType">The FIX datatype to format.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The encode limits and text encoding.</param>
    /// <returns>The explicit encode outcome.</returns>
    EncodeResult EncodeFieldValue(FixField field, FixFieldType fieldType, Span<byte> destination, EncodeOptions options);
}
