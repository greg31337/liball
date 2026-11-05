using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace cbor;

internal class CborDiagnosticFormatterCore
{
    internal CborDiagnosticOptions? Options;
    internal StringBuilder Output = new StringBuilder();
    internal List<CborValue> ActiveValues = new List<CborValue>();

    internal CborDiagnosticFormatterCore(CborDiagnosticOptions? options)
    {
        Options = options;
    }

    internal string Format(CborValue? value)
    {
        ValidateOptions();
        if (value == null)
        {
            Fail(
                "CBOR_VALUE_MISSING",
                "A CBOR value is required for diagnostic formatting.",
                "$"
            );
        }
        CborValue formattedValue = value ?? new CborValue();
        ValidateRepresentation(formattedValue, "$" );
        AppendValue(formattedValue, 0, "$", true);
        return Output.ToString();
    }

    internal string FormatSequence(List<CborValue>? values)
    {
        ValidateOptions();
        if (values == null)
        {
            Fail(
                "CBOR_DIAGNOSTIC_SEQUENCE_VALUES_MISSING",
                "A diagnostic sequence value collection is required.",
                "$"
            );
        }
        List<CborValue> items = values ?? new List<CborValue>();
        int index = 0;
        while (index < items.Count)
        {
            if (index > 0)
            {
                AppendLimited(",", "$" );
                if (Options != null
                    && Options.Style == CborDiagnosticStyle.Indented)
                {
                    AppendLimited("\n", "$" );
                }
            }
            if (items[index] == null)
            {
                Fail(
                    "CBOR_VALUE_MISSING",
                    "The diagnostic sequence contains a missing value.",
                    $"$[{index}]"
                );
            }
            ValidateRepresentation(items[index], $"$[{index}]");
            AppendValue(items[index], 0, $"$[{index}]", true);
            index = index + 1;
        }
        return Output.ToString();
    }

    internal void ValidateOptions()
    {
        if (Options == null)
        {
            Fail(
                "CBOR_DIAGNOSTIC_OPTIONS_MISSING",
                "Diagnostic formatting options are required.",
                "$"
            );
        }
        int style = Options == null ? -1 : (int)Options.Style;
        if (style < (int)CborDiagnosticStyle.Compact
            || style > (int)CborDiagnosticStyle.Indented)
        {
            Fail(
                "CBOR_DIAGNOSTIC_STYLE_INVALID",
                "The diagnostic formatting style is not defined.",
                "$"
            );
        }
        if (Options != null && Options.IndentSize < 0)
        {
            Fail(
                "CBOR_DIAGNOSTIC_INDENT_INVALID",
                "The diagnostic indentation size cannot be negative.",
                "$"
            );
        }
        if (Options != null && Options.MaxNestingDepth < 0)
        {
            Fail(
                "CBOR_DIAGNOSTIC_DEPTH_INVALID",
                "The diagnostic nesting limit cannot be negative.",
                "$"
            );
        }
        if (Options != null && Options.MaxCharacters < 0)
        {
            Fail(
                "CBOR_DIAGNOSTIC_CHARACTER_LIMIT_INVALID",
                "The diagnostic character limit cannot be negative.",
                "$"
            );
        }
        int mapOrder = Options == null ? -1 : (int)Options.MapKeyOrder;
        if (mapOrder < (int)CborMapKeyOrder.Preserve
            || mapOrder > (int)CborMapKeyOrder.LengthFirst)
        {
            Fail(
                "CBOR_DIAGNOSTIC_MAP_KEY_ORDER_INVALID",
                "The diagnostic map-key ordering policy is not defined.",
                "$"
            );
        }
    }

    internal void AppendValue(
        CborValue value,
        int depth,
        string path,
        bool trackIdentity)
    {
        if (Options == null)
        {
            return;
        }
        if (depth > Options.MaxNestingDepth)
        {
            Fail(
                "CBOR_DIAGNOSTIC_MAX_NESTING_DEPTH",
                "The value exceeds the diagnostic nesting-depth limit.",
                path
            );
        }
        if (trackIdentity && IsActive(value))
        {
            Fail(
                "CBOR_DIAGNOSTIC_CYCLE",
                "A cyclic value graph cannot be represented by basic diagnostic notation.",
                path
            );
        }
        if (trackIdentity)
        {
            ActiveValues.Add(value);
        }
        switch (value.Kind)
        {
            case CborValueKind.UnsignedInteger:
                AppendInteger(value, false, path);
                break;
            case CborValueKind.NegativeInteger:
                AppendInteger(value, true, path);
                break;
            case CborValueKind.ByteString:
                AppendByteString(value, path);
                break;
            case CborValueKind.TextString:
                AppendTextString(value, path);
                break;
            case CborValueKind.Array:
                AppendArray(value, depth, path);
                break;
            case CborValueKind.Map:
                AppendMap(value, depth, path);
                break;
            case CborValueKind.Tag:
                AppendTag(value, depth, path);
                break;
            case CborValueKind.Float:
                AppendFloat(value, path);
                break;
            case CborValueKind.SimpleValue:
                AppendSimple(value, path);
                break;
            default:
                Fail(
                    "CBOR_VALUE_KIND_INVALID",
                    "The value kind cannot be formatted as diagnostic notation.",
                    path
                );
                break;
        }
        if (value.Kind != CborValueKind.Array
            && value.Kind != CborValueKind.Map)
        {
            AppendTrailingRepresentation(value, path);
        }
        if (trackIdentity)
        {
            ActiveValues.RemoveAt(ActiveValues.Count - 1);
        }
    }

    internal void AppendInteger(CborValue value, bool negative, string path)
    {
        BigInteger integer = value.IntegerValue;
        if ((!negative && integer < BigInteger.Zero)
            || (negative && integer >= BigInteger.Zero))
        {
            Fail(
                negative
                    ? "CBOR_NEGATIVE_INTEGER_OUT_OF_RANGE"
                    : "CBOR_UNSIGNED_INTEGER_OUT_OF_RANGE",
                "The integer sign does not match its selected CBOR value kind.",
                path
            );
        }
        AppendLimited(integer.ToString(CultureInfo.InvariantCulture), path);
    }

    internal void AppendByteString(CborValue value, string path)
    {
        if (value.ByteStringValue == null)
        {
            Fail(
                "CBOR_BYTE_STRING_VALUE_MISSING",
                "The byte-string value is missing.",
                path
            );
        }
        if (ShouldFormatChunks(value))
        {
            AppendByteStringChunks(value, path);
            return;
        }
        AppendLimited("h'", path);
        int remaining = Options == null
            ? 0
            : Options.MaxCharacters - Output.Length;
        AppendLimited(
            CborDiagnosticEscapes.FormatBytes(
                value.ByteStringValue ?? [],
                remaining,
                path
            ),
            path
        );
        AppendLimited("'", path);
    }

    internal void AppendTextString(CborValue value, string path)
    {
        if (value.TextStringValue == null)
        {
            Fail(
                "CBOR_TEXT_STRING_VALUE_MISSING",
                "The text-string value is missing.",
                path
            );
        }
        string text = value.TextStringValue ?? "";
        if (!CborTokenWritePlan.IsWellFormedUtf16(text))
        {
            Fail(
                "CBOR_TEXT_STRING_INVALID_UTF16",
                "The text string contains an unpaired UTF-16 surrogate.",
                path
            );
        }
        if (ShouldFormatChunks(value))
        {
            AppendTextStringChunks(value, path);
            return;
        }
        AppendLimited("\"", path);
        int remaining = Options == null
            ? 0
            : Options.MaxCharacters - Output.Length;
        AppendLimited(
            CborDiagnosticEscapes.EscapeText(text, remaining, path),
            path
        );
        AppendLimited("\"", path);
    }

    internal void AppendArray(CborValue value, int depth, string path)
    {
        if (value.ArrayItems == null)
        {
            Fail(
                "CBOR_ARRAY_ITEMS_MISSING",
                "The array item collection is missing.",
                path
            );
        }
        List<CborValue> items = value.ArrayItems ?? new List<CborValue>();
        AppendLimited("[", path);
        AppendContainerRepresentation(value, items.Count, path);
        int index = 0;
        while (index < items.Count)
        {
            if (items[index] == null)
            {
                Fail(
                    "CBOR_VALUE_MISSING",
                    "The array contains a missing value.",
                    $"{path}[{index}]"
                );
            }
            AppendCollectionPrefix(index, depth + 1, path);
            AppendValue(
                items[index],
                depth + 1,
                $"{path}[{index}]",
                true
            );
            index = index + 1;
        }
        AppendCollectionSuffix(items.Count, depth, path);
        AppendLimited("]", path);
    }

    internal void AppendMap(CborValue value, int depth, string path)
    {
        if (value.MapEntries == null)
        {
            Fail(
                "CBOR_MAP_ENTRIES_MISSING",
                "The map entry collection is missing.",
                path
            );
        }
        List<CborMapEntry> entries = value.MapEntries ?? new List<CborMapEntry>();
        List<CborDiagnosticMapItem> display = OrderMapEntries(entries, path);
        AppendLimited("{", path);
        AppendContainerRepresentation(value, entries.Count, path);
        int index = 0;
        while (index < display.Count)
        {
            CborMapEntry? entry = display[index].Entry;
            int sourceIndex = display[index].SourceIndex;
            if (entry == null)
            {
                Fail(
                    "CBOR_MAP_ENTRY_MISSING",
                    "The map contains a missing entry.",
                    $"{path}[{sourceIndex}]"
                );
            }
            if (entry != null && entry.Key == null)
            {
                Fail(
                    "CBOR_MAP_KEY_MISSING",
                    "The map entry key is missing.",
                    $"{path}[{sourceIndex}].key"
                );
            }
            if (entry != null && entry.Value == null)
            {
                Fail(
                    "CBOR_VALUE_MISSING",
                    "The map entry value is missing.",
                    $"{path}[{sourceIndex}].value"
                );
            }
            AppendCollectionPrefix(index, depth + 1, path);
            AppendValue(
                entry == null || entry.Key == null ? new CborValue() : entry.Key,
                depth + 1,
                $"{path}[{sourceIndex}].key",
                true
            );
            bool indented = Options != null
                && Options.Style == CborDiagnosticStyle.Indented;
            AppendLimited(indented ? ": " : ":", path);
            AppendValue(
                entry == null || entry.Value == null ? new CborValue() : entry.Value,
                depth + 1,
                $"{path}[{sourceIndex}].value",
                true
            );
            index = index + 1;
        }
        AppendCollectionSuffix(display.Count, depth, path);
        AppendLimited("}", path);
    }

    internal void AppendTag(CborValue value, int depth, string path)
    {
        if (value.TaggedValue == null)
        {
            Fail(
                "CBOR_TAG_CONTENT_MISSING",
                "The semantic tag content is missing.",
                path
            );
        }
        AppendLimited(value.TagNumber.ToString(CultureInfo.InvariantCulture), path);
        AppendLimited("(", path);
        AppendValue(
            value.TaggedValue ?? new CborValue(),
            depth + 1,
            $"{path}<tag:{value.TagNumber}>",
            true
        );
        AppendLimited(")", path);
    }

    internal void AppendFloat(CborValue value, string path)
    {
        if (value.FloatValue == null)
        {
            Fail(
                "CBOR_FLOAT_VALUE_MISSING",
                "The floating-point value is missing.",
                path
            );
        }
        double number = value.FloatValue == null ? 0.0 : value.FloatValue.Value;
        if (double.IsPositiveInfinity(number))
        {
            AppendLimited("Infinity", path);
            return;
        }
        if (double.IsNegativeInfinity(number))
        {
            AppendLimited("-Infinity", path);
            return;
        }
        if (double.IsNaN(number))
        {
            AppendLimited("NaN", path);
            return;
        }
        if (number == 0.0)
        {
            long bits = System.BitConverter.DoubleToInt64Bits(number);
            AppendLimited(bits < 0 ? "-0.0" : "0.0", path);
            return;
        }
        string roundTrip = number.ToString("R", CultureInfo.InvariantCulture);
        AppendLimited(NormalizeFloat(roundTrip), path);
    }

    internal string NormalizeFloat(string value)
    {
        int exponentIndex = value.IndexOf('E');
        if (exponentIndex < 0)
        {
            exponentIndex = value.IndexOf('e');
        }
        if (exponentIndex < 0)
        {
            return value.IndexOf('.') < 0 ? value + ".0" : value;
        }
        string mantissa = value.Substring(0, exponentIndex);
        if (mantissa.IndexOf('.') < 0)
        {
            mantissa = mantissa + ".0";
        }
        string exponentText = value.Substring(exponentIndex + 1);
        int exponent = int.Parse(exponentText, CultureInfo.InvariantCulture);
        string sign = exponent >= 0 ? "+" : "";
        return mantissa
            + "e"
            + sign
            + exponent.ToString(CultureInfo.InvariantCulture);
    }

    internal void AppendSimple(CborValue value, string path)
    {
        if (value.SimpleValue >= CborSimpleValues.FirstReserved
            && value.SimpleValue <= CborSimpleValues.LastReserved)
        {
            Fail(
                "CBOR_SIMPLE_VALUE_RESERVED",
                "Reserved simple values cannot be represented as valid CBOR.",
                path
            );
        }
        string formatted;
        switch (value.SimpleValue)
        {
            case CborSimpleValues.False:
                formatted = "false";
                break;
            case CborSimpleValues.True:
                formatted = "true";
                break;
            case CborSimpleValues.Null:
                formatted = "null";
                break;
            case CborSimpleValues.Undefined:
                formatted = "undefined";
                break;
            default:
                formatted = $"simple({value.SimpleValue})";
                break;
        }
        AppendLimited(formatted, path);
    }

    internal bool ShouldFormatChunks(CborValue value)
    {
        if (Options == null || !Options.IncludeRepresentation)
        {
            return false;
        }
        CborRepresentation? representation = value.Representation;
        CborLengthKind lengthKind = representation != null
            && representation.LengthKind != CborLengthKind.None
            ? representation.LengthKind
            : value.LengthKind;
        if (lengthKind != CborLengthKind.Indefinite)
        {
            return false;
        }
        if (representation != null && representation.ChunkByteLengths != null)
        {
            return representation.ChunkByteLengths.Count > 0;
        }
        int length = value.Kind == CborValueKind.ByteString
            ? value.ByteStringValue == null ? 0 : value.ByteStringValue.Length
            : Encoding.UTF8.GetByteCount(value.TextStringValue ?? "");
        return length > 0;
    }

    internal void AppendByteStringChunks(CborValue value, string path)
    {
        byte[] bytes = value.ByteStringValue ?? [];
        List<int> lengths = new List<int>();
        if (value.Representation != null
            && value.Representation.ChunkByteLengths != null)
        {
            lengths.AddRange(value.Representation.ChunkByteLengths);
        }
        else
        {
            lengths.Add(bytes.Length);
        }
        AppendLimited("(_ ", path);
        int sourceOffset = 0;
        int index = 0;
        while (index < lengths.Count)
        {
            if (index > 0)
            {
                AppendLimited(",", path);
            }
            int length = lengths[index];
            AppendLimited("h'", path);
            byte[] chunk = new byte[length];
            System.Array.Copy(bytes, sourceOffset, chunk, 0, length);
            int remaining = Options == null
                ? 0
                : Options.MaxCharacters - Output.Length;
            AppendLimited(
                CborDiagnosticEscapes.FormatBytes(chunk, remaining, path),
                path
            );
            AppendLimited("'", path);
            sourceOffset = sourceOffset + length;
            index = index + 1;
        }
        AppendLimited(")", path);
    }

    internal void AppendTextStringChunks(CborValue value, string path)
    {
        string text = value.TextStringValue ?? "";
        List<int> lengths = new List<int>();
        if (value.Representation != null
            && value.Representation.ChunkByteLengths != null)
        {
            lengths.AddRange(value.Representation.ChunkByteLengths);
        }
        else
        {
            lengths.Add(Encoding.UTF8.GetByteCount(text));
        }
        AppendLimited("(_ ", path);
        int characterOffset = 0;
        int index = 0;
        while (index < lengths.Count)
        {
            if (index > 0)
            {
                AppendLimited(",", path);
            }
            int characterCount = CountTextChunkCharacters(
                text,
                characterOffset,
                lengths[index],
                path
            );
            string chunk = text.Substring(characterOffset, characterCount);
            AppendLimited("\"", path);
            int remaining = Options == null
                ? 0
                : Options.MaxCharacters - Output.Length;
            AppendLimited(CborDiagnosticEscapes.EscapeText(chunk, remaining, path), path);
            AppendLimited("\"", path);
            characterOffset = characterOffset + characterCount;
            index = index + 1;
        }
        AppendLimited(")", path);
    }

    internal int CountTextChunkCharacters(
        string text,
        int start,
        int byteLength,
        string path)
    {
        int index = start;
        int bytes = 0;
        while (index < text.Length && bytes < byteLength)
        {
            char current = text[index];
            int scalarBytes;
            int characters = 1;
            if (current <= 0x7f)
            {
                scalarBytes = 1;
            }
            else if (current <= 0x7ff)
            {
                scalarBytes = 2;
            }
            else if (char.IsHighSurrogate(current))
            {
                scalarBytes = 4;
                characters = 2;
            }
            else
            {
                scalarBytes = 3;
            }
            if (bytes > byteLength - scalarBytes)
            {
                Fail(
                    "CBOR_REPRESENTATION_CHUNK_BOUNDARY_INVALID",
                    "A retained text chunk ends inside a UTF-8 scalar value.",
                    path
                );
            }
            bytes = bytes + scalarBytes;
            index = index + characters;
        }
        if (bytes != byteLength)
        {
            Fail(
                "CBOR_REPRESENTATION_CHUNK_LENGTH_MISMATCH",
                "The retained text chunk lengths do not match the text value.",
                path
            );
        }
        return index - start;
    }

    internal void AppendContainerRepresentation(
        CborValue value,
        int count,
        string path)
    {
        if (Options == null || !Options.IncludeRepresentation)
        {
            return;
        }
        CborRepresentation? representation = value.Representation;
        CborLengthKind lengthKind = representation != null
            && representation.LengthKind != CborLengthKind.None
            ? representation.LengthKind
            : value.LengthKind;
        string indicator = "";
        if (lengthKind == CborLengthKind.Indefinite)
        {
            indicator = "_";
        }
        else if (representation != null && representation.HeadArgumentWidth > 0)
        {
            indicator = FormatHeadIndicator(representation.HeadArgumentWidth, path);
        }
        if (indicator.Length > 0)
        {
            AppendLimited(indicator, path);
            if (count > 0 && Options.Style == CborDiagnosticStyle.Compact)
            {
                AppendLimited(" ", path);
            }
        }
    }

    internal void AppendTrailingRepresentation(CborValue value, string path)
    {
        if (Options == null || !Options.IncludeRepresentation)
        {
            return;
        }
        CborRepresentation? representation = value.Representation;
        if (value.Kind == CborValueKind.Float)
        {
            CborFloatWidth width = representation != null
                && representation.FloatWidth != CborFloatWidth.None
                ? representation.FloatWidth
                : value.FloatWidth;
            if (width == CborFloatWidth.Half)
            {
                AppendLimited("_1", path);
            }
            else if (width == CborFloatWidth.Single)
            {
                AppendLimited("_2", path);
            }
            else if (width == CborFloatWidth.Double)
            {
                AppendLimited("_3", path);
            }
            return;
        }
        if ((value.Kind == CborValueKind.ByteString
                || value.Kind == CborValueKind.TextString)
            && !ShouldFormatChunks(value))
        {
            CborLengthKind lengthKind = representation != null
                && representation.LengthKind != CborLengthKind.None
                ? representation.LengthKind
                : value.LengthKind;
            if (lengthKind == CborLengthKind.Indefinite)
            {
                AppendLimited("_", path);
                return;
            }
        }
        if (representation != null && representation.HeadArgumentWidth > 0)
        {
            AppendLimited(
                FormatHeadIndicator(representation.HeadArgumentWidth, path),
                path
            );
        }
    }

    internal string FormatHeadIndicator(int width, string path)
    {
        if (width == 1)
        {
            return "_0";
        }
        if (width == 2)
        {
            return "_1";
        }
        if (width == 4)
        {
            return "_2";
        }
        if (width == 8)
        {
            return "_3";
        }
        Fail(
            "CBOR_REPRESENTATION_HEAD_WIDTH_INVALID",
            "A retained CBOR head width must be one, two, four, or eight bytes.",
            path
        );
        return "";
    }

    internal void ValidateRepresentation(CborValue value, string path)
    {
        if (Options == null || !Options.IncludeRepresentation)
        {
            return;
        }
        CborEncodeOptions encoding = new CborEncodeOptions();
        encoding.PreserveRepresentation = true;
        encoding.MaxNestingDepth = Options.MaxNestingDepth;
        encoding.MaxTotalItems = int.MaxValue;
        encoding.MaxOutputBytes = long.MaxValue;
        CborSizeCalculator calculator = new CborSizeCalculator(encoding);
        if (!calculator.Calculate(value))
        {
            Fail(
                calculator.ErrorCode ?? "CBOR_REPRESENTATION_INVALID",
                calculator.ErrorMessage ?? "The retained CBOR representation is invalid.",
                CombinePath(path, calculator.ErrorPath)
            );
        }
    }

    internal List<CborDiagnosticMapItem> OrderMapEntries(
        List<CborMapEntry> entries,
        string path)
    {
        List<CborDiagnosticMapItem> ordered = new List<CborDiagnosticMapItem>();
        int sourceIndex = 0;
        while (sourceIndex < entries.Count)
        {
            CborDiagnosticMapItem item = new CborDiagnosticMapItem();
            item.Entry = entries[sourceIndex];
            item.SourceIndex = sourceIndex;
            if (Options != null && Options.MapKeyOrder != CborMapKeyOrder.Preserve)
            {
                CborValue? key = entries[sourceIndex] == null
                    ? null
                    : entries[sourceIndex].Key;
                if (key == null)
                {
                    Fail(
                        "CBOR_MAP_KEY_MISSING",
                        "The map entry key is missing.",
                        $"{path}[{sourceIndex}].key"
                    );
                }
                item.EncodedKey = EncodeDeterministicKey(
                    key ?? new CborValue(),
                    $"{path}[{sourceIndex}].key"
                );
            }
            int position = ordered.Count;
            ordered.Add(item);
            if (Options != null && Options.MapKeyOrder != CborMapKeyOrder.Preserve)
            {
                while (position > 0
                    && CompareMapItems(ordered[position - 1], item) > 0)
                {
                    ordered[position] = ordered[position - 1];
                    position = position - 1;
                }
                ordered[position] = item;
                if (position > 0
                    && CompareMapItems(ordered[position - 1], item) == 0)
                {
                    Fail(
                        "CBOR_DIAGNOSTIC_DETERMINISTIC_DUPLICATE_KEY",
                        "Deterministic diagnostic maps cannot contain equal encoded keys.",
                        $"{path}[{sourceIndex}].key"
                    );
                }
                if (position + 1 < ordered.Count
                    && CompareMapItems(item, ordered[position + 1]) == 0)
                {
                    Fail(
                        "CBOR_DIAGNOSTIC_DETERMINISTIC_DUPLICATE_KEY",
                        "Deterministic diagnostic maps cannot contain equal encoded keys.",
                        $"{path}[{sourceIndex}].key"
                    );
                }
            }
            sourceIndex = sourceIndex + 1;
        }
        return ordered;
    }

    internal byte[] EncodeDeterministicKey(CborValue key, string path)
    {
        CborEncodeOptions encoding = new CborEncodeOptions();
        encoding.EncodingProfile = Options != null
            && Options.MapKeyOrder == CborMapKeyOrder.LengthFirst
            ? CborEncodingProfile.LengthFirstCoreDeterministic
            : CborEncodingProfile.CoreDeterministic;
        encoding.MaxNestingDepth = Options == null ? 64 : Options.MaxNestingDepth;
        encoding.MaxTotalItems = int.MaxValue;
        encoding.MaxOutputBytes = long.MaxValue;
        CborSizeCalculator calculator = new CborSizeCalculator(encoding);
        if (!calculator.Calculate(key))
        {
            Fail(
                calculator.ErrorCode ?? "CBOR_DIAGNOSTIC_MAP_KEY_INVALID",
                calculator.ErrorMessage ?? "The map key cannot be deterministically encoded.",
                CombinePath(path, calculator.ErrorPath)
            );
        }
        if (calculator.RequiredBytes > int.MaxValue)
        {
            Fail(
                "CBOR_DIAGNOSTIC_MAP_KEY_SIZE_OUT_OF_RANGE",
                "The deterministic map-key encoding exceeds the supported size.",
                path
            );
        }
        byte[] encoded = new byte[(int)calculator.RequiredBytes];
        CborEncodingPlanWriter writer = new CborEncodingPlanWriter(encoding);
        if (!writer.WritePlan(calculator, encoded))
        {
            Fail(
                writer.ErrorCode ?? "CBOR_DIAGNOSTIC_MAP_KEY_INVALID",
                writer.ErrorMessage ?? "The map key cannot be deterministically encoded.",
                CombinePath(path, writer.ErrorPath)
            );
        }
        return encoded;
    }

    internal int CompareMapItems(
        CborDiagnosticMapItem left,
        CborDiagnosticMapItem right)
    {
        CborMapKeyOrder order = Options == null
            ? CborMapKeyOrder.Lexicographic
            : Options.MapKeyOrder;
        return CborKeyComparer.CompareDeterministicEncoding(
            left.EncodedKey ?? [],
            right.EncodedKey ?? [],
            order
        );
    }

    internal string CombinePath(string prefix, string? suffix)
    {
        if (suffix == null || suffix == "$")
        {
            return prefix;
        }
        if (suffix.Length > 0 && suffix[0] == '$')
        {
            return prefix + suffix.Substring(1);
        }
        return prefix;
    }

    internal void AppendCollectionPrefix(int index, int depth, string path)
    {
        if (Options == null)
        {
            return;
        }
        if (index > 0)
        {
            AppendLimited(",", path);
        }
        if (Options.Style == CborDiagnosticStyle.Indented)
        {
            AppendLimited("\n", path);
            AppendIndent(depth, path);
        }
    }

    internal void AppendCollectionSuffix(int count, int depth, string path)
    {
        if (Options != null
            && count > 0
            && Options.Style == CborDiagnosticStyle.Indented)
        {
            AppendLimited("\n", path);
            AppendIndent(depth, path);
        }
    }

    internal void AppendIndent(int depth, string path)
    {
        if (Options == null)
        {
            return;
        }
        int level = 0;
        while (level < depth)
        {
            int index = 0;
            while (index < Options.IndentSize)
            {
                AppendLimited(" ", path);
                index = index + 1;
            }
            level = level + 1;
        }
    }

    internal void AppendLimited(string value, string path)
    {
        if (Options == null)
        {
            return;
        }
        if (value.Length > Options.MaxCharacters - Output.Length)
        {
            Fail(
                "CBOR_DIAGNOSTIC_MAX_CHARACTERS",
                "The formatted diagnostic notation exceeds the character limit.",
                path
            );
        }
        Output.Append(value);
    }

    internal bool IsActive(CborValue value)
    {
        int index = 0;
        while (index < ActiveValues.Count)
        {
            if (object.ReferenceEquals(ActiveValues[index], value))
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    internal void Fail(string code, string message, string path)
    {
        throw new CborDiagnosticException(code, message, -1, path);
    }
}
