using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class GeometryApiCompletionTests
{
    [Test]
    public void Bounds_MarginsInsetAndExpandRoundTrip()
    {
        PptxBoundsOperations operations = new PptxBoundsOperations();
        PptxBounds original = new PptxBounds(100L, 200L, 1000L, 500L);
        PptxMargins margins = new PptxMargins(10L, 20L, 30L, 40L);

        PptxBounds inset = operations.Inset(original, margins);
        PptxBounds expanded = operations.Expand(inset, margins);

        AssertBounds(inset, 110L, 220L, 960L, 440L);
        AssertBounds(expanded, 100L, 200L, 1000L, 500L);
        Assert.Throws<System.ArgumentException>(() => operations.Inset(new PptxBounds(0L, 0L, 10L, 10L), margins));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new PptxMargins(-1L, 0L, 0L, 0L));
    }

    [Test]
    public void Transform_AnchorBoundsAndPointMappingApplyRotationAndFlips()
    {
        PptxTransform transform = new PptxTransform(new PptxBounds(0L, 0L, 200L, 100L));
        transform.SetRotationDegrees(90.0);

        AssertBounds(transform.AnchorBounds(), 50L, -50L, 100L, 200L);

        PptxTransform group = new PptxTransform(new PptxBounds(100L, 200L, 1000L, 500L));
        group.SetChildBounds(new PptxBounds(0L, 0L, 100L, 50L));
        group.FlipHorizontal = true;
        group.SetRotationDegrees(90.0);

        PptxPoint mapped = group.MapChildPoint(new PptxPoint(0L, 0L));
        Assert.That(mapped.X, Is.EqualTo(850L));
        Assert.That(mapped.Y, Is.EqualTo(950L));
    }

    [Test]
    public void Shape_RotationFlipsAndAnchorBoundsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 200L, 100L));

        shape.SetRotationDegrees(90.0);
        shape.SetFlips(true, false);
        AssertBounds(shape.GetAnchorBounds(), 50L, -50L, 100L, 200L);

        PptxShape reopened = SaveReopenAndLoad(presentation).GetSlide(0).GetShapeTree().Get(0);
        Assert.That(reopened.GetRotationDegrees(), Is.EqualTo(90.0).Within(0.000001));
        Assert.That(reopened.GetTransform().FlipHorizontal, Is.True);
        Assert.That(reopened.GetTransform().FlipVertical, Is.False);
        AssertBounds(reopened.GetEffectiveAnchorBounds(), 50L, -50L, 100L, 200L);
    }

    [Test]
    public void PresetAdjustments_AddUpdateRemoveAndRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 200L, 100L));

        shape.SetPresetAdjustment("adj", 25000);
        shape.SetPresetAdjustment("adj2", -5000);
        shape.SetPresetAdjustment("adj", 30000);
        Assert.That(shape.RemovePresetAdjustment("adj2"), Is.True);
        Assert.That(shape.RemovePresetAdjustment("missing"), Is.False);

        PptxPresentation reopenedPresentation = SaveReopenAndLoad(presentation);
        PptxShape reopened = reopenedPresentation.GetSlide(0).GetShapeTree().Get(0);
        List<PptxGeometryAdjustment> adjustments = reopened.GetPresetAdjustments();

        Assert.That(adjustments.Count, Is.EqualTo(1));
        Assert.That(adjustments[0].Name, Is.EqualTo("adj"));
        Assert.That(adjustments[0].Value, Is.EqualTo(30000));
        Assert.That(reopened.Element.OuterXml, Does.Contain("fmla=\"val 30000\""));
        Assert.That(reopenedPresentation.Package.Validate().Succeeded(), Is.True, reopenedPresentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void FreeformPaths_PreserveMultipleLineAndBezierCommands()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        List<PptxPoint> initial = new List<PptxPoint>();
        initial.Add(new PptxPoint(0L, 0L));
        initial.Add(new PptxPoint(100L, 100L));
        PptxShape shape = presentation.GetSlide(0).AddFreeform(new PptxBounds(10L, 20L, 300L, 200L), initial);
        List<PptxFreeformPath> paths = CreateFreeformPaths();

        shape.SetFreeformPaths(paths);
        PptxPresentation reopenedPresentation = SaveReopenAndLoad(presentation);
        PptxShape reopened = reopenedPresentation.GetSlide(0).GetShapeTree().Get(0);
        List<PptxFreeformPath> reopenedPaths = reopened.GetFreeformPaths();

        Assert.That(reopenedPaths.Count, Is.EqualTo(2));
        Assert.That(reopenedPaths[0].Commands.Count, Is.EqualTo(3));
        Assert.That(reopenedPaths[0].Commands[1].Type, Is.EqualTo(PptxFreeformCommandType.CubicBezierTo));
        Assert.That(reopenedPaths[0].Commands[1].Points.Count, Is.EqualTo(3));
        Assert.That(reopenedPaths[1].Commands[1].Type, Is.EqualTo(PptxFreeformCommandType.QuadraticBezierTo));
        Assert.That(reopened.GetCustomGeometryXml(), Does.Contain("cubicBezTo"));
        Assert.That(reopened.GetCustomGeometryXml(), Does.Contain("quadBezTo"));
        Assert.That(reopenedPresentation.Package.Validate().Succeeded(), Is.True, reopenedPresentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void FreeformPath_ReportsTheInvalidDimensionParameter()
    {
        System.ArgumentOutOfRangeException width = Assert.Throws<System.ArgumentOutOfRangeException>(() => new PptxFreeformPath(0L, 100L))!;
        System.ArgumentOutOfRangeException height = Assert.Throws<System.ArgumentOutOfRangeException>(() => new PptxFreeformPath(100L, 0L))!;

        Assert.That(width.ParamName, Is.EqualTo("width"));
        Assert.That(height.ParamName, Is.EqualTo("height"));
    }

    [Test]
    public void ConnectorConnections_PreserveShapeAndSiteIdsWithEndpointPositions()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShape first = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 100L, 100L));
        PptxShape second = slide.AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(300L, 0L, 100L, 100L));
        PptxShape connector = slide.AddConnector(PptxConnector.Straight, new PptxPoint(100L, 50L), new PptxPoint(300L, 50L));
        PptxConnectionPoint start = new PptxConnectionPoint(new PptxPoint(100L, 50L));
        start.Connected = true;
        start.ShapeId = first.Id;
        start.SiteIndex = 2;
        PptxConnectionPoint end = new PptxConnectionPoint(new PptxPoint(300L, 50L));
        end.Connected = true;
        end.ShapeId = second.Id;
        end.SiteIndex = 0;

        connector.SetConnectorConnections(start, end);
        PptxPresentation reopenedPresentation = SaveReopenAndLoad(presentation);
        PptxShape reopened = reopenedPresentation.GetSlide(0).GetShapeTree().Get(2);
        PptxConnectionPoint reopenedStart = reopened.GetConnectorStartConnection();
        PptxConnectionPoint reopenedEnd = reopened.GetConnectorEndConnection();

        Assert.That(reopenedStart.Connected, Is.True);
        Assert.That(reopenedStart.ShapeId, Is.EqualTo(first.Id));
        Assert.That(reopenedStart.SiteIndex, Is.EqualTo(2));
        Assert.That(reopenedStart.Position.X, Is.EqualTo(100L));
        Assert.That(reopenedEnd.ShapeId, Is.EqualTo(second.Id));
        Assert.That(reopenedEnd.Position.X, Is.EqualTo(300L));
        Assert.That(reopened.Element.OuterXml, Does.Contain("stCxn"));
        Assert.That(reopened.Element.OuterXml, Does.Contain("endCxn"));
        Assert.That(reopenedPresentation.Package.Validate().Succeeded(), Is.True, reopenedPresentation.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void PictureCropRectangle_ValidatesPercentagesAndCalculatesVisibleBounds()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape picture = presentation.GetSlide(0).AddImage(CreatePngBytes(), new PptxBounds(0L, 0L, 1000L, 500L));
        PptxImageCrop crop = new PptxImageCrop();
        crop.Left = 10000;
        crop.Top = 20000;
        crop.Right = 30000;
        crop.Bottom = 10000;

        picture.SetImageCropRectangle(crop);
        AssertBounds(crop.VisibleBounds(picture.GetBounds()), 100L, 100L, 600L, 350L);

        PptxShape reopened = SaveReopenAndLoad(presentation).GetSlide(0).GetShapeTree().Get(0);
        Assert.That(reopened.GetImageCrop().Left, Is.EqualTo(10000));
        Assert.That(reopened.GetImageCrop().Right, Is.EqualTo(30000));

        PptxImageCrop invalid = new PptxImageCrop();
        invalid.Left = 60000;
        invalid.Right = 50000;
        Assert.That(invalid.IsValid(), Is.False);
        Assert.Throws<System.ArgumentException>(() => picture.SetImageCropRectangle(invalid));
    }

    static List<PptxFreeformPath> CreateFreeformPaths()
    {
        List<PptxFreeformPath> paths = new List<PptxFreeformPath>();
        PptxFreeformPath first = new PptxFreeformPath(300L, 200L);
        PptxFreeformCommand move = new PptxFreeformCommand(PptxFreeformCommandType.MoveTo);
        move.Points.Add(new PptxPoint(0L, 0L));
        first.Commands.Add(move);
        PptxFreeformCommand cubic = new PptxFreeformCommand(PptxFreeformCommandType.CubicBezierTo);
        cubic.Points.Add(new PptxPoint(50L, 0L));
        cubic.Points.Add(new PptxPoint(250L, 200L));
        cubic.Points.Add(new PptxPoint(300L, 200L));
        first.Commands.Add(cubic);
        first.Commands.Add(new PptxFreeformCommand(PptxFreeformCommandType.Close));
        paths.Add(first);

        PptxFreeformPath second = new PptxFreeformPath(300L, 200L);
        PptxFreeformCommand secondMove = new PptxFreeformCommand(PptxFreeformCommandType.MoveTo);
        secondMove.Points.Add(new PptxPoint(0L, 200L));
        second.Commands.Add(secondMove);
        PptxFreeformCommand quadratic = new PptxFreeformCommand(PptxFreeformCommandType.QuadraticBezierTo);
        quadratic.Points.Add(new PptxPoint(150L, 0L));
        quadratic.Points.Add(new PptxPoint(300L, 200L));
        second.Commands.Add(quadratic);
        paths.Add(second);
        return paths;
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        PptxDocument reopened = PptxDocument.OpenBytes(document.SaveBytes());
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        PptxDocument reopened = PptxDocument.OpenBytes(document.SaveBytes());
        return reopened.LoadPresentation();
    }

    static byte[] CreatePngBytes()
    {
        byte[] bytes = new byte[24];
        bytes[0] = 137;
        bytes[1] = 80;
        bytes[2] = 78;
        bytes[3] = 71;
        bytes[4] = 13;
        bytes[5] = 10;
        bytes[6] = 26;
        bytes[7] = 10;
        bytes[12] = 73;
        bytes[13] = 72;
        bytes[14] = 68;
        bytes[15] = 82;
        bytes[19] = 100;
        bytes[23] = 50;
        return bytes;
    }

    static void AssertBounds(PptxBounds bounds, long x, long y, long width, long height)
    {
        Assert.That(bounds.X, Is.EqualTo(x));
        Assert.That(bounds.Y, Is.EqualTo(y));
        Assert.That(bounds.Width, Is.EqualTo(width));
        Assert.That(bounds.Height, Is.EqualTo(height));
    }
}
