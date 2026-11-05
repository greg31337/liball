namespace xml;

/// <summary>Provides the public XML Schema compiler implementation.</summary>
public class XmlSchemaCompiler : IXmlSchemaCompiler
{
    /// <summary>Creates an XML Schema compiler.</summary>
    public XmlSchemaCompiler()
    {
    }

    /// <summary>Compiles XML Schema inputs.</summary>
    public XmlSchemaSet CompileXmlSchemas(XmlInput[] schemas, XmlSchemaValidationOptions options, IXmlResourceResolver resolver)
    {
        Guard.NotNull(schemas, "schemas");
        Guard.NotNull(options, "options");
        Guard.NotNull(resolver, "resolver");
        return new XmlSchemaDocumentLoader(options, resolver).CompileXmlSchemaInputs(schemas);
    }
}
