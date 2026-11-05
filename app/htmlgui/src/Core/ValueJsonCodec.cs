using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;

namespace htmlgui;

internal class UiValueTraversalState
{
    internal int MaximumDepth;
    internal int MaximumCollectionCount;
    internal int MaximumStringLength;
    internal int MaximumBinaryLength;
    internal int CollectionCount;
    internal List<UiValue> Ancestors;

    internal UiValueTraversalState(int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        MaximumDepth = maximumDepth;
        MaximumCollectionCount = maximumCollectionCount;
        MaximumStringLength = maximumStringLength;
        MaximumBinaryLength = maximumBinaryLength;
        CollectionCount = 0;
        Ancestors = new List<UiValue>();
    }
}

internal static class ValueJsonCodec
{
    internal static void ValidateValue(UiValue? value, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        ValidateLimits(maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        UiValueTraversalState state = new UiValueTraversalState(maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        ValidateNode(value, 0, state);
    }

    internal static UiValue CloneValue(UiValue? value, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        ValidateValue(value, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        return CloneNode(value);
    }

    internal static bool ValuesEqual(UiValue? left, UiValue? right, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        ValidateValue(left, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        ValidateValue(right, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        return CompareNodes(left, right);
    }

    internal static string SerializeValue(UiValue? value, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        ValidateValue(value, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        using (MemoryStream stream = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(stream))
            {
                WriteNode(writer, value);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    internal static UiValue DeserializeValue(string? json, int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        string checkedJson = Guard.ArgumentNotEmpty(json, "json");
        ValidateLimits(maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
        JsonDocumentOptions options = new JsonDocumentOptions();
        options.MaxDepth = maximumDepth * 2 + 4;
        using (JsonDocument document = JsonDocument.Parse(checkedJson, options))
        {
            UiValueTraversalState state = new UiValueTraversalState(maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
            UiValue result = ReadNode(document.RootElement, 0, state);
            ValidateValue(result, maximumDepth, maximumCollectionCount, maximumStringLength, maximumBinaryLength);
            return result;
        }
    }

    internal static void ValidateLimits(int maximumDepth, int maximumCollectionCount, int maximumStringLength, int maximumBinaryLength)
    {
        Guard.ArgumentInRange(maximumDepth, 0, 128, "maximumDepth");
        Guard.ArgumentNonNegative(maximumCollectionCount, "maximumCollectionCount");
        Guard.ArgumentNonNegative(maximumStringLength, "maximumStringLength");
        Guard.ArgumentNonNegative(maximumBinaryLength, "maximumBinaryLength");
    }

    internal static void ValidateNode(UiValue? value, int depth, UiValueTraversalState state)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (depth > state.MaximumDepth)
        {
            throw new ArgumentException("The value exceeds the maximum nesting depth.", "value");
        }

        EnterNode(value, state);
        ValidateShape(value, state);
        if (value.Kind == UiValueKind.Array)
        {
            UiValue[] items = value.ArrayValue ?? Array.Empty<UiValue>();
            AddCollectionItems(items.Length, state);
            int index = 0;
            while (index < items.Length)
            {
                ValidateNode(items[index], depth + 1, state);
                index++;
            }
        }
        else if (value.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = value.ObjectValue ?? Array.Empty<UiValueEntry>();
            AddCollectionItems(entries.Length, state);
            int index = 0;
            while (index < entries.Length)
            {
                UiValueEntry entry = entries[index];
                if (entry == null)
                {
                    throw new ArgumentException("Object entries cannot contain null.", "value");
                }

                if (entry.Key == null)
                {
                    throw new ArgumentException("Object entry keys cannot be null.", "value");
                }

                ValidateText(entry.Key, state);
                int comparisonIndex = 0;
                while (comparisonIndex < index)
                {
                    if (string.Equals(entries[comparisonIndex].Key, entry.Key, StringComparison.Ordinal))
                    {
                        throw new ArgumentException("Object entry keys must be unique.", "value");
                    }

                    comparisonIndex++;
                }

                ValidateNode(entry.Value, depth + 1, state);
                index++;
            }
        }

        LeaveNode(state);
    }

    internal static void ValidateShape(UiValue value, UiValueTraversalState state)
    {
        bool hasReferenceValue = value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null;
        bool hasPrimitiveValue = value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m;
        switch (value.Kind)
        {
            case UiValueKind.Null:
                if (hasReferenceValue || hasPrimitiveValue)
                {
                    throw new ArgumentException("A null value cannot carry tagged data.", "value");
                }

                break;
            case UiValueKind.Boolean:
                if (hasReferenceValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m)
                {
                    throw new ArgumentException("The value contains data outside its Boolean tag.", "value");
                }

                break;
            case UiValueKind.Integer:
                if (hasReferenceValue || value.BooleanValue || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m)
                {
                    throw new ArgumentException("The value contains data outside its integer tag.", "value");
                }

                break;
            case UiValueKind.FloatingPoint:
                if (hasReferenceValue || value.BooleanValue || value.IntegerValue != 0L || value.DecimalValue != 0m)
                {
                    throw new ArgumentException("The value contains data outside its floating-point tag.", "value");
                }

                if (double.IsNaN(value.FloatingPointValue) || double.IsInfinity(value.FloatingPointValue))
                {
                    throw new ArgumentOutOfRangeException("value", value.FloatingPointValue, "Floating-point values must be finite.");
                }

                break;
            case UiValueKind.Decimal:
                if (hasReferenceValue || value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0)
                {
                    throw new ArgumentException("The value contains data outside its decimal tag.", "value");
                }

                break;
            case UiValueKind.String:
                if (value.StringValue == null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    throw new ArgumentException("The value does not match its string tag.", "value");
                }

                ValidateText(value.StringValue, state);
                break;
            case UiValueKind.DateTime:
                if (value.StringValue != null || value.DateTimeValue == null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    throw new ArgumentException("The value does not match its date-time tag.", "value");
                }

                ValidateText(value.DateTimeValue, state);
                break;
            case UiValueKind.Binary:
                if (value.StringValue != null || value.DateTimeValue != null || value.BinaryValue == null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    throw new ArgumentException("The value does not match its binary tag.", "value");
                }

                if (value.BinaryValue.Length > state.MaximumBinaryLength)
                {
                    throw new ArgumentException("The binary value exceeds the maximum length.", "value");
                }

                break;
            case UiValueKind.Array:
                if (value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue == null || value.ObjectValue != null || hasPrimitiveValue)
                {
                    throw new ArgumentException("The value does not match its array tag.", "value");
                }

                break;
            case UiValueKind.Object:
                if (value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue == null || hasPrimitiveValue)
                {
                    throw new ArgumentException("The value does not match its object tag.", "value");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException("value", value.Kind, "The value kind is not supported.");
        }
    }

    internal static void ValidateText(string value, UiValueTraversalState state)
    {
        if (value.Length > state.MaximumStringLength)
        {
            throw new ArgumentException("A text value exceeds the maximum length.", "value");
        }
    }

    internal static void EnterNode(UiValue value, UiValueTraversalState state)
    {
        int index = 0;
        while (index < state.Ancestors.Count)
        {
            if (object.ReferenceEquals(state.Ancestors[index], value))
            {
                throw new ArgumentException("The value graph cannot contain a cycle.", "value");
            }

            index++;
        }

        state.Ancestors.Add(value);
    }

    internal static void LeaveNode(UiValueTraversalState state)
    {
        state.Ancestors.RemoveAt(state.Ancestors.Count - 1);
    }

    internal static void AddCollectionItems(int count, UiValueTraversalState state)
    {
        if (count > state.MaximumCollectionCount - state.CollectionCount)
        {
            throw new ArgumentException("The value exceeds the maximum aggregate collection count.", "value");
        }

        state.CollectionCount += count;
    }

    internal static UiValue CloneNode(UiValue? value)
    {
        UiValue source = value ?? new UiValue();
        UiValue result = new UiValue();
        result.Kind = source.Kind;
        result.BooleanValue = source.BooleanValue;
        result.IntegerValue = source.IntegerValue;
        result.FloatingPointValue = source.FloatingPointValue;
        result.DecimalValue = source.DecimalValue;
        result.StringValue = source.StringValue;
        result.DateTimeValue = source.DateTimeValue;
        if (source.BinaryValue != null)
        {
            result.BinaryValue = new byte[source.BinaryValue.Length];
            Array.Copy(source.BinaryValue, result.BinaryValue, source.BinaryValue.Length);
        }

        if (source.ArrayValue != null)
        {
            result.ArrayValue = new UiValue[source.ArrayValue.Length];
            int index = 0;
            while (index < source.ArrayValue.Length)
            {
                result.ArrayValue[index] = CloneNode(source.ArrayValue[index]);
                index++;
            }
        }

        if (source.ObjectValue != null)
        {
            result.ObjectValue = new UiValueEntry[source.ObjectValue.Length];
            int index = 0;
            while (index < source.ObjectValue.Length)
            {
                UiValueEntry entry = new UiValueEntry();
                entry.Key = source.ObjectValue[index].Key;
                entry.Value = CloneNode(source.ObjectValue[index].Value);
                result.ObjectValue[index] = entry;
                index++;
            }
        }

        return result;
    }

    internal static bool CompareNodes(UiValue? left, UiValue? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Kind != right.Kind)
        {
            return false;
        }

        switch (left.Kind)
        {
            case UiValueKind.Null:
                return true;
            case UiValueKind.Boolean:
                return left.BooleanValue == right.BooleanValue;
            case UiValueKind.Integer:
                return left.IntegerValue == right.IntegerValue;
            case UiValueKind.FloatingPoint:
                return left.FloatingPointValue == right.FloatingPointValue;
            case UiValueKind.Decimal:
                return left.DecimalValue == right.DecimalValue;
            case UiValueKind.String:
                return string.Equals(left.StringValue, right.StringValue, StringComparison.Ordinal);
            case UiValueKind.DateTime:
                return string.Equals(left.DateTimeValue, right.DateTimeValue, StringComparison.Ordinal);
            case UiValueKind.Binary:
                return CompareBytes(left.BinaryValue, right.BinaryValue);
            case UiValueKind.Array:
                return CompareArrays(left.ArrayValue, right.ArrayValue);
            case UiValueKind.Object:
                return CompareObjects(left.ObjectValue, right.ObjectValue);
            default:
                return false;
        }
    }

    internal static bool CompareBytes(byte[]? left, byte[]? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Length != right.Length)
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

            index++;
        }

        return true;
    }

    internal static bool CompareArrays(UiValue[]? left, UiValue[]? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!CompareNodes(left[index], right[index]))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal static bool CompareObjects(UiValueEntry[]? left, UiValueEntry[]? right)
    {
        if (left == null || right == null)
        {
            return left == right;
        }

        if (left.Length != right.Length)
        {
            return false;
        }

        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            int rightIndex = 0;
            bool found = false;
            while (rightIndex < right.Length)
            {
                if (string.Equals(left[leftIndex].Key, right[rightIndex].Key, StringComparison.Ordinal))
                {
                    if (!CompareNodes(left[leftIndex].Value, right[rightIndex].Value))
                    {
                        return false;
                    }

                    found = true;
                    break;
                }

                rightIndex++;
            }

            if (!found)
            {
                return false;
            }

            leftIndex++;
        }

        return true;
    }

    internal static void WriteNode(Utf8JsonWriter writer, UiValue? value)
    {
        UiValue source = value ?? new UiValue();
        writer.WriteStartObject();
        writer.WriteString("kind", GetKindToken(source.Kind));
        switch (source.Kind)
        {
            case UiValueKind.Null:
                break;
            case UiValueKind.Boolean:
                writer.WriteBoolean("value", source.BooleanValue);
                break;
            case UiValueKind.Integer:
                writer.WriteNumber("value", source.IntegerValue);
                break;
            case UiValueKind.FloatingPoint:
                writer.WritePropertyName("value");
                if (source.FloatingPointValue == 0.0)
                {
                    writer.WriteNumberValue(0.0);
                }
                else
                {
                    writer.WriteNumberValue(source.FloatingPointValue);
                }

                break;
            case UiValueKind.Decimal:
                writer.WriteString("value", InvariantFormatter.FormatDecimal(source.DecimalValue));
                break;
            case UiValueKind.String:
                writer.WriteString("value", source.StringValue);
                break;
            case UiValueKind.DateTime:
                writer.WriteString("value", source.DateTimeValue);
                break;
            case UiValueKind.Binary:
                writer.WriteString("value", Convert.ToBase64String(source.BinaryValue ?? Array.Empty<byte>()));
                break;
            case UiValueKind.Array:
                writer.WriteStartArray("value");
                UiValue[] items = source.ArrayValue ?? Array.Empty<UiValue>();
                int itemIndex = 0;
                while (itemIndex < items.Length)
                {
                    WriteNode(writer, items[itemIndex]);
                    itemIndex++;
                }

                writer.WriteEndArray();
                break;
            case UiValueKind.Object:
                WriteObject(writer, source.ObjectValue);
                break;
            default:
                break;
        }

        writer.WriteEndObject();
    }

    internal static void WriteObject(Utf8JsonWriter writer, UiValueEntry[]? entries)
    {
        writer.WriteStartObject("value");
        UiValueEntry[] sorted = SortEntries(entries ?? Array.Empty<UiValueEntry>());
        int index = 0;
        while (index < sorted.Length)
        {
            writer.WritePropertyName(sorted[index].Key);
            WriteNode(writer, sorted[index].Value);
            index++;
        }

        writer.WriteEndObject();
    }

    internal static UiValueEntry[] SortEntries(UiValueEntry[] entries)
    {
        UiValueEntry[] result = new UiValueEntry[entries.Length];
        Array.Copy(entries, result, entries.Length);
        int index = 1;
        while (index < result.Length)
        {
            UiValueEntry candidate = result[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(result[insertionIndex].Key, candidate.Key) > 0)
            {
                result[insertionIndex + 1] = result[insertionIndex];
                insertionIndex--;
            }

            result[insertionIndex + 1] = candidate;
            index++;
        }

        return result;
    }

    internal static string GetKindToken(UiValueKind kind)
    {
        switch (kind)
        {
            case UiValueKind.Null:
                return "null";
            case UiValueKind.Boolean:
                return "boolean";
            case UiValueKind.Integer:
                return "integer";
            case UiValueKind.FloatingPoint:
                return "floatingPoint";
            case UiValueKind.Decimal:
                return "decimal";
            case UiValueKind.String:
                return "string";
            case UiValueKind.DateTime:
                return "dateTime";
            case UiValueKind.Binary:
                return "binary";
            case UiValueKind.Array:
                return "array";
            case UiValueKind.Object:
                return "object";
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The value kind is not supported.");
        }
    }

    internal static UiValue ReadNode(JsonElement element, int depth, UiValueTraversalState state)
    {
        if (depth > state.MaximumDepth)
        {
            throw new JsonException("The value exceeds the maximum nesting depth.");
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("A tagged UI value must be a JSON object.");
        }

        bool hasKind = false;
        bool hasValue = false;
        string kindToken = string.Empty;
        JsonElement payload = default(JsonElement);
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, "kind", StringComparison.Ordinal))
            {
                if (hasKind || property.Value.ValueKind != JsonValueKind.String)
                {
                    throw new JsonException("The kind member must be one string.");
                }

                string? parsedKind = property.Value.GetString();
                kindToken = parsedKind ?? string.Empty;
                hasKind = true;
            }
            else if (string.Equals(property.Name, "value", StringComparison.Ordinal))
            {
                if (hasValue)
                {
                    throw new JsonException("The value member cannot be repeated.");
                }

                payload = property.Value;
                hasValue = true;
            }
            else
            {
                throw new JsonException("A tagged UI value contains an unknown member.");
            }
        }

        if (!hasKind)
        {
            throw new JsonException("A tagged UI value requires a kind member.");
        }

        UiValueKind kind = ParseKindToken(kindToken);
        if (kind == UiValueKind.Null)
        {
            if (hasValue)
            {
                throw new JsonException("A null tagged value cannot contain a value member.");
            }

            return new UiValue();
        }

        if (!hasValue)
        {
            throw new JsonException("A non-null tagged value requires a value member.");
        }

        return ReadPayload(kind, payload, depth, state);
    }

    internal static UiValue ReadPayload(UiValueKind kind, JsonElement payload, int depth, UiValueTraversalState state)
    {
        UiValue result = new UiValue();
        result.Kind = kind;
        switch (kind)
        {
            case UiValueKind.Boolean:
                RequireJsonKind(payload, JsonValueKind.True, JsonValueKind.False);
                result.BooleanValue = payload.GetBoolean();
                break;
            case UiValueKind.Integer:
                RequireJsonKind(payload, JsonValueKind.Number, JsonValueKind.Undefined);
                result.IntegerValue = payload.GetInt64();
                break;
            case UiValueKind.FloatingPoint:
                RequireJsonKind(payload, JsonValueKind.Number, JsonValueKind.Undefined);
                result.FloatingPointValue = payload.GetDouble();
                if (result.FloatingPointValue == 0.0)
                {
                    result.FloatingPointValue = 0.0;
                }

                break;
            case UiValueKind.Decimal:
                result.DecimalValue = decimal.Parse(ReadBoundedString(payload, state), NumberStyles.Float, CultureInfo.InvariantCulture);
                break;
            case UiValueKind.String:
                result.StringValue = ReadBoundedString(payload, state);
                break;
            case UiValueKind.DateTime:
                result.DateTimeValue = ReadBoundedString(payload, state);
                break;
            case UiValueKind.Binary:
                string encoded = ReadBoundedBinaryText(payload, state);
                result.BinaryValue = Convert.FromBase64String(encoded);
                if (result.BinaryValue.Length > state.MaximumBinaryLength)
                {
                    throw new JsonException("The decoded binary value exceeds the maximum length.");
                }

                break;
            case UiValueKind.Array:
                RequireJsonKind(payload, JsonValueKind.Array, JsonValueKind.Undefined);
                List<UiValue> items = new List<UiValue>();
                foreach (JsonElement item in payload.EnumerateArray())
                {
                    AddCollectionItemsForJson(1, state);
                    items.Add(ReadNode(item, depth + 1, state));
                }

                result.ArrayValue = items.ToArray();
                break;
            case UiValueKind.Object:
                RequireJsonKind(payload, JsonValueKind.Object, JsonValueKind.Undefined);
                List<UiValueEntry> entries = new List<UiValueEntry>();
                foreach (JsonProperty property in payload.EnumerateObject())
                {
                    ValidateJsonText(property.Name, state);
                    if (ContainsEntryKey(entries, property.Name))
                    {
                        throw new JsonException("Object entry keys must be unique.");
                    }

                    AddCollectionItemsForJson(1, state);
                    UiValueEntry entry = new UiValueEntry();
                    entry.Key = property.Name;
                    entry.Value = ReadNode(property.Value, depth + 1, state);
                    entries.Add(entry);
                }

                result.ObjectValue = entries.ToArray();
                break;
            default:
                throw new JsonException("The value kind is not supported.");
        }

        return result;
    }

    internal static void RequireJsonKind(JsonElement element, JsonValueKind first, JsonValueKind second)
    {
        if (element.ValueKind != first && element.ValueKind != second)
        {
            throw new JsonException("The JSON payload does not match its tagged value kind.");
        }
    }

    internal static string ReadBoundedString(JsonElement element, UiValueTraversalState state)
    {
        RequireJsonKind(element, JsonValueKind.String, JsonValueKind.Undefined);
        string? value = element.GetString();
        string result = value ?? string.Empty;
        ValidateJsonText(result, state);
        return result;
    }

    internal static string ReadBoundedBinaryText(JsonElement element, UiValueTraversalState state)
    {
        RequireJsonKind(element, JsonValueKind.String, JsonValueKind.Undefined);
        string? value = element.GetString();
        string result = value ?? string.Empty;
        long maximumEncodedLength = ((long)state.MaximumBinaryLength + 2L) / 3L * 4L;
        if ((long)result.Length > maximumEncodedLength)
        {
            throw new JsonException("The encoded binary value exceeds the maximum length.");
        }

        return result;
    }

    internal static void ValidateJsonText(string value, UiValueTraversalState state)
    {
        if (value.Length > state.MaximumStringLength)
        {
            throw new JsonException("A text value exceeds the maximum length.");
        }
    }

    internal static bool ContainsEntryKey(List<UiValueEntry> entries, string key)
    {
        int index = 0;
        while (index < entries.Count)
        {
            if (string.Equals(entries[index].Key, key, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal static void AddCollectionItemsForJson(int count, UiValueTraversalState state)
    {
        if (count > state.MaximumCollectionCount - state.CollectionCount)
        {
            throw new JsonException("The value exceeds the maximum aggregate collection count.");
        }

        state.CollectionCount += count;
    }

    internal static UiValueKind ParseKindToken(string token)
    {
        switch (token)
        {
            case "null":
                return UiValueKind.Null;
            case "boolean":
                return UiValueKind.Boolean;
            case "integer":
                return UiValueKind.Integer;
            case "floatingPoint":
                return UiValueKind.FloatingPoint;
            case "decimal":
                return UiValueKind.Decimal;
            case "string":
                return UiValueKind.String;
            case "dateTime":
                return UiValueKind.DateTime;
            case "binary":
                return UiValueKind.Binary;
            case "array":
                return UiValueKind.Array;
            case "object":
                return UiValueKind.Object;
            default:
                throw new JsonException("The tagged UI value kind is not supported.");
        }
    }
}
