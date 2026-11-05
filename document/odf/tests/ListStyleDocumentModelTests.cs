using System.Text;

namespace odf.Tests;

public class ListStyleDocumentModelTests
{
    [Test]
    public void MaterializesListOutlineAndEveryLevelKind()
    {
        OdfDocument document = ReadFlat(ListFlatXml(
            "<office:styles>"
            + "<text:list-style style:name='Lists' style:display-name='Lists' "
            + "text:consecutive-numbering='true' ext:token='keep'>"
            + "<text:list-level-style-number text:level='1' text:style-name='Label' "
            + "style:num-format='1' style:num-prefix='(' style:num-suffix=')' "
            + "style:num-letter-sync='true' style:num-list-format-name='Decimal' "
            + "text:display-levels='2' text:start-value='3'>"
            + "<style:list-level-properties text:space-before='0.50cm' "
            + "text:list-level-position-and-space-mode='label-alignment'>"
            + "<style:list-level-label-alignment text:label-followed-by='listtab' "
            + "text:list-tab-stop-position='1.25cm' fo:text-indent='-0.50cm' "
            + "fo:margin-left='1.25cm'/></style:list-level-properties>"
            + "<style:text-properties fo:color='#112233' fo:font-weight='bold'/>"
            + "</text:list-level-style-number>"
            + "<text:list-level-style-bullet text:level='2' text:bullet-char='•' "
            + "text:bullet-relative-size='90%' style:num-prefix='[' "
            + "style:num-suffix=']'><style:list-level-properties "
            + "text:min-label-width='0.40cm'/></text:list-level-style-bullet>"
            + "<text:list-level-style-image text:level='3' "
            + "xlink:href='Pictures/bullet.png' xlink:type='simple' "
            + "xlink:show='embed' xlink:actuate='onLoad'>"
            + "<style:list-level-properties fo:width='0.40cm' fo:height='0.40cm'/>"
            + "</text:list-level-style-image></text:list-style>"
            + "<text:outline-style style:name='Outline'>"
            + "<text:outline-level-style text:level='1' text:style-name='Heading' "
            + "style:num-format='I' style:num-prefix='' style:num-suffix='.' "
            + "text:display-levels='1' text:start-value='1'>"
            + "<style:list-level-properties text:space-before='0cm'/>"
            + "<style:text-properties fo:font-weight='bold'/>"
            + "</text:outline-level-style></text:outline-style>"
            + "<style:style style:name='Paragraph' style:family='paragraph' "
            + "style:list-style-name='Lists'/></office:styles>"));
        OdfListStyle list = document.ListStyles.Single(
            item => item.Name == "Lists");
        OdfListStyle outline = document.ListStyles.Single(
            item => item.Name == "Outline");
        OdfListLevel number = list.Levels[0];
        OdfListLevel bullet = list.Levels[1];
        OdfListLevel image = list.Levels[2];

        Assert.Multiple(() =>
        {
            Assert.That(document.ListStyles, Has.Length.EqualTo(2));
            Assert.That(list.Kind, Is.EqualTo(OdfListStyleKind.List));
            Assert.That(list.ConsecutiveNumbering, Is.EqualTo("true"));
            Assert.That(list.Levels, Has.Length.EqualTo(3));
            Assert.That(number.Kind, Is.EqualTo(OdfListLevelKind.Number));
            Assert.That(number.Level, Is.EqualTo("1"));
            Assert.That(number.StyleName, Is.EqualTo("Label"));
            Assert.That(number.NumberFormat, Is.EqualTo("1"));
            Assert.That(number.NumberPrefix, Is.EqualTo("("));
            Assert.That(number.NumberSuffix, Is.EqualTo(")"));
            Assert.That(number.NumberLetterSync, Is.EqualTo("true"));
            Assert.That(number.NumberListFormatName, Is.EqualTo("Decimal"));
            Assert.That(number.DisplayLevels, Is.EqualTo("2"));
            Assert.That(number.StartValue, Is.EqualTo("3"));
            Assert.That(number.LevelProperties, Has.Length.EqualTo(2));
            Assert.That(number.LabelAlignmentNode, Is.Not.Null);
            Assert.That(number.TextProperties, Has.Length.EqualTo(2));
            Assert.That(bullet.Kind, Is.EqualTo(OdfListLevelKind.Bullet));
            Assert.That(bullet.BulletCharacter, Is.EqualTo("•"));
            Assert.That(bullet.BulletRelativeSize, Is.EqualTo("90%"));
            Assert.That(image.Kind, Is.EqualTo(OdfListLevelKind.Image));
            Assert.That(image.ImageHref, Is.EqualTo("Pictures/bullet.png"));
            Assert.That(outline.Kind, Is.EqualTo(OdfListStyleKind.Outline));
            Assert.That(outline.Levels[0].Kind,
                Is.EqualTo(OdfListLevelKind.Outline));
            Assert.That(PropertyNamed(
                list.Attributes,
                "urn:example:list",
                "token").LexicalValue, Is.EqualTo("keep"));
            Assert.That(document.Styles.Single().ListStyle, Is.SameAs(list));
            Assert.That(document.Styles.Single().ListStyleNode,
                Is.SameAs(list.Node));
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));
        Assert.That(reread.ListStyles.Single(item => item.Name == "Lists")
            .Levels[0].LabelAlignmentNode, Is.Not.Null);
    }

    [Test]
    public void WritesListStyleLevelChangesMovementAndDeletion()
    {
        OdfDocument document = ReadFlat(ListFlatXml(
            "<office:automatic-styles>"
            + "<text:list-style style:name='Keep'>"
            + "<text:list-level-style-number text:level='1' "
            + "style:num-format='1'><style:list-level-properties "
            + "text:space-before='0.25cm'/></text:list-level-style-number>"
            + "</text:list-style><text:list-style style:name='Remove'>"
            + "<text:list-level-style-bullet text:level='1' "
            + "text:bullet-char='-'/></text:list-style>"
            + "<style:style style:name='P' style:family='paragraph' "
            + "style:list-style-name='Keep'/></office:automatic-styles>"));
        OdfListStyle style = document.ListStyles.Single(
            item => item.Name == "Keep");
        OdfListLevel level = style.Levels[0];
        style.Name = "Changed";
        style.DisplayName = "Changed List";
        style.ConsecutiveNumbering = "false";
        style.IsAutomatic = false;
        level.Kind = OdfListLevelKind.Bullet;
        level.Level = "2";
        level.StyleName = "BulletText";
        level.NumberFormat = "";
        level.BulletCharacter = "→";
        level.BulletRelativeSize = "110%";
        level.LabelAlignmentNode = StyleElement(
            "list-level-label-alignment");
        level.TextProperties =
        [
            StyleProperty(OdfNamespaces.Fo, "color", "#334455")
        ];
        OdfListLevel image = new OdfListLevel
        {
            Kind = OdfListLevelKind.Image,
            Level = "3",
            ImageDataNode = OfficeTextNode("binary-data", "aW1hZ2U=")
        };
        style.Levels = [level, image];
        document.ListStyles = [style];
        document.Styles[0].ListStyleName = "Changed";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(WriteDocument(document)));
        OdfListStyle changed = reread.ListStyles.Single();

        Assert.Multiple(() =>
        {
            Assert.That(changed.Name, Is.EqualTo("Changed"));
            Assert.That(changed.DisplayName, Is.EqualTo("Changed List"));
            Assert.That(changed.ConsecutiveNumbering, Is.EqualTo("false"));
            Assert.That(changed.IsAutomatic, Is.False);
            Assert.That(changed.Levels, Has.Length.EqualTo(2));
            Assert.That(changed.Levels[0].Kind,
                Is.EqualTo(OdfListLevelKind.Bullet));
            Assert.That(changed.Levels[0].Level, Is.EqualTo("2"));
            Assert.That(changed.Levels[0].BulletCharacter, Is.EqualTo("→"));
            Assert.That(changed.Levels[0].LabelAlignmentNode, Is.Not.Null);
            Assert.That(changed.Levels[0].TextProperties, Has.Length.EqualTo(1));
            Assert.That(changed.Levels[1].Kind,
                Is.EqualTo(OdfListLevelKind.Image));
            Assert.That(changed.Levels[1].ImageDataNode, Is.Not.Null);
            Assert.That(reread.Styles.Single().ListStyle,
                Is.SameAs(changed));
            Assert.That(style.Node, Is.Not.Null);
            Assert.That(image.Node, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesCommonAndAutomaticListStylesInPackageParts()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfListStyle common = new OdfListStyle
        {
            Name = "CommonOutline",
            Kind = OdfListStyleKind.Outline,
            Levels =
            [
                new OdfListLevel
                {
                    Kind = OdfListLevelKind.Outline,
                    Level = "1",
                    NumberFormat = "I",
                    NumberSuffix = ".",
                    StartValue = "1"
                }
            ]
        };
        OdfListStyle automatic = new OdfListStyle
        {
            Name = "AutoList",
            IsAutomatic = true,
            ConsecutiveNumbering = "true",
            Levels =
            [
                new OdfListLevel
                {
                    Kind = OdfListLevelKind.Number,
                    Level = "1",
                    NumberFormat = "a",
                    NumberSuffix = "."
                }
            ]
        };
        document.ListStyles = [common, automatic];

        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            WriteDocument(document),
            "list-styles.odt",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reread.ListStyles.Single(item => item.Name == "CommonOutline")
                .PartPath, Is.EqualTo(OdfPartNames.Styles));
            Assert.That(reread.ListStyles.Single(item => item.Name == "CommonOutline")
                .Kind, Is.EqualTo(OdfListStyleKind.Outline));
            Assert.That(reread.ListStyles.Single(item => item.Name == "AutoList")
                .PartPath, Is.EqualTo(OdfPartNames.Content));
            Assert.That(reread.ListStyles.Single(item => item.Name == "AutoList")
                .Levels[0].NumberFormat, Is.EqualTo("a"));
            Assert.That(reread.Manifest?.Entries.Select(item => item.FullPath),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    static OdfStyleProperty PropertyNamed(
        OdfStyleProperty[] properties,
        string namespaceName,
        string localName)
    {
        return properties.Single(item => item.Name.NamespaceName == namespaceName
            && item.Name.LocalName == localName);
    }

    static OdfStyleProperty StyleProperty(
        string namespaceName,
        string localName,
        string value)
    {
        return new OdfStyleProperty
        {
            Name = new OdfQualifiedName
            {
                NamespaceName = namespaceName,
                LocalName = localName
            },
            LexicalValue = value
        };
    }

    static OdfNode StyleElement(string localName)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Style,
                LocalName = localName,
                Prefix = OdfNamespaces.StylePrefix
            }
        };
    }

    static OdfNode OfficeTextNode(string localName, string value)
    {
        return new OdfNode
        {
            Kind = OdfNodeKind.Element,
            Name = new OdfQualifiedName
            {
                NamespaceName = OdfNamespaces.Office,
                LocalName = localName,
                Prefix = OdfNamespaces.OfficePrefix
            },
            Children =
            [
                new OdfNode
                {
                    Kind = OdfNodeKind.Text,
                    Value = value
                }
            ]
        };
    }

    static byte[] WriteDocument(OdfDocument document)
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
            "list-styles.fodt",
            new OdfLoadOptions());
    }

    static string ListFlatXml(string sections)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:list'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + sections
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
