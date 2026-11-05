namespace svg.Tests;

public class SvgSceneBoundsTests
{
    [Test]
    public void PrecomputesNestedAbsoluteTransformsAndFillStrokeWorldBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='100'><g transform='translate(10 20)'><rect x='1' y='2' width='4' height='3' transform='scale(2)' fill='red' stroke='blue' stroke-width='2'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode rectangle = built.Scene.FindNode(document.DocumentElement.Children[0].Children[0], "");
        SvgSceneNode group = rectangle.Parent;

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            AssertMatrix(rectangle.AbsoluteTransform, 2, 0, 0, 2, 10, 20);
            AssertRect(rectangle.FillBounds, 12, 24, 8, 6);
            AssertRect(rectangle.StrokeBounds, 10, 22, 12, 10);
            AssertRect(rectangle.WorldBounds, 10, 22, 12, 10);
            AssertRect(group.WorldBounds, 10, 22, 12, 10);
            AssertRect(built.Scene.Bounds, 10, 22, 12, 10);
        });
    }

    [Test]
    public void FilterThenClipRegionsProducePrecomputedEffectAndWorldBounds()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='20' height='10'><feFlood/></filter>" +
            "<clipPath id='c' clipPathUnits='userSpaceOnUse'><rect x='2' y='1' width='4' height='6'/></clipPath>" +
            "</defs><g transform='translate(10 5)'><rect width='8' height='8' filter='url(#f)' clip-path='url(#c)'/></g></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode rectangle = built.Scene.FindNode(document.DocumentElement.Children[1].Children[0], "");

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            AssertRect(rectangle.FilterBounds, 10, 5, 20, 10);
            AssertRect(rectangle.FillBounds, 10, 5, 8, 8);
            AssertRect(rectangle.WorldBounds, 12, 6, 4, 6);
            AssertRect(built.Scene.Bounds, 12, 6, 4, 6);
        });
    }

    [Test]
    public void MarkerInstancesContributeToPrecomputedWorldBounds()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><marker id='m' markerUnits='userSpaceOnUse' markerWidth='2' markerHeight='2' orient='0'><rect width='2' height='2'/></marker></defs>" +
            "<path d='M1 1 L11 1' fill='none' marker-start='url(#m)' marker-end='url(#m)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode path = built.Scene.FindNode(document.DocumentElement.Children[1], "");

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(path.FillBounds.IsEmpty, Is.True);
            AssertRect(path.MarkerBounds, 1, 1, 12, 2);
            AssertRect(path.WorldBounds, 1, 1, 12, 2);
            AssertRect(built.Scene.Bounds, 1, 1, 12, 2);
        });
    }

    [Test]
    public void UseInstanceIdentityRetainsIndependentAbsoluteTransformAndBounds()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><g id='art'><rect x='1' y='2' width='3' height='4'/></g></defs><use href='#art' x='20' y='10'/></svg>");
        SvgNode rectangleSource = document.DocumentElement.Children[0].Children[0].Children[0];
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode instance = built.Scene.FindNode(rectangleSource, "0/1/instance/0/0");

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.SourceNode, Is.SameAs(rectangleSource));
            Assert.That(instance.InstancePath, Is.EqualTo("0/1/instance/0/0"));
            AssertMatrix(instance.AbsoluteTransform, 1, 0, 0, 1, 20, 10);
            AssertRect(instance.FillBounds, 21, 12, 3, 4);
            AssertRect(instance.WorldBounds, 21, 12, 3, 4);
            Assert.That(built.Scene.FindNode(rectangleSource, ""), Is.Null);
        });
    }

    [Test]
    public void ExplicitRecomputationHonorsTraversalLimits()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect width='2' height='3'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneBoundsOptions limited = new SvgSceneBoundsOptions();
        limited.MaximumNodes = 1;

        SvgSceneBoundsResult result = scene.ComputeBoundsWithOptions(resolved, limited);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgDrawingBoundsError.LimitExceeded));
            Assert.That(result.ProcessedNodeCount, Is.Zero);
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
