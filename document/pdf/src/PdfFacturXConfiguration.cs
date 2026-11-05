using System;
using System.Text;
using System.Xml;

namespace pdf;

internal static class PdfFacturXConfiguration
{
    internal const string FacturXNamespace = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";
    internal const string CrossIndustryInvoiceNamespace = "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100";
    internal const string ReusableAggregateNamespace = "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100";

    internal static void Configure(PdfDocument document, PdfFacturXMetadata metadata, byte[] xmlData)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        ValidateMetadata(metadata);
        ValidateXml(xmlData, metadata.Profile);
        if (document.HasFacturX)
        {
            throw new InvalidOperationException("Factur-X/ZUGFeRD data has already been configured.");
        }

        PdfAssociatedFileRelationship relationship = GetRelationship(metadata.Profile);
        document.AddAssociatedFile(metadata.DocumentFileName, xmlData, "text/xml", "Factur-X/ZUGFeRD structured invoice data", relationship);
        document.FacturXMetadata = metadata.Copy();
        document.HasFacturX = true;
    }

    internal static void ValidateMetadata(PdfFacturXMetadata metadata)
    {
        if (metadata == null)
        {
            throw new ArgumentNullException("metadata");
        }

        GetConformanceLevel(metadata.Profile);
        ValidateExactText(metadata.DocumentFileName, "factur-x.xml", "DocumentFileName");
        ValidateExactText(metadata.DocumentType, "INVOICE", "DocumentType");
        ValidateExactText(metadata.Version, "1.0", "Version");
        ValidateExactText(metadata.NamespaceUri, FacturXNamespace, "NamespaceUri");
    }

    internal static void ValidateXml(byte[] xmlData, PdfFacturXProfile profile)
    {
        if (xmlData == null)
        {
            throw new ArgumentNullException("xmlData");
        }

        if (xmlData.Length == 0)
        {
            throw new ArgumentException("Factur-X/ZUGFeRD XML data cannot be empty.", "xmlData");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.DtdProcessing = DtdProcessing.Prohibit;
        settings.XmlResolver = null;
        XmlDocument xml = new XmlDocument();
        xml.XmlResolver = null;
        using (System.IO.MemoryStream stream = new System.IO.MemoryStream(xmlData, false))
        using (XmlReader reader = XmlReader.Create(stream, settings))
        {
            xml.Load(reader);
        }

        XmlElement? root = xml.DocumentElement;
        if (root == null || root.LocalName != "CrossIndustryInvoice" || root.NamespaceURI != CrossIndustryInvoiceNamespace)
        {
            throw new ArgumentException("Factur-X/ZUGFeRD data must contain a UN/CEFACT CrossIndustryInvoice root element.", "xmlData");
        }

        string expectedGuideline = GetGuidelineIdentifier(profile);
        string actualGuideline = FindGuidelineIdentifier(xml);
        if (actualGuideline != expectedGuideline)
        {
            throw new ArgumentException("The XML guideline identifier does not match the selected Factur-X/ZUGFeRD profile.", "xmlData");
        }
    }

    internal static string FindGuidelineIdentifier(XmlDocument xml)
    {
        XmlNodeList identifiers = xml.GetElementsByTagName("ID", ReusableAggregateNamespace);
        foreach (XmlNode identifier in identifiers)
        {
            XmlNode? parent = identifier.ParentNode;
            if (parent != null && parent.LocalName == "GuidelineSpecifiedDocumentContextParameter")
            {
                return identifier.InnerText;
            }
        }

        return string.Empty;
    }

    internal static PdfAssociatedFileRelationship GetRelationship(PdfFacturXProfile profile)
    {
        if (profile == PdfFacturXProfile.Minimum || profile == PdfFacturXProfile.BasicWl)
        {
            return PdfAssociatedFileRelationship.Data;
        }

        GetConformanceLevel(profile);
        return PdfAssociatedFileRelationship.Alternative;
    }

    internal static string GetConformanceLevel(PdfFacturXProfile profile)
    {
        if (profile == PdfFacturXProfile.Minimum)
        {
            return "MINIMUM";
        }

        if (profile == PdfFacturXProfile.BasicWl)
        {
            return "BASIC WL";
        }

        if (profile == PdfFacturXProfile.Basic)
        {
            return "BASIC";
        }

        if (profile == PdfFacturXProfile.En16931)
        {
            return "EN 16931";
        }

        if (profile == PdfFacturXProfile.Extended)
        {
            return "EXTENDED";
        }

        throw new ArgumentException("Factur-X/ZUGFeRD profile is not supported.", "Profile");
    }

    internal static string GetGuidelineIdentifier(PdfFacturXProfile profile)
    {
        if (profile == PdfFacturXProfile.Minimum)
        {
            return "urn:factur-x.eu:1p0:minimum";
        }

        if (profile == PdfFacturXProfile.BasicWl)
        {
            return "urn:factur-x.eu:1p0:basicwl";
        }

        if (profile == PdfFacturXProfile.Basic)
        {
            return "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic";
        }

        if (profile == PdfFacturXProfile.En16931)
        {
            return "urn:cen.eu:en16931:2017";
        }

        if (profile == PdfFacturXProfile.Extended)
        {
            return "urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended";
        }

        throw new ArgumentException("Factur-X/ZUGFeRD profile is not supported.", "profile");
    }

    internal static void ValidatePreparedDocument(PdfDocument document)
    {
        if (!document.HasFacturX)
        {
            return;
        }

        ValidateMetadata(document.FacturXMetadata);
        if (!document.HasPdfA || document.PdfAOptions.Conformance != PdfAConformance.PdfA3B)
        {
            throw new InvalidOperationException("Factur-X/ZUGFeRD output requires PDF/A-3b preparation.");
        }

        PdfAttachment? invoiceData = null;
        foreach (PdfAttachment attachment in document.Attachments)
        {
            if (attachment.FileName == document.FacturXMetadata.DocumentFileName)
            {
                invoiceData = attachment;
            }
        }

        if (invoiceData == null || invoiceData.MimeType != "text/xml" || !invoiceData.IsAssociatedFile || invoiceData.Relationship != GetRelationship(document.FacturXMetadata.Profile))
        {
            throw new InvalidOperationException("Factur-X/ZUGFeRD XML attachment metadata is incomplete or inconsistent.");
        }

        ValidateXml(invoiceData.Data, document.FacturXMetadata.Profile);
    }

    internal static void AppendXmp(StringBuilder xmp, PdfFacturXMetadata metadata)
    {
        ValidateMetadata(metadata);
        string conformance = GetConformanceLevel(metadata.Profile);
        xmp.Append("<rdf:Description rdf:about=\"\" xmlns:fx=\"");
        xmp.Append(metadata.NamespaceUri);
        xmp.AppendLine("\">");
        AppendXmpValue(xmp, "DocumentType", metadata.DocumentType);
        AppendXmpValue(xmp, "DocumentFileName", metadata.DocumentFileName);
        AppendXmpValue(xmp, "Version", metadata.Version);
        AppendXmpValue(xmp, "ConformanceLevel", conformance);
        xmp.AppendLine("</rdf:Description>");
        xmp.AppendLine("<rdf:Description rdf:about=\"\" xmlns:pdfaExtension=\"http://www.aiim.org/pdfa/ns/extension/\" xmlns:pdfaSchema=\"http://www.aiim.org/pdfa/ns/schema#\" xmlns:pdfaProperty=\"http://www.aiim.org/pdfa/ns/property#\">");
        xmp.AppendLine("<pdfaExtension:schemas><rdf:Bag><rdf:li rdf:parseType=\"Resource\">");
        xmp.AppendLine("<pdfaSchema:schema>Factur-X PDFA Extension Schema</pdfaSchema:schema>");
        xmp.Append("<pdfaSchema:namespaceURI>");
        xmp.Append(metadata.NamespaceUri);
        xmp.AppendLine("</pdfaSchema:namespaceURI>");
        xmp.AppendLine("<pdfaSchema:prefix>fx</pdfaSchema:prefix>");
        xmp.AppendLine("<pdfaSchema:property><rdf:Seq>");
        AppendPropertyDefinition(xmp, "DocumentFileName", "name of the embedded XML invoice file");
        AppendPropertyDefinition(xmp, "DocumentType", "INVOICE");
        AppendPropertyDefinition(xmp, "Version", "version of the Factur-X XML schema");
        AppendPropertyDefinition(xmp, "ConformanceLevel", "conformance level of the embedded Factur-X data");
        xmp.AppendLine("</rdf:Seq></pdfaSchema:property>");
        xmp.AppendLine("</rdf:li></rdf:Bag></pdfaExtension:schemas>");
        xmp.AppendLine("</rdf:Description>");
    }

    internal static void AppendXmpValue(StringBuilder xmp, string name, string value)
    {
        xmp.Append("<fx:");
        xmp.Append(name);
        xmp.Append(">");
        xmp.Append(PdfAConfiguration.EscapeXml(value));
        xmp.Append("</fx:");
        xmp.Append(name);
        xmp.AppendLine(">");
    }

    internal static void AppendPropertyDefinition(StringBuilder xmp, string name, string description)
    {
        xmp.AppendLine("<rdf:li rdf:parseType=\"Resource\">");
        xmp.Append("<pdfaProperty:name>");
        xmp.Append(name);
        xmp.AppendLine("</pdfaProperty:name>");
        xmp.AppendLine("<pdfaProperty:valueType>Text</pdfaProperty:valueType>");
        xmp.AppendLine("<pdfaProperty:category>external</pdfaProperty:category>");
        xmp.Append("<pdfaProperty:description>");
        xmp.Append(description);
        xmp.AppendLine("</pdfaProperty:description>");
        xmp.AppendLine("</rdf:li>");
    }

    internal static void ValidateExactText(string value, string expected, string name)
    {
        if (value == null)
        {
            throw new ArgumentNullException(name);
        }

        if (value != expected)
        {
            throw new ArgumentException("Factur-X/ZUGFeRD metadata does not contain the required standardized value.", name);
        }
    }
}
