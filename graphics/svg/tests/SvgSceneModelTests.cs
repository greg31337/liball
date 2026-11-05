namespace svg.Tests;

public class SvgSceneModelTests
{
    [Test]
    public void NodeTagsCreateExactlyOneMatchingPayloadWithDeterministicDefaults()
    {
        SvgSceneNode group = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgSceneNode path = new SvgSceneNode(SvgSceneNodeKind.Path);
        SvgSceneNode text = new SvgSceneNode(SvgSceneNodeKind.Text);
        SvgSceneNode image = new SvgSceneNode(SvgSceneNodeKind.Image);

        Assert.Multiple(() =>
        {
            Assert.That(group.HasValidTag(), Is.True);
            Assert.That(group.Group, Is.Not.Null);
            Assert.That(group.Path, Is.Null);
            Assert.That(path.HasValidTag(), Is.True);
            Assert.That(path.Path.FillRule, Is.EqualTo(SvgFillRule.Nonzero));
            Assert.That(text.HasValidTag(), Is.True);
            Assert.That(text.Text.Text, Is.Empty);
            Assert.That(image.HasValidTag(), Is.True);
            Assert.That(image.Image.ContentTransform.A, Is.EqualTo(1));
            Assert.That(image.Image.AspectRatio.Alignment, Is.EqualTo(SvgAspectRatioAlignment.XMidYMid));
            Assert.That(group.Opacity, Is.EqualTo(1));
            Assert.That(group.BlendMode, Is.EqualTo("normal"));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSceneNode((SvgSceneNodeKind)99));
        });

        path.Group = new SvgSceneGroupData();
        Assert.That(path.HasValidTag(), Is.False);
    }

    [Test]
    public void GroupTreeOperationsPreventCyclesAndSceneRegistrationUsesPreorder()
    {
        SvgSceneNode root = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgSceneNode group = new SvgSceneNode(SvgSceneNodeKind.Group);
        SvgSceneNode first = new SvgSceneNode(SvgSceneNodeKind.Path);
        SvgSceneNode second = new SvgSceneNode(SvgSceneNodeKind.Text);
        SvgScene scene = new SvgScene();

        bool firstAttached = root.AddChild(first);
        bool groupAttached = root.AddChild(group);
        bool secondAttached = group.AddChild(second);
        bool duplicate = group.AddChild(first);
        bool cycle = group.AddChild(root);
        bool leafChild = first.AddChild(new SvgSceneNode(SvgSceneNodeKind.Image));
        bool registered = scene.SetRoot(root);

        Assert.Multiple(() =>
        {
            Assert.That(firstAttached, Is.True);
            Assert.That(groupAttached, Is.True);
            Assert.That(secondAttached, Is.True);
            Assert.That(duplicate, Is.False);
            Assert.That(cycle, Is.False);
            Assert.That(leafChild, Is.False);
            Assert.That(registered, Is.True);
            Assert.That(scene.Root, Is.SameAs(root));
            Assert.That(scene.Nodes, Is.EqualTo(new[] { root, first, group, second }));
            Assert.That(second.Parent, Is.SameAs(group));
        });

        Assert.That(group.RemoveChild(second), Is.True);
        Assert.That(second.Parent, Is.Null);
        Assert.That(group.RemoveChild(second), Is.False);
    }

    [Test]
    public void ResourceTagsSupportSharedLookupAndRejectDuplicateOrMalformedEntries()
    {
        SvgSceneResource[] resources = new[]
        {
            new SvgSceneResource("solid", SvgSceneResourceKind.SolidPaint),
            new SvgSceneResource("gradient", SvgSceneResourceKind.GradientPaint),
            new SvgSceneResource("pattern", SvgSceneResourceKind.PatternPaint),
            new SvgSceneResource("image", SvgSceneResourceKind.Image),
            new SvgSceneResource("marker", SvgSceneResourceKind.Marker),
            new SvgSceneResource("clip", SvgSceneResourceKind.Clip),
            new SvgSceneResource("mask", SvgSceneResourceKind.Mask),
            new SvgSceneResource("filter", SvgSceneResourceKind.Filter)
        };
        SvgScene scene = new SvgScene();
        List<bool> added = new List<bool>();
        foreach (SvgSceneResource resource in resources) added.Add(scene.AddResource(resource));

        SvgSceneResource duplicate = new SvgSceneResource("solid", SvgSceneResourceKind.SolidPaint);
        SvgSceneResource malformed = new SvgSceneResource("bad", SvgSceneResourceKind.Image);
        malformed.SolidPaint = new SvgSceneSolidPaintData();

        Assert.Multiple(() =>
        {
            Assert.That(resources.All(resource => resource.HasValidTag()), Is.True);
            Assert.That(added, Is.All.True);
            Assert.That(scene.Resources, Has.Count.EqualTo(8));
            Assert.That(scene.FindResource("gradient"), Is.SameAs(resources[1]));
            Assert.That(scene.FindResource("missing"), Is.Null);
            Assert.That(scene.AddResource(duplicate), Is.False);
            Assert.That(scene.AddResource(malformed), Is.False);
            Assert.That(resources[0].SolidPaint, Is.Not.Null);
            Assert.That(resources[1].GradientPaint, Is.Not.Null);
            Assert.That(resources[2].PatternPaint, Is.Not.Null);
            Assert.That(resources[3].Image, Is.Not.Null);
            Assert.That(resources[4].Marker, Is.Not.Null);
            Assert.That(resources[5].Clip, Is.Not.Null);
            Assert.That(resources[6].Mask, Is.Not.Null);
            Assert.That(resources[7].Filter, Is.Not.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSceneResource("", SvgSceneResourceKind.Image));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SvgSceneResource("bad-kind", (SvgSceneResourceKind)99));
        });
    }

    [Test]
    public void ResourcePayloadCollectionsAndContentGraphsAreIndependent()
    {
        SvgSceneResource firstGradient = new SvgSceneResource("g1", SvgSceneResourceKind.GradientPaint);
        SvgSceneResource secondGradient = new SvgSceneResource("g2", SvgSceneResourceKind.GradientPaint);
        SvgSceneResource filter = new SvgSceneResource("f", SvgSceneResourceKind.Filter);
        SvgSceneResource pattern = new SvgSceneResource("p", SvgSceneResourceKind.PatternPaint);
        SvgSceneGradientStopData stop = new SvgSceneGradientStopData();
        stop.Offset = 0.5;
        firstGradient.GradientPaint.Stops.Add(stop);
        SvgSceneFilterPrimitiveData primitive = new SvgSceneFilterPrimitiveData();
        primitive.Index = 2;
        primitive.ElementKind = SvgElementKind.FeGaussianBlur;
        filter.Filter.Primitives.Add(primitive);
        pattern.PatternPaint.Content = new SvgSceneNode(SvgSceneNodeKind.Group);

        Assert.Multiple(() =>
        {
            Assert.That(firstGradient.GradientPaint.Stops, Has.Count.EqualTo(1));
            Assert.That(secondGradient.GradientPaint.Stops, Is.Empty);
            Assert.That(firstGradient.GradientPaint.Transform.A, Is.EqualTo(1));
            Assert.That(filter.Filter.Primitives.Single().Index, Is.EqualTo(2));
            Assert.That(filter.Filter.Units, Is.EqualTo(SvgPaintServerUnits.ObjectBoundingBox));
            Assert.That(pattern.PatternPaint.Content.Kind, Is.EqualTo(SvgSceneNodeKind.Group));
            Assert.That(pattern.PatternPaint.AspectRatio.Scale, Is.EqualTo(SvgAspectRatioScale.Meet));
        });
    }

    [Test]
    public void SceneIdentityLookupAndCurrentChecksTrackSourceVersions()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect width='10' height='5'/></svg>");
        SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(document);
        SvgResolvedNode resolvedRectangle = resolved.FindSource(document.DocumentElement.Children[0]);
        SvgScene scene = SvgScene.Create(document);
        SvgSceneNode root = new SvgSceneNode(SvgSceneNodeKind.Group);
        root.SetIdentity(resolved.Root);
        SvgSceneNode rectangle = new SvgSceneNode(SvgSceneNodeKind.Path);
        rectangle.SetIdentity(resolvedRectangle);
        root.AddChild(rectangle);
        scene.SetRoot(root);
        SvgSceneResource paint = new SvgSceneResource("paint", SvgSceneResourceKind.SolidPaint);
        paint.SetSource(document.DocumentElement.Children[0]);
        scene.AddResource(paint);

        Assert.Multiple(() =>
        {
            Assert.That(scene.IsCurrent(), Is.True);
            Assert.That(scene.FindNode(document.DocumentElement.Children[0], ""), Is.SameAs(rectangle));
            Assert.That(rectangle.SourcePath, Is.EqualTo("0/0"));
            Assert.That(paint.IsCurrent(), Is.True);
        });

        document.DocumentElement.Children[0].MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(scene.IsCurrent(), Is.False);
            Assert.That(paint.IsCurrent(), Is.False);
        });
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True);
        return result.Document;
    }
}
