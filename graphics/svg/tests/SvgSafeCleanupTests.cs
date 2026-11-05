namespace svg.Tests;

public class SvgSafeCleanupTests
{
    [Test]
    public void SafeDefaultRunsEverySafeCleanupAndIsIdempotent()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:inkscape='http://www.inkscape.org/namespaces/inkscape' xmlns:unused='urn:unused' viewBox='0 0 20 10'><!--discard--><inkscape:namedview/><rect id='defaults' x='0' y='0' opacity='1' fill='black' inkscape:label='draft'/><g id='empty'><g/></g><rect id='gone' display='none'/><rect id='accessible' display='none' aria-label='Retain me'/><defs><path id='referenced' display='none'/></defs><use href='#referenced'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = document.ProcessWithOptions(new SvgProcessPresets().SafeDefault());
        SvgProcessResult second = document.ProcessWithOptions(new SvgProcessPresets().SafeDefault());
        SvgNode defaults = document.Index.GetById("defaults");

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(first.Converged, Is.True);
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-comments"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-editor-metadata"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-default-attributes"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-empty-containers"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-unreferenced-invisible"));
            Assert.That(first.Changes.Select(change => change.JobId), Does.Contain("remove-unused-namespaces"));
            Assert.That(document.DocumentElement.Children.Any(node => node.Kind == SvgNodeKind.Comment), Is.False);
            Assert.That(document.DocumentElement.Children.Any(node => node.NamespaceUri == "http://www.inkscape.org/namespaces/inkscape"), Is.False);
            Assert.That(defaults.Attributes.Any(attribute => attribute.NamespaceUri == "http://www.inkscape.org/namespaces/inkscape"), Is.False);
            Assert.That(defaults.Attributes.Any(attribute => attribute.LocalName == "x" || attribute.LocalName == "y" || attribute.LocalName == "opacity"), Is.False);
            Assert.That(defaults.Attributes.Any(attribute => attribute.LocalName == "fill" && attribute.Value == "black"), Is.True);
            Assert.That(document.Index.GetById("empty"), Is.Null);
            Assert.That(document.Index.GetById("gone"), Is.Null);
            Assert.That(document.Index.GetById("accessible"), Is.Not.Null);
            Assert.That(document.Index.GetById("referenced"), Is.Not.Null);
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:unused"), Is.False);
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.LocalName == SvgAttributeNames.ViewBox), Is.True);
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void EditorCleanupRemovesOnlyRecognizedEditorContent()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:inkscape='http://www.inkscape.org/namespaces/inkscape' xmlns:rdf='http://www.w3.org/1999/02/22-rdf-syntax-ns#' xmlns:vendor='urn:vendor'><metadata><rdf:RDF><rdf:Description>license</rdf:Description></rdf:RDF></metadata><inkscape:namedview/><rect id='shape' inkscape:label='Layer' vendor:state='keep'/><vendor:settings/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult result = Process(document, new SvgProcessJobs().RemoveEditorMetadata());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(AllNodes(document).Any(node => node.NamespaceUri == "http://www.inkscape.org/namespaces/inkscape"), Is.False);
            Assert.That(document.Index.GetById("shape").Attributes.Any(attribute => attribute.Prefix == "inkscape"), Is.False);
            Assert.That(document.Index.GetById("shape").Attributes.Any(attribute => attribute.Prefix == "vendor" && attribute.Value == "keep"), Is.True);
            Assert.That(AllNodes(document).Any(node => node.QualifiedName == "rdf:Description" && new SvgNodeApi().GetTextContent(node) == "license"), Is.True);
            Assert.That(AllNodes(document).Any(node => node.QualifiedName == "vendor:settings"), Is.True);
        });
    }

    [Test]
    public void NamespaceCleanupHonorsUseInheritanceAndShadowing()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:used='urn:used' xmlns:unused='urn:outer' xmlns:qname='urn:qname'><used:item data-type='qname:Type'/><g xmlns:used='urn:used'><used:child/></g><g xmlns:unused='urn:inner'><unused:item/></g></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult result = Process(document, new SvgProcessJobs().RemoveUnusedNamespaces());
        SvgNode firstGroup = document.DocumentElement.Children.Single(node => node.ElementKind == SvgElementKind.G && node.Children.Single().QualifiedName == "used:child");
        SvgNode secondGroup = document.DocumentElement.Children.Single(node => node.ElementKind == SvgElementKind.G && node.Children.Single().QualifiedName == "unused:item");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.QualifiedName == "xmlns" && attribute.Value == SvgNamespaces.Svg), Is.True);
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:used" && attribute.Value == "urn:used"), Is.True);
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:qname" && attribute.Value == "urn:qname"), Is.True);
            Assert.That(document.DocumentElement.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:unused"), Is.False);
            Assert.That(firstGroup.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:used"), Is.False);
            Assert.That(secondGroup.Attributes.Any(attribute => attribute.QualifiedName == "xmlns:unused" && attribute.Value == "urn:inner"), Is.True);
        });
    }

    [Test]
    public void DefaultCleanupIsCatalogDrivenAndConservativeAroundSelectors()
    {
        SvgDocument plain = Load("<svg xmlns='http://www.w3.org/2000/svg' xml:space='default' viewBox='0 0 10 10'><rect id='shape' x='0.0' y='0' opacity='1.0' fill='BLACK'/></svg>");
        SvgDocument styled = Load("<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><style>.x[x='0']{fill:red}</style><rect id='shape' class='x' x='0' y='0' opacity='1'/><use id='copy' href='#shape' xlink:href='#shape'/></svg>");

        SvgProcessResult plainResult = Process(plain, new SvgProcessJobs().RemoveDefaultAttributes());
        SvgProcessResult styledResult = Process(styled, new SvgProcessJobs().RemoveDefaultAttributes());
        SvgNode plainShape = plain.Index.GetById("shape");
        SvgNode styledShape = styled.Index.GetById("shape");

        Assert.Multiple(() =>
        {
            Assert.That(plainResult.Success, Is.True, Messages(plainResult));
            Assert.That(plainShape.Attributes.Any(attribute => attribute.LocalName == "x" || attribute.LocalName == "y" || attribute.LocalName == "opacity"), Is.False);
            Assert.That(plainShape.Attributes.Any(attribute => attribute.LocalName == "fill" && attribute.Value == "BLACK"), Is.True);
            Assert.That(plain.DocumentElement.Attributes.Any(attribute => attribute.NamespaceUri == SvgNamespaces.Xml && attribute.LocalName == SvgAttributeNames.Space), Is.True);
            Assert.That(plain.DocumentElement.Attributes.Any(attribute => attribute.LocalName == SvgAttributeNames.ViewBox), Is.True);
            Assert.That(styledResult.Success, Is.True, Messages(styledResult));
            Assert.That(styledShape.Attributes.Any(attribute => attribute.LocalName == "x"), Is.True);
            Assert.That(styledShape.Attributes.Any(attribute => attribute.LocalName == "y"), Is.True);
            Assert.That(styledShape.Attributes.Any(attribute => attribute.LocalName == "opacity"), Is.True);
            Assert.That(styled.Index.GetById("copy").Attributes.Any(attribute => attribute.NamespaceUri == SvgNamespaces.XLink && attribute.LocalName == SvgAttributeNames.Href), Is.False);
        });
    }

    [Test]
    public void EmptyCleanupRetainsReferencesAccessibilityViewBoxesAndMetadata()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><symbol id='referenced'/><symbol id='viewport' viewBox='0 0 4 4'/><linearGradient id='derived' href='#missing'/></defs><use href='#referenced'/><g id='empty' data-state='unused'><g/></g><g id='accessible' role='group'/><metadata>application data</metadata></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult result = Process(document, new SvgProcessJobs().RemoveEmptyContainers());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.Index.GetById("empty"), Is.Null);
            Assert.That(document.Index.GetById("referenced"), Is.Not.Null);
            Assert.That(document.Index.GetById("viewport"), Is.Not.Null);
            Assert.That(document.Index.GetById("derived"), Is.Not.Null);
            Assert.That(document.Index.GetById("accessible"), Is.Not.Null);
            Assert.That(AllNodes(document).Any(node => node.ElementKind == SvgElementKind.Metadata && new SvgNodeApi().GetTextContent(node) == "application data"), Is.True);
        });
    }

    [Test]
    public void InvisibleCleanupRetainsReferencesStylesAccessibilityAndAnimation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:vendor='urn:vendor'><g id='ancestor' display='none'><rect id='descendant'/></g><use href='#descendant'/><rect id='gone' display='none'/><rect id='zero' opacity='0'/><rect id='inline' style='display:none'/><rect id='accessible' visibility='hidden' aria-label='Retain'/><rect id='animated' display='none'><animate attributeName='x' values='0;1'/></rect><path id='emptyPath' d=''/><rect id='timed' visibility='collapse'/><animate attributeName='x' begin='timed.click'/><rect id='paint' display='none'/><rect fill='url(#paint)'/><g id='inherited' visibility='hidden'><rect/></g><g id='extension' display='none'><vendor:data/></g></svg>";
        SvgDocument document = Load(source);
        SvgDocument styled = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>.hidden{display:none}</style><rect id='authored' display='none'/></svg>");

        SvgProcessResult result = Process(document, new SvgProcessJobs().RemoveUnreferencedInvisibleNodes());
        SvgProcessResult styledResult = Process(styled, new SvgProcessJobs().RemoveUnreferencedInvisibleNodes());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.Index.GetById("gone"), Is.Null);
            Assert.That(document.Index.GetById("zero"), Is.Null);
            Assert.That(document.Index.GetById("inline"), Is.Null);
            Assert.That(document.Index.GetById("emptyPath"), Is.Null);
            Assert.That(document.Index.GetById("ancestor"), Is.Not.Null);
            Assert.That(document.Index.GetById("descendant"), Is.Not.Null);
            Assert.That(document.Index.GetById("accessible"), Is.Not.Null);
            Assert.That(document.Index.GetById("animated"), Is.Not.Null);
            Assert.That(document.Index.GetById("timed"), Is.Not.Null);
            Assert.That(document.Index.GetById("paint"), Is.Not.Null);
            Assert.That(document.Index.GetById("inherited"), Is.Not.Null);
            Assert.That(document.Index.GetById("extension"), Is.Not.Null);
            Assert.That(styledResult.Success, Is.True, Messages(styledResult));
            Assert.That(styledResult.Changes, Is.Empty);
            Assert.That(styled.Index.GetById("authored"), Is.Not.Null);
        });
    }

    [Test]
    public void BuiltInFactoriesDescribeOnlyImplementedPreservingJobs()
    {
        SvgProcessJob[] jobs = new[]
        {
            new SvgProcessJobs().RemoveComments(),
            new SvgProcessJobs().RemoveEditorMetadata(),
            new SvgProcessJobs().RemoveUnusedNamespaces(),
            new SvgProcessJobs().RemoveDefaultAttributes(),
            new SvgProcessJobs().RemoveEmptyContainers(),
            new SvgProcessJobs().RemoveUnreferencedInvisibleNodes(),
            new SvgProcessJobs().RemoveUnusedDefinitions(),
            new SvgProcessJobs().DeduplicateDefinitions(),
            new SvgProcessJobs().CollapseGroups(),
            new SvgProcessJobs().HoistCommonAttributes(),
            new SvgProcessJobs().MergeAdjacentPaths(),
            new SvgProcessJobs().CleanupStyles(),
            new SvgProcessJobs().InlineStyles(),
            new SvgProcessJobs().ResolveCustomProperties(),
            new SvgProcessJobs().CanonicalizeValuesWithNoArguments(),
            new SvgProcessJobs().PrefixIdsWithNoArguments(),
            new SvgProcessJobs().MinifyIdsWithNoArguments(),
            new SvgProcessJobs().ConvertShapesToPathsWithNoArguments(),
            new SvgProcessJobs().BakeTransformsWithNoArguments(),
            new SvgProcessJobs().SimplifyPathsWithNoArguments(),
            new SvgProcessJobs().ConvertTextToPathsWithNoArguments(),
            new SvgProcessJobs().FlattenEffectsWithNoArguments(),
            new SvgProcessJobs().EmbedImagesWithNoArguments(),
            new SvgProcessJobs().RecolorWithNoArguments(),
            new SvgProcessJobs().ConvertColorsToCurrentColor(),
            new SvgProcessJobs().ResizeViewBoxWithNoArguments(),
            new SvgProcessJobs().ConvertCoordinateSystemWithNoArguments(),
            new SvgProcessJobs().CropToDrawingBoundsWithNoArguments(),
            new SvgProcessJobs().FlattenReferencesWithNoArguments(),
            new SvgProcessJobs().ExtractFragmentWithNoArguments(),
            new SvgProcessJobs().SplitSymbolsWithNoArguments(),
            new SvgProcessJobs().MergeDocumentsWithNoArguments(),
            new SvgProcessJobs().BuildSpriteWithNoArguments(),
            new SvgProcessJobs().IsolateNamespaceWithNoArguments()
        };

        Assert.Multiple(() =>
        {
            Assert.That(jobs.Select(job => job.Kind), Is.EqualTo(new[] { SvgProcessJobKind.RemoveComments, SvgProcessJobKind.RemoveEditorMetadata, SvgProcessJobKind.RemoveUnusedNamespaces, SvgProcessJobKind.RemoveDefaultAttributes, SvgProcessJobKind.RemoveEmptyContainers, SvgProcessJobKind.RemoveUnreferencedInvisibleNodes, SvgProcessJobKind.RemoveUnusedDefinitions, SvgProcessJobKind.DeduplicateDefinitions, SvgProcessJobKind.CollapseGroups, SvgProcessJobKind.HoistCommonAttributes, SvgProcessJobKind.MergeAdjacentPaths, SvgProcessJobKind.CleanupStyles, SvgProcessJobKind.InlineStyles, SvgProcessJobKind.ResolveCustomProperties, SvgProcessJobKind.CanonicalizeValues, SvgProcessJobKind.PrefixIds, SvgProcessJobKind.MinifyIds, SvgProcessJobKind.ConvertShapesToPaths, SvgProcessJobKind.BakeTransforms, SvgProcessJobKind.SimplifyPaths, SvgProcessJobKind.ConvertTextToPaths, SvgProcessJobKind.FlattenEffects, SvgProcessJobKind.EmbedImages, SvgProcessJobKind.Recolor, SvgProcessJobKind.ConvertThemeColors, SvgProcessJobKind.ResizeViewBox, SvgProcessJobKind.ConvertCoordinateSystem, SvgProcessJobKind.CropDrawingBounds, SvgProcessJobKind.FlattenReferences, SvgProcessJobKind.ExtractFragment, SvgProcessJobKind.SplitSymbols, SvgProcessJobKind.MergeDocuments, SvgProcessJobKind.BuildSprite, SvgProcessJobKind.IsolateNamespace }));
            Assert.That(jobs, Has.All.Matches<SvgProcessJob>(job => job.Enabled && job.PreservesAccessibility));
            Assert.That(jobs.Where((job, index) => index < 25 || index > 27), Has.All.Matches<SvgProcessJob>(job => job.PreservesViewBox));
            Assert.That(jobs.Skip(25).Take(3), Has.All.Matches<SvgProcessJob>(job => !job.PreservesViewBox));
            Assert.That(jobs.Take(17), Has.All.Matches<SvgProcessJob>(job => !job.SemanticLoss));
            Assert.That(jobs.Skip(17).Take(5), Has.All.Matches<SvgProcessJob>(job => job.SemanticLoss));
            Assert.That(jobs[22].SemanticLoss, Is.False);
            Assert.That(jobs.Skip(23).Take(5), Has.All.Matches<SvgProcessJob>(job => job.SemanticLoss));
            Assert.That(jobs[28].SemanticLoss, Is.False);
            Assert.That(jobs[29].SemanticLoss, Is.True);
            Assert.That(jobs[30].SemanticLoss, Is.False);
            Assert.That(jobs[31].SemanticLoss, Is.True);
            Assert.That(jobs.Skip(32), Has.All.Matches<SvgProcessJob>(job => !job.SemanticLoss));
            Assert.That(jobs.Select(job => job.Id).Distinct().ToList(), Has.Count.EqualTo(jobs.Length));
            Assert.That(new SvgProcessJobs().Create(SvgProcessOperation.CanonicalizeValues), Is.Not.Null);
        });
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
        List<SvgNode> pending = new List<SvgNode>();
        pending.Add(document.DocumentNode);
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
