using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using pdf;

namespace pdf.Tests;

[TestFixture]
public class PdfIndependentConformanceTests
{
    [TestCase(PdfAConformance.PdfA1B, 1, "%PDF-1.4")]
    [TestCase(PdfAConformance.PdfA2B, 2, "%PDF-1.7")]
    [TestCase(PdfAConformance.PdfA3B, 3, "%PDF-1.7")]
    public void IndependentPdfAValidatorAcceptsEveryPreparedProfile(PdfAConformance conformance, int part, string header)
    {
        PdfDocument document = CreatePdfADocument(conformance);

        byte[] bytes = document.ToBytes();
        string[] issues = ValidatePdfAIndependently(bytes, part, header);

        Assert.That(issues, Is.Empty);
        Assert.That(document.ValidatePdfAConformance().IsValid(), Is.True);
    }

    [Test]
    public void IndependentPdfAValidatorRejectsDamagedIdentificationIntentAndEncryptionFixtures()
    {
        string valid = Encoding.Latin1.GetString(CreatePdfADocument(PdfAConformance.PdfA2B).ToBytes());
        string damaged = valid
            .Replace("pdfaid:part=\"2\"", "pdfaid:part=\"9\"", StringComparison.Ordinal)
            .Replace("/OutputIntents [", "/OutputIntentX [", StringComparison.Ordinal)
            .Replace("trailer\n<<", "trailer\n<< /Encrypt 1 0 R", StringComparison.Ordinal);

        string[] issues = ValidatePdfAIndependently(Encoding.Latin1.GetBytes(damaged), 2, "%PDF-1.7");

        Assert.That(issues, Does.Contain("identification"));
        Assert.That(issues, Does.Contain("output-intent"));
        Assert.That(issues, Does.Contain("encryption"));
    }

    [Test]
    public void IndependentPdfUaValidatorAcceptsTaggedReadingOrderArtifactsFormsLinksAndAlternateText()
    {
        PdfDocument document = CreatePdfUaDocument();

        byte[] bytes = document.ToBytes();
        string[] issues = ValidatePdfUaIndependently(bytes);

        Assert.That(issues, Is.Empty);
        Assert.That(document.ValidatePdfUaPreparation().IsValid(), Is.True);
    }

    [Test]
    public void IndependentPdfUaValidatorRejectsDamagedLanguageTaggingAlternateTextAndFormFixtures()
    {
        string valid = Encoding.ASCII.GetString(CreatePdfUaDocument().ToBytes());
        string damaged = valid
            .Replace("/MarkInfo << /Marked true >>", "/MarkXnfo << /Marked true >>", StringComparison.Ordinal)
            .Replace("/Lang (en-US)", "/Lxng (en-US)", StringComparison.Ordinal)
            .Replace("/Alt (Status diagram)", "/Alx (Status diagram)", StringComparison.Ordinal)
            .Replace("/TU (Full name)", "/TX (Full name)", StringComparison.Ordinal);

        string[] issues = ValidatePdfUaIndependently(Encoding.ASCII.GetBytes(damaged));

        Assert.That(issues, Does.Contain("mark-info"));
        Assert.That(issues, Does.Contain("language"));
        Assert.That(issues, Does.Contain("alternate-text"));
        Assert.That(issues, Does.Contain("form-name"));
    }

    [Test]
    public void PdfUaValidatorRequiresExactlyOneDocumentStructureRoot()
    {
        PdfDocument wrongRole = CreatePdfUaDocumentWithRoot("Part", false);
        PdfValidationResult wrongRoleResult = wrongRole.ValidatePdfUaPreparation();
        Assert.That(wrongRoleResult.HasIssue("PDFUA_DOCUMENT_ROOT_INVALID"), Is.True);

        PdfDocument multipleRoots = CreatePdfUaDocumentWithRoot("Document", true);
        PdfValidationResult multipleRootsResult = multipleRoots.ValidatePdfUaPreparation();
        Assert.That(multipleRootsResult.HasIssue("PDFUA_DOCUMENT_ROOT_INVALID"), Is.True);
    }

    static PdfDocument CreatePdfADocument(PdfAConformance conformance)
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Archival fixture");
        metadata.SetProducer("pdf tests");
        document.SetMetadata(metadata);
        document.AddPage(100, 100).GetCanvas().FillRectangle(10, 10, 20, 20);
        if (conformance == PdfAConformance.PdfA3B)
        {
            document.AddAssociatedFile("source.xml", Encoding.UTF8.GetBytes("<source/>"), "text/xml", "Source", PdfAssociatedFileRelationship.Source);
        }

        PdfAPreparationOptions options = new PdfAPreparationOptions();
        options.Conformance = conformance;
        options.IccProfileData = CreateRgbIccProfile();
        options.OutputConditionIdentifier = "Test RGB";
        options.OutputCondition = "Test RGB";
        options.Info = "Test RGB";
        document.PrepareForPdfA(options);
        return document;
    }

    static PdfDocument CreatePdfUaDocument()
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Accessible fixture");
        document.SetMetadata(metadata);
        PdfPage page = document.AddPage(200, 200);
        document.SetLanguage("en-US");
        document.SetDisplayDocumentTitle(true);
        document.EnableTaggedPdf();

        PdfAccessibilityNode root = document.AddAccessibilityNode(page, "Document");
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", string.Empty);
        PdfAccessibilityNode figure = document.AddStructureChild(root, "Figure", string.Empty);
        PdfAccessibilityNode linkNode = document.AddStructureChild(root, "Link", string.Empty);
        PdfAccessibilityNode formNode = document.AddStructureChild(root, "Form", string.Empty);
        document.SetStructureAlternateText(figure, "Status diagram");

        page.GetCanvas().BeginStructureContent(paragraph);
        page.GetCanvas().DrawText("Accessible text", 10, 170, 10);
        page.GetCanvas().EndMarkedContent();
        page.GetCanvas().BeginStructureContent(figure);
        page.GetCanvas().FillRectangle(10, 120, 40, 20);
        page.GetCanvas().EndMarkedContent();
        page.GetCanvas().BeginArtifact(PdfArtifactType.Layout);
        page.GetCanvas().DrawLine(0, 100, 200, 100);
        page.GetCanvas().EndMarkedContent();
        page.GetCanvas().BeginPaginationArtifact(PdfPaginationArtifactType.Footer);
        page.GetCanvas().DrawText("Page 1", 80, 5, 8);
        page.GetCanvas().EndMarkedContent();

        PdfExternalLink link = page.AddExternalLink("https://example.test", 10, 80, 60, 12);
        PdfAcroFormField field = document.AddTextField("name", page, 10, 40, 80, 20);
        field.SetAccessibilityName("Full name");
        document.TagExternalLink(linkNode, link);
        document.TagFormField(formNode, field);
        return document;
    }

    static PdfDocument CreatePdfUaDocumentWithRoot(string rootRole, bool addSecondRoot)
    {
        PdfDocument document = new PdfDocument();
        PdfMetadata metadata = new PdfMetadata();
        metadata.SetTitle("Root fixture");
        document.SetMetadata(metadata);
        PdfPage page = document.AddPage(100, 100);
        document.SetLanguage("en");
        document.SetDisplayDocumentTitle(true);
        document.EnableTaggedPdf();
        PdfAccessibilityNode root = document.AddAccessibilityNode(page, rootRole);
        PdfAccessibilityNode paragraph = document.AddStructureChild(root, "P", string.Empty);
        page.GetCanvas().BeginStructureContent(paragraph);
        page.GetCanvas().DrawText("text", 10, 50, 10);
        page.GetCanvas().EndMarkedContent();
        if (addSecondRoot)
        {
            document.AddAccessibilityNode(page, "Part");
        }

        return document;
    }

    static string[] ValidatePdfAIndependently(byte[] bytes, int part, string header)
    {
        string pdf = Encoding.Latin1.GetString(bytes);
        List<string> issues = new List<string>();
        if (!pdf.StartsWith(header + "\n", StringComparison.Ordinal))
        {
            issues.Add("version");
        }

        if (!pdf.Contains("pdfaid:part=\"" + part.ToString() + "\" pdfaid:conformance=\"B\"", StringComparison.Ordinal))
        {
            issues.Add("identification");
        }

        if (!pdf.Contains("/Metadata ", StringComparison.Ordinal))
        {
            issues.Add("metadata");
        }

        if (!pdf.Contains("/OutputIntents [", StringComparison.Ordinal) || !pdf.Contains("/S /GTS_PDFA1", StringComparison.Ordinal) || !Regex.IsMatch(pdf, @"/N (1|3|4) /Length \d+"))
        {
            issues.Add("output-intent");
        }

        if (pdf.Contains("/Encrypt ", StringComparison.Ordinal))
        {
            issues.Add("encryption");
        }

        if (pdf.Contains("/BaseFont /Helvetica", StringComparison.Ordinal))
        {
            issues.Add("font-embedding");
        }

        if (part == 3 && pdf.Contains("/EmbeddedFiles", StringComparison.Ordinal) && (!pdf.Contains(" /AF [", StringComparison.Ordinal) || !pdf.Contains("/AFRelationship /", StringComparison.Ordinal)))
        {
            issues.Add("associated-file");
        }

        return issues.ToArray();
    }

    static string[] ValidatePdfUaIndependently(byte[] bytes)
    {
        string pdf = Encoding.ASCII.GetString(bytes);
        List<string> issues = new List<string>();
        if (!pdf.Contains("/MarkInfo << /Marked true >>", StringComparison.Ordinal))
        {
            issues.Add("mark-info");
        }

        if (!pdf.Contains("/StructTreeRoot ", StringComparison.Ordinal) || !pdf.Contains("/ParentTree ", StringComparison.Ordinal) || !pdf.Contains("/S /Document", StringComparison.Ordinal))
        {
            issues.Add("structure-tree");
        }

        if (!pdf.Contains("/Lang (en-US)", StringComparison.Ordinal))
        {
            issues.Add("language");
        }

        if (!pdf.Contains("/Alt (Status diagram)", StringComparison.Ordinal))
        {
            issues.Add("alternate-text");
        }

        if (!pdf.Contains("/Artifact <</Type /Layout>> BDC", StringComparison.Ordinal) || !pdf.Contains("/Artifact <</Type /Pagination /Subtype /Footer>> BDC", StringComparison.Ordinal))
        {
            issues.Add("artifacts");
        }

        if (!pdf.Contains("/TU (Full name)", StringComparison.Ordinal))
        {
            issues.Add("form-name");
        }

        int objectReferences = Regex.Matches(pdf, "/Type /OBJR").Count;
        int structureParents = Regex.Matches(pdf, "/StructParent [0-9]+").Count;
        if (objectReferences != 2 || structureParents != 2)
        {
            issues.Add("interactive-tagging");
        }

        return issues.ToArray();
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
