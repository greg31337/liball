using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace cbor;

internal class CborJsonWriterCore
{
    internal CborJsonOptions? Options;
    internal MemoryStream Output;
    internal Utf8JsonWriter Writer;
    internal List<CborValue> Active;
    internal static readonly BigInteger MaximumSafeInteger = new BigInteger(9007199254740991L);
    internal static readonly BigInteger MinimumSafeInteger = new BigInteger(-9007199254740991L);

    internal CborJsonWriterCore(CborJsonOptions? options)
    {
        Options = options;
        Output = new MemoryStream();
        JsonWriterOptions writerOptions = new JsonWriterOptions();
        writerOptions.Indented = options != null && options.Indented;
        writerOptions.MaxDepth = int.MaxValue;
        Writer = new Utf8JsonWriter(Output, writerOptions);
        Active = new List<CborValue>();
    }

    internal string Convert(CborValue? value)
    {
        ValidateOptions();
        WriteValue(value, 0, "$");
        Writer.Flush();
        byte[] bytes = Output.ToArray();
        Writer.Dispose();
        Output.Dispose();
        string json = Encoding.UTF8.GetString(bytes);
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (json.Length > options.MaxOutputCharacters)
        {
            Fail(
                "CBOR_JSON_MAX_OUTPUT_CHARACTERS",
                "The generated JSON exceeds the configured output character limit.",
                "$"
            );
        }
        return json;
    }

    internal void ValidateOptions()
    {
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if ((int)options.Policy < (int)CborJsonPolicy.Strict
            || (int)options.Policy > (int)CborJsonPolicy.AllowLossy)
        {
            Fail("CBOR_JSON_POLICY_INVALID", "The JSON conversion policy is invalid.", "$" );
        }
        if ((int)options.DuplicatePropertyPolicy < (int)CborDuplicateKeyPolicy.Preserve
            || (int)options.DuplicatePropertyPolicy > (int)CborDuplicateKeyPolicy.KeepLast)
        {
            Fail(
                "CBOR_JSON_DUPLICATE_PROPERTY_POLICY_INVALID",
                "The duplicate JSON property policy is invalid.",
                "$"
            );
        }
        if ((int)options.ByteStringConvention < (int)CborJsonByteStringConvention.TextString
            || (int)options.ByteStringConvention > (int)CborJsonByteStringConvention.PreserveWrapper)
        {
            Fail(
                "CBOR_JSON_BYTE_STRING_CONVENTION_INVALID",
                "The JSON byte-string convention is invalid.",
                "$"
            );
        }
        if ((int)options.TagConvention < (int)CborJsonTagConvention.Map
            || (int)options.TagConvention > (int)CborJsonTagConvention.PreserveWrapper)
        {
            Fail(
                "CBOR_JSON_TAG_CONVENTION_INVALID",
                "The JSON tag convention is invalid.",
                "$"
            );
        }
        if (options.MaxNestingDepth < 0)
        {
            Fail("CBOR_JSON_MAX_NESTING_DEPTH_INVALID", "The nesting limit cannot be negative.", "$" );
        }
        if (options.MaxInputCharacters < 0)
        {
            Fail("CBOR_JSON_MAX_INPUT_CHARACTERS_INVALID", "The input limit cannot be negative.", "$" );
        }
        if (options.MaxOutputCharacters < 0)
        {
            Fail("CBOR_JSON_MAX_OUTPUT_CHARACTERS_INVALID", "The output limit cannot be negative.", "$" );
        }
    }

    internal void WriteValue(CborValue? value, int depth, string path)
    {
        CborValue presentValue = value ?? throw new CborJsonException(
            "CBOR_VALUE_MISSING",
            "The CBOR value is missing.",
            -1,
            path
        );
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (depth > options.MaxNestingDepth)
        {
            Fail(
                "CBOR_JSON_MAX_NESTING_DEPTH",
                "The CBOR value exceeds the configured nesting limit.",
                path
            );
        }
        if (IsActive(presentValue))
        {
            Fail("CBOR_JSON_CYCLE", "A cyclic CBOR value cannot be converted to JSON.", path);
        }

        Active.Add(presentValue);
        switch (presentValue.Kind)
        {
            case CborValueKind.UnsignedInteger:
            case CborValueKind.NegativeInteger:
                WriteInteger(presentValue, path);
                break;
            case CborValueKind.ByteString:
                WriteByteString(presentValue, path);
                break;
            case CborValueKind.TextString:
                WriteTextString(presentValue, path);
                break;
            case CborValueKind.Array:
                WriteArray(presentValue, depth, path);
                break;
            case CborValueKind.Map:
                WriteMap(presentValue, depth, path);
                break;
            case CborValueKind.Tag:
                WriteTag(presentValue, depth, path);
                break;
            case CborValueKind.Float:
                WriteFloat(presentValue, path);
                break;
            case CborValueKind.SimpleValue:
                WriteSimple(presentValue, path);
                break;
            default:
                Fail("CBOR_VALUE_KIND_INVALID", "The CBOR value kind is invalid.", path);
                break;
        }
        Active.RemoveAt(Active.Count - 1);
    }

    internal void WriteInteger(CborValue value, string path)
    {
        if (value.Kind == CborValueKind.UnsignedInteger && value.IntegerValue < BigInteger.Zero)
        {
            Fail("CBOR_INTEGER_SIGN_INVALID", "An unsigned integer cannot be negative.", path);
        }
        if (value.Kind == CborValueKind.NegativeInteger && value.IntegerValue >= BigInteger.Zero)
        {
            Fail("CBOR_INTEGER_SIGN_INVALID", "A negative integer must be negative.", path);
        }

        string text = value.IntegerValue.ToString(CultureInfo.InvariantCulture);
        bool isSafe = value.IntegerValue >= MinimumSafeInteger
            && value.IntegerValue <= MaximumSafeInteger;
        if (isSafe)
        {
            Writer.WriteRawValue(text);
            return;
        }
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (options.Policy == CborJsonPolicy.Strict)
        {
            Fail(
                "CBOR_JSON_INTEGER_OUT_OF_RANGE",
                "The integer is outside the exactly interoperable JSON integer range.",
                path
            );
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            WriteIntegerWrapper(text);
            return;
        }
        Writer.WriteStringValue(text);
    }

    internal void WriteByteString(CborValue value, string path)
    {
        byte[] bytes = value.ByteStringValue ?? throw new CborJsonException(
            "CBOR_BYTE_STRING_MISSING",
            "The byte string payload is missing.",
            -1,
            path
        );
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        string text = EncodeBase64Url(bytes);
        if (options.Policy == CborJsonPolicy.Strict)
        {
            Fail(
                "CBOR_JSON_BYTE_STRING_UNREPRESENTABLE",
                "A CBOR byte string has no direct JSON representation.",
                path
            );
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            WriteBytesWrapper(text);
            return;
        }
        Writer.WriteStringValue(text);
    }

    internal void WriteTextString(CborValue value, string path)
    {
        string text = value.TextStringValue ?? throw new CborJsonException(
            "CBOR_TEXT_STRING_MISSING",
            "The text string payload is missing.",
            -1,
            path
        );
        ValidateString(text, path);
        Writer.WriteStringValue(text);
    }

    internal void WriteArray(CborValue value, int depth, string path)
    {
        List<CborValue> items = value.ArrayItems ?? throw new CborJsonException(
            "CBOR_ARRAY_ITEMS_MISSING",
            "The array items are missing.",
            -1,
            path
        );
        Writer.WriteStartArray();
        int index = 0;
        while (index < items.Count)
        {
            WriteValue(items[index], depth + 1, $"{path}[{index}]");
            index++;
        }
        Writer.WriteEndArray();
    }

    internal void WriteMap(CborValue value, int depth, string path)
    {
        List<CborMapEntry> entries = value.MapEntries ?? throw new CborJsonException(
            "CBOR_MAP_ENTRIES_MISSING",
            "The map entries are missing.",
            -1,
            path
        );
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (options.Policy == CborJsonPolicy.Strict)
        {
            WriteStrictMap(entries, depth, path);
            return;
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            WritePreservedMap(entries, depth, path);
            return;
        }
        WriteLossyMap(entries, depth, path);
    }

    internal void WriteStrictMap(List<CborMapEntry> entries, int depth, string path)
    {
        List<string> names = new List<string>();
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index] ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A map entry is missing.",
                -1,
                $"{path}[{index}]"
            );
            CborValue key = entry.Key ?? throw new CborJsonException(
                "CBOR_MAP_KEY_MISSING",
                "A map key is missing.",
                -1,
                $"{path}[{index}].key"
            );
            if (key.Kind != CborValueKind.TextString)
            {
                Fail(
                    "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
                    "A non-text map key has no direct JSON representation.",
                    $"{path}[{index}].key"
                );
            }
            string name = key.TextStringValue ?? throw new CborJsonException(
                "CBOR_TEXT_STRING_MISSING",
                "The map key text is missing.",
                -1,
                $"{path}[{index}].key"
            );
            ValidateString(name, $"{path}[{index}].key");
            int duplicate = 0;
            while (duplicate < names.Count)
            {
                if (names[duplicate] == name)
                {
                    Fail(
                        "CBOR_JSON_DUPLICATE_PROPERTY",
                        "Duplicate CBOR text keys cannot be represented in a JSON object.",
                        $"{path}[{index}].key"
                    );
                }
                duplicate++;
            }
            names.Add(name);
            if (entry.Value == null)
            {
                Fail("CBOR_MAP_VALUE_MISSING", "A map value is missing.", $"{path}[{index}].value");
            }
            index++;
        }

        Writer.WriteStartObject();
        index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index] ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A map entry is missing.",
                -1,
                $"{path}[{index}]"
            );
            CborValue key = entry.Key ?? throw new CborJsonException(
                "CBOR_MAP_KEY_MISSING",
                "A map key is missing.",
                -1,
                $"{path}[{index}].key"
            );
            string name = key.TextStringValue ?? throw new CborJsonException(
                "CBOR_TEXT_STRING_MISSING",
                "The map key text is missing.",
                -1,
                $"{path}[{index}].key"
            );
            Writer.WritePropertyName(name);
            WriteValue(entry.Value, depth + 1, $"{path}[{index}].value");
            index++;
        }
        Writer.WriteEndObject();
    }

    internal void WritePreservedMap(List<CborMapEntry> entries, int depth, string path)
    {
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index] ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A map entry is missing.",
                -1,
                $"{path}[{index}]"
            );
            CborValue key = entry.Key ?? throw new CborJsonException(
                "CBOR_MAP_KEY_MISSING",
                "A map key is missing.",
                -1,
                $"{path}[{index}].key"
            );
            if (entry.Value == null)
            {
                Fail("CBOR_MAP_VALUE_MISSING", "A map value is missing.", $"{path}[{index}].value");
            }
            index++;
        }

        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "map");
        Writer.WritePropertyName("entries");
        Writer.WriteStartArray();
        index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index] ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A map entry is missing.",
                -1,
                $"{path}[{index}]"
            );
            Writer.WriteStartArray();
            WriteValue(entry.Key, depth + 1, $"{path}[{index}].key");
            WriteValue(entry.Value, depth + 1, $"{path}[{index}].value");
            Writer.WriteEndArray();
            index++;
        }
        Writer.WriteEndArray();
        Writer.WriteEndObject();
    }

    internal void WriteLossyMap(List<CborMapEntry> entries, int depth, string path)
    {
        List<CborJsonProperty> properties = CreateLossyProperties(entries, path);
        Writer.WriteStartObject();
        int index = 0;
        while (index < properties.Count)
        {
            CborJsonProperty property = properties[index];
            string name = property.Name ?? throw new CborJsonException(
                "CBOR_JSON_PROPERTY_NAME_MISSING",
                "A converted JSON property name is missing.",
                -1,
                path
            );
            CborMapEntry entry = property.Entry ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A converted map entry is missing.",
                -1,
                path
            );
            Writer.WritePropertyName(name);
            WriteValue(
                entry.Value,
                depth + 1,
                $"{path}[{property.SourceIndex}].value"
            );
            index++;
        }
        Writer.WriteEndObject();
    }

    internal List<CborJsonProperty> CreateLossyProperties(List<CborMapEntry> entries, string path)
    {
        List<CborJsonProperty> properties = new List<CborJsonProperty>();
        int index = 0;
        while (index < entries.Count)
        {
            CborMapEntry entry = entries[index] ?? throw new CborJsonException(
                "CBOR_MAP_ENTRY_MISSING",
                "A map entry is missing.",
                -1,
                $"{path}[{index}]"
            );
            CborValue key = entry.Key ?? throw new CborJsonException(
                "CBOR_MAP_KEY_MISSING",
                "A map key is missing.",
                -1,
                $"{path}[{index}].key"
            );
            if (entry.Value == null)
            {
                Fail("CBOR_MAP_VALUE_MISSING", "A map value is missing.", $"{path}[{index}].value");
            }
            string name = ConvertLossyKey(key, $"{path}[{index}].key");
            int existing = FindProperty(properties, name);
            if (existing >= 0)
            {
                properties.RemoveAt(existing);
            }
            CborJsonProperty property = new CborJsonProperty();
            property.Name = name;
            property.Entry = entry;
            property.SourceIndex = index;
            properties.Add(property);
            index++;
        }
        return properties;
    }

    internal int FindProperty(List<CborJsonProperty> properties, string name)
    {
        int index = 0;
        while (index < properties.Count)
        {
            if (properties[index].Name == name)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal string ConvertLossyKey(CborValue key, string path)
    {
        if (key.Kind == CborValueKind.TextString)
        {
            string text = key.TextStringValue ?? throw new CborJsonException(
                "CBOR_TEXT_STRING_MISSING",
                "The map key text is missing.",
                -1,
                path
            );
            ValidateString(text, path);
            return text;
        }
        if (key.Kind == CborValueKind.UnsignedInteger
            || key.Kind == CborValueKind.NegativeInteger)
        {
            if (key.Kind == CborValueKind.UnsignedInteger && key.IntegerValue < BigInteger.Zero)
            {
                Fail("CBOR_INTEGER_SIGN_INVALID", "An unsigned integer cannot be negative.", path);
            }
            if (key.Kind == CborValueKind.NegativeInteger && key.IntegerValue >= BigInteger.Zero)
            {
                Fail("CBOR_INTEGER_SIGN_INVALID", "A negative integer must be negative.", path);
            }
            return key.IntegerValue.ToString(CultureInfo.InvariantCulture);
        }
        if (key.Kind == CborValueKind.ByteString)
        {
            byte[] bytes = key.ByteStringValue ?? throw new CborJsonException(
                "CBOR_BYTE_STRING_MISSING",
                "The map key bytes are missing.",
                -1,
                path
            );
            return "base64url:" + EncodeBase64Url(bytes);
        }
        if (key.Kind == CborValueKind.Float)
        {
            CborFloatValue floatValue = key.FloatValue ?? throw new CborJsonException(
                "CBOR_FLOAT_VALUE_MISSING",
                "The map key float data is missing.",
                -1,
                path
            );
            double number = floatValue.Value;
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                Fail(
                    "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
                    "A non-finite map key cannot be converted to a JSON property name.",
                    path
                );
            }
            return number.ToString("R", CultureInfo.InvariantCulture);
        }
        if (key.Kind == CborValueKind.SimpleValue)
        {
            if (key.SimpleValue == 20)
            {
                return "false";
            }
            if (key.SimpleValue == 21)
            {
                return "true";
            }
            if (key.SimpleValue == 22)
            {
                return "null";
            }
            if (key.SimpleValue == 23)
            {
                return "undefined";
            }
            if (key.SimpleValue >= 24 && key.SimpleValue <= 31)
            {
                Fail("CBOR_SIMPLE_VALUE_RESERVED", "The simple value number is reserved.", path);
            }
            return "simple:" + key.SimpleValue.ToString(CultureInfo.InvariantCulture);
        }
        Fail(
            "CBOR_JSON_NON_TEXT_KEY_UNREPRESENTABLE",
            "The complex map key cannot be converted to a JSON property name.",
            path
        );
        return "";
    }

    internal void WriteTag(CborValue value, int depth, string path)
    {
        CborValue taggedValue = value.TaggedValue ?? throw new CborJsonException(
            "CBOR_TAGGED_VALUE_MISSING",
            "The tagged value is missing.",
            -1,
            path
        );
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (options.Policy == CborJsonPolicy.Strict)
        {
            Fail("CBOR_JSON_TAG_UNREPRESENTABLE", "A CBOR tag has no direct JSON representation.", path);
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            WriteTagWrapper(value, depth, path);
            return;
        }
        WriteValue(taggedValue, depth + 1, $"{path}<tag:{value.TagNumber}>");
    }

    internal void WriteFloat(CborValue value, string path)
    {
        CborFloatValue floatValue = value.FloatValue ?? throw new CborJsonException(
            "CBOR_FLOAT_VALUE_MISSING",
            "The floating-point data is missing.",
            -1,
            path
        );
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        double number = floatValue.Value;
        if (!double.IsNaN(number) && !double.IsInfinity(number))
        {
            Writer.WriteNumberValue(number);
            return;
        }
        if (options.Policy == CborJsonPolicy.Strict)
        {
            Fail(
                "CBOR_JSON_NON_FINITE_FLOAT_UNREPRESENTABLE",
                "A non-finite floating-point value has no JSON number representation.",
                path
            );
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            string text = "NaN";
            if (double.IsPositiveInfinity(number))
            {
                text = "Infinity";
            }
            if (double.IsNegativeInfinity(number))
            {
                text = "-Infinity";
            }
            WriteFloatWrapper(text);
            return;
        }
        Writer.WriteNullValue();
    }

    internal void WriteSimple(CborValue value, string path)
    {
        CborJsonOptions options = Options ?? throw new CborJsonException(
            "CBOR_JSON_OPTIONS_MISSING",
            "JSON conversion options are required.",
            -1,
            "$"
        );
        if (value.SimpleValue == 20)
        {
            Writer.WriteBooleanValue(false);
            return;
        }
        if (value.SimpleValue == 21)
        {
            Writer.WriteBooleanValue(true);
            return;
        }
        if (value.SimpleValue == 22)
        {
            Writer.WriteNullValue();
            return;
        }
        if (value.SimpleValue >= 24 && value.SimpleValue <= 31)
        {
            Fail("CBOR_SIMPLE_VALUE_RESERVED", "The simple value number is reserved.", path);
        }
        if (options.Policy == CborJsonPolicy.Strict)
        {
            string code = "CBOR_JSON_SIMPLE_VALUE_UNREPRESENTABLE";
            if (value.SimpleValue == 23)
            {
                code = "CBOR_JSON_UNDEFINED_UNREPRESENTABLE";
            }
            Fail(code, "The CBOR simple value has no direct JSON representation.", path);
        }
        if (options.Policy == CborJsonPolicy.PreserveInformation)
        {
            if (value.SimpleValue == 23)
            {
                WriteUndefinedWrapper();
                return;
            }
            WriteSimpleWrapper(value.SimpleValue);
            return;
        }
        Writer.WriteNullValue();
    }

    internal void WriteBytesWrapper(string text)
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "bytes");
        Writer.WriteString("encoding", "base64url");
        Writer.WriteString("value", text);
        Writer.WriteEndObject();
    }

    internal void WriteIntegerWrapper(string text)
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "integer");
        Writer.WriteString("value", text);
        Writer.WriteEndObject();
    }

    internal void WriteTagWrapper(CborValue value, int depth, string path)
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "tag");
        Writer.WriteString("number", value.TagNumber.ToString(CultureInfo.InvariantCulture));
        Writer.WritePropertyName("value");
        WriteValue(value.TaggedValue, depth + 1, $"{path}<tag:{value.TagNumber}>");
        Writer.WriteEndObject();
    }

    internal void WriteUndefinedWrapper()
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "undefined");
        Writer.WriteEndObject();
    }

    internal void WriteFloatWrapper(string text)
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "float");
        Writer.WriteString("value", text);
        Writer.WriteEndObject();
    }

    internal void WriteSimpleWrapper(byte value)
    {
        Writer.WriteStartObject();
        Writer.WriteString("$cbor", "simple");
        Writer.WriteNumber("value", value);
        Writer.WriteEndObject();
    }

    internal string EncodeBase64Url(byte[] value)
    {
        string base64 = System.Convert.ToBase64String(value);
        StringBuilder builder = new StringBuilder(base64.Length);
        int index = 0;
        while (index < base64.Length)
        {
            char character = base64[index];
            if (character == '=')
            {
                break;
            }
            if (character == '+')
            {
                builder.Append('-');
            }
            else if (character == '/')
            {
                builder.Append('_');
            }
            else
            {
                builder.Append(character);
            }
            index++;
        }
        return builder.ToString();
    }

    internal void ValidateString(string value, string path)
    {
        if (!CborTokenWritePlan.IsWellFormedUtf16(value))
        {
            Fail("CBOR_TEXT_INVALID_UTF16", "The text contains an unpaired UTF-16 surrogate.", path);
        }
    }

    internal bool IsActive(CborValue value)
    {
        int index = 0;
        while (index < Active.Count)
        {
            if (Active[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal void Fail(string code, string message, string path)
    {
        throw new CborJsonException(code, message, -1, path);
    }
}
