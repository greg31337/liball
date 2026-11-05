namespace svg.Tests;

public class SvgIdRewriterTests
{
    [Test]
    public void PrefixingRewritesXmlCssTimingAriaAndConfiguredReferences()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><defs><linearGradient id='gradient-long'/><clipPath id='clip-long'/><path id='path-long'/></defs><text id='label-long'>Label</text><style>#target\\2d long,[id='target-long'],[href='#path-long']{fill:url(\"#gradient-long\");clip-path:url(#clip-long)}</style><rect id='target-long' fill='url(#gradient-long)' clip-path='url( #clip-long )' style='stroke:url(\"#gradient-long\")' aria-labelledby='label-long path-long' data-targets='path-long label-long'/><use id='copy-long' href='#path-long' xlink:href='#path-long'/><animate id='animation-long' href='#target-long' begin='target-long.click; label-long.end+1s'/><image href='asset.svg#path-long'/></svg>";
        SvgDocument document = Load(source);
        SvgIdPrefixOptions settings = new SvgIdPrefixOptions();
        settings.Prefix = "icon-";
        settings.AdditionalIdReferenceAttributes.Add("data-targets");

        SvgProcessResult first = Process(document, new SvgProcessJobs().PrefixIdsWithOptions(settings));
        SvgProcessResult second = Process(document, new SvgProcessJobs().PrefixIdsWithOptions(settings));
        SvgNode target = document.Index.GetById("icon-target-long");
        string css = new SvgNodeApi().GetTextContent(AllNodes(document).Single(node => node.ElementKind == SvgElementKind.Style));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, Messages(first));
            Assert.That(document.Index.GetById("icon-gradient-long"), Is.Not.Null);
            Assert.That(document.Index.GetById("icon-clip-long"), Is.Not.Null);
            Assert.That(document.Index.GetById("icon-path-long"), Is.Not.Null);
            Assert.That(target, Is.Not.Null);
            Assert.That(Attribute(target, "fill"), Is.EqualTo("url(#icon-gradient-long)"));
            Assert.That(Attribute(target, "clip-path"), Is.EqualTo("url(#icon-clip-long)"));
            Assert.That(Attribute(target, "style"), Does.Contain("url(\"#icon-gradient-long\")"));
            Assert.That(Attribute(target, "aria-labelledby"), Is.EqualTo("icon-label-long icon-path-long"));
            Assert.That(Attribute(target, "data-targets"), Is.EqualTo("icon-path-long icon-label-long"));
            Assert.That(Attribute(document.Index.GetById("icon-copy-long"), "href"), Is.EqualTo("#icon-path-long"));
            Assert.That(Attribute(document.Index.GetById("icon-copy-long"), "href", SvgNamespaces.XLink), Is.EqualTo("#icon-path-long"));
            Assert.That(Attribute(document.Index.GetById("icon-animation-long"), "begin"), Is.EqualTo("icon-target-long.click; icon-label-long.end+1s"));
            Assert.That(css, Does.Contain("#icon-target-long"));
            Assert.That(css, Does.Contain("[id='icon-target-long']"));
            Assert.That(css, Does.Contain("[href='#icon-path-long']"));
            Assert.That(css, Does.Contain("url(\"#icon-gradient-long\")"));
            Assert.That(css, Does.Contain("url(#icon-clip-long)"));
            Assert.That(AllNodes(document).Single(node => node.ElementKind == SvgElementKind.Image).Attributes.Single(item => item.LocalName == "href").Value, Is.EqualTo("asset.svg#path-long"));
            Assert.That(second.Success, Is.True, Messages(second));
            Assert.That(second.Changes, Is.Empty);
        });
    }

    [Test]
    public void MinificationIsDeterministicCompactAndIdempotent()
    {
        SvgDocument first = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='verbose-paint'/><path id='verbose-path'/></defs><style>#verbose-target{fill:url(#verbose-paint)}</style><rect id='verbose-target' aria-describedby='verbose-path'/><use id='verbose-copy' href='#verbose-path'/></svg>");
        SvgDocument second = Load("<svg xmlns='http://www.w3.org/2000/svg'><defs><linearGradient id='verbose-paint'/><path id='verbose-path'/></defs><style>#verbose-target{fill:url(#verbose-paint)}</style><rect id='verbose-target' aria-describedby='verbose-path'/><use id='verbose-copy' href='#verbose-path'/></svg>");

        SvgProcessResult firstResult = Process(first, new SvgProcessJobs().MinifyIdsWithNoArguments());
        SvgProcessResult repeatResult = Process(first, new SvgProcessJobs().MinifyIdsWithNoArguments());
        SvgProcessResult secondResult = Process(second, new SvgProcessJobs().MinifyIdsWithNoArguments());

        Assert.Multiple(() =>
        {
            Assert.That(firstResult.Success, Is.True, Messages(firstResult));
            Assert.That(secondResult.Success, Is.True, Messages(secondResult));
            Assert.That(first.Index.GetById("a"), Is.Not.Null);
            Assert.That(first.Index.GetById("b"), Is.Not.Null);
            Assert.That(first.Index.GetById("c"), Is.Not.Null);
            Assert.That(first.Index.GetById("d"), Is.Not.Null);
            Assert.That(Attribute(first.Index.GetById("c"), "aria-describedby"), Is.EqualTo("b"));
            Assert.That(Attribute(first.Index.GetById("d"), "href"), Is.EqualTo("#b"));
            Assert.That(new SvgNodeApi().GetTextContent(AllNodes(first).Single(node => node.ElementKind == SvgElementKind.Style)), Is.EqualTo("#c{fill:url(#a)}"));
            Assert.That(AllNodes(first).SelectMany(node => node.Attributes).Where(item => item.LocalName == "id").Select(item => item.Value), Is.EqualTo(AllNodes(second).SelectMany(node => node.Attributes).Where(item => item.LocalName == "id").Select(item => item.Value)));
            Assert.That(new SvgNodeApi().GetTextContent(AllNodes(first).Single(node => node.ElementKind == SvgElementKind.Style)), Is.EqualTo(new SvgNodeApi().GetTextContent(AllNodes(second).Single(node => node.ElementKind == SvgElementKind.Style))));
            Assert.That(repeatResult.Success, Is.True, Messages(repeatResult));
            Assert.That(repeatResult.Changes, Is.Empty);
        });
    }

    [Test]
    public void PrefixingAvoidsExistingTargetsAndCopiesCallerSettings()
    {
        SvgIdPrefixOptions settings = new SvgIdPrefixOptions();
        settings.Prefix = "svg-";
        settings.AdditionalIdReferenceAttributes.Add("data-ref");
        SvgProcessJob job = new SvgProcessJobs().PrefixIdsWithOptions(settings);
        settings.Prefix = "changed-";
        settings.AdditionalIdReferenceAttributes.Clear();
        SvgDocument document = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='svg-a'/><g id='a' data-ref='svg-a'/><use href='#a'/></svg>");

        SvgProcessResult result = Process(document, job);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, Messages(result));
            Assert.That(document.Index.GetById("svg-a"), Is.Not.Null);
            Assert.That(document.Index.GetById("svg-svg-a"), Is.Not.Null);
            Assert.That(Attribute(document.Index.GetById("svg-svg-a"), "data-ref"), Is.EqualTo("svg-a"));
            Assert.That(Attribute(AllNodes(document).Single(node => node.ElementKind == SvgElementKind.Use), "href"), Is.EqualTo("#svg-svg-a"));
        });
    }

    [Test]
    public void DuplicateIdsAndInvalidSettingsFailAtomically()
    {
        SvgDocument duplicate = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='same'/><g id='same'/><use href='#same'/></svg>");
        SvgProcessResult duplicateResult = Process(duplicate, new SvgProcessJobs().MinifyIdsWithNoArguments());
        SvgIdPrefixOptions invalid = new SvgIdPrefixOptions();
        invalid.Prefix = "";
        SvgDocument invalidDocument = Load("<svg xmlns='http://www.w3.org/2000/svg'><g id='x'/></svg>");
        SvgProcessResult invalidResult = Process(invalidDocument, new SvgProcessJobs().PrefixIdsWithOptions(invalid));

        Assert.Multiple(() =>
        {
            Assert.That(duplicateResult.Success, Is.False);
            Assert.That(AllNodes(duplicate).Count(node => node.Attributes.Any(item => item.LocalName == "id" && item.Value == "same")), Is.EqualTo(2));
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidDocument.Index.GetById("x"), Is.Not.Null);
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

    static string Attribute(SvgNode node, string name)
    {
        return Attribute(node, name, "");
    }

    static string Attribute(SvgNode node, string name, string namespaceUri)
    {
        return node.Attributes.Single(item => item.LocalName == name && item.NamespaceUri == namespaceUri).Value;
    }

    static string Messages(SvgProcessResult result)
    {
        return string.Join(" | ", result.Diagnostics.Select(item => item.Message));
    }
}
