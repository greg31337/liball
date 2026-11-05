namespace svg.Tests;

public class SvgNavigationTests
{
    [Test]
    public void FocusOrderPlacesPositiveIndicesBeforeDocumentOrderedZeroEntries()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'>" +
            "<rect id='zero' tabindex='0'/><a id='link' href='next.svg'/><circle id='two' tabindex='2'/>" +
            "<path id='one' tabindex='1'/><a id='negative' href='other.svg' tabindex='-1'/>" +
            "<g id='invalid' tabindex='x'/><rect id='hidden' tabindex='0' display='none'/></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/doc.svg");

        SvgFocusOrderResult result = new SvgNavigation().FocusOrderWithDocument(document);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Entries.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "zero", "link", "two", "one", "negative" }));
            Assert.That(result.TabOrder.Select(item => Id(item.SourceNode)), Is.EqualTo(new[] { "one", "two", "zero", "link" }));
            Assert.That(result.Entries.Single(item => Id(item.SourceNode) == "negative").Sequential, Is.False);
            Assert.That(result.Entries.Single(item => Id(item.SourceNode) == "link").ImplicitLink, Is.True);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(SvgDiagnosticCode.InvalidValue));
        });
    }

    [Test]
    public void ExternalLinkActivationReturnsCompleteHostRequestWithoutNavigation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><a id='link' href='../next.svg#part' target='_blank' download='copy.svg' hreflang='FR' type='image/svg+xml' rel='noopener external'><rect id='child'/></a></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/art/doc.svg");
        SvgResolvedNode child = document.FindSource(document.SourceDocument.Index.GetById("child"));
        long version = document.SourceDocument.Version;

        SvgActivationRecord record = new SvgNavigation().ActivateWithDocumentAndNode(document, child);

        Assert.Multiple(() =>
        {
            Assert.That(record.Success, Is.True);
            Assert.That(record.Kind, Is.EqualTo(SvgActivationKind.Link));
            Assert.That(Id(record.LinkNode.SourceNode), Is.EqualTo("link"));
            Assert.That(record.Request.Kind, Is.EqualTo(SvgNavigationRequestKind.Navigate));
            Assert.That(record.Request.NormalizedIri, Is.EqualTo("https://example.test/next.svg#part"));
            Assert.That(record.Request.NormalizedUri, Is.EqualTo("https://example.test/next.svg#part"));
            Assert.That(record.Request.ResourceIri, Is.EqualTo("https://example.test/next.svg"));
            Assert.That(record.Request.Fragment, Is.EqualTo("part"));
            Assert.That(record.Request.Target, Is.EqualTo("_blank"));
            Assert.That(record.Request.HasDownload, Is.True);
            Assert.That(record.Request.Download, Is.EqualTo("copy.svg"));
            Assert.That(record.Request.HrefLanguage, Is.EqualTo("fr"));
            Assert.That(record.Request.MediaType.ToText(), Is.EqualTo("image/svg+xml"));
            Assert.That(record.Request.Relations, Is.EqualTo(new[] { "noopener", "external" }));
            Assert.That(document.SourceDocument.Version, Is.EqualTo(version));
        });
    }

    [Test]
    public void FragmentActivationReturnsLocalTargetWithoutMutatingInteractionState()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><a id='link' href='#section'><rect/></a><g id='section'/></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/doc.svg");
        SvgInteractionState interaction = new SvgInteractionState(document.SourceDocument);
        SvgResolvedNode link = document.FindSource(document.SourceDocument.Index.GetById("link"));

        SvgActivationRecord record = new SvgNavigation().ActivateWithDocumentAndNode(document, link);

        Assert.Multiple(() =>
        {
            Assert.That(record.Success, Is.True);
            Assert.That(record.Kind, Is.EqualTo(SvgActivationKind.Fragment));
            Assert.That(record.FragmentTarget, Is.SameAs(document.SourceDocument.Index.GetById("section")));
            Assert.That(record.Request.Kind, Is.EqualTo(SvgNavigationRequestKind.SameDocumentFragment));
            Assert.That(record.Request.Fragment, Is.EqualTo("section"));
            Assert.That(interaction.Target, Is.Null);
            Assert.That(interaction.Version, Is.Zero);
        });
    }

    [Test]
    public void AbsoluteCurrentDocumentUriIsAlsoAFragmentActivation()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><a id='link' href='https://example.test/art/doc.svg#section'/><g id='section'/></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/art/doc.svg");

        SvgActivationRecord record = new SvgNavigation().ActivateWithDocumentAndNode(document, document.FindSource(document.SourceDocument.Index.GetById("link")));

        Assert.Multiple(() =>
        {
            Assert.That(record.Success, Is.True);
            Assert.That(record.Kind, Is.EqualTo(SvgActivationKind.Fragment));
            Assert.That(Id(record.FragmentTarget), Is.EqualTo("section"));
        });
    }

    [Test]
    public void HitActivationSelectsTheNearestLinkAncestor()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' width='20' height='20'><a id='outer' href='outer.svg'><g><a id='inner' href='inner.svg'><rect width='10' height='10'/></a></g></a></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/doc.svg");
        SvgHitTestMatch match = document.HitTest(new SvgPoint(5, 5)).Topmost;

        SvgActivationRecord record = new SvgNavigation().ActivateWithDocumentAndMatch(document, match);

        Assert.Multiple(() =>
        {
            Assert.That(record.Success, Is.True);
            Assert.That(Id(record.LinkNode.SourceNode), Is.EqualTo("inner"));
            Assert.That(record.Request.NormalizedIri, Is.EqualTo("https://example.test/inner.svg"));
        });
    }

    [Test]
    public void ModernHrefWinsOverLegacyXlinkHref()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg' xmlns:xlink='http://www.w3.org/1999/xlink'><a id='link' href='modern.svg' xlink:href='legacy.svg'/></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/doc.svg");

        SvgActivationRecord record = new SvgNavigation().ActivateWithDocumentAndNode(document, document.FindSource(document.SourceDocument.Index.GetById("link")));

        Assert.Multiple(() =>
        {
            Assert.That(record.Success, Is.True);
            Assert.That(record.Request.Href, Is.EqualTo("modern.svg"));
            Assert.That(record.Request.NormalizedIri, Is.EqualTo("https://example.test/modern.svg"));
        });
    }

    [Test]
    public void MissingLinksFragmentsAndBoundsReturnStableFailures()
    {
        string source = "<svg xmlns='http://www.w3.org/2000/svg'><rect id='plain'/><a id='empty'/><a id='missing' href='#absent'/><a href='next.svg'><g><rect id='deep'/></g></a></svg>";
        SvgResolvedDocument document = Resolve(source, "https://example.test/doc.svg");
        SvgNavigationOptions limited = new SvgNavigationOptions();
        limited.MaximumNodes = 1;

        SvgActivationRecord plain = new SvgNavigation().ActivateWithDocumentAndNode(document, document.FindSource(document.SourceDocument.Index.GetById("plain")));
        SvgActivationRecord empty = new SvgNavigation().ActivateWithDocumentAndNode(document, document.FindSource(document.SourceDocument.Index.GetById("empty")));
        SvgActivationRecord missing = new SvgNavigation().ActivateWithDocumentAndNode(document, document.FindSource(document.SourceDocument.Index.GetById("missing")));
        SvgActivationRecord deep = new SvgNavigation().ActivateWithDocumentAndNodeAndOptions(document, document.FindSource(document.SourceDocument.Index.GetById("deep")), limited);
        SvgFocusOrderResult bounded = new SvgNavigation().FocusOrderWithDocumentAndOptions(document, limited);

        Assert.Multiple(() =>
        {
            Assert.That(plain.Error, Is.EqualTo(SvgNavigationError.NotActivatable));
            Assert.That(empty.Error, Is.EqualTo(SvgNavigationError.NotActivatable));
            Assert.That(missing.Error, Is.EqualTo(SvgNavigationError.FragmentNotFound));
            Assert.That(missing.Request, Is.Null);
            Assert.That(deep.Error, Is.EqualTo(SvgNavigationError.LimitExceeded));
            Assert.That(bounded.Error, Is.EqualTo(SvgNavigationError.LimitExceeded));
        });
    }

    static SvgResolvedDocument Resolve(string source, string baseUri)
    {
        SvgLoadOptions loadOptions = new SvgLoadOptions();
        loadOptions.BaseUri = baseUri;
        SvgLoadResult loaded = new SvgLoader().LoadWithStringAndSvgLoadOptions(source, loadOptions);
        Assert.That(loaded.Success, Is.True);
        SvgResolvedDocument result = new SvgDocumentResolver().Resolve(loaded.Document);
        Assert.That(result.Success, Is.True, string.Join(" | ", result.Diagnostics.Select(item => item.Message)));
        return result;
    }

    static string Id(SvgNode node)
    {
        if (node == null) return "";
        int index = 0;
        while (index < node.Attributes.Count)
        {
            SvgAttribute attribute = node.Attributes[index];
            if (attribute.NamespaceUri.Length == 0 && attribute.LocalName == "id") return attribute.Value;
            index = index + 1;
        }
        return "";
    }
}
