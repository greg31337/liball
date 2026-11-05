namespace xml;

/// <summary>
/// Compiles XML Schema documents into an immutable schema set.
/// </summary>
public interface IXmlSchemaCompiler
{
    /// <summary>
    /// Compiles a set of schema inputs.
    /// </summary>
    /// <param name="schemas">The schema document inputs.</param>
    /// <param name="options">The schema processing options.</param>
    /// <param name="resolver">The schema resource resolver.</param>
    /// <returns>The compiled schema set.</returns>
    XmlSchemaSet CompileXmlSchemas(XmlInput[] schemas, XmlSchemaValidationOptions options, IXmlResourceResolver resolver);
}

