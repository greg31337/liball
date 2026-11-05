namespace iso20022;

/// <summary>Contains stable ISO 20022 and XML namespace identifiers.</summary>
public static class Iso20022Namespaces
{
    /// <summary>The prefix shared by ISO 20022 technical namespace URNs.</summary>
    public const string TechnicalUrnPrefix = "urn:iso:std:iso:20022:tech:";
    /// <summary>The prefix used by ISO 20022 XSD namespace URNs.</summary>
    public const string SchemaUrnPrefix = "urn:iso:std:iso:20022:tech:xsd:";
    /// <summary>The namespace of the ISO 20022:2013 Ecore metamodel.</summary>
    public const string Ecore2013 = "urn:iso:std:iso:20022:2013:ecore";
    /// <summary>The extension namespace of the ISO 20022:2013 Ecore metamodel.</summary>
    public const string Ecore2013Extension = "urn:iso:std:iso:20022:2013:ecore:extension";
    /// <summary>The XMI namespace used by the repository artifact.</summary>
    public const string Xmi = "http://www.omg.org/XMI";
    /// <summary>The Eclipse Modeling Framework Ecore namespace.</summary>
    public const string Ecore = "http://www.eclipse.org/emf/2002/Ecore";
    /// <summary>The XML namespace.</summary>
    public const string Xml = "http://www.w3.org/XML/1998/namespace";
    /// <summary>The XML namespace-declaration namespace.</summary>
    public const string Xmlns = "http://www.w3.org/2000/xmlns/";
    /// <summary>The XML Schema namespace.</summary>
    public const string XmlSchema = "http://www.w3.org/2001/XMLSchema";
    /// <summary>The XML Schema-instance namespace.</summary>
    public const string XmlSchemaInstance = "http://www.w3.org/2001/XMLSchema-instance";
    /// <summary>The XML Signature namespace.</summary>
    public const string XmlSignature = "http://www.w3.org/2000/09/xmldsig#";
}
