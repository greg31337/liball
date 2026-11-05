namespace svg.Tests;

public class SvgRenderCommandTests
{
    [Test]
    public void EnumeratesPaintOrderWithExactSharedResourceReferences()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><marker id='m' markerUnits='userSpaceOnUse' markerWidth='2' markerHeight='2'><rect width='2' height='2'/></marker></defs>" +
            "<path d='M0 0 L10 0' fill='red' stroke='blue' marker-start='url(#m)' paint-order='stroke markers fill'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode path = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgRenderCommandList commands = built.Scene.RenderCommands;
        List<SvgRenderCommand> pathCommands = commands.Find(path);
        List<SvgRenderCommand> paints = pathCommands.Where(item => item.Kind == SvgRenderCommandKind.StrokePath || item.Kind == SvgRenderCommandKind.DrawMarkers || item.Kind == SvgRenderCommandKind.FillPath).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(built.Success, Is.True);
            Assert.That(commands.Success, Is.True);
            Assert.That(paints.Select(item => item.Kind), Is.EqualTo(new[] { SvgRenderCommandKind.StrokePath, SvgRenderCommandKind.DrawMarkers, SvgRenderCommandKind.FillPath }));
            Assert.That(paints[0].Resource, Is.SameAs(path.Stroke));
            Assert.That(paints[1].MarkerStart, Is.SameAs(path.MarkerStart));
            Assert.That(paints[1].MarkerMid, Is.Null);
            Assert.That(paints[2].Resource, Is.SameAs(path.Fill));
            Assert.That(paints[0].Opacity, Is.EqualTo(path.StrokeOpacity));
            Assert.That(paints[2].Opacity, Is.EqualTo(path.FillOpacity));
            Assert.That(pathCommands.First().Kind, Is.EqualTo(SvgRenderCommandKind.PushTransform));
            Assert.That(pathCommands.First().Transform, Is.SameAs(path.AbsoluteTransform));
            Assert.That(pathCommands.Last().Kind, Is.EqualTo(SvgRenderCommandKind.PopTransform));
            Assert.That(commands.Commands.Select((item, index) => item.Sequence == index), Is.All.True);
        });
    }

    [Test]
    public void NestedTransformClipMaskAndLayerScopesAreBalanced()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20' overflow='hidden'><defs>" +
            "<clipPath id='c'><rect width='8' height='8'/></clipPath>" +
            "<mask id='m' maskUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><rect width='20' height='20'/></mask>" +
            "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='20' height='20'><feFlood/></filter>" +
            "</defs><g opacity='.5' mask='url(#m)' filter='url(#f)'><rect width='10' height='10' clip-path='url(#c)'/></g></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgRenderCommandList result = resolved.BuildScene().Scene.RenderCommands;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(ScopesAreBalanced(result.Commands), Is.True);
            Assert.That(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PushTransform), Is.EqualTo(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PopTransform)));
            Assert.That(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PushClip), Is.EqualTo(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PopClip)));
            Assert.That(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PushMask), Is.EqualTo(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.PopMask)));
            Assert.That(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.BeginLayer), Is.EqualTo(result.Commands.Count(item => item.Kind == SvgRenderCommandKind.EndLayer)));
            Assert.That(result.Commands.Any(item => item.Kind == SvgRenderCommandKind.PushClip && item.Resource == null && item.Bounds != null), Is.True);
            Assert.That(result.Commands.Any(item => item.Kind == SvgRenderCommandKind.PushClip && item.Resource != null && item.Resource.Kind == SvgSceneResourceKind.Clip), Is.True);
            Assert.That(result.Commands.Any(item => item.Kind == SvgRenderCommandKind.PushMask && item.Resource.Kind == SvgSceneResourceKind.Mask), Is.True);
            Assert.That(result.Commands.Any(item => item.Kind == SvgRenderCommandKind.BeginLayer && item.Resource != null && item.Resource.Kind == SvgSceneResourceKind.Filter), Is.True);
        });
    }

    [Test]
    public void SkipsSuppressedNodesAndKeepsVisibleSiblingSourceOrder()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='first' width='2' height='2' fill='red'/><rect id='hidden' width='3' height='3' visibility='hidden'/><rect id='second' x='5' width='2' height='2' fill='blue'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgSceneBuildResult built = resolved.BuildScene();
        SvgSceneNode first = built.Scene.FindNode(document.DocumentElement.Children[0], "");
        SvgSceneNode hidden = built.Scene.FindNode(document.DocumentElement.Children[1], "");
        SvgSceneNode second = built.Scene.FindNode(document.DocumentElement.Children[2], "");
        List<SvgRenderCommand> fills = built.Scene.RenderCommands.Commands.Where(item => item.Kind == SvgRenderCommandKind.FillPath).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(fills.Select(item => item.Node), Is.EqualTo(new[] { first, second }));
            Assert.That(built.Scene.RenderCommands.Find(hidden), Is.Empty);
            Assert.That(fills[0].Resource, Is.SameAs(first.Fill));
            Assert.That(fills[1].Resource, Is.SameAs(second.Fill));
        });
    }

    [Test]
    public void ReusesOneSharedPaintResourceAcrossCommands()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='2' height='2' fill='#123456'/><rect x='4' width='2' height='2' fill='#123456'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgScene scene = resolved.BuildScene().Scene;
        List<SvgRenderCommand> fills = scene.RenderCommands.Commands.Where(item => item.Kind == SvgRenderCommandKind.FillPath).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(fills, Has.Count.EqualTo(2));
            Assert.That(fills[0].Resource, Is.SameAs(fills[1].Resource));
            Assert.That(scene.Resources.Count(item => item.Kind == SvgSceneResourceKind.SolidPaint), Is.EqualTo(1));
        });
    }

    [Test]
    public void ImageCommandsReferenceSharedImageResourcesAndWorldBounds()
    {
        string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZK8sAAAAASUVORK5CYII=";
        string uri = "data:image/png;base64," + png;
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><image x='3' y='4' width='5' height='6' href='" + uri + "'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneNode image = scene.FindNode(document.DocumentElement.Children[0], "");
        SvgRenderCommand command = scene.RenderCommands.Commands.Single(item => item.Kind == SvgRenderCommandKind.DrawImage);

        Assert.Multiple(() =>
        {
            Assert.That(command.Node, Is.SameAs(image));
            Assert.That(command.Resource, Is.SameAs(image.Image.Resource));
            Assert.That(command.Bounds, Is.SameAs(image.FillBounds));
            Assert.That(command.Resource.Kind, Is.EqualTo(SvgSceneResourceKind.Image));
        });
    }

    [Test]
    public void ExplicitCommandLimitDoesNotReplaceCompletedAttachedCommands()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='2' height='2'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgRenderCommandList completed = scene.RenderCommands;
        SvgRenderCommandOptions limited = new SvgRenderCommandOptions();
        limited.MaximumCommands = 1;

        SvgRenderCommandList result = scene.EnumerateCommandsWithOptions(limited);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo(SvgRenderCommandError.LimitExceeded));
            Assert.That(scene.RenderCommands, Is.SameAs(completed));
        });
    }

    static bool ScopesAreBalanced(List<SvgRenderCommand> commands)
    {
        Stack<SvgRenderCommandKind> stack = new Stack<SvgRenderCommandKind>();
        foreach (SvgRenderCommand command in commands)
        {
            if (command.Kind == SvgRenderCommandKind.PushTransform || command.Kind == SvgRenderCommandKind.PushClip || command.Kind == SvgRenderCommandKind.PushMask || command.Kind == SvgRenderCommandKind.BeginLayer) stack.Push(command.Kind);
            else if (command.Kind == SvgRenderCommandKind.PopTransform && !Pop(stack, SvgRenderCommandKind.PushTransform)) return false;
            else if (command.Kind == SvgRenderCommandKind.PopClip && !Pop(stack, SvgRenderCommandKind.PushClip)) return false;
            else if (command.Kind == SvgRenderCommandKind.PopMask && !Pop(stack, SvgRenderCommandKind.PushMask)) return false;
            else if (command.Kind == SvgRenderCommandKind.EndLayer && !Pop(stack, SvgRenderCommandKind.BeginLayer)) return false;
        }
        return stack.Count == 0;
    }

    static bool Pop(Stack<SvgRenderCommandKind> stack, SvgRenderCommandKind expected)
    {
        return stack.Count != 0 && stack.Pop() == expected;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
