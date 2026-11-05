using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class BaseShapeXmlTests
{
    [Test]
    public void ShapeMetadata_RoundTripsNonVisualLocksVisibilityAltTextTransformAndHyperlink()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));
        shape.SetName("Metadata Shape");
        shape.SetAlternativeText("Describes the shape");
        shape.SetTitle("Shape title");
        shape.SetVisible(false);
        shape.SetLocks(true, true, true, true);
        PptxTransform transform = new PptxTransform(new PptxBounds(100L, 200L, 300L, 400L));
        transform.SetRotationDegrees(30.0);
        transform.FlipHorizontal = true;
        shape.SetTransform(transform);
        shape.SetHyperlink("https://example.com/shape");

        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        PptxShape? reopenedShape = reopened.GetSlide(0).FindShapeByName("Metadata Shape");
        Assert.That(reopenedShape, Is.Not.Null);
        Assert.That(reopenedShape!.AlternativeText, Is.EqualTo("Describes the shape"));
        Assert.That(reopenedShape.Title, Is.EqualTo("Shape title"));
        Assert.That(reopenedShape.Hidden, Is.True);
        Assert.That(reopenedShape.IsVisible(), Is.False);
        Assert.That(reopenedShape.NoMove, Is.True);
        Assert.That(reopenedShape.NoResize, Is.True);
        Assert.That(reopenedShape.NoRotate, Is.True);
        Assert.That(reopenedShape.NoSelect, Is.True);
        Assert.That(reopenedShape.GetLock("noRotate"), Is.True);
        Assert.That(reopenedShape.GetTransform().RotationDegrees(), Is.EqualTo(30.0).Within(0.000001));
        Assert.That(reopenedShape.GetTransform().FlipHorizontal, Is.True);
        Assert.That(reopenedShape.GetTransform().Bounds().Width, Is.EqualTo(300L));
        Assert.That(reopenedShape.HyperlinkRelationshipId, Is.Not.Empty);
        Assert.That(reopenedShape.HyperlinkTarget, Is.EqualTo("https://example.com/shape"));
        Assert.That(FindHyperlinkRelationship(reopened.GetSlide(0).Part.Relationships, reopenedShape.HyperlinkRelationshipId).Target, Is.EqualTo("https://example.com/shape"));
        Assert.That(reopened.Package.Validate().Succeeded(), Is.True, reopened.Package.Validate().FormatDiagnostics());
    }

    [Test]
    public void ShapeHyperlink_UpdateAndRemoveReusesThenDeletesRelationship()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));

        shape.SetHyperlink("https://example.com/one");
        string relationshipId = shape.HyperlinkRelationshipId;
        shape.SetHyperlink("https://example.com/two");

        Assert.That(shape.HyperlinkRelationshipId, Is.EqualTo(relationshipId));
        Assert.That(CountHyperlinkRelationships(shape.Tree.Slide.Part.Relationships), Is.EqualTo(1));
        Assert.That(FindHyperlinkRelationship(shape.Tree.Slide.Part.Relationships, relationshipId).Target, Is.EqualTo("https://example.com/two"));

        shape.RemoveHyperlink();
        PptxPresentation reopened = SaveReopenAndLoad(presentation);

        Assert.That(shape.HyperlinkRelationshipId, Is.Empty);
        Assert.That(shape.HyperlinkTarget, Is.Empty);
        Assert.That(CountHyperlinkRelationships(reopened.GetSlide(0).Part.Relationships), Is.EqualTo(0));
        Assert.That(reopened.GetSlide(0).GetShapeTree().Get(0).HyperlinkRelationshipId, Is.Empty);
    }

    [Test]
    public void ShapeHyperlink_RejectsInternalTargets()
    {
        PptxPresentation presentation = LoadGeneratedPresentation();
        PptxShape shape = presentation.GetSlide(0).AddShape(PptxShapeType.Rectangle, new PptxBounds(10L, 20L, 30L, 40L));

        Assert.Throws<System.ArgumentException>(() => shape.SetHyperlink("../slides/slide2.xml"));
    }

    static PptxRelationship FindHyperlinkRelationship(PptxRelationshipCollection relationships, string relationshipId)
    {
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            PptxRelationship relationship = relationships.Relationships[index];
            if (relationship.Id == relationshipId && relationship.Type == PptxRelationshipTypes.Hyperlink)
            {
                return relationship;
            }
        }

        throw new System.ArgumentException("Hyperlink relationship was not found.");
    }

    static int CountHyperlinkRelationships(PptxRelationshipCollection relationships)
    {
        int count = 0;
        for (int index = 0; index < relationships.Relationships.Count; index++)
        {
            if (relationships.Relationships[index].Type == PptxRelationshipTypes.Hyperlink)
            {
                count = count + 1;
            }
        }

        return count;
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
