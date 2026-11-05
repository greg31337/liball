using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxTransformTests
{
    [Test]
    public void CreateDefault_ReturnsZeroBoundsAndDefaultFlags()
    {
        PptxTransform transform = PptxTransform.CreateDefault();

        Assert.That(transform.Bounds().IsEmpty(), Is.True);
        Assert.That(transform.X, Is.EqualTo(0L));
        Assert.That(transform.Y, Is.EqualTo(0L));
        Assert.That(transform.Width, Is.EqualTo(0L));
        Assert.That(transform.Height, Is.EqualTo(0L));
        Assert.That(transform.HasChildBounds(), Is.False);
        Assert.That(transform.RotationAngleUnits, Is.EqualTo(0));
        Assert.That(transform.FlipHorizontal, Is.False);
        Assert.That(transform.FlipVertical, Is.False);
    }

    [Test]
    public void BoundsHelpersComputeEdgesIntersectionUnionOffsetAndScale()
    {
        PptxBounds bounds = new PptxBounds(10L, 20L, 100L, 50L);
        PptxBounds overlapping = new PptxBounds(90L, 60L, 20L, 20L);
        PptxBounds separate = new PptxBounds(200L, 200L, 10L, 10L);

        Assert.That(bounds.Right(), Is.EqualTo(110L));
        Assert.That(bounds.Bottom(), Is.EqualTo(70L));
        Assert.That(bounds.Contains(110L, 70L), Is.True);
        Assert.That(bounds.Intersects(overlapping), Is.True);
        Assert.That(bounds.Intersects(separate), Is.False);

        PptxBounds union = bounds.Union(new PptxBounds(0L, 10L, 20L, 20L));
        Assert.That(union.X, Is.EqualTo(0L));
        Assert.That(union.Y, Is.EqualTo(10L));
        Assert.That(union.Width, Is.EqualTo(110L));
        Assert.That(union.Height, Is.EqualTo(60L));

        PptxBounds offset = bounds.Offset(5L, -10L);
        Assert.That(offset.X, Is.EqualTo(15L));
        Assert.That(offset.Y, Is.EqualTo(10L));

        PptxBounds scaled = bounds.Scale(0.5, 2.0);
        Assert.That(scaled.X, Is.EqualTo(5L));
        Assert.That(scaled.Y, Is.EqualTo(40L));
        Assert.That(scaled.Width, Is.EqualTo(50L));
        Assert.That(scaled.Height, Is.EqualTo(100L));
    }

    [Test]
    public void Transform_FromShapeReadsBoundsRotationFlipsAndChildBounds()
    {
        XmlDocument document = PptxXml.LoadString(CreateGroupShapeXml());
        XmlElement shape = document.DocumentElement!;

        PptxTransform transform = PptxTransform.FromShape(shape);

        Assert.That(transform.X, Is.EqualTo(100L));
        Assert.That(transform.Y, Is.EqualTo(200L));
        Assert.That(transform.Width, Is.EqualTo(1000L));
        Assert.That(transform.Height, Is.EqualTo(500L));
        Assert.That(transform.ChildX, Is.EqualTo(10L));
        Assert.That(transform.ChildY, Is.EqualTo(20L));
        Assert.That(transform.ChildWidth, Is.EqualTo(100L));
        Assert.That(transform.ChildHeight, Is.EqualTo(50L));
        Assert.That(transform.RotationAngleUnits, Is.EqualTo(5400000));
        Assert.That(transform.RotationDegrees(), Is.EqualTo(90.0).Within(0.000001));
        Assert.That(transform.FlipHorizontal, Is.True);
        Assert.That(transform.FlipVertical, Is.False);
        Assert.That(transform.HasChildBounds(), Is.True);
    }

    [Test]
    public void Transform_ApplyToShapeWritesTransformXml()
    {
        XmlDocument document = PptxXml.LoadString(CreateShapeXml());
        XmlElement shape = document.DocumentElement!;
        PptxTransform transform = new PptxTransform(PptxBounds.FromInches(1.0, 0.5, 2.0, 1.0));
        transform.SetRotationDegrees(45.0);
        transform.FlipVertical = true;

        transform.ApplyToShape(shape);

        PptxTransform written = PptxTransform.FromShape(shape);
        Assert.That(written.X, Is.EqualTo(914400L));
        Assert.That(written.Y, Is.EqualTo(457200L));
        Assert.That(written.Width, Is.EqualTo(1828800L));
        Assert.That(written.Height, Is.EqualTo(914400L));
        Assert.That(written.RotationAngleUnits, Is.EqualTo(2700000));
        Assert.That(written.FlipHorizontal, Is.False);
        Assert.That(written.FlipVertical, Is.True);
    }

    [Test]
    public void Transform_MapChildBoundsScalesGroupCoordinatesToParentBounds()
    {
        PptxTransform transform = new PptxTransform(new PptxBounds(100L, 200L, 1000L, 500L));
        transform.SetChildBounds(new PptxBounds(10L, 20L, 100L, 50L));

        PptxBounds mapped = transform.MapChildBounds(new PptxBounds(60L, 45L, 20L, 10L));

        Assert.That(mapped.X, Is.EqualTo(600L));
        Assert.That(mapped.Y, Is.EqualTo(450L));
        Assert.That(mapped.Width, Is.EqualTo(200L));
        Assert.That(mapped.Height, Is.EqualTo(100L));
    }

    [Test]
    public void Transform_MapChildBoundsOffsetsWhenGroupHasNoChildExtent()
    {
        PptxTransform transform = new PptxTransform(new PptxBounds(100L, 200L, 1000L, 500L));

        PptxBounds mapped = transform.MapChildBounds(new PptxBounds(5L, 6L, 7L, 8L));

        Assert.That(mapped.X, Is.EqualTo(105L));
        Assert.That(mapped.Y, Is.EqualTo(206L));
        Assert.That(mapped.Width, Is.EqualTo(7L));
        Assert.That(mapped.Height, Is.EqualTo(8L));
    }

    static string CreateShapeXml()
    {
        return
            "<p:sp xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:a=\"" + PptxNamespaces.Drawing + "\">" +
            "<p:nvSpPr><p:cNvPr id=\"2\" name=\"Shape\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr/>" +
            "</p:sp>";
    }

    static string CreateGroupShapeXml()
    {
        return
            "<p:grpSp xmlns:p=\"" + PptxNamespaces.Presentation + "\" xmlns:a=\"" + PptxNamespaces.Drawing + "\">" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr>" +
            "<a:xfrm rot=\"5400000\" flipH=\"1\" flipV=\"0\">" +
            "<a:off x=\"100\" y=\"200\"/>" +
            "<a:ext cx=\"1000\" cy=\"500\"/>" +
            "<a:chOff x=\"10\" y=\"20\"/>" +
            "<a:chExt cx=\"100\" cy=\"50\"/>" +
            "</a:xfrm>" +
            "</p:grpSpPr>" +
            "</p:grpSp>";
    }
}
