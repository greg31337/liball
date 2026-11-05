namespace svg.Tests;

public class SvgCompositionProcessJobTests
{
    [Test]
    public void FlattenReferencesExpandsUsesAndInheritedResourcesWithIsolatedIds()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='base'><stop offset='0' stop-color='red'/></linearGradient><linearGradient id='derived' href='#base'/><g id='art'><style>#part{fill:url(#derived)}</style><rect id='part' width='10' height='20' fill='url(#derived)'/></g></defs><use id='copy' href='#art' x='5' y='6'/></svg>";
        SvgDocument document = Load(source);

        SvgProcessResult first = Process(document, new SvgProcessJobs().FlattenReferencesWithNoArguments());
        SvgProcessResult second = Process(document, new SvgProcessJobs().FlattenReferencesWithNoArguments());
        List<SvgNode> nodes = AllNodes(document);
        SvgNode derived = document.Index.GetById("derived");
        SvgNode clone = nodes.Single(item => item.Attributes.Any(attribute => attribute.LocalName == "id" && attribute.Value == "use-1-part"));
        string css = new SvgNodeApi().GetTextContent(nodes.Single(item => item.ElementKind == SvgElementKind.Style && item.Parent != null && item.Parent.Attributes.Any(attribute => attribute.Value == "use-1-art")));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(nodes.Any(item => item.ElementKind == SvgElementKind.Use), Is.False);
            Assert.That(Attribute(derived.Children.Single(), "stop-color"), Is.EqualTo("red"));
            Assert.That(derived.Attributes.Any(item => item.LocalName == "href"), Is.False);
            Assert.That(Attribute(clone, "fill"), Is.EqualTo("url(#derived)"));
            Assert.That(css, Does.Contain("#use-1-part"));
            Assert.That(css, Does.Contain("url(#derived)"));
            Assert.That(nodes.Single(item => item.Attributes.Any(attribute => attribute.Value == "use-1-copy")).Attributes.Single(item => item.LocalName == "transform").Value, Is.EqualTo("matrix(1 0 0 1 5 6)"));
            Assert.That(first.Changes, Has.All.Matches<SvgProcessChange>(item => !item.SemanticLoss && item.Severity == SvgProcessChangeSeverity.Representation));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void FragmentExtractionRetainsDefinitionsAndDropsOtherGraphics()
    {
        SvgFragmentExtractionProcessOptions settings = new SvgFragmentExtractionProcessOptions();
        settings.FragmentIdentifier = "#keep";
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><title>Icon</title><defs><linearGradient id='paint'/></defs><rect id='drop'/><circle id='keep' fill='url(#paint)'/></svg>");

        SvgProcessResult result = Process(document, new SvgProcessJobs().ExtractFragmentWithOptions(settings));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.Index.GetById("keep"), Is.Not.Null);
            Assert.That(document.Index.GetById("drop"), Is.Null);
            Assert.That(document.Index.GetById("paint"), Is.Not.Null);
            Assert.That(document.DocumentElement.Children.Any(item => item.ElementKind == SvgElementKind.Title), Is.True);
            Assert.That(Attribute(document.DocumentElement, "viewBox"), Is.EqualTo("0 0 20 20"));
        });
    }

    [Test]
    public void SymbolSplitCreatesIndependentIsolatedDocuments()
    {
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='paint'/><symbol id='one' viewBox='0 0 10 10'><rect id='shape' fill='url(#paint)'/></symbol><symbol id='two' viewBox='0 0 20 20'><circle id='shape'/></symbol></defs></svg>");
        SvgSymbolSplitProcessOptions settings = new SvgSymbolSplitProcessOptions();

        SvgProcessResult result = Process(document, new SvgProcessJobs().SplitSymbolsWithOptions(settings));

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(settings.OutputDocuments, Has.Count.EqualTo(2));
            Assert.That(Attribute(settings.OutputDocuments[0].DocumentElement, "viewBox"), Is.EqualTo("0 0 10 10"));
            Assert.That(settings.OutputDocuments[0].Index.GetById("symbol-one-shape"), Is.Not.Null);
            Assert.That(settings.OutputDocuments[0].Index.GetById("symbol-one-paint"), Is.Not.Null);
            Assert.That(Attribute(settings.OutputDocuments[0].Index.GetById("symbol-one-shape"), "fill"), Is.EqualTo("url(#symbol-one-paint)"));
            Assert.That(settings.OutputDocuments[1].Index.GetById("symbol-two-shape"), Is.Not.Null);
            Assert.That(settings.OutputDocuments[0], Is.Not.SameAs(settings.OutputDocuments[1]));
        });
    }

    [Test]
    public void MergeDocumentsIsolatesCollisionsAndRewritesLocalReferencesDeterministically()
    {
        SvgDocument target = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape'/></svg>");
        SvgDocument firstInput = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='paint'/></defs><rect id='shape' fill='url(#paint)'/></svg>");
        SvgDocument secondInput = Load("<svg xmlns='http://www.w3.org/2000/svg'><rect id='shape'/></svg>");
        SvgDocumentMergeProcessOptions settings = new SvgDocumentMergeProcessOptions();
        settings.Documents.Add(firstInput);
        settings.Documents.Add(secondInput);

        SvgProcessResult first = Process(target, new SvgProcessJobs().MergeDocumentsWithOptions(settings));
        SvgProcessResult second = Process(target, new SvgProcessJobs().MergeDocumentsWithOptions(settings));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(target.Index.GetById("shape"), Is.Not.Null);
            Assert.That(target.Index.GetById("merge-1-shape"), Is.Not.Null);
            Assert.That(target.Index.GetById("merge-1-paint"), Is.Not.Null);
            Assert.That(target.Index.GetById("merge-2-shape"), Is.Not.Null);
            Assert.That(Attribute(target.Index.GetById("merge-1-shape"), "fill"), Is.EqualTo("url(#merge-1-paint)"));
            Assert.That(AllNodes(target).Count(item => item.Attributes.Any(attribute => attribute.LocalName == "data-svg-merge-index")), Is.EqualTo(2));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void SpriteConstructionBuildsIsolatedSymbolsAndNamespaceAliasUsesCompleteRewriter()
    {
        SvgDocument imported = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 20 20'><rect id='shape'/></svg>");
        SvgDocument sprite = Load("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 10 10'><title>Sprite</title><rect id='shape' aria-label='Shape'/></svg>");
        SvgSpriteProcessOptions settings = new SvgSpriteProcessOptions();
        settings.Documents.Add(imported);

        SvgProcessResult first = Process(sprite, new SvgProcessJobs().BuildSpriteWithOptions(settings));
        SvgProcessResult second = Process(sprite, new SvgProcessJobs().BuildSpriteWithOptions(settings));
        SvgNode defs = sprite.DocumentElement.Children.Single(item => item.ElementKind == SvgElementKind.Defs);
        List<SvgNode> symbols = defs.Children.Where(item => item.ElementKind == SvgElementKind.Symbol).ToList();

        SvgNamespaceIsolationProcessOptions isolation = new SvgNamespaceIsolationProcessOptions();
        isolation.IdOptions.Prefix = "icon-";
        SvgDocument isolated = Load("<svg xmlns='http://www.w3.org/2000/svg'><style>#shape{fill:red}</style><rect id='shape'/><use href='#shape'/></svg>");
        SvgProcessResult isolationResult = Process(isolated, new SvgProcessJobs().IsolateNamespaceWithOptions(isolation));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(symbols, Has.Count.EqualTo(2));
            Assert.That(symbols.Select(item => Attribute(item, "id")), Is.EqualTo(new[] { "sprite-0", "sprite-1" }));
            Assert.That(symbols[0].Attributes.Single(item => item.LocalName == "viewBox").Value, Is.EqualTo("0 0 10 10"));
            Assert.That(symbols[1].Attributes.Single(item => item.LocalName == "viewBox").Value, Is.EqualTo("0 0 20 20"));
            Assert.That(sprite.DocumentElement.Children.Any(item => item.ElementKind == SvgElementKind.Rect), Is.False);
            Assert.That(sprite.DocumentElement.Children.Any(item => item.ElementKind == SvgElementKind.Title), Is.True);
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
            Assert.That(isolationResult.Success, Is.True, Messages(isolationResult));
            Assert.That(isolated.Index.GetById("icon-shape"), Is.Not.Null);
            Assert.That(AllNodes(isolated).Single(item => item.ElementKind == SvgElementKind.Use).Attributes.Single(item => item.LocalName == "href").Value, Is.EqualTo("#icon-shape"));
            Assert.That(new SvgNodeApi().GetTextContent(AllNodes(isolated).Single(item => item.ElementKind == SvgElementKind.Style)), Does.Contain("#icon-shape"));
        });
    }

    static SvgProcessResult Process(SvgDocument document, SvgProcessJob job)
    {
        SvgProcessOptions options = new SvgProcessOptions();
        options.Jobs.Add(job);
        return document.ProcessWithOptions(options);
    }

    static SvgDocument Load(string source)
    {
        SvgLoadResult result = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, new SvgLoadOptions());
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result.Document;
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

    static string Attribute(SvgNode node, string name)
    {
        return node.Attributes.Single(item => item.LocalName == name && item.NamespaceUri.Length == 0).Value;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
