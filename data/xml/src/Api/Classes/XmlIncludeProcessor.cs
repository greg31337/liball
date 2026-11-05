namespace xml;

/// <summary>Provides the public XML Include processor implementation.</summary>
public class XmlIncludeProcessor : IXmlIncludeProcessor
{
    /// <summary>Creates an XML Include processor.</summary>
    public XmlIncludeProcessor()
    {
    }

    /// <summary>Processes XML Include instructions.</summary>
    public XmlDocument ProcessXmlIncludes(XmlDocument document, XmlIncludeOptions options, IXmlResourceResolver resolver)
    {
        XIncludeEngine engine = new XIncludeEngine(options, resolver);
        return engine.ProcessXmlIncludeDocument(document);
    }
}
