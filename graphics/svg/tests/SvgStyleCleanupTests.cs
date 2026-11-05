namespace svg.Tests;

public class SvgStyleCleanupTests
{
    [Test]
    public void CleanupJobDeduplicatesDeclarationsAndConvertsSafePresentationValues()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape' fill='green' style='fill:red;fill:blue;stroke:black;opacity:.5!important;'/><text id='shorthand' style='font:italic 12px serif;font-size:20px;'>x</text></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().CleanupStyles());
        SvgProcessResult second = Process(document, new SvgProcessJobs().CleanupStyles());
        SvgNode shape = document.Index.GetById("shape");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(Attribute(shape, "fill"), Is.EqualTo("blue"));
            Assert.That(Attribute(shape, "stroke"), Is.EqualTo("black"));
            Assert.That(Attribute(shape, SvgAttributeNames.Style), Is.EqualTo("opacity:.5!important;"));
            Assert.That(Attribute(document.Index.GetById("shorthand"), SvgAttributeNames.Style), Is.EqualTo("font:italic 12px serif;font-size:20px;"));
            Assert.That(FindAttribute(document.Index.GetById("shorthand"), "font-size"), Is.Null);
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "cleanup-styles"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void CleanupJobPrunesUnusedSelectorsAndMergesAdjacentEquivalentRules()
    {
        string css = ".unused{fill:red;}.used{fill:blue;}.used{stroke:black;}.same{opacity:.5;}.other{opacity:.5;}.dynamic:hover{fill:green;}";
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>" + css + "</style><rect class='used same'/><circle class='other'/></svg>");

        SvgProcessResult first = Process(document, new SvgProcessJobs().CleanupStyles());
        SvgProcessResult second = Process(document, new SvgProcessJobs().CleanupStyles());
        string result = StyleText(document);

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(result, Does.Not.Contain(".unused"));
            Assert.That(result, Does.Contain(".used{fill:blue;stroke:black;}"));
            Assert.That(result, Does.Contain(".same,.other{opacity:.5;}"));
            Assert.That(result, Does.Contain(".dynamic:hover{fill:green;}"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void InlineJobMaterializesWinningStaticRulesAndRemovesStyleElements()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>.shape{fill:red;stroke:blue;}.strong{fill:green!important;}g{color:purple;opacity:.4;}.inherit{opacity:inherit;}</style><rect id='shape' class='shape strong' style='fill:black;'/><g id='group'><path id='child' class='inherit' d='M0 0h1'/></g></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().InlineStyles());
        SvgProcessResult second = Process(document, new SvgProcessJobs().InlineStyles());
        SvgNode shape = document.Index.GetById("shape");
        SvgNode group = document.Index.GetById("group");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(AllNodes(document).Any(node => node.ElementKind == SvgElementKind.Style), Is.False);
            Assert.That(Attribute(shape, SvgAttributeNames.Style), Does.Contain("fill:green!important;"));
            Assert.That(Attribute(shape, SvgAttributeNames.Style), Does.Contain("stroke:blue;"));
            Assert.That(Attribute(group, SvgAttributeNames.Style), Does.Contain("color:purple;"));
            Assert.That(Attribute(document.Index.GetById("child"), SvgAttributeNames.Style), Does.Contain("opacity:inherit;"));
            Assert.That(first.Changes.Select(change => change.JobId), Has.All.EqualTo("inline-styles"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void InlineJobPreservesDynamicMediaAndScriptObservableStyleSheets()
    {
        SvgDocument dynamic = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>.shape:hover{fill:red}</style><rect class='shape'/></svg>");
        SvgDocument media = Load("<svg xmlns='http://www.w3.org/2000/svg'><style media='print'>.shape{fill:red}</style><rect class='shape'/></svg>");
        SvgDocument scripted = Load("<svg xmlns='http://www.w3.org/2000/svg'><script>inspect()</script><style>.shape{fill:red}</style><rect class='shape'/></svg>");
        SvgDocument rollback = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>.shape{fill:red}</style><rect class='shape' style='fill:revert-layer'/></svg>");

        SvgProcessResult dynamicResult = Process(dynamic, new SvgProcessJobs().InlineStyles());
        SvgProcessResult mediaResult = Process(media, new SvgProcessJobs().InlineStyles());
        SvgProcessResult scriptedResult = Process(scripted, new SvgProcessJobs().InlineStyles());
        SvgProcessResult rollbackResult = Process(rollback, new SvgProcessJobs().InlineStyles());

        Assert.Multiple(() =>
        {
            Assert.That(dynamicResult.Changes, Is.Empty);
            Assert.That(mediaResult.Changes, Is.Empty);
            Assert.That(scriptedResult.Changes, Is.Empty);
            Assert.That(rollbackResult.Changes, Is.Empty);
            Assert.That(new[] { dynamic, media, scripted, rollback }, Has.All.Matches<SvgDocument>(item => AllNodes(item).Any(node => node.ElementKind == SvgElementKind.Style)));
        });
    }

    [Test]
    public void VariableJobResolvesStableRuleInlineAndPresentationUses()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><style>svg{--brand:rgb(1 2 3);}.shape{fill:var(--brand);stroke:var(--missing, blue);}</style><rect id='shape' class='shape' style='color:var(--brand);' opacity='var(--alpha,.5)'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().ResolveCustomProperties());
        SvgProcessResult second = Process(document, new SvgProcessJobs().ResolveCustomProperties());
        SvgNode shape = document.Index.GetById("shape");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(StyleText(document), Does.Contain(".shape{fill:rgb(1 2 3);stroke:blue;}"));
            Assert.That(Attribute(shape, SvgAttributeNames.Style), Is.EqualTo("color:rgb(1 2 3);"));
            Assert.That(Attribute(shape, "opacity"), Is.EqualTo(".5"));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(change => change.JobId == "resolve-custom-properties"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void VariableJobPreservesRuleUsesThatResolveDifferentlyAcrossMatches()
    {
        string css = ".a{--tone:red;}.b{--tone:blue;}.a,.b{fill:var(--tone);}";
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>" + css + "</style><rect class='a'/><rect class='b'/></svg>");

        SvgProcessResult result = Process(document, new SvgProcessJobs().ResolveCustomProperties());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(result.Changes, Is.Empty);
            Assert.That(StyleText(document), Is.EqualTo(css));
        });
    }

    [Test]
    public void PresetsMaterializeStyleJobsInTheirDeclaredGroups()
    {
        SvgProcessOptions safe = new SvgProcessPresets().SafeDefault();
        SvgProcessOptions aggressive = new SvgProcessPresets().Aggressive();

        Assert.Multiple(() =>
        {
            Assert.That(safe.Jobs.Select(job => job.Kind), Does.Contain(SvgProcessJobKind.CleanupStyles));
            Assert.That(safe.Jobs.Select(job => job.Kind), Does.Not.Contain(SvgProcessJobKind.InlineStyles));
            Assert.That(aggressive.Jobs.Select(job => job.Kind), Does.Contain(SvgProcessJobKind.InlineStyles));
            Assert.That(aggressive.Jobs.Select(job => job.Kind), Does.Contain(SvgProcessJobKind.ResolveCustomProperties));
            Assert.That(new[] { new SvgProcessJobs().CleanupStyles(), new SvgProcessJobs().InlineStyles(), new SvgProcessJobs().ResolveCustomProperties() }, Has.All.Matches<SvgProcessJob>(job => job.PreservesAccessibility && job.PreservesViewBox && !job.SemanticLoss));
        });
    }

    static string StyleText(SvgDocument document)
    {
        SvgNode style = AllNodes(document).Single(node => node.ElementKind == SvgElementKind.Style);
        return new SvgNodeApi().GetTextContent(style);
    }

    static SvgProcessResult Process(SvgDocument document, SvgProcessJob job)
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(job);
        return document.ProcessWithOptions(options);
    }

    static SvgAttribute FindAttribute(SvgNode node, string name)
    {
        return node.Attributes.FirstOrDefault(attribute => attribute.NamespaceUri.Length == 0 && attribute.LocalName == name);
    }

    static string Attribute(SvgNode node, string name)
    {
        return node.Attributes.Single(attribute => attribute.NamespaceUri.Length == 0 && attribute.LocalName == name).Value;
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
