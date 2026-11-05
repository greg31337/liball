namespace uspto;

internal class JsonFieldValue
{
    internal bool IsPresent = false;
    internal bool IsNull = false;
    internal bool IsValid = false;
    internal string Path = "$";
    internal string? RawJson = null;
    internal string? StringValue = null;
    internal bool BooleanValue = false;
    internal int Int32Value = 0;
    internal long Int64Value = 0L;
    internal global::System.DateTimeOffset DateValue = default(global::System.DateTimeOffset);
    internal global::System.Text.Json.JsonElement[] ArrayItems = new global::System.Text.Json.JsonElement[0];
    internal string[] ArrayItemPaths = new string[0];
    internal global::System.Text.Json.JsonElement ElementValue = default(global::System.Text.Json.JsonElement);
    internal bool HasElementValue = false;
}

internal class JsonUnknownField
{
    internal string Name = string.Empty;
    internal string Path = "$";
    internal string RawJson = string.Empty;
    internal global::System.Text.Json.JsonElement Value = default(global::System.Text.Json.JsonElement);
}

internal class JsonLocatedField
{
    internal bool IsContainerValid = false;
    internal bool IsPresent = false;
    internal string Path = "$";
    internal global::System.Text.Json.JsonElement Value = default(global::System.Text.Json.JsonElement);
}

internal class JsonIntegerParseResult
{
    internal long Value = 0L;
    internal bool IsValid = false;
}

internal class JsonDateParseResult
{
    internal global::System.DateTimeOffset Value = default(global::System.DateTimeOffset);
    internal bool IsValid = false;
}

internal class JsonFieldReader
{
    internal global::System.Text.Json.JsonElement Element = default(global::System.Text.Json.JsonElement);
    internal string Path = "$";

    internal JsonFieldReader(global::System.Text.Json.JsonElement element, string path)
    {
        Element = element;
        if (!global::System.String.IsNullOrWhiteSpace(path))
        {
            Path = path;
        }
    }

    internal JsonFieldValue ReadString(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            result.StringValue = located.Value.GetString();
            result.IsValid = true;
        }
        else if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.Number || located.Value.ValueKind == global::System.Text.Json.JsonValueKind.True || located.Value.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            result.StringValue = located.Value.GetRawText();
            result.IsValid = true;
        }

        return result;
    }

    internal JsonFieldValue ReadBoolean(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.True)
        {
            result.BooleanValue = true;
            result.IsValid = true;
        }
        else if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.False)
        {
            result.BooleanValue = false;
            result.IsValid = true;
        }
        else
        {
            string? text = null;
            if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.String)
            {
                text = located.Value.GetString();
            }
            else if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.Number)
            {
                text = located.Value.GetRawText();
            }

            if (text != null)
            {
                string normalized = text.Trim();
                if (normalized.Equals("true", global::System.StringComparison.OrdinalIgnoreCase) || normalized == "1")
                {
                    result.BooleanValue = true;
                    result.IsValid = true;
                }
                else if (normalized.Equals("false", global::System.StringComparison.OrdinalIgnoreCase) || normalized == "0")
                {
                    result.BooleanValue = false;
                    result.IsValid = true;
                }
            }
        }

        return result;
    }

    internal JsonFieldValue ReadInt32(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        JsonIntegerParseResult parsed = ParseIntegerElement(located.Value);
        if (parsed.IsValid && parsed.Value >= global::System.Int32.MinValue && parsed.Value <= global::System.Int32.MaxValue)
        {
            result.Int32Value = (int)parsed.Value;
            result.Int64Value = parsed.Value;
            result.IsValid = true;
        }

        return result;
    }

    internal JsonFieldValue ReadInt64(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        JsonIntegerParseResult parsed = ParseIntegerElement(located.Value);
        if (parsed.IsValid)
        {
            result.Int64Value = parsed.Value;
            result.IsValid = true;
        }

        return result;
    }

    internal JsonFieldValue ReadArray(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        if (located.Value.ValueKind != global::System.Text.Json.JsonValueKind.Array)
        {
            return result;
        }

        global::System.Collections.Generic.List<global::System.Text.Json.JsonElement> items = new global::System.Collections.Generic.List<global::System.Text.Json.JsonElement>();
        global::System.Collections.Generic.List<string> paths = new global::System.Collections.Generic.List<string>();
        int index = 0;
        foreach (global::System.Text.Json.JsonElement item in located.Value.EnumerateArray())
        {
            items.Add(item.Clone());
            paths.Add(located.Path + "[" + index.ToString(global::System.Globalization.CultureInfo.InvariantCulture) + "]");
            index += 1;
        }

        result.ArrayItems = new global::System.Text.Json.JsonElement[items.Count];
        items.CopyTo(result.ArrayItems, 0);
        result.ArrayItemPaths = new string[paths.Count];
        paths.CopyTo(result.ArrayItemPaths, 0);
        result.IsValid = true;
        return result;
    }

    internal JsonFieldValue ReadObject(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        if (located.Value.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            result.ElementValue = located.Value.Clone();
            result.HasElementValue = true;
            result.IsValid = true;
        }

        return result;
    }

    internal JsonFieldValue ReadDate(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        if (!located.IsPresent)
        {
            result.IsValid = located.IsContainerValid;
            return result;
        }

        if (result.IsNull)
        {
            result.IsValid = true;
            return result;
        }

        JsonDateParseResult parsed = ParseDateElement(located.Value);
        if (parsed.IsValid)
        {
            result.DateValue = parsed.Value;
            result.IsValid = true;
        }

        return result;
    }

    internal JsonFieldValue ReadRaw(string name)
    {
        JsonLocatedField located = LocateField(name);
        JsonFieldValue result = CreateValue(located);
        result.IsValid = located.IsContainerValid;
        return result;
    }

    internal JsonUnknownField[] CaptureUnknownFields(string[] knownNames)
    {
        if (knownNames == null)
        {
            throw new ValidationException("Known JSON field names are required.", "knownNames", "REQUIRED", null);
        }

        global::System.Collections.Generic.HashSet<string> known = new global::System.Collections.Generic.HashSet<string>(global::System.StringComparer.Ordinal);
        foreach (string knownName in knownNames)
        {
            if (knownName == null)
            {
                throw new ValidationException("Known JSON field names cannot contain null values.", "knownNames", "REQUIRED", null);
            }

            known.Add(knownName);
        }

        global::System.Collections.Generic.List<JsonUnknownField> unknown = new global::System.Collections.Generic.List<JsonUnknownField>();
        if (Element.ValueKind == global::System.Text.Json.JsonValueKind.Object)
        {
            foreach (global::System.Text.Json.JsonProperty property in Element.EnumerateObject())
            {
                if (!known.Contains(property.Name))
                {
                    JsonUnknownField field = new JsonUnknownField();
                    field.Name = property.Name;
                    field.Path = AppendPropertyPath(Path, property.Name);
                    field.RawJson = property.Value.GetRawText();
                    field.Value = property.Value.Clone();
                    unknown.Add(field);
                }
            }
        }

        JsonUnknownField[] result = new JsonUnknownField[unknown.Count];
        unknown.CopyTo(result, 0);
        return result;
    }

    internal JsonLocatedField LocateField(string name)
    {
        if (name == null)
        {
            throw new ValidationException("A JSON field name is required.", "name", "REQUIRED", null);
        }

        JsonLocatedField located = new JsonLocatedField();
        located.Path = AppendPropertyPath(Path, name);
        if (Element.ValueKind != global::System.Text.Json.JsonValueKind.Object)
        {
            return located;
        }

        located.IsContainerValid = true;

        foreach (global::System.Text.Json.JsonProperty property in Element.EnumerateObject())
        {
            if (property.NameEquals(name))
            {
                located.Value = property.Value;
                located.IsPresent = true;
            }
        }

        return located;
    }

    internal static JsonFieldValue CreateValue(JsonLocatedField located)
    {
        JsonFieldValue result = new JsonFieldValue();
        result.IsPresent = located.IsPresent;
        result.Path = located.Path;
        if (located.IsPresent)
        {
            result.IsNull = located.Value.ValueKind == global::System.Text.Json.JsonValueKind.Null;
            result.RawJson = located.Value.GetRawText();
            result.ElementValue = located.Value.Clone();
            result.HasElementValue = true;
        }

        return result;
    }

    internal static JsonIntegerParseResult ParseIntegerElement(global::System.Text.Json.JsonElement element)
    {
        JsonIntegerParseResult invalid = new JsonIntegerParseResult();
        string? text = null;
        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            text = element.GetRawText();
        }
        else if (element.ValueKind == global::System.Text.Json.JsonValueKind.String)
        {
            text = element.GetString();
        }

        if (text == null)
        {
            return invalid;
        }

        return ParseSignedInteger(text.Trim());
    }

    internal static JsonIntegerParseResult ParseSignedInteger(string text)
    {
        JsonIntegerParseResult result = new JsonIntegerParseResult();
        if (text.Length == 0)
        {
            return result;
        }

        int index = 0;
        bool negative = false;
        if (text[0] == '-' || text[0] == '+')
        {
            negative = text[0] == '-';
            index = 1;
        }

        if (index == text.Length)
        {
            return result;
        }

        long value = 0L;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                return result;
            }

            int digit = character - '0';
            if (negative)
            {
                if (value < (global::System.Int64.MinValue + digit) / 10L)
                {
                    return result;
                }

                value = value * 10L - digit;
            }
            else
            {
                if (value > (global::System.Int64.MaxValue - digit) / 10L)
                {
                    return result;
                }

                value = value * 10L + digit;
            }

            index += 1;
        }

        result.Value = value;
        result.IsValid = true;
        return result;
    }

    internal static JsonDateParseResult ParseDateElement(global::System.Text.Json.JsonElement element)
    {
        JsonDateParseResult invalid = new JsonDateParseResult();
        if (element.ValueKind == global::System.Text.Json.JsonValueKind.Number)
        {
            JsonIntegerParseResult epoch = ParseSignedInteger(element.GetRawText());
            return ParseEpochDate(epoch);
        }

        if (element.ValueKind != global::System.Text.Json.JsonValueKind.String)
        {
            return invalid;
        }

        string? value = element.GetString();
        if (value == null)
        {
            return invalid;
        }

        return ParseFlexibleDate(value.Trim());
    }

    internal static JsonDateParseResult ParseFlexibleDate(string value)
    {
        JsonDateParseResult result = new JsonDateParseResult();
        if (value.Length == 8)
        {
            JsonDateParseResult compact = ParseCompactDate(value);
            if (compact.IsValid)
            {
                return compact;
            }
        }

        if (value.Length == 10 && value[4] == '-' && value[7] == '-')
        {
            return ParseIsoDate(value);
        }

        if (value.Length == 10 && value[2] == '/' && value[5] == '/')
        {
            return ParseUsDate(value);
        }

        if (value.Length >= 19 && value[4] == '-' && value[7] == '-' && (value[10] == 'T' || value[10] == ' '))
        {
            return ParseIsoTimestamp(value);
        }

        if (value.Length == 10 || value.Length == 13)
        {
            JsonIntegerParseResult epoch = ParseSignedInteger(value);
            return ParseEpochDate(epoch);
        }

        return result;
    }

    internal static JsonDateParseResult ParseCompactDate(string value)
    {
        int year = ParseFixedDigits(value, 0, 4);
        int month = ParseFixedDigits(value, 4, 2);
        int day = ParseFixedDigits(value, 6, 2);
        return CreateDate(year, month, day, 0, 0, 0, 0L, 0);
    }

    internal static JsonDateParseResult ParseIsoDate(string value)
    {
        int year = ParseFixedDigits(value, 0, 4);
        int month = ParseFixedDigits(value, 5, 2);
        int day = ParseFixedDigits(value, 8, 2);
        return CreateDate(year, month, day, 0, 0, 0, 0L, 0);
    }

    internal static JsonDateParseResult ParseUsDate(string value)
    {
        int month = ParseFixedDigits(value, 0, 2);
        int day = ParseFixedDigits(value, 3, 2);
        int year = ParseFixedDigits(value, 6, 4);
        return CreateDate(year, month, day, 0, 0, 0, 0L, 0);
    }

    internal static JsonDateParseResult ParseIsoTimestamp(string value)
    {
        JsonDateParseResult invalid = new JsonDateParseResult();
        int year = ParseFixedDigits(value, 0, 4);
        int month = ParseFixedDigits(value, 5, 2);
        int day = ParseFixedDigits(value, 8, 2);
        int hour = ParseFixedDigits(value, 11, 2);
        int minute = ParseFixedDigits(value, 14, 2);
        int second = ParseFixedDigits(value, 17, 2);
        if (value[13] != ':' || value[16] != ':')
        {
            return invalid;
        }

        int index = 19;
        long fractionTicks = 0L;
        if (index < value.Length && value[index] == '.')
        {
            index += 1;
            int fractionStart = index;
            int digits = 0;
            while (index < value.Length && value[index] >= '0' && value[index] <= '9')
            {
                if (digits < 7)
                {
                    fractionTicks = fractionTicks * 10L + value[index] - '0';
                }

                digits += 1;
                index += 1;
            }

            if (index == fractionStart || digits > 7)
            {
                return invalid;
            }

            while (digits < 7)
            {
                fractionTicks *= 10L;
                digits += 1;
            }
        }

        int offsetMinutes = 0;
        if (index < value.Length)
        {
            if ((value[index] == 'Z' || value[index] == 'z') && index == value.Length - 1)
            {
                index += 1;
            }
            else if ((value[index] == '+' || value[index] == '-') && index + 6 == value.Length && value[index + 3] == ':')
            {
                bool negativeOffset = value[index] == '-';
                int offsetHour = ParseFixedDigits(value, index + 1, 2);
                int offsetMinute = ParseFixedDigits(value, index + 4, 2);
                if (offsetHour < 0 || offsetHour > 14 || offsetMinute < 0 || offsetMinute > 59 || offsetHour == 14 && offsetMinute != 0)
                {
                    return invalid;
                }

                offsetMinutes = offsetHour * 60 + offsetMinute;
                if (negativeOffset)
                {
                    offsetMinutes = -offsetMinutes;
                }

                index = value.Length;
            }
        }

        if (index != value.Length)
        {
            return invalid;
        }

        return CreateDate(year, month, day, hour, minute, second, fractionTicks, offsetMinutes);
    }

    internal static JsonDateParseResult ParseEpochDate(JsonIntegerParseResult epoch)
    {
        JsonDateParseResult result = new JsonDateParseResult();
        if (!epoch.IsValid)
        {
            return result;
        }

        if (epoch.Value >= -62135596800L && epoch.Value <= 253402300799L)
        {
            result.Value = global::System.DateTimeOffset.FromUnixTimeSeconds(epoch.Value);
            result.IsValid = true;
        }
        else if (epoch.Value >= -62135596800000L && epoch.Value <= 253402300799999L)
        {
            result.Value = global::System.DateTimeOffset.FromUnixTimeMilliseconds(epoch.Value);
            result.IsValid = true;
        }

        return result;
    }

    internal static JsonDateParseResult CreateDate(int year, int month, int day, int hour, int minute, int second, long fractionTicks, int offsetMinutes)
    {
        JsonDateParseResult result = new JsonDateParseResult();
        if (year < 1 || year > 9999 || month < 1 || month > 12 || day < 1 || hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59 || fractionTicks < 0L || fractionTicks >= global::System.TimeSpan.TicksPerSecond)
        {
            return result;
        }

        if (day > global::System.DateTime.DaysInMonth(year, month) || offsetMinutes < -840 || offsetMinutes > 840)
        {
            return result;
        }

        global::System.DateTime local = new global::System.DateTime(year, month, day, hour, minute, second, global::System.DateTimeKind.Unspecified);
        long localTicks = local.Ticks + fractionTicks;
        long offsetTicks = (long)offsetMinutes * global::System.TimeSpan.TicksPerMinute;
        if (offsetTicks > 0L && localTicks < offsetTicks || offsetTicks < 0L && localTicks > global::System.DateTimeOffset.MaxValue.Ticks + offsetTicks)
        {
            return result;
        }

        global::System.TimeSpan offset = global::System.TimeSpan.FromMinutes(offsetMinutes);
        result.Value = new global::System.DateTimeOffset(localTicks, offset);
        result.IsValid = true;
        return result;
    }

    internal static int ParseFixedDigits(string value, int start, int length)
    {
        if (start < 0 || length <= 0 || start > value.Length - length)
        {
            return -1;
        }

        int result = 0;
        int end = start + length;
        for (int index = start; index < end; index += 1)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                return -1;
            }

            result = result * 10 + character - '0';
        }

        return result;
    }

    internal static string AppendPropertyPath(string path, string name)
    {
        bool simple = name.Length != 0 && (global::System.Char.IsLetter(name[0]) || name[0] == '_');
        for (int index = 1; index < name.Length && simple; index += 1)
        {
            simple = global::System.Char.IsLetterOrDigit(name[index]) || name[index] == '_';
        }

        if (simple)
        {
            return path + "." + name;
        }

        string escaped = name.Replace("\\", "\\\\", global::System.StringComparison.Ordinal).Replace("'", "\\'", global::System.StringComparison.Ordinal);
        return path + "['" + escaped + "']";
    }
}
