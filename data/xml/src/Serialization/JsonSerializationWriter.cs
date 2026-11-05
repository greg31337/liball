using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace xml;

internal class JsonSerializationWriter
{
    internal XmlSerializationOptions Options;
    internal string EncodingName;
    internal string NewLine;
    internal StringBuilder Result;
    internal XmlEscaper Escaper;
    internal OutputCharacterMap CharacterMap;
    internal List<XdmMapValue> ActiveMaps;
    internal List<XdmArrayValue> ActiveArrays;

    internal JsonSerializationWriter(XmlSerializationOptions options, string encodingName)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(encodingName, "encodingName");
        Options = options;
        EncodingName = encodingName;
        NewLine = options.NewLine == null ? "\n" : options.NewLine;
        Result = new StringBuilder();
        Escaper = new XmlEscaper();
        Escaper.ConfigureXmlSerializationErrorCodes(XmlErrorCodes.SERE0021, XmlErrorCodes.SERE0021);
        ActiveMaps = new List<XdmMapValue>();
        ActiveArrays = new List<XdmArrayValue>();
        ValidateJsonSerializationOptions();
        CharacterMap = new OutputCharacterMap(options.UseCharacterMaps);
    }

    internal string SerializeJsonSequence(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        ValidateJsonSerializationSequenceStorage(sequence);
        if (sequence.Count == 0)
        {
            return "null";
        }

        if (sequence.Count > 1)
        {
            throw CreateJsonSerializationError("A JSON sequence cannot contain more than one item.", XmlErrorCodes.SERE0023);
        }

        WriteJsonSerializationSequenceItem(sequence, 0, 0);
        return Result.ToString();
    }

    internal void WriteJsonSerializationSequenceValue(XdmSequence sequence, int depth)
    {
        ValidateJsonSerializationSequenceStorage(sequence);
        if (sequence.Count == 0)
        {
            Result.Append("null");
            return;
        }

        if (sequence.Count > 1)
        {
            throw CreateJsonSerializationError("A JSON array member or map value cannot contain more than one item.", XmlErrorCodes.SERE0023);
        }

        WriteJsonSerializationSequenceItem(sequence, 0, depth);
    }

    internal void WriteJsonSerializationSequenceItem(XdmSequence sequence, int position, int depth)
    {
        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        XdmItemKind kind = kinds[position];
        int itemIndex = indexes[position];
        if (kind == XdmItemKind.Atomic)
        {
            XdmAtomicValue[] values = sequence.AtomicValues == null ? [] : sequence.AtomicValues;
            if (itemIndex < 0 || itemIndex >= values.Length || values[itemIndex] == null)
            {
                throw CreateJsonSerializationError("A JSON sequence contains an invalid atomic-value index.", XmlErrorCodes.SERE0021);
            }

            WriteJsonSerializationAtomic(values[itemIndex]);
            return;
        }

        if (kind == XdmItemKind.Map)
        {
            XdmMapValue[] maps = sequence.Maps == null ? [] : sequence.Maps;
            if (itemIndex < 0 || itemIndex >= maps.Length || maps[itemIndex] == null)
            {
                throw CreateJsonSerializationError("A JSON sequence contains an invalid map index.", XmlErrorCodes.SERE0021);
            }

            WriteJsonSerializationMap(maps[itemIndex], depth);
            return;
        }

        if (kind == XdmItemKind.Array)
        {
            XdmArrayValue[] arrays = sequence.Arrays == null ? [] : sequence.Arrays;
            if (itemIndex < 0 || itemIndex >= arrays.Length || arrays[itemIndex] == null)
            {
                throw CreateJsonSerializationError("A JSON sequence contains an invalid array index.", XmlErrorCodes.SERE0021);
            }

            WriteJsonSerializationArray(arrays[itemIndex], depth);
            return;
        }

        if (kind == XdmItemKind.Node)
        {
            XmlNode[] nodes = sequence.Nodes == null ? [] : sequence.Nodes;
            if (itemIndex < 0 || itemIndex >= nodes.Length || nodes[itemIndex] == null)
            {
                throw CreateJsonSerializationError("A JSON sequence contains an invalid node index.", XmlErrorCodes.SERE0021);
            }

            WriteJsonSerializationNode(nodes[itemIndex]);
            return;
        }

        throw CreateJsonSerializationError("A function item cannot be serialized using the JSON output method.", XmlErrorCodes.SERE0021);
    }

    internal void WriteJsonSerializationMap(XdmMapValue map, int depth)
    {
        if (ContainsActiveJsonSerializationMap(map))
        {
            throw CreateJsonSerializationError("A recursive map cannot be serialized as JSON.", XmlErrorCodes.SERE0021);
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        if (map.Count < 0 || map.Count != entries.Length)
        {
            throw CreateJsonSerializationError("A JSON map has inconsistent entry storage.", XmlErrorCodes.SERE0021);
        }

        ActiveMaps.Add(map);
        List<string> names = new List<string>();
        Result.Append('{');
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry? entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                RemoveActiveJsonSerializationMap(map);
                throw CreateJsonSerializationError("A JSON map contains an incomplete entry.", XmlErrorCodes.SERE0021);
            }

            string name = JsonSerializationAtomicString(entry.Key);
            if (!Options.AllowDuplicateNames && ContainsJsonSerializationName(names, name))
            {
                RemoveActiveJsonSerializationMap(map);
                throw CreateJsonSerializationError("Two JSON map keys have the same string value.", XmlErrorCodes.SERE0022);
            }

            names.Add(name);
            if (index > 0)
            {
                Result.Append(',');
            }

            if (Options.Indent)
            {
                WriteJsonSerializationIndent(depth + 1);
            }

            WriteJsonSerializationString(name);
            Result.Append(Options.Indent ? ": " : ":");
            WriteJsonSerializationSequenceValue(entry.Value, depth + 1);
            index = index + 1;
        }

        if (Options.Indent && entries.Length > 0)
        {
            WriteJsonSerializationIndent(depth);
        }

        Result.Append('}');
        RemoveActiveJsonSerializationMap(map);
    }

    internal void WriteJsonSerializationArray(XdmArrayValue array, int depth)
    {
        if (ContainsActiveJsonSerializationArray(array))
        {
            throw CreateJsonSerializationError("A recursive array cannot be serialized as JSON.", XmlErrorCodes.SERE0021);
        }

        XdmSequence[] members = array.Members == null ? [] : array.Members;
        if (array.Count < 0 || array.Count != members.Length)
        {
            throw CreateJsonSerializationError("A JSON array has inconsistent member storage.", XmlErrorCodes.SERE0021);
        }

        ActiveArrays.Add(array);
        Result.Append('[');
        int index = 0;
        while (index < members.Length)
        {
            XdmSequence? member = members[index];
            if (member == null)
            {
                RemoveActiveJsonSerializationArray(array);
                throw CreateJsonSerializationError("A JSON array contains a missing member.", XmlErrorCodes.SERE0021);
            }

            if (index > 0)
            {
                Result.Append(',');
            }

            if (Options.Indent)
            {
                WriteJsonSerializationIndent(depth + 1);
            }

            WriteJsonSerializationSequenceValue(member, depth + 1);
            index = index + 1;
        }

        if (Options.Indent && members.Length > 0)
        {
            WriteJsonSerializationIndent(depth);
        }

        Result.Append(']');
        RemoveActiveJsonSerializationArray(array);
    }

    internal void WriteJsonSerializationAtomic(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Boolean)
        {
            Result.Append(value.BooleanValue ? "true" : "false");
            return;
        }

        if (value.Type == XdmAtomicType.Decimal)
        {
            if (value.CanonicalValue != null)
            {
                Result.Append(value.CanonicalValue);
            }
            else
            {
                Result.Append(global::System.Xml.XmlConvert.ToString(value.DecimalValue));
            }

            return;
        }

        if (value.Type >= XdmAtomicType.Integer && value.Type <= XdmAtomicType.PositiveInteger)
        {
            if (value.CanonicalValue != null)
            {
                Result.Append(value.CanonicalValue);
            }
            else
            {
                Result.Append(value.IntegerValue.ToString(CultureInfo.InvariantCulture));
            }

            return;
        }

        if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
        {
            double number = value.Type == XdmAtomicType.Float ? (float)value.FloatingValue : value.FloatingValue;
            if (double.IsNaN(number) || double.IsInfinity(number))
            {
                throw CreateJsonSerializationError("A non-finite numeric value cannot be represented in JSON.", XmlErrorCodes.SERE0020);
            }

            if (value.Type == XdmAtomicType.Float)
            {
                Result.Append(((float)number).ToString("R", CultureInfo.InvariantCulture));
            }
            else
            {
                Result.Append(number.ToString("R", CultureInfo.InvariantCulture));
            }

            return;
        }

        WriteJsonSerializationString(JsonSerializationAtomicString(value));
    }

    internal string JsonSerializationAtomicString(XdmAtomicValue value)
    {
        return new SequenceNormalizer().CastXdmAtomicToSerializationString(value);
    }

    internal void WriteJsonSerializationNode(XmlNode node)
    {
        XmlSerializationMethod method = Options.JsonNodeOutputMethod;
        if (method != XmlSerializationMethod.Xml && method != XmlSerializationMethod.Xhtml && method != XmlSerializationMethod.Html && method != XmlSerializationMethod.Text)
        {
            throw CreateJsonSerializationError("The json-node-output-method must be xml, xhtml, html, or text.", XmlErrorCodes.SEPM0004);
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument document = normalizer.NormalizeXmlSerializationNode(node, null);
        if (node.OwnerDocument != null)
        {
            document.Version = node.OwnerDocument.Version;
        }

        XmlSerializationOptions delegated = new XmlSerializationOptions();
        delegated.Method = method;
        delegated.OmitXmlDeclaration = true;
        delegated.InvalidCharacterPolicy = XmlInvalidCharacterPolicy.Error;
        string value;
        if (method == XmlSerializationMethod.Text)
        {
            value = new TextSerializationWriter(delegated).SerializeTextDocument(document);
        }
        else
        {
            value = new XmlSerializationWriter(delegated, document.Version, EncodingName).SerializeXmlDocumentTree(document);
        }

        WriteJsonSerializationString(value);
    }

    internal void WriteJsonSerializationString(string value)
    {
        Result.Append('"');
        int position = 0;
        int plainStart = 0;
        while (position < value.Length)
        {
            int scalar = value[position];
            int characterCount = 1;
            if (scalar >= 0xD800 && scalar <= 0xDBFF && position + 1 < value.Length && value[position + 1] >= 0xDC00 && value[position + 1] <= 0xDFFF)
            {
                int second = value[position + 1];
                scalar = 0x10000 + (scalar - 0xD800) * 0x400 + second - 0xDC00;
                characterCount = 2;
            }

            int mapIndex = CharacterMap.FindOutputCharacterMap(scalar);
            if (mapIndex >= 0)
            {
                WriteJsonSerializationPlainString(value.Substring(plainStart, position - plainStart));
                Result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        WriteJsonSerializationPlainString(value.Substring(plainStart));
        Result.Append('"');
    }

    internal void WriteJsonSerializationPlainString(string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        string normalized = NormalizeJsonSerializationString(value);
        string escaped = Escaper.EscapeXmlValue(normalized, XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Error, '"');
        int position = 0;
        while (position < escaped.Length)
        {
            int character = escaped[position];
            if (character >= 0xD800 && character <= 0xDBFF && position + 1 < escaped.Length && char.IsSurrogatePair(escaped, position) && RequiresJsonEncodingEscape())
            {
                Result.Append("\\u");
                Result.Append(character.ToString("X4"));
                Result.Append("\\u");
                Result.Append(((int)escaped[position + 1]).ToString("X4"));
                position = position + 1;
            }
            else if (character > 0x7F && RequiresJsonEncodingEscape())
            {
                Result.Append("\\u");
                Result.Append(character.ToString("X4"));
            }
            else if (character == '/')
            {
                Result.Append("\\/");
            }
            else if (character >= 0x7F && character <= 0x9F)
            {
                Result.Append("\\u");
                Result.Append(character.ToString("X4"));
            }
            else
            {
                Result.Append(escaped[position]);
            }

            position = position + 1;
        }
    }

    internal bool RequiresJsonEncodingEscape()
    {
        string normalized = EncodingName.ToLowerInvariant();
        return !normalized.StartsWith("utf-", StringComparison.Ordinal) && normalized != "utf8" && normalized != "unicode";
    }

    internal string NormalizeJsonSerializationString(string value)
    {
        string? form = Options.NormalizationForm;
        SerializationNormalization.ValidateFullyNormalizedValue(value, form);
        if (form == null || form.Length == 0 || string.Equals(form, "none", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormD);
        }

        if (string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormKC);
        }

        if (string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormKD);
        }

        return value.Normalize(global::System.Text.NormalizationForm.FormC);
    }

    internal void ValidateJsonSerializationOptions()
    {
        if (Options.Method != XmlSerializationMethod.Json)
        {
            throw CreateJsonSerializationError("The JSON serialization writer requires the JSON output method.", XmlErrorCodes.SEPM0004);
        }

        string? form = Options.NormalizationForm;
        if (form != null && form.Length > 0 && !string.Equals(form, "none", StringComparison.OrdinalIgnoreCase) && !string.Equals(form, "NFC", StringComparison.OrdinalIgnoreCase) && !string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase) && !string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase) && !string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase) && !string.Equals(form, "fully-normalized", StringComparison.OrdinalIgnoreCase))
        {
            throw CreateJsonSerializationError("The normalization-form parameter is not supported for JSON output.", XmlErrorCodes.SESU0011);
        }
    }

    internal void ValidateJsonSerializationSequenceStorage(XdmSequence sequence)
    {
        if (sequence.Count < 0)
        {
            throw CreateJsonSerializationError("A JSON sequence has a negative item count.", XmlErrorCodes.SERE0021);
        }

        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        if (kinds.Length != sequence.Count || indexes.Length != sequence.Count)
        {
            throw CreateJsonSerializationError("A JSON sequence has inconsistent ordered item storage.", XmlErrorCodes.SERE0021);
        }
    }

    internal bool ContainsJsonSerializationName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], name, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsActiveJsonSerializationMap(XdmMapValue map)
    {
        int index = 0;
        while (index < ActiveMaps.Count)
        {
            if (ActiveMaps[index] == map)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsActiveJsonSerializationArray(XdmArrayValue array)
    {
        int index = 0;
        while (index < ActiveArrays.Count)
        {
            if (ActiveArrays[index] == array)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void RemoveActiveJsonSerializationMap(XdmMapValue map)
    {
        int index = ActiveMaps.Count - 1;
        while (index >= 0)
        {
            if (ActiveMaps[index] == map)
            {
                ActiveMaps.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    internal void RemoveActiveJsonSerializationArray(XdmArrayValue array)
    {
        int index = ActiveArrays.Count - 1;
        while (index >= 0)
        {
            if (ActiveArrays[index] == array)
            {
                ActiveArrays.RemoveAt(index);
                return;
            }

            index = index - 1;
        }
    }

    internal void WriteJsonSerializationIndent(int depth)
    {
        Result.Append(NewLine);
        int index = 0;
        while (index < depth * 2)
        {
            Result.Append(' ');
            index = index + 1;
        }
    }

    internal XmlSerializationException CreateJsonSerializationError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
