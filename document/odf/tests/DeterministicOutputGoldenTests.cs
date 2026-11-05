using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace odf.Tests;

public class DeterministicOutputGoldenTests
{
    [Test]
    public void PackageOrderingAndTimestampsMatchGoldenOutput()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        editor.AddDocumentResource(
            document,
            "z.bin",
            [26],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        editor.AddDocumentResource(
            document,
            "a.bin",
            [1],
            OdfMediaTypes.OctetStream,
            OdfCompressionMethod.Deflated);
        OdfSaveOptions options = DeterministicOptions();

        byte[] first = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            options);
        byte[] repeated = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            options);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(PackageSnapshot(first),
                Is.EqualTo(Golden("package")));
        });
    }

    [Test]
    public void NamespaceSelectionMatchesGoldenOutput()
    {
        OdfNode paragraph = Element(
            OdfNamespaces.Text,
            "p",
            "arbitrary-text",
            [Text("Golden")]);
        OdfNode text = Element(
            OdfNamespaces.Office,
            "text",
            "arbitrary-office",
            [paragraph]);
        OdfNode body = Element(
            OdfNamespaces.Office,
            "body",
            "arbitrary-office",
            [text]);
        OdfNode root = Element(
            OdfNamespaces.Office,
            "document",
            "arbitrary-office",
            [body]);
        root.Attributes =
        [
            Attribute(OdfNamespaces.Office, "version", "ignored", "1.4")
        ];

        string output = OdfTreeWriter.WriteText(
            [root],
            DeterministicOptions());

        Assert.That(output, Is.EqualTo(Golden("namespaces")));
    }

    [Test]
    public void XmlSerializationMatchesIndentedGoldenOutput()
    {
        OdfNode paragraph = Element(
            OdfNamespaces.Text,
            "p",
            "text-alias",
            []);
        paragraph.WasEmptyElement = true;
        OdfNode text = Element(
            OdfNamespaces.Office,
            "text",
            "office-alias",
            [paragraph]);
        OdfNode body = Element(
            OdfNamespaces.Office,
            "body",
            "office-alias",
            [text]);
        OdfNode root = Element(
            OdfNamespaces.Office,
            "document",
            "office-alias",
            [body]);
        root.Attributes =
        [
            Attribute(
                OdfNamespaces.Office,
                "version",
                "office-alias",
                "1.4"),
            Attribute(
                OdfNamespaces.Office,
                "mimetype",
                "office-alias",
                OdfMediaTypes.Text)
        ];
        OdfSaveOptions options = DeterministicOptions();
        options.IndentXml = true;

        string first = OdfTreeWriter.WriteText([root], options);
        string repeated = OdfTreeWriter.WriteText([root], options);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(first, Is.EqualTo(Golden("xml")));
        });
    }

    [Test]
    public void AutomaticNamesMatchGoldenCollisionSequence()
    {
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument named = ReadFlat(
            "<office:body><office:text>"
                + "<draw:g draw:name='Hero' xml:id='group'/>"
                + "<draw:g draw:name='Hero Copy 2' xml:id='group-2'/>"
                + "</office:text></office:body>");
        OdfDocument packaged = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        editor.AddDocumentResource(
            packaged,
            "Pictures/image.png",
            [1],
            "image/png",
            OdfCompressionMethod.Deflated);
        editor.AddDocumentResource(
            packaged,
            "Pictures/image-2.png",
            [2],
            "image/png",
            OdfCompressionMethod.Deflated);

        string snapshot = editor.CreateUniqueDocumentName(named, "Hero")
            + "|" + editor.CreateUniqueDocumentIdentifier(named, "group")
            + "|" + editor.CreateUniqueDocumentResourcePath(
                packaged,
                "Pictures/image.png");

        Assert.That(snapshot, Is.EqualTo(Golden("automatic-names")));
    }

    [Test]
    public void FormulaFormattingAndRebasingMatchGoldenOutput()
    {
        IOdfFormulaParser parser = new OdfFormulaParser();
        OdfFormula formula = parser.ParseFormula(
            "of:=  sum ( 1 ; 2 * ( 3 + 4 ) )  ",
            32);
        string canonical = parser.FormatFormulaCanonical(formula);
        string copied = parser.RebaseFormulaCopy(
            "of:=[.A1]+[.$B2]+[.C$3]",
            "Sheet1",
            2,
            1);
        string renamed = parser.RebaseFormulaSheetRename(
            "of:=['Old Sheet'.A1]",
            "Calc",
            "Old Sheet",
            "New Sheet");

        string snapshot = canonical + "|" + copied + "|" + renamed;

        Assert.That(snapshot, Is.EqualTo(Golden("formulas")));
    }

    [Test]
    public void AutomaticStyleSerializationMatchesGoldenOutput()
    {
        OdfDocument document = ReadFlat(
            "<office:automatic-styles>"
                + AutomaticStyleXml("Auto", "left")
                + AutomaticStyleXml("Auto2", "center")
                + "</office:automatic-styles>"
                + "<office:body><office:text/></office:body>");
        IOdfStyleEditor editor = new OdfStyleEditor();
        OdfStyle source = ParagraphStyle("Source", "right");
        OdfStyle created = editor.CreateAutomaticStyle(
            document,
            source,
            "Auto");
        OdfSaveOptions options = DeterministicOptions();

        byte[] first = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            options);
        byte[] repeated = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            options);
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            first,
            "styles.fodt",
            new OdfLoadOptions());
        OdfStyle written = reread.Styles.Single(item => item.Name == created.Name);
        string snapshot = written.Name + "|" + written.Family + "|"
            + written.ParagraphProperties.Single().LexicalValue;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(repeated));
            Assert.That(snapshot, Is.EqualTo(Golden("styles")));
        });
    }

    [Test]
    public void FlatAndPackageConversionsMatchGoldenOutput()
    {
        OdfDocument source = ReadFlat(
            "<office:styles/><office:meta/><office:settings/>"
                + "<office:body><office:text><text:p>Converted</text:p>"
                + "</office:text></office:body>");
        source.SaveOptions = DeterministicOptions();
        IOdfDocumentEditor editor = new OdfDocumentEditor();
        OdfDocument package = editor.ConvertDocumentToPackage(source);
        OdfDocument flat = editor.ConvertDocumentToFlat(package);
        byte[] packageBytes = new OdfDocumentWriter().WriteDocumentBytes(
            package,
            DeterministicOptions());
        byte[] repeatedPackage = new OdfDocumentWriter().WriteDocumentBytes(
            package,
            DeterministicOptions());
        byte[] flatBytes = new OdfDocumentWriter().WriteDocumentBytes(
            flat,
            DeterministicOptions());
        byte[] repeatedFlat = new OdfDocumentWriter().WriteDocumentBytes(
            flat,
            DeterministicOptions());
        OdfDocument writtenPackage = new OdfDocumentReader().ReadDocumentBytes(
            packageBytes,
            "converted.odt",
            new OdfLoadOptions());
        OdfDocument writtenFlat = new OdfDocumentReader().ReadDocumentBytes(
            flatBytes,
            "converted.fodt",
            new OdfLoadOptions());
        string snapshot = "Package:"
            + string.Join(",", writtenPackage.PackageEntries.Select(
                item => item.Path))
            + "|Flat:" + writtenFlat.TextBlocks.Single().Text;

        Assert.Multiple(() =>
        {
            Assert.That(packageBytes, Is.EqualTo(repeatedPackage));
            Assert.That(flatBytes, Is.EqualTo(repeatedFlat));
            Assert.That(snapshot, Is.EqualTo(Golden("conversions")));
        });
    }

    static OdfSaveOptions DeterministicOptions()
    {
        return new OdfSaveOptions
        {
            Deterministic = true,
            PreserveTimestamps = false,
            SortManifestEntries = true
        };
    }

    static string PackageSnapshot(byte[] bytes)
    {
        List<string> entries = new List<string>();
        using (MemoryStream input = new MemoryStream(bytes))
        using (ZipArchive archive = new ZipArchive(
            input,
            ZipArchiveMode.Read,
            false))
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string timestamp = entry.LastWriteTime.DateTime.ToString(
                    "yyyy-MM-ddTHH:mm:ss",
                    CultureInfo.InvariantCulture);
                entries.Add(entry.FullName + "@" + timestamp);
            }
        }
        return string.Join("|", entries);
    }

    static OdfDocument ReadFlat(string content)
    {
        string xml = "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:text='" + OdfNamespaces.Text
            + "' xmlns:draw='" + OdfNamespaces.Drawing
            + "' xmlns:style='" + OdfNamespaces.Style
            + "' xmlns:fo='" + OdfNamespaces.Fo
            + "' xmlns:xml='" + OdfNamespaces.Xml
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'>" + content + "</office:document>";
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "golden.fodt",
            new OdfLoadOptions());
    }

    static OdfStyle ParagraphStyle(string name, string alignment)
    {
        OdfStyle style = new OdfStyle();
        style.Name = name;
        style.Family = OdfStyleFamily.Paragraph;
        style.ParagraphProperties =
        [
            new OdfStyleProperty
            {
                Name = new OdfQualifiedName
                {
                    NamespaceName = OdfNamespaces.Fo,
                    LocalName = "text-align",
                    Prefix = OdfNamespaces.FoPrefix
                },
                LexicalValue = alignment
            }
        ];
        return style;
    }

    static string AutomaticStyleXml(string name, string alignment)
    {
        return "<style:style style:name='" + name
            + "' style:family='paragraph'><style:paragraph-properties "
            + "fo:text-align='" + alignment + "'/></style:style>";
    }

    static OdfNode Element(
        string namespaceName,
        string localName,
        string prefix,
        OdfNode[] children)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Element;
        result.Name = Name(namespaceName, localName, prefix);
        result.Children = children;
        return result;
    }

    static OdfNode Text(string value)
    {
        OdfNode result = new OdfNode();
        result.Kind = OdfNodeKind.Text;
        result.Value = value;
        return result;
    }

    static OdfAttribute Attribute(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        OdfAttribute result = new OdfAttribute();
        result.Name = Name(namespaceName, localName, prefix);
        result.Value = value;
        return result;
    }

    static OdfQualifiedName Name(
        string namespaceName,
        string localName,
        string prefix)
    {
        OdfQualifiedName result = new OdfQualifiedName();
        result.NamespaceName = namespaceName;
        result.LocalName = localName;
        result.Prefix = prefix;
        return result;
    }

    static string Golden(string key)
    {
        foreach (string line in File.ReadAllLines(GoldenPath()))
        {
            if (line.StartsWith(key + "\t", StringComparison.Ordinal))
            {
                return line.Substring(key.Length + 1).Replace("\\n", "\n");
            }
        }
        Assert.Fail("Golden output key was not found: " + key);
        return "";
    }

    static string GoldenPath()
    {
        return Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "..",
            "..",
            "..",
            "Fixtures",
            "Golden",
            "deterministic-output.golden"));
    }
}
