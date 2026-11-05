using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class ShapeTreeTests
{
    [Test]
    public void ShapeTree_AddsEnumeratesAndFindsShapes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxSlide slide = presentation.GetSlide(0);
        PptxShapeTree tree = slide.GetShapeTree();

        PptxShape rectangle = tree.AddShape(PptxShapeType.Rectangle, PptxBounds.FromInches(1.0, 1.0, 2.0, 1.0));
        PptxShape ellipse = tree.AddShape(PptxShapeType.Ellipse, new PptxBounds(10L, 20L, 30L, 40L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(tree.Count(), Is.EqualTo(2));
        Assert.That(rectangle.Id, Is.EqualTo(2));
        Assert.That(ellipse.Id, Is.EqualTo(3));
        Assert.That(rectangle.Name, Is.EqualTo("rect 2"));
        Assert.That(ellipse.PresetGeometry, Is.EqualTo("ellipse"));
        Assert.That(tree.FindById(2), Is.SameAs(rectangle));
        Assert.That(tree.FindByName("ellipse 3"), Is.SameAs(ellipse));

        PptxShapeTree reopenedTree = reopened.GetSlide(0).GetShapeTree();
        Assert.That(reopenedTree.Count(), Is.EqualTo(2));
        Assert.That(reopenedTree.Get(0).Id, Is.EqualTo(2));
        Assert.That(reopenedTree.Get(0).GetBounds().X, Is.EqualTo(914400L));
        Assert.That(reopenedTree.Get(0).PresetGeometry, Is.EqualTo("rect"));
        Assert.That(reopenedTree.FindByName("ellipse 3"), Is.Not.Null);
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void Shape_SetNameAndBoundsRoundTrip()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddShape(PptxShapeType.Rectangle, new PptxBounds(1L, 2L, 3L, 4L));

        shape.SetName("Renamed");
        shape.SetBounds(new PptxBounds(100L, 200L, 300L, 400L));
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShape? reopenedShape = reopened.GetSlide(0).FindShapeByName("Renamed");
        Assert.That(reopenedShape, Is.Not.Null);
        Assert.That(reopenedShape!.Id, Is.EqualTo(2));
        Assert.That(reopenedShape.GetBounds().X, Is.EqualTo(100L));
        Assert.That(reopenedShape.GetBounds().Y, Is.EqualTo(200L));
        Assert.That(reopenedShape.GetBounds().Width, Is.EqualTo(300L));
        Assert.That(reopenedShape.GetBounds().Height, Is.EqualTo(400L));
    }

    [Test]
    public void ShapeTree_MoveReordersShapes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        tree.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 10L, 10L)).SetName("A");
        tree.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 10L, 10L)).SetName("B");
        tree.AddShape(PptxShapeType.Rectangle, new PptxBounds(0L, 0L, 10L, 10L)).SetName("C");

        tree.Move(0, 2);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShapeTree reopenedTree = reopened.GetSlide(0).GetShapeTree();
        Assert.That(reopenedTree.Get(0).Name, Is.EqualTo("B"));
        Assert.That(reopenedTree.Get(1).Name, Is.EqualTo("C"));
        Assert.That(reopenedTree.Get(2).Name, Is.EqualTo("A"));
    }

    [Test]
    public void ShapeTree_CloneAndRemoveShapes()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();
        PptxShape original = tree.AddShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));
        original.SetName("Original");

        PptxShape clone = original.Clone();
        bool removed = tree.RemoveById(original.Id);
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(clone.Id, Is.EqualTo(3));
        Assert.That(clone.Name, Is.EqualTo("Original Copy"));
        Assert.That(removed, Is.True);
        Assert.That(tree.Count(), Is.EqualTo(1));
        Assert.That(tree.FindByName("Original"), Is.Null);
        Assert.That(reopened.GetSlide(0).GetShapeTree().Count(), Is.EqualTo(1));
        Assert.That(reopened.GetSlide(0).FindShapeByName("Original Copy"), Is.Not.Null);
    }

    [Test]
    public void ShapeTree_RemoveByIdReturnsFalseWhenMissing()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();

        Assert.That(tree.RemoveById(99), Is.False);
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
