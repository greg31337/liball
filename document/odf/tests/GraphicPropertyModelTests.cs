using System.Text;

namespace odf.Tests;

public class GraphicPropertyModelTests
{
    [Test]
    public void ResolvesGroupedAndCompleteEffectiveGraphicProperties()
    {
        OdfDocument document = ReadFlat(GraphicXml());
        OdfGraphicProperties graphic = Drawing(document, "Styled")
            .GraphicProperties;
        OdfDrawingObject image = document.DrawingObjects.Single(
            item => item.Kind == OdfDrawingKind.Image);

        Assert.Multiple(() =>
        {
            Assert.That(graphic.Style?.Name, Is.EqualTo("Child"));
            Assert.That(graphic.Fill.Mode, Is.EqualTo("bitmap"));
            Assert.That(graphic.Fill.Color, Is.EqualTo("#112233"));
            Assert.That(graphic.Fill.GradientName, Is.EqualTo("Gradient"));
            Assert.That(graphic.Fill.HatchName, Is.EqualTo("Hatch"));
            Assert.That(graphic.Fill.ImageName, Is.EqualTo("Bitmap"));
            Assert.That(graphic.Fill.ImageRepeat, Is.EqualTo("repeat"));
            Assert.That(graphic.Fill.Opacity, Is.EqualTo("80%"));
            Assert.That(graphic.Stroke.Mode, Is.EqualTo("dash"));
            Assert.That(graphic.Stroke.DashName, Is.EqualTo("Dash"));
            Assert.That(graphic.Stroke.Width, Is.EqualTo("0.05cm"));
            Assert.That(graphic.Stroke.Color, Is.EqualTo("#445566"));
            Assert.That(graphic.Stroke.LineCap, Is.EqualTo("round"));
            Assert.That(graphic.Markers.StartName, Is.EqualTo("Arrow"));
            Assert.That(graphic.Markers.EndWidth, Is.EqualTo("0.3cm"));
            Assert.That(graphic.Shadow.Visibility, Is.EqualTo("visible"));
            Assert.That(graphic.Shadow.OffsetX, Is.EqualTo("0.2cm"));
            Assert.That(graphic.Wrap.Mode, Is.EqualTo("parallel"));
            Assert.That(graphic.Wrap.ContourValue, Is.EqualTo("true"));
            Assert.That(graphic.Wrap.RunThrough, Is.EqualTo("foreground"));
            Assert.That(graphic.Position.Horizontal, Is.EqualTo("center"));
            Assert.That(graphic.Position.Vertical, Is.EqualTo("top"));
            Assert.That(graphic.Position.Clip,
                Is.EqualTo("rect(1cm, 8cm, 6cm, 2cm)"));
            Assert.That(graphic.Image.ColorMode, Is.EqualTo("greyscale"));
            Assert.That(graphic.Image.Luminance, Is.EqualTo("20%"));
            Assert.That(graphic.Image.Gamma, Is.EqualTo("120%"));
            Assert.That(graphic.Properties.Single(item =>
                item.Name.NamespaceName == "urn:example:graphic"
                && item.Name.LocalName == "token").LexicalValue,
                Is.EqualTo("preserved"));
            Assert.That(image.GraphicProperties.Image.FilterName,
                Is.EqualTo("draw_png_Import"));
        });
    }

    [Test]
    public void WritesGroupedGenericAndImageFilterChanges()
    {
        OdfDocument document = ReadFlat(GraphicXml());
        OdfGraphicProperties graphic = Drawing(document, "Styled")
            .GraphicProperties;
        OdfDrawingObject image = document.DrawingObjects.Single(
            item => item.Kind == OdfDrawingKind.Image);
        graphic.Fill.Color = "#AABBCC";
        graphic.Stroke.LineCap = "square";
        graphic.Markers.EndName = "Diamond";
        graphic.Shadow.Opacity = "35%";
        graphic.Wrap.Mode = "dynamic";
        graphic.Position.Horizontal = "right";
        graphic.Position.Clip = "rect(0cm, 7cm, 5cm, 1cm)";
        graphic.Image.Gamma = "90%";
        graphic.Properties.Single(item =>
            item.Name.NamespaceName == "urn:example:graphic"
            && item.Name.LocalName == "token").LexicalValue = "changed";
        graphic.Properties = graphic.Properties.Where(item =>
            item.Name.NamespaceName != OdfNamespaces.Drawing
            || item.Name.LocalName != "contrast").Append(
                Property("urn:example:graphic", "added", "ext", "new"))
            .ToArray();
        image.GraphicProperties.Image.FilterName = "custom_filter";

        OdfDocument reread = ReadFlat(Write(document));
        OdfGraphicProperties updated = Drawing(reread, "Styled")
            .GraphicProperties;
        OdfDrawingObject updatedImage = reread.DrawingObjects.Single(
            item => item.Kind == OdfDrawingKind.Image);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Fill.Color, Is.EqualTo("#AABBCC"));
            Assert.That(updated.Stroke.LineCap, Is.EqualTo("square"));
            Assert.That(updated.Markers.EndName, Is.EqualTo("Diamond"));
            Assert.That(updated.Shadow.Opacity, Is.EqualTo("35%"));
            Assert.That(updated.Wrap.Mode, Is.EqualTo("dynamic"));
            Assert.That(updated.Position.Horizontal, Is.EqualTo("right"));
            Assert.That(updated.Position.Clip,
                Is.EqualTo("rect(0cm, 7cm, 5cm, 1cm)"));
            Assert.That(updated.Image.Gamma, Is.EqualTo("90%"));
            Assert.That(updated.Image.Contrast, Is.Empty);
            Assert.That(updated.Properties.Single(item =>
                item.Name.NamespaceName == "urn:example:graphic"
                && item.Name.LocalName == "token").LexicalValue,
                Is.EqualTo("changed"));
            Assert.That(updated.Properties.Single(item =>
                item.Name.NamespaceName == "urn:example:graphic"
                && item.Name.LocalName == "added").LexicalValue,
                Is.EqualTo("new"));
            Assert.That(updatedImage.GraphicProperties.Image.FilterName,
                Is.EqualTo("custom_filter"));
        });
    }

    [Test]
    public void CreatesAPropertySetForTypedGraphicChanges()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:styles><style:style style:name='Empty' "
            + "style:family='graphic'/></office:styles>"
            + "<office:body><office:drawing><draw:page draw:name='Page'>"
            + "<draw:rect draw:name='Empty Shape' draw:style-name='Empty'/>"
            + "</draw:page></office:drawing></office:body>"));
        OdfGraphicProperties graphic = Drawing(document, "Empty Shape")
            .GraphicProperties;
        graphic.Fill.Mode = "solid";
        graphic.Fill.Color = "#010203";
        graphic.Stroke.Mode = "none";
        graphic.Wrap.ContourValue = "false";

        OdfGraphicProperties reread = Drawing(
            ReadFlat(Write(document)),
            "Empty Shape").GraphicProperties;

        Assert.Multiple(() =>
        {
            Assert.That(reread.Fill.Mode, Is.EqualTo("solid"));
            Assert.That(reread.Fill.Color, Is.EqualTo("#010203"));
            Assert.That(reread.Stroke.Mode, Is.EqualTo("none"));
            Assert.That(reread.Wrap.ContourValue, Is.EqualTo("false"));
            Assert.That(reread.Style?.GraphicPropertiesNode, Is.Not.Null);
        });
    }

    static OdfDrawingObject Drawing(OdfDocument document, string name)
    {
        return document.DrawingObjects.Single(item => item.Name == name);
    }

    static OdfStyleProperty Property(
        string namespaceName,
        string localName,
        string prefix,
        string value)
    {
        OdfStyleProperty result = new OdfStyleProperty();
        result.Name.NamespaceName = namespaceName;
        result.Name.LocalName = localName;
        result.Name.Prefix = prefix;
        result.LexicalValue = value;
        return result;
    }

    static string Write(OdfDocument document)
    {
        return Encoding.UTF8.GetString(
            new OdfDocumentWriter().WriteDocumentBytes(
                document,
                new OdfSaveOptions
                {
                    Deterministic = true
                }));
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "graphic-properties.fodg",
            new OdfLoadOptions());
    }

    static string GraphicXml()
    {
        return FlatXml(
            "<office:styles>"
            + "<style:style style:name='Base' style:family='graphic'>"
            + "<style:graphic-properties draw:fill='bitmap' "
            + "draw:fill-color='#112233' "
            + "draw:secondary-fill-color='#223344' "
            + "draw:fill-gradient-name='Gradient' "
            + "draw:fill-hatch-name='Hatch' draw:fill-hatch-solid='true' "
            + "draw:fill-image-name='Bitmap' style:repeat='repeat' "
            + "draw:fill-image-width='40%' draw:fill-image-height='2cm' "
            + "draw:fill-image-ref-point='center' "
            + "draw:opacity='60%' svg:fill-rule='evenodd' "
            + "draw:stroke='dash' draw:stroke-dash='Dash' "
            + "svg:stroke-width='0.05cm' svg:stroke-color='#445566' "
            + "svg:stroke-opacity='75%' draw:stroke-linejoin='bevel' "
            + "svg:stroke-linecap='round' draw:marker-start='Arrow' "
            + "draw:marker-end='Arrow' draw:marker-start-width='0.2cm' "
            + "draw:marker-end-width='0.3cm' "
            + "draw:marker-start-center='true' "
            + "draw:marker-end-center='false' draw:shadow='visible' "
            + "draw:shadow-offset-x='0.2cm' draw:shadow-offset-y='0.3cm' "
            + "draw:shadow-color='#778899' draw:shadow-opacity='50%' "
            + "style:horizontal-pos='center' style:horizontal-rel='page' "
            + "style:vertical-pos='top' style:vertical-rel='paragraph' "
            + "draw:textarea-horizontal-align='center' "
            + "draw:textarea-vertical-align='middle' "
            + "fo:wrap-option='wrap' style:mirror='horizontal' "
            + "fo:clip='rect(1cm, 8cm, 6cm, 2cm)'/>"
            + "</style:style>"
            + "</office:styles>"
            + "<office:automatic-styles>"
            + "<style:style style:name='Child' style:family='graphic' "
            + "style:parent-style-name='Base'>"
            + "<style:graphic-properties draw:opacity='80%' "
            + "style:wrap='parallel' style:wrap-dynamic-threshold='1cm' "
            + "style:number-wrapped-paragraphs='2' "
            + "style:wrap-contour='true' style:wrap-contour-mode='outside' "
            + "style:run-through='foreground' style:flow-with-text='true' "
            + "draw:wrap-influence-on-position='once-successive' "
            + "draw:color-mode='greyscale' draw:color-inversion='false' "
            + "draw:luminance='20%' draw:contrast='10%' draw:gamma='120%' "
            + "draw:red='5%' draw:green='-5%' draw:blue='0%' "
            + "draw:image-opacity='90%' ext:token='preserved'/>"
            + "</style:style>"
            + "</office:automatic-styles>"
            + "<office:body><office:drawing><draw:page draw:name='Page'>"
            + "<draw:rect draw:name='Styled' draw:style-name='Child'/>"
            + "<draw:frame draw:name='Image Frame'>"
            + "<draw:image draw:filter-name='draw_png_Import'/>"
            + "</draw:frame>"
            + "</draw:page></office:drawing></office:body>");
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:style='" + OdfNamespaces.Style + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " xmlns:ext='urn:example:graphic'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
