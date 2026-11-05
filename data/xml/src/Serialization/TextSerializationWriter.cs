using System;
using System.Text;

namespace xml;

internal class TextSerializationWriter
{
    internal XmlSerializationOptions Options;
    internal OutputCharacterMap CharacterMap;

    internal TextSerializationWriter(XmlSerializationOptions options)
    {
        Guard.NotNull(options, "options");
        Options = options;
        ValidateTextSerializationOptions();
        CharacterMap = new OutputCharacterMap(options.UseCharacterMaps);
    }

    internal string SerializeTextDocument(XmlDocument document)
    {
        Guard.NotNull(document, "document");
        XmlNode? documentNode = document.DocumentNode;
        if (documentNode == null)
        {
            throw CreateTextSerializationError("The text output method requires a document node.", XmlErrorCodes.SENR0001);
        }

        StringBuilder value = new StringBuilder();
        AppendTextSerializationNodeValue(documentNode, value);
        string rawValue = value.ToString();
        ValidateTextSerializationCharacters(rawValue);
        return ApplyTextSerializationCharacterMap(rawValue);
    }

    internal void AppendTextSerializationNodeValue(XmlNode node, StringBuilder result)
    {
        if (node.Kind == XmlNodeKind.Text || node.Kind == XmlNodeKind.CData)
        {
            result.Append(node.Value == null ? "" : node.Value);
            return;
        }

        if (node.Kind == XmlNodeKind.EntityReference)
        {
            XmlNode[] entityChildren = node.Children == null ? [] : node.Children;
            if (entityChildren.Length == 0)
            {
                result.Append(node.Value == null ? "" : node.Value);
                return;
            }
        }

        if (node.Kind != XmlNodeKind.Document && node.Kind != XmlNodeKind.DocumentFragment && node.Kind != XmlNodeKind.Element && node.Kind != XmlNodeKind.EntityReference)
        {
            return;
        }

        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            AppendTextSerializationNodeValue(children[index], result);
            index = index + 1;
        }
    }

    internal void ValidateTextSerializationOptions()
    {
        if (Options.Method != XmlSerializationMethod.Text)
        {
            throw CreateTextSerializationError("The text serialization writer requires the text output method.", XmlErrorCodes.SEPM0004);
        }

        string? form = Options.NormalizationForm;
        if (form == null || form.Length == 0 || string.Equals(form, "none", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKC", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "NFKD", StringComparison.OrdinalIgnoreCase) || string.Equals(form, "fully-normalized", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        throw CreateTextSerializationError("The normalization-form parameter is not supported for text output.", XmlErrorCodes.SESU0011);
    }

    internal string NormalizeTextSerializationValue(string value)
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

    internal string ApplyTextSerializationCharacterMap(string value)
    {
        StringBuilder result = new StringBuilder(value.Length);
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
                AppendTextSerializationPlainValue(value.Substring(plainStart, position - plainStart), result);
                result.Append(CharacterMap.Replacements[mapIndex]);
                position = position + characterCount;
                plainStart = position;
            }
            else
            {
                position = position + characterCount;
            }
        }

        AppendTextSerializationPlainValue(value.Substring(plainStart), result);
        return result.ToString();
    }

    internal void AppendTextSerializationPlainValue(string value, StringBuilder result)
    {
        if (value.Length == 0)
        {
            return;
        }

        string normalized = NormalizeTextSerializationValue(value);
        if (Options.NewLine == null || string.Equals(Options.NewLine, "\n", StringComparison.Ordinal))
        {
            result.Append(normalized);
            return;
        }

        result.Append(normalized.Replace("\n", Options.NewLine, StringComparison.Ordinal));
    }

    internal void ValidateTextSerializationCharacters(string value)
    {
        int position = 0;
        while (position < value.Length)
        {
            int first = value[position];
            if (first >= 0xD800 && first <= 0xDBFF)
            {
                if (position + 1 >= value.Length || value[position + 1] < 0xDC00 || value[position + 1] > 0xDFFF)
                {
                    throw CreateTextSerializationError("The text output contains an unpaired high surrogate.", XmlErrorCodes.SERE0008);
                }

                position = position + 2;
                continue;
            }

            if (first >= 0xDC00 && first <= 0xDFFF)
            {
                throw CreateTextSerializationError("The text output contains an unpaired low surrogate.", XmlErrorCodes.SERE0008);
            }

            position = position + 1;
        }
    }

    internal XmlSerializationException CreateTextSerializationError(string message, string code)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = code;
        diagnostic.Category = XmlErrorCategory.Serialization;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlSerializationException(message, diagnostic, null);
    }
}
