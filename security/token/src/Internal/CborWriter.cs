using System;
namespace token;

/// <summary>
/// Writes CBOR values using definite lengths and preferred integer encodings.
/// </summary>
internal class CborWriter
{
    TokenBufferWriter writer;
    int maximumDepth;

    /// <summary>
    /// Initializes an empty CBOR writer with encoded length and nesting depth limits.
    /// </summary>
    /// <param name="maximumLength">The maximum number of encoded bytes.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a limit is negative.</exception>
    internal CborWriter(int maximumLength, int maximumDepth)
    {
        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        writer = new TokenBufferWriter(maximumLength);
        this.maximumDepth = maximumDepth;
    }

    /// <summary>
    /// Creates an empty CBOR writer with the largest supported output limit and a maximum nesting depth of 64.
    /// </summary>
    /// <returns>A writer configured with default limits.</returns>
    internal static CborWriter Create()
    {
        return new CborWriter(int.MaxValue, 64);
    }

    /// <summary>
    /// Creates an empty CBOR writer with a maximum encoded length and a maximum nesting depth of 64.
    /// </summary>
    /// <param name="maximumLength">The maximum number of encoded bytes.</param>
    /// <returns>A writer configured with the supplied length limit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumLength</c> is negative.</exception>
    internal static CborWriter Create(int maximumLength)
    {
        return new CborWriter(maximumLength, 64);
    }

    /// <summary>
    /// Creates an empty CBOR writer with encoded length and nesting depth limits.
    /// </summary>
    /// <param name="maximumLength">The maximum number of encoded bytes.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>A writer configured with the supplied limits.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a limit is negative.</exception>
    internal static CborWriter Create(int maximumLength, int maximumDepth)
    {
        return new CborWriter(maximumLength, maximumDepth);
    }

    /// <summary>
    /// Returns the current encoded length.
    /// </summary>
    /// <returns>The number of bytes written.</returns>
    internal int GetLength()
    {
        return writer.GetLength();
    }

    /// <summary>
    /// Returns the remaining capacity under the configured output limit.
    /// </summary>
    /// <returns>The number of bytes that may still be written.</returns>
    internal int GetRemainingCapacity()
    {
        return writer.GetRemainingCapacity();
    }

    /// <summary>
    /// Returns a copy of all encoded bytes.
    /// </summary>
    /// <returns>A new array containing the encoded CBOR values.</returns>
    internal byte[] ToArray()
    {
        return writer.ToArray();
    }

    /// <summary>
    /// Writes an unsigned integer using its preferred CBOR encoding.
    /// </summary>
    /// <param name="value">The unsigned integer value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteUnsignedInteger(ulong value)
    {
        return writer.WriteBytes(EncodeArgument(0, value));
    }

    /// <summary>
    /// Writes a negative integer from the CBOR argument <c>n</c> representing <c>-1-n</c>.
    /// </summary>
    /// <param name="argument">The CBOR negative integer argument.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteNegativeInteger(ulong argument)
    {
        return writer.WriteBytes(EncodeArgument(1, argument));
    }

    /// <summary>
    /// Writes a signed 64-bit integer using its preferred CBOR encoding.
    /// </summary>
    /// <param name="value">The signed integer value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteInt64(long value)
    {
        if (value >= 0)
        {
            return WriteUnsignedInteger((ulong)value);
        }

        ulong argument = (ulong)(-(value + 1));
        return WriteNegativeInteger(argument);
    }

    /// <summary>
    /// Writes a definite-length byte string.
    /// </summary>
    /// <param name="value">The bytes to write.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    internal TokenResult WriteByteString(byte[] value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return WriteByteStringSpan(value.AsSpan());
    }

    /// <summary>
    /// Writes a definite-length byte string from a span.
    /// </summary>
    /// <param name="value">The bytes to write.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteByteStringSpan(ReadOnlySpan<byte> value)
    {
        return WriteString(2, value);
    }

    /// <summary>
    /// Writes a definite-length UTF-8 text string.
    /// </summary>
    /// <param name="value">The text to write.</param>
    /// <returns>A successful result, or failure details for invalid UTF-16 or an exceeded output limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    internal TokenResult WriteTextString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        TokenBytesResult encoded = Utf8Codec.Encode(value);
        if (!encoded.Success)
        {
            TokenResult failure = new TokenResult();
            failure.Error = encoded.Error;
            return failure;
        }

        return WriteString(3, encoded.Bytes);
    }

    /// <summary>
    /// Writes a CBOR boolean value.
    /// </summary>
    /// <param name="value">The boolean value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteBoolean(bool value)
    {
        return writer.Write(value ? (byte)245 : (byte)244);
    }

    /// <summary>
    /// Writes a CBOR null value.
    /// </summary>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteNull()
    {
        return writer.Write((byte)246);
    }

    /// <summary>
    /// Writes a CBOR undefined value.
    /// </summary>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteUndefined()
    {
        return writer.Write((byte)247);
    }

    /// <summary>
    /// Writes an unassigned or application-defined CBOR simple value.
    /// </summary>
    /// <param name="value">The simple value other than 20 through 23.</param>
    /// <returns>A successful result, or failure details for a reserved semantic value or an exceeded output limit.</returns>
    internal TokenResult WriteSimple(byte value)
    {
        if (value >= 20 && value <= 23)
        {
            TokenResult invalid = new TokenResult();
            invalid.Error = new TokenError(
                "invalid_cbor_simple",
                "Simple values 20 through 23 must be written with their dedicated methods.",
                writer.GetLength());
            return invalid;
        }

        if (value < 20)
        {
            return writer.Write((byte)(224 | value));
        }

        return writer.WriteBytes(new byte[] { 248, value });
    }

    /// <summary>
    /// Writes a half-precision floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteHalf(Half value)
    {
        ushort bits = BitConverter.HalfToUInt16Bits(value);
        byte[] encoded = new byte[3];
        encoded[0] = 249;
        encoded[1] = (byte)(bits >> 8);
        encoded[2] = (byte)(bits & 255);
        return writer.WriteBytes(encoded);
    }

    /// <summary>
    /// Writes a single-precision floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteSingle(float value)
    {
        uint bits = BitConverter.SingleToUInt32Bits(value);
        byte[] encoded = new byte[5];
        encoded[0] = 250;
        WriteUInt32(encoded.AsSpan(), 1, bits);
        return writer.WriteBytes(encoded);
    }

    /// <summary>
    /// Writes a double-precision floating-point value.
    /// </summary>
    /// <param name="value">The floating-point value.</param>
    /// <returns>A successful result, or failure details when the output limit would be exceeded.</returns>
    internal TokenResult WriteDouble(double value)
    {
        ulong bits = BitConverter.DoubleToUInt64Bits(value);
        byte[] encoded = new byte[9];
        encoded[0] = 251;
        WriteUInt64(encoded.AsSpan(), 1, bits);
        return writer.WriteBytes(encoded);
    }

    /// <summary>
    /// Writes a CBOR item, including nested arrays, maps, and tags.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <returns>A successful result, or failure details for an invalid, overly deep, or oversized item.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    internal TokenResult WriteItem(CborItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        CborWriter temporary = CborWriter.Create(writer.GetRemainingCapacity(), maximumDepth);
        TokenResult encoded = WriteItemCore(temporary, item, false, 0);
        if (!encoded.Success)
        {
            return encoded;
        }

        return writer.WriteBytes(temporary.ToArray());
    }

    /// <summary>
    /// Writes a CBOR item with every map recursively ordered by deterministic encoded key order.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <returns>A successful result, or failure details for an invalid, overly deep, or oversized item.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    internal TokenResult WriteDeterministicItem(CborItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        TokenBytesResult encoded = EncodeDeterministicItem(
            item,
            writer.GetRemainingCapacity(),
            maximumDepth,
            0);
        if (!encoded.Success)
        {
            TokenResult failure = new TokenResult();
            failure.Error = encoded.Error;
            return failure;
        }

        return writer.WriteBytes((encoded.Bytes ?? throw new InvalidOperationException("A required value is null.")));
    }

    /// <summary>
    /// Writes a definite-length CBOR array in supplied item order.
    /// </summary>
    /// <param name="items">The array items.</param>
    /// <returns>A successful result, or failure details for invalid items or an exceeded output limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>items</c> is null.</exception>
    internal TokenResult WriteArray(CborItem[] items)
    {
        if (items == null)
        {
            throw new ArgumentNullException("items");
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = items;
        return WriteItem(item);
    }

    /// <summary>
    /// Writes a definite-length CBOR map in supplied entry order.
    /// </summary>
    /// <param name="entries">The map entries.</param>
    /// <returns>A successful result, or failure details for invalid entries or an exceeded output limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    internal TokenResult WriteMap(CborMapEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return WriteItem(item);
    }

    /// <summary>
    /// Writes a definite-length CBOR map in deterministic encoded key order.
    /// </summary>
    /// <param name="entries">The map entries to order and write without modifying the input array.</param>
    /// <returns>A successful result, or failure details for invalid entries or an exceeded output limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    internal TokenResult WriteDeterministicMap(CborMapEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries;
        return WriteDeterministicItem(item);
    }

    /// <summary>
    /// Writes a CBOR tag followed by one item.
    /// </summary>
    /// <param name="tag">The tag number.</param>
    /// <param name="item">The tagged item.</param>
    /// <returns>A successful result, or failure details for an invalid item or exceeded output limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>item</c> is null.</exception>
    internal TokenResult WriteTag(ulong tag, CborItem item)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        CborItem tagged = new CborItem();
        tagged.Kind = TokenCborValueKind.Tag;
        tagged.TagValue = tag;
        tagged.TaggedValue = item;
        return WriteItem(tagged);
    }

    internal static TokenBytesResult EncodeDeterministicItem(CborItem item)
    {
        return EncodeDeterministicItem(item, int.MaxValue, 64, 0);
    }

    internal static TokenBytesResult EncodeDeterministicItem(
        CborItem item,
        int maximumLength,
        int maximumDepth,
        int depth)
    {
        TokenBytesResult result = new TokenBytesResult();
        if (item == null)
        {
            result.Error = new TokenError("invalid_cbor_item", "A CBOR item must not be null.", 0);
            return result;
        }

        CborWriter temporary = CborWriter.Create(maximumLength, maximumDepth);
        TokenResult encoded = WriteItemCore(temporary, item, true, depth);
        if (!encoded.Success)
        {
            result.Error = encoded.Error;
            return result;
        }

        result.Bytes = temporary.ToArray();
        result.Success = true;
        return result;
    }

    static TokenResult WriteItemCore(CborWriter destination, CborItem item, bool deterministicMaps, int depth)
    {
        if (item == null)
        {
            return InvalidItem("A CBOR item must not be null.", destination.GetLength());
        }

        switch (item.Kind)
        {
            case TokenCborValueKind.UnsignedInteger:
                return destination.WriteUnsignedInteger(item.IntegerArgument);
            case TokenCborValueKind.NegativeInteger:
                return destination.WriteNegativeInteger(item.IntegerArgument);
            case TokenCborValueKind.ByteString:
                if (item.Bytes == null)
                {
                    return InvalidItem("A CBOR byte string must contain bytes.", destination.GetLength());
                }

                return destination.WriteByteString(item.Bytes);
            case TokenCborValueKind.TextString:
                if (item.Text == null)
                {
                    return InvalidItem("A CBOR text string must contain text.", destination.GetLength());
                }

                return destination.WriteTextString(item.Text);
            case TokenCborValueKind.Boolean:
                return destination.WriteBoolean(item.BooleanValue);
            case TokenCborValueKind.Null:
                return destination.WriteNull();
            case TokenCborValueKind.Undefined:
                return destination.WriteUndefined();
            case TokenCborValueKind.Simple:
                return destination.WriteSimple(item.SimpleValue);
            case TokenCborValueKind.FloatingPoint:
                if (item.FloatingPointWidth == 2)
                {
                    return destination.WriteHalf((Half)item.FloatingPointValue);
                }

                if (item.FloatingPointWidth == 4)
                {
                    return destination.WriteSingle((float)item.FloatingPointValue);
                }

                if (item.FloatingPointWidth == 8)
                {
                    return destination.WriteDouble(item.FloatingPointValue);
                }

                return InvalidItem("A CBOR floating-point width must be two, four, or eight bytes.", destination.GetLength());
            case TokenCborValueKind.Array:
                return WriteArrayCore(destination, item.ArrayValue, deterministicMaps, depth);
            case TokenCborValueKind.Map:
                return WriteMapCore(destination, item.MapValue, deterministicMaps, depth);
            case TokenCborValueKind.Tag:
                TokenResult depthResult = CheckDepth(destination, depth);
                if (!depthResult.Success)
                {
                    return depthResult;
                }

                if (item.TaggedValue == null)
                {
                    return InvalidItem("A CBOR tag must contain a tagged item.", destination.GetLength());
                }

                TokenResult tagResult = destination.writer.WriteBytes(EncodeArgument(6, item.TagValue));
                if (!tagResult.Success)
                {
                    return tagResult;
                }

                return WriteItemCore(destination, item.TaggedValue, deterministicMaps, depth + 1);
            default:
                return InvalidItem("The CBOR item kind is invalid.", destination.GetLength());
        }
    }

    static TokenResult WriteArrayCore(CborWriter destination, CborItem[]? items, bool deterministicMaps, int depth)
    {
        TokenResult depthResult = CheckDepth(destination, depth);
        if (!depthResult.Success)
        {
            return depthResult;
        }

        if (items == null)
        {
            return InvalidItem("A CBOR array must contain an item array.", destination.GetLength());
        }

        byte[] encodedHeader = EncodeArgument(4, (ulong)items.Length);
        TokenResult sizeResult = CheckMinimumContainerLength(destination, encodedHeader, items.Length, 1);
        if (!sizeResult.Success)
        {
            return sizeResult;
        }

        TokenResult header = destination.writer.WriteBytes(encodedHeader);
        if (!header.Success)
        {
            return header;
        }

        int index = 0;
        while (index < items.Length)
        {
            TokenResult itemResult = WriteItemCore(destination, items[index], deterministicMaps, depth + 1);
            if (!itemResult.Success)
            {
                return itemResult;
            }

            index += 1;
        }

        return SuccessfulResult();
    }

    static TokenResult WriteMapCore(CborWriter destination, CborMapEntry[]? entries, bool deterministicMaps, int depth)
    {
        TokenResult depthResult = CheckDepth(destination, depth);
        if (!depthResult.Success)
        {
            return depthResult;
        }

        if (entries == null)
        {
            return InvalidItem("A CBOR map must contain an entry array.", destination.GetLength());
        }

        byte[] encodedHeader = EncodeArgument(5, (ulong)entries.Length);
        TokenResult sizeResult = CheckMinimumContainerLength(destination, encodedHeader, entries.Length, 2);
        if (!sizeResult.Success)
        {
            return sizeResult;
        }

        TokenResult validated = CborMapKeyComparer.ValidateCore(entries, destination.maximumDepth, depth);
        if (!validated.Success)
        {
            return validated;
        }

        CborMapEntry[]? orderedEntries = entries;
        if (deterministicMaps)
        {
            CborMapSortResult sorted = CborMapSorter.SortCore(
                entries,
                destination.GetRemainingCapacity(),
                destination.maximumDepth,
                depth + 1);
            if (!sorted.Success)
            {
                TokenResult failure = new TokenResult();
                failure.Error = sorted.Error;
                return failure;
            }

            orderedEntries = sorted.Entries;
        }

        TokenResult header = destination.writer.WriteBytes(encodedHeader);
        if (!header.Success)
        {
            return header;
        }

        int index = 0;
        while (index < (orderedEntries ?? throw new InvalidOperationException("A required value is null.")).Length)
        {
            CborMapEntry entry = orderedEntries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                return InvalidItem("A CBOR map entry must contain a key and value.", destination.GetLength());
            }

            TokenResult keyResult = WriteItemCore(destination, entry.Key, deterministicMaps, depth + 1);
            if (!keyResult.Success)
            {
                return keyResult;
            }

            TokenResult valueResult = WriteItemCore(destination, entry.Value, deterministicMaps, depth + 1);
            if (!valueResult.Success)
            {
                return valueResult;
            }

            index += 1;
        }

        return SuccessfulResult();
    }

    static TokenResult SuccessfulResult()
    {
        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    static TokenResult CheckDepth(CborWriter destination, int depth)
    {
        if (depth >= destination.maximumDepth)
        {
            TokenResult failure = new TokenResult();
            failure.Error = new TokenError(
                "cbor_depth_exceeded",
                "The CBOR value exceeds the configured maximum nesting depth.",
                destination.GetLength());
            return failure;
        }

        return SuccessfulResult();
    }

    static TokenResult CheckMinimumContainerLength(
        CborWriter destination,
        byte[] encodedHeader,
        int itemCount,
        int encodedItemsPerEntry)
    {
        int remaining = destination.GetRemainingCapacity();
        if (encodedHeader.Length > remaining
            || itemCount > (remaining - encodedHeader.Length) / encodedItemsPerEntry)
        {
            TokenResult failure = new TokenResult();
            failure.Error = new TokenError(
                "buffer_limit_exceeded",
                "The output would exceed the configured maximum length.",
                destination.GetLength());
            return failure;
        }

        return SuccessfulResult();
    }

    static TokenResult InvalidItem(string message, int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError("invalid_cbor_item", message, position);
        return result;
    }

    TokenResult WriteString(int majorType, ReadOnlySpan<byte> value)
    {
        byte[] header = EncodeArgument(majorType, (ulong)value.Length);
        if (header.Length > int.MaxValue - value.Length)
        {
            TokenResult invalid = new TokenResult();
            invalid.Error = new TokenError(
                "buffer_limit_exceeded",
                "The encoded CBOR string exceeds the supported length.",
                writer.GetLength());
            return invalid;
        }

        int encodedLength = header.Length + value.Length;
        if (encodedLength > writer.GetRemainingCapacity())
        {
            TokenResult invalid = new TokenResult();
            invalid.Error = new TokenError(
                "buffer_limit_exceeded",
                "The output would exceed the configured maximum length.",
                writer.GetLength());
            return invalid;
        }

        byte[] encoded = new byte[encodedLength];
        header.AsSpan().CopyTo(encoded);
        value.CopyTo(encoded.AsSpan(header.Length));
        return writer.WriteBytes(encoded);
    }

    static byte[] EncodeArgument(int majorType, ulong value)
    {
        byte prefix = (byte)(majorType << 5);
        byte[] encoded;

        if (value < 24)
        {
            return new byte[] { (byte)(prefix | (byte)value) };
        }

        if (value <= byte.MaxValue)
        {
            return new byte[] { (byte)(prefix | 24), (byte)value };
        }

        if (value <= ushort.MaxValue)
        {
            encoded = new byte[3];
            encoded[0] = (byte)(prefix | 25);
            encoded[1] = (byte)(value >> 8);
            encoded[2] = (byte)(value & 255);
            return encoded;
        }

        if (value <= uint.MaxValue)
        {
            encoded = new byte[5];
            encoded[0] = (byte)(prefix | 26);
            WriteUInt32(encoded.AsSpan(), 1, (uint)value);
            return encoded;
        }

        encoded = new byte[9];
        encoded[0] = (byte)(prefix | 27);
        WriteUInt64(encoded.AsSpan(), 1, value);
        return encoded;
    }

    static void WriteUInt32(Span<byte> destination, int position, uint value)
    {
        destination[position] = (byte)(value >> 24);
        destination[position + 1] = (byte)((value >> 16) & 255);
        destination[position + 2] = (byte)((value >> 8) & 255);
        destination[position + 3] = (byte)(value & 255);
    }

    static void WriteUInt64(Span<byte> destination, int position, ulong value)
    {
        int index = 7;
        while (index >= 0)
        {
            destination[position + index] = (byte)(value & 255);
            value >>= 8;
            index -= 1;
        }
    }
}
