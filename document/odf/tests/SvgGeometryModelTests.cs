using System.Text;

namespace odf.Tests;

public class SvgGeometryModelTests
{
    [Test]
    public void ParsesPathsPointsViewBoxesTransformsAndContours()
    {
        OdfDocument document = ReadFlat(GeometryXml());
        OdfDrawingObject path = Drawing(document, "Path");
        OdfDrawingObject polygon = Drawing(document, "Polygon");
        OdfDrawingObject frame = Drawing(document, "Frame");
        OdfContour pathContour = Drawing(document, "Path Frame").Contour;
        OdfEnhancedGeometry enhanced = Drawing(document, "Custom")
            .Geometry.EnhancedGeometry!;

        Assert.Multiple(() =>
        {
            Assert.That(path.Geometry.Path.IsValid, Is.True);
            Assert.That(path.Geometry.Path.Commands, Has.Length.EqualTo(10));
            Assert.That(path.Geometry.Path.Commands[0].Command, Is.EqualTo("M"));
            Assert.That(path.Geometry.Path.Commands[1].Parameters,
                Is.EqualTo(new[] { 10.5, -20.0 }));
            Assert.That(path.Geometry.Path.Commands[8].Command, Is.EqualTo("A"));
            Assert.That(path.Geometry.Path.Commands[9].Command, Is.EqualTo("z"));
            Assert.That(path.Geometry.ViewBoxValue.IsValid, Is.True);
            Assert.That(path.Geometry.ViewBoxValue.Left, Is.EqualTo(-10));
            Assert.That(path.Geometry.ViewBoxValue.Width, Is.EqualTo(100));
            Assert.That(path.Transform.IsValid, Is.True);
            Assert.That(path.Transform.Operations, Has.Length.EqualTo(5));
            Assert.That(path.Transform.Operations[0].Kind,
                Is.EqualTo(OdfTransformOperationKind.Translate));
            Assert.That(path.Transform.Operations[0].Arguments,
                Is.EqualTo(new[] { "01.00cm", "2.500cm" }));
            Assert.That(path.Transform.Operations[4].Kind,
                Is.EqualTo(OdfTransformOperationKind.Matrix));
            Assert.That(polygon.Geometry.PointList.IsValid, Is.True);
            Assert.That(polygon.Geometry.PointList.Points, Has.Length.EqualTo(3));
            Assert.That(polygon.Geometry.PointList.Points[1].X, Is.EqualTo(10));
            Assert.That(frame.Contour.Kind, Is.EqualTo(OdfContourKind.Polygon));
            Assert.That(frame.Contour.PointList.IsValid, Is.True);
            Assert.That(frame.Contour.ViewBox.Height, Is.EqualTo(100));
            Assert.That(frame.Contour.RecreateOnEdit, Is.True);
            Assert.That(frame.Contour.IsRecreateOnEditValid, Is.True);
            Assert.That(pathContour.Kind, Is.EqualTo(OdfContourKind.Path));
            Assert.That(pathContour.Path.IsValid, Is.True);
            Assert.That(pathContour.Path.Commands, Has.Length.EqualTo(5));
            Assert.That(enhanced.ViewBoxValue.IsValid, Is.True);
            Assert.That(enhanced.ViewBoxValue.Width, Is.EqualTo(1000));
        });
    }

    [Test]
    public void TypedGeometryChangesAreWrittenCanonicallyInPreserveMode()
    {
        OdfDocument document = ReadFlat(GeometryXml());
        OdfDrawingObject path = Drawing(document, "Path");
        OdfDrawingObject polygon = Drawing(document, "Polygon");
        OdfContour contour = Drawing(document, "Frame").Contour;
        OdfEnhancedGeometry enhanced = Drawing(document, "Custom")
            .Geometry.EnhancedGeometry!;
        path.Geometry.Path.Commands[1].Parameters[0] = 25;
        path.Geometry.ViewBoxValue.Width = 250;
        path.Transform.Operations[0].Arguments[1] = "3.00cm";
        polygon.Geometry.PointList.Points[1].Y = 15;
        contour.PointList.Points[2].X = 75;
        contour.RecreateOnEdit = false;
        enhanced.ViewBoxValue.Height = 750;

        OdfDocument reread = ReadFlat(Write(document, true));
        OdfDrawingObject changedPath = Drawing(reread, "Path");
        OdfDrawingObject changedPolygon = Drawing(reread, "Polygon");
        OdfContour changedContour = Drawing(reread, "Frame").Contour;

        Assert.Multiple(() =>
        {
            Assert.That(changedPath.Geometry.PathData,
                Does.StartWith("M 0 0 L 25 -20"));
            Assert.That(changedPath.Geometry.ViewBox,
                Is.EqualTo("-10 20 250 200"));
            Assert.That(changedPath.Transform.Expression,
                Does.StartWith("translate(1cm 3cm)"));
            Assert.That(changedPolygon.Geometry.Points,
                Is.EqualTo("0,0 10,15 10,10"));
            Assert.That(changedContour.PointList.LexicalValue,
                Is.EqualTo("0,0 100,0 75,100 0,100"));
            Assert.That(changedContour.RecreateOnEditValue,
                Is.EqualTo("false"));
            Assert.That(Drawing(reread, "Custom").Geometry
                .EnhancedGeometry!.ViewBox,
                Is.EqualTo("0 0 1000 750"));
        });
    }

    [Test]
    public void SaveOptionChoosesPreservedOrCanonicalLexicalGeometry()
    {
        OdfDocument preserved = ReadFlat(GeometryXml());
        OdfDocument canonical = ReadFlat(GeometryXml());

        OdfDocument preservedAgain = ReadFlat(Write(preserved, true));
        OdfDocument canonicalAgain = ReadFlat(Write(canonical, false));

        Assert.Multiple(() =>
        {
            Assert.That(Drawing(preservedAgain, "Path").Geometry.PathData,
                Is.EqualTo(PathText()));
            Assert.That(Drawing(preservedAgain, "Path").Geometry.ViewBox,
                Is.EqualTo(" -10 +20 00100 200 "));
            Assert.That(Drawing(preservedAgain, "Path").Transform.Expression,
                Is.EqualTo(TransformText()));
            Assert.That(Drawing(canonicalAgain, "Path").Geometry.PathData,
                Does.StartWith("M 0 0 L 10.5 -20"));
            Assert.That(Drawing(canonicalAgain, "Path").Geometry.ViewBox,
                Is.EqualTo("-10 20 100 200"));
            Assert.That(Drawing(canonicalAgain, "Path").Transform.Expression,
                Is.EqualTo("translate(1cm 2.5cm) rotate(0.5) "
                    + "scale(2 3) skewX(-0.25) matrix(1 0 0 1 4 5)"));
            Assert.That(Drawing(canonicalAgain, "Frame").Contour.Width,
                Is.EqualTo("2cm"));
        });
    }

    [Test]
    public void InvalidGeometryRemainsAvailableAndIsNeverCanonicalized()
    {
        OdfDocument document = ReadFlat(InvalidGeometryXml());
        OdfDrawingObject invalid = Drawing(document, "Invalid");

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Geometry.Path.IsValid, Is.False);
            Assert.That(invalid.Geometry.PointList.IsValid, Is.False);
            Assert.That(invalid.Geometry.ViewBoxValue.IsValid, Is.False);
            Assert.That(invalid.Transform.IsValid, Is.False);
        });

        OdfDrawingObject reread = Drawing(
            ReadFlat(Write(document, false)),
            "Invalid");
        Assert.Multiple(() =>
        {
            Assert.That(reread.Geometry.PathData, Is.EqualTo("L 1 nope"));
            Assert.That(reread.Geometry.Points, Is.EqualTo("0,0 1,bad"));
            Assert.That(reread.Geometry.ViewBox, Is.EqualTo("0 0 10"));
            Assert.That(reread.Transform.Expression,
                Is.EqualTo("translate(2)"));
        });
    }

    static OdfDrawingObject Drawing(OdfDocument document, string name)
    {
        return document.DrawingObjects.Single(item => item.Name == name);
    }

    static string Write(OdfDocument document, bool preserve)
    {
        byte[] bytes = new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true,
                PreserveLexicalValues = preserve
            });
        return Encoding.UTF8.GetString(bytes);
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "geometry.fodg",
            new OdfLoadOptions());
    }

    static string GeometryXml()
    {
        return FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<draw:path draw:name='Path' svg:d='" + PathText() + "' "
            + "svg:viewBox=' -10&#x9;+20 00100&#xA;200 ' "
            + "draw:transform='" + TransformText() + "'/>"
            + "<draw:polygon draw:name='Polygon' "
            + "draw:points='0,0 10,0 10,10' svg:viewBox='0 0 10 10'/>"
            + "<draw:frame draw:name='Frame'>"
            + "<draw:image/>"
            + "<draw:contour-polygon svg:width='02.00cm' svg:height='1cm' "
            + "svg:viewBox='0 0 100 100' "
            + "draw:points='0,0 100,0 100,100 0,100' "
            + "draw:recreate-on-edit='true'/>"
            + "</draw:frame>"
            + "<draw:frame draw:name='Path Frame'>"
            + "<draw:image/>"
            + "<draw:contour-path svg:width='1cm' svg:height='1cm' "
            + "svg:viewBox='0 0 100 100' "
            + "svg:d='M0 0L100 0L100 100L0 100z' "
            + "draw:recreate-on-edit='false'/>"
            + "</draw:frame>"
            + "<draw:custom-shape draw:name='Custom'>"
            + "<draw:enhanced-geometry svg:viewBox='0 0 1000 0500'/>"
            + "</draw:custom-shape>"
            + "</draw:page></office:drawing></office:body>");
    }

    static string InvalidGeometryXml()
    {
        return FlatXml(
            "<office:body><office:drawing>"
            + "<draw:page draw:name='Page'>"
            + "<draw:path draw:name='Invalid' svg:d='L 1 nope' "
            + "draw:points='0,0 1,bad' svg:viewBox='0 0 10' "
            + "draw:transform='translate(2)'/>"
            + "</draw:page></office:drawing></office:body>");
    }

    static string PathText()
    {
        return "M0,0L10.5-2e1h5v6C1 2 3 4 5 6S7 8 9 10"
            + "Q11 12 13 14T15 16A5 6 45 0 1 20 30z";
    }

    static string TransformText()
    {
        return "translate(01.00cm,2.500cm), rotate(+0.50) "
            + "scale(2 3) skewX(-.25) matrix(1,0,0,1,4,5)";
    }

    static string FlatXml(string body)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document"
            + " xmlns:office='" + OdfNamespaces.Office + "'"
            + " xmlns:draw='" + OdfNamespaces.Drawing + "'"
            + " xmlns:svg='" + OdfNamespaces.Svg + "'"
            + " office:version='1.4'"
            + " office:mimetype='" + OdfMediaTypes.Graphics + "'>"
            + body
            + "</office:document>";
    }
}
