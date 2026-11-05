using System;
using System.Collections.Generic;
using System.Numerics;

namespace xml;

internal class XdmMapOperations
{
    internal XdmFactory Factory;
    internal XdmTypeSystem Types;
    internal XdmSequenceOperations Sequences;

    internal XdmMapOperations()
    {
        Factory = new XdmFactory();
        Types = new XdmTypeSystem();
        Sequences = new XdmSequenceOperations();
    }

    internal XdmMapValue CreateEmptyXdmMap()
    {
        return CreateXdmMap([]);
    }

    internal XdmMapValue CreateXdmMap(XdmMapEntry[] entries)
    {
        Guard.NotNull(entries, "entries");
        ValidateXdmMapEntrySet(entries, true);
        XdmMapEntry[] copiedEntries = new XdmMapEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry entry = entries[index];
            XdmMapEntry copiedEntry = new XdmMapEntry();
            copiedEntry.Key = Factory.FreezeXdmAtomicValue(entry.Key == null ? throw new ArgumentException("A map entry must have a key.", "entries") : entry.Key);
            copiedEntry.Value = Sequences.FreezeXdmSequenceStorage(entry.Value == null ? throw new ArgumentException("A map entry must have a value.", "entries") : entry.Value);
            copiedEntries[index] = copiedEntry;
            index = index + 1;
        }

        XdmMapValue result = new XdmMapValue();
        result.Entries = copiedEntries;
        result.Count = copiedEntries.Length;
        result.IsImmutable = true;
        return result;
    }

    internal void ValidateXdmMapStorage(XdmMapValue map)
    {
        Guard.NotNull(map, "map");
        if (map.Count < 0)
        {
            throw new ArgumentException("An XDM map cannot have a negative count.", "map");
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        if (entries.Length != map.Count)
        {
            throw new ArgumentException("The XDM map storage is inconsistent.", "map");
        }

        ValidateXdmMapEntrySet(entries, false);
    }

    internal bool SameXdmMapKey(XdmAtomicValue left, XdmAtomicValue right)
    {
        Factory.ValidateXdmAtomicValue(left);
        Factory.ValidateXdmAtomicValue(right);
        if (IsXdmStringMapKey(left.Type) && IsXdmStringMapKey(right.Type))
        {
            string leftText = new XdmTypeConverter().ReadXdmAtomicStringValue(left);
            string rightText = new XdmTypeConverter().ReadXdmAtomicStringValue(right);
            return string.Equals(leftText, rightText, StringComparison.Ordinal);
        }

        if (Types.IsXdmNumericAtomicType(left.Type) && Types.IsXdmNumericAtomicType(right.Type))
        {
            return SameXdmNumericMapKey(left, right);
        }

        if (IsXdmDateTimeMapKey(left.Type) && IsXdmDateTimeMapKey(right.Type))
        {
            return SameXdmDateTimeMapKey(left, right);
        }

        if (left.Type == XdmAtomicType.Boolean && right.Type == XdmAtomicType.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (Types.IsXdmDurationDerivedAtomicType(left.Type) && Types.IsXdmDurationDerivedAtomicType(right.Type))
        {
            return new XdmDurationDatatype().EqualXdmDuration(left, right);
        }

        if (left.Type == right.Type && (left.Type == XdmAtomicType.HexBinary || left.Type == XdmAtomicType.Base64Binary))
        {
            return new XdmBinaryDatatype().EqualXdmBinary(left, right);
        }

        if (left.Type == XdmAtomicType.QName && right.Type == XdmAtomicType.QName)
        {
            return new XdmQNameDatatype().EqualXdmQName(left, right);
        }

        if (left.Type == XdmAtomicType.Notation && right.Type == XdmAtomicType.Notation)
        {
            return new XdmQNameDatatype().EqualXdmNotation(left, right);
        }

        return false;
    }

    internal bool ContainsXdmMapKey(XdmMapValue map, XdmAtomicValue key)
    {
        return FindXdmMapKeyIndex(map, key) >= 0;
    }

    internal XdmSequence GetXdmMapValue(XdmMapValue map, XdmAtomicValue key)
    {
        int index = FindXdmMapKeyIndex(map, key);
        if (index < 0)
        {
            return Sequences.CreateEmptyXdmSequence();
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        XdmSequence? value = entries[index].Value;
        return value == null ? throw new ArgumentException("A map entry must have a value.", "map") : value;
    }

    internal XdmSequence GetXdmMapKeys(XdmMapValue map)
    {
        ValidateXdmMapStorage(map);
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        XdmSequenceBuilder builder = new XdmSequenceBuilder();
        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? key = entries[index].Key;
            builder.AddXdmAtomicValue(key == null ? throw new ArgumentException("A map entry must have a key.", "map") : key);
            index = index + 1;
        }

        return builder.BuildImmutableXdmSequence();
    }

    internal XdmMapValue PutXdmMapEntry(XdmMapValue map, XdmAtomicValue key, XdmSequence value)
    {
        ValidateXdmMapStorage(map);
        Factory.ValidateXdmAtomicValue(key);
        Sequences.ValidateXdmSequenceStorage(value);
        int existingIndex = FindXdmMapKeyIndex(map, key);
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        int resultLength = existingIndex < 0 ? entries.Length + 1 : entries.Length;
        XdmMapEntry[] updated = new XdmMapEntry[resultLength];
        int index = 0;
        while (index < resultLength)
        {
            if (index == existingIndex || existingIndex < 0 && index == entries.Length)
            {
                updated[index] = CreateXdmMapEntry(key, value);
            }
            else
            {
                updated[index] = entries[index];
            }

            index = index + 1;
        }

        return CreateXdmMap(updated);
    }

    internal XdmMapValue RemoveXdmMapKeys(XdmMapValue map, XdmAtomicValue[] keys)
    {
        ValidateXdmMapStorage(map);
        Guard.NotNull(keys, "keys");
        int keyIndex = 0;
        while (keyIndex < keys.Length)
        {
            Factory.ValidateXdmAtomicValue(keys[keyIndex]);
            keyIndex = keyIndex + 1;
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        XdmMapEntry[] retained = new XdmMapEntry[entries.Length];
        int count = 0;
        int entryIndex = 0;
        while (entryIndex < entries.Length)
        {
            bool remove = false;
            keyIndex = 0;
            while (keyIndex < keys.Length && !remove)
            {
                XdmAtomicValue? entryKey = entries[entryIndex].Key;
                remove = SameXdmMapKey(entryKey == null ? throw new ArgumentException("A map entry must have a key.", "map") : entryKey, keys[keyIndex]);
                keyIndex = keyIndex + 1;
            }

            if (!remove)
            {
                retained[count] = entries[entryIndex];
                count = count + 1;
            }

            entryIndex = entryIndex + 1;
        }

        XdmMapEntry[] result = new XdmMapEntry[count];
        Array.Copy(retained, result, count);
        return CreateXdmMap(result);
    }

    internal XdmMapEntry CreateXdmMapEntry(XdmAtomicValue key, XdmSequence value)
    {
        Guard.NotNull(key, "key");
        Guard.NotNull(value, "value");
        XdmMapEntry entry = new XdmMapEntry();
        entry.Key = key;
        entry.Value = value;
        return entry;
    }

    internal int FindXdmMapKeyIndex(XdmMapValue map, XdmAtomicValue key)
    {
        Guard.NotNull(map, "map");
        Factory.ValidateXdmAtomicValue(key);
        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        if (map.Count != entries.Length)
        {
            throw new ArgumentException("The XDM map storage is inconsistent.", "map");
        }

        int index = 0;
        while (index < entries.Length)
        {
            XdmAtomicValue? candidate = entries[index].Key;
            if (candidate != null && SameXdmMapKey(candidate, key))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void ValidateXdmMapEntrySet(XdmMapEntry[] entries, bool dynamicErrors)
    {
        Dictionary<string, List<XdmAtomicValue>> keys = new Dictionary<string, List<XdmAtomicValue>>(StringComparer.Ordinal);
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry? entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                throw new ArgumentException("Every map entry must provide a key and value.", "entries");
            }

            Factory.ValidateXdmAtomicValue(entry.Key);
            Sequences.ValidateXdmSequenceStorage(entry.Value);
            string identity = CreateXdmMapKeyIdentity(entry.Key);
            List<XdmAtomicValue>? candidates = keys.ContainsKey(identity) ? keys[identity] : null;
            int earlier = 0;
            while (candidates != null && earlier < candidates.Count)
            {
                if (SameXdmMapKey(candidates[earlier], entry.Key))
                {
                    if (dynamicErrors)
                    {
                        throw CreateXdmMapDynamicError("A map cannot contain two entries with the same key.", "XQDY0137");
                    }

                    throw new ArgumentException("A map cannot contain two entries with the same key.", "entries");
                }

                earlier = earlier + 1;
            }

            if (candidates == null)
            {
                candidates = new List<XdmAtomicValue>();
                keys[identity] = candidates;
            }

            candidates.Add(entry.Key);

            index = index + 1;
        }
    }

    internal string CreateXdmMapKeyIdentity(XdmAtomicValue key)
    {
        Factory.ValidateXdmAtomicValue(key);
        if (IsXdmStringMapKey(key.Type))
        {
            return "string|" + new XdmTypeConverter().ReadXdmAtomicStringValue(key);
        }

        if (Types.IsXdmNumericAtomicType(key.Type))
        {
            return CreateXdmNumericMapKeyIdentity(key);
        }

        if (key.Type == XdmAtomicType.Boolean)
        {
            return key.BooleanValue ? "boolean|true" : "boolean|false";
        }

        if (key.Type == XdmAtomicType.QName || key.Type == XdmAtomicType.Notation)
        {
            XmlName? name = key.NameValue;
            return key.Type.ToString() + "|" + (name == null ? "" : name.NamespaceUri) + "|" + (name == null ? "" : name.LocalName);
        }

        if (key.Type == XdmAtomicType.HexBinary || key.Type == XdmAtomicType.Base64Binary)
        {
            return key.Type.ToString() + "|" + Convert.ToBase64String(key.BinaryValue == null ? [] : key.BinaryValue);
        }

        if (IsXdmDateTimeMapKey(key.Type))
        {
            return "date-time|" + (key.Type == XdmAtomicType.DateTimeStamp ? XdmAtomicType.DateTime.ToString() : key.Type.ToString());
        }

        if (Types.IsXdmDurationDerivedAtomicType(key.Type))
        {
            return "duration";
        }

        return key.Type.ToString();
    }

    internal string CreateXdmNumericMapKeyIdentity(XdmAtomicValue key)
    {
        double floating = ReadXdmMapFloatingValue(key);
        bool floatingType = key.Type == XdmAtomicType.Float || key.Type == XdmAtomicType.Double;
        if (floatingType && double.IsNaN(floating))
        {
            return "numeric|NaN";
        }

        if (floatingType && double.IsInfinity(floating))
        {
            return floating > 0 ? "numeric|INF" : "numeric|-INF";
        }

        XdmDecimalStorage value = ReadXdmMapExactNumericValue(key);
        BigInteger coefficient = value.Coefficient;
        int scale = value.Scale;
        while (scale > 0 && coefficient % 10 == 0)
        {
            coefficient = coefficient / 10;
            scale = scale - 1;
        }

        return "numeric|" + coefficient.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + scale.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal bool IsXdmStringMapKey(XdmAtomicType type)
    {
        return Types.IsXdmStringDerivedAtomicType(type) || type == XdmAtomicType.UntypedAtomic || type == XdmAtomicType.AnyUri;
    }

    internal bool IsXdmDateTimeMapKey(XdmAtomicType type)
    {
        return type >= XdmAtomicType.DateTime && type <= XdmAtomicType.GDay;
    }

    internal bool SameXdmNumericMapKey(XdmAtomicValue left, XdmAtomicValue right)
    {
        double leftFloating = ReadXdmMapFloatingValue(left);
        double rightFloating = ReadXdmMapFloatingValue(right);
        bool leftIsFloating = left.Type == XdmAtomicType.Float || left.Type == XdmAtomicType.Double;
        bool rightIsFloating = right.Type == XdmAtomicType.Float || right.Type == XdmAtomicType.Double;
        if (leftIsFloating && double.IsNaN(leftFloating) || rightIsFloating && double.IsNaN(rightFloating))
        {
            return leftIsFloating && rightIsFloating && double.IsNaN(leftFloating) && double.IsNaN(rightFloating);
        }

        if (leftIsFloating && double.IsInfinity(leftFloating) || rightIsFloating && double.IsInfinity(rightFloating))
        {
            return leftIsFloating && rightIsFloating && leftFloating == rightFloating;
        }

        XdmDecimalStorage leftValue = ReadXdmMapExactNumericValue(left);
        XdmDecimalStorage rightValue = ReadXdmMapExactNumericValue(right);
        int scale = leftValue.Scale > rightValue.Scale ? leftValue.Scale : rightValue.Scale;
        XdmNumericDatatype numeric = new XdmNumericDatatype();
        BigInteger leftCoefficient = numeric.ScaleXdmDecimalCoefficient(leftValue, scale);
        BigInteger rightCoefficient = numeric.ScaleXdmDecimalCoefficient(rightValue, scale);
        return leftCoefficient == rightCoefficient;
    }

    internal double ReadXdmMapFloatingValue(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Float)
        {
            return (float)value.FloatingValue;
        }

        return value.FloatingValue;
    }

    internal XdmDecimalStorage ReadXdmMapExactNumericValue(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
        {
            return new XdmFloatingDatatype().ConvertXdmFloatingToExactDecimal(ReadXdmMapFloatingValue(value), value.Type);
        }

        return new XdmNumericDatatype().ReadXdmDecimalStorage(value);
    }

    internal bool SameXdmDateTimeMapKey(XdmAtomicValue left, XdmAtomicValue right)
    {
        bool samePrimitive = left.Type == right.Type || left.Type == XdmAtomicType.DateTime && right.Type == XdmAtomicType.DateTimeStamp || left.Type == XdmAtomicType.DateTimeStamp && right.Type == XdmAtomicType.DateTime;
        if (!samePrimitive)
        {
            return false;
        }

        XdmDateTimeDatatype datatype = new XdmDateTimeDatatype();
        XdmDateTimeStorage leftStorage = datatype.ReadXdmDateTimeStorage(left);
        XdmDateTimeStorage rightStorage = datatype.ReadXdmDateTimeStorage(right);
        if (leftStorage.HasTimezone != rightStorage.HasTimezone)
        {
            return false;
        }

        return datatype.EqualXdmDateTime(left, right, 0);
    }

    internal XPathDynamicException CreateXdmMapDynamicError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.XPathDynamic;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XPathDynamicException(message, diagnostic, null);
    }
}
