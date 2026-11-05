using System.Text;

namespace odf.Tests;

public class SemanticDocumentValidatorTests
{
    [Test]
    public void AcceptsResolvedSemanticReferencesAndConsistentContent()
    {
        string content =
            "<office:automatic-styles>"
            + "<style:style style:name='Body' style:family='paragraph'/>"
            + "<style:page-layout style:name='PageLayout'/>"
            + "<presentation:presentation-page-layout style:name='SlideLayout'/>"
            + "</office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='Master' style:page-layout-name='PageLayout'/>"
            + "</office:master-styles>"
            + "<office:body><office:text>"
            + "<text:tracked-changes><text:changed-region text:id='change-1'/></text:tracked-changes>"
            + "<text:p text:style-name='Body'>"
            + "<text:change-start text:change-id='change-1'/>"
            + "<text:change-end text:change-id='change-1'/>"
            + "</text:p>"
            + "<draw:page draw:master-page-name='Master' presentation:presentation-page-layout-name='SlideLayout'>"
            + "<draw:image xlink:href='Pictures/image.png'/>"
            + "</draw:page>"
            + "<table:table table:name='Sheet1'><table:table-row>"
            + "<table:table-cell office:value-type='float' office:value='1' table:formula='of:=[.A1]' table:number-columns-spanned='2'/>"
            + "<table:covered-table-cell/>"
            + "</table:table-row></table:table>"
            + "<chart:chart chart:class='chart:bar' chart:values-cell-range-address='Sheet1.A1:Sheet1.A2'/>"
            + "</office:text></office:body>";
        OdfDocument document = Document(content, ["Pictures/image.png"]);

        SemanticValidationResult result = Validate(
            document,
            OdfValidationMode.Complete);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.StoppedEarly, Is.False);
            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.ValidatedNodeCount, Is.GreaterThan(10));
        });
    }

    [Test]
    public void CompleteModeCollectsEverySemanticFailureCategory()
    {
        string content =
            "<office:body><office:text>"
            + "<text:p text:style-name='MissingStyle'/>"
            + "<draw:page draw:master-page-name='MissingMaster'>"
            + "<draw:object xlink:href='MissingObject/content.xml'/>"
            + "</draw:page>"
            + "<text:change-start text:change-id='missing-change'/>"
            + "<table:table table:name='Sheet1'><table:table-row>"
            + "<table:table-cell office:value-type='float' office:boolean-value='true' table:formula='of:=(1+2' table:number-columns-spanned='2'/>"
            + "</table:table-row></table:table>"
            + "<chart:chart chart:class='chart:unknown' chart:values-cell-range-address='MissingSheet.A1:MissingSheet.A2'/>"
            + "</office:text></office:body>";
        OdfDocument document = Document(content, []);

        SemanticValidationResult result = Validate(
            document,
            OdfValidationMode.Complete);
        string[] codes = result.Diagnostics
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.False);
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidStyleReference));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.BrokenReference));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidResourceReference));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidRangeReference));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidMerge));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.ValueTypeMismatch));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidFormula));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidDatatype));
            Assert.That(result.Diagnostics.All(
                diagnostic => diagnostic.Location.PartPath == OdfPartNames.Content),
                Is.True);
        });
    }

    [Test]
    public void StrictModeStopsAtFirstSemanticFailure()
    {
        OdfDocument document = Document(
            "<office:body><office:text>"
                + "<text:p text:style-name='MissingStyle'/>"
                + "<draw:image xlink:href='missing.png'/>"
                + "</office:text></office:body>",
            []);

        SemanticValidationResult result = Validate(
            document,
            OdfValidationMode.Strict);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.StoppedEarly, Is.True);
            Assert.That(result.Diagnostics, Has.Length.EqualTo(1));
            Assert.That(
                result.Diagnostics[0].Code,
                Is.EqualTo(OdfDiagnosticCodes.InvalidStyleReference));
        });
    }

    [Test]
    public void ReportsFormulaDepthAndTypedFormulaState()
    {
        OdfDocument document = Document(
            "<office:body><office:text><table:table-cell table:formula='of:=(((1)))'/></office:text></office:body>",
            []);
        document.Formulas =
        [
            new OdfFormula
            {
                Tokens = [new OdfFormulaToken()],
                Result = new OdfFormulaResult
                {
                    Error = OdfFormulaError.CircularReference
                }
            }
        ];
        OdfValidationOptions options = Options(OdfValidationMode.Complete);
        options.SecurityLimits.MaximumFormulaDepth = 2;

        SemanticValidationResult result =
            new SemanticDocumentValidator(options).ValidateDocument(document);
        string[] codes = result.Diagnostics
            .Select(diagnostic => diagnostic.Code)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.FormulaDepthLimitExceeded));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.InvalidFormula));
            Assert.That(codes, Does.Contain(OdfDiagnosticCodes.CircularReference));
        });
    }

    [Test]
    public void HonorsSemanticAndFormulaValidationSwitches()
    {
        OdfDocument document = Document(
            "<office:body><office:text><table:table-cell table:formula='bad'/></office:text></office:body>",
            []);
        OdfValidationOptions noFormulas = Options(OdfValidationMode.Complete);
        noFormulas.ValidateFormulas = false;
        OdfValidationOptions noSemantics = Options(OdfValidationMode.Complete);
        noSemantics.ValidateSemantics = false;

        SemanticValidationResult formulaResult =
            new SemanticDocumentValidator(noFormulas).ValidateDocument(document);
        SemanticValidationResult semanticResult =
            new SemanticDocumentValidator(noSemantics).ValidateDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(formulaResult.IsValid, Is.True);
            Assert.That(formulaResult.Diagnostics, Is.Empty);
            Assert.That(semanticResult.IsValid, Is.True);
            Assert.That(semanticResult.ValidatedNodeCount, Is.Zero);
        });
    }

    static SemanticValidationResult Validate(
        OdfDocument document,
        OdfValidationMode mode)
    {
        return new SemanticDocumentValidator(Options(mode))
            .ValidateDocument(document);
    }

    static OdfValidationOptions Options(OdfValidationMode mode)
    {
        OdfValidationOptions result = new OdfValidationOptions();
        result.Mode = mode;
        result.Version = OdfVersion.Version14;
        return result;
    }

    static OdfDocument Document(string content, string[] packagePaths)
    {
        string xml = "<office:document-content xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:style='"
            + OdfNamespaces.Style
            + "' xmlns:text='"
            + OdfNamespaces.Text
            + "' xmlns:table='"
            + OdfNamespaces.Table
            + "' xmlns:draw='"
            + OdfNamespaces.Drawing
            + "' xmlns:presentation='"
            + OdfNamespaces.Presentation
            + "' xmlns:chart='"
            + OdfNamespaces.Chart
            + "' xmlns:xlink='"
            + OdfNamespaces.XLink
            + "' xmlns:of='"
            + OdfNamespaces.OpenFormula
            + "' office:version='1.4'>"
            + content
            + "</office:document-content>";
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(
            Encoding.UTF8.GetBytes(xml),
            OdfPartNames.Content,
            new OdfLoadOptions());
        OdfPart part = new OdfPart();
        part.Path = OdfPartNames.Content;
        part.Nodes = nodes;
        part.Root = nodes.Single(node => node.Kind == OdfNodeKind.Element);
        OdfPackageEntry[] entries = packagePaths
            .Select(path => new OdfPackageEntry
            {
                Path = path,
                CanonicalPath = path
            })
            .ToArray();
        OdfDocument result = new OdfDocument();
        result.PackageKind = OdfPackageKind.Package;
        result.Parts = [part];
        result.PackageEntries = entries;
        return result;
    }
}
