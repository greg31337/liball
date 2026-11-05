namespace json;

/// <summary>Writes stateful UTF-8 JSON tokens to a configured destination.</summary>
public interface IJsonTokenWriter
{
    /// <summary>Writes the beginning of an object.</summary>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteStartObject();

    /// <summary>Writes the end of an object.</summary>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteEndObject();

    /// <summary>Writes the beginning of an array.</summary>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteStartArray();

    /// <summary>Writes the end of an array.</summary>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteEndArray();

    /// <summary>Writes an object property name.</summary>
    /// <param name="name">The decoded property name.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WritePropertyName(string name);

    /// <summary>Writes the JSON null value.</summary>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteNull();

    /// <summary>Writes a JSON boolean value.</summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteBoolean(bool value);

    /// <summary>Writes a JSON string value.</summary>
    /// <param name="value">The decoded string value.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteString(string value);

    /// <summary>Writes a validated exact JSON number lexeme.</summary>
    /// <param name="lexeme">The exact number lexeme.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteNumberLexeme(string lexeme);

    /// <summary>Writes a signed 64-bit number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteInt64(long value);

    /// <summary>Writes an unsigned 64-bit number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteUInt64(ulong value);

    /// <summary>Writes a decimal number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteDecimal(decimal value);

    /// <summary>Writes a finite double-precision number.</summary>
    /// <param name="value">The number to write.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteDouble(double value);

    /// <summary>Writes a validated JSON model value.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteJsonValue(JsonValue value);

    /// <summary>Validates and writes exactly one raw UTF-8 JSON value.</summary>
    /// <param name="utf8">The raw UTF-8 value, which is not retained.</param>
    /// <returns>The write outcome.</returns>
    JsonOperationResult WriteRawValue(global::System.ReadOnlySpan<byte> utf8);

    /// <summary>Flushes pending output without closing the caller-owned destination.</summary>
    /// <returns>The flush outcome.</returns>
    JsonOperationResult FlushWriter();

    /// <summary>Completes and flushes one well-formed output stream.</summary>
    /// <returns>The completion outcome.</returns>
    JsonOperationResult CompleteWriter();

    /// <summary>Gets the number of output bytes pending in the writer.</summary>
    /// <returns>The pending byte count.</returns>
    long GetBytesPending();

    /// <summary>Gets the number of output bytes committed to the destination.</summary>
    /// <returns>The committed byte count.</returns>
    long GetBytesCommitted();

    /// <summary>Resets the writer for independent reuse.</summary>
    void ResetWriter();
}
