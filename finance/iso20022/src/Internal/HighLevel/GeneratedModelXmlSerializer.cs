using System;
using System.Reflection;
using System.Text;
using System.Xml;

namespace iso20022;

internal static class GeneratedModelXmlSerializer
{
    internal static GeneratedModelWriteResult WriteModel(object? model, MessageIdentifier? identifier, string namespaceUri)
    {
        GeneratedModelWriteResult result = new GeneratedModelWriteResult();
        if (model == null)
        {
            result.ErrorCode = "generated.model_missing";
            result.ErrorMessage = "A generated model instance is required.";
            return result;
        }
        MessageIdentifier? normalized = NormalizeGeneratedIdentifier(identifier);
        if (normalized == null)
        {
            result.ErrorCode = "generated.identity_missing";
            result.ErrorMessage = "An exact message identifier is required for a generated model.";
            return result;
        }
        string effectiveNamespace = namespaceUri;
        if (effectiveNamespace.Length == 0)
        {
            effectiveNamespace = Iso20022Namespaces.SchemaUrnPrefix + normalized.Value;
        }

        Type modelType = model.GetType();
        string modelNamespace = modelType.Namespace ?? string.Empty;
        Type? codecType = modelType.Assembly.GetType(modelNamespace + "." + modelType.Name + "Codec");
        if (codecType == null)
        {
            result.ErrorCode = "generated.codec_missing";
            result.ErrorMessage = "The generated model does not expose its XML codec.";
            return result;
        }
        MethodInfo? writeMethod = codecType.GetMethod(
            "WriteDocument",
            BindingFlags.NonPublic | BindingFlags.Static);
        if (writeMethod == null)
        {
            result.ErrorCode = "generated.codec_missing";
            result.ErrorMessage = "The generated model codec does not expose document writing.";
            return result;
        }

        StringBuilder output = new StringBuilder();
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
        using (XmlWriter writer = XmlWriter.Create(output, settings))
        {
            object?[] arguments = new object?[]
            {
                writer,
                model,
                normalized.Value,
                effectiveNamespace
            };
            writeMethod.Invoke(null, arguments);
            writer.Flush();
        }
        result.Success = true;
        result.Xml = output.ToString();
        return result;
    }

    internal static MessageIdentifier? NormalizeGeneratedIdentifier(MessageIdentifier? identifier)
    {
        if (identifier == null)
        {
            return null;
        }
        string value = MessageBuilder.GetExactIdentifierValue(identifier);
        if (value.Length == 0)
        {
            return null;
        }
        return MessageIdentifierLexical.ParseMessageIdentifier(value.AsSpan());
    }
}
