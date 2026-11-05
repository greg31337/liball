using System.Text;

namespace odf.Tests;

public class DrawingShapeModelTests
{
    [Test]
    public void MaterializesEveryTwoDimensionalShapeGeometry()
    {
        OdfDocument document = ReadFlat(ShapeXml());
        OdfDrawingObject rectangle = Shape(document, "Rectangle");
        OdfDrawingObject ellipse = Shape(document, "Ellipse");
        OdfDrawingObject circle = Shape(document, "Circle");
        OdfDrawingObject line = Shape(document, "Line");
        OdfDrawingObject polyline = Shape(document, "Polyline");
        OdfDrawingObject polygon = Shape(document, "Polygon");
        OdfDrawingObject path = Shape(document, "Path");
        OdfDrawingObject connector = Shape(document, "Connector");
        OdfDrawingObject measure = Shape(document, "Measure");
        OdfDrawingObject caption = Shape(document, "Caption");
        OdfDrawingObject control = Shape(document, "Control");
        OdfDrawingObject frame = Shape(document, "Frame");
        OdfDrawingObject textBox = document.DrawingObjects.Single(
            item => item.Kind == OdfDrawingKind.TextBox);

        Assert.Multiple(() =>
        {
            Assert.That(rectangle.Kind, Is.EqualTo(OdfDrawingKind.Rectangle));
            Assert.That(rectangle.Geometry.CornerRadius, Is.EqualTo("0.2cm"));
            Assert.That(rectangle.X, Is.EqualTo("1cm"));
            Assert.That(ellipse.Kind, Is.EqualTo(OdfDrawingKind.Ellipse));
            Assert.That(ellipse.Geometry.CenterX, Is.EqualTo("5cm"));
            Assert.That(ellipse.Geometry.RadiusX, Is.EqualTo("2cm"));
            Assert.That(ellipse.Geometry.EllipseKind, Is.EqualTo("section"));
            Assert.That(ellipse.Geometry.StartAngle, Is.EqualTo("30deg"));
            Assert.That(ellipse.Geometry.EndAngle, Is.EqualTo("120deg"));
            Assert.That(circle.Kind, Is.EqualTo(OdfDrawingKind.Ellipse));
            Assert.That(circle.ElementName, Is.EqualTo("circle"));
            Assert.That(circle.Geometry.Radius, Is.EqualTo("1cm"));
            Assert.That(line.Geometry.X1, Is.EqualTo("1cm"));
            Assert.That(line.Geometry.Y2, Is.EqualTo("4cm"));
            Assert.That(polyline.Geometry.Points, Is.EqualTo("0,0 10,10"));
            Assert.That(polyline.Geometry.ViewBox, Is.EqualTo("0 0 10 10"));
            Assert.That(polygon.Geometry.Points,
                Is.EqualTo("0,0 10,0 10,10"));
            Assert.That(path.Geometry.PathData, Is.EqualTo("M 0 0 L 10 10"));
            Assert.That(path.Geometry.ViewBox, Is.EqualTo("0 0 10 10"));
            Assert.That(connector.Geometry.ConnectorType, Is.EqualTo("curve"));
            Assert.That(connector.Geometry.StartGluePoint, Is.EqualTo("1"));
            Assert.That(connector.Geometry.EndGluePoint, Is.EqualTo("2"));
            Assert.That(connector.Geometry.LineSkew, Is.EqualTo("1cm 2cm"));
            Assert.That(connector.References.Single(
                item => item.Kind == OdfDrawingReferenceKind.ConnectorStart)
                .TargetObject, Is.SameAs(rectangle));
            Assert.That(measure.Kind, Is.EqualTo(OdfDrawingKind.Measure));
            Assert.That(measure.Geometry.X2, Is.EqualTo("9cm"));
            Assert.That(caption.Geometry.CaptionPointX, Is.EqualTo("3cm"));
            Assert.That(caption.Geometry.CaptionPointY, Is.EqualTo("4cm"));
            Assert.That(caption.TextBlocks[0].Text, Is.EqualTo("Caption text"));
            Assert.That(control.Kind, Is.EqualTo(OdfDrawingKind.Control));
            Assert.That(control.ControlId, Is.EqualTo("ControlA"));
            Assert.That(control.References.Single(
                item => item.Kind == OdfDrawingReferenceKind.Control).Target,
                Is.EqualTo("ControlA"));
            Assert.That(frame.TextBox, Is.Not.Null);
            Assert.That(textBox.TextBox, Is.SameAs(frame.TextBox));
            Assert.That(textBox.TextBlocks[0].Text, Is.EqualTo("Box text"));
            Assert.That(textBox.TextBox?.MinimumWidth, Is.EqualTo("2cm"));
            Assert.That(textBox.TextBox?.XmlId, Is.EqualTo("box1"));
            Assert.That(textBox.TextBox?.TextId, Is.EqualTo("box-text"));
            Assert.That(textBox.TextBox?.ChainNextName, Is.EqualTo("Second"));
            Assert.That(frame.TextBox?.Frame?.NextFrame?.Name,
                Is.EqualTo("Second"));
            Assert.That(textBox.Geometry.RadiusX, Is.EqualTo("0.1cm"));
        });
    }

    [Test]
    public void WritesShapeGeometryControlAndTextBoxChanges()
    {
        OdfDocument document = ReadFlat(ShapeXml());
        OdfDrawingObject rectangle = Shape(document, "Rectangle");
        OdfDrawingObject ellipse = Shape(document, "Ellipse");
        OdfDrawingObject line = Shape(document, "Line");
        OdfDrawingObject polyline = Shape(document, "Polyline");
        OdfDrawingObject path = Shape(document, "Path");
        OdfDrawingObject connector = Shape(document, "Connector");
        OdfDrawingObject caption = Shape(document, "Caption");
        OdfDrawingObject control = Shape(document, "Control");
        OdfDrawingObject textBox = document.DrawingObjects.Single(
            item => item.Kind == OdfDrawingKind.TextBox);
        rectangle.Geometry.CornerRadius = "0.5cm";
        ellipse.Geometry.CenterX = "6cm";
        ellipse.Geometry.RadiusY = "2.5cm";
        line.Geometry.X2 = "8cm";
        polyline.Geometry.Points = "0,0 20,20";
        path.Geometry.PathData = "M 0 0 L 20 20";
        connector.Geometry.ConnectorType = "lines";
        connector.Geometry.StartGluePoint = "3";
        caption.Geometry.CaptionPointX = "7cm";
        caption.TextBlocks[0].Text = "Changed caption";
        control.ControlId = "ControlB";
        textBox.Geometry.RadiusX = "0.3cm";
        textBox.TextBox!.MinimumWidth = "4cm";

        OdfDocument reread = ReadFlat(
            Encoding.UTF8.GetString(Write(document)));

        Assert.Multiple(() =>
        {
            Assert.That(Shape(reread, "Rectangle").Geometry.CornerRadius,
                Is.EqualTo("0.5cm"));
            Assert.That(Shape(reread, "Ellipse").Geometry.CenterX,
                Is.EqualTo("6cm"));
            Assert.That(Shape(reread, "Ellipse").Geometry.RadiusY,
                Is.EqualTo("2.5cm"));
            Assert.That(Shape(reread, "Line").Geometry.X2,
                Is.EqualTo("8cm"));
            Assert.That(Shape(reread, "Polyline").Geometry.Points,
                Is.EqualTo("0,0 20,20"));
            Assert.That(Shape(reread, "Path").Geometry.PathData,
                Is.EqualTo("M 0 0 L 20 20"));
            Assert.That(Shape(reread, "Connector").Geometry.ConnectorType,
                Is.EqualTo("lines"));
            Assert.That(Shape(reread, "Connector").Geometry.StartGluePoint,
                Is.EqualTo("3"));
            Assert.That(Shape(reread, "Caption").Geometry.CaptionPointX,
                Is.EqualTo("7cm"));
            Assert.That(Shape(reread, "Caption").TextBlocks[0].Text,
                Is.EqualTo("Changed caption"));
            Assert.That(Shape(reread, "Control").ControlId,
                Is.EqualTo("ControlB"));
            Assert.That(reread.DrawingObjects.Single(
                item => item.Kind == OdfDrawingKind.TextBox)
                .Geometry.RadiusX, Is.EqualTo("0.3cm"));
            Assert.That(reread.DrawingObjects.Single(
                item => item.Kind == OdfDrawingKind.TextBox)
                .TextBox?.MinimumWidth, Is.EqualTo("4cm"));
        });
    }

    [Test]
    public void MaterializesRegularPolygonLexicalGeometry()
    {
        OdfDocument document = ReadFlat(FlatXml(
            "<office:body><office:drawing><draw:page "
            + "draw:name='Page' draw:master-page-name='Standard'>"
            + "<draw:regular-polygon draw:name='Star' draw:corners='5' "
            + "draw:concave='true' draw:sharpness='40%'/>"
            + "</draw:page></office:drawing></office:body>"));
        OdfDrawingObject star = Shape(document, "Star");

        Assert.Multiple(() =>
        {
            Assert.That(star.Kind,
                Is.EqualTo(OdfDrawingKind.RegularPolygon));
            Assert.That(star.Geometry.Corners, Is.EqualTo("5"));
            Assert.That(star.Geometry.IsConcave, Is.True);
            Assert.That(star.Geometry.IsConcaveValid, Is.True);
            Assert.That(star.Geometry.Sharpness, Is.EqualTo("40%"));
        });
    }

    static OdfDrawingObject Shape(OdfDocument document, string name)
    {
        return document.DrawingObjects.Single(item => item.Name == name);
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
            "shapes.fodg",
            new OdfLoadOptions());
    }

    static string ShapeXml()
    {
        return FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page' draw:master-page-name='Standard'>"
            + "<draw:rect draw:name='Rectangle' draw:id='rect1' "
            + "svg:x='1cm' svg:y='2cm' svg:width='3cm' svg:height='4cm' "
            + "draw:corner-radius='0.2cm'/>"
            + "<draw:ellipse draw:name='Ellipse' svg:cx='5cm' svg:cy='6cm' "
            + "svg:rx='2cm' svg:ry='1cm' draw:kind='section' "
            + "draw:start-angle='30deg' draw:end-angle='120deg'/>"
            + "<draw:circle draw:name='Circle' svg:cx='2cm' svg:cy='2cm' "
            + "svg:r='1cm'/>"
            + "<draw:line draw:name='Line' svg:x1='1cm' svg:y1='2cm' "
            + "svg:x2='3cm' svg:y2='4cm'/>"
            + "<draw:polyline draw:name='Polyline' draw:points='0,0 10,10' "
            + "svg:viewBox='0 0 10 10'/>"
            + "<draw:polygon draw:name='Polygon' "
            + "draw:points='0,0 10,0 10,10' svg:viewBox='0 0 10 10'/>"
            + "<draw:path draw:name='Path' svg:d='M 0 0 L 10 10' "
            + "svg:viewBox='0 0 10 10'/>"
            + "<draw:connector draw:name='Connector' draw:type='curve' "
            + "draw:start-shape='rect1' draw:end-shape='ellipse1' "
            + "draw:start-glue-point='1' draw:end-glue-point='2' "
            + "draw:line-skew='1cm 2cm' svg:x1='1cm' svg:y1='1cm' "
            + "svg:x2='5cm' svg:y2='5cm' svg:d='M 0 0 C 1 1 2 2 3 3'/>"
            + "<draw:measure draw:name='Measure' svg:x1='1cm' svg:y1='2cm' "
            + "svg:x2='9cm' svg:y2='2cm'/>"
            + "<draw:caption draw:name='Caption' draw:caption-point-x='3cm' "
            + "draw:caption-point-y='4cm' svg:rx='0.2cm'>"
            + "<text:p>Caption text</text:p></draw:caption>"
            + "<draw:control draw:name='Control' draw:control='ControlA' "
            + "svg:x='1cm' svg:y='1cm' svg:width='2cm' svg:height='1cm'/>"
            + "<draw:frame draw:name='Frame'>"
            + "<draw:text-box fo:min-width='2cm' fo:max-height='5cm' "
            + "svg:rx='0.1cm' xml:id='box1' text:id='box-text' "
            + "draw:chain-next-name='Second'>"
            + "<text:p>Box text</text:p></draw:text-box>"
            + "</draw:frame>"
            + "<draw:frame draw:name='Second'/>"
            + "<draw:ellipse draw:name='Ellipse target' draw:id='ellipse1' "
            + "svg:cx='1cm' svg:cy='1cm' svg:rx='1cm' svg:ry='1cm'/>"
            + "</draw:page></office:drawing></office:body>");
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:text='" + OdfNamespaces.Text + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " xmlns:fo='" + OdfNamespaces.Fo + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
