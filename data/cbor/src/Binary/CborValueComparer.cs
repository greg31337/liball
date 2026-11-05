using System;
using System.Collections.Generic;

namespace cbor;

internal static class CborValueComparer
{
    internal static bool AreEqual(CborValue? left, CborValue? right)
    {
        CborComparisonState state = new CborComparisonState();
        return AreEqualCore(left, right, state);
    }

    internal static bool AreEqualCore(CborValue? left, CborValue? right, CborComparisonState state)
    {
        if (object.ReferenceEquals(left, right))
        {
            return true;
        }
        if (left == null || right == null)
        {
            return false;
        }
        if (left.Kind != right.Kind)
        {
            return false;
        }
        int pairStatus = GetPairStatus(left, right, state);
        if (pairStatus > 0)
        {
            return true;
        }
        if (pairStatus < 0)
        {
            return false;
        }
        if (state.Depth >= 512)
        {
            return false;
        }

        int checkpoint = state.LeftValues.Count;
        state.LeftValues.Add(left);
        state.RightValues.Add(right);
        state.Depth = state.Depth + 1;
        bool equal = CompareContent(left, right, state);
        state.Depth = state.Depth - 1;
        if (!equal)
        {
            Rollback(checkpoint, state);
        }
        return equal;
    }

    internal static bool CompareContent(CborValue left, CborValue right, CborComparisonState state)
    {
        if (left.ReferencedValue == null && right.ReferencedValue != null)
        {
            return false;
        }
        if (left.ReferencedValue != null && right.ReferencedValue == null)
        {
            return false;
        }
        if (left.ReferencedValue != null && !AreEqualCore(left.ReferencedValue, right.ReferencedValue, state))
        {
            return false;
        }

        switch (left.Kind)
        {
            case CborValueKind.None:
                return true;
            case CborValueKind.UnsignedInteger:
            case CborValueKind.NegativeInteger:
                return left.IntegerValue == right.IntegerValue;
            case CborValueKind.ByteString:
                return CompareBytes(left.ByteStringValue, right.ByteStringValue);
            case CborValueKind.TextString:
                return left.TextStringValue == right.TextStringValue;
            case CborValueKind.Array:
                return CompareArrays(left.ArrayItems, right.ArrayItems, state);
            case CborValueKind.Map:
                return CompareMaps(left.MapEntries, right.MapEntries, state);
            case CborValueKind.Tag:
                return left.TagNumber == right.TagNumber
                    && AreEqualCore(left.TaggedValue, right.TaggedValue, state);
            case CborValueKind.Float:
                return CompareFloats(left.FloatValue, right.FloatValue);
            case CborValueKind.SimpleValue:
                return left.SimpleValue == right.SimpleValue;
            default:
                return false;
        }
    }

    internal static bool CompareBytes(byte[]? left, byte[]? right)
    {
        if (object.ReferenceEquals(left, right))
        {
            return true;
        }
        if (left == null || right == null || left.Length != right.Length)
        {
            return false;
        }
        for (int index = 0; index < left.Length; index++)
        {
            if (left[index] != right[index])
            {
                return false;
            }
        }
        return true;
    }

    internal static bool CompareArrays(
        List<CborValue>? left,
        List<CborValue>? right,
        CborComparisonState state)
    {
        if (object.ReferenceEquals(left, right))
        {
            return true;
        }
        if (left == null || right == null || left.Count != right.Count)
        {
            return false;
        }
        for (int index = 0; index < left.Count; index++)
        {
            if (!AreEqualCore(left[index], right[index], state))
            {
                return false;
            }
        }
        return true;
    }

    internal static bool CompareMaps(
        List<CborMapEntry>? left,
        List<CborMapEntry>? right,
        CborComparisonState state)
    {
        if (object.ReferenceEquals(left, right))
        {
            return true;
        }
        if (left == null || right == null || left.Count != right.Count)
        {
            return false;
        }

        bool[] used = new bool[right.Count];
        for (int leftIndex = 0; leftIndex < left.Count; leftIndex++)
        {
            bool found = false;
            for (int rightIndex = 0; rightIndex < right.Count; rightIndex++)
            {
                if (used[rightIndex])
                {
                    continue;
                }
                int checkpoint = state.LeftValues.Count;
                bool keysEqual = AreEqualCore(left[leftIndex].Key, right[rightIndex].Key, state);
                bool valuesEqual = false;
                if (keysEqual)
                {
                    valuesEqual = AreEqualCore(left[leftIndex].Value, right[rightIndex].Value, state);
                }
                if (keysEqual && valuesEqual)
                {
                    used[rightIndex] = true;
                    found = true;
                    break;
                }
                Rollback(checkpoint, state);
            }
            if (!found)
            {
                return false;
            }
        }
        return true;
    }

    internal static bool CompareFloats(CborFloatValue? left, CborFloatValue? right)
    {
        if (object.ReferenceEquals(left, right))
        {
            return true;
        }
        if (left == null || right == null)
        {
            return false;
        }
        bool leftIsNaN = IsNaN(left);
        bool rightIsNaN = IsNaN(right);
        if (leftIsNaN || rightIsNaN)
        {
            return leftIsNaN && rightIsNaN
                && GetNaNSignificand(left) == GetNaNSignificand(right);
        }
        return left.Value == right.Value;
    }

    internal static bool IsNaN(CborFloatValue value)
    {
        switch (value.Width)
        {
            case CborFloatWidth.Half:
                if (value.RawBits <= ushort.MaxValue)
                {
                    return CborHalfPrecision.IsHalfNaN((ushort)value.RawBits);
                }
                return double.IsNaN(value.Value);
            case CborFloatWidth.Single:
                if (value.RawBits <= uint.MaxValue)
                {
                    return CborHalfPrecision.IsSingleNaN((uint)value.RawBits);
                }
                return double.IsNaN(value.Value);
            case CborFloatWidth.Double:
                return CborHalfPrecision.IsDoubleNaN(value.RawBits);
            default:
                return double.IsNaN(value.Value);
        }
    }

    internal static ulong GetNaNSignificand(CborFloatValue value)
    {
        switch (value.Width)
        {
            case CborFloatWidth.Half:
                if (value.RawBits > ushort.MaxValue)
                {
                    return (CborHalfPrecision.ConvertDoubleToDoubleBits(value.Value)
                        & 0x000fffffffffffffUL) << 12;
                }
                return (value.RawBits & 0x03ffUL) << 54;
            case CborFloatWidth.Single:
                if (value.RawBits > uint.MaxValue)
                {
                    return (CborHalfPrecision.ConvertDoubleToDoubleBits(value.Value)
                        & 0x000fffffffffffffUL) << 12;
                }
                return (value.RawBits & 0x007fffffUL) << 41;
            case CborFloatWidth.Double:
                return (value.RawBits & 0x000fffffffffffffUL) << 12;
            default:
                return (CborHalfPrecision.ConvertDoubleToDoubleBits(value.Value)
                    & 0x000fffffffffffffUL) << 12;
        }
    }

    internal static int GetPairStatus(CborValue left, CborValue right, CborComparisonState state)
    {
        for (int index = 0; index < state.LeftValues.Count; index++)
        {
            bool leftMatches = object.ReferenceEquals(left, state.LeftValues[index]);
            bool rightMatches = object.ReferenceEquals(right, state.RightValues[index]);
            if (leftMatches && rightMatches)
            {
                return 1;
            }
            if (leftMatches || rightMatches)
            {
                return -1;
            }
        }
        return 0;
    }

    internal static void Rollback(int checkpoint, CborComparisonState state)
    {
        while (state.LeftValues.Count > checkpoint)
        {
            int lastIndex = state.LeftValues.Count - 1;
            state.LeftValues.RemoveAt(lastIndex);
            state.RightValues.RemoveAt(lastIndex);
        }
    }
}

internal class CborComparisonState
{
    internal List<CborValue> LeftValues = new List<CborValue>();
    internal List<CborValue> RightValues = new List<CborValue>();
    internal int Depth;
}
