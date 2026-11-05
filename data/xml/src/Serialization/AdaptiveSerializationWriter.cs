using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace xml;

internal class AdaptiveSerializationWriter
{
    internal XmlSerializationOptions Options;
    internal string EncodingName;
    internal StringBuilder Result;
    internal XmlEscaper Escaper;
    internal OutputCharacterMap CharacterMap;
    internal List<XdmMapValue> ActiveMaps;
    internal List<XdmArrayValue> ActiveArrays;

    internal AdaptiveSerializationWriter(XmlSerializationOptions options, string encodingName)
    {
        Guard.NotNull(options, "options");
        Guard.NotNull(encodingName, "encodingName");
        if (options.Method != XmlSerializationMethod.Adaptive)
        {
            throw CreateAdaptiveSerializationError("The adaptive serialization writer requires the adaptive output method.", XmlErrorCodes.SEPM0004);
        }

        Options = options;
        EncodingName = encodingName;
        Result = new StringBuilder();
        Escaper = new XmlEscaper();
        Escaper.ConfigureXmlSerializationErrorCodes(XmlErrorCodes.SERE0006, XmlErrorCodes.SERE0003);
        Escaper.ConfigureXmlSerializationInvalidCharacterReplacement(options.InvalidCharacterReplacement);
        ActiveMaps = new List<XdmMapValue>();
        ActiveArrays = new List<XdmArrayValue>();
        CharacterMap = new OutputCharacterMap(options.UseCharacterMaps);
    }

    internal string SerializeAdaptiveSequence(XdmSequence sequence)
    {
        Guard.NotNull(sequence, "sequence");
        ValidateAdaptiveSerializationSequenceStorage(sequence);
        string separator = Options.ItemSeparator == null ? "\n" : Options.ItemSeparator;
        int index = 0;
        while (index < sequence.Count)
        {
            if (index > 0)
            {
                Result.Append(separator);
            }

            WriteAdaptiveSerializationItem(sequence, index);
            index = index + 1;
        }

        return Result.ToString();
    }

    internal void WriteAdaptiveSerializationItem(XdmSequence sequence, int position)
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
                throw CreateAdaptiveSerializationError("An adaptive sequence contains an invalid atomic-value index.", XmlErrorCodes.SERE0021);
            }

            WriteAdaptiveSerializationAtomic(values[itemIndex]);
            return;
        }

        if (kind == XdmItemKind.Node)
        {
            XmlNode[] nodes = sequence.Nodes == null ? [] : sequence.Nodes;
            if (itemIndex < 0 || itemIndex >= nodes.Length || nodes[itemIndex] == null)
            {
                throw CreateAdaptiveSerializationError("An adaptive sequence contains an invalid node index.", XmlErrorCodes.SERE0021);
            }

            WriteAdaptiveSerializationNode(nodes[itemIndex]);
            return;
        }

        if (kind == XdmItemKind.Map)
        {
            XdmMapValue[] maps = sequence.Maps == null ? [] : sequence.Maps;
            if (itemIndex < 0 || itemIndex >= maps.Length || maps[itemIndex] == null)
            {
                throw CreateAdaptiveSerializationError("An adaptive sequence contains an invalid map index.", XmlErrorCodes.SERE0021);
            }

            WriteAdaptiveSerializationMap(maps[itemIndex]);
            return;
        }

        if (kind == XdmItemKind.Array)
        {
            XdmArrayValue[] arrays = sequence.Arrays == null ? [] : sequence.Arrays;
            if (itemIndex < 0 || itemIndex >= arrays.Length || arrays[itemIndex] == null)
            {
                throw CreateAdaptiveSerializationError("An adaptive sequence contains an invalid array index.", XmlErrorCodes.SERE0021);
            }

            WriteAdaptiveSerializationArray(arrays[itemIndex]);
            return;
        }

        if (kind == XdmItemKind.Function)
        {
            XdmFunctionItem[] functions = sequence.Functions == null ? [] : sequence.Functions;
            if (itemIndex < 0 || itemIndex >= functions.Length || functions[itemIndex] == null)
            {
                throw CreateAdaptiveSerializationError("An adaptive sequence contains an invalid function index.", XmlErrorCodes.SERE0021);
            }

            WriteAdaptiveSerializationFunction(functions[itemIndex]);
            return;
        }

        throw CreateAdaptiveSerializationError("An adaptive sequence contains an unknown item kind.", XmlErrorCodes.SERE0021);
    }

    internal void WriteAdaptiveSerializationSequenceValue(XdmSequence sequence)
    {
        ValidateAdaptiveSerializationSequenceStorage(sequence);
        if (sequence.Count == 0)
        {
            Result.Append("()");
            return;
        }

        bool parentheses = sequence.Count > 1;
        if (parentheses)
        {
            Result.Append('(');
        }

        int index = 0;
        while (index < sequence.Count)
        {
            if (index > 0)
            {
                Result.Append(',');
            }

            WriteAdaptiveSerializationItem(sequence, index);
            index = index + 1;
        }

        if (parentheses)
        {
            Result.Append(')');
        }
    }

    internal void WriteAdaptiveSerializationMap(XdmMapValue map)
    {
        if (ContainsActiveAdaptiveSerializationMap(map))
        {
            throw CreateAdaptiveSerializationError("A recursive map cannot be serialized adaptively.", XmlErrorCodes.SERE0021);
        }

        XdmMapEntry[] entries = map.Entries == null ? [] : map.Entries;
        if (map.Count < 0 || map.Count != entries.Length)
        {
            throw CreateAdaptiveSerializationError("An adaptive map has inconsistent entry storage.", XmlErrorCodes.SERE0021);
        }

        ActiveMaps.Add(map);
        Result.Append("map{");
        int index = 0;
        while (index < entries.Length)
        {
            XdmMapEntry? entry = entries[index];
            if (entry == null || entry.Key == null || entry.Value == null)
            {
                RemoveActiveAdaptiveSerializationMap(map);
                throw CreateAdaptiveSerializationError("An adaptive map contains an incomplete entry.", XmlErrorCodes.SERE0021);
            }

            if (index > 0)
            {
                Result.Append(',');
            }

            WriteAdaptiveSerializationAtomic(entry.Key);
            Result.Append(':');
            WriteAdaptiveSerializationSequenceValue(entry.Value);
            index = index + 1;
        }

        Result.Append('}');
        RemoveActiveAdaptiveSerializationMap(map);
    }

    internal void WriteAdaptiveSerializationArray(XdmArrayValue array)
    {
        if (ContainsActiveAdaptiveSerializationArray(array))
        {
            throw CreateAdaptiveSerializationError("A recursive array cannot be serialized adaptively.", XmlErrorCodes.SERE0021);
        }

        XdmSequence[] members = array.Members == null ? [] : array.Members;
        if (array.Count < 0 || array.Count != members.Length)
        {
            throw CreateAdaptiveSerializationError("An adaptive array has inconsistent member storage.", XmlErrorCodes.SERE0021);
        }

        ActiveArrays.Add(array);
        Result.Append('[');
        int index = 0;
        while (index < members.Length)
        {
            XdmSequence? member = members[index];
            if (member == null)
            {
                RemoveActiveAdaptiveSerializationArray(array);
                throw CreateAdaptiveSerializationError("An adaptive array contains a missing member.", XmlErrorCodes.SERE0021);
            }

            if (index > 0)
            {
                Result.Append(',');
            }

            WriteAdaptiveSerializationSequenceValue(member);
            index = index + 1;
        }

        Result.Append(']');
        RemoveActiveAdaptiveSerializationArray(array);
    }

    internal void WriteAdaptiveSerializationAtomic(XdmAtomicValue value)
    {
        if (value.Type == XdmAtomicType.Boolean)
        {
            Result.Append(value.BooleanValue ? "true()" : "false()");
            return;
        }

        if (value.Type == XdmAtomicType.String || value.Type == XdmAtomicType.UntypedAtomic || value.Type == XdmAtomicType.AnyUri)
        {
            WriteAdaptiveSerializationQuotedString(AdaptiveSerializationAtomicString(value));
            return;
        }

        if (value.Type == XdmAtomicType.Decimal || value.Type >= XdmAtomicType.Integer && value.Type <= XdmAtomicType.PositiveInteger)
        {
            Result.Append(AdaptiveSerializationAtomicString(value));
            return;
        }

        if (value.Type == XdmAtomicType.Float || value.Type == XdmAtomicType.Double)
        {
            WriteAdaptiveSerializationFloating(value);
            return;
        }

        if (value.Type == XdmAtomicType.QName || value.Type == XdmAtomicType.Notation)
        {
            XmlName? name = value.NameValue;
            if (name == null || name.LocalName == null)
            {
                throw CreateAdaptiveSerializationError("An adaptive QName value has no expanded name.", XmlErrorCodes.SERE0021);
            }

            Result.Append("Q{");
            Result.Append(name.NamespaceUri == null ? "" : name.NamespaceUri);
            Result.Append('}');
            Result.Append(name.LocalName);
            return;
        }

        Result.Append("xs:");
        Result.Append(AdaptiveSerializationAtomicTypeName(value.Type));
        Result.Append('(');
        WriteAdaptiveSerializationQuotedString(AdaptiveSerializationAtomicString(value));
        Result.Append(')');
    }

    internal void WriteAdaptiveSerializationFloating(XdmAtomicValue value)
    {
        double number = value.Type == XdmAtomicType.Float ? (float)value.FloatingValue : value.FloatingValue;
        if (double.IsNaN(number))
        {
            Result.Append("NaN");
            return;
        }

        if (double.IsPositiveInfinity(number))
        {
            Result.Append("INF");
            return;
        }

        if (double.IsNegativeInfinity(number))
        {
            Result.Append("-INF");
            return;
        }

        Result.Append(number.ToString("0.0##########################e0", CultureInfo.InvariantCulture));
    }

    internal void WriteAdaptiveSerializationFunction(XdmFunctionItem function)
    {
        if (function.Arity < 0)
        {
            throw CreateAdaptiveSerializationError("An adaptive function item cannot have a negative arity.", XmlErrorCodes.SERE0021);
        }

        XmlName? name = function.Name;
        if (name == null || name.LocalName == null)
        {
            Result.Append("(anonymous-function)#");
            Result.Append(function.Arity.ToString(CultureInfo.InvariantCulture));
            return;
        }

        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        if (string.Equals(namespaceUri, XPathNamespaces.Functions, StringComparison.Ordinal))
        {
            Result.Append("fn:");
        }
        else if (string.Equals(namespaceUri, XPathNamespaces.Math, StringComparison.Ordinal))
        {
            Result.Append("math:");
        }
        else if (string.Equals(namespaceUri, XPathNamespaces.Map, StringComparison.Ordinal))
        {
            Result.Append("map:");
        }
        else if (string.Equals(namespaceUri, XPathNamespaces.Array, StringComparison.Ordinal))
        {
            Result.Append("array:");
        }
        else if (string.Equals(namespaceUri, XPathNamespaces.XmlSchema, StringComparison.Ordinal))
        {
            Result.Append("xs:");
        }
        else
        {
            Result.Append("Q{");
            Result.Append(namespaceUri);
            Result.Append('}');
        }

        Result.Append(name.LocalName);
        Result.Append('#');
        Result.Append(function.Arity.ToString(CultureInfo.InvariantCulture));
    }

    internal void WriteAdaptiveSerializationNode(XmlNode node)
    {
        if (node.Kind == XmlNodeKind.Attribute)
        {
            Result.Append(AdaptiveSerializationNodeName(node));
            Result.Append("=\"");
            WriteAdaptiveSerializationXmlValue(node.Value == null ? "" : node.Value, XmlCharacterEscapeContext.AttributeDoubleQuoted);
            Result.Append('"');
            return;
        }

        if (node.Kind == XmlNodeKind.Namespace)
        {
            string prefix = node.Name == null || node.Name.LocalName == null ? "" : node.Name.LocalName;
            Result.Append("xmlns");
            if (prefix.Length > 0)
            {
                Result.Append(':');
                Result.Append(prefix);
            }

            Result.Append("=\"");
            WriteAdaptiveSerializationXmlValue(node.Value == null ? "" : node.Value, XmlCharacterEscapeContext.AttributeDoubleQuoted);
            Result.Append('"');
            return;
        }

        SequenceNormalizer normalizer = new SequenceNormalizer();
        XmlDocument document = normalizer.NormalizeXmlSerializationNode(node, null);
        if (node.OwnerDocument != null)
        {
            document.Version = node.OwnerDocument.Version;
            document.DocumentType = node.Kind == XmlNodeKind.Document ? node.OwnerDocument.DocumentType : null;
        }

        XmlSerializationOptions delegated = CopyAdaptiveSerializationOptions();
        string value = new XmlSerializationWriter(delegated, document.Version, EncodingName).SerializeXmlDocumentTree(document);
        Result.Append(value);
    }

    internal XmlSerializationOptions CopyAdaptiveSerializationOptions()
    {
        XmlSerializationOptions result = new XmlSerializationOptions();
        result.Method = XmlSerializationMethod.Xml;
        result.AllowDuplicateNames = Options.AllowDuplicateNames;
        result.CDataSectionElements = Options.CDataSectionElements;
        result.DoctypePublic = Options.DoctypePublic;
        result.DoctypeSystem = Options.DoctypeSystem;
        result.Encoding = Options.Encoding;
        result.EscapeUriAttributes = Options.EscapeUriAttributes;
        result.HtmlVersion = Options.HtmlVersion;
        result.IncludeContentType = Options.IncludeContentType;
        result.Indent = Options.Indent;
        result.JsonNodeOutputMethod = Options.JsonNodeOutputMethod;
        result.MediaType = Options.MediaType;
        result.NormalizationForm = Options.NormalizationForm;
        result.OmitXmlDeclaration = Options.OmitXmlDeclaration;
        result.Standalone = Options.Standalone;
        result.SuppressIndentation = Options.SuppressIndentation;
        result.UndeclarePrefixes = Options.UndeclarePrefixes;
        result.UseCharacterMaps = Options.UseCharacterMaps;
        result.Version = Options.Version;
        result.InvalidCharacterPolicy = Options.InvalidCharacterPolicy;
        result.InvalidCharacterReplacement = Options.InvalidCharacterReplacement;
        result.NewLine = Options.NewLine;
        return result;
    }

    internal void WriteAdaptiveSerializationQuotedString(string value)
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
                WriteAdaptiveSerializationPlainString(value.Substring(plainStart, position - plainStart));
                Result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        WriteAdaptiveSerializationPlainString(value.Substring(plainStart));
        Result.Append('"');
    }

    internal void WriteAdaptiveSerializationPlainString(string value)
    {
        string normalized = NormalizeAdaptiveSerializationString(value);
        Result.Append(normalized.Replace("\"", "\"\"", StringComparison.Ordinal));
    }

    internal void WriteAdaptiveSerializationXmlValue(string value, XmlCharacterEscapeContext context)
    {
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
                WriteAdaptiveSerializationPlainXmlValue(value.Substring(plainStart, position - plainStart), context);
                Result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        WriteAdaptiveSerializationPlainXmlValue(value.Substring(plainStart), context);
    }

    internal void WriteAdaptiveSerializationPlainXmlValue(string value, XmlCharacterEscapeContext context)
    {
        string normalized = NormalizeAdaptiveSerializationString(value);
        Result.Append(Escaper.EscapeXmlValue(normalized, XmlVersion.Xml10, context, Options.InvalidCharacterPolicy, context == XmlCharacterEscapeContext.AttributeDoubleQuoted ? '"' : '\0'));
    }

    internal string NormalizeAdaptiveSerializationString(string value)
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

        if (string.Equals(form, "NFC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "fully-normalized", StringComparison.OrdinalIgnoreCase))
        {
            return value.Normalize(global::System.Text.NormalizationForm.FormC);
        }

        throw CreateAdaptiveSerializationError("The normalization-form parameter is not supported.", XmlErrorCodes.SESU0011);
    }

    internal string AdaptiveSerializationAtomicString(XdmAtomicValue value)
    {
        return new SequenceNormalizer().CastXdmAtomicToSerializationString(value);
    }

    internal string AdaptiveSerializationAtomicTypeName(XdmAtomicType type)
    {
        if (type == XdmAtomicType.AnyAtomicType) return "anyAtomicType";
        if (type == XdmAtomicType.NormalizedString) return "normalizedString";
        if (type == XdmAtomicType.Token) return "token";
        if (type == XdmAtomicType.Language) return "language";
        if (type == XdmAtomicType.Name) return "Name";
        if (type == XdmAtomicType.NcName) return "NCName";
        if (type == XdmAtomicType.Nmtoken) return "NMTOKEN";
        if (type == XdmAtomicType.Id) return "ID";
        if (type == XdmAtomicType.Idref) return "IDREF";
        if (type == XdmAtomicType.Entity) return "ENTITY";
        if (type == XdmAtomicType.Duration) return "duration";
        if (type == XdmAtomicType.YearMonthDuration) return "yearMonthDuration";
        if (type == XdmAtomicType.DayTimeDuration) return "dayTimeDuration";
        if (type == XdmAtomicType.DateTime) return "dateTime";
        if (type == XdmAtomicType.DateTimeStamp) return "dateTimeStamp";
        if (type == XdmAtomicType.Date) return "date";
        if (type == XdmAtomicType.Time) return "time";
        if (type == XdmAtomicType.GYearMonth) return "gYearMonth";
        if (type == XdmAtomicType.GYear) return "gYear";
        if (type == XdmAtomicType.GMonthDay) return "gMonthDay";
        if (type == XdmAtomicType.GMonth) return "gMonth";
        if (type == XdmAtomicType.GDay) return "gDay";
        if (type == XdmAtomicType.HexBinary) return "hexBinary";
        if (type == XdmAtomicType.Base64Binary) return "base64Binary";
        throw CreateAdaptiveSerializationError("The atomic type cannot be serialized adaptively.", XmlErrorCodes.SERE0021);
    }

    internal string AdaptiveSerializationNodeName(XmlNode node)
    {
        XmlName? name = node.Name;
        if (name == null)
        {
            throw CreateAdaptiveSerializationError("An adaptive named node has no name.", XmlErrorCodes.SERE0021);
        }

        if (name.QualifiedName != null && name.QualifiedName.Length > 0)
        {
            return name.QualifiedName;
        }

        string localName = name.LocalName == null ? "" : name.LocalName;
        return name.Prefix == null || name.Prefix.Length == 0 ? localName : name.Prefix + ":" + localName;
    }

    internal void ValidateAdaptiveSerializationSequenceStorage(XdmSequence sequence)
    {
        if (sequence.Count < 0)
        {
            throw CreateAdaptiveSerializationError("An adaptive sequence has a negative item count.", XmlErrorCodes.SERE0021);
        }

        XdmItemKind[] kinds = sequence.ItemKinds == null ? [] : sequence.ItemKinds;
        int[] indexes = sequence.ItemIndexes == null ? [] : sequence.ItemIndexes;
        if (kinds.Length != sequence.Count || indexes.Length != sequence.Count)
        {
            throw CreateAdaptiveSerializationError("An adaptive sequence has inconsistent ordered item storage.", XmlErrorCodes.SERE0021);
        }
    }

    internal bool ContainsActiveAdaptiveSerializationMap(XdmMapValue map)
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

    internal bool ContainsActiveAdaptiveSerializationArray(XdmArrayValue array)
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

    internal void RemoveActiveAdaptiveSerializationMap(XdmMapValue map)
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

    internal void RemoveActiveAdaptiveSerializationArray(XdmArrayValue array)
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

    internal XmlSerializationException CreateAdaptiveSerializationError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
