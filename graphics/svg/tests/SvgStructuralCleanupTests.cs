namespace svg.Tests;

public class SvgStructuralCleanupTests
{
    [Test]
    public void CollapseJobRemovesOnlyNeutralGroupsAndCombinesTransforms()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g><rect id='plain' width='2' height='2'/></g><g transform='translate(10 0)'> <path id='combined' transform='scale(2)' d='M0 0h1v1z'/> </g><rect id='list' transform='translate(1) scale(2)' width='1' height='1'/><g id='identified'><rect/></g><g opacity='.5'><rect/></g><g transform='translate(2)'><rect/><rect/></g><switch><g><rect/></g></switch><g><title>Named</title><rect/></g></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().CollapseGroups());
        SvgProcessResult second = Process(document, new SvgProcessJobs().CollapseGroups());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(document.Index.GetById("plain").Parent, Is.SameAs(document.DocumentElement));
            Assert.That(document.Index.GetById("combined").Parent, Is.SameAs(document.DocumentElement));
            Assert.That(Attribute(document.Index.GetById("combined"), SvgAttributeNames.Transform), Is.EqualTo("matrix(2 0 0 2 10 0)"));
            Assert.That(Attribute(document.Index.GetById("list"), SvgAttributeNames.Transform), Is.EqualTo("matrix(2 0 0 2 1 0)"));
            Assert.That(AllNodes(document).Count(node => node.ElementKind == SvgElementKind.G), Is.EqualTo(5));
            Assert.That(document.Index.GetById("identified"), Is.Not.Null);
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "collapse-groups"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void AttributeJobHoistsCommonValuesAndSinksSingleChildValuesForCollapse()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><g id='hoist'><path d='M0 0h1' fill='red' stroke='blue'/><path d='M2 0h1' fill='red' stroke='blue'/></g><g fill='green' transform='translate(5)'><path id='sunk' d='M0 0h1'/></g><g id='context' fill='currentColor'><path d='M0 0h1'/></g><g id='different'><path d='M0 0h1' fill='red'/><path d='M2 0h1' fill='blue'/></g><g id='effect' opacity='.5'><path d='M0 0h1'/></g><g fill='red'><path id='inherited' d='M0 0h1' fill='inherit'/></g></svg>";
        SvgDocument document = Load(source);
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(new SvgProcessJobs().CollapseGroups());
        options.Jobs.Add(new SvgProcessJobs().HoistCommonAttributes());

        SvgProcessResult first = document.ProcessWithOptions(options);
        SvgProcessResult second = document.ProcessWithOptions(options);
        SvgNode hoist = document.Index.GetById("hoist");
        SvgNode sunk = document.Index.GetById("sunk");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(Attribute(hoist, "fill"), Is.EqualTo("red"));
            Assert.That(Attribute(hoist, "stroke"), Is.EqualTo("blue"));
            Assert.That(hoist.Children.Where(node => node.ElementKind == SvgElementKind.Path), Has.All.Matches<SvgNode>(path => FindAttribute(path, "fill") == null && FindAttribute(path, "stroke") == null));
            Assert.That(sunk.Parent, Is.SameAs(document.DocumentElement));
            Assert.That(Attribute(sunk, "fill"), Is.EqualTo("green"));
            Assert.That(Attribute(sunk, SvgAttributeNames.Transform), Is.EqualTo("translate(5)"));
            Assert.That(document.Index.GetById("context").Attributes.Any(attribute => attribute.LocalName == "fill" && attribute.Value == "currentColor"), Is.True);
            Assert.That(document.Index.GetById("different").Children.Where(node => node.ElementKind == SvgElementKind.Path).Select(path => Attribute(path, "fill")), Is.EqualTo(new[] { "red", "blue" }));
            Assert.That(document.Index.GetById("effect").Parent, Is.Not.Null);
            Assert.That(document.Index.GetById("inherited").Parent.ElementKind, Is.EqualTo(SvgElementKind.G));
            Assert.That(Attribute(document.Index.GetById("inherited").Parent, "fill"), Is.EqualTo("red"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("hoist-common-attributes"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("collapse-groups"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void PathJobMergesOnlyAdjacentEquivalentDisjointSolidFills()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><marker id='m'><path d='M0 0h1'/></marker></defs><g id='merge'><path d='M0 0H10V10H0Z' fill='red' stroke='none'/><!--gap--><path d='M20 0H30V10H20Z' fill='red' stroke='none'/><path d='M25 0H35V10H25Z' fill='red' stroke='none'/></g><g id='identified'><path id='one' d='M0 20H5V25H0Z' fill='red' stroke='none'/><path d='M10 20H15V25H10Z' fill='red' stroke='none'/></g><g id='touching'><path d='M0 30H10V40H0Z' fill='red' stroke='none'/><path d='M10 30H20V40H10Z' fill='red' stroke='none'/></g><g id='paint'><path d='M0 50H5V55H0Z' fill='url(#paint)' stroke='none'/><path d='M10 50H15V55H10Z' fill='url(#paint)' stroke='none'/></g><g id='stroke'><path d='M0 60H5V65H0Z' fill='red' stroke='blue'/><path d='M10 60H15V65H10Z' fill='red' stroke='blue'/></g><g id='markers' marker-start='url(#m)'><path d='M0 70H5V75H0Z' fill='red' stroke='none'/><path d='M10 70H15V75H10Z' fill='red' stroke='none'/></g><g id='inline-markers' style='marker-end:url(#m)'><path d='M0 80H5V85H0Z' fill='red' stroke='none'/><path d='M10 80H15V85H10Z' fill='red' stroke='none'/></g></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().MergeAdjacentPaths());
        SvgProcessResult second = Process(document, new SvgProcessJobs().MergeAdjacentPaths());
        SvgNode merge = document.Index.GetById("merge");
        List<SvgNode> mergedPaths = merge.Children.Where(node => node.ElementKind == SvgElementKind.Path).ToList();

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(mergedPaths, Has.Count.EqualTo(2));
            Assert.That(Attribute(mergedPaths[0], SvgAttributeNames.D), Is.EqualTo("M0 0H10V10H0Z M20 0H30V10H20Z"));
            Assert.That(first.Changes.Count(change => change.Kind == SvgChangeKind.AttributeReplace), Is.EqualTo(1));
            Assert.That(first.Changes.Count(change => change.Kind == SvgChangeKind.NodeRemove), Is.EqualTo(1));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "merge-adjacent-paths" && change.Reason == "Merge adjacent paths with equivalent paint and disjoint geometry."));
            Assert.That(PathCount(document.Index.GetById("identified")), Is.EqualTo(2));
            Assert.That(PathCount(document.Index.GetById("touching")), Is.EqualTo(2));
            Assert.That(PathCount(document.Index.GetById("paint")), Is.EqualTo(2));
            Assert.That(PathCount(document.Index.GetById("stroke")), Is.EqualTo(2));
            Assert.That(PathCount(document.Index.GetById("markers")), Is.EqualTo(2));
            Assert.That(PathCount(document.Index.GetById("inline-markers")), Is.EqualTo(2));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void StructuralJobsPreserveStylesScriptsAnimationsEffectsAndStackingBoundaries()
    {
        string candidates = "<g><path d='M0 0H1V1H0Z' fill='red' stroke='none'/><path d='M2 0H3V1H2Z' fill='red' stroke='none'/></g>";
        SvgDocument styled = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>g > path{fill:blue}</style>" + candidates + "</svg>");
        SvgDocument scripted = Load("<svg xmlns='http://www.w3.org/2000/svg'><script>observe()</script>" + candidates + "</svg>");
        SvgDocument animated = Load("<svg xmlns='http://www.w3.org/2000/svg'><animate attributeName='x' values='0;1'/>" + candidates + "</svg>");
        SvgDocument effects = Load("<svg xmlns='http://www.w3.org/2000/svg'><g opacity='.5'><path d='M0 0h1'/></g><g filter='url(#f)'><path d='M0 0h1'/></g><g transform='translate(2)'><path d='M0 0h1'/><path d='M2 0h1'/></g></svg>");

        SvgProcessResult styledResult = styled.ProcessWithOptions(StructuralOptions());
        SvgProcessResult scriptedResult = scripted.ProcessWithOptions(StructuralOptions());
        SvgProcessResult animatedResult = animated.ProcessWithOptions(StructuralOptions());
        SvgProcessResult effectsResult = effects.ProcessWithOptions(StructuralOptions());

        Assert.Multiple(() =>
        {
            Assert.That(styledResult.Changes, Is.Empty);
            Assert.That(scriptedResult.Changes, Is.Empty);
            Assert.That(animatedResult.Changes, Is.Empty);
            Assert.That(effectsResult.Changes, Is.Empty);
            Assert.That(AllNodes(styled).Count(node => node.ElementKind == SvgElementKind.G), Is.EqualTo(1));
            Assert.That(AllNodes(scripted).Count(node => node.ElementKind == SvgElementKind.G), Is.EqualTo(1));
            Assert.That(AllNodes(animated).Count(node => node.ElementKind == SvgElementKind.G), Is.EqualTo(1));
            Assert.That(AllNodes(effects).Count(node => node.ElementKind == SvgElementKind.G), Is.EqualTo(3));
        });
    }

    [Test]
    public void PresetsMaterializeStructuralJobsInDeclaredOrder()
    {
        SvgProcessOptions options = new SvgProcessPresets().SafeDefault();
        SvgProcessJob collapse = options.Jobs.Single(job => job.Kind == SvgProcessJobKind.CollapseGroups);
        SvgProcessJob hoist = options.Jobs.Single(job => job.Kind == SvgProcessJobKind.HoistCommonAttributes);
        SvgProcessJob merge = options.Jobs.Single(job => job.Kind == SvgProcessJobKind.MergeAdjacentPaths);

        Assert.Multiple(() =>
        {
            Assert.That(options.Jobs.IndexOf(collapse), Is.LessThan(options.Jobs.IndexOf(hoist)));
            Assert.That(options.Jobs.IndexOf(hoist), Is.LessThan(options.Jobs.IndexOf(merge)));
            Assert.That(new[] { collapse, hoist, merge }, Has.All.Matches<SvgProcessJob>(job => job.PreservesAccessibility && job.PreservesViewBox && !job.SemanticLoss));
        });
    }

    static SvgProcessOptions StructuralOptions()
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(new SvgProcessJobs().CollapseGroups());
        options.Jobs.Add(new SvgProcessJobs().HoistCommonAttributes());
        options.Jobs.Add(new SvgProcessJobs().MergeAdjacentPaths());
        return options;
    }

    static int PathCount(SvgNode node)
    {
        return node.Children.Count(child => child.ElementKind == SvgElementKind.Path);
    }

    static SvgAttribute FindAttribute(SvgNode node, string name)
    {
        return node.Attributes.FirstOrDefault(attribute => attribute.NamespaceUri.Length == 0 && attribute.LocalName == name);
    }

    static string Attribute(SvgNode node, string name)
    {
        return node.Attributes.Single(attribute => attribute.NamespaceUri.Length == 0 && attribute.LocalName == name).Value;
    }

    static SvgProcessResult Process(SvgDocument document, SvgProcessJob job)
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(job);
        return document.ProcessWithOptions(options);
    }

    static List<SvgNode> AllNodes(SvgDocument document)
    {
        List<SvgNode> result = new List<SvgNode>();
        List<SvgNode> pending = new List<SvgNode> { document.DocumentNode };
        while (pending.Count != 0)
        {
            SvgNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            result.Add(node);
            pending.AddRange(node.Children);
        }
        return result;
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
