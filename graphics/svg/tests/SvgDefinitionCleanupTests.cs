namespace svg.Tests;

public class SvgDefinitionCleanupTests
{
    [Test]
    public void UnusedDefinitionCleanupRetainsReachableGraphsAndRemovesDeadCycles()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:vendor='urn:vendor'><defs><linearGradient id='base'><stop offset='0' stop-color='red'/></linearGradient><linearGradient id='live' href='#base'/><pattern id='dead-a' href='#dead-b'/><pattern id='dead-b' href='#dead-a'/><g id='bundle'><path id='child' d='M0 0h1'/></g><path id='unused' d='M0 0h2'/><symbol id='symbol'><rect width='1' height='1'/></symbol><marker id='marker'><path d='M0 0h1'/></marker><clipPath id='clip'><rect width='1' height='1'/></clipPath><mask id='mask'><rect width='1' height='1'/></mask><filter id='filter'><feGaussianBlur stdDeviation='1'/></filter><image id='image' href='asset.png'/><g id='semantic' role='group'/><vendor:data id='vendor'/><animate href='#outside' attributeName='x' values='0;1'/></defs><linearGradient id='outside-dead'/><linearGradient id='outside-live'/><style>.paint{filter:url(#filter)}</style><rect id='outside' class='paint' fill='url(#live)' stroke='url(#outside-live)' marker-start='url(#marker)' clip-path='url(#clip)' mask='url(#mask)'/><use href='#child'/><use href='#symbol'/><use href='#image'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().RemoveUnusedDefinitions());
        SvgProcessResult second = Process(document, new SvgProcessJobs().RemoveUnusedDefinitions());

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(first.Changes, Has.Count.EqualTo(4));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "remove-unused-definitions" && change.Kind == SvgChangeKind.NodeRemove));
            Assert.That(document.Index.GetById("dead-a"), Is.Null);
            Assert.That(document.Index.GetById("dead-b"), Is.Null);
            Assert.That(document.Index.GetById("unused"), Is.Null);
            Assert.That(document.Index.GetById("outside-dead"), Is.Null);
            Assert.That(new[] { "base", "live", "bundle", "child", "symbol", "marker", "clip", "mask", "filter", "image", "semantic", "vendor", "outside-live" }, Has.All.Matches<string>(id => document.Index.GetById(id) != null));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void DefinitionDeduplicationCoversEverySupportedResourceAndRewritesReferences()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='linear-a'><stop offset='0' stop-color='red'/></linearGradient><linearGradient id='linear-b'><stop offset='0' stop-color='red'/></linearGradient><radialGradient id='radial-a'><stop offset='1' stop-color='blue'/></radialGradient><radialGradient id='radial-b'><stop offset='1' stop-color='blue'/></radialGradient><pattern id='pattern-a' width='2' height='2'><rect width='2' height='2'/></pattern><pattern id='pattern-b' width='2' height='2'><rect width='2' height='2'/></pattern><marker id='marker-a'><path d='M0 0h1'/></marker><marker id='marker-b'><path d='M0 0h1'/></marker><clipPath id='clip-a'><rect width='2' height='2'/></clipPath><clipPath id='clip-b'><rect width='2' height='2'/></clipPath><mask id='mask-a'><rect width='2' height='2'/></mask><mask id='mask-b'><rect width='2' height='2'/></mask><filter id='filter-a'><feGaussianBlur stdDeviation='1'/></filter><filter id='filter-b'><feGaussianBlur stdDeviation='1'/></filter><image id='image-a' href='asset.png' width='2' height='2'/><image id='image-b' href='asset.png' width='2' height='2'/></defs><style>.paint{fill:url(#linear-b)}</style><style>.paint{fill:url(#linear-b)}</style><rect id='target' class='paint' stroke='url( #radial-b )' fill='url(&quot;#pattern-b&quot;)' marker-start='url(#marker-b)' clip-path='url(#clip-b)' mask='url(#mask-b)' filter='url(#filter-b)' aria-describedby='image-b'/><use id='copy' href=' #image-b '/><animate id='timing' attributeName='x' begin='filter-b.click'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult result = Process(document, new SvgProcessJobs().DeduplicateDefinitions());
        SvgProcessResult repeat = Process(document, new SvgProcessJobs().DeduplicateDefinitions());
        SvgNode target = document.Index.GetById("target");
        string css = AllNodes(document).Where(node => node.ElementKind == SvgElementKind.Style).Select(node => new SvgNodeApi().GetTextContent(node)).Single();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(new[] { "linear-b", "radial-b", "pattern-b", "marker-b", "clip-b", "mask-b", "filter-b", "image-b" }, Has.All.Matches<string>(id => document.Index.GetById(id) == null));
            Assert.That(new[] { "linear-a", "radial-a", "pattern-a", "marker-a", "clip-a", "mask-a", "filter-a", "image-a" }, Has.All.Matches<string>(id => document.Index.GetById(id) != null));
            Assert.That(Attribute(target, "stroke"), Is.EqualTo("url( #radial-a )"));
            Assert.That(Attribute(target, "fill"), Is.EqualTo("url(&quot;#pattern-a&quot;)".Replace("&quot;", "\"")));
            Assert.That(Attribute(target, "marker-start"), Is.EqualTo("url(#marker-a)"));
            Assert.That(Attribute(target, "clip-path"), Is.EqualTo("url(#clip-a)"));
            Assert.That(Attribute(target, "mask"), Is.EqualTo("url(#mask-a)"));
            Assert.That(Attribute(target, "filter"), Is.EqualTo("url(#filter-a)"));
            Assert.That(Attribute(target, "aria-describedby"), Is.EqualTo("image-a"));
            Assert.That(Attribute(document.Index.GetById("copy"), SvgAttributeNames.Href), Is.EqualTo(" #image-a "));
            Assert.That(Attribute(document.Index.GetById("timing"), "begin"), Is.EqualTo("filter-a.click"));
            Assert.That(css, Is.EqualTo(".paint{fill:url(#linear-a)}"));
            Assert.That(result.Changes.Count(change => change.Kind == SvgChangeKind.NodeRemove), Is.EqualTo(9));
            Assert.That(result.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "deduplicate-definitions" && change.Reason == "Deduplicate an equivalent definition and rewrite its references."));
            Assert.That(repeat.Success, Is.True, Messages(repeat));
            Assert.That(repeat.Changes, Is.Empty);
        });
    }

    [Test]
    public void DeduplicationConvergesWhenEquivalentResourcesDependOnEquivalentBases()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='derived-a' href='#base-a'/><linearGradient id='derived-b' href='#base-b'/><linearGradient id='base-a'><stop offset='0'/></linearGradient><linearGradient id='base-b'><stop offset='0'/></linearGradient></defs><rect fill='url(#derived-b)'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult result = Process(document, new SvgProcessJobs().DeduplicateDefinitions());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Converged, Is.True);
            Assert.That(result.PassCount, Is.EqualTo(3));
            Assert.That(document.Index.GetById("base-b"), Is.Null);
            Assert.That(document.Index.GetById("derived-b"), Is.Null);
            Assert.That(Attribute(document.DocumentElement.Children.Last(), "fill"), Is.EqualTo("url(#derived-a)"));
            Assert.That(result.Changes.Count(change => change.Kind == SvgChangeKind.NodeRemove), Is.EqualTo(2));
        });
    }

    [Test]
    public void CleanupRetainsDefinitionsWithScriptsSelectorsUnknownReferencesOrDistinctContent()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='selected'><stop offset='0'/></linearGradient><linearGradient id='selected-copy'><stop offset='0'/></linearGradient><linearGradient id='different'><stop offset='1'/></linearGradient><linearGradient id='complex:id'><stop offset='0'/></linearGradient></defs><style>#selected-copy{color:red}</style><rect data-resource='selected-copy'/><style>.a{fill:red}</style><rect/><style>.a{fill:red}</style></svg>";
        SvgDocument document = Load(source);
        SvgDocument scripted = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='unused'/><linearGradient id='same'/><linearGradient id='same-copy'/></defs><script>getElementById('unused')</script></svg>");

        SvgProcessResult deduplicate = Process(document, new SvgProcessJobs().DeduplicateDefinitions());
        SvgProcessResult scriptedUnused = Process(scripted, new SvgProcessJobs().RemoveUnusedDefinitions());
        SvgProcessResult scriptedDedupe = Process(scripted, new SvgProcessJobs().DeduplicateDefinitions());

        Assert.Multiple(() =>
        {
            Assert.That(deduplicate.Success, Is.True, Messages(deduplicate));
            Assert.That(new[] { "selected", "selected-copy", "different", "complex:id" }, Has.All.Matches<string>(id => document.Index.GetById(id) != null));
            Assert.That(AllNodes(document).Count(node => node.ElementKind == SvgElementKind.Style && new SvgNodeApi().GetTextContent(node) == ".a{fill:red}"), Is.EqualTo(2));
            Assert.That(scriptedUnused.Changes, Is.Empty);
            Assert.That(scriptedDedupe.Changes, Is.Empty);
            Assert.That(new[] { "unused", "same", "same-copy" }, Has.All.Matches<string>(id => scripted.Index.GetById(id) != null));
        });
    }

    [Test]
    public void PresetsMaterializeBothDefinitionJobsInDeclaredOrder()
    {
        SvgProcessOptions options = new SvgProcessPresets().SafeDefault();
        int unusedOperation = options.Operations.IndexOf(SvgProcessOperation.RemoveUnusedDefinitions);
        int deduplicateOperation = options.Operations.IndexOf(SvgProcessOperation.DeduplicateDefinitions);
        int unusedJob = options.Jobs.FindIndex(job => job.Kind == SvgProcessJobKind.RemoveUnusedDefinitions);
        int deduplicateJob = options.Jobs.FindIndex(job => job.Kind == SvgProcessJobKind.DeduplicateDefinitions);

        Assert.Multiple(() =>
        {
            Assert.That(unusedOperation, Is.GreaterThanOrEqualTo(0));
            Assert.That(deduplicateOperation, Is.GreaterThan(unusedOperation));
            Assert.That(unusedJob, Is.GreaterThanOrEqualTo(0));
            Assert.That(deduplicateJob, Is.GreaterThan(unusedJob));
            Assert.That(options.Jobs[unusedJob].PreservesAccessibility, Is.True);
            Assert.That(options.Jobs[deduplicateJob].PreservesViewBox, Is.True);
        });
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
