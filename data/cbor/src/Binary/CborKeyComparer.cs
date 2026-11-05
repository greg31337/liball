using System;
using System.Collections.Generic;

namespace cbor;

internal static class CborKeyComparer
{
    internal static bool AreEquivalent(CborValue? left, CborValue? right)
    {
        return CborValueComparer.AreEqual(left, right);
    }

    internal static ulong GetEquivalentHash(CborValue? value)
    {
        CborHashState state = new CborHashState();
        return GetHashCore(value, state);
    }

    internal static int FindEquivalentKeyIndex(List<CborMapEntry> entries, CborValue key)
    {
        ulong keyHash = GetEquivalentHash(key);
        for (int index = 0; index < entries.Count; index++)
        {
            if (GetEquivalentHash(entries[index].Key) == keyHash
                && AreEquivalent(entries[index].Key, key))
            {
                return index;
            }
        }
        return -1;
    }

    internal static int CompareDeterministicEncoding(
        ReadOnlySpan<byte> left,
        ReadOnlySpan<byte> right,
        CborMapKeyOrder order)
    {
        if (order == CborMapKeyOrder.LengthFirst)
        {
            if (left.Length < right.Length)
            {
                return -1;
            }
            if (left.Length > right.Length)
            {
                return 1;
            }
        }
        else if (order != CborMapKeyOrder.Lexicographic)
        {
            throw new CborEncodeException(
                "CBOR_MAP_KEY_ORDER_INVALID",
                "Deterministic encoded-key comparison requires lexical or length-first ordering.",
                -1,
                "$"
            );
        }

        int commonLength = left.Length;
        if (right.Length < commonLength)
        {
            commonLength = right.Length;
        }
        for (int index = 0; index < commonLength; index++)
        {
            if (left[index] < right[index])
            {
                return -1;
            }
            if (left[index] > right[index])
            {
                return 1;
            }
        }
        if (left.Length < right.Length)
        {
            return -1;
        }
        if (left.Length > right.Length)
        {
            return 1;
        }
        return 0;
    }

    internal static ulong GetHashCore(CborValue? value, CborHashState state)
    {
        if (value == null)
        {
            return 0x6a09e667f3bcc909UL;
        }
        if (ContainsActive(value, state))
        {
            return 0xbb67ae8584caa73bUL ^ GetKindHash(value.Kind);
        }
        if (state.Depth >= 512)
        {
            return 0xa4093822299f31d0UL ^ GetKindHash(value.Kind);
        }

        state.ActiveValues.Add(value);
        state.Depth = state.Depth + 1;
        ulong hash = MixHash(0x3c6ef372fe94f82bUL, GetKindHash(value.Kind));
        if (value.ReferencedValue != null)
        {
            hash = MixHash(hash, GetHashCore(value.ReferencedValue, state));
        }

        switch (value.Kind)
        {
            case CborValueKind.None:
                break;
            case CborValueKind.UnsignedInteger:
            case CborValueKind.NegativeInteger:
                hash = MixHash(hash, HashBytes(value.IntegerValue.ToByteArray()));
                break;
            case CborValueKind.ByteString:
                hash = MixHash(hash, HashBytes(value.ByteStringValue));
                break;
            case CborValueKind.TextString:
                hash = MixHash(hash, HashText(value.TextStringValue));
                break;
            case CborValueKind.Array:
                hash = MixHash(hash, HashArray(value.ArrayItems, state));
                break;
            case CborValueKind.Map:
                hash = MixHash(hash, HashMap(value.MapEntries, state));
                break;
            case CborValueKind.Tag:
                hash = MixHash(hash, value.TagNumber);
                hash = MixHash(hash, GetHashCore(value.TaggedValue, state));
                break;
            case CborValueKind.Float:
                hash = MixHash(hash, HashFloat(value.FloatValue));
                break;
            case CborValueKind.SimpleValue:
                hash = MixHash(hash, value.SimpleValue);
                break;
            default:
                hash = MixHash(hash, 0xffffffffffffffffUL);
                break;
        }

        state.Depth = state.Depth - 1;
        state.ActiveValues.RemoveAt(state.ActiveValues.Count - 1);
        return hash;
    }

    internal static ulong HashBytes(byte[]? value)
    {
        if (value == null)
        {
            return 0xa54ff53a5f1d36f1UL;
        }
        ulong hash = MixHash(0x510e527fade682d1UL, (ulong)value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            hash = MixHash(hash, value[index]);
        }
        return hash;
    }

    internal static ulong HashText(string? value)
    {
        if (value == null)
        {
            return 0x9b05688c2b3e6c1fUL;
        }
        ulong hash = MixHash(0x1f83d9abfb41bd6bUL, (ulong)value.Length);
        for (int index = 0; index < value.Length; index++)
        {
            hash = MixHash(hash, value[index]);
        }
        return hash;
    }

    internal static ulong HashArray(List<CborValue>? values, CborHashState state)
    {
        if (values == null)
        {
            return 0x5be0cd19137e2179UL;
        }
        ulong hash = MixHash(0xcbbb9d5dc1059ed8UL, (ulong)values.Count);
        for (int index = 0; index < values.Count; index++)
        {
            hash = MixHash(hash, GetHashCore(values[index], state));
        }
        return hash;
    }

    internal static ulong HashMap(List<CborMapEntry>? entries, CborHashState state)
    {
        if (entries == null)
        {
            return 0x629a292a367cd507UL;
        }
        ulong unorderedHash = 0;
        for (int index = 0; index < entries.Count; index++)
        {
            ulong pairHash = MixHash(
                GetHashCore(entries[index].Key, state),
                GetHashCore(entries[index].Value, state));
            unorderedHash = unorderedHash ^ RotateHash(pairHash);
        }
        return MixHash(unorderedHash, (ulong)entries.Count);
    }

    internal static ulong HashFloat(CborFloatValue? value)
    {
        if (value == null)
        {
            return 0x9159015a3070dd17UL;
        }
        if (CborValueComparer.IsNaN(value))
        {
            return 0x152fecd8f70e5939UL ^ CborValueComparer.GetNaNSignificand(value);
        }
        if (value.Value == 0.0)
        {
            return 0;
        }
        return CborHalfPrecision.ConvertDoubleToDoubleBits(value.Value);
    }

    internal static ulong MixHash(ulong current, ulong value)
    {
        return RotateHash(current) ^ value ^ 0x9e3779b97f4a7c15UL;
    }

    internal static ulong RotateHash(ulong value)
    {
        return (value << 17) | (value >> 47);
    }

    internal static ulong GetKindHash(CborValueKind kind)
    {
        switch (kind)
        {
            case CborValueKind.None:
                return 0;
            case CborValueKind.UnsignedInteger:
                return 1;
            case CborValueKind.NegativeInteger:
                return 2;
            case CborValueKind.ByteString:
                return 3;
            case CborValueKind.TextString:
                return 4;
            case CborValueKind.Array:
                return 5;
            case CborValueKind.Map:
                return 6;
            case CborValueKind.Tag:
                return 7;
            case CborValueKind.Float:
                return 8;
            case CborValueKind.SimpleValue:
                return 9;
            default:
                return 0xffffffffffffffffUL;
        }
    }

    internal static bool ContainsActive(CborValue value, CborHashState state)
    {
        for (int index = 0; index < state.ActiveValues.Count; index++)
        {
            if (object.ReferenceEquals(value, state.ActiveValues[index]))
            {
                return true;
            }
        }
        return false;
    }
}

internal class CborHashState
{
    internal List<CborValue> ActiveValues = new List<CborValue>();
    internal int Depth;
}
