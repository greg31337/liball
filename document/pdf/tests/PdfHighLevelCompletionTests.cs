using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfHighLevelCompletionTests
{
    [Test]
    public void MissingOptionalTemplateDataDoesNotRenderEmptyMetadataLabels()
    {
        string certificate = Content(new PdfCertificate("Recipient", "Award").Build());
        string receipt = Content(new PdfReceipt("Merchant").Build());
        string contract = Content(new PdfContract("Agreement").Build());
        string packingSlip = Content(new PdfPackingSlip("PS-1").Build());
        string statement = Content(new PdfStatement("Account", "A-1").Build());
        string badge = Content(new PdfBadge("Name").Build());

        Assert.That(certificate, Does.Not.Contain("() Tj"));
        Assert.That(receipt, Does.Not.Contain("(Receipt ) Tj"));
        Assert.That(contract, Does.Not.Contain("(Between  and ) Tj"));
        Assert.That(contract, Does.Not.Contain("(Effective date: ) Tj"));
        Assert.That(packingSlip, Does.Not.Contain("(From: ) Tj"));
        Assert.That(packingSlip, Does.Not.Contain("(Ship to: ) Tj"));
        Assert.That(packingSlip, Does.Not.Contain("(Ship date:    Carrier: ) Tj"));
        Assert.That(packingSlip, Does.Not.Contain("(Tracking: ) Tj"));
        Assert.That(statement, Does.Not.Contain("(Period:  to ) Tj"));
        Assert.That(badge, Does.Not.Contain("() Tj"));
    }

    [Test]
    public void HighLevelNumericInputsRejectNonFiniteReceiptAndTableValues()
    {
        PdfReceipt receipt = new PdfReceipt("Merchant");
        PdfReportTable table = new PdfReportTable(new string[] { "A" });

        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.AddLine("item", double.NaN, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => receipt.AddLine("item", 1, double.PositiveInfinity));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetColumnWidths(new double[] { double.NaN }));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.SetColumnWidths(new double[] { double.NegativeInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.CreateBlock(double.PositiveInfinity, HeaderStyle(), BodyStyle()));
    }

    [Test]
    public void FlowBasedTemplatesPaginateAndRetainLateContent()
    {
        PdfContract contract = new PdfContract("Long Contract");
        PdfPackingSlip packingSlip = new PdfPackingSlip("PS-LONG");
        PdfStatement statement = new PdfStatement("Account", "A-LONG");
        PdfWorksheet worksheet = new PdfWorksheet("Long Worksheet");
        PdfFormTemplate form = new PdfFormTemplate("Long Form");
        for (int index = 0; index < 80; index++)
        {
            contract.AddClause(new PdfContractClause("Clause " + index.ToString(), "Contract body " + index.ToString()));
            packingSlip.AddItem(new PdfPackingSlipItem("SKU-" + index.ToString(), "Item " + index.ToString(), index + 1));
            statement.AddEntry(new PdfStatementEntry("2026-01-01", "Entry " + index.ToString(), 1));
            worksheet.AddQuestion(new PdfWorksheetQuestion("Question " + index.ToString(), 2));
            form.AddField(new PdfFormTemplateField("field_" + index.ToString(), "Field " + index.ToString(), PdfFormTemplateFieldKind.Text));
        }

        PdfDocument contractDocument = contract.Build();
        PdfDocument packingDocument = packingSlip.Build();
        PdfDocument statementDocument = statement.Build();
        PdfDocument worksheetDocument = worksheet.Build();
        PdfDocument formDocument = form.Build();

        Assert.That(contractDocument.GetPageCount(), Is.GreaterThan(1));
        Assert.That(packingDocument.GetPageCount(), Is.GreaterThan(1));
        Assert.That(statementDocument.GetPageCount(), Is.GreaterThan(1));
        Assert.That(worksheetDocument.GetPageCount(), Is.GreaterThan(1));
        Assert.That(formDocument.GetPageCount(), Is.GreaterThan(1));
        Assert.That(Content(contractDocument), Does.Contain("(80. Clause 79) Tj"));
        Assert.That(Content(packingDocument), Does.Contain("(Item 79) Tj"));
        Assert.That(Content(statementDocument), Does.Contain("(Entry 79) Tj"));
        Assert.That(Content(worksheetDocument), Does.Contain("(80. Question 79) Tj"));
        Assert.That(Content(formDocument), Does.Contain("(Field 79) Tj"));
    }

    [TestCase("certificate", "D2F7E055CF912B29D38197CEE2BDDF85FF1DB5780A6F2C0A80AF7329A155BF77")]
    [TestCase("letter", "BD613EDC68F6103BA0ECFC1752F235F6ED0FED6E42B6E32835C4A6BA71B53D66")]
    [TestCase("receipt", "5F5C026DC64050A28E4C4BF470C76C3BA2E6EA9F234FF9D30E969A28C8070D6F")]
    [TestCase("label", "928C4004F994D1626CD9736E72A9717857B81D72A57154F8F54ED8C762D93E5C")]
    [TestCase("contract", "87D47279CA67D928DCA990973FCB944CE476BA0CC5A693EEC34C988EE8C3AA82")]
    [TestCase("packing-slip", "F099C2F895CA8059703CAB399094E0A93287F771567867C5B442032A37AC41AA")]
    [TestCase("statement", "7499563B3BDC5FD1738EA1EB4E06B1CE550363799A6244D97F46A669017A7D7C")]
    [TestCase("worksheet", "2FEAEC6714FE5ECE26CE9628FEF2D5DC663BBC8B882E2542BA7593604059BF9F")]
    [TestCase("form", "EA84B55DD8A4F186F58E9350E044E8CDE8989229A17E766F97C6DB0DE83B13C5")]
    [TestCase("badge", "35F0DEBBAFA0876690327158753CBBADEC8BFB098EA53DAF562C307EC0056DC7")]
    [TestCase("credit-note", "560FF113A85A8D88DA60D6EA7F58AAF79484B33BA43B250AB31D69C540B099EB")]
    [TestCase("factur-x", "45BFEC53A8262A3F0BEC55310F779317B1996B91121C7A0306DA946934260574")]
    public void HighLevelTemplateMatchesDeterministicRenderedSnapshot(string name, string expectedHash)
    {
        PdfDocument first = CreateTemplate(name);
        PdfDocument second = CreateTemplate(name);
        string documentId = SnapshotDocumentId(name);
        first.UseDeterministicWriterModeWithId(documentId);
        second.UseDeterministicWriterModeWithId(documentId);

        byte[] firstBytes = first.ToBytes();
        byte[] secondBytes = second.ToBytes();
        string actualHash = Convert.ToHexString(SHA256.HashData(firstBytes));

        Assert.That(secondBytes, Is.EqualTo(firstBytes));
        Assert.That(actualHash, Is.EqualTo(expectedHash), name + " actual hash " + actualHash);
    }

    static PdfDocument CreateTemplate(string name)
    {
        if (name == "certificate")
        {
            PdfCertificate value = new PdfCertificate("Ada Lovelace", "Completion");
            value.SetIssuer("Institute");
            return value.Build();
        }

        if (name == "letter")
        {
            PdfLetter value = new PdfLetter();
            value.SetSender("Sender");
            value.SetRecipient("Recipient");
            value.SetBody("Body");
            return value.Build();
        }

        if (name == "receipt")
        {
            PdfReceipt value = new PdfReceipt("Merchant");
            value.AddLine("Item", 2, 3.5);
            return value.Build();
        }

        if (name == "label")
        {
            PdfLabelSheet value = new PdfLabelSheet(1, 2);
            value.AddLabel("Alpha");
            value.AddLabel("Beta");
            return value.Build();
        }

        if (name == "contract")
        {
            PdfContract value = new PdfContract("Agreement");
            value.AddClause(new PdfContractClause("Term", "Body"));
            return value.Build();
        }

        if (name == "packing-slip")
        {
            PdfPackingSlip value = new PdfPackingSlip("PS-1");
            value.AddItem(new PdfPackingSlipItem("SKU", "Item", 2));
            return value.Build();
        }

        if (name == "statement")
        {
            PdfStatement value = new PdfStatement("Account", "A-1");
            value.SetOpeningBalance(10);
            value.AddEntry(new PdfStatementEntry("2026-01-01", "Entry", -2));
            return value.Build();
        }

        if (name == "worksheet")
        {
            PdfWorksheet value = new PdfWorksheet("Worksheet");
            value.AddQuestion(new PdfWorksheetQuestion("Question", 2));
            return value.Build();
        }

        if (name == "form")
        {
            PdfFormTemplate value = new PdfFormTemplate("Form");
            value.AddField(new PdfFormTemplateField("name", "Name", PdfFormTemplateFieldKind.Text));
            return value.Build();
        }

        if (name == "badge")
        {
            PdfBadge value = new PdfBadge("Name");
            value.SetOrganization("Organization");
            return value.Build();
        }

        if (name == "credit-note")
        {
            PdfInvoice value = new PdfInvoice();
            value.SetDocumentType(PdfInvoiceDocumentType.CreditNote);
            value.SetCreditNoteReference("INV-1", "Return");
            value.AddLine(new PdfInvoiceLine("Item", 1, 5));
            return value.Build();
        }

        if (name == "factur-x")
        {
            PdfInvoice value = new PdfInvoice();
            PdfTrueTypeFont font = value.GetDocument().RegisterTrueTypeFont(TestSfntFont.Create(0x00010000U, 0, false));
            int[] codePoints = new int[95];
            int[] glyphIndices = new int[95];
            for (int index = 0; index < codePoints.Length; index++)
            {
                codePoints[index] = 32 + index;
                glyphIndices[index] = 1;
            }

            font.UnicodeCodePoints = codePoints;
            font.GlyphIndices = glyphIndices;
            value.SetEmbeddedFont(font);
            value.AddLine(new PdfInvoiceLine("Item", 1, 5));
            PdfAPreparationOptions options = new PdfAPreparationOptions();
            options.Conformance = PdfAConformance.PdfA3B;
            options.IccProfileData = CreateRgbIccProfile();
            value.SetFacturXData(new PdfFacturXMetadata(PdfFacturXProfile.En16931), FacturXXml(), options);
            return value.Build();
        }

        throw new ArgumentException("Unknown snapshot template.", "name");
    }

    static string SnapshotDocumentId(string name)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(name));
        return Convert.ToHexString(hash).Substring(0, 32);
    }

    static string Content(PdfDocument document)
    {
        StringBuilder content = new StringBuilder();
        foreach (PdfPage page in document.Pages)
        {
            content.Append(page.GetCanvas().ToPdfString());
        }

        return content.ToString();
    }

    static PdfParagraphStyle HeaderStyle()
    {
        return PdfParagraphStyle.Create(PdfTextStyle.Create(10, PdfFont.Bold), 12, 0, 0, 0);
    }

    static PdfParagraphStyle BodyStyle()
    {
        return PdfParagraphStyle.Create(PdfTextStyle.Create(10, PdfFont.Regular), 12, 0, 0, 0);
    }

    static byte[] FacturXXml()
    {
        string xml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<rsm:CrossIndustryInvoice xmlns:rsm=\"urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100\" xmlns:ram=\"urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100\">"
            + "<rsm:ExchangedDocumentContext><ram:GuidelineSpecifiedDocumentContextParameter><ram:ID>urn:cen.eu:en16931:2017</ram:ID></ram:GuidelineSpecifiedDocumentContextParameter></rsm:ExchangedDocumentContext>"
            + "</rsm:CrossIndustryInvoice>";
        return Encoding.UTF8.GetBytes(xml);
    }

    static byte[] CreateRgbIccProfile()
    {
        byte[] profile = new byte[128];
        profile[3] = 128;
        profile[16] = (byte)'R';
        profile[17] = (byte)'G';
        profile[18] = (byte)'B';
        profile[19] = (byte)' ';
        profile[36] = (byte)'a';
        profile[37] = (byte)'c';
        profile[38] = (byte)'s';
        profile[39] = (byte)'p';
        return profile;
    }
}
