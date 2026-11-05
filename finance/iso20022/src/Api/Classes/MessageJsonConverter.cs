using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace iso20022;

/// <summary>Converts ISO 20022 generic message documents between XML-oriented models and JSON.</summary>
public class MessageJsonConverter : IMessageJsonConverter
{
    /// <summary>Initializes an ISO 20022 JSON converter.</summary>
    public MessageJsonConverter()
    {
    }

    /// <summary>Writes a generic message document in the ISO 20022 JSON object form.</summary>
    /// <param name="document">The generic message document.</param>
    /// <returns>The conversion result.</returns>
    public MessageJsonResult WriteGenericDocument(MessageDocument? document)
    {
        if (document == null)
        {
            return CreateFailureResult(
                "json.document_missing",
                "A generic message document is required.");
        }

        DynamicXmlNode? tree = XmlMessageWriter.BuildDocumentTree(
            document,
            new MessageWriteOptions());
        DynamicXmlNode? root = FindFirstElementNode(tree);
        if (root == null)
        {
            return CreateFailureResult(
                "json.content_missing",
                "The generic message document does not contain an element root.");
        }

        MessageJsonResult result = new MessageJsonResult();
        result.NamespaceUri = root.Name.NamespaceUri;
        string messageIdentifier = FindMessageIdentifierValue(document);
        result.SchemaIdentifier = CreateSchemaIdentifier(
            result.NamespaceUri,
            messageIdentifier);
        JsonWriterOptions settings = new JsonWriterOptions();
        settings.Indented = true;
        using (MemoryStream output = new MemoryStream())
        {
            using (Utf8JsonWriter writer = new Utf8JsonWriter(output, settings))
            {
                writer.WriteStartObject();
                writer.WriteString("$schema", result.SchemaIdentifier);
                writer.WriteString("$namespace", result.NamespaceUri);
                writer.WritePropertyName(root.Name.LocalName);
                WriteElementJsonValue(
                    writer,
                    root,
                    result.NamespaceUri,
                    "/" + root.Name.LocalName,
                    result);
                writer.WriteEndObject();
                writer.Flush();
            }
            result.Json = Encoding.UTF8.GetString(output.ToArray());
        }
        result.Success = true;
        return result;
    }

    /// <summary>Reads ISO 20022 JSON into a generic message document and reconstructed XML.</summary>
    /// <param name="json">The JSON text.</param>
    /// <returns>The conversion result.</returns>
    public MessageJsonResult ReadGenericText(string? json)
    {
        if (json == null)
        {
            return CreateFailureResult(
                "json.input_missing",
                "ISO 20022 JSON text is required.");
        }

        using (JsonDocument parsed = JsonDocument.Parse(json))
        {
            JsonElement top = parsed.RootElement;
            if (top.ValueKind != JsonValueKind.Object)
            {
                return CreateFailureResult(
                    "json.root_invalid",
                    "The ISO 20022 JSON root must be an object.");
            }

            string schemaIdentifier = string.Empty;
            string namespaceUri = string.Empty;
            string rootName = string.Empty;
            JsonElement rootValue = default(JsonElement);
            bool hasRootValue = false;
            foreach (JsonProperty property in top.EnumerateObject())
            {
                if (property.Name == "$schema" &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    schemaIdentifier = property.Value.GetString() ?? string.Empty;
                }
                else if (property.Name == "$namespace" &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    namespaceUri = property.Value.GetString() ?? string.Empty;
                }
                else if (property.Name.Length == 0 || property.Name[0] != '$')
                {
                    if (hasRootValue)
                    {
                        return CreateFailureResult(
                            "json.root_ambiguous",
                            "ISO 20022 JSON must contain exactly one message root property.");
                    }
                    rootName = property.Name;
                    rootValue = property.Value;
                    hasRootValue = true;
                }
            }

            if (schemaIdentifier.Length == 0 || namespaceUri.Length == 0)
            {
                return CreateFailureResult(
                    "json.identity_missing",
                    "Both $schema and $namespace identity fields are required.");
            }
            if (!hasRootValue || rootName.Length == 0)
            {
                return CreateFailureResult(
                    "json.content_missing",
                    "The ISO 20022 JSON object does not contain message content.");
            }
            string expectedSchema = CreateSchemaIdentifier(namespaceUri, string.Empty);
            if (schemaIdentifier != expectedSchema)
            {
                return CreateFailureResult(
                    "json.schema_namespace_mismatch",
                    "The $schema identifier does not match the declared $namespace identity.");
            }
            if (rootValue.ValueKind == JsonValueKind.Array)
            {
                return CreateFailureResult(
                    "json.root_invalid",
                    "The message root property cannot be an array.");
            }

            MessageJsonResult result = new MessageJsonResult();
            result.SchemaIdentifier = schemaIdentifier;
            result.NamespaceUri = namespaceUri;
            DynamicXmlNode documentNode = new DynamicXmlNode();
            documentNode.Kind = XmlNodeKind.Document;
            DynamicXmlNode messageRoot = CreateElementFromJsonValue(
                rootName,
                namespaceUri,
                rootValue,
                "/" + rootName,
                result);
            NamespaceDeclarationData declaration = new NamespaceDeclarationData();
            declaration.NamespaceUri = namespaceUri;
            messageRoot.NamespaceDeclarations.Add(declaration);
            documentNode.Children.Add(messageRoot);
            MessageWriteOptions writeOptions = new MessageWriteOptions();
            writeOptions.WriteXmlDeclaration = false;
            writeOptions.PreservePrefixes = false;
            result.Xml = new GenericXmlNodeWriter(writeOptions).WriteText(documentNode);
            MessageReadOptions readOptions = new MessageReadOptions();
            readOptions.NamespaceMode = NamespaceMode.Lenient;
            MessageReadResult read = new XmlMessageReader().ReadGenericText(
                result.Xml,
                readOptions);
            if (!read.Success || read.Document == null)
            {
                return CreateFailureResult(read.ErrorCode, read.ErrorMessage);
            }
            result.Document = read.Document;
            result.Success = true;
            return result;
        }
    }

    internal static MessageJsonResult CreateFailureResult(string code, string message)
    {
        MessageJsonResult result = new MessageJsonResult();
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }

    internal static DynamicXmlNode? FindFirstElementNode(DynamicXmlNode? node)
    {
        if (node == null)
        {
            return null;
        }
        if (node.Kind == XmlNodeKind.Element)
        {
            return node;
        }
        int index = 0;
        while (index < node.Children.Count)
        {
            DynamicXmlNode? found = FindFirstElementNode(node.Children[index]);
            if (found != null)
            {
                return found;
            }
            index++;
        }
        return null;
    }

    internal static string FindMessageIdentifierValue(MessageDocument document)
    {
        if (document.Metadata.MessageIdentifier != null &&
            document.Metadata.MessageIdentifier.Value.Length > 0)
        {
            return document.Metadata.MessageIdentifier.Value;
        }
        int index = 0;
        while (index < document.Payloads.Count)
        {
            MessageIdentifier? identifier = document.Payloads[index].Identifier;
            if (identifier != null && identifier.Value.Length > 0)
            {
                return identifier.Value;
            }
            index++;
        }
        return string.Empty;
    }

    internal static string CreateSchemaIdentifier(
        string namespaceUri,
        string messageIdentifier)
    {
        const string xsdMarker = ":tech:xsd:";
        int marker = namespaceUri.IndexOf(xsdMarker, StringComparison.Ordinal);
        if (marker >= 0)
        {
            return namespaceUri.Substring(0, marker) + ":tech:json:" +
                namespaceUri.Substring(marker + xsdMarker.Length);
        }
        if (messageIdentifier.Length > 0)
        {
            return "urn:iso:std:iso:20022:tech:json:" + messageIdentifier;
        }
        return namespaceUri + "#json";
    }

    internal static void WriteElementJsonValue(
        Utf8JsonWriter writer,
        DynamicXmlNode element,
        string messageNamespace,
        string path,
        MessageJsonResult result)
    {
        if (element.IsNil)
        {
            writer.WriteNullValue();
            return;
        }
        bool hasElements = HasElementChildren(element);
        string text = CollectTextChildren(element, path, result);
        if (!hasElements && element.Attributes.Count == 0)
        {
            writer.WriteStringValue(text);
            return;
        }

        writer.WriteStartObject();
        int attributeIndex = 0;
        while (attributeIndex < element.Attributes.Count)
        {
            XmlAttributeData attribute = element.Attributes[attributeIndex];
            if (attribute.Name.NamespaceUri == Iso20022Namespaces.XmlSchemaInstance &&
                attribute.Name.LocalName == "nil")
            {
                attributeIndex++;
                continue;
            }
            if (attribute.Name.NamespaceUri.Length > 0 &&
                attribute.Name.NamespaceUri != Iso20022Namespaces.Xml)
            {
                AddLossDiagnostic(
                    result,
                    "json.wildcard_attribute_namespace_loss",
                    "A namespace-qualified wildcard attribute was represented by its local name.",
                    path + "/@" + attribute.Name.LocalName);
            }
            writer.WriteString("@" + attribute.Name.LocalName, attribute.Value);
            attributeIndex++;
        }
        List<string> names = new List<string>();
        int childIndex = 0;
        while (childIndex < element.Children.Count)
        {
            DynamicXmlNode child = element.Children[childIndex];
            if (child.Kind == XmlNodeKind.Element &&
                !ContainsOrdinalText(names, child.Name.LocalName))
            {
                names.Add(child.Name.LocalName);
                int occurrenceCount = CountNamedElementChildren(
                    element,
                    child.Name.LocalName);
                writer.WritePropertyName(child.Name.LocalName);
                if (occurrenceCount > 1)
                {
                    writer.WriteStartArray();
                }
                int occurrenceIndex = 0;
                while (occurrenceIndex < element.Children.Count)
                {
                    DynamicXmlNode occurrence = element.Children[occurrenceIndex];
                    if (occurrence.Kind == XmlNodeKind.Element &&
                        occurrence.Name.LocalName == child.Name.LocalName)
                    {
                        if (occurrence.Name.NamespaceUri != messageNamespace)
                        {
                            AddLossDiagnostic(
                                result,
                                "json.wildcard_namespace_loss",
                                "A wildcard element namespace was represented only by its local name.",
                                path + "/" + occurrence.Name.LocalName);
                        }
                        WriteElementJsonValue(
                            writer,
                            occurrence,
                            messageNamespace,
                            path + "/" + occurrence.Name.LocalName,
                            result);
                    }
                    occurrenceIndex++;
                }
                if (occurrenceCount > 1)
                {
                    writer.WriteEndArray();
                }
            }
            childIndex++;
        }
        if (text.Length > 0)
        {
            writer.WriteString("$value", text);
            if (hasElements)
            {
                AddLossDiagnostic(
                    result,
                    "json.wildcard_mixed_content_loss",
                    "Mixed wildcard content order cannot be represented exactly in JSON.",
                    path);
            }
        }
        writer.WriteEndObject();
    }

    internal static bool HasElementChildren(DynamicXmlNode element)
    {
        int index = 0;
        while (index < element.Children.Count)
        {
            if (element.Children[index].Kind == XmlNodeKind.Element)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static string CollectTextChildren(
        DynamicXmlNode element,
        string path,
        MessageJsonResult result)
    {
        StringBuilder value = new StringBuilder();
        int index = 0;
        while (index < element.Children.Count)
        {
            DynamicXmlNode child = element.Children[index];
            if (child.Kind == XmlNodeKind.Text || child.Kind == XmlNodeKind.CData)
            {
                value.Append(child.Value);
                if (child.Kind == XmlNodeKind.CData)
                {
                    AddLossDiagnostic(
                        result,
                        "json.wildcard_node_kind_loss",
                        "CDATA was represented as ordinary JSON text.",
                        path);
                }
            }
            else if (child.Kind == XmlNodeKind.Comment ||
                child.Kind == XmlNodeKind.ProcessingInstruction)
            {
                AddLossDiagnostic(
                    result,
                    "json.wildcard_node_kind_loss",
                    "XML comments and processing instructions are not represented in JSON.",
                    path);
            }
            index++;
        }
        return value.ToString();
    }

    internal static int CountNamedElementChildren(
        DynamicXmlNode element,
        string localName)
    {
        int count = 0;
        int index = 0;
        while (index < element.Children.Count)
        {
            DynamicXmlNode child = element.Children[index];
            if (child.Kind == XmlNodeKind.Element &&
                child.Name.LocalName == localName)
            {
                count++;
            }
            index++;
        }
        return count;
    }

    internal static bool ContainsOrdinalText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static void AddLossDiagnostic(
        MessageJsonResult result,
        string code,
        string message,
        string path)
    {
        int index = 0;
        while (index < result.Diagnostics.Count)
        {
            MessageJsonDiagnostic existing = result.Diagnostics[index];
            if (existing.Code == code && existing.Path == path)
            {
                return;
            }
            index++;
        }
        MessageJsonDiagnostic diagnostic = new MessageJsonDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Path = path;
        result.Diagnostics.Add(diagnostic);
        result.IsLossless = false;
    }

    internal static DynamicXmlNode CreateElementFromJsonValue(
        string localName,
        string namespaceUri,
        JsonElement value,
        string path,
        MessageJsonResult result)
    {
        DynamicXmlNode element = new DynamicXmlNode();
        element.Kind = XmlNodeKind.Element;
        element.Name.LocalName = localName;
        element.Name.NamespaceUri = namespaceUri;
        if (value.ValueKind == JsonValueKind.Null)
        {
            element.IsNil = true;
            return element;
        }
        if (value.ValueKind != JsonValueKind.Object)
        {
            AddScalarTextNode(element, value);
            return element;
        }

        foreach (JsonProperty property in value.EnumerateObject())
        {
            if (property.Name.Length > 1 && property.Name[0] == '@')
            {
                XmlAttributeData attribute = new XmlAttributeData();
                attribute.Name.LocalName = property.Name.Substring(1);
                attribute.Value = ReadScalarText(property.Value);
                attribute.IsSpecified = true;
                element.Attributes.Add(attribute);
            }
            else if (property.Name == "$value")
            {
                AddScalarTextNode(element, property.Value);
            }
            else if (property.Name.Length > 0 && property.Name[0] == '$')
            {
                AddLossDiagnostic(
                    result,
                    "json.reserved_property_loss",
                    "An unsupported reserved JSON property was ignored.",
                    path + "/" + property.Name);
            }
            else
            {
                AddJsonPropertyChildren(
                    element,
                    property.Name,
                    namespaceUri,
                    property.Value,
                    path + "/" + property.Name,
                    result);
            }
        }
        return element;
    }

    internal static void AddJsonPropertyChildren(
        DynamicXmlNode parent,
        string localName,
        string namespaceUri,
        JsonElement value,
        string path,
        MessageJsonResult result)
    {
        if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in value.EnumerateArray())
            {
                parent.Children.Add(CreateElementFromJsonValue(
                    localName,
                    namespaceUri,
                    item,
                    path,
                    result));
            }
            return;
        }
        parent.Children.Add(CreateElementFromJsonValue(
            localName,
            namespaceUri,
            value,
            path,
            result));
    }

    internal static void AddScalarTextNode(DynamicXmlNode parent, JsonElement value)
    {
        DynamicXmlNode text = new DynamicXmlNode();
        text.Kind = XmlNodeKind.Text;
        text.Value = ReadScalarText(value);
        parent.Children.Add(text);
    }

    internal static string ReadScalarText(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.String)
        {
            return value.GetString() ?? string.Empty;
        }
        if (value.ValueKind == JsonValueKind.True)
        {
            return "true";
        }
        if (value.ValueKind == JsonValueKind.False)
        {
            return "false";
        }
        if (value.ValueKind == JsonValueKind.Null)
        {
            return string.Empty;
        }
        return value.GetRawText();
    }
}
