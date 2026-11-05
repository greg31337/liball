using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace jmap;

internal class JmapJsonWriter
{
    internal Utf8JsonWriter Output;
    internal JmapJsonReader ElementValidator;

    internal JmapJsonWriter(Stream stream)
    {
        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        JsonWriterOptions options = new JsonWriterOptions();
        options.Encoder = JavaScriptEncoder.Default;
        options.Indented = false;
        options.SkipValidation = false;

        Output = new Utf8JsonWriter(stream, options);
        ElementValidator = new JmapJsonReader(int.MaxValue, 1000);
    }

    internal void WriteObjectStart()
    {
        Output.WriteStartObject();
    }

    internal void WriteObjectEnd()
    {
        Output.WriteEndObject();
    }

    internal void WriteArrayStart()
    {
        Output.WriteStartArray();
    }

    internal void WriteArrayEnd()
    {
        Output.WriteEndArray();
    }

    internal void WriteMemberName(string name)
    {
        ValidateOutputString(name, "name");
        Output.WritePropertyName(name);
    }

    internal void WriteStringValue(string? value)
    {
        if (value == null)
        {
            Output.WriteNullValue();
            return;
        }

        ValidateOutputString(value, "value");
        Output.WriteStringValue(value);
    }

    internal void WriteBooleanValue(bool value)
    {
        Output.WriteBooleanValue(value);
    }

    internal void WriteIntegerValue(long value)
    {
        ValidateInteger(value);
        Output.WriteNumberValue(value);
    }

    internal void WriteNullValue()
    {
        Output.WriteNullValue();
    }

    internal void WriteElementValue(JsonElement value)
    {
        ValidateElement(value);
        WriteElementContent(value);
    }

    internal void WriteStringMember(string name, string? value, bool includeNull)
    {
        if (value == null && !includeNull)
        {
            return;
        }

        ValidateOutputString(name, "name");
        if (value != null)
        {
            ValidateOutputString(value, "value");
        }

        Output.WritePropertyName(name);
        if (value == null)
        {
            Output.WriteNullValue();
        }
        else
        {
            Output.WriteStringValue(value);
        }
    }

    internal void WriteBooleanMember(string name, bool value, bool includeDefault)
    {
        if (!value && !includeDefault)
        {
            return;
        }

        ValidateOutputString(name, "name");
        Output.WriteBoolean(name, value);
    }

    internal void WriteIntegerMember(string name, long value, bool includeDefault)
    {
        if (value == 0L && !includeDefault)
        {
            return;
        }

        ValidateOutputString(name, "name");
        ValidateInteger(value);
        Output.WriteNumber(name, value);
    }

    internal void WriteNullMember(string name)
    {
        ValidateOutputString(name, "name");
        Output.WriteNull(name);
    }

    internal void WriteElementMember(string name, JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            return;
        }

        ValidateOutputString(name, "name");
        ValidateElement(value);
        Output.WritePropertyName(name);
        WriteElementContent(value);
    }

    internal void FlushWriter()
    {
        Output.Flush();
    }

    internal void DisposeWriter()
    {
        Output.Dispose();
    }

    internal void ValidateInteger(long value)
    {
        if (value < JmapProtocolLimits.MinimumInt || value > JmapProtocolLimits.MaximumInt)
        {
            throw new JmapValidationException("The integer is outside the interoperable safe range.", new List<string>(), default(JsonElement));
        }
    }

    internal void ValidateElement(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Undefined)
        {
            throw new JmapValidationException("An undefined JSON value cannot be written.", new List<string>(), default(JsonElement));
        }

        byte[] utf8 = Encoding.UTF8.GetBytes(value.GetRawText());
        ElementValidator.ReadUtf8Memory(utf8);
    }

    internal void WriteElementContent(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            List<JsonProperty> members = new List<JsonProperty>();
            foreach (JsonProperty member in value.EnumerateObject())
            {
                members.Add(member);
            }

            members.Sort((left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
            Output.WriteStartObject();

            string? previousName = null;
            foreach (JsonProperty member in members)
            {
                if (previousName != null && previousName == member.Name)
                {
                    throw new JmapProtocolException("A JSON object contains a duplicate member name.", member.Name, default(JsonElement));
                }

                ValidateOutputString(member.Name, "name");
                Output.WritePropertyName(member.Name);
                WriteElementContent(member.Value);
                previousName = member.Name;
            }

            Output.WriteEndObject();
            return;
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            Output.WriteStartArray();
            foreach (JsonElement item in value.EnumerateArray())
            {
                WriteElementContent(item);
            }

            Output.WriteEndArray();
            return;
        }

        if (value.ValueKind == JsonValueKind.String)
        {
            string? text = value.GetString();
            if (text == null)
            {
                throw new JmapValidationException("A JSON string value cannot be read.", new List<string>(), default(JsonElement));
            }

            ValidateOutputString(text, "value");
            Output.WriteStringValue(text);
            return;
        }

        if (value.ValueKind == JsonValueKind.Number)
        {
            Output.WriteRawValue(value.GetRawText(), false);
            return;
        }

        if (value.ValueKind == JsonValueKind.True)
        {
            Output.WriteBooleanValue(true);
            return;
        }

        if (value.ValueKind == JsonValueKind.False)
        {
            Output.WriteBooleanValue(false);
            return;
        }

        if (value.ValueKind == JsonValueKind.Null)
        {
            Output.WriteNullValue();
            return;
        }

        throw new JmapValidationException("An undefined JSON value cannot be written.", new List<string>(), default(JsonElement));
    }

    internal void ValidateOutputString(string value, string argumentName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(argumentName);
        }

        int index = 0;
        while (index < value.Length)
        {
            int codePoint = value[index];
            if (codePoint >= 55296 && codePoint <= 56319)
            {
                if (index + 1 >= value.Length)
                {
                    throw new JmapValidationException("A JSON string contains an unpaired Unicode surrogate.", new List<string>(), default(JsonElement));
                }

                int low = value[index + 1];
                if (low < 56320 || low > 57343)
                {
                    throw new JmapValidationException("A JSON string contains an unpaired Unicode surrogate.", new List<string>(), default(JsonElement));
                }

                codePoint = 65536 + ((codePoint - 55296) * 1024) + (low - 56320);
                index = index + 2;
            }
            else
            {
                if (codePoint >= 56320 && codePoint <= 57343)
                {
                    throw new JmapValidationException("A JSON string contains an unpaired Unicode surrogate.", new List<string>(), default(JsonElement));
                }

                index = index + 1;
            }

            if ((codePoint >= 64976 && codePoint <= 65007) || (codePoint & 65535) == 65534 || (codePoint & 65535) == 65535)
            {
                throw new JmapValidationException("A JSON string contains a Unicode noncharacter.", new List<string>(), default(JsonElement));
            }
        }
    }
}
