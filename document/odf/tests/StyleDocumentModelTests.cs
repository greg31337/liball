using System.Text;

namespace odf.Tests;

public class StyleDocumentModelTests
{
    static object[][] Families =
    [
        ["paragraph", OdfStyleFamily.Paragraph],
        ["text", OdfStyleFamily.Text],
        ["section", OdfStyleFamily.Section],
        ["table", OdfStyleFamily.Table],
        ["table-column", OdfStyleFamily.TableColumn],
        ["table-row", OdfStyleFamily.TableRow],
        ["table-cell", OdfStyleFamily.TableCell],
        ["graphic", OdfStyleFamily.Graphic],
        ["chart", OdfStyleFamily.Chart],
        ["drawing-page", OdfStyleFamily.DrawingPage],
        ["presentation", OdfStyleFamily.Presentation],
        ["ruby", OdfStyleFamily.Ruby],
        ["list", OdfStyleFamily.List]
    ];

    [Test]
    public void MaterializesNamedDefaultCommonAndAutomaticStyles()
    {
        OdfDocument document = ReadFlat(AllFamiliesXml());

        Assert.Multiple(() =>
        {
            Assert.That(document.Styles, Has.Length.EqualTo(16));
            Assert.That(
                document.Styles.Single(item => item.IsDefault).Family,
                Is.EqualTo(OdfStyleFamily.Paragraph));
            Assert.That(
                document.Styles.Single(item => item.Name == "Automatic").IsAutomatic,
                Is.True);
            Assert.That(
                document.Styles.Single(item => item.Name == "Automatic").IsCommon,
                Is.False);
            Assert.That(
                document.Styles.Single(item => item.Name == "Unknown").Family,
                Is.EqualTo(OdfStyleFamily.Unknown));
            Assert.That(
                document.Styles.Single(item => item.Name == "Unknown").FamilyName,
                Is.EqualTo("future-family"));
            Assert.That(
                document.Styles.Where(item => item.IsCommon && item.IsNamed)
                    .Select(item => item.Family)
                    .Where(item => item != OdfStyleFamily.Unknown),
                Is.SupersetOf(Families.Select(item => (OdfStyleFamily)item[1])));
            Assert.That(
                document.Styles.All(item => item.Node != null),
                Is.True);
            Assert.That(
                document.Styles.All(item => item.PartPath == ""),
                Is.True);
        });
    }

    [TestCaseSource(nameof(Families))]
    public void WritesEveryTypedStyleFamily(
        string familyName,
        OdfStyleFamily family)
    {
        OdfDocument document = ReadFlat(
            FlatXml("<office:styles/>", ""));
        document.Styles =
        [
            new OdfStyle
            {
                Name = "Generated",
                Family = family,
                IsCommon = true
            }
        ];

        byte[] written = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "family.fodt",
            new OdfLoadOptions());
        OdfStyle style = reread.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(style.Family, Is.EqualTo(family));
            Assert.That(style.FamilyName, Is.EqualTo(familyName));
            Assert.That(style.Name, Is.EqualTo("Generated"));
            Assert.That(style.IsNamed, Is.True);
            Assert.That(style.IsCommon, Is.True);
        });
    }

    [Test]
    public void UnchangedStyleWritingPreservesUnknownAttributesAndChildren()
    {
        string xml = FlatXml(
            "<office:styles>"
            + "<style:style style:name='Body' style:display-name='Body Text' "
            + "style:family='paragraph' ext:token='keep'>"
            + "<style:text-properties fo:font-weight='bold' ext:property='retained'/>"
            + "</style:style>"
            + "</office:styles>",
            "");
        OdfDocument document = ReadFlat(xml);

        byte[] written = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        string output = Encoding.UTF8.GetString(written);
        OdfDocument reread = ReadFlat(output);
        OdfStyle style = reread.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(style.Name, Is.EqualTo("Body"));
            Assert.That(style.DisplayName, Is.EqualTo("Body Text"));
            Assert.That(style.Family, Is.EqualTo(OdfStyleFamily.Paragraph));
            Assert.That(output, Does.Contain("ext:token=\"keep\""));
            Assert.That(output, Does.Contain("ext:property=\"retained\""));
            Assert.That(output, Does.Contain("fo:font-weight=\"bold\""));
            Assert.That(style.Node?.Children, Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void TypedStyleChangesAddMoveAndRemoveDefinitionsOnWrite()
    {
        string xml = FlatXml(
            "<office:styles>"
            + "<style:style style:name='Old' style:family='paragraph'/>"
            + "<style:style style:name='Keep' style:family='text'/>"
            + "</office:styles>",
            "<office:automatic-styles>"
            + "<style:style style:name='RemoveMe' style:family='graphic'/>"
            + "</office:automatic-styles>");
        OdfDocument document = ReadFlat(xml);
        OdfStyle changed = document.Styles.Single(item => item.Name == "Old");
        OdfStyle keep = document.Styles.Single(item => item.Name == "Keep");
        changed.Name = "Moved";
        changed.DisplayName = "Moved Cell";
        changed.Family = OdfStyleFamily.TableCell;
        changed.IsAutomatic = true;
        OdfStyle defaultStyle = new OdfStyle
        {
            Family = OdfStyleFamily.Paragraph,
            IsDefault = true,
            IsCommon = true
        };
        OdfStyle automatic = new OdfStyle
        {
            Name = "Generated",
            DisplayName = "Generated Graphic",
            Family = OdfStyleFamily.Graphic,
            IsAutomatic = true
        };
        document.Styles = [keep, changed, defaultStyle, automatic];

        byte[] written = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "styles.fodt",
            new OdfLoadOptions());
        OdfStyle moved = reread.Styles.Single(item => item.Name == "Moved");
        OdfStyle generated = reread.Styles.Single(item => item.Name == "Generated");

        Assert.Multiple(() =>
        {
            Assert.That(reread.Styles, Has.Length.EqualTo(4));
            Assert.That(reread.Styles.Any(item => item.Name == "RemoveMe"), Is.False);
            Assert.That(moved.DisplayName, Is.EqualTo("Moved Cell"));
            Assert.That(moved.Family, Is.EqualTo(OdfStyleFamily.TableCell));
            Assert.That(moved.FamilyName, Is.EqualTo("table-cell"));
            Assert.That(moved.IsAutomatic, Is.True);
            Assert.That(generated.DisplayName, Is.EqualTo("Generated Graphic"));
            Assert.That(generated.IsAutomatic, Is.True);
            Assert.That(reread.Styles.Single(item => item.IsDefault).Family, Is.EqualTo(OdfStyleFamily.Paragraph));
            Assert.That(defaultStyle.Node, Is.Not.Null);
            Assert.That(automatic.Node, Is.Not.Null);
        });
    }

    [Test]
    public void PackageWritingCreatesRequiredStylePartAndKeepsAutomaticStylesInContent()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        document.Styles =
        [
            new OdfStyle
            {
                Name = "Common",
                Family = OdfStyleFamily.Paragraph,
                IsCommon = true
            },
            new OdfStyle
            {
                Name = "Automatic",
                Family = OdfStyleFamily.Graphic,
                IsAutomatic = true
            }
        ];

        byte[] written = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "styles.odt",
            new OdfLoadOptions());
        OdfStyle common = reread.Styles.Single(item => item.Name == "Common");
        OdfStyle automatic = reread.Styles.Single(item => item.Name == "Automatic");

        Assert.Multiple(() =>
        {
            Assert.That(
                reread.Parts.Select(item => item.Path),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(common.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(common.IsCommon, Is.True);
            Assert.That(automatic.PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(automatic.IsAutomatic, Is.True);
            Assert.That(
                reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    private static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "styles.fodt",
            new OdfLoadOptions());
    }

    private static string AllFamiliesXml()
    {
        StringBuilder styles = new StringBuilder();
        styles.Append("<office:styles>");
        styles.Append("<style:default-style style:family='paragraph'/>");
        foreach (object[] family in Families)
        {
            string name = (string)family[0];
            styles.Append("<style:style style:name='");
            styles.Append(name);
            styles.Append("' style:display-name='");
            styles.Append(name);
            styles.Append(" style' style:family='");
            styles.Append(name);
            styles.Append("'/>");
        }
        styles.Append("<style:style style:name='Unknown' style:family='future-family'/>");
        styles.Append("</office:styles>");
        return FlatXml(
            styles.ToString(),
            "<office:automatic-styles>"
            + "<style:style style:name='Automatic' style:family='graphic'/>"
            + "</office:automatic-styles>");
    }

    private static string FlatXml(string styles, string automaticStyles)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:ext='urn:example:style-extension'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + automaticStyles
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
