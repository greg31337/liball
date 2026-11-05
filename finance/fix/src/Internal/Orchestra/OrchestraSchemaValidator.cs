using System.Xml;
using System.Xml.Schema;

namespace fix;

internal static class OrchestraSchemaValidator
{
    internal static OrchestraXmlLoadResult Validate(OrchestraXmlLoadResult result)
    {
        if (!result.Success || result.Document == null) return result;
        result.Document.Schemas = OrchestraSchemaCatalog.GetSchemas(result.Version, result.InterfacesDocument);
        result.Document.Validate((sender, arguments) => AddSchemaDiagnostic(result, arguments));
        return OrchestraXmlLoader.Complete(result);
    }

    internal static void AddSchemaDiagnostic(OrchestraXmlLoadResult result, ValidationEventArgs arguments)
    {
        ValidationSeverity severity = arguments.Severity == XmlSeverityType.Warning ? ValidationSeverity.Warning : ValidationSeverity.Error;
        XmlSchemaValidationException? validationException = arguments.Exception as XmlSchemaValidationException;
        XmlNode? node = validationException == null ? null : validationException.SourceObject as XmlNode;
        XmlElement? element = node as XmlElement;
        if (element == null && node != null) element = node.ParentNode as XmlElement;
        if (element == null) element = result.Root;
        string code = severity == ValidationSeverity.Warning ? "orchestra-schema-validation-warning" : "orchestra-schema-validation-error";
        if (element != null)
        {
            OrchestraXmlLoader.AddElementDiagnostic(result, element, severity, code, arguments.Message);
        }
        else
        {
            OrchestraXmlLoader.AddDiagnostic(result, severity, code, arguments.Message, "", arguments.Exception.LineNumber, arguments.Exception.LinePosition, "");
        }
    }
}
