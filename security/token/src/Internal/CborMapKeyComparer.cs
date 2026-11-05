using System;
namespace token;

/// <summary>
/// Compares CBOR map keys using RFC 8949 generic data model equivalence.
/// </summary>
internal static class CborMapKeyComparer
{
    /// <summary>
    /// Determines whether two CBOR items are equivalent when used as map keys, with a maximum depth of 64.
    /// </summary>
    /// <param name="left">The first map key.</param>
    /// <param name="right">The second map key.</param>
    /// <returns><c>true</c> when the keys are equivalent; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either key is null.</exception>
    internal static bool AreEqual(CborItem left, CborItem right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        return AreEqual(left, right, 64);
    }

    /// <summary>
    /// Determines whether two CBOR items are equivalent when used as map keys within a depth limit.
    /// </summary>
    /// <param name="left">The first map key.</param>
    /// <param name="right">The second map key.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns><c>true</c> when the keys are equivalent within the limit; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when either key is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static bool AreEqual(CborItem left, CborItem right, int maximumDepth)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        return AreEqualCore(left, right, maximumDepth, 0);
    }

    /// <summary>
    /// Validates that a CBOR map contains no equivalent keys, with a maximum depth of 64.
    /// </summary>
    /// <param name="entries">The map entries to validate.</param>
    /// <returns>A successful result, or duplicate-key or invalid-entry failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    internal static TokenResult Validate(CborMapEntry[] entries)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        return Validate(entries, 64);
    }

    /// <summary>
    /// Validates that a CBOR map contains no equivalent keys within a depth limit.
    /// </summary>
    /// <param name="entries">The map entries to validate.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>A successful result, or duplicate-key or invalid-entry failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>maximumDepth</c> is negative.</exception>
    internal static TokenResult Validate(CborMapEntry[] entries, int maximumDepth)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }

        return ValidateCore(entries, maximumDepth, 0);
    }

    internal static TokenResult ValidateCore(CborMapEntry[] entries, int maximumDepth, int depth)
    {
        int index = 0;
        while (index < entries.Length)
        {
            CborMapEntry entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                return Failure(
                    "invalid_cbor_map",
                    "A CBOR map entry must contain a key and value.",
                    index);
            }

            int previous = 0;
            while (previous < index)
            {
                if (AreEqualCore(entries[previous].Key, entry.Key, maximumDepth, depth + 1))
                {
                    return Failure(
                        "duplicate_cbor_map_key",
                        "A CBOR map contains an equivalent duplicate key.",
                        index);
                }

                previous += 1;
            }

            index += 1;
        }

        TokenResult result = new TokenResult();
        result.Success = true;
        return result;
    }

    internal static bool AreEqualCore(CborItem? left, CborItem? right, int maximumDepth, int depth)
    {
        if (left == null || right == null || left.Kind != right.Kind)
        {
            return false;
        }

        if ((left.Kind == TokenCborValueKind.Array
                || left.Kind == TokenCborValueKind.Map
                || left.Kind == TokenCborValueKind.Tag)
            && depth >= maximumDepth)
        {
            return false;
        }

        switch (left.Kind)
        {
            case TokenCborValueKind.UnsignedInteger:
            case TokenCborValueKind.NegativeInteger:
                return left.IntegerArgument == right.IntegerArgument;
            case TokenCborValueKind.ByteString:
                return BytesEqual(left.Bytes, right.Bytes);
            case TokenCborValueKind.TextString:
                return left.Text != null && right.Text != null && left.Text == right.Text;
            case TokenCborValueKind.Boolean:
                return left.BooleanValue == right.BooleanValue;
            case TokenCborValueKind.Null:
            case TokenCborValueKind.Undefined:
                return true;
            case TokenCborValueKind.Simple:
                return left.SimpleValue == right.SimpleValue;
            case TokenCborValueKind.FloatingPoint:
                return FloatingPointEqual(left, right);
            case TokenCborValueKind.Array:
                return ArraysEqual(left.ArrayValue, right.ArrayValue, maximumDepth, depth);
            case TokenCborValueKind.Map:
                return MapsEqual(left.MapValue, right.MapValue, maximumDepth, depth);
            case TokenCborValueKind.Tag:
                return left.TagValue == right.TagValue
                    && AreEqualCore(left.TaggedValue, right.TaggedValue, maximumDepth, depth + 1);
            default:
                return false;
        }
    }

    static bool BytesEqual(byte[]? left, byte[]? right)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static bool FloatingPointEqual(CborItem left, CborItem right)
    {
        double leftValue = RepresentedFloatingPointValue(left);
        double rightValue = RepresentedFloatingPointValue(right);
        if (double.IsNaN(leftValue) || double.IsNaN(rightValue))
        {
            if (!double.IsNaN(leftValue) || !double.IsNaN(rightValue))
            {
                return false;
            }

            ulong significandMask = 4503599627370495UL;
            ulong leftBits = BitConverter.DoubleToUInt64Bits(leftValue);
            ulong rightBits = BitConverter.DoubleToUInt64Bits(rightValue);
            return (leftBits & significandMask) == (rightBits & significandMask);
        }

        return leftValue == rightValue;
    }

    static double RepresentedFloatingPointValue(CborItem item)
    {
        if (item.FloatingPointWidth == 2)
        {
            return (double)(Half)item.FloatingPointValue;
        }

        if (item.FloatingPointWidth == 4)
        {
            return (double)(float)item.FloatingPointValue;
        }

        return item.FloatingPointValue;
    }

    static bool ArraysEqual(CborItem[]? left, CborItem[]? right, int maximumDepth, int depth)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!AreEqualCore(left[index], right[index], maximumDepth, depth + 1))
            {
                return false;
            }

            index += 1;
        }

        return true;
    }

    static bool MapsEqual(CborMapEntry[]? left, CborMapEntry[]? right, int maximumDepth, int depth)
    {
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }

        bool[] matched = new bool[right.Length];
        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            CborMapEntry leftEntry = left[leftIndex];
            if (leftEntry == null || leftEntry.Key == null || leftEntry.Value == null)
            {
                return false;
            }

            bool found = false;
            int rightIndex = 0;
            while (rightIndex < right.Length && !found)
            {
                CborMapEntry rightEntry = right[rightIndex];
                if (!matched[rightIndex]
                    && rightEntry != null
                    && rightEntry.Key != null
                    && rightEntry.Value != null
                    && AreEqualCore(leftEntry.Key, rightEntry.Key, maximumDepth, depth + 1)
                    && AreEqualCore(leftEntry.Value, rightEntry.Value, maximumDepth, depth + 1))
                {
                    matched[rightIndex] = true;
                    found = true;
                }

                rightIndex += 1;
            }

            if (!found)
            {
                return false;
            }

            leftIndex += 1;
        }

        return true;
    }

    static TokenResult Failure(string code, string message, int position)
    {
        TokenResult result = new TokenResult();
        result.Error = new TokenError(code, message, position);
        return result;
    }
}
