namespace svg.Tests;

public class SvgSceneLayerPlannerTests
{
    [Test]
    public void RetainsPaintOrderAndPerComponentOpacity()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><path d='M0 0 H10 V5 H0 Z' fill='red' fill-opacity='.25' stroke='blue' stroke-opacity='.6' paint-order='stroke markers fill'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode path = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneLayer layer = built.Scene.LayerPlan.Find(path);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(path.PaintOrder, Is.EqualTo(new[] { SvgPaintOrderComponent.Stroke, SvgPaintOrderComponent.Markers, SvgPaintOrderComponent.Fill }));
            Assert.That(layer.PaintOrder, Is.EqualTo(path.PaintOrder));
            Assert.That(path.FillOpacity, Is.EqualTo(.25).Within(1e-12));
            Assert.That(path.StrokeOpacity, Is.EqualTo(.6).Within(1e-12));
            Assert.That(layer.Paintable, Is.True);
            Assert.That(layer.CreatesCompositingLayer, Is.False);
        });
    }

    [Test]
    public void DisplayAndVisibilitySuppressPaintButRetainSceneState()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<rect id='hidden' width='20' height='20' visibility='hidden'/>" +
            "<rect id='none' width='30' height='30' display='none'/>" +
            "<rect id='visible' x='40' y='2' width='5' height='6'/>" +
            "</svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode hidden = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneNode none = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneNode visible = built.Scene.FindNode(document.DocumentElement.Children[2], "");

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(hidden, Is.Not.Null);
            Assert.That(hidden.Displayed, Is.True);
            Assert.That(hidden.Visible, Is.False);
            Assert.That(built.Scene.LayerPlan.Find(hidden).Paintable, Is.False);
            Assert.That(none, Is.Not.Null);
            Assert.That(none.Displayed, Is.False);
            Assert.That(none.Visible, Is.True);
            Assert.That(built.Scene.LayerPlan.Find(none).SubtreePaintable, Is.False);
            Assert.That(built.Scene.LayerPlan.Find(visible).Paintable, Is.True);
            AssertRect(built.Scene.LayerPlan.PaintBounds, 40, 2, 5, 6);
            AssertRect(built.Scene.Bounds, 40, 2, 5, 6);
        });
    }

    [Test]
    public void OpacityBlendIsolationMaskAndFilterRequireOneGroupedLayer()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<mask id='m' maskUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><rect width='20' height='20'/></mask>" +
            "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><feFlood/></filter>" +
            "</defs><g opacity='.4' style='mix-blend-mode:multiply;isolation:isolate' mask='url(#m)' filter='url(#f)'><rect width='10' height='10'/></g></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode group = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneLayer layer = built.Scene.LayerPlan.Find(group);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(layer.CreatesCompositingLayer, Is.True);
            Assert.That(layer.Opacity, Is.EqualTo(.4).Within(1e-12));
            Assert.That(layer.BlendMode, Is.EqualTo("multiply"));
            Assert.That(layer.Isolated, Is.True);
            Assert.That(layer.Features, Does.Contain(SvgSceneLayerFeature.Opacity));
            Assert.That(layer.Features, Does.Contain(SvgSceneLayerFeature.Blend));
            Assert.That(layer.Features, Does.Contain(SvgSceneLayerFeature.Isolation));
            Assert.That(layer.Features, Does.Contain(SvgSceneLayerFeature.Mask));
            Assert.That(layer.Features, Does.Contain(SvgSceneLayerFeature.Filter));
            Assert.That(layer.SubtreePaintable, Is.True);
            Assert.That(built.Scene.LayerPlan.CompositingLayerCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void OverflowClipPropagatesAndClipsLayerPaintBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='10' height='8' overflow='hidden'><g><rect x='6' y='3' width='10' height='10'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode group = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneNode rectangle = built.Scene.FindNode(document.DocumentElement.Children[0].Children[0], "");
        SvgSceneLayer rootLayer = built.Scene.LayerPlan.Root;
        SvgSceneLayer groupLayer = built.Scene.LayerPlan.Find(group);
        SvgSceneLayer rectangleLayer = built.Scene.LayerPlan.Find(rectangle);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            AssertRect(rootLayer.EffectiveClipBounds, 0, 0, 10, 8);
            AssertRect(groupLayer.EffectiveClipBounds, 0, 0, 10, 8);
            AssertRect(rectangleLayer.EffectiveClipBounds, 0, 0, 10, 8);
            AssertRect(rectangleLayer.PaintBounds, 6, 3, 4, 5);
            AssertRect(built.Scene.Bounds, 6, 3, 4, 5);
            Assert.That(rootLayer.Features, Does.Contain(SvgSceneLayerFeature.Clip));
            Assert.That(rootLayer.CreatesCompositingLayer, Is.True);
        });
    }

    [Test]
    public void ObjectBoundingBoxTransformMatchesTargetAndDrivesPaintResourceGeometry()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs>" +
            "<rect x='2' y='3' width='4' height='5' fill='url(#g)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode rectangle = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneLayer layer = built.Scene.LayerPlan.Find(rectangle);

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(rectangle.HasObjectBoundingBox, Is.True);
            AssertRect(rectangle.ObjectBoundingBox, 2, 3, 4, 5);
            AssertMatrix(rectangle.ObjectBoundingBoxTransform, 4, 0, 0, 5, 2, 3);
            Assert.That(layer.HasObjectBoundingBox, Is.True);
            Assert.That(layer.ObjectBoundingBox, Is.SameAs(rectangle.ObjectBoundingBox));
            Assert.That(layer.ObjectBoundingBoxTransform, Is.SameAs(rectangle.ObjectBoundingBoxTransform));
            Assert.That(rectangle.Fill.Kind, Is.EqualTo(SvgSceneResourceKind.GradientPaint));
            AssertMatrix(rectangle.Fill.GradientPaint.Transform, 4, 0, 0, 5, 2, 3);
        });
    }

    [Test]
    public void ExplicitPlanningHonorsLayerLimitsWithoutReplacingCompletedPlan()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect width='2' height='2'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneLayerPlan completed = scene.LayerPlan;
        SvgSceneLayerOptions limited = new SvgSceneLayerOptions();
        limited.MaximumLayers = 1;

        SvgSceneLayerPlan result = scene.PlanLayersWithOptions(resolved, limited);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgDrawingBoundsError.LimitExceeded));
            Assert.That(scene.LayerPlan, Is.SameAs(completed));
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static void AssertMatrix(SvgMatrix matrix, double a, double b, double c, double d, double e, double f)
    {
        Assert.That(matrix.A, Is.EqualTo(a).Within(0.000000001));
        Assert.That(matrix.B, Is.EqualTo(b).Within(0.000000001));
        Assert.That(matrix.C, Is.EqualTo(c).Within(0.000000001));
        Assert.That(matrix.D, Is.EqualTo(d).Within(0.000000001));
        Assert.That(matrix.E, Is.EqualTo(e).Within(0.000000001));
        Assert.That(matrix.F, Is.EqualTo(f).Within(0.000000001));
    }

    static void AssertRect(SvgRect rectangle, double x, double y, double width, double height)
    {
        Assert.That(rectangle, Is.Not.Null);
        Assert.That(rectangle.IsEmpty, Is.False);
        Assert.That(rectangle.X, Is.EqualTo(x).Within(0.000000001));
        Assert.That(rectangle.Y, Is.EqualTo(y).Within(0.000000001));
        Assert.That(rectangle.Width, Is.EqualTo(width).Within(0.000000001));
        Assert.That(rectangle.Height, Is.EqualTo(height).Within(0.000000001));
    }
}
