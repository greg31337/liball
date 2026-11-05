using System.Text;
using System.Xml;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfFacturXInvoiceTests
{
    [Test]
    public void FacturXInvoice_BuildsPdfA3WithEmbeddedEn16931XmlAndExtensionMetadata()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfTrueTypeFont font = RegisterAsciiFont(invoice.GetDocument());
        byte[] xmlData = FacturXXml("urn:cen.eu:en16931:2017");
        PdfFacturXMetadata facturX = new PdfFacturXMetadata(PdfFacturXProfile.En16931);
        PdfAPreparationOptions pdfA = PdfAOptions();

        invoice.SetEmbeddedFont(font);
        invoice.SetInvoiceDetails("INV-EN-1", "2026-08-26", "2026-09-25");
        invoice.SetSeller(new PdfParty("ABC Seller"));
        invoice.SetBuyer(new PdfParty("ABC Buyer"));
        invoice.AddLine(new PdfInvoiceLine("ABC", 1, 25));
        invoice.SetFacturXData(facturX, xmlData, pdfA);
        facturX.Profile = PdfFacturXProfile.Minimum;
        facturX.DocumentFileName = "changed.xml";
        pdfA.IccProfileData[40] = 99;

        PdfDocument document = invoice.Build();
        string pdf = Encoding.Latin1.GetString(document.ToBytes());
        PdfAttachment attachment = document.Attachments[0];
        XmlDocument xmp = new XmlDocument();
        xmp.LoadXml(document.Metadata.XmpMetadata!);
        XmlNamespaceManager namespaces = new XmlNamespaceManager(xmp.NameTable);
        namespaces.AddNamespace("pdfaProperty", "http://www.aiim.org/pdfa/ns/property#");
        XmlNodeList propertyNames = xmp.SelectNodes("//pdfaProperty:name", namespaces)!;

        Assert.That(document.IsPreparedForPdfA(), Is.True);
        Assert.That(document.GetPdfAConformance(), Is.EqualTo(PdfAConformance.PdfA3B));
        Assert.That(document.ValidatePdfAConformance().IsValid(), Is.True);
        Assert.That(document.PdfAOptions.IccProfileData[40], Is.EqualTo(0));
        Assert.That(attachment.FileName, Is.EqualTo("factur-x.xml"));
        Assert.That(attachment.Data, Is.EqualTo(xmlData));
        Assert.That(attachment.MimeType, Is.EqualTo("text/xml"));
        Assert.That(attachment.IsAssociatedFile, Is.True);
        Assert.That(attachment.Relationship, Is.EqualTo(PdfAssociatedFileRelationship.Alternative));
        Assert.That(propertyNames.Count, Is.EqualTo(4));
        Assert.That(pdf, Does.StartWith("%PDF-1.7"));
        Assert.That(pdf, Does.Contain("/AFRelationship /Alternative"));
        Assert.That(pdf, Does.Contain(" /AF ["));
        Assert.That(pdf, Does.Contain("/Subtype /text#2Fxml"));
        Assert.That(pdf, Does.Contain("pdfaid:part=\"3\" pdfaid:conformance=\"B\""));
        Assert.That(pdf, Does.Contain("xmlns:fx=\"urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#\""));
        Assert.That(pdf, Does.Contain("<fx:DocumentType>INVOICE</fx:DocumentType>"));
        Assert.That(pdf, Does.Contain("<fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>"));
        Assert.That(pdf, Does.Contain("<fx:Version>1.0</fx:Version>"));
        Assert.That(pdf, Does.Contain("<fx:ConformanceLevel>EN 16931</fx:ConformanceLevel>"));
        Assert.That(pdf, Does.Contain("<pdfaSchema:prefix>fx</pdfaSchema:prefix>"));
        Assert.That(pdf, Does.Contain("<pdfaProperty:name>ConformanceLevel</pdfaProperty:name>"));
        Assert.That(pdf, Does.Contain("/FontFile2 "));
        Assert.That(pdf, Does.Not.Contain("/BaseFont /Helvetica"));
        Assert.That(pdf, Does.Contain("urn:cen.eu:en16931:2017"));
    }

    [Test]
    public void CreditNote_RendersOriginalInvoiceReasonAndCreditTotal()
    {
        PdfInvoice creditNote = new PdfInvoice();
        creditNote.SetDocumentType(PdfInvoiceDocumentType.CreditNote);
        creditNote.SetCreditNoteReference("INV-100", "Returned goods");
        creditNote.SetInvoiceDetails("CN-100", "2026-08-26", string.Empty);
        creditNote.AddLine(new PdfInvoiceLine("Returned item", 2, 15));

        string content = AllPageContent(creditNote.Build());

        Assert.That(content, Does.Contain("(CREDIT NOTE) Tj"));
        Assert.That(content, Does.Contain("(Credit Note) Tj"));
        Assert.That(content, Does.Contain("(Credit Note #) Tj"));
        Assert.That(content, Does.Contain("(Credit Note Date) Tj"));
        Assert.That(content, Does.Contain("(Original Invoice) Tj"));
        Assert.That(content, Does.Contain("(INV-100) Tj"));
        Assert.That(content, Does.Contain("(Credit Reason) Tj"));
        Assert.That(content, Does.Contain("(Returned goods) Tj"));
        Assert.That(content, Does.Contain("(Credit Total) Tj"));
        Assert.That(content, Does.Contain("($30.00) Tj"));
    }

    [Test]
    public void FacturXProfiles_UseValidatedGuidelinesAndRelationships()
    {
        AssertProfile(PdfFacturXProfile.Minimum, "urn:factur-x.eu:1p0:minimum", PdfAssociatedFileRelationship.Data);
        AssertProfile(PdfFacturXProfile.BasicWl, "urn:factur-x.eu:1p0:basicwl", PdfAssociatedFileRelationship.Data);
        AssertProfile(PdfFacturXProfile.Basic, "urn:cen.eu:en16931:2017#compliant#urn:factur-x.eu:1p0:basic", PdfAssociatedFileRelationship.Alternative);
        AssertProfile(PdfFacturXProfile.En16931, "urn:cen.eu:en16931:2017", PdfAssociatedFileRelationship.Alternative);
        AssertProfile(PdfFacturXProfile.Extended, "urn:cen.eu:en16931:2017#conformant#urn:factur-x.eu:1p0:extended", PdfAssociatedFileRelationship.Alternative);
    }

    [Test]
    public void FacturXAndCreditNoteHelpers_RejectInvalidInputs()
    {
        PdfInvoice invoice = new PdfInvoice();
        PdfAPreparationOptions pdfA = PdfAOptions();
        PdfFacturXMetadata metadata = new PdfFacturXMetadata(PdfFacturXProfile.En16931);
        byte[] validXml = FacturXXml("urn:cen.eu:en16931:2017");
        PdfDocument otherDocument = new PdfDocument();
        PdfTrueTypeFont foreignFont = RegisterAsciiFont(otherDocument);

        Assert.Throws<ArgumentException>(() => invoice.SetDocumentType((PdfInvoiceDocumentType)99));
        Assert.Throws<ArgumentNullException>(() => invoice.SetCreditNoteReference(null!, "reason"));
        Assert.Throws<ArgumentException>(() => invoice.SetCreditNoteReference(string.Empty, "reason"));
        Assert.Throws<ArgumentException>(() => invoice.SetCreditNoteReference("INV", string.Empty));
        Assert.Throws<ArgumentNullException>(() => invoice.SetEmbeddedFont(null!));
        Assert.Throws<ArgumentException>(() => invoice.SetEmbeddedFont(foreignFont));
        Assert.Throws<ArgumentNullException>(() => invoice.SetFacturXData(null!, validXml, pdfA));
        Assert.Throws<ArgumentNullException>(() => invoice.SetFacturXData(metadata, null!, pdfA));
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, new byte[0], pdfA));

        PdfAPreparationOptions pdfA2 = PdfAOptions();
        pdfA2.Conformance = PdfAConformance.PdfA2B;
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA2));
        Assert.Throws<System.Xml.XmlException>(() => invoice.SetFacturXData(metadata, Encoding.UTF8.GetBytes("<broken>"), pdfA));
        Assert.Throws<System.Xml.XmlException>(() => invoice.SetFacturXData(metadata, Encoding.UTF8.GetBytes("<!DOCTYPE x [<!ENTITY e SYSTEM 'file:///tmp/data'>]><x>&e;</x>"), pdfA));
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, Encoding.UTF8.GetBytes("<x/>"), pdfA));
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, FacturXXml("urn:factur-x.eu:1p0:basic"), pdfA));

        metadata.DocumentFileName = "invoice.xml";
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA));
        metadata.DocumentFileName = "factur-x.xml";
        metadata.DocumentType = "CREDIT_NOTE";
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA));
        metadata.DocumentType = "INVOICE";
        metadata.Version = "2.0";
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA));
        metadata.Version = "1.0";
        metadata.NamespaceUri = "urn:wrong";
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA));
        metadata.NamespaceUri = "urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#";
        metadata.Profile = (PdfFacturXProfile)99;
        Assert.Throws<ArgumentException>(() => invoice.SetFacturXData(metadata, validXml, pdfA));

        PdfInvoice missingFont = new PdfInvoice();
        missingFont.SetFacturXData(new PdfFacturXMetadata(PdfFacturXProfile.En16931), validXml, pdfA);
        Assert.Throws<InvalidOperationException>(() => missingFont.Build());
    }

    static void AssertProfile(PdfFacturXProfile profile, string guideline, PdfAssociatedFileRelationship relationship)
    {
        PdfInvoice invoice = new PdfInvoice();
        invoice.SetFacturXData(new PdfFacturXMetadata(profile), FacturXXml(guideline), PdfAOptions());

        Assert.That(invoice.GetDocument().Attachments[0].Relationship, Is.EqualTo(relationship));
    }

    static PdfTrueTypeFont RegisterAsciiFont(PdfDocument document)
    {
        PdfTrueTypeFont font = document.RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
        int[] codePoints = new int[95];
        int[] glyphIndices = new int[95];
        for (int index = 0; index < codePoints.Length; index++)
        {
            codePoints[index] = 32 + index;
            glyphIndices[index] = 1;
        }

        font.UnicodeCodePoints = codePoints;
        font.GlyphIndices = glyphIndices;
        return font;
    }

    static byte[] FacturXXml(string guideline)
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\" xmlns:ram=\"urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100\">"
            + "<rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>"
            + guideline
            + "</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext>"
            + "</rsm:CrossIndustryInvoice>";
        return Encoding.UTF8.GetBytes(xml);
    }

    static PdfAPreparationOptions PdfAOptions()
    {
        PdfAPreparationOptions options = new PdfAPreparationOptions();
        options.Conformance = PdfAConformance.PdfA3B;
        options.IccProfileData = RgbIccProfile();
        options.OutputConditionIdentifier = "Factur-X RGB";
        options.OutputCondition = "Factur-X RGB";
        options.Info = "Factur-X RGB";
        return options;
    }

    static byte[] RgbIccProfile()
    {
        byte[] profile = new byte[128];
        profile[3] = 128;
        profile[12] = (byte)'m';
        profile[13] = (byte)'n';
        profile[14] = (byte)'t';
        profile[15] = (byte)'r';
        profile[16] = (byte)'R';
        profile[17] = (byte)'G';
        profile[18] = (byte)'B';
        profile[19] = (byte)' ';
        profile[20] = (byte)'X';
        profile[21] = (byte)'Y';
        profile[22] = (byte)'Z';
        profile[23] = (byte)' ';
        profile[36] = (byte)'a';
        profile[37] = (byte)'c';
        profile[38] = (byte)'s';
        profile[39] = (byte)'p';
        return profile;
    }

    static string AllPageContent(PdfDocument document)
    {
        string content = string.Empty;
        for (int index = 0; index < document.Pages.Count; index++)
        {
            content += document.Pages[index].Canvas.ToPdfString();
        }

        return content;
    }
}
