using System.Text;

namespace odf.Tests;

public class PageLayoutDocumentModelTests
{
    [Test]
    public void MaterializesPageLayoutsPropertiesAndMasterVariants()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:default-page-layout>"
            + "<style:page-layout-properties fo:page-width='8.500in' "
            + "fo:page-height='11in' style:print-orientation='portrait'/>"
            + "</style:default-page-layout>"
            + "</office:styles>"
            + "<office:automatic-styles>"
            + "<style:page-layout style:name='Standard' style:page-usage='mirrored'>"
            + "<style:page-layout-properties fo:page-width='21.000cm' "
            + "fo:page-height='29.700cm' fo:margin-left='2.00cm' "
            + "style:num-format='i' style:num-prefix='[' style:num-suffix=']' "
            + "style:num-letter-sync='true' style:first-page-number='continue' "
            + "style:print-orientation='landscape' ext:page-token='keep'>"
            + "<style:background-image xlink:href='Pictures/page.png' "
            + "xlink:type='simple'/>"
            + "<style:columns fo:column-count='2' fo:column-gap='0.50cm'>"
            + "<style:column style:rel-width='1*'/>"
            + "<style:column style:rel-width='1*'/></style:columns>"
            + "<style:footnote-sep style:width='0.020cm' "
            + "style:rel-width='25%' style:line-style='solid' "
            + "style:adjustment='left'/></style:page-layout-properties>"
            + "<style:header-style><style:header-footer-properties "
            + "svg:height='1.250cm' fo:margin-bottom='0.25cm' "
            + "style:dynamic-spacing='true'>"
            + "<style:background-image xlink:href='Pictures/header.png' "
            + "xlink:type='simple'/></style:header-footer-properties>"
            + "</style:header-style>"
            + "<style:footer-style><style:header-footer-properties "
            + "fo:min-height='1.000cm' fo:border-top='0.01cm solid #000000'/>"
            + "</style:footer-style>"
            + "</style:page-layout>"
            + "</office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='First' style:display-name='First Master' "
            + "style:page-layout-name='Standard' draw:style-name='MasterDrawing' "
            + "style:next-style-name='Second'>"
            + "<style:header style:display='true'><text:p>Header</text:p></style:header>"
            + "<style:header-left><text:p>Left Header</text:p></style:header-left>"
            + "<style:header-first><text:p>First Header</text:p></style:header-first>"
            + "<style:footer><text:p>Footer</text:p></style:footer>"
            + "<style:footer-left><text:p>Left Footer</text:p></style:footer-left>"
            + "<style:footer-first><text:p>First Footer</text:p></style:footer-first>"
            + "<draw:frame draw:name='Master Shape'/></style:master-page>"
            + "<style:master-page style:name='Second' "
            + "style:page-layout-name='Standard'/>"
            + "</office:master-styles>"));
        OdfPageLayout standard = document.PageLayouts.Single(
            item => item.Name == "Standard");
        OdfPageLayout defaultLayout = document.PageLayouts.Single(
            item => item.IsDefault);
        OdfMasterPage first = document.MasterPages.Single(
            item => item.Name == "First");

        Assert.Multiple(() =>
        {
            Assert.That(document.PageLayouts, Has.Length.EqualTo(2));
            Assert.That(defaultLayout.Name, Is.Empty);
            Assert.That(defaultLayout.PrintOrientation, Is.EqualTo("portrait"));
            Assert.That(standard.PageUsage, Is.EqualTo("mirrored"));
            Assert.That(standard.NumberFormat, Is.EqualTo("i"));
            Assert.That(standard.NumberPrefix, Is.EqualTo("["));
            Assert.That(standard.NumberSuffix, Is.EqualTo("]"));
            Assert.That(standard.NumberLetterSync, Is.EqualTo("true"));
            Assert.That(standard.FirstPageNumber, Is.EqualTo("continue"));
            Assert.That(standard.PrintOrientation, Is.EqualTo("landscape"));
            Assert.That(
                Property(
                    standard.PageProperties,
                    OdfNamespaces.Fo,
                    "page-width").LexicalValue,
                Is.EqualTo("21.000cm"));
            Assert.That(
                Property(
                    standard.PageProperties,
                    "urn:example:page-extension",
                    "page-token").LexicalValue,
                Is.EqualTo("keep"));
            Assert.That(standard.BackgroundImageNode, Is.Not.Null);
            Assert.That(standard.ColumnsNode?.Children, Has.Length.EqualTo(2));
            Assert.That(standard.FootnoteSeparatorNode, Is.Not.Null);
            Assert.That(
                Property(
                    standard.HeaderProperties,
                    OdfNamespaces.Svg,
                    "height").LexicalValue,
                Is.EqualTo("1.250cm"));
            Assert.That(standard.HeaderPropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                Property(
                    standard.FooterProperties,
                    OdfNamespaces.Fo,
                    "min-height").LexicalValue,
                Is.EqualTo("1.000cm"));
            Assert.That(document.MasterPages, Has.Length.EqualTo(2));
            Assert.That(first.DisplayName, Is.EqualTo("First Master"));
            Assert.That(first.DrawingStyleName, Is.EqualTo("MasterDrawing"));
            Assert.That(first.Header, Is.Not.Null);
            Assert.That(first.HeaderLeft, Is.Not.Null);
            Assert.That(first.HeaderFirst, Is.Not.Null);
            Assert.That(first.Footer, Is.Not.Null);
            Assert.That(first.FooterLeft, Is.Not.Null);
            Assert.That(first.FooterFirst, Is.Not.Null);
            Assert.That(first.Content, Has.Length.EqualTo(1));
            Assert.That(first.Content[0].Name?.LocalName, Is.EqualTo("frame"));
            Assert.That(first.PageLayout, Is.SameAs(standard));
            Assert.That(first.NextMasterPage?.Name, Is.EqualTo("Second"));
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        Assert.That(
            reread.MasterPages.Single(item => item.Name == "First").HeaderFirst,
            Is.Not.Null);
    }

    [Test]
    public void WritesLayoutAndMasterAddChangeAndRemovalOperations()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:automatic-styles>"
            + "<style:page-layout style:name='Keep' style:page-usage='all'>"
            + "<style:page-layout-properties fo:page-width='20cm' "
            + "style:num-format='1' style:first-page-number='1'>"
            + "<style:columns fo:column-count='2'/>"
            + "<style:footnote-sep style:width='0.01cm'/></style:page-layout-properties>"
            + "<style:footer-style><style:header-footer-properties "
            + "fo:min-height='1cm'/></style:footer-style>"
            + "</style:page-layout>"
            + "<style:page-layout style:name='Remove'/>"
            + "</office:automatic-styles>"
            + "<office:master-styles>"
            + "<style:master-page style:name='KeepMaster' "
            + "style:page-layout-name='Keep'>"
            + "<style:header-left><text:p>Old Left</text:p></style:header-left>"
            + "</style:master-page>"
            + "<style:master-page style:name='RemoveMaster' "
            + "style:page-layout-name='Remove'/>"
            + "</office:master-styles>"));
        OdfPageLayout layout = document.PageLayouts.Single(
            item => item.Name == "Keep");
        layout.Name = "Changed";
        layout.PageUsage = "right";
        layout.NumberFormat = "A";
        layout.NumberPrefix = "Page ";
        layout.NumberSuffix = " of";
        layout.NumberLetterSync = "false";
        layout.FirstPageNumber = "continue";
        layout.PrintOrientation = "portrait";
        Property(
            layout.PageProperties,
            OdfNamespaces.Fo,
            "page-width").LexicalValue = "21.00cm";
        layout.ColumnsNode = StyleElement("columns");
        layout.FootnoteSeparatorNode = null;
        layout.HeaderProperties =
        [
            StyleProperty(OdfNamespaces.Svg, "height", "1.500cm")
        ];
        layout.FooterProperties = [];
        document.PageLayouts = [layout];
        OdfMasterPage master = document.MasterPages.Single(
            item => item.Name == "KeepMaster");
        master.Name = "ChangedMaster";
        master.DisplayName = "Changed Master";
        master.PageLayoutName = "Changed";
        master.HeaderLeft = null;
        master.HeaderFirst = StyleElement("header-first");
        master.Footer = StyleElement("footer");
        document.MasterPages = [master];

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfPageLayout changed = document.PageLayouts.Single();
        OdfMasterPage changedMaster = reread.MasterPages.Single();

        Assert.Multiple(() =>
        {
            Assert.That(reread.PageLayouts, Has.Length.EqualTo(1));
            Assert.That(reread.PageLayouts[0].Name, Is.EqualTo("Changed"));
            Assert.That(reread.PageLayouts[0].PageUsage, Is.EqualTo("right"));
            Assert.That(reread.PageLayouts[0].NumberFormat, Is.EqualTo("A"));
            Assert.That(reread.PageLayouts[0].NumberPrefix, Is.EqualTo("Page "));
            Assert.That(reread.PageLayouts[0].NumberSuffix, Is.EqualTo(" of"));
            Assert.That(reread.PageLayouts[0].FirstPageNumber, Is.EqualTo("continue"));
            Assert.That(reread.PageLayouts[0].PrintOrientation, Is.EqualTo("portrait"));
            Assert.That(reread.PageLayouts[0].ColumnsNode, Is.Not.Null);
            Assert.That(reread.PageLayouts[0].FootnoteSeparatorNode, Is.Null);
            Assert.That(reread.PageLayouts[0].HeaderProperties, Has.Length.EqualTo(1));
            Assert.That(reread.PageLayouts[0].FooterPropertiesNode, Is.Null);
            Assert.That(changedMaster.Name, Is.EqualTo("ChangedMaster"));
            Assert.That(changedMaster.PageLayoutName, Is.EqualTo("Changed"));
            Assert.That(changedMaster.HeaderLeft, Is.Null);
            Assert.That(changedMaster.HeaderFirst, Is.Not.Null);
            Assert.That(changedMaster.Footer, Is.Not.Null);
            Assert.That(changedMaster.PageLayout?.Name, Is.EqualTo("Changed"));
            Assert.That(changed.Node, Is.Not.Null);
            Assert.That(master.Node, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesPackageLayoutDefaultStylesAndMasterPages()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfPageLayout defaultLayout = new OdfPageLayout
        {
            IsDefault = true,
            PageProperties =
            [
                StyleProperty(OdfNamespaces.Fo, "page-width", "8.27in")
            ]
        };
        OdfPageLayout layout = new OdfPageLayout
        {
            Name = "Generated",
            PageUsage = "all",
            NumberFormat = "I",
            NumberPrefix = "-",
            NumberSuffix = "-",
            FirstPageNumber = "7",
            PrintOrientation = "landscape",
            PageProperties =
            [
                StyleProperty(OdfNamespaces.Fo, "page-width", "29.70cm"),
                StyleProperty(OdfNamespaces.Fo, "page-height", "21.00cm")
            ],
            ColumnsNode = StyleElement("columns"),
            FootnoteSeparatorNode = StyleElement("footnote-sep"),
            HeaderProperties =
            [
                StyleProperty(OdfNamespaces.Fo, "min-height", "1.20cm")
            ],
            FooterProperties =
            [
                StyleProperty(OdfNamespaces.Style, "dynamic-spacing", "true")
            ]
        };
        OdfMasterPage second = new OdfMasterPage
        {
            Name = "Second",
            PageLayoutName = "Generated"
        };
        OdfMasterPage master = new OdfMasterPage
        {
            Name = "GeneratedMaster",
            DisplayName = "Generated Master",
            PageLayoutName = "Generated",
            NextStyleName = "Second",
            Header = StyleElement("header"),
            HeaderLeft = StyleElement("header-left"),
            HeaderFirst = StyleElement("header-first"),
            Footer = StyleElement("footer"),
            FooterLeft = StyleElement("footer-left"),
            FooterFirst = StyleElement("footer-first"),
            Content = [DrawingElement("frame")]
        };
        document.PageLayouts = [defaultLayout, layout];
        document.MasterPages = [master, second];

        byte[] written = Write(document);
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "page-layouts.odt",
            new OdfLoadOptions());
        OdfPageLayout generated = reread.PageLayouts.Single(
            item => item.Name == "Generated");
        OdfMasterPage generatedMaster = reread.MasterPages.Single(
            item => item.Name == "GeneratedMaster");

        Assert.Multiple(() =>
        {
            Assert.That(reread.PageLayouts.Any(item => item.IsDefault), Is.True);
            Assert.That(generated.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(generated.NumberFormat, Is.EqualTo("I"));
            Assert.That(generated.FirstPageNumber, Is.EqualTo("7"));
            Assert.That(generated.ColumnsNode, Is.Not.Null);
            Assert.That(generated.FootnoteSeparatorNode, Is.Not.Null);
            Assert.That(generated.HeaderPropertiesNode, Is.Not.Null);
            Assert.That(generated.FooterPropertiesNode, Is.Not.Null);
            Assert.That(generatedMaster.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(generatedMaster.Header, Is.Not.Null);
            Assert.That(generatedMaster.HeaderLeft, Is.Not.Null);
            Assert.That(generatedMaster.HeaderFirst, Is.Not.Null);
            Assert.That(generatedMaster.FooterFirst, Is.Not.Null);
            Assert.That(generatedMaster.Content, Has.Length.EqualTo(1));
            Assert.That(generatedMaster.PageLayout, Is.SameAs(generated));
            Assert.That(generatedMaster.NextMasterPage?.Name, Is.EqualTo("Second"));
            Assert.That(
                reread.Parts.Select(item => item.Path),
                Does.Contain(OdfPartNames.Styles));
            Assert.That(
                reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    static OdfStyleProperty Property(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        return properties.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName);
    }

    static OdfStyleProperty StyleProperty(
        string namespaceName,
        string localName,
        string lexicalValue)
    {
        return new OdfStyleProperty
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName
            },
            LexicalValue = lexicalValue
        };
    }

    static OdfNode StyleElement(string localName)
    {
        return Element(
            OdfNamespaces.Style,
            localName,
            OdfNamespaces.StylePrefix);
    }

    static OdfNode DrawingElement(string localName)
    {
        return Element(
            OdfNamespaces.Drawing,
            localName,
            OdfNamespaces.DrawingPrefix);
    }

    static OdfNode Element(
        string namespaceName,
        string localName,
        string prefix)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            }
        };
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "page-layouts.fodt",
            new OdfLoadOptions());
    }

    static string FlatXml(string sections)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:page-extension'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + sections
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
