namespace svg.Tests;

public class SvgSceneBuilderTests
{
    [Test]
    public void ConvertsStructuralLinksAndShapesWhileSharingSolidPaint()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' width='100' height='60'><defs><rect id='unused' width='99' height='99'/></defs><g opacity='.5'><rect id='box' x='2' y='3' width='10' height='8' fill='#f00'/><a href='https://example.test/'><circle id='dot' cx='30' cy='20' r='4' fill='#f00' fill-rule='evenodd'/></a></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult result = resolved.BuildScene();
        SvgNode groupSource = document.DocumentElement.Children[1];
        SvgNode rectangleSource = groupSource.Children[0];
        SvgNode linkSource = groupSource.Children[1];
        SvgNode circleSource = linkSource.Children[0];
        SvgSceneNode group = result.Scene.FindNode(groupSource, "");
        SvgSceneNode rectangle = result.Scene.FindNode(rectangleSource, "");
        SvgSceneNode link = result.Scene.FindNode(linkSource, "");
        SvgSceneNode circle = result.Scene.FindNode(circleSource, "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(resolved.Scene, Is.SameAs(result.Scene));
            Assert.That(result.Scene.Root.Kind, Is.EqualTo(SvgSceneNodeKind.Group));
            Assert.That(result.Scene.Root.Group.EstablishesViewport, Is.True);
            Assert.That(group.Kind, Is.EqualTo(SvgSceneNodeKind.Group));
            Assert.That(group.Opacity, Is.EqualTo(.5));
            Assert.That(link.Kind, Is.EqualTo(SvgSceneNodeKind.Group));
            Assert.That(rectangle.Kind, Is.EqualTo(SvgSceneNodeKind.Path));
            Assert.That(rectangle.Path.Path.Commands, Is.Not.Empty);
            Assert.That(circle.Kind, Is.EqualTo(SvgSceneNodeKind.Path));
            Assert.That(circle.Path.FillRule, Is.EqualTo(SvgFillRule.EvenOdd));
            Assert.That(rectangle.Fill, Is.SameAs(circle.Fill));
            Assert.That(rectangle.Fill.Kind, Is.EqualTo(SvgSceneResourceKind.SolidPaint));
            Assert.That(result.Scene.FindNode(document.DocumentElement.Children[0], ""), Is.Null);
            Assert.That(result.Scene.FindNode(document.DocumentElement.Children[0].Children[0], ""), Is.Null);
            Assert.That(result.Scene.IsCurrent(), Is.True);
        });
    }

    [Test]
    public void ResolvesAndSharesTargetDependentGradientResources()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='g'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='blue'/></linearGradient></defs><rect id='a' width='20' height='10' fill='url(#g)'/><rect id='b' width='20' height='10' fill='url(#g)'/><rect id='c' x='30' width='40' height='10' fill='url(#g)'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult result = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode first = result.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneNode second = result.Scene.FindNode(document.DocumentElement.Children[2], "");
        SvgSceneNode third = result.Scene.FindNode(document.DocumentElement.Children[3], "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(first.Fill, Is.Not.Null);
            Assert.That(first.Fill.Kind, Is.EqualTo(SvgSceneResourceKind.GradientPaint));
            Assert.That(first.Fill, Is.SameAs(second.Fill));
            Assert.That(third.Fill, Is.Not.SameAs(first.Fill));
            Assert.That(first.Fill.GradientPaint.Stops, Has.Count.EqualTo(2));
            Assert.That(first.Fill.GradientPaint.Transform.A, Is.EqualTo(20).Within(1e-12));
            Assert.That(third.Fill.GradientPaint.Transform.A, Is.EqualTo(40).Within(1e-12));
            Assert.That(first.Fill.SourceNode, Is.SameAs(document.DocumentElement.Children[0].Children[0]));
        });
    }

    [Test]
    public void ConvertsClipMaskMarkerAndFilterGraphs()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<clipPath id='clip'><rect width='8' height='8'/></clipPath>" +
            "<mask id='mask' maskUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><circle cx='5' cy='5' r='5'/></mask>" +
            "<marker id='marker' markerWidth='4' markerHeight='4'><path d='M0 0 L4 2 L0 4 Z'/></marker>" +
            "<filter id='filter' filterUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><feGaussianBlur stdDeviation='1' result='blur'/><feOffset in='blur'/></filter>" +
            "</defs><path d='M1 1 L15 1' clip-path='url(#clip)' mask='url(#mask)' marker-start='url(#marker)' filter='url(#filter)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult result = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode path = result.Scene.FindNode(document.DocumentElement.Children[1], "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(path.Clip.Kind, Is.EqualTo(SvgSceneResourceKind.Clip));
            Assert.That(path.Clip.Clip.Content.Children.Single().Kind, Is.EqualTo(SvgSceneNodeKind.Path));
            Assert.That(path.Mask.Kind, Is.EqualTo(SvgSceneResourceKind.Mask));
            Assert.That(path.Mask.Mask.Content.Children.Single().Kind, Is.EqualTo(SvgSceneNodeKind.Path));
            Assert.That(path.MarkerStart.Kind, Is.EqualTo(SvgSceneResourceKind.Marker));
            Assert.That(path.MarkerStart.Marker.Content.Children.Single().Kind, Is.EqualTo(SvgSceneNodeKind.Path));
            Assert.That(path.Filter.Kind, Is.EqualTo(SvgSceneResourceKind.Filter));
            Assert.That(path.Filter.Filter.Primitives, Has.Count.EqualTo(2));
            Assert.That(path.Filter.Filter.Primitives[0].ResultName, Is.EqualTo("blur"));
            Assert.That(path.Filter.Filter.Primitives[1].Inputs.Single().Kind, Is.EqualTo(SvgFilterInputKind.NamedResult));
        });
    }

    [Test]
    public void CopiesImageBytesAndSharesRepeatedImageAcquisitions()
    {
        string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZK8sAAAAASUVORK5CYII=";
        string uri = "data:image/png;base64," + png;
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image width='10' height='10' href='" + uri + "'/><image x='20' width='10' height='10' href='" + uri + "'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult result = new SvgSceneBuilder().BuildWithDocument(resolved);
        SvgSceneNode first = result.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneNode second = result.Scene.FindNode(document.DocumentElement.Children[1], "");
        byte firstByte = first.Image.Resource.Image.Bytes[0];
        resolved.Root.Children[0].EmbeddedContent.ImageResource.Acquisition.Bytes[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(first.Kind, Is.EqualTo(SvgSceneNodeKind.Image));
            Assert.That(first.Image.Resource, Is.SameAs(second.Image.Resource));
            Assert.That(first.Image.Resource.Image.MediaType, Is.EqualTo("image/png"));
            Assert.That(first.Image.Resource.Image.IntrinsicSize.Width, Is.EqualTo(1));
            Assert.That(first.Image.Resource.Image.Bytes[0], Is.EqualTo(firstByte));
            Assert.That(first.Image.Resource.Image.Bytes, Is.Not.SameAs(resolved.Root.Children[0].EmbeddedContent.ImageResource.Acquisition.Bytes));
        });
    }

    [Test]
    public void EnforcesNodeLimitsWithoutAttachingAPartialScene()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g><rect width='10' height='10'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgSceneBuildOptions options = new SvgSceneBuildOptions();
        options.MaxNodes = 1;

        SvgSceneBuildResult result = new SvgSceneBuilder().BuildWithDocumentAndOptions(resolved, options);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.NodeCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Any(item => item.Code == SvgDiagnosticCode.InputLimitExceeded), Is.True);
            Assert.That(resolved.Scene, Is.Null);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
