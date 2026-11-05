using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace yaml;

/// <summary>
/// Converts between YAML nodes and JSON-compatible .NET object graphs.
/// </summary>
internal static class YamlJsonConverter
{
    /// <summary>
    /// Creates a YAML node graph from dictionaries, lists, arrays, scalar CLR values, or null.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The resulting YAML node.</returns>
    internal static YamlNode FromJsonValue(object? value)
    {
        Dictionary<object, object> path = new Dictionary<object, object>(ReferenceEqualityComparer.Instance);
        return FromJsonValueWithPath(value, path);
    }

    internal static YamlNode FromJsonValueWithPath(object? value, Dictionary<object, object> path)
    {
        if (value == null)
        {
            return NullNode();
        }

        Type valueType = value.GetType();
        if (valueType.FullName == "System.Byte[]")
        {
            return BinaryNode((byte[])value);
        }

        if (valueType.FullName == "System.DateTime")
        {
            return TimestampNode(((DateTime)value).ToString("O", CultureInfo.InvariantCulture));
        }

        if (valueType.FullName == "System.DateTimeOffset")
        {
            return TimestampNode(((DateTimeOffset)value).ToString("O", CultureInfo.InvariantCulture));
        }

        if (valueType.FullName == "System.Guid")
        {
            return StringNode(((Guid)value).ToString("D"));
        }

        if (valueType.IsEnum)
        {
            return StringNode(value.ToString());
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            return MappingFromDictionary(dictionary, path);
        }

        IList? list = value as IList;
        if (list != null)
        {
            return SequenceFromList(list, path);
        }

        TypeCode code = Convert.GetTypeCode(value);
        if (code == TypeCode.Boolean)
        {
            return BooleanNode(Convert.ToBoolean(value, CultureInfo.InvariantCulture));
        }

        if (code == TypeCode.UInt64)
        {
            return UnsignedIntegerNode(Convert.ToUInt64(value, CultureInfo.InvariantCulture));
        }

        if (IsIntegerTypeCode(code))
        {
            return IntegerNode(Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }

        if (IsFloatTypeCode(code))
        {
            return FloatNode(Convert.ToDouble(value, CultureInfo.InvariantCulture));
        }

        return StringNode(Convert.ToString(value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Converts a YAML node graph to dictionaries, lists, scalar CLR values, or null.
    /// </summary>
    /// <param name="node">The YAML node to process.</param>
    /// <returns>The loaded or converted value; null is a valid YAML result.</returns>
    internal static object? ToJsonValue(YamlNode? node)
    {
        return ToJsonValue(node, new Dictionary<YamlNode, YamlNode>());
    }

    internal static object? ToObjectValue(YamlNode? node, YamlScalarConverterRegistry? converters)
    {
        return ConvertNodeToObject(node, converters, new Dictionary<YamlNode, YamlNode>());
    }

    internal static object? ToConfiguredObjectValue(YamlNode? node, YamlObjectOptions options, YamlTagRegistry? typeTags)
    {
        return ConvertConfiguredNode(node, options, typeTags, new Dictionary<YamlNode, YamlNode>());
    }

    internal static object? ConvertConfiguredNode(YamlNode? node, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<YamlNode, YamlNode> visiting)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            if (node.AliasTarget == null || visiting.ContainsKey(node.AliasTarget))
            {
                return null;
            }

            return ConvertConfiguredNode(node.AliasTarget, options, typeTags, visiting);
        }

        if (visiting.ContainsKey(node))
        {
            return null;
        }

        visiting[node] = node;
        object? value = null;
        if (node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Empty)
        {
            value = ConvertScalarToObject(node, options.ScalarConverters);
        }
        else if (node.Kind == YamlNodeKind.Sequence)
        {
            value = ConvertConfiguredSequence(node, options, typeTags, visiting);
        }
        else if (node.Kind == YamlNodeKind.Mapping)
        {
            value = ConvertConfiguredMapping(node, options, typeTags, visiting);
        }

        visiting.Remove(node);
        return ConvertConfiguredType(node, value, options, typeTags);
    }

    internal static List<object?> ConvertConfiguredSequence(YamlNode node, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<YamlNode, YamlNode> visiting)
    {
        List<object?> values = new List<object?>();
        int index = 0;
        while (index < node.Children.Count)
        {
            values.Add(ConvertConfiguredNode(node.Children[index], options, typeTags, visiting));
            index = index + 1;
        }

        return values;
    }

    internal static Dictionary<string, object?> ConvertConfiguredMapping(YamlNode node, YamlObjectOptions options, YamlTagRegistry? typeTags, Dictionary<YamlNode, YamlNode> visiting)
    {
        Dictionary<string, object?> values = new Dictionary<string, object?>();
        int index = 0;
        while (index < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[index];
            string key = ConvertObjectKeyToString(pair.Key, options.ScalarConverters, visiting);
            values[key] = ConvertConfiguredNode(pair.Value, options, typeTags, visiting);
            index = index + 1;
        }

        return values;
    }

    internal static object? ConvertConfiguredType(YamlNode node, object? value, YamlObjectOptions options, YamlTagRegistry? typeTags)
    {
        if (!options.UseTypeTags || typeTags == null || typeTags.Registrations == null)
        {
            return value;
        }

        string tag = node.Tag == null ? "" : node.Tag;
        int index = 0;
        while (index < typeTags.Registrations.Count)
        {
            YamlTagRegistration? registration = typeTags.Registrations[index];
            if (registration != null && registration.MatchesTag(tag) && registration.ClrType != null
                && (registration.NodeKind == YamlNodeKind.Unknown || registration.NodeKind == node.Kind))
            {
                if (value == null || registration.ClrType.IsAssignableFrom(value.GetType()))
                {
                    return value;
                }

                return Yaml.ConvertLoadedValue(value, registration.ClrType, options);
            }

            index = index + 1;
        }

        return value;
    }

    internal static object? ConvertNodeToObject(YamlNode? node, YamlScalarConverterRegistry? converters, Dictionary<YamlNode, YamlNode> visiting)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            if (node.AliasTarget == null || visiting.ContainsKey(node.AliasTarget))
            {
                return null;
            }

            return ConvertNodeToObject(node.AliasTarget, converters, visiting);
        }

        if (visiting.ContainsKey(node))
        {
            return null;
        }

        visiting[node] = node;
        object? value = null;
        if (node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Empty)
        {
            value = ConvertScalarToObject(node, converters);
        }
        else if (node.Kind == YamlNodeKind.Sequence)
        {
            value = ConvertSequenceToObject(node, converters, visiting);
        }
        else if (node.Kind == YamlNodeKind.Mapping)
        {
            value = ConvertMappingToObject(node, converters, visiting);
        }

        visiting.Remove(node);
        return value;
    }

    internal static object? ConvertScalarToObject(YamlNode node, YamlScalarConverterRegistry? converters)
    {
        YamlScalarConverterRegistration? registration = FindScalarReaderByTag(node, converters);
        if (registration != null)
        {
            return registration.Read(node.Value == null ? "" : node.Value);
        }

        return ScalarToJsonValue(node);
    }

    internal static YamlScalarConverterRegistration? FindScalarReaderByTag(YamlNode node, YamlScalarConverterRegistry? converters)
    {
        if (converters == null || converters.Registrations == null)
        {
            return null;
        }

        string tag = node.Tag == null ? "" : node.Tag;
        int index = 0;
        while (index < converters.Registrations.Count)
        {
            YamlScalarConverterRegistration? registration = converters.Registrations[index];
            if (registration != null && registration.MatchesTag(tag) && registration.CanRead())
            {
                return registration;
            }

            index = index + 1;
        }

        return null;
    }

    internal static List<object?> ConvertSequenceToObject(YamlNode node, YamlScalarConverterRegistry? converters, Dictionary<YamlNode, YamlNode> visiting)
    {
        List<object?> values = new List<object?>();
        int index = 0;
        while (index < node.Children.Count)
        {
            values.Add(ConvertNodeToObject(node.Children[index], converters, visiting));
            index = index + 1;
        }

        return values;
    }

    internal static Dictionary<string, object?> ConvertMappingToObject(YamlNode node, YamlScalarConverterRegistry? converters, Dictionary<YamlNode, YamlNode> visiting)
    {
        Dictionary<string, object?> values = new Dictionary<string, object?>();
        int index = 0;
        while (index < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[index];
            string key = ConvertObjectKeyToString(pair.Key, converters, visiting);
            values[key] = ConvertNodeToObject(pair.Value, converters, visiting);
            index = index + 1;
        }

        return values;
    }

    internal static string ConvertObjectKeyToString(YamlNode? key, YamlScalarConverterRegistry? converters, Dictionary<YamlNode, YamlNode> visiting)
    {
        if (key == null)
        {
            return "";
        }

        if (key.Kind == YamlNodeKind.Scalar || key.Kind == YamlNodeKind.Empty)
        {
            YamlScalarConverterRegistration? registration = FindScalarReaderByTag(key, converters);
            if (registration == null)
            {
                return key.Value == null ? "" : key.Value;
            }

            object? scalar = registration.Read(key.Value == null ? "" : key.Value);
            if (scalar == null)
            {
                return "";
            }

            string? scalarText = Convert.ToString(scalar, CultureInfo.InvariantCulture);
            return scalarText == null ? "" : scalarText;
        }

        object? converted = ConvertNodeToObject(key, converters, visiting);
        if (converted == null)
        {
            return "";
        }

        string? text = Convert.ToString(converted, CultureInfo.InvariantCulture);
        return text == null ? "" : text;
    }

    internal static object? ToJsonValue(YamlNode? node, Dictionary<YamlNode, YamlNode> visiting)
    {
        if (node == null)
        {
            return null;
        }

        if (node.Kind == YamlNodeKind.Alias)
        {
            if (node.AliasTarget == null || visiting.ContainsKey(node.AliasTarget))
            {
                return null;
            }

            return ToJsonValue(node.AliasTarget, visiting);
        }

        if (visiting.ContainsKey(node))
        {
            return null;
        }

        visiting[node] = node;
        object? value = null;
        if (node.Kind == YamlNodeKind.Scalar || node.Kind == YamlNodeKind.Empty)
        {
            value = ScalarToJsonValue(node);
        }
        else if (node.Kind == YamlNodeKind.Sequence)
        {
            value = SequenceToJsonValue(node, visiting);
        }
        else if (node.Kind == YamlNodeKind.Mapping)
        {
            value = MappingToJsonValue(node, visiting);
        }

        visiting.Remove(node);
        return value;
    }

    internal static YamlNode NullNode()
    {
        return YamlNodeFactory.TaggedScalar("", YamlScalarResolver.NullTag);
    }

    internal static YamlNode BooleanNode(bool value)
    {
        string text = value ? "true" : "false";
        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.BoolTag);
    }

    internal static YamlNode IntegerNode(long value)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.IntTag);
    }

    internal static YamlNode UnsignedIntegerNode(ulong value)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.IntTag);
    }

    internal static YamlNode FloatNode(double value)
    {
        string text = "";
        if (double.IsNaN(value))
        {
            text = ".nan";
        }
        else if (double.IsPositiveInfinity(value))
        {
            text = ".inf";
        }
        else if (double.IsNegativeInfinity(value))
        {
            text = "-.inf";
        }
        else
        {
            text = value.ToString("R", CultureInfo.InvariantCulture);
        }

        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.FloatTag);
    }

    internal static YamlNode StringNode(string? value)
    {
        string text = value == null ? "" : value;
        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.FailsafeStringTag);
    }

    internal static YamlNode BinaryNode(byte[]? bytes)
    {
        byte[] value = bytes == null ? new byte[0] : bytes;
        return YamlNodeFactory.TaggedScalar(Convert.ToBase64String(value), YamlScalarResolver.BinaryTag);
    }

    internal static YamlNode TimestampNode(string? value)
    {
        string text = value == null ? "" : value;
        return YamlNodeFactory.TaggedScalar(text, YamlScalarResolver.TimestampTag);
    }

    internal static YamlNode SequenceFromList(IList list, Dictionary<object, object> path)
    {
        if (path.ContainsKey(list))
        {
            throw YamlException.Create("JSON-compatible conversion cannot process collection cycles.");
        }

        path[list] = list;
        YamlNode node = YamlNodeFactory.Sequence();
        foreach (object? item in list)
        {
            node.AddSequenceItem(FromJsonValueWithPath(item, path));
        }

        path.Remove(list);
        return node;
    }

    internal static YamlNode MappingFromDictionary(IDictionary dictionary, Dictionary<object, object> path)
    {
        if (path.ContainsKey(dictionary))
        {
            throw YamlException.Create("JSON-compatible conversion cannot process collection cycles.");
        }

        path[dictionary] = dictionary;
        YamlNode node = YamlNodeFactory.Mapping();
        foreach (DictionaryEntry entry in dictionary)
        {
            string key = JsonKeyToString(entry.Key);
            node.AddMappingValue(key, FromJsonValueWithPath(entry.Value, path));
        }

        path.Remove(dictionary);
        return node;
    }

    internal static string JsonKeyToString(object? key)
    {
        if (key == null)
        {
            return "";
        }

        string? text = Convert.ToString(key, CultureInfo.InvariantCulture);
        return text == null ? "" : text;
    }

    internal static object? ScalarToJsonValue(YamlNode node)
    {
        string tag = ResolvedTagForScalar(node);
        if (tag == YamlScalarResolver.NullTag)
        {
            return null;
        }

        if (tag == YamlScalarResolver.BoolTag)
        {
            return ParseBooleanScalar(node.Value);
        }

        if (tag == YamlScalarResolver.IntTag)
        {
            return ParseIntegerScalar(node.Value);
        }

        if (tag == YamlScalarResolver.FloatTag)
        {
            return ParseFloatScalar(node.Value);
        }

        return node.Value;
    }

    internal static List<object?> SequenceToJsonValue(YamlNode node, Dictionary<YamlNode, YamlNode> visiting)
    {
        List<object?> values = new List<object?>();
        int index = 0;
        while (index < node.Children.Count)
        {
            values.Add(ToJsonValue(node.Children[index], visiting));
            index = index + 1;
        }

        return values;
    }

    internal static Dictionary<string, object?> MappingToJsonValue(YamlNode node, Dictionary<YamlNode, YamlNode> visiting)
    {
        Dictionary<string, object?> values = new Dictionary<string, object?>();
        int index = 0;
        while (index < node.Pairs.Count)
        {
            YamlPair pair = node.Pairs[index];
            string key = KeyToJsonString(pair.Key, visiting);
            values[key] = ToJsonValue(pair.Value, visiting);
            index = index + 1;
        }

        return values;
    }

    internal static string KeyToJsonString(YamlNode? key, Dictionary<YamlNode, YamlNode> visiting)
    {
        if (key == null)
        {
            return "";
        }

        if (key.Kind == YamlNodeKind.Scalar || key.Kind == YamlNodeKind.Empty)
        {
            return key.Value;
        }

        object? value = ToJsonValue(key, visiting);
        if (value == null)
        {
            return "";
        }

        string? text = value.ToString();
        return text == null ? "" : text;
    }

    internal static string ResolvedTagForScalar(YamlNode node)
    {
        if (node == null)
        {
            return YamlScalarResolver.FailsafeStringTag;
        }

        string tag = YamlScalarResolver.NormalizeStandardTag(node.Tag);
        if (tag != "")
        {
            return tag;
        }

        return YamlScalarResolver.ResolveScalarTag(node.Value, node.ScalarStyle, null);
    }

    internal static object ParseBooleanScalar(string value)
    {
        if (value == "true"
            || value == "True"
            || value == "TRUE"
            || value == "y"
            || value == "Y"
            || value == "yes"
            || value == "Yes"
            || value == "YES"
            || value == "on"
            || value == "On"
            || value == "ON")
        {
            return true;
        }

        return false;
    }

    internal static object ParseIntegerScalar(string value)
    {
        string text = value == null ? "" : value.Replace("_", "");
        if (text == "")
        {
            return value == null ? "" : value;
        }

        int sign = 1;
        int index = 0;
        if (text[index] == '+' || text[index] == '-')
        {
            if (text[index] == '-')
            {
                sign = -1;
            }

            index = index + 1;
        }

        int numberBase = 10;
        if (YamlScalarResolver.StartsWithAt(text, index, "0x") || YamlScalarResolver.StartsWithAt(text, index, "0X"))
        {
            numberBase = 16;
            index = index + 2;
        }
        else if (YamlScalarResolver.StartsWithAt(text, index, "0b") || YamlScalarResolver.StartsWithAt(text, index, "0B"))
        {
            numberBase = 2;
            index = index + 2;
        }
        else if (YamlScalarResolver.StartsWithAt(text, index, "0o") || YamlScalarResolver.StartsWithAt(text, index, "0O"))
        {
            numberBase = 8;
            index = index + 2;
        }

        if (index >= text.Length)
        {
            return text;
        }

        ulong number = 0;
        ulong limit = sign < 0 ? 9223372036854775808UL : ulong.MaxValue;
        while (index < text.Length)
        {
            int digit = DigitValue(text[index]);
            if (digit < 0 || digit >= numberBase)
            {
                return text;
            }

            ulong unsignedDigit = (ulong)digit;
            ulong unsignedBase = (ulong)numberBase;
            if (number > (limit - unsignedDigit) / unsignedBase)
            {
                return text;
            }

            number = number * unsignedBase + unsignedDigit;
            index = index + 1;
        }

        if (sign < 0)
        {
            if (number == 9223372036854775808UL)
            {
                return long.MinValue;
            }

            return 0 - (long)number;
        }

        if (number <= long.MaxValue)
        {
            return (long)number;
        }

        return number;
    }

    internal static object ParseFloatScalar(string value)
    {
        string text = value == null ? "" : value.Replace("_", "");
        if (YamlScalarResolver.IsCoreInfinity(text))
        {
            if (text.Length > 0 && text[0] == '-')
            {
                return double.NegativeInfinity;
            }

            return double.PositiveInfinity;
        }

        if (YamlScalarResolver.IsCoreNan(text))
        {
            return double.NaN;
        }

        double parsed = 0;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed))
        {
            return parsed;
        }

        return value == null ? "" : value;
    }

    internal static bool IsIntegerTypeCode(TypeCode code)
    {
        return code == TypeCode.Byte
            || code == TypeCode.SByte
            || code == TypeCode.Int16
            || code == TypeCode.UInt16
            || code == TypeCode.Int32
            || code == TypeCode.UInt32
            || code == TypeCode.Int64;
    }

    internal static bool IsFloatTypeCode(TypeCode code)
    {
        return code == TypeCode.Single
            || code == TypeCode.Double
            || code == TypeCode.Decimal;
    }

    internal static int DigitValue(char value)
    {
        if (value >= '0' && value <= '9')
        {
            return value - '0';
        }

        if (value >= 'a' && value <= 'f')
        {
            return value - 'a' + 10;
        }

        if (value >= 'A' && value <= 'F')
        {
            return value - 'A' + 10;
        }

        return -1;
    }
}
