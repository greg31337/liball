using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class GroupShapeTests
{
    [Test]
    public void ShapeTree_LoadsGroupChildrenAndFindsNestedShapes()
    {
        PptxPresentation presentation = LoadGroupedPresentation();
        PptxShapeTree tree = presentation.GetSlide(0).GetShapeTree();

        Assert.That(tree.Count(), Is.EqualTo(1));
        Assert.That(tree.CountAll(), Is.EqualTo(4));

        PptxShape group = tree.Get(0);
        Assert.That(group.IsGroup(), Is.True);
        Assert.That(group.PresetGeometry, Is.Empty);
        Assert.That(group.ChildCount(), Is.EqualTo(2));
        Assert.That(group.AsGroup().Count(), Is.EqualTo(2));
        Assert.That(group.GetChild(0).Name, Is.EqualTo("Direct Rect"));
        Assert.That(group.GetChild(1).IsGroup(), Is.True);

        PptxShape? nestedShape = tree.FindByName("Nested Rect");
        Assert.That(nestedShape, Is.Not.Null);
        Assert.That(tree.FindById(5), Is.SameAs(nestedShape));
        Assert.That(group.AsGroup().FindByName("Nested Rect"), Is.SameAs(nestedShape));
    }

    [Test]
    public void Shape_GetEffectiveBoundsMapsDirectAndNestedGroupTransforms()
    {
        PptxPresentation presentation = LoadGroupedPresentation();
        PptxShape group = presentation.GetSlide(0).GetShapeTree().Get(0);
        PptxShape direct = group.GetChild(0);
        PptxShape nestedGroup = group.GetChild(1);
        PptxShape nested = nestedGroup.GetChild(0);

        AssertBounds(group.GetEffectiveBounds(), 100L, 200L, 1000L, 500L);
        AssertBounds(direct.GetBounds(), 60L, 45L, 20L, 10L);
        AssertBounds(direct.GetEffectiveBounds(), 600L, 450L, 200L, 100L);
        AssertBounds(nested.GetBounds(), 25L, 40L, 50L, 20L);
        AssertBounds(nested.GetEffectiveBounds(), 350L, 600L, 500L, 200L);
        AssertBounds(nested.GetEffectiveTransform().Bounds(), 350L, 600L, 500L, 200L);
    }

    [Test]
    public void Transform_MapChildBoundsHonorsGroupFlips()
    {
        PptxTransform transform = new PptxTransform(new PptxBounds(100L, 200L, 1000L, 500L));
        transform.SetChildBounds(new PptxBounds(10L, 20L, 100L, 50L));
        transform.FlipHorizontal = true;
        transform.FlipVertical = true;

        PptxBounds mapped = transform.MapChildBounds(new PptxBounds(60L, 45L, 20L, 10L));

        AssertBounds(mapped, 400L, 350L, 200L, 100L);
    }

    static void AssertBounds(PptxBounds bounds, long x, long y, long width, long height)
    {
        Assert.That(bounds.X, Is.EqualTo(x));
        Assert.That(bounds.Y, Is.EqualTo(y));
        Assert.That(bounds.Width, Is.EqualTo(width));
        Assert.That(bounds.Height, Is.EqualTo(height));
    }

    static PptxPresentation LoadGroupedPresentation()
    {
        PptxPackage package = PptxMinimumPresentation.CreatePackage();
        package.ReplacePart(PptxPartNames.FirstSlide, PptxContentTypes.Slide, Encoding.UTF8.GetBytes(CreateGroupedSlideXml()));
        PptxDocument document = PptxDocument.OpenBytes(package.SaveBytes());
        return document.LoadPresentation();
    }

    static string CreateGroupedSlideXml()
    {
        return
            "<p:sld xmlns:a=\"" + PptxNamespaces.Drawing + "\" xmlns:r=\"" + PptxNamespaces.Relationships + "\" xmlns:p=\"" + PptxNamespaces.Presentation + "\">" +
            "<p:cSld>" +
            "<p:spTree>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"1\" name=\"\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr><a:xfrm><a:off x=\"0\" y=\"0\"/><a:ext cx=\"0\" cy=\"0\"/><a:chOff x=\"0\" y=\"0\"/><a:chExt cx=\"0\" cy=\"0\"/></a:xfrm></p:grpSpPr>" +
            CreateGroupXml() +
            "</p:spTree>" +
            "</p:cSld>" +
            "<p:clrMapOvr><a:masterClrMapping/></p:clrMapOvr>" +
            "</p:sld>";
    }

    static string CreateGroupXml()
    {
        return
            "<p:grpSp>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"2\" name=\"Outer Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr>" +
            "<a:xfrm>" +
            "<a:off x=\"100\" y=\"200\"/>" +
            "<a:ext cx=\"1000\" cy=\"500\"/>" +
            "<a:chOff x=\"10\" y=\"20\"/>" +
            "<a:chExt cx=\"100\" cy=\"50\"/>" +
            "</a:xfrm>" +
            "</p:grpSpPr>" +
            CreateShapeXml("3", "Direct Rect", "60", "45", "20", "10") +
            CreateNestedGroupXml() +
            "</p:grpSp>";
    }

    static string CreateNestedGroupXml()
    {
        return
            "<p:grpSp>" +
            "<p:nvGrpSpPr><p:cNvPr id=\"4\" name=\"Nested Group\"/><p:cNvGrpSpPr/><p:nvPr/></p:nvGrpSpPr>" +
            "<p:grpSpPr>" +
            "<a:xfrm>" +
            "<a:off x=\"10\" y=\"20\"/>" +
            "<a:ext cx=\"100\" cy=\"100\"/>" +
            "<a:chOff x=\"0\" y=\"0\"/>" +
            "<a:chExt cx=\"100\" cy=\"100\"/>" +
            "</a:xfrm>" +
            "</p:grpSpPr>" +
            CreateShapeXml("5", "Nested Rect", "25", "40", "50", "20") +
            "</p:grpSp>";
    }

    static string CreateShapeXml(string id, string name, string x, string y, string width, string height)
    {
        return
            "<p:sp>" +
            "<p:nvSpPr><p:cNvPr id=\"" + id + "\" name=\"" + name + "\"/><p:cNvSpPr/><p:nvPr/></p:nvSpPr>" +
            "<p:spPr>" +
            "<a:xfrm><a:off x=\"" + x + "\" y=\"" + y + "\"/><a:ext cx=\"" + width + "\" cy=\"" + height + "\"/></a:xfrm>" +
            "<a:prstGeom prst=\"rect\"><a:avLst/></a:prstGeom>" +
            "</p:spPr>" +
            "<p:txBody><a:bodyPr/><a:lstStyle/><a:p/></p:txBody>" +
            "</p:sp>";
    }
}
