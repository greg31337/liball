using System.Text;

namespace odf.Tests;

public class StylePropertyDocumentModelTests
{
    [Test]
    public void MaterializesCompleteQualifiedTextAndParagraphPropertySets()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:style style:name='Body' style:family='paragraph'>"
            + "<style:paragraph-properties "
            + "fo:line-height='125.00%' fo:text-align='justify' "
            + "fo:text-align-last='start' fo:margin-left='1.250cm' "
            + "fo:margin-right='0cm' fo:text-indent='-0.25in' "
            + "fo:break-before='page' fo:keep-together='always' "
            + "fo:widows='2' fo:orphans='3' "
            + "style:line-spacing='0.10cm' "
            + "style:font-independent-line-spacing='true' "
            + "style:writing-mode='lr-tb' style:join-border='false' "
            + "style:punctuation-wrap='hanging' "
            + "text:number-lines='true' text:line-number='7' "
            + "ext:paragraph-token='future-value'>"
            + "<style:tab-stops><style:tab-stop "
            + "style:position='1.25cm' style:type='char' style:char='.'/></style:tab-stops>"
            + "<style:drop-cap style:lines='3' style:length='word' "
            + "style:distance='0.10cm'/>"
            + "<style:background-image xlink:href='Pictures/paper.png' "
            + "xlink:type='simple' style:repeat='stretch'/>"
            + "</style:paragraph-properties>"
            + "<style:text-properties "
            + "fo:color='#123456' fo:background-color='transparent' "
            + "fo:font-family='Example Serif' fo:font-size='10.500pt' "
            + "fo:font-style='italic' fo:font-weight='600' "
            + "fo:language='en' fo:country='US' "
            + "fo:letter-spacing='0.025cm' fo:text-transform='uppercase' "
            + "style:font-name='Example' style:font-size-rel='115%' "
            + "style:text-position='super 58%' "
            + "style:text-underline-style='wave' "
            + "style:text-line-through-type='double-line' "
            + "style:text-emphasize='dot above' "
            + "style:letter-kerning='true' text:display='condition' "
            + "ext:text-token='preserve-me'/>"
            + "</style:style>"
            + "</office:styles>"));
        OdfStyle style = document.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(style.ParagraphPropertiesNode, Is.Not.Null);
            Assert.That(style.TextPropertiesNode, Is.Not.Null);
            Assert.That(style.ParagraphProperties, Has.Length.EqualTo(18));
            Assert.That(style.TextProperties, Has.Length.EqualTo(19));
            Assert.That(
                FindProperty(
                    style.ParagraphProperties,
                    OdfNamespaces.Fo,
                    "line-height").LexicalValue,
                Is.EqualTo("125.00%"));
            Assert.That(
                FindProperty(
                    style.ParagraphProperties,
                    OdfNamespaces.Text,
                    "line-number").LexicalValue,
                Is.EqualTo("7"));
            Assert.That(
                FindProperty(
                    style.ParagraphProperties,
                    "urn:example:style-extension",
                    "paragraph-token").LexicalValue,
                Is.EqualTo("future-value"));
            Assert.That(
                FindProperty(
                    style.TextProperties,
                    OdfNamespaces.Fo,
                    "font-size").LexicalValue,
                Is.EqualTo("10.500pt"));
            Assert.That(
                FindProperty(
                    style.TextProperties,
                    OdfNamespaces.Style,
                    "text-position").LexicalValue,
                Is.EqualTo("super 58%"));
            Assert.That(
                FindProperty(
                    style.TextProperties,
                    "urn:example:style-extension",
                    "text-token").Name.Prefix,
                Is.EqualTo("ext"));
            Assert.That(style.ParagraphPropertyChildren, Has.Length.EqualTo(3));
            Assert.That(
                style.ParagraphPropertyChildren.Select(
                    item => item.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "tab-stops",
                    "drop-cap",
                    "background-image"
                }));
            Assert.That(
                style.TextProperties.All(item => item.Attribute != null),
                Is.True);
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        Assert.That(
            FindProperty(
                reread.Styles.Single().TextProperties,
                OdfNamespaces.Fo,
                "font-size").LexicalValue,
            Is.EqualTo("10.500pt"));
    }

    [Test]
    public void WritesAddedChangedAndRemovedPropertiesAndChildContent()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:style style:name='Editable' style:family='paragraph'>"
            + "<style:paragraph-properties fo:margin-left='1.00cm' "
            + "fo:line-height='120%'>"
            + "<style:tab-stops><style:tab-stop style:position='2cm'/></style:tab-stops>"
            + "</style:paragraph-properties>"
            + "<style:text-properties fo:color='#010203' "
            + "style:text-position='super 58%' ext:token='keep'/>"
            + "</style:style>"
            + "</office:styles>"));
        OdfStyle style = document.Styles.Single();
        OdfStyleProperty color = FindProperty(
            style.TextProperties,
            OdfNamespaces.Fo,
            "color");
        OdfStyleProperty extension = FindProperty(
            style.TextProperties,
            "urn:example:style-extension",
            "token");
        color.LexicalValue = "#AABBCC";
        style.TextProperties =
        [
            color,
            extension,
            Property(OdfNamespaces.Fo, "font-size", "11.250pt", ""),
            Property(
                OdfNamespaces.Style,
                "text-underline-width",
                "bold",
                "")
        ];
        style.ParagraphProperties =
        [
            FindProperty(
                style.ParagraphProperties,
                OdfNamespaces.Fo,
                "line-height"),
            Property(OdfNamespaces.Fo, "text-align", "center", "")
        ];
        style.ParagraphPropertyChildren =
        [
            Element(
                OdfNamespaces.Style,
                "drop-cap",
                OdfNamespaces.StylePrefix)
        ];

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfStyle updated = reread.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(updated.TextProperties, Has.Length.EqualTo(4));
            Assert.That(
                FindProperty(
                    updated.TextProperties,
                    OdfNamespaces.Fo,
                    "color").LexicalValue,
                Is.EqualTo("#AABBCC"));
            Assert.That(
                FindProperty(
                    updated.TextProperties,
                    OdfNamespaces.Fo,
                    "font-size").LexicalValue,
                Is.EqualTo("11.250pt"));
            Assert.That(
                updated.TextProperties.Any(
                    item => item.Name.LocalName == "text-position"),
                Is.False);
            Assert.That(
                FindProperty(
                    updated.TextProperties,
                    "urn:example:style-extension",
                    "token").LexicalValue,
                Is.EqualTo("keep"));
            Assert.That(updated.ParagraphProperties, Has.Length.EqualTo(2));
            Assert.That(
                updated.ParagraphProperties.Any(
                    item => item.Name.LocalName == "margin-left"),
                Is.False);
            Assert.That(
                FindProperty(
                    updated.ParagraphProperties,
                    OdfNamespaces.Fo,
                    "text-align").LexicalValue,
                Is.EqualTo("center"));
            Assert.That(updated.ParagraphPropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                updated.ParagraphPropertyChildren[0].Name?.LocalName,
                Is.EqualTo("drop-cap"));
            Assert.That(color.Attribute, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesPropertySetsAndNamespaceBindingsInPackageStyles()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfStyle style = new OdfStyle
        {
            Name = "Generated",
            Family = OdfStyleFamily.Paragraph,
            IsCommon = true,
            TextProperties =
            [
                Property(OdfNamespaces.Fo, "font-size", "12.000pt", ""),
                Property(OdfNamespaces.Style, "font-name", "Generated Face", "")
            ],
            ParagraphProperties =
            [
                Property(OdfNamespaces.Fo, "text-align", "justify", ""),
                Property(OdfNamespaces.Text, "number-lines", "false", "")
            ]
        };
        document.Styles = [style];

        byte[] written = Write(document);
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "properties.odt",
            new OdfLoadOptions());
        OdfStyle generated = reread.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(generated.PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(generated.ParagraphPropertiesNode, Is.Not.Null);
            Assert.That(generated.TextPropertiesNode, Is.Not.Null);
            Assert.That(
                generated.Node?.Children.Select(item => item.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "paragraph-properties",
                    "text-properties"
                }));
            Assert.That(
                FindProperty(
                    generated.TextProperties,
                    OdfNamespaces.Fo,
                    "font-size").Name.Prefix,
                Is.EqualTo(OdfNamespaces.FoPrefix));
            Assert.That(
                FindProperty(
                    generated.ParagraphProperties,
                    OdfNamespaces.Text,
                    "number-lines").Name.Prefix,
                Is.EqualTo(OdfNamespaces.TextPrefix));
            Assert.That(style.TextProperties[0].Attribute, Is.Not.Null);
        });
    }

    [Test]
    public void PreservesUnderlyingNodeEditsWhenTypedPropertiesAreUnchanged()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:style style:name='Raw' style:family='text'>"
            + "<style:text-properties fo:color='#111111' "
            + "style:text-position='sub 58%'/>"
            + "</style:style>"
            + "</office:styles>"));
        OdfStyle style = document.Styles.Single();
        OdfNode node = style.TextPropertiesNode
            ?? throw new InvalidOperationException(
                "Text properties were not materialized.");
        OdfAttribute color = node.Attributes.Single(
            item => item.Name.LocalName == "color");
        color.Value = "#FEDCBA";
        OdfAttribute added = new OdfAttribute
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Fo,
                LocalName = "font-weight",
                Prefix = OdfNamespaces.FoPrefix
            },
            Value = "575"
        };
        node.Attributes = [color, added];

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfStyle raw = reread.Styles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(raw.TextProperties, Has.Length.EqualTo(2));
            Assert.That(
                FindProperty(
                    raw.TextProperties,
                    OdfNamespaces.Fo,
                    "color").LexicalValue,
                Is.EqualTo("#FEDCBA"));
            Assert.That(
                FindProperty(
                    raw.TextProperties,
                    OdfNamespaces.Fo,
                    "font-weight").LexicalValue,
                Is.EqualTo("575"));
            Assert.That(
                raw.TextProperties.Any(
                    item => item.Name.LocalName == "text-position"),
                Is.False);
        });
    }

    static OdfStyleProperty FindProperty(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        return properties.Single(
            item => item.Name.NamespaceName == namespaceName
                && item.Name.LocalName == localName);
    }

    static OdfStyleProperty Property(
        string namespaceName,
        string localName,
        string lexicalValue,
        string prefix)
    {
        return new OdfStyleProperty
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName,
                Prefix = prefix
            },
            LexicalValue = lexicalValue
        };
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
            "properties.fodt",
            new OdfLoadOptions());
    }

    static string FlatXml(string styles)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:style-extension'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
