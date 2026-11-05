using System.Text;

namespace odf.Tests;

public class AdditionalStylePropertyDocumentModelTests
{
    [Test]
    public void MaterializesEveryRemainingStylePropertySet()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:style style:name='Section' style:family='section'>"
            + "<style:section-properties fo:background-color='#FAFAFA' "
            + "style:protect='true' text:dont-balance-text-columns='true' "
            + "ext:section-token='section-lexical'>"
            + "<style:columns fo:column-count='2' fo:column-gap='0.50cm'/>"
            + "</style:section-properties></style:style>"
            + "<style:style style:name='Table' style:family='table'>"
            + "<style:table-properties style:width='12.000cm' "
            + "table:align='margins' table:border-model='collapsing'>"
            + "<style:background-image xlink:href='Pictures/table.png' "
            + "xlink:type='simple'/>"
            + "</style:table-properties></style:style>"
            + "<style:style style:name='Column' style:family='table-column'>"
            + "<style:table-column-properties style:column-width='2.500cm' "
            + "style:rel-column-width='1*' "
            + "style:use-optimal-column-width='false'/>"
            + "</style:style>"
            + "<style:style style:name='Row' style:family='table-row'>"
            + "<style:table-row-properties style:row-height='0.750cm' "
            + "style:min-row-height='0.500cm' fo:keep-together='always'/>"
            + "</style:style>"
            + "<style:style style:name='Cell' style:family='table-cell'>"
            + "<style:table-cell-properties style:vertical-align='middle' "
            + "style:cell-protect='protected formula-hidden' "
            + "fo:background-color='#FFFFCC' fo:wrap-option='wrap'>"
            + "<style:background-image xlink:href='Pictures/cell.png' "
            + "xlink:type='simple'/></style:table-cell-properties>"
            + "</style:style>"
            + "<style:style style:name='Graphic' style:family='graphic'>"
            + "<style:graphic-properties draw:fill='solid' "
            + "draw:fill-color='#336699' svg:stroke-width='0.050cm' "
            + "style:wrap='parallel' ext:graphic-token='graphic-lexical'>"
            + "<style:background-image xlink:href='Pictures/graphic.png' "
            + "xlink:type='simple'/></style:graphic-properties>"
            + "</style:style>"
            + "<style:style style:name='Presentation' style:family='presentation'>"
            + "<style:graphic-properties draw:fill='none' "
            + "presentation:style-name='Slide Object'/>"
            + "</style:style>"
            + "<style:style style:name='Chart' style:family='chart'>"
            + "<style:chart-properties chart:scale-text='true' "
            + "chart:three-dimensional='false' chart:symbol-width='0.300cm'/>"
            + "</style:style>"
            + "<style:style style:name='DrawingPage' style:family='drawing-page'>"
            + "<style:drawing-page-properties "
            + "presentation:transition-type='automatic' "
            + "presentation:transition-speed='medium' "
            + "smil:type='fade' draw:background-size='full'>"
            + "<presentation:sound xlink:href='Media/transition.ogg' "
            + "xlink:type='simple'/></style:drawing-page-properties>"
            + "</style:style>"
            + "<style:style style:name='Ruby' style:family='ruby'>"
            + "<style:ruby-properties style:ruby-position='above' "
            + "style:ruby-align='distribute-space'/></style:style>"
            + "<style:style style:name='List' style:family='list'>"
            + "<style:list-level-properties text:space-before='0.500cm' "
            + "text:min-label-width='0.750cm' "
            + "text:list-level-position-and-space-mode='label-alignment'>"
            + "<style:list-level-label-alignment text:label-followed-by='tab' "
            + "text:list-tab-stop-position='1.250cm'/></style:list-level-properties>"
            + "</style:style>"
            + "</office:styles>"));

        OdfStyle section = Named(document, "Section");
        OdfStyle table = Named(document, "Table");
        OdfStyle column = Named(document, "Column");
        OdfStyle row = Named(document, "Row");
        OdfStyle cell = Named(document, "Cell");
        OdfStyle graphic = Named(document, "Graphic");
        OdfStyle presentation = Named(document, "Presentation");
        OdfStyle chart = Named(document, "Chart");
        OdfStyle drawingPage = Named(document, "DrawingPage");
        OdfStyle ruby = Named(document, "Ruby");
        OdfStyle list = Named(document, "List");

        Assert.Multiple(() =>
        {
            Assert.That(
                FindProperty(
                    section.SectionProperties,
                    OdfNamespaces.Text,
                    "dont-balance-text-columns").LexicalValue,
                Is.EqualTo("true"));
            Assert.That(section.SectionPropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                FindProperty(
                    table.TableProperties,
                    OdfNamespaces.Style,
                    "width").LexicalValue,
                Is.EqualTo("12.000cm"));
            Assert.That(table.TablePropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                FindProperty(
                    column.TableColumnProperties,
                    OdfNamespaces.Style,
                    "rel-column-width").LexicalValue,
                Is.EqualTo("1*"));
            Assert.That(
                FindProperty(
                    row.TableRowProperties,
                    OdfNamespaces.Fo,
                    "keep-together").LexicalValue,
                Is.EqualTo("always"));
            Assert.That(
                FindProperty(
                    cell.TableCellProperties,
                    OdfNamespaces.Style,
                    "cell-protect").LexicalValue,
                Is.EqualTo("protected formula-hidden"));
            Assert.That(cell.TableCellPropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                FindProperty(
                    graphic.GraphicProperties,
                    "urn:example:style-extension",
                    "graphic-token").LexicalValue,
                Is.EqualTo("graphic-lexical"));
            Assert.That(graphic.GraphicPropertyChildren, Has.Length.EqualTo(1));
            Assert.That(
                FindProperty(
                    presentation.GraphicProperties,
                    OdfNamespaces.Drawing,
                    "fill").LexicalValue,
                Is.EqualTo("none"));
            Assert.That(
                FindProperty(
                    chart.ChartProperties,
                    OdfNamespaces.Chart,
                    "symbol-width").LexicalValue,
                Is.EqualTo("0.300cm"));
            Assert.That(
                FindProperty(
                    drawingPage.DrawingPageProperties,
                    OdfNamespaces.Smil,
                    "type").LexicalValue,
                Is.EqualTo("fade"));
            Assert.That(
                drawingPage.DrawingPagePropertyChildren[0].Name?.LocalName,
                Is.EqualTo("sound"));
            Assert.That(
                FindProperty(
                    ruby.RubyProperties,
                    OdfNamespaces.Style,
                    "ruby-align").LexicalValue,
                Is.EqualTo("distribute-space"));
            Assert.That(
                FindProperty(
                    list.ListProperties,
                    OdfNamespaces.Text,
                    "space-before").LexicalValue,
                Is.EqualTo("0.500cm"));
            Assert.That(list.ListPropertyChildren, Has.Length.EqualTo(1));
        });

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        Assert.That(
            FindProperty(
                Named(reread, "DrawingPage").DrawingPageProperties,
                OdfNamespaces.Presentation,
                "transition-speed").LexicalValue,
            Is.EqualTo("medium"));
    }

    [Test]
    public void WritesChangesAndRemovesPropertyAttributesAndElements()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles>"
            + "<style:style style:name='Graphic' style:family='graphic'>"
            + "<style:graphic-properties draw:fill='solid' "
            + "draw:fill-color='#112233' style:wrap='parallel' ext:token='keep'>"
            + "<style:background-image xlink:href='Pictures/old.png' "
            + "xlink:type='simple'/></style:graphic-properties>"
            + "</style:style>"
            + "<style:style style:name='Chart' style:family='chart'>"
            + "<style:chart-properties chart:scale-text='false'/>"
            + "</style:style>"
            + "<style:style style:name='Ruby' style:family='ruby'>"
            + "<style:ruby-properties style:ruby-align='left'/>"
            + "</style:style>"
            + "</office:styles>"));
        OdfStyle graphic = Named(document, "Graphic");
        OdfStyleProperty fill = FindProperty(
            graphic.GraphicProperties,
            OdfNamespaces.Drawing,
            "fill");
        OdfStyleProperty extension = FindProperty(
            graphic.GraphicProperties,
            "urn:example:style-extension",
            "token");
        fill.LexicalValue = "gradient";
        graphic.GraphicProperties =
        [
            fill,
            extension,
            Property(OdfNamespaces.Svg, "stroke-width", "0.075cm")
        ];
        graphic.GraphicPropertyChildren = [];
        OdfStyle chart = Named(document, "Chart");
        chart.ChartProperties[0].LexicalValue = "true";
        chart.GraphicProperties =
        [
            Property(OdfNamespaces.Drawing, "fill", "none")
        ];
        OdfStyle ruby = Named(document, "Ruby");
        ruby.RubyProperties = [];

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));
        OdfStyle updatedGraphic = Named(reread, "Graphic");
        OdfStyle updatedChart = Named(reread, "Chart");
        OdfStyle updatedRuby = Named(reread, "Ruby");

        Assert.Multiple(() =>
        {
            Assert.That(updatedGraphic.GraphicProperties, Has.Length.EqualTo(3));
            Assert.That(
                FindProperty(
                    updatedGraphic.GraphicProperties,
                    OdfNamespaces.Drawing,
                    "fill").LexicalValue,
                Is.EqualTo("gradient"));
            Assert.That(
                updatedGraphic.GraphicProperties.Any(
                    item => item.Name.LocalName == "fill-color"),
                Is.False);
            Assert.That(updatedGraphic.GraphicPropertyChildren, Is.Empty);
            Assert.That(
                FindProperty(
                    updatedChart.ChartProperties,
                    OdfNamespaces.Chart,
                    "scale-text").LexicalValue,
                Is.EqualTo("true"));
            Assert.That(updatedChart.GraphicPropertiesNode, Is.Not.Null);
            Assert.That(
                updatedChart.Node?.Children.Select(item => item.Name?.LocalName),
                Is.EqualTo(new[]
                {
                    "chart-properties",
                    "graphic-properties"
                }));
            Assert.That(updatedRuby.RubyPropertiesNode, Is.Null);
            Assert.That(fill.Attribute, Is.Not.Null);
        });
    }

    [Test]
    public void CreatesAllRemainingPropertySetsInPackageStyles()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        OdfStyle section = Style("Section", OdfStyleFamily.Section);
        section.SectionProperties =
        [
            Property(OdfNamespaces.Text, "dont-balance-text-columns", "false")
        ];
        OdfStyle table = Style("Table", OdfStyleFamily.Table);
        table.TableProperties =
        [
            Property(OdfNamespaces.Table, "align", "center")
        ];
        OdfStyle column = Style("Column", OdfStyleFamily.TableColumn);
        column.TableColumnProperties =
        [
            Property(OdfNamespaces.Style, "column-width", "3.000cm")
        ];
        OdfStyle row = Style("Row", OdfStyleFamily.TableRow);
        row.TableRowProperties =
        [
            Property(OdfNamespaces.Style, "row-height", "0.800cm")
        ];
        OdfStyle cell = Style("Cell", OdfStyleFamily.TableCell);
        cell.TableCellProperties =
        [
            Property(OdfNamespaces.Fo, "background-color", "#FFFFFF")
        ];
        OdfStyle graphic = Style("Graphic", OdfStyleFamily.Graphic);
        graphic.GraphicProperties =
        [
            Property(OdfNamespaces.Drawing, "fill", "solid")
        ];
        OdfStyle presentation = Style(
            "Presentation",
            OdfStyleFamily.Presentation);
        presentation.GraphicProperties =
        [
            Property(OdfNamespaces.Drawing, "fill", "none")
        ];
        OdfStyle chart = Style("Chart", OdfStyleFamily.Chart);
        chart.ChartProperties =
        [
            Property(OdfNamespaces.Chart, "three-dimensional", "true")
        ];
        chart.GraphicProperties =
        [
            Property(OdfNamespaces.Drawing, "stroke", "solid")
        ];
        OdfStyle page = Style("Page", OdfStyleFamily.DrawingPage);
        page.DrawingPageProperties =
        [
            Property(OdfNamespaces.Presentation, "transition-type", "manual"),
            Property(OdfNamespaces.Smil, "subtype", "crossfade")
        ];
        OdfStyle ruby = Style("Ruby", OdfStyleFamily.Ruby);
        ruby.RubyProperties =
        [
            Property(OdfNamespaces.Style, "ruby-position", "below")
        ];
        OdfStyle list = Style("List", OdfStyleFamily.List);
        list.ListProperties =
        [
            Property(OdfNamespaces.Text, "min-label-distance", "0.250cm")
        ];
        document.Styles =
        [
            section,
            table,
            column,
            row,
            cell,
            graphic,
            presentation,
            chart,
            page,
            ruby,
            list
        ];

        byte[] written = Write(document);
        OdfDocument reread = new OdfDocumentReader().ReadDocumentBytes(
            written,
            "additional-properties.odt",
            new OdfLoadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(reread.Styles, Has.Length.EqualTo(11));
            Assert.That(Named(reread, "Section").SectionPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Table").TablePropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Column").TableColumnPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Row").TableRowPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Cell").TableCellPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Graphic").GraphicPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Presentation").GraphicPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Chart").ChartPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Page").DrawingPagePropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "Ruby").RubyPropertiesNode, Is.Not.Null);
            Assert.That(Named(reread, "List").ListPropertiesNode, Is.Not.Null);
            Assert.That(
                FindProperty(
                    Named(reread, "Page").DrawingPageProperties,
                    OdfNamespaces.Smil,
                    "subtype").Name.Prefix,
                Is.EqualTo(OdfNamespaces.SmilPrefix));
            Assert.That(
                reread.Parts.Select(item => item.Path),
                Does.Contain(OdfPartNames.Styles));
        });
    }

    static OdfStyle Named(OdfDocument document, string name)
    {
        return document.Styles.Single(item => item.Name == name);
    }

    static OdfStyle Style(string name, OdfStyleFamily family)
    {
        return new OdfStyle
        {
            Name = name,
            Family = family,
            IsCommon = true
        };
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
            "additional-properties.fodt",
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
            + " xmlns:table='" + OdfNamespaces.Table + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:chart='" + OdfNamespaces.Chart + "'"
            + " xmlns:presentation='" + OdfNamespaces.Presentation + "'"
            + " xmlns:smil='" + OdfNamespaces.Smil + "'"
            + " xmlns:xlink='" + OdfNamespaces.XLink + "'"
            + " xmlns:ext='urn:example:style-extension'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Text + "'>"
            + styles
            + "<office:body><office:text/></office:body>"
            + "</office:document>";
    }
}
