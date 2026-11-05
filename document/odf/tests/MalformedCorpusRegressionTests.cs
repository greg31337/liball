using System.Text;

namespace odf.Tests;

public class MalformedCorpusRegressionTests
{
    [Test]
    [CancelAfter(3000)]
    public void PackageCorpusIsRejectedWithinConfiguredExpansionBounds()
    {
        IOdfPackageReader reader = new OdfPackageReader();
        foreach (string[] item in ReadTsv("package.tsv", 2))
        {
            byte[] payload = Convert.FromBase64String(item[1]);
            OdfLoadOptions options = BoundedLoadOptions();

            OdfPackage package = reader.ReadPackageBytes(payload, options);

            Assert.That(package.IsValid, Is.False, item[0]);
            Assert.That(package.Diagnostics, Is.Not.Empty, item[0]);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void XmlCorpusFailsClosedWithinConfiguredTreeBounds()
    {
        IOdfXmlReader reader = new OdfXmlReader();
        foreach (string[] item in ReadTsv("xml.tsv", 2))
        {
            byte[] payload = Encoding.UTF8.GetBytes(item[1]);
            OdfLoadOptions options = BoundedLoadOptions();

            Assert.That(
                () => reader.ReadXmlBytes(payload, "", options),
                Throws.Exception,
                item[0]);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void ManifestCorpusProducesBoundedInvalidModels()
    {
        IOdfManifestService service = new OdfManifestService();
        foreach (string[] item in ReadTsv("manifest.tsv", 2))
        {
            byte[] payload = Encoding.UTF8.GetBytes(item[1]);

            OdfManifest manifest = service.ReadManifestBytes(payload);

            Assert.That(manifest.IsValid, Is.False, item[0]);
            Assert.That(manifest.Diagnostics, Is.Not.Empty, item[0]);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void FormulaCorpusReturnsDiagnosticsAtBoundedSyntaxDepth()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        foreach (string source in ReadLines("formula.txt"))
        {
            OdfFormula formula = parser.ParseFormula(source, 8);

            Assert.That(formula.IsValid, Is.False, source);
            Assert.That(formula.Diagnostics, Is.Not.Empty, source);
            Assert.That(formula.Tokens.Length, Is.LessThan(128), source);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void RangeCorpusRemainsInvalidAndLexicallyPreserved()
    {
        foreach (string lexical in ReadLines("range.txt"))
        {
            OdfDocument document = ReadFlat(
                SpreadsheetXml(lexical),
                "malformed-range.fods");
            OdfTable table = document.Tables[0];
            OdfDiagnostic[] diagnostics = new OdfValidator().ValidateDocument(
                document,
                new OdfValidationOptions
                {
                    Mode = OdfValidationMode.SemanticOnly,
                    Version = OdfVersion.Version14
                });

            Assert.That(table.PrintRangeText, Is.EqualTo(lexical), lexical);
            Assert.That(
                diagnostics.Any(diagnostic =>
                    diagnostic.Code == OdfDiagnosticCodes.InvalidRangeReference),
                Is.True,
                lexical);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void GeometryCorpusMaterializesInvalidValuesWithoutThrowing()
    {
        foreach (string[] item in ReadTsv("geometry.tsv", 5))
        {
            OdfDocument document = ReadFlat(
                DrawingXml(item[1], item[2], item[3], item[4]),
                "malformed-geometry.fodg");
            OdfDrawingObject drawing = document.DrawingObjects[1];

            Assert.That(drawing.Geometry.Path.IsValid, Is.False, item[0]);
            Assert.That(drawing.Geometry.PointList.IsValid, Is.False, item[0]);
            Assert.That(drawing.Geometry.ViewBoxValue.IsValid, Is.False, item[0]);
            Assert.That(drawing.Transform.IsValid, Is.False, item[0]);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void RdfCorpusPreservesButDoesNotResolveMalformedStatements()
    {
        foreach (string[] item in ReadTsv("rdf.tsv", 4))
        {
            OdfDocument document = ReadFlat(
                RdfaXml(item[1], item[2], item[3]),
                "malformed-rdf.fodt");

            Assert.That(document.RdfTriples, Is.Not.Empty, item[0]);
            Assert.That(
                document.RdfTriples.Any(triple => !triple.IsValid),
                Is.True,
                item[0]);
        }
    }

    [Test]
    [CancelAfter(3000)]
    public void ChangeCorpusRefusesAmbiguousOrBrokenBoundaries()
    {
        IOdfChangeTracker tracker = new OdfChangeTracker();
        foreach (string[] item in ReadTsv("change.tsv", 2))
        {
            OdfDocument document = ReadFlat(
                ChangeXml(item[1]),
                "malformed-change.fodt");
            OdfChange change = tracker.GetTextChanges(document)[0];

            Assert.That(
                change.IsBoundaryResolved
                    && !change.IsReversed
                    && !change.HasDuplicateBoundary,
                Is.False,
                item[0]);
            Assert.That(
                tracker.RejectTextChange(document, change),
                Is.False,
                item[0]);
        }
    }

    static OdfLoadOptions BoundedLoadOptions()
    {
        OdfLoadOptions options = new OdfLoadOptions();
        options.SecurityLimits.MaximumPackageEntries = 16;
        options.SecurityLimits.MaximumTotalExpandedBytes = 65536;
        options.SecurityLimits.MaximumSingleEntryBytes = 32768;
        options.SecurityLimits.MaximumCompressionRatio = 16;
        options.SecurityLimits.MaximumPathLength = 256;
        options.SecurityLimits.MaximumXmlDepth = 16;
        options.SecurityLimits.MaximumXmlAttributesPerElement = 32;
        options.SecurityLimits.MaximumXmlTextLength = 4096;
        options.SecurityLimits.MaximumFormulaDepth = 8;
        options.SecurityLimits.MaximumEmbeddedPackageDepth = 2;
        return options;
    }

    static OdfDocument ReadFlat(string xml, string name)
    {
        byte[] payload = Encoding.UTF8.GetBytes(xml);
        Assert.That(payload.Length, Is.LessThan(16384), name);
        return new OdfDocumentReader().ReadDocumentBytes(
            payload,
            name,
            BoundedLoadOptions());
    }

    static string SpreadsheetXml(string lexical)
    {
        return FlatStart(
            OdfMediaTypes.Spreadsheet,
            " xmlns:table='" + OdfNamespaces.Table + "'")
            + "<office:body><office:spreadsheet>"
            + "<table:table table:name='Sheet1' table:print-ranges='"
            + lexical
            + "'><table:table-row><table:table-cell/></table:table-row>"
            + "</table:table></office:spreadsheet></office:body>"
            + "</office:document>";
    }

    static string DrawingXml(
        string path,
        string points,
        string viewBox,
        string transform)
    {
        return FlatStart(
            OdfMediaTypes.Graphics,
            " xmlns:draw='" + OdfNamespaces.Drawing
                + "' xmlns:svg='" + OdfNamespaces.Svg + "'")
            + "<office:body><office:drawing><draw:page draw:name='Page'>"
            + "<draw:path draw:name='Malformed' svg:d='" + path
            + "' draw:points='" + points + "' svg:viewBox='" + viewBox
            + "' draw:transform='" + transform + "'/>"
            + "</draw:page></office:drawing></office:body>"
            + "</office:document>";
    }

    static string RdfaXml(string about, string property, string datatype)
    {
        return FlatStart(
            OdfMediaTypes.Text,
            " xmlns:text='" + OdfNamespaces.Text
                + "' xmlns:xhtml='" + OdfNamespaces.Xhtml + "'")
            + "<office:body><office:text><text:p xhtml:about='"
            + about + "' xhtml:property='" + property
            + "' xhtml:datatype='" + datatype
            + "'>malformed RDF</text:p></office:text></office:body>"
            + "</office:document>";
    }

    static string ChangeXml(string boundaries)
    {
        return FlatStart(
            OdfMediaTypes.Text,
            " xmlns:text='" + OdfNamespaces.Text
                + "' xmlns:dc='" + OdfNamespaces.DublinCore + "'")
            + "<office:body><office:text><text:tracked-changes>"
            + "<text:changed-region xml:id='broken'><text:insertion>"
            + "<office:change-info><dc:creator>Fuzzer</dc:creator>"
            + "</office:change-info></text:insertion></text:changed-region>"
            + "</text:tracked-changes>" + boundaries
            + "</office:text></office:body></office:document>";
    }

    static string FlatStart(string mediaType, string namespaces)
    {
        return "<office:document xmlns:office='" + OdfNamespaces.Office
            + "'" + namespaces + " office:version='1.4' office:mimetype='"
            + mediaType + "'>";
    }

    static string[] ReadLines(string name)
    {
        string[] lines = File.ReadAllLines(CorpusPath(name));
        return lines.Where(line => line.Length > 0 && !line.StartsWith("#"))
            .ToArray();
    }

    static string[][] ReadTsv(string name, int fieldCount)
    {
        List<string[]> result = new List<string[]>();
        foreach (string line in ReadLines(name))
        {
            string[] fields = line.Split('\t');
            Assert.That(fields, Has.Length.EqualTo(fieldCount), name);
            result.Add(fields);
        }
        Assert.That(result, Is.Not.Empty, name);
        return result.ToArray();
    }

    static string CorpusPath(string name)
    {
        string path = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "MalformedCorpus",
            name));
        Assert.That(new FileInfo(path).Length, Is.LessThan(16384), name);
        return path;
    }
}
