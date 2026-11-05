using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ConnectorTests
{
    [Test]
    public void Connector_NormalizesTypesAliasesAndPresets()
    {
        Assert.That(PptxConnectorCore.NormalizeConnectorType(""), Is.EqualTo(PptxConnector.Straight));
        Assert.That(PptxConnectorCore.NormalizeConnectorType("line"), Is.EqualTo(PptxConnector.Straight));
        Assert.That(PptxConnectorCore.NormalizeConnectorType(PptxConnector.StraightPreset), Is.EqualTo(PptxConnector.Straight));
        Assert.That(PptxConnectorCore.NormalizeConnectorType("bent"), Is.EqualTo(PptxConnector.Elbow));
        Assert.That(PptxConnectorCore.NormalizeConnectorType(PptxConnector.ElbowPreset), Is.EqualTo(PptxConnector.Elbow));
        Assert.That(PptxConnectorCore.NormalizeConnectorType("curve"), Is.EqualTo(PptxConnector.Curved));
        Assert.That(PptxConnectorCore.NormalizeConnectorType(PptxConnector.CurvedPreset), Is.EqualTo(PptxConnector.Curved));
        Assert.That(PptxConnectorCore.GetPresetGeometry(PptxConnector.Straight), Is.EqualTo(PptxConnector.StraightPreset));
        Assert.That(PptxConnectorCore.GetPresetGeometry(PptxConnector.Elbow), Is.EqualTo(PptxConnector.ElbowPreset));
        Assert.That(PptxConnectorCore.GetPresetGeometry(PptxConnector.Curved), Is.EqualTo(PptxConnector.CurvedPreset));
        Assert.That(PptxConnectorCore.IsConnectorPreset(PptxConnector.CurvedPreset), Is.True);
        Assert.That(PptxConnectorCore.IsConnectorPreset("rectangle"), Is.False);
    }

    [Test]
    public void AddConnector_CreatesStraightElbowAndCurvedConnectors()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.AddConnector(PptxConnector.Straight, new PptxPoint(10L, 20L), new PptxPoint(110L, 120L));
        slide.AddConnector(PptxConnector.Elbow, new PptxPoint(200L, 210L), new PptxPoint(260L, 300L));
        slide.AddConnector(PptxConnector.Curved, new PptxPoint(400L, 420L), new PptxPoint(520L, 460L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShapeTree tree = reopened.GetSlide(0).GetShapeTree();
        Assert.That(tree.Count(), Is.EqualTo(3));
        AssertConnector(tree.Get(0), PptxConnector.Straight, PptxConnector.StraightPreset, 10L, 20L, 110L, 120L);
        AssertConnector(tree.Get(1), PptxConnector.Elbow, PptxConnector.ElbowPreset, 200L, 210L, 260L, 300L);
        AssertConnector(tree.Get(2), PptxConnector.Curved, PptxConnector.CurvedPreset, 400L, 420L, 520L, 460L);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Connector_SetTypeAndEndsEditsExistingConnectorAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddConnector(PptxConnector.Straight, new PptxPoint(10L, 20L), new PptxPoint(110L, 120L));

        shape.SetConnectorType(PptxConnector.Curved);
        shape.SetConnectorEnds(new PptxPoint(250L, 160L), new PptxPoint(50L, 60L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        AssertConnector(reopenedShape, PptxConnector.Curved, PptxConnector.CurvedPreset, 250L, 160L, 50L, 60L);
        Assert.That(reopenedShape.GetBounds().X, Is.EqualTo(50L));
        Assert.That(reopenedShape.GetBounds().Y, Is.EqualTo(60L));
        Assert.That(reopenedShape.GetBounds().Width, Is.EqualTo(200L));
        Assert.That(reopenedShape.GetBounds().Height, Is.EqualTo(100L));
        Assert.That(reopenedShape.GetTransform().FlipHorizontal, Is.True);
        Assert.That(reopenedShape.GetTransform().FlipVertical, Is.True);
    }

    [Test]
    public void ConnectorEditing_RejectsNonConnectorShapes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));

        Assert.Throws<System.ArgumentException>(() => shape.SetConnectorType(PptxConnector.Elbow));
        Assert.Throws<System.ArgumentException>(() => shape.SetConnectorEndCoordinates(0L, 0L, 100L, 100L));
    }

    static void AssertConnector(PptxShape shape, string connectorType, string presetGeometry, long startX, long startY, long endX, long endY)
    {
        Assert.That(shape.ElementType, Is.EqualTo("cxnSp"));
        Assert.That(shape.IsConnector(), Is.True);
        Assert.That(shape.GetConnectorType(), Is.EqualTo(connectorType));
        Assert.That(shape.ConnectorType, Is.EqualTo(connectorType));
        Assert.That(shape.GetPresetGeometry(), Is.EqualTo(presetGeometry));
        Assert.That(shape.Start.X, Is.EqualTo(startX));
        Assert.That(shape.Start.Y, Is.EqualTo(startY));
        Assert.That(shape.End.X, Is.EqualTo(endX));
        Assert.That(shape.End.Y, Is.EqualTo(endY));
    }

    static PptxPresentation LoadGeneratedPresentation()
    {
        PptxDocument document = PptxDocument.CreateMinimumPresentation();
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }

    static PptxPresentation SaveReopenAndLoad(PptxPresentation presentation)
    {
        PptxDocument document = new PptxDocument(presentation.Package);
        byte[] bytes = document.SaveBytes();
        PptxDocument reopened = PptxDocument.OpenBytes(bytes);
        return reopened.LoadPresentation();
    }
}
