namespace svg.Tests;

public class SvgSceneExtractorTests
{
    [Test]
    public void FragmentExtractionRetainsAncestorEffectsAndLeavesFullSceneUntouched()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='30' height='20' overflow='hidden'><defs>" +
            "<mask id='m' maskUnits='userSpaceOnUse' x='0' y='0' width='30' height='20'><rect width='30' height='20'/></mask>" +
            "<filter id='f' filterUnits='userSpaceOnUse' x='0' y='0' width='30' height='20'><feFlood/></filter>" +
            "</defs><g transform='translate(5 4)' opacity='.4' style='mix-blend-mode:multiply;isolation:isolate;color-interpolation:linearRGB' mask='url(#m)' filter='url(#f)'>" +
            "<g id='target' transform='scale(2)'><rect id='selected' x='1' y='2' width='3' height='4'/></g>" +
            "<rect id='sibling' x='50' width='2' height='2'/></g></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgNode ancestorSource = document.DocumentElement.Children[1];
        SvgNode targetSource = ancestorSource.Children[0];
        SvgNode selectedSource = targetSource.Children[0];
        SvgNode siblingSource = ancestorSource.Children[1];
        SvgSceneNode originalAncestor = scene.FindNode(ancestorSource, "");
        SvgSceneNode originalTarget = scene.FindNode(targetSource, "");
        SvgSceneNode originalSelected = scene.FindNode(selectedSource, "");
        SvgSceneLayerPlan originalPlan = scene.LayerPlan;
        SvgRenderCommandList originalCommands = scene.RenderCommands;

        SvgSceneExtractionResult result = scene.ExtractFragment(resolved, "#target");
        SvgSceneNode extractedAncestor = result.Target.Parent;
        SvgSceneNode extractedSelected = result.Scene.FindNode(selectedSource, "");
        SvgSceneLayer ancestorLayer = result.Scene.LayerPlan.Find(extractedAncestor);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Error, Is.EqualTo(SvgSceneExtractionError.None));
            Assert.That(result.Scene, Is.Not.SameAs(scene));
            Assert.That(result.SourceTarget, Is.SameAs(originalTarget));
            Assert.That(result.Target, Is.Not.SameAs(originalTarget));
            Assert.That(result.AncestorCount, Is.EqualTo(2));
            Assert.That(result.Scene.Nodes, Has.Count.EqualTo(4));
            Assert.That(extractedAncestor.SourceNode, Is.SameAs(ancestorSource));
            Assert.That(extractedAncestor.Children, Has.Count.EqualTo(1));
            Assert.That(result.Scene.FindNode(siblingSource, ""), Is.Null);
            AssertMatrix(result.Target.AbsoluteTransform, originalTarget.AbsoluteTransform);
            AssertRect(extractedSelected.WorldBounds, originalSelected.WorldBounds);
            Assert.That(ancestorLayer.Features, Does.Contain(SvgSceneLayerFeature.Opacity));
            Assert.That(ancestorLayer.Features, Does.Contain(SvgSceneLayerFeature.Blend));
            Assert.That(ancestorLayer.Features, Does.Contain(SvgSceneLayerFeature.Isolation));
            Assert.That(extractedAncestor.ColorInterpolation, Is.EqualTo(SvgColorSpace.SrgbLinear));
            Assert.That(ancestorLayer.ColorInterpolation, Is.EqualTo(SvgColorSpace.SrgbLinear));
            Assert.That(ancestorLayer.Features, Does.Contain(SvgSceneLayerFeature.Mask));
            Assert.That(ancestorLayer.Features, Does.Contain(SvgSceneLayerFeature.Filter));
            Assert.That(result.Scene.LayerPlan.Root.EffectiveClipBounds, Is.Not.Null);
            Assert.That(result.Scene.RenderCommands.Success, Is.True);
            Assert.That(scene.LayerPlan, Is.SameAs(originalPlan));
            Assert.That(scene.RenderCommands, Is.SameAs(originalCommands));
            Assert.That(originalAncestor.Children, Has.Count.EqualTo(2));
            Assert.That(originalTarget.Parent, Is.SameAs(originalAncestor));
            Assert.That(originalSelected.Parent, Is.SameAs(originalTarget));
            Assert.That(extractedSelected.Path.Path, Is.Not.SameAs(originalSelected.Path.Path));
            Assert.That(extractedSelected.Path.Path.Commands[0], Is.Not.SameAs(originalSelected.Path.Path.Commands[0]));
            Assert.That(extractedSelected.Path.Path.Commands[0].Values, Is.Not.SameAs(originalSelected.Path.Path.Commands[0].Values));
            Assert.That(extractedSelected.Path.StrokeStyle, Is.Not.SameAs(originalSelected.Path.StrokeStyle));
            Assert.That(extractedSelected.Path.StrokeStyle.DashArray, Is.Not.SameAs(originalSelected.Path.StrokeStyle.DashArray));
            Assert.That(extractedSelected.Path.ShapeRendering, Is.EqualTo(originalSelected.Path.ShapeRendering));
            Assert.That(extractedSelected.Path.VectorEffect, Is.EqualTo(originalSelected.Path.VectorEffect));
        });

        result.Target.Opacity = 0;
        double originalValue = originalSelected.Path.Path.Commands[0].Values[0];
        extractedSelected.Path.Path.Commands[0].Values[0] = originalValue + 100;
        extractedSelected.Path.StrokeStyle.Width = 9;
        Assert.That(extractedAncestor.RemoveChild(result.Target), Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(originalTarget.Opacity, Is.EqualTo(1));
            Assert.That(originalTarget.Parent, Is.SameAs(originalAncestor));
            Assert.That(originalAncestor.Children, Has.Count.EqualTo(2));
            Assert.That(originalSelected.Path.Path.Commands[0].Values[0], Is.EqualTo(originalValue));
            Assert.That(originalSelected.Path.StrokeStyle.Width, Is.EqualTo(1));
        });
    }

    [Test]
    public void SourceExtractionRetainsOnlyTransitivelyReferencedSharedResourcesInOrder()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs>" +
            "<linearGradient id='keep'><stop offset='0' stop-color='red'/><stop offset='1' stop-color='yellow'/></linearGradient>" +
            "<linearGradient id='discard'><stop offset='0' stop-color='blue'/><stop offset='1' stop-color='green'/></linearGradient>" +
            "<clipPath id='clip'><rect width='8' height='8'/></clipPath>" +
            "</defs><g id='target' clip-path='url(#clip)'><rect id='inside' width='10' height='10' fill='url(#keep)'/></g>" +
            "<rect id='outside' x='20' width='10' height='10' fill='url(#discard)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgNode targetSource = document.DocumentElement.Children[1];
        SvgNode insideSource = targetSource.Children[0];
        SvgNode outsideSource = document.DocumentElement.Children[2];
        SvgSceneNode originalTarget = scene.FindNode(targetSource, "");
        SvgSceneNode inside = scene.FindNode(insideSource, "");
        SvgSceneNode outside = scene.FindNode(outsideSource, "");

        SvgSceneExtractionResult result = scene.ExtractSource(resolved, targetSource);
        SvgSceneNode extractedInside = result.Scene.FindNode(insideSource, "");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceTarget, Is.SameAs(originalTarget));
            Assert.That(extractedInside.Fill, Is.SameAs(inside.Fill));
            Assert.That(result.Target.Clip, Is.SameAs(originalTarget.Clip));
            Assert.That(ContainsResource(result.Scene.Resources, inside.Fill), Is.True);
            Assert.That(ContainsResource(result.Scene.Resources, originalTarget.Clip), Is.True);
            Assert.That(ContainsResource(result.Scene.Resources, outside.Fill), Is.False);
            Assert.That(ResourcesFollowSourceOrder(scene.Resources, result.Scene.Resources), Is.True);
            Assert.That(AllCommandResourcesRetained(result.Scene), Is.True);
            Assert.That(result.Scene.FindNode(outsideSource, ""), Is.Null);
            Assert.That(scene.FindNode(outsideSource, ""), Is.SameAs(outside));
        });
    }

    [Test]
    public void SourceExtractionRetainsImagesReferencedByFilters()
    {
        string png = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZK8sAAAAASUVORK5CYII=";
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><filter id='f'><feImage href='data:image/png;base64," + png + "'/></filter></defs><rect id='target' width='2' height='2' filter='url(#f)'/></svg>";
        SvgDocument document = Load(source);
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgNode targetSource = document.DocumentElement.Children[1];
        SvgSceneNode target = scene.FindNode(targetSource, "");
        SvgSceneResource image = target.Filter.Filter.Primitives[0].ImageResource;

        SvgSceneExtractionResult result = scene.ExtractSource(resolved, targetSource);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(image, Is.Not.Null);
            Assert.That(ContainsResource(result.Scene.Resources, target.Filter), Is.True);
            Assert.That(ContainsResource(result.Scene.Resources, image), Is.True);
        });
    }

    [Test]
    public void InstanceExtractionSelectsOneExactUseIdentityAndPreservesWorldGeometry()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><g id='art'><rect x='1' y='2' width='3' height='4'/></g></defs><use href='#art' x='10' y='5'/><use href='#art' x='30' y='7'/></svg>");
        SvgNode rectangleSource = document.DocumentElement.Children[0].Children[0].Children[0];
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneNode first = scene.FindNode(rectangleSource, "0/1/instance/0/0");
        SvgSceneNode second = scene.FindNode(rectangleSource, "0/2/instance/0/0");

        SvgSceneExtractionResult result = scene.ExtractInstance(resolved, second.InstancePath);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceTarget, Is.SameAs(second));
            Assert.That(result.Target.SourceNode, Is.SameAs(rectangleSource));
            Assert.That(result.Target.InstancePath, Is.EqualTo(second.InstancePath));
            AssertMatrix(result.Target.AbsoluteTransform, second.AbsoluteTransform);
            AssertRect(result.Target.WorldBounds, second.WorldBounds);
            Assert.That(result.Scene.FindNode(rectangleSource, first.InstancePath), Is.Null);
            Assert.That(result.Scene.FindNode(rectangleSource, second.InstancePath), Is.SameAs(result.Target));
            Assert.That(first.Parent, Is.Not.Null);
            Assert.That(second.Parent, Is.Not.Null);
        });
    }

    [Test]
    public void FragmentExtractionFallsBackToTheFirstRenderedInstanceForDefinitions()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><g id='art'><rect width='2' height='3'/></g></defs><use href='#art' x='8'/><use href='#art' x='20'/></svg>");
        SvgNode artSource = document.DocumentElement.Children[0].Children[0];
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;

        SvgSceneExtractionResult result = scene.ExtractFragment(resolved, "art");

        Assert.Multiple(() =>
        {
            Assert.That(scene.FindNode(artSource, ""), Is.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.SourceTarget.SourceNode, Is.SameAs(artSource));
            Assert.That(result.SourceTarget.InstancePath, Is.Not.Empty);
            Assert.That(result.Target.InstancePath, Is.EqualTo(result.SourceTarget.InstancePath));
            Assert.That(result.Scene.FindNode(artSource, result.Target.InstancePath), Is.SameAs(result.Target));
        });
    }

    [Test]
    public void ExtractionReportsStableLookupAndBoundFailuresWithoutChangingTheFullScene()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='target'><rect width='4' height='5'/></g></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgScene scene = resolved.BuildScene().Scene;
        SvgSceneNode root = scene.Root;
        SvgSceneLayerPlan plan = scene.LayerPlan;
        SvgRenderCommandList commands = scene.RenderCommands;
        SvgSceneExtractionOptions nodeLimit = new SvgSceneExtractionOptions();
        nodeLimit.MaximumNodes = 2;
        SvgSceneExtractionOptions resourceLimit = new SvgSceneExtractionOptions();
        resourceLimit.MaximumResources = 0;
        SvgSceneExtractionOptions commandLimit = new SvgSceneExtractionOptions();
        commandLimit.MaximumCommands = 1;

        SvgSceneExtractionResult missing = scene.ExtractFragment(resolved, "missing");
        SvgSceneExtractionResult invalidInstance = scene.ExtractInstance(resolved, "");
        SvgSceneExtractionResult absentInstance = scene.ExtractInstance(resolved, "not/an/instance");
        SvgSceneExtractionResult nodes = scene.ExtractFragmentWithOptions(resolved, "target", nodeLimit);
        SvgSceneExtractionResult resources = scene.ExtractFragmentWithOptions(resolved, "target", resourceLimit);
        SvgSceneExtractionResult command = scene.ExtractFragmentWithOptions(resolved, "target", commandLimit);

        Assert.Multiple(() =>
        {
            AssertFailure(missing, SvgSceneExtractionError.TargetNotFound);
            AssertFailure(invalidInstance, SvgSceneExtractionError.InvalidArgument);
            AssertFailure(absentInstance, SvgSceneExtractionError.TargetNotFound);
            AssertFailure(nodes, SvgSceneExtractionError.LimitExceeded);
            AssertFailure(resources, SvgSceneExtractionError.LimitExceeded);
            AssertFailure(command, SvgSceneExtractionError.LimitExceeded);
            Assert.That(scene.Root, Is.SameAs(root));
            Assert.That(scene.LayerPlan, Is.SameAs(plan));
            Assert.That(scene.RenderCommands, Is.SameAs(commands));
            Assert.That(scene.Success, Is.True);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }

    static bool ContainsResource(System.Collections.Generic.List<SvgSceneResource> resources, SvgSceneResource target)
    {
        int index = 0;
        while (index < resources.Count)
        {
            if (object.ReferenceEquals(resources[index], target)) return true;
            index = index + 1;
        }
        return false;
    }

    static bool ResourcesFollowSourceOrder(System.Collections.Generic.List<SvgSceneResource> source, System.Collections.Generic.List<SvgSceneResource> extracted)
    {
        int sourceIndex = 0;
        int extractedIndex = 0;
        while (sourceIndex < source.Count && extractedIndex < extracted.Count)
        {
            if (object.ReferenceEquals(source[sourceIndex], extracted[extractedIndex])) extractedIndex = extractedIndex + 1;
            sourceIndex = sourceIndex + 1;
        }
        return extractedIndex == extracted.Count;
    }

    static bool AllCommandResourcesRetained(SvgScene scene)
    {
        int index = 0;
        while (index < scene.RenderCommands.Commands.Count)
        {
            SvgRenderCommand command = scene.RenderCommands.Commands[index];
            if (command.Resource != null && !ContainsResource(scene.Resources, command.Resource)) return false;
            if (command.MarkerStart != null && !ContainsResource(scene.Resources, command.MarkerStart)) return false;
            if (command.MarkerMid != null && !ContainsResource(scene.Resources, command.MarkerMid)) return false;
            if (command.MarkerEnd != null && !ContainsResource(scene.Resources, command.MarkerEnd)) return false;
            index = index + 1;
        }
        return true;
    }

    static void AssertFailure(SvgSceneExtractionResult result, SvgSceneExtractionError error)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(error));
        Assert.That(result.Scene, Is.Null);
        Assert.That(result.Target, Is.Null);
    }

    static void AssertMatrix(SvgMatrix actual, SvgMatrix expected)
    {
        Assert.That(actual.A, Is.EqualTo(expected.A).Within(0.000000001));
        Assert.That(actual.B, Is.EqualTo(expected.B).Within(0.000000001));
        Assert.That(actual.C, Is.EqualTo(expected.C).Within(0.000000001));
        Assert.That(actual.D, Is.EqualTo(expected.D).Within(0.000000001));
        Assert.That(actual.E, Is.EqualTo(expected.E).Within(0.000000001));
        Assert.That(actual.F, Is.EqualTo(expected.F).Within(0.000000001));
    }

    static void AssertRect(SvgRect actual, SvgRect expected)
    {
        Assert.That(actual.IsEmpty, Is.EqualTo(expected.IsEmpty));
        Assert.That(actual.X, Is.EqualTo(expected.X).Within(0.000000001));
        Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(0.000000001));
        Assert.That(actual.Width, Is.EqualTo(expected.Width).Within(0.000000001));
        Assert.That(actual.Height, Is.EqualTo(expected.Height).Within(0.000000001));
    }
}
