namespace svg.Tests;

public class SvgSceneDeterminismTests
{
    [Test]
    public void IndependentBuildsKeepNormalizedPreorderResourcesBoundsLayersAndCommandsStable()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='40' height='30' overflow='hidden'><defs>" +
            "<linearGradient id='paint'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='yellow'/></linearGradient>" +
            "<clipPath id='clip'><rect width='8' height='8'/></clipPath>" +
            "<mask id='mask' maskUnits='userSpaceOnUse' x='0' y='0' width='30' height='20'><rect width='30' height='20'/></mask>" +
            "<filter id='filter' filterUnits='userSpaceOnUse' x='0' y='0' width='30' height='20'><feFlood/></filter>" +
            "<marker id='marker' markerUnits='userSpaceOnUse' markerWidth='2' markerHeight='2'><rect width='2' height='2'/></marker>" +
            "<g id='art'><rect width='3' height='4'/></g></defs>" +
            "<g id='layer' opacity='.5' mask='url(#mask)' filter='url(#filter)'>" +
            "<path id='first' d='M1 1 H11 V11 H1 Z' fill='url(#paint)' stroke='blue' clip-path='url(#clip)' marker-end='url(#marker)'/>" +
            "<use href='#art' x='15' y='5'/></g>" +
            "<rect id='last' x='35' y='25' width='10' height='10' fill='green'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);

        SvgScene first = new SvgSceneBuilder().BuildWithDocument(resolved).Scene;
        SvgScene second = new SvgSceneBuilder().BuildWithDocument(resolved).Scene;

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(NodeSignature(first), Is.EqualTo(NodeSignature(second)));
            Assert.That(ResourceSignature(first), Is.EqualTo(ResourceSignature(second)));
            Assert.That(LayerSignature(first), Is.EqualTo(LayerSignature(second)));
            Assert.That(CommandSignature(first), Is.EqualTo(CommandSignature(second)));
            Assert.That(IsPreorder(first), Is.True);
            Assert.That(IsPreorder(second), Is.True);
            Assert.That(ScopesAreBalanced(first.RenderCommands.Commands), Is.True);
            Assert.That(ScopesAreBalanced(second.RenderCommands.Commands), Is.True);
            AssertRect(first.Bounds, 0, 0, 40, 30);
            AssertRect(second.Bounds, 0, 0, 40, 30);
            Assert.That(first.LayerPlan.PaintBounds.ValueEquals(first.Bounds), Is.True);
            Assert.That(second.LayerPlan.PaintBounds.ValueEquals(second.Bounds), Is.True);
        });
    }

    static string NodeSignature(SvgScene scene)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < scene.Nodes.Count)
        {
            SvgSceneNode node = scene.Nodes[index];
            result.Append(index).Append(':').Append((int)node.Kind).Append(':').Append(node.SourcePath).Append(':').Append(node.InstancePath).Append(';');
            index = index + 1;
        }
        return result.ToString();
    }

    static string ResourceSignature(SvgScene scene)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < scene.Resources.Count)
        {
            SvgSceneResource resource = scene.Resources[index];
            result.Append(index).Append(':').Append((int)resource.Kind).Append(':').Append(resource.Key).Append(';');
            index = index + 1;
        }
        return result.ToString();
    }

    static string LayerSignature(SvgScene scene)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < scene.LayerPlan.Layers.Count)
        {
            SvgSceneLayer layer = scene.LayerPlan.Layers[index];
            result.Append(index).Append(':').Append(layer.Node.SourcePath).Append(':').Append(layer.Node.InstancePath).Append(':');
            int featureIndex = 0;
            while (featureIndex < layer.Features.Count)
            {
                result.Append((int)layer.Features[featureIndex]).Append(',');
                featureIndex = featureIndex + 1;
            }
            result.Append(';');
            index = index + 1;
        }
        return result.ToString();
    }

    static string CommandSignature(SvgScene scene)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < scene.RenderCommands.Commands.Count)
        {
            SvgRenderCommand command = scene.RenderCommands.Commands[index];
            string resourceKey = command.Resource == null ? "" : command.Resource.Key;
            string startKey = command.MarkerStart == null ? "" : command.MarkerStart.Key;
            string middleKey = command.MarkerMid == null ? "" : command.MarkerMid.Key;
            string endKey = command.MarkerEnd == null ? "" : command.MarkerEnd.Key;
            result.Append(command.Sequence).Append(':').Append((int)command.Kind).Append(':').Append(command.Node.SourcePath).Append(':').Append(command.Node.InstancePath).Append(':').Append(resourceKey).Append(':').Append(startKey).Append(':').Append(middleKey).Append(':').Append(endKey).Append(';');
            index = index + 1;
        }
        return result.ToString();
    }

    static bool IsPreorder(SvgScene scene)
    {
        int index = 0;
        while (index < scene.Nodes.Count)
        {
            SvgSceneNode node = scene.Nodes[index];
            if (node.Parent != null && IndexOf(scene.Nodes, node.Parent) >= index) return false;
            index = index + 1;
        }
        return true;
    }

    static int IndexOf(List<SvgSceneNode> nodes, SvgSceneNode target)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            if (object.ReferenceEquals(nodes[index], target)) return index;
            index = index + 1;
        }
        return -1;
    }

    static bool ScopesAreBalanced(List<SvgRenderCommand> commands)
    {
        Stack<SvgRenderCommandKind> stack = new Stack<SvgRenderCommandKind>();
        int index = 0;
        while (index < commands.Count)
        {
            SvgRenderCommandKind kind = commands[index].Kind;
            if (kind == SvgRenderCommandKind.PushTransform || kind == SvgRenderCommandKind.PushClip || kind == SvgRenderCommandKind.PushMask || kind == SvgRenderCommandKind.BeginLayer) stack.Push(kind);
            else if (kind == SvgRenderCommandKind.PopTransform && !Pop(stack, SvgRenderCommandKind.PushTransform)) return false;
            else if (kind == SvgRenderCommandKind.PopClip && !Pop(stack, SvgRenderCommandKind.PushClip)) return false;
            else if (kind == SvgRenderCommandKind.PopMask && !Pop(stack, SvgRenderCommandKind.PushMask)) return false;
            else if (kind == SvgRenderCommandKind.EndLayer && !Pop(stack, SvgRenderCommandKind.BeginLayer)) return false;
            index = index + 1;
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
