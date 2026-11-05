using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class AutoShapeTests
{
    [Test]
    public void ShapeType_RecognizesCommonPresetGeometriesAndAliases()
    {
        Assert.That(PptxShapeTypeCore.NormalizePresetGeometry("rectangle"), Is.EqualTo(PptxShapeType.Rectangle));
        Assert.That(PptxShapeTypeCore.NormalizePresetGeometry("rightTriangle"), Is.EqualTo(PptxShapeType.RightTriangle));
        Assert.That(PptxShapeTypeCore.NormalizePresetGeometry(PptxShapeType.TextBox), Is.EqualTo(PptxShapeType.Rectangle));
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Rectangle), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.RoundRectangle), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Ellipse), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Triangle), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.RightTriangle), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Diamond), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Pentagon), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Hexagon), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Heptagon), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Octagon), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Plus), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Star5), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Heart), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Cloud), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Cube), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Can), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.Donut), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.BlockArc), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.FoldedCorner), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry(PptxShapeType.SmileyFace), Is.True);
        Assert.That(PptxShapeTypeCore.IsCommonPresetGeometry("unknownPreset"), Is.False);
    }

    [Test]
    public void AddAutoShape_CreatesCommonPresetGeometries()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);

        slide.AddAutoShape(PptxShapeType.RoundRectangle, new PptxBounds(0L, 0L, 100L, 100L));
        slide.AddAutoShape(PptxShapeType.Ellipse, new PptxBounds(100L, 0L, 100L, 100L));
        slide.AddAutoShape(PptxShapeType.Diamond, new PptxBounds(200L, 0L, 100L, 100L));
        slide.AddAutoShape(PptxShapeType.Star5, new PptxBounds(300L, 0L, 100L, 100L));
        slide.AddAutoShape(PptxShapeType.Cloud, new PptxBounds(400L, 0L, 100L, 100L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShapeTree tree = reopened.GetSlide(0).GetShapeTree();
        Assert.That(tree.Count(), Is.EqualTo(5));
        Assert.That(tree.Get(0).PresetGeometry, Is.EqualTo(PptxShapeType.RoundRectangle));
        Assert.That(tree.Get(1).PresetGeometry, Is.EqualTo(PptxShapeType.Ellipse));
        Assert.That(tree.Get(2).PresetGeometry, Is.EqualTo(PptxShapeType.Diamond));
        Assert.That(tree.Get(3).PresetGeometry, Is.EqualTo(PptxShapeType.Star5));
        Assert.That(tree.Get(4).PresetGeometry, Is.EqualTo(PptxShapeType.Cloud));
        Assert.That(tree.Get(3).IsAutoShape(), Is.True);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetPresetGeometryEditsExistingAutoShapeAndRoundTrips()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));

        shape.SetPresetGeometry(PptxShapeType.Heart);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShape reopenedShape = reopened.GetSlide(0).GetShapeTree().Get(0);
        Assert.That(shape.GetPresetGeometry(), Is.EqualTo(PptxShapeType.Heart));
        Assert.That(reopenedShape.GetPresetGeometry(), Is.EqualTo(PptxShapeType.Heart));
        Assert.That(reopenedShape.GetBounds().X, Is.EqualTo(10L));
        Assert.That(reopenedShape.GetBounds().Width, Is.EqualTo(30L));
    }

    [Test]
    public void Shape_SetPresetGeometryNormalizesAliases()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddAutoShape("", new PptxBounds(10L, 20L, 30L, 40L));

        shape.SetPresetGeometry("rightTriangle");

        Assert.That(shape.GetPresetGeometry(), Is.EqualTo(PptxShapeType.RightTriangle));
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
