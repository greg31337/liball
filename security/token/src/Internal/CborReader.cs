using System;
using System.Collections.Generic;
using System.Text;

namespace token;

/// <summary>
/// Reads CBOR values sequentially from a byte array.
/// </summary>
internal class CborReader
{
    byte[] buffer;
    int maximumLength;
    int maximumDepth;
    int position;
    bool limitExceeded;

    /// <summary>
    /// Initializes a reader with maximum accepted encoded length and nesting depth limits.
    /// </summary>
    /// <param name="buffer">The CBOR bytes to read.</param>
    /// <param name="maximumLength">The maximum accepted encoded length.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a limit is negative.</exception>
    internal CborReader(byte[] buffer, int maximumLength, int maximumDepth)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        this.buffer = buffer;
        this.maximumLength = maximumLength;
        this.maximumDepth = maximumDepth;
        position = 0;
        limitExceeded = buffer.Length > maximumLength;
    }

    /// <summary>
    /// Creates a reader that accepts the entire supplied byte array with a maximum nesting depth of 64.
    /// </summary>
    /// <param name="buffer">The CBOR bytes to read.</param>
    /// <returns>A reader configured with default limits.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> is null.</exception>
    internal static CborReader Create(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return new CborReader(buffer, buffer.Length, 64);
    }

    /// <summary>
    /// Creates a reader with a maximum accepted encoded length and a maximum nesting depth of 64.
    /// </summary>
    /// <param name="buffer">The CBOR bytes to read.</param>
    /// <param name="maximumLength">The maximum accepted encoded length.</param>
    /// <returns>A reader configured with the supplied length limit.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumLength</c> is negative.</exception>
    internal static CborReader Create(byte[] buffer, int maximumLength)
    {
        return new CborReader(buffer, maximumLength, 64);
    }

    /// <summary>
    /// Creates a reader with maximum accepted encoded length and nesting depth limits.
    /// </summary>
    /// <param name="buffer">The CBOR bytes to read.</param>
    /// <param name="maximumLength">The maximum accepted encoded length.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>A reader configured with the supplied limits.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>buffer</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when a limit is negative.</exception>
    internal static CborReader Create(byte[] buffer, int maximumLength, int maximumDepth)
    {
        return new CborReader(buffer, maximumLength, maximumDepth);
    }

    /// <summary>
    /// Returns the position of the next unread byte.
    /// </summary>
    /// <returns>The zero-based byte position.</returns>
    internal int GetPosition()
    {
        return position;
    }

    /// <summary>
    /// Returns the number of unread bytes.
    /// </summary>
    /// <returns>The remaining encoded byte count.</returns>
    internal int GetRemainingLength()
    {
        return buffer.Length - position;
    }

    /// <summary>
    /// Reads the next CBOR value.
    /// </summary>
    /// <returns>A result containing the decoded item, or failure details.</returns>
    internal CborReadResult Read()
    {
        return ReadItem(0);
    }

    CborReadResult ReadItem(int depth)
    {
        if (limitExceeded)
        {
            return Failure(
                "cbor_size_exceeded",
                "The CBOR input exceeds the configured maximum length.",
                maximumLength);
        }

        if (position >= buffer.Length)
        {
            return Failure("unexpected_end_of_cbor", "A CBOR item is required.", position);
        }

        int start = position;
        byte initial = buffer[start];
        int majorType = initial >> 5;
        int additionalInformation = initial & 31;

        if (majorType == 0 || majorType == 1)
        {
            return ReadInteger(start, majorType, additionalInformation);
        }

        if (majorType == 2 || majorType == 3)
        {
            return ReadString(start, majorType, additionalInformation);
        }

        if (majorType == 4)
        {
            return ReadArray(start, additionalInformation, depth);
        }

        if (majorType == 5)
        {
            return ReadMap(start, additionalInformation, depth);
        }

        if (majorType == 6)
        {
            return ReadTag(start, additionalInformation, depth);
        }

        if (majorType == 7)
        {
            return ReadSimpleOrFloat(start, additionalInformation);
        }

        return Failure("invalid_cbor", "The CBOR major type is invalid.", start);
    }

    CborReadResult ReadArray(int start, int additionalInformation, int depth)
    {
        CborReadResult? depthFailure = CheckDepth(start, depth);
        if (depthFailure != null)
        {
            return depthFailure;
        }

        CborArgumentResult count = ReadArgument(buffer, start + 1, additionalInformation);
        if (!count.Success)
        {
            return Failure((count.Error ?? throw new InvalidOperationException("A required value is null.")).Code, count.Error.Message, count.Error.Position);
        }

        if (!count.Indefinite && (count.Value > int.MaxValue || count.Value > (ulong)(buffer.Length - count.NextPosition)))
        {
            return Failure("unexpected_end_of_cbor", "The CBOR array has more items than the available input.", buffer.Length);
        }

        List<CborItem> items = new List<CborItem>();
        int current = count.NextPosition;
        ulong itemIndex = 0;
        while (count.Indefinite || itemIndex < count.Value)
        {
            if (current >= buffer.Length)
            {
                position = start;
                return Failure("unexpected_end_of_cbor", "The CBOR array is truncated.", buffer.Length);
            }

            if (count.Indefinite && buffer[current] == 255)
            {
                current += 1;
                break;
            }

            position = current;
            CborReadResult child = ReadItem(depth + 1);
            if (!child.Success)
            {
                position = start;
                return child;
            }

            items.Add((child.Item ?? throw new InvalidOperationException("A required value is null.")));
            current = position;
            itemIndex += 1;
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Array;
        item.ArrayValue = items.ToArray();
        position = current;
        return Success(item);
    }

    CborReadResult ReadMap(int start, int additionalInformation, int depth)
    {
        CborReadResult? depthFailure = CheckDepth(start, depth);
        if (depthFailure != null)
        {
            return depthFailure;
        }

        CborArgumentResult count = ReadArgument(buffer, start + 1, additionalInformation);
        if (!count.Success)
        {
            return Failure((count.Error ?? throw new InvalidOperationException("A required value is null.")).Code, count.Error.Message, count.Error.Position);
        }

        if (!count.Indefinite && (count.Value > int.MaxValue || count.Value > (ulong)((buffer.Length - count.NextPosition) / 2)))
        {
            return Failure("unexpected_end_of_cbor", "The CBOR map has more entries than the available input.", buffer.Length);
        }

        List<CborMapEntry> entries = new List<CborMapEntry>();
        int current = count.NextPosition;
        ulong entryIndex = 0;
        while (count.Indefinite || entryIndex < count.Value)
        {
            if (current >= buffer.Length)
            {
                position = start;
                return Failure("unexpected_end_of_cbor", "The CBOR map is truncated.", buffer.Length);
            }

            if (count.Indefinite && buffer[current] == 255)
            {
                current += 1;
                break;
            }

            position = current;
            int keyPosition = current;
            CborReadResult key = ReadItem(depth + 1);
            if (!key.Success)
            {
                position = start;
                return key;
            }

            int existingIndex = 0;
            while (existingIndex < entries.Count)
            {
                if (CborMapKeyComparer.AreEqualCore(
                    entries[existingIndex].Key,
                    key.Item,
                    maximumDepth,
                    depth + 1))
                {
                    position = start;
                    return Failure(
                        "duplicate_cbor_map_key",
                        "A CBOR map contains an equivalent duplicate key.",
                        keyPosition);
                }

                existingIndex += 1;
            }

            current = position;
            if (current >= buffer.Length || (count.Indefinite && buffer[current] == 255))
            {
                position = start;
                return Failure("unexpected_end_of_cbor", "A CBOR map value is missing.", current);
            }

            CborReadResult value = ReadItem(depth + 1);
            if (!value.Success)
            {
                position = start;
                return value;
            }

            entries.Add(new CborMapEntry((key.Item ?? throw new InvalidOperationException("A required value is null.")), (value.Item ?? throw new InvalidOperationException("A required value is null."))));
            current = position;
            entryIndex += 1;
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Map;
        item.MapValue = entries.ToArray();
        position = current;
        return Success(item);
    }

    CborReadResult ReadTag(int start, int additionalInformation, int depth)
    {
        CborReadResult? depthFailure = CheckDepth(start, depth);
        if (depthFailure != null)
        {
            return depthFailure;
        }

        CborArgumentResult tag = ReadArgument(buffer, start + 1, additionalInformation);
        if (!tag.Success)
        {
            return Failure((tag.Error ?? throw new InvalidOperationException("A required value is null.")).Code, tag.Error.Message, tag.Error.Position);
        }

        if (tag.Indefinite)
        {
            return Failure("invalid_cbor", "A CBOR tag cannot have indefinite length.", start);
        }

        if (tag.NextPosition >= buffer.Length)
        {
            return Failure("unexpected_end_of_cbor", "A CBOR tag must be followed by one item.", buffer.Length);
        }

        position = tag.NextPosition;
        CborReadResult tagged = ReadItem(depth + 1);
        if (!tagged.Success)
        {
            position = start;
            return tagged;
        }

        CborItem item = new CborItem();
        item.Kind = TokenCborValueKind.Tag;
        item.TagValue = tag.Value;
        item.TaggedValue = tagged.Item;
        return Success(item);
    }

    static CborArgumentResult ReadArgument(byte[] source, int position, int additionalInformation)
    {
        CborArgumentResult result = new CborArgumentResult();

        if (additionalInformation < 24)
        {
            result.Value = (ulong)additionalInformation;
            result.NextPosition = position;
            result.Success = true;
            return result;
        }

        if (additionalInformation == 31)
        {
            result.Indefinite = true;
            result.NextPosition = position;
            result.Success = true;
            return result;
        }

        int byteCount;
        if (additionalInformation == 24)
        {
            byteCount = 1;
        }
        else if (additionalInformation == 25)
        {
            byteCount = 2;
        }
        else if (additionalInformation == 26)
        {
            byteCount = 4;
        }
        else if (additionalInformation == 27)
        {
            byteCount = 8;
        }
        else
        {
            result.Error = new TokenError(
                "invalid_cbor",
                "The CBOR additional information value is reserved.",
                position - 1);
            return result;
        }

        if (byteCount > source.Length - position)
        {
            result.Error = new TokenError(
                "unexpected_end_of_cbor",
                "The CBOR argument is truncated.",
                source.Length);
            return result;
        }

        ulong value = 0;
        int index = 0;
        while (index < byteCount)
        {
            value = (value << 8) | source[position + index];
            index += 1;
        }

        result.Value = value;
        result.NextPosition = position + byteCount;
        result.Success = true;
        return result;
    }

    CborReadResult ReadInteger(int start, int majorType, int additionalInformation)
    {
        CborArgumentResult argument = ReadArgument(buffer, start + 1, additionalInformation);
        if (!argument.Success)
        {
            return Failure((argument.Error ?? throw new InvalidOperationException("A required value is null.")).Code, argument.Error.Message, argument.Error.Position);
        }

        if (argument.Indefinite)
        {
            return Failure("invalid_cbor", "CBOR integers cannot have indefinite length.", start);
        }

        CborItem item = new CborItem();
        item.Kind = majorType == 0 ? TokenCborValueKind.UnsignedInteger : TokenCborValueKind.NegativeInteger;
        item.IntegerArgument = argument.Value;
        position = argument.NextPosition;
        return Success(item);
    }

    CborReadResult ReadString(int start, int majorType, int additionalInformation)
    {
        CborArgumentResult argument = ReadArgument(buffer, start + 1, additionalInformation);
        if (!argument.Success)
        {
            return Failure((argument.Error ?? throw new InvalidOperationException("A required value is null.")).Code, argument.Error.Message, argument.Error.Position);
        }

        if (argument.Indefinite)
        {
            return ReadIndefiniteString(start, majorType, argument.NextPosition);
        }

        if (argument.Value > int.MaxValue || argument.Value > (ulong)(buffer.Length - argument.NextPosition))
        {
            int errorPosition = argument.Value > int.MaxValue ? start : buffer.Length;
            return Failure("unexpected_end_of_cbor", "The CBOR string length exceeds the available input.", errorPosition);
        }

        int length = (int)argument.Value;
        CborItem item = new CborItem();
        if (majorType == 2)
        {
            item.Kind = TokenCborValueKind.ByteString;
            item.Bytes = buffer.AsSpan(argument.NextPosition, length).ToArray();
        }
        else
        {
            TokenTextResult decoded = Utf8Codec.Decode(buffer.AsSpan(argument.NextPosition, length));
            if (!decoded.Success)
            {
                return Failure("invalid_cbor_utf8", (decoded.Error ?? throw new InvalidOperationException("A required value is null.")).Message, argument.NextPosition + decoded.Error.Position);
            }

            item.Kind = TokenCborValueKind.TextString;
            item.Text = decoded.Text;
        }

        position = argument.NextPosition + length;
        return Success(item);
    }

    CborReadResult ReadIndefiniteString(int start, int majorType, int contentPosition)
    {
        TokenBufferWriter byteWriter = new TokenBufferWriter(buffer.Length);
        StringBuilder textBuilder = new StringBuilder();
        int current = contentPosition;

        while (current < buffer.Length)
        {
            if (buffer[current] == 255)
            {
                CborItem completed = new CborItem();
                if (majorType == 2)
                {
                    completed.Kind = TokenCborValueKind.ByteString;
                    completed.Bytes = byteWriter.ToArray();
                }
                else
                {
                    completed.Kind = TokenCborValueKind.TextString;
                    completed.Text = textBuilder.ToString();
                }

                position = current + 1;
                return Success(completed);
            }

            byte initial = buffer[current];
            int chunkMajorType = initial >> 5;
            int chunkAdditionalInformation = initial & 31;
            if (chunkMajorType != majorType || chunkAdditionalInformation == 31)
            {
                return Failure(
                    "invalid_cbor",
                    "An indefinite-length string must contain definite chunks of the same string type.",
                    current);
            }

            CborArgumentResult length = ReadArgument(buffer, current + 1, chunkAdditionalInformation);
            if (!length.Success)
            {
                return Failure((length.Error ?? throw new InvalidOperationException("A required value is null.")).Code, length.Error.Message, length.Error.Position);
            }

            if (length.Value > int.MaxValue || length.Value > (ulong)(buffer.Length - length.NextPosition))
            {
                return Failure("unexpected_end_of_cbor", "A CBOR string chunk is truncated.", buffer.Length);
            }

            int chunkLength = (int)length.Value;
            if (majorType == 2)
            {
                byteWriter.WriteSpan(buffer.AsSpan(length.NextPosition, chunkLength));
            }
            else
            {
                TokenTextResult decoded = Utf8Codec.Decode(buffer.AsSpan(length.NextPosition, chunkLength));
                if (!decoded.Success)
                {
                    return Failure("invalid_cbor_utf8", (decoded.Error ?? throw new InvalidOperationException("A required value is null.")).Message, length.NextPosition + decoded.Error.Position);
                }

                textBuilder.Append(decoded.Text);
            }

            current = length.NextPosition + chunkLength;
        }

        return Failure("unexpected_end_of_cbor", "An indefinite-length CBOR string is missing its break byte.", buffer.Length);
    }

    CborReadResult ReadSimpleOrFloat(int start, int additionalInformation)
    {
        CborItem item = new CborItem();

        if (additionalInformation < 20)
        {
            item.Kind = TokenCborValueKind.Simple;
            item.SimpleValue = (byte)additionalInformation;
            position = start + 1;
            return Success(item);
        }

        if (additionalInformation == 20 || additionalInformation == 21)
        {
            item.Kind = TokenCborValueKind.Boolean;
            item.BooleanValue = additionalInformation == 21;
            position = start + 1;
            return Success(item);
        }

        if (additionalInformation == 22)
        {
            item.Kind = TokenCborValueKind.Null;
            position = start + 1;
            return Success(item);
        }

        if (additionalInformation == 23)
        {
            item.Kind = TokenCborValueKind.Undefined;
            position = start + 1;
            return Success(item);
        }

        if (additionalInformation == 24)
        {
            if (start + 1 >= buffer.Length)
            {
                return Failure("unexpected_end_of_cbor", "The CBOR simple value is truncated.", buffer.Length);
            }

            item.Kind = TokenCborValueKind.Simple;
            item.SimpleValue = buffer[start + 1];
            position = start + 2;
            return Success(item);
        }

        if (additionalInformation == 25)
        {
            if (start + 2 >= buffer.Length)
            {
                return Failure("unexpected_end_of_cbor", "The CBOR half-precision value is truncated.", buffer.Length);
            }

            ushort bits = (ushort)((buffer[start + 1] << 8) | buffer[start + 2]);
            item.Kind = TokenCborValueKind.FloatingPoint;
            item.FloatingPointValue = (double)BitConverter.UInt16BitsToHalf(bits);
            item.FloatingPointWidth = 2;
            position = start + 3;
            return Success(item);
        }

        if (additionalInformation == 26)
        {
            if (start + 4 >= buffer.Length)
            {
                return Failure("unexpected_end_of_cbor", "The CBOR single-precision value is truncated.", buffer.Length);
            }

            uint bits = ReadUInt32(buffer, start + 1);
            item.Kind = TokenCborValueKind.FloatingPoint;
            item.FloatingPointValue = BitConverter.UInt32BitsToSingle(bits);
            item.FloatingPointWidth = 4;
            position = start + 5;
            return Success(item);
        }

        if (additionalInformation == 27)
        {
            if (start + 8 >= buffer.Length)
            {
                return Failure("unexpected_end_of_cbor", "The CBOR double-precision value is truncated.", buffer.Length);
            }

            ulong bits = ReadUInt64(buffer, start + 1);
            item.Kind = TokenCborValueKind.FloatingPoint;
            item.FloatingPointValue = BitConverter.UInt64BitsToDouble(bits);
            item.FloatingPointWidth = 8;
            position = start + 9;
            return Success(item);
        }

        if (additionalInformation == 31)
        {
            return Failure("invalid_cbor", "A CBOR break byte is not a standalone primitive value.", start);
        }

        return Failure("invalid_cbor", "The CBOR additional information value is reserved.", start);
    }

    static uint ReadUInt32(byte[] source, int position)
    {
        uint value = (uint)source[position] << 24;
        value |= (uint)source[position + 1] << 16;
        value |= (uint)source[position + 2] << 8;
        value |= source[position + 3];
        return value;
    }

    static ulong ReadUInt64(byte[] source, int position)
    {
        ulong value = 0;
        int index = 0;
        while (index < 8)
        {
            value = (value << 8) | source[position + index];
            index += 1;
        }

        return value;
    }

    static CborReadResult Success(CborItem item)
    {
        CborReadResult result = new CborReadResult();
        result.Item = item;
        result.Success = true;
        return result;
    }

    CborReadResult? CheckDepth(int start, int depth)
    {
        if (depth >= maximumDepth)
        {
            return Failure(
                "cbor_depth_exceeded",
                "The CBOR value exceeds the configured maximum nesting depth.",
                start);
        }

        return null;
    }

    static CborReadResult Failure(string code, string message, int position)
    {
        CborReadResult result = new CborReadResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }
}
