using System.Text;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfValidationTests
{
    [Test]
    public void ValidatePdfStructure_AcceptsWriterOutputAndReportsOrderedResults()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        page.GetCanvas().DrawText("Validated", 10, 50, 12);
        document.AddTextField("name", page, 10, 10, 80, 20);
        document.AddSignaturePlaceholderWithLength("signature", page, 100, 10, 80, 20, 8);

        PdfValidationResult result = document.ValidatePdfStructure();

        Assert.That(result.IsValid(), Is.True, Describe(result));
        Assert.That(result.HasErrors(), Is.False);
        Assert.That(result.CountErrors(), Is.EqualTo(0));
        Assert.That(result.CountWarnings(), Is.EqualTo(0));
        Assert.That(result.GetIssues(), Is.Empty);
    }

    [Test]
    public void StructureValidator_ReportsSyntaxCrossReferenceReachabilityAndPageTreeDamage()
    {
        PdfStructureValidator validator = new PdfStructureValidator();
        PdfValidationResult syntax = validator.ValidateBytes(Encoding.ASCII.GetBytes("%PDF-1.4\n%%EOF\n"));
        Assert.That(syntax.HasIssue("PDF_XREF_START_MISSING"), Is.True);

        PdfDocument document = new PdfDocument();
        document.AddPage(200, 100);
        string valid = Encoding.ASCII.GetString(document.ToBytes());

        string badPageTree = valid.Replace("/Pages 2 0 R", "/Pages 1 0 R", System.StringComparison.Ordinal);
        PdfValidationResult reachability = validator.ValidateBytes(Encoding.ASCII.GetBytes(badPageTree));
        Assert.That(reachability.HasIssue("PDF_PAGE_TREE_TYPE_INVALID"), Is.True);
        Assert.That(reachability.HasIssue("PDF_RESOURCE_UNREACHABLE"), Is.True);

        string badCount = valid.Replace("/Count 1", "/Count 2", System.StringComparison.Ordinal);
        PdfValidationResult pageTree = validator.ValidateBytes(Encoding.ASCII.GetBytes(badCount));
        Assert.That(pageTree.HasIssue("PDF_PAGE_TREE_COUNT_MISMATCH"), Is.True);

        string badStartXref = ReplaceStartXref(valid, "0");
        PdfValidationResult crossReference = validator.ValidateBytes(Encoding.ASCII.GetBytes(badStartXref));
        Assert.That(crossReference.HasIssue("PDF_XREF_OFFSET_INVALID"), Is.True);
    }

    [Test]
    public void StructureValidator_ReportsMalformedFormsAndSignatures()
    {
        PdfDocument formDocument = new PdfDocument();
        PdfPage formPage = formDocument.AddPage(200, 100);
        formDocument.AddTextField("name", formPage, 10, 10, 80, 20);
        string formPdf = Encoding.ASCII.GetString(formDocument.ToBytes());
        string badForm = formPdf.Replace("/FT /Tx", "/XX /Tx", System.StringComparison.Ordinal);

        PdfValidationResult formResult = new PdfStructureValidator().ValidateBytes(Encoding.ASCII.GetBytes(badForm));

        Assert.That(formResult.HasIssue("PDF_FORM_FIELD_TYPE_MISSING"), Is.True);

        PdfDocument signatureDocument = new PdfDocument();
        PdfPage signaturePage = signatureDocument.AddPage(200, 100);
        signatureDocument.AddSignaturePlaceholderWithLength("signature", signaturePage, 10, 10, 80, 20, 8);
        string signaturePdf = Encoding.ASCII.GetString(signatureDocument.ToBytes());
        string badSignature = signaturePdf.Replace("/ByteRange [", "/ByteRangX [", System.StringComparison.Ordinal);

        PdfValidationResult signatureResult = new PdfStructureValidator().ValidateBytes(Encoding.ASCII.GetBytes(badSignature));

        Assert.That(signatureResult.HasIssue("PDF_SIGNATURE_BYTE_RANGE_INVALID"), Is.True);
    }

    [Test]
    public void ValidatePdfAConformance_ChecksPreparationAndEmittedIdentification()
    {
        PdfDocument unprepared = new PdfDocument();
        unprepared.AddPage(100, 100);

        PdfValidationResult unpreparedResult = unprepared.ValidatePdfAConformance();

        Assert.That(unpreparedResult.HasIssue("PDFA_NOT_PREPARED"), Is.True);

        PdfDocument prepared = new PdfDocument();
        prepared.AddPage(100, 100);
        PdfAPreparationOptions options = new PdfAPreparationOptions();
        options.Conformance = PdfAConformance.PdfA2B;
        options.IccProfileData = CreateRgbIccProfile();
        prepared.PrepareForPdfA(options);

        PdfValidationResult preparedResult = prepared.ValidatePdfAConformance();

        Assert.That(preparedResult.IsValid(), Is.True, Describe(preparedResult));
        Assert.That(preparedResult.CountErrors(), Is.EqualTo(0));

        prepared.PdfAOptions.IccProfileData = new byte[0];
        PdfValidationResult damaged = prepared.ValidatePdfAConformance();
        Assert.That(damaged.HasIssue("PDFA_OUTPUT_INTENT_INVALID"), Is.True);
    }

    [Test]
    public void ValidatePdfUaPreparation_ChecksTaggedOutputAndAccessibleObjects()
    {
        PdfDocument document = new PdfDocument();
        PdfPage page = document.AddPage(200, 100);
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Accessible output");
        document.SetMetadata(metadata);
        document.SetLanguage("en-US");
        document.SetDisplayDocumentTitle(true);
        document.EnableTaggedPdf();
        PdfAccessibilityNode root = document.AddAccessibilityNode(page, "Document");
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", string.Empty);
        page.GetCanvas().BeginStructureContent(paragraph);
        page.GetCanvas().DrawText("Accessible", 10, 50, 12);
        page.GetCanvas().EndMarkedContent();

        PdfValidationResult valid = document.ValidatePdfUaPreparation();

        Assert.That(valid.IsValid(), Is.True, Describe(valid));
        Assert.That(valid.CountErrors(), Is.EqualTo(0));
        Assert.That(valid.CountWarnings(), Is.EqualTo(0));

        PdfDocument incomplete = new PdfDocument();
        PdfPage incompletePage = incomplete.AddPage(200, 100);
        incomplete.SetLanguage("en");
        incomplete.EnableTaggedPdf();
        PdfAccessibilityNode incompleteRoot = incomplete.AddAccessibilityNode(incompletePage, "Document");
        incomplete.AddStructureChild(incompleteRoot, "Figure", string.Empty);
        incompletePage.AddExternalLink("https://example.test", 10, 10, 40, 12);

        PdfValidationResult invalid = incomplete.ValidatePdfUaPreparation();

        Assert.That(invalid.HasIssue("PDFUA_TITLE_MISSING"), Is.True);
        Assert.That(invalid.HasIssue("PDFUA_FIGURE_ALT_TEXT_MISSING"), Is.True);
        Assert.That(invalid.HasIssue("PDFUA_LINK_NOT_TAGGED"), Is.True);
        Assert.That(invalid.HasIssue("PDFUA_DISPLAY_TITLE_RECOMMENDED"), Is.True);
    }

    [Test]
    public void ValidationApis_RejectNullAndUnsavableDocumentsWithoutThrowingForFindings()
    {
        PdfStructureValidator structure = new PdfStructureValidator();
        PdfConformanceValidator conformance = new PdfConformanceValidator();
        Assert.Throws<ArgumentNullException>(() => structure.ValidateBytes(null!));
        Assert.Throws<ArgumentNullException>(() => structure.ValidateDocument(null!));
        Assert.Throws<ArgumentNullException>(() => conformance.ValidatePdfA(null!));
        Assert.Throws<ArgumentNullException>(() => conformance.ValidatePdfUa(null!));

        PdfDocument empty = new PdfDocument();
        PdfValidationResult result = empty.ValidatePdfStructure();
        Assert.That(result.HasIssue("PDF_DOCUMENT_NO_PAGES"), Is.True);
        Assert.That(result.GetIssues()[0].ObjectNumber, Is.EqualTo(0));
        Assert.That(result.GetIssues()[0].Severity, Is.EqualTo(PdfValidationIssueSeverity.Error));
    }

    static string ReplaceStartXref(string pdf, string replacement)
    {
        int marker = pdf.LastIndexOf("startxref\n", System.StringComparison.Ordinal);
        int numberStart = marker + 10;
        int numberEnd = pdf.IndexOf('\n', numberStart);
        return pdf.Substring(0, numberStart) + replacement + pdf.Substring(numberEnd);
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

    static string Describe(PdfValidationResult result)
    {
        StringBuilder description = new StringBuilder();
        foreach (PdfValidationIssue issue in result.GetIssues())
        {
            description.Append(issue.Code);
            description.Append(' ');
        }

        return description.ToString();
    }
}
