using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace cbor;

internal class CborJsonReaderCore
{
    internal string? Source;
    internal CborJsonOptions? Options;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal int ErrorOffset = -1;
    internal string? ErrorPath;

    internal CborJsonReaderCore(string? source, CborJsonOptions? options)
    {
        Source = source;
        Options = options;
    }

    internal CborReadResult Parse()
    {
        if (!ValidateOptions())
        {
            return CreateResult();
        }
        if (Source == null)
        {
            SetFailure(
                "CBOR_JSON_TEXT_MISSING",
                "JSON text is required.",
                -1,
                "$"
            );
            return CreateResult();
        }
        string source = Source;
        CborJsonOptions activeOptions = GetOptions();
        if (source.Length > activeOptions.MaxInputCharacters)
        {
            SetFailure(
                "CBOR_JSON_MAX_INPUT_CHARACTERS",
                "The JSON input exceeds the configured character limit.",
                activeOptions.MaxInputCharacters,
                "$"
            );
            return CreateResult();
        }

        CborJsonSyntaxValidator syntax = new CborJsonSyntaxValidator(
            source,
            activeOptions.MaxNestingDepth
        );
        if (!syntax.ValidateDocument())
        {
            SetFailure(
                syntax.ErrorCode ?? "CBOR_JSON_SYNTAX_INVALID",
                syntax.ErrorMessage ?? "The JSON input is invalid.",
                syntax.ErrorOffset,
                syntax.ErrorPath ?? "$"
            );
            return CreateResult();
        }

        JsonDocumentOptions documentOptions = new JsonDocumentOptions();
        documentOptions.AllowTrailingCommas = false;
        documentOptions.CommentHandling = JsonCommentHandling.Disallow;
        documentOptions.MaxDepth = int.MaxValue;
        JsonDocument document = JsonDocument.Parse(source, documentOptions);
        CborValue? value = ConvertElement(document.RootElement, 0, "$" );
        document.Dispose();
        if (value == null)
        {
            return CreateResult();
        }

        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Done;
        result.Value = value;
        result.Consumed = source.Length;
        result.Remaining = 0;
        result.SourceOffset = 0;
        return result;
    }

    internal bool ValidateOptions()
    {
        if (Options == null)
        {
            SetFailure(
                "CBOR_JSON_OPTIONS_MISSING",
                "JSON conversion options are required.",
                -1,
                "$"
            );
            return false;
        }
        int policy = (int)Options.Policy;
        int duplicatePolicy = (int)Options.DuplicatePropertyPolicy;
        int byteConvention = (int)Options.ByteStringConvention;
        int tagConvention = (int)Options.TagConvention;
        if (policy < (int)CborJsonPolicy.Strict
            || policy > (int)CborJsonPolicy.AllowLossy)
        {
            SetFailure(
                "CBOR_JSON_POLICY_INVALID",
                "The JSON conversion policy is invalid.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null
            && (duplicatePolicy < (int)CborDuplicateKeyPolicy.Preserve
                || duplicatePolicy > (int)CborDuplicateKeyPolicy.KeepLast))
        {
            SetFailure(
                "CBOR_JSON_DUPLICATE_PROPERTY_POLICY_INVALID",
                "The duplicate JSON property policy is invalid.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null
            && (byteConvention < (int)CborJsonByteStringConvention.TextString
                || byteConvention > (int)CborJsonByteStringConvention.PreserveWrapper))
        {
            SetFailure(
                "CBOR_JSON_BYTE_STRING_CONVENTION_INVALID",
                "The JSON byte-string convention is invalid.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null
            && (tagConvention < (int)CborJsonTagConvention.Map
                || tagConvention > (int)CborJsonTagConvention.PreserveWrapper))
        {
            SetFailure(
                "CBOR_JSON_TAG_CONVENTION_INVALID",
                "The JSON tag convention is invalid.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null && Options.MaxNestingDepth < 0)
        {
            SetFailure(
                "CBOR_JSON_MAX_NESTING_DEPTH_INVALID",
                "The nesting limit cannot be negative.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null && Options.MaxInputCharacters < 0)
        {
            SetFailure(
                "CBOR_JSON_MAX_INPUT_CHARACTERS_INVALID",
                "The input limit cannot be negative.",
                -1,
                "$"
            );
        }
        if (ErrorCode == null && Options.MaxOutputCharacters < 0)
        {
            SetFailure(
                "CBOR_JSON_MAX_OUTPUT_CHARACTERS_INVALID",
                "The output limit cannot be negative.",
                -1,
                "$"
            );
        }
        return ErrorCode == null;
    }

    internal CborJsonOptions GetOptions()
    {
        return Options ?? new CborJsonOptions();
    }

    internal CborValue? ConvertElement(JsonElement element, int depth, string path)
    {
        JsonValueKind kind = element.ValueKind;
        if (kind == JsonValueKind.Object)
        {
            return ConvertObject(element, depth, path);
        }
        if (kind == JsonValueKind.Array)
        {
            return ConvertArray(element, depth, path);
        }
        if (kind == JsonValueKind.String)
        {
            return ConvertString(element, path);
        }
        if (kind == JsonValueKind.Number)
        {
            return ConvertNumber(element, path);
        }
        if (kind == JsonValueKind.True)
        {
            return CreateSimple(CborSimpleValues.True);
        }
        if (kind == JsonValueKind.False)
        {
            return CreateSimple(CborSimpleValues.False);
        }
        if (kind == JsonValueKind.Null)
        {
            return CreateSimple(CborSimpleValues.Null);
        }
        SetFailure(
            "CBOR_JSON_VALUE_KIND_INVALID",
            "The JSON value kind is invalid.",
            -1,
            path
        );
        return null;
    }

    internal CborValue? ConvertString(JsonElement element, string path)
    {
        string? decoded = element.GetString();
        if (decoded == null)
        {
            SetFailure(
                "CBOR_JSON_STRING_INVALID",
                "The JSON string could not be decoded.",
                -1,
                path
            );
            return null;
        }
        CborJsonOptions activeOptions = GetOptions();
        if (activeOptions.ByteStringConvention == CborJsonByteStringConvention.Base64UrlString)
        {
            byte[]? bytes = DecodeBase64Url(decoded, path);
            if (bytes == null)
            {
                return null;
            }
            CborValue byteString = new CborValue();
            byteString.Kind = CborValueKind.ByteString;
            byteString.ByteStringValue = bytes;
            byteString.LengthKind = CborLengthKind.Definite;
            return byteString;
        }
        CborValue text = new CborValue();
        text.Kind = CborValueKind.TextString;
        text.TextStringValue = decoded;
        text.LengthKind = CborLengthKind.Definite;
        return text;
    }

    internal CborValue? ConvertNumber(JsonElement element, string path)
    {
        string raw = element.GetRawText();
        bool isFloat = raw.IndexOf('.') >= 0
            || raw.IndexOf('e') >= 0
            || raw.IndexOf('E') >= 0;
        if (!isFloat && raw == "-0")
        {
            return CreateFloat(-0.0);
        }
        if (!isFloat)
        {
            BigInteger integer = ParseDecimalInteger(raw, false, path);
            if (ErrorCode != null)
            {
                return null;
            }
            return CreateInteger(integer);
        }
        double number = double.Parse(
            raw,
            NumberStyles.Float,
            CultureInfo.InvariantCulture
        );
        if (double.IsNaN(number) || double.IsInfinity(number))
        {
            SetFailure(
                "CBOR_JSON_NUMBER_OUT_OF_RANGE",
                "The JSON number is outside the binary64 range.",
                -1,
                path
            );
            return null;
        }
        return CreateFloat(number);
    }

    internal CborValue? ConvertArray(JsonElement element, int depth, string path)
    {
        CborValue array = new CborValue();
        array.Kind = CborValueKind.Array;
        array.LengthKind = CborLengthKind.Definite;
        array.ArrayItems = new List<CborValue>();
        int index = 0;
        foreach (JsonElement child in element.EnumerateArray())
        {
            CborValue? item = ConvertElement(child, depth + 1, $"{path}[{index}]");
            if (item == null)
            {
                return null;
            }
            array.ArrayItems.Add(item);
            index++;
        }
        return array;
    }

    internal CborValue? ConvertObject(JsonElement element, int depth, string path)
    {
        string? discriminator = FindDiscriminator(element);
        if (discriminator != null && ShouldRecognizeWrapper(discriminator))
        {
            return ConvertWrapper(element, discriminator, depth, path);
        }

        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.LengthKind = CborLengthKind.Definite;
        map.MapEntries = new List<CborMapEntry>();
        CborJsonOptions activeOptions = GetOptions();
        int propertyIndex = 0;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            int duplicate = FindMapEntry(map.MapEntries, property.Name);
            if (duplicate >= 0
                && activeOptions.DuplicatePropertyPolicy == CborDuplicateKeyPolicy.Reject)
            {
                SetFailure(
                    "CBOR_JSON_DUPLICATE_PROPERTY",
                    "The JSON object contains a duplicate property name.",
                    -1,
                    $"{path}.property[{propertyIndex}]"
                );
                return null;
            }
            if (duplicate >= 0
                && activeOptions.DuplicatePropertyPolicy == CborDuplicateKeyPolicy.KeepFirst)
            {
                propertyIndex++;
                continue;
            }
            CborValue? value = ConvertElement(
                property.Value,
                depth + 1,
                $"{path}.property[{propertyIndex}]"
            );
            if (value == null)
            {
                return null;
            }
            if (duplicate >= 0
                && activeOptions.DuplicatePropertyPolicy == CborDuplicateKeyPolicy.KeepLast)
            {
                map.MapEntries.RemoveAt(duplicate);
            }
            CborValue key = new CborValue();
            key.Kind = CborValueKind.TextString;
            key.TextStringValue = property.Name;
            key.LengthKind = CborLengthKind.Definite;
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = value;
            map.MapEntries.Add(entry);
            propertyIndex++;
        }
        return map;
    }

    internal string? FindDiscriminator(JsonElement element)
    {
        CborJsonElementSlot slot = FindObjectProperty(element, "$cbor");
        if (!slot.Found || slot.Count != 1 || slot.Value.ValueKind != JsonValueKind.String)
        {
            return null;
        }
        return slot.Value.GetString();
    }

    internal bool ShouldRecognizeWrapper(string discriminator)
    {
        CborJsonOptions activeOptions = GetOptions();
        if (activeOptions.Policy == CborJsonPolicy.PreserveInformation)
        {
            return IsKnownDiscriminator(discriminator);
        }
        if (discriminator == "bytes"
            && activeOptions.ByteStringConvention == CborJsonByteStringConvention.PreserveWrapper)
        {
            return true;
        }
        if (discriminator == "tag"
            && activeOptions.TagConvention == CborJsonTagConvention.PreserveWrapper)
        {
            return true;
        }
        return false;
    }

    internal bool IsKnownDiscriminator(string discriminator)
    {
        return discriminator == "bytes"
            || discriminator == "integer"
            || discriminator == "tag"
            || discriminator == "map"
            || discriminator == "undefined"
            || discriminator == "float"
            || discriminator == "simple";
    }

    internal CborValue? ConvertWrapper(
        JsonElement element,
        string discriminator,
        int depth,
        string path)
    {
        if (discriminator == "bytes")
        {
            return ConvertBytesWrapper(element, path);
        }
        if (discriminator == "integer")
        {
            return ConvertIntegerWrapper(element, path);
        }
        if (discriminator == "tag")
        {
            return ConvertTagWrapper(element, depth, path);
        }
        if (discriminator == "map")
        {
            return ConvertMapWrapper(element, depth, path);
        }
        if (discriminator == "undefined")
        {
            return ConvertUndefinedWrapper(element, path);
        }
        if (discriminator == "float")
        {
            return ConvertFloatWrapper(element, path);
        }
        if (discriminator == "simple")
        {
            return ConvertSimpleWrapper(element, path);
        }
        SetFailure(
            "CBOR_JSON_WRAPPER_INVALID",
            "The preserved CBOR wrapper discriminator is invalid.",
            -1,
            path
        );
        return null;
    }

    internal CborValue? ConvertBytesWrapper(JsonElement element, string path)
    {
        CborJsonElementSlot encoding = FindObjectProperty(element, "encoding");
        CborJsonElementSlot value = FindObjectProperty(element, "value");
        if (CountObjectProperties(element) != 3
            || !encoding.Found || encoding.Count != 1
            || !value.Found || value.Count != 1
            || encoding.Value.ValueKind != JsonValueKind.String
            || value.Value.ValueKind != JsonValueKind.String
            || encoding.Value.GetString() != "base64url")
        {
            SetFailure(
                "CBOR_JSON_BYTE_WRAPPER_INVALID",
                "The preserved byte-string wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        string encoded = value.Value.GetString() ?? "";
        byte[]? bytes = DecodeBase64Url(encoded, path);
        if (bytes == null)
        {
            return null;
        }
        CborValue result = new CborValue();
        result.Kind = CborValueKind.ByteString;
        result.ByteStringValue = bytes;
        result.LengthKind = CborLengthKind.Definite;
        return result;
    }

    internal CborValue? ConvertIntegerWrapper(JsonElement element, string path)
    {
        CborJsonElementSlot value = FindObjectProperty(element, "value");
        if (CountObjectProperties(element) != 2
            || !value.Found || value.Count != 1
            || value.Value.ValueKind != JsonValueKind.String)
        {
            SetFailure(
                "CBOR_JSON_INTEGER_WRAPPER_INVALID",
                "The preserved integer wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        string text = value.Value.GetString() ?? "";
        BigInteger integer = ParseDecimalInteger(text, true, path);
        if (ErrorCode != null)
        {
            return null;
        }
        return CreateInteger(integer);
    }

    internal CborValue? ConvertTagWrapper(JsonElement element, int depth, string path)
    {
        CborJsonElementSlot number = FindObjectProperty(element, "number");
        CborJsonElementSlot value = FindObjectProperty(element, "value");
        if (CountObjectProperties(element) != 3
            || !number.Found || number.Count != 1
            || !value.Found || value.Count != 1
            || number.Value.ValueKind != JsonValueKind.String)
        {
            SetFailure(
                "CBOR_JSON_TAG_WRAPPER_INVALID",
                "The preserved tag wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        string text = number.Value.GetString() ?? "";
        BigInteger parsed = ParseDecimalInteger(text, true, path);
        if (ErrorCode != null || parsed < BigInteger.Zero
            || parsed > new BigInteger(ulong.MaxValue))
        {
            if (ErrorCode == null)
            {
                SetFailure(
                    "CBOR_JSON_TAG_NUMBER_INVALID",
                    "The preserved tag number is outside the unsigned 64-bit range.",
                    -1,
                    path
                );
            }
            return null;
        }
        CborValue? content = ConvertElement(
            value.Value,
            depth + 1,
            $"{path}<tag:{text}>"
        );
        if (content == null)
        {
            return null;
        }
        CborValue tag = new CborValue();
        tag.Kind = CborValueKind.Tag;
        tag.TagNumber = (ulong)parsed;
        tag.TaggedValue = content;
        return tag;
    }

    internal CborValue? ConvertMapWrapper(JsonElement element, int depth, string path)
    {
        CborJsonElementSlot entries = FindObjectProperty(element, "entries");
        if (CountObjectProperties(element) != 2
            || !entries.Found || entries.Count != 1
            || entries.Value.ValueKind != JsonValueKind.Array)
        {
            SetFailure(
                "CBOR_JSON_MAP_WRAPPER_INVALID",
                "The preserved map wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        CborValue map = new CborValue();
        map.Kind = CborValueKind.Map;
        map.LengthKind = CborLengthKind.Definite;
        map.MapEntries = new List<CborMapEntry>();
        int index = 0;
        foreach (JsonElement pair in entries.Value.EnumerateArray())
        {
            if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
            {
                SetFailure(
                    "CBOR_JSON_MAP_ENTRY_WRAPPER_INVALID",
                    "A preserved map entry must be a two-element array.",
                    -1,
                    $"{path}[{index}]"
                );
                return null;
            }
            JsonElement.ArrayEnumerator pairValues = pair.EnumerateArray();
            pairValues.MoveNext();
            CborValue? key = ConvertElement(
                pairValues.Current,
                depth + 3,
                $"{path}[{index}].key"
            );
            pairValues.MoveNext();
            CborValue? value = ConvertElement(
                pairValues.Current,
                depth + 3,
                $"{path}[{index}].value"
            );
            pairValues.Dispose();
            if (key == null || value == null)
            {
                return null;
            }
            CborMapEntry entry = new CborMapEntry();
            entry.Key = key;
            entry.Value = value;
            map.MapEntries.Add(entry);
            index++;
        }
        return map;
    }

    internal CborValue? ConvertUndefinedWrapper(JsonElement element, string path)
    {
        if (CountObjectProperties(element) != 1)
        {
            SetFailure(
                "CBOR_JSON_UNDEFINED_WRAPPER_INVALID",
                "The preserved undefined wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        return CreateSimple(CborSimpleValues.Undefined);
    }

    internal CborValue? ConvertFloatWrapper(JsonElement element, string path)
    {
        CborJsonElementSlot value = FindObjectProperty(element, "value");
        if (CountObjectProperties(element) != 2
            || !value.Found || value.Count != 1
            || value.Value.ValueKind != JsonValueKind.String)
        {
            SetFailure(
                "CBOR_JSON_FLOAT_WRAPPER_INVALID",
                "The preserved floating-point wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        string text = value.Value.GetString() ?? "";
        if (text == "NaN")
        {
            return CreateFloat(double.NaN);
        }
        if (text == "Infinity")
        {
            return CreateFloat(double.PositiveInfinity);
        }
        if (text == "-Infinity")
        {
            return CreateFloat(double.NegativeInfinity);
        }
        SetFailure(
            "CBOR_JSON_FLOAT_WRAPPER_INVALID",
            "The preserved floating-point wrapper value is invalid.",
            -1,
            path
        );
        return null;
    }

    internal CborValue? ConvertSimpleWrapper(JsonElement element, string path)
    {
        CborJsonElementSlot value = FindObjectProperty(element, "value");
        if (CountObjectProperties(element) != 2
            || !value.Found || value.Count != 1
            || value.Value.ValueKind != JsonValueKind.Number)
        {
            SetFailure(
                "CBOR_JSON_SIMPLE_WRAPPER_INVALID",
                "The preserved simple-value wrapper is invalid.",
                -1,
                path
            );
            return null;
        }
        string text = value.Value.GetRawText();
        if (text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0)
        {
            SetFailure(
                "CBOR_JSON_SIMPLE_WRAPPER_INVALID",
                "The preserved simple-value number must be an integer.",
                -1,
                path
            );
            return null;
        }
        BigInteger number = ParseDecimalInteger(text, false, path);
        if (ErrorCode != null || number < BigInteger.Zero || number > new BigInteger(255))
        {
            if (ErrorCode == null)
            {
                SetFailure(
                    "CBOR_JSON_SIMPLE_WRAPPER_INVALID",
                    "The preserved simple-value number is outside the byte range.",
                    -1,
                    path
                );
            }
            return null;
        }
        byte simple = (byte)number;
        if (simple >= CborSimpleValues.FirstReserved
            && simple <= CborSimpleValues.LastReserved)
        {
            SetFailure(
                "CBOR_SIMPLE_VALUE_RESERVED",
                "The preserved simple-value number is reserved.",
                -1,
                path
            );
            return null;
        }
        return CreateSimple(simple);
    }

    internal CborJsonElementSlot FindObjectProperty(JsonElement element, string name)
    {
        CborJsonElementSlot slot = new CborJsonElementSlot();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (property.Name == name)
            {
                slot.Found = true;
                slot.Count++;
                slot.Value = property.Value;
            }
        }
        return slot;
    }

    internal int CountObjectProperties(JsonElement element)
    {
        int count = 0;
        foreach (JsonProperty property in element.EnumerateObject())
        {
            count++;
        }
        return count;
    }

    internal int FindMapEntry(List<CborMapEntry> entries, string name)
    {
        int index = 0;
        while (index < entries.Count)
        {
            CborValue? key = entries[index].Key;
            if (key != null
                && key.Kind == CborValueKind.TextString
                && key.TextStringValue == name)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    internal BigInteger ParseDecimalInteger(string text, bool canonical, string path)
    {
        if (text.Length == 0)
        {
            SetFailure(
                "CBOR_JSON_INTEGER_TEXT_INVALID",
                "The decimal integer text is empty.",
                -1,
                path
            );
            return BigInteger.Zero;
        }
        int index = 0;
        bool negative = false;
        if (text[0] == '-')
        {
            negative = true;
            index = 1;
        }
        if (index >= text.Length)
        {
            SetFailure(
                "CBOR_JSON_INTEGER_TEXT_INVALID",
                "The decimal integer requires at least one digit.",
                -1,
                path
            );
            return BigInteger.Zero;
        }
        if (canonical && text[index] == '0'
            && (index + 1 < text.Length || negative))
        {
            SetFailure(
                "CBOR_JSON_INTEGER_TEXT_INVALID",
                "The preserved decimal integer is not canonical.",
                -1,
                path
            );
            return BigInteger.Zero;
        }
        BigInteger value = BigInteger.Zero;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                SetFailure(
                    "CBOR_JSON_INTEGER_TEXT_INVALID",
                    "The decimal integer contains a non-digit character.",
                    -1,
                    path
                );
                return BigInteger.Zero;
            }
            value = value * 10 + (character - '0');
            index++;
        }
        return negative ? -value : value;
    }

    internal byte[]? DecodeBase64Url(string text, string path)
    {
        int remainder = text.Length % 4;
        if (remainder == 1)
        {
            SetFailure(
                "CBOR_JSON_BASE64URL_INVALID",
                "The base64url string length is invalid.",
                -1,
                path
            );
            return null;
        }
        long outputLength = (long)text.Length * 6 / 8;
        if (outputLength > int.MaxValue)
        {
            SetFailure(
                "CBOR_JSON_BASE64URL_TOO_LARGE",
                "The decoded base64url byte string is too large.",
                -1,
                path
            );
            return null;
        }
        byte[] output = new byte[(int)outputLength];
        int buffer = 0;
        int bitCount = 0;
        int outputIndex = 0;
        int index = 0;
        while (index < text.Length)
        {
            int digit = Base64UrlValue(text[index]);
            if (digit < 0)
            {
                SetFailure(
                    "CBOR_JSON_BASE64URL_INVALID",
                    "The base64url string contains an invalid character.",
                    -1,
                    path
                );
                return null;
            }
            buffer = buffer * 64 + digit;
            bitCount = bitCount + 6;
            if (bitCount >= 8)
            {
                bitCount = bitCount - 8;
                output[outputIndex] = (byte)(buffer / (1 << bitCount));
                outputIndex++;
                buffer = buffer % (1 << bitCount);
            }
            index++;
        }
        if (buffer != 0)
        {
            SetFailure(
                "CBOR_JSON_BASE64URL_INVALID",
                "The base64url string has non-zero trailing bits.",
                -1,
                path
            );
            return null;
        }
        return output;
    }

    internal int Base64UrlValue(char value)
    {
        if (value >= 'A' && value <= 'Z')
        {
            return value - 'A';
        }
        if (value >= 'a' && value <= 'z')
        {
            return value - 'a' + 26;
        }
        if (value >= '0' && value <= '9')
        {
            return value - '0' + 52;
        }
        if (value == '-')
        {
            return 62;
        }
        if (value == '_')
        {
            return 63;
        }
        return -1;
    }

    internal CborValue CreateInteger(BigInteger value)
    {
        CborValue result = new CborValue();
        result.Kind = value < BigInteger.Zero
            ? CborValueKind.NegativeInteger
            : CborValueKind.UnsignedInteger;
        result.IntegerValue = value;
        return result;
    }

    internal CborValue CreateFloat(double value)
    {
        CborFloatValue floatValue = new CborFloatValue();
        floatValue.Value = value;
        floatValue.Width = CborFloatWidth.Double;
        floatValue.RawBits = System.BitConverter.DoubleToUInt64Bits(value);
        floatValue.IsNaN = double.IsNaN(value);
        CborValue result = new CborValue();
        result.Kind = CborValueKind.Float;
        result.FloatValue = floatValue;
        result.FloatWidth = CborFloatWidth.Double;
        return result;
    }

    internal CborValue CreateSimple(byte value)
    {
        CborValue result = new CborValue();
        result.Kind = CborValueKind.SimpleValue;
        result.SimpleValue = value;
        return result;
    }

    internal void SetFailure(string code, string message, int offset, string path)
    {
        if (ErrorCode == null)
        {
            ErrorCode = code;
            ErrorMessage = message;
            ErrorOffset = offset;
            ErrorPath = path;
        }
    }

    internal CborReadResult CreateResult()
    {
        CborReadResult result = new CborReadResult();
        result.Status = CborReadStatus.Error;
        result.Consumed = 0;
        result.Remaining = Source == null ? 0 : Source.Length;
        result.SourceOffset = Source == null ? -1 : 0;
        result.ErrorCode = ErrorCode ?? "CBOR_JSON_PARSE_FAILED";
        result.ErrorMessage = ErrorMessage ?? "The JSON value could not be converted to CBOR.";
        result.ErrorOffset = ErrorOffset;
        result.ErrorPath = ErrorPath ?? "$";
        return result;
    }
}
