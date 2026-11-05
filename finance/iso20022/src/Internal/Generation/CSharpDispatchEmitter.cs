using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal static class CSharpDispatchEmitter
{
    internal static void WriteResultClass(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(
            builder, context, 0, "internal class Iso20022GeneratedDispatchResult");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal bool IsTyped;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal object? Model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal global::iso20022.MessageDocument? GenericDocument;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal string MessageIdentifier = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal string NamespaceUri = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal string ModelTypeName = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal string DiagnosticCode = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal string DiagnosticMessage = string.Empty;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 0, "}");
    }

    internal static void WriteRegistryDispatchMethods(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        WriteReadMessageText(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteGenericFallback(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteCastMessageModel(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteJsonConversionMethods(builder, context, entries);
    }

    internal static void WriteReadMessageText(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static Iso20022GeneratedDispatchResult ReadMessageText(" +
            "string xml, string messageIdentifier, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (xml == null || messageIdentifier == null || namespaceUri == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.ArgumentNullException(" +
            "\"Typed dispatch inputs cannot be null.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.IO.StringReader textReader = " +
            "new global::System.IO.StringReader(xml);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlReader reader = " +
            "global::System.Xml.XmlReader.Create(textReader);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "reader.MoveToContent();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string actualNamespace = reader.NamespaceURI;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string actualRoot = reader.LocalName;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (actualNamespace != namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return CreateGenericFallback(xml, messageIdentifier, namespaceUri, " +
            "\"generation.codec.namespace_mismatch\", " +
            "\"The XML root namespace does not match the requested namespace.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (messageIdentifier + \"\\n\" + namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            CSharpRegistryEntry entry = entries[index];
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "case \"" + CSharpMetadataEmitter.EscapeCSharpString(
                    entry.MessageIdentifier + "\n" + entry.NamespaceUri) + "\":");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "if (actualRoot != \"" +
                CSharpMetadataEmitter.EscapeCSharpString(entry.RootElement) + "\")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 5,
                "return CreateGenericFallback(xml, messageIdentifier, namespaceUri, " +
                "\"generation.codec.root_mismatch\", " +
                "\"The XML root element does not match the generated model.\");");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "Iso20022GeneratedDispatchResult result" + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) +
                " = new Iso20022GeneratedDispatchResult();");
            string target = "result" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                target + ".IsTyped = true;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                target + ".Model = " + entry.CodecTypeName + ".ReadModel(reader);");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                target + ".MessageIdentifier = messageIdentifier;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                target + ".NamespaceUri = namespaceUri;");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                target + ".ModelTypeName = \"" +
                CSharpMetadataEmitter.EscapeCSharpString(entry.ModelTypeName) + "\";");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "reader.Dispose();");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "textReader.Dispose();");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "return " + target + ";");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "reader.Dispose();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "textReader.Dispose();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "string knownNamespace = FindMessageNamespace(messageIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "if (knownNamespace.Length > 0)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 5,
            "return CreateGenericFallback(xml, messageIdentifier, namespaceUri, " +
            "\"generation.codec.namespace_mismatch\", " +
            "\"The identifier is known, but its namespace does not match.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
        WriteKnownNamespaceCheck(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "return CreateGenericFallback(xml, messageIdentifier, namespaceUri, " +
            "\"generation.codec.generic_fallback\", " +
            "\"No generated model matches the exact identifier and namespace.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteKnownNamespaceCheck(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "switch (namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
        List<string> emitted = new List<string>();
        int index = 0;
        while (index < entries.Count)
        {
            string namespaceUri = entries[index].NamespaceUri;
            if (namespaceUri.Length > 0 &&
                !CSharpModelEmitter.TextListContains(emitted, namespaceUri))
            {
                emitted.Add(namespaceUri);
                CSharpModelEmitter.AppendSourceLine(builder, context, 5,
                    "case \"" + CSharpMetadataEmitter.EscapeCSharpString(
                        namespaceUri) + "\":");
                CSharpModelEmitter.AppendSourceLine(builder, context, 6,
                    "return CreateGenericFallback(xml, messageIdentifier, namespaceUri, " +
                    "\"generation.codec.identifier_mismatch\", " +
                    "\"The namespace is known, but its message identifier does not match.\");");
            }
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 5, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 6, "break;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
    }

    internal static void WriteGenericFallback(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static Iso20022GeneratedDispatchResult CreateGenericFallback(" +
            "string xml, string messageIdentifier, string namespaceUri, " +
            "string diagnosticCode, string diagnosticMessage)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.MessageReadResult generic = " +
            "new global::iso20022.XmlMessageReader().ReadGenericText(" +
            "xml, new global::iso20022.MessageReadOptions());");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "Iso20022GeneratedDispatchResult result = " +
            "new Iso20022GeneratedDispatchResult();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.MessageIdentifier = messageIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.NamespaceUri = namespaceUri;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.ModelTypeName = FindModelName(messageIdentifier);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.GenericDocument = generic.Document;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.DiagnosticCode = diagnosticCode;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "result.DiagnosticMessage = diagnosticMessage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!generic.Success)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "result.DiagnosticCode = generic.ErrorCode;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "result.DiagnosticMessage = generic.ErrorMessage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "return result;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteCastMessageModel(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static object CastMessageModel(" +
            "Iso20022GeneratedDispatchResult result, string requestedModelTypeName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidCastException(" +
            "\"generation.codec.cast_result_missing: A dispatch result is required.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!result.IsTyped || result.Model == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidCastException(" +
            "\"generation.codec.cast_generic_fallback: A generic fallback cannot be " +
            "cast to generated model '\" + requestedModelTypeName + \"'. Routing " +
            "diagnostic: \" + result.DiagnosticCode);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result.ModelTypeName != requestedModelTypeName)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "throw new global::System.InvalidCastException(" +
            "\"generation.codec.type_mismatch: Requested generated model '\" + " +
            "requestedModelTypeName + \"', but dispatch produced '\" + " +
            "result.ModelTypeName + \"'.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return result.Model;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteJsonConversionMethods(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        WriteMessageJsonMethod(builder, context);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteTypedXmlMethod(builder, context, entries);
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, string.Empty);
        WriteReadMessageJsonMethod(builder, context);
    }

    internal static void WriteMessageJsonMethod(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static global::iso20022.MessageJsonResult WriteMessageJson(" +
            "Iso20022GeneratedDispatchResult result)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.MessageJsonConverter converter = " +
            "new global::iso20022.MessageJsonConverter();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (result == null)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return converter.WriteGenericDocument(null);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!result.IsTyped)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return converter.WriteGenericDocument(result.GenericDocument);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "string xml = WriteTypedMessageXml(result);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.MessageReadResult generic = " +
            "new global::iso20022.XmlMessageReader().ReadGenericText(" +
            "xml, new global::iso20022.MessageReadOptions());");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return converter.WriteGenericDocument(generic.Document);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteTypedXmlMethod(
        StringBuilder builder,
        CSharpGenerationContext context,
        List<CSharpRegistryEntry> entries)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static string WriteTypedMessageXml(" +
            "Iso20022GeneratedDispatchResult result)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Text.StringBuilder output = " +
            "new global::System.Text.StringBuilder();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlWriterSettings settings = " +
            "new global::System.Xml.XmlWriterSettings();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "settings.OmitXmlDeclaration = true;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "settings.NamespaceHandling = " +
            "global::System.Xml.NamespaceHandling.OmitDuplicates;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::System.Xml.XmlWriter writer = " +
            "global::System.Xml.XmlWriter.Create(output, settings);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "switch (result.MessageIdentifier + \"\\n\" + result.NamespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        int index = 0;
        while (index < entries.Count)
        {
            CSharpRegistryEntry entry = entries[index];
            CSharpModelEmitter.AppendSourceLine(builder, context, 3,
                "case \"" + CSharpMetadataEmitter.EscapeCSharpString(
                    entry.MessageIdentifier + "\n" + entry.NamespaceUri) + "\":");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                "if (result.Model == null || result.ModelTypeName != \"" +
                CSharpMetadataEmitter.EscapeCSharpString(entry.ModelTypeName) + "\")");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "{");
            CSharpModelEmitter.AppendSourceLine(builder, context, 5,
                "writer.Dispose();");
            CSharpModelEmitter.AppendSourceLine(builder, context, 5,
                "throw new global::System.InvalidCastException(" +
                "\"generation.codec.type_mismatch: The typed JSON model does not \" + " +
                "\"match its dispatch identity.\");");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "}");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4,
                entry.CodecTypeName + ".WriteDocument(writer, (" +
                entry.ModelTypeName + ")result.Model, result.MessageIdentifier, " +
                "result.NamespaceUri);");
            CSharpModelEmitter.AppendSourceLine(builder, context, 4, "break;");
            index++;
        }
        CSharpModelEmitter.AppendSourceLine(builder, context, 3, "default:");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "writer.Dispose();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 4,
            "throw new global::System.InvalidCastException(" +
            "\"generation.codec.json_model_unknown: No generated JSON codec matches \" + " +
            "\"the typed dispatch identity.\");");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "writer.Dispose();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return output.ToString();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }

    internal static void WriteReadMessageJsonMethod(
        StringBuilder builder,
        CSharpGenerationContext context)
    {
        CSharpModelEmitter.AppendSourceLine(builder, context, 1,
            "internal static Iso20022GeneratedDispatchResult ReadMessageJson(" +
            "string json, string messageIdentifier, string namespaceUri)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "global::iso20022.MessageJsonResult converted = " +
            "new global::iso20022.MessageJsonConverter().ReadGenericText(json);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "if (!converted.Success)");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "{");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "Iso20022GeneratedDispatchResult failure = " +
            "new Iso20022GeneratedDispatchResult();");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "failure.MessageIdentifier = messageIdentifier;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "failure.NamespaceUri = namespaceUri;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "failure.DiagnosticCode = converted.ErrorCode;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "failure.DiagnosticMessage = converted.ErrorMessage;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 3,
            "return failure;");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2, "}");
        CSharpModelEmitter.AppendSourceLine(builder, context, 2,
            "return ReadMessageText(converted.Xml, messageIdentifier, namespaceUri);");
        CSharpModelEmitter.AppendSourceLine(builder, context, 1, "}");
    }
}
