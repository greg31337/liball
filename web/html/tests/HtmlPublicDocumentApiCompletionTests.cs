using System.IO;
using System.Text;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlPublicDocumentApiCompletionTests
{
    const string SvgNamespace = "http://www.w3.org/2000/svg";
    const string XlinkNamespace = "http://www.w3.org/1999/xlink";

    [Test]
    public void TokenListApiSupportsEveryOperation()
    {
        IHtmlTokenListsApi api = new HtmlTokenListsApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        string[] spaces = api.ParseSpaceSeparated(" one\ttwo one ");
        string[] commas = api.ParseCommaSeparated("one, two,,three");
        HtmlTokenListResult boundedSpaces =
            api.ParseSpaceSeparatedWithLimits("one two", limits,
                CancellationToken.None);
        HtmlTokenListResult boundedCommas =
            api.ParseCommaSeparatedWithLimits("one,two", limits,
                CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(spaces, Is.EqualTo(new[] { "one", "two", "one" }));
            Assert.That(commas,
                Is.EqualTo(new[] { "one", "two", "", "three" }));
            Assert.That(boundedSpaces.GetTokens(),
                Is.EqualTo(new[] { "one", "two" }));
            Assert.That(boundedCommas.GetTokens(),
                Is.EqualTo(new[] { "one", "two" }));
            Assert.That(api.SerializeSpaceSeparated(spaces),
                Is.EqualTo("one two one"));
            Assert.That(api.SerializeCommaSeparated(commas),
                Is.EqualTo("one, two, , three"));
            Assert.That(api.SerializeSpaceSeparatedWithLimits(
                new[] { "one", "two" }, limits, CancellationToken.None),
                Is.EqualTo("one two"));
            Assert.That(api.SerializeCommaSeparatedWithLimits(
                new[] { "one", "two" }, limits, CancellationToken.None),
                Is.EqualTo("one, two"));
            Assert.That(api.Contains(spaces, "one"), Is.True);
            Assert.That(api.ContainsAsciiInsensitive(spaces, "TWO"),
                Is.True);
            Assert.That(api.ContainsAsciiInsensitivePrefix(spaces, 2,
                "ONE"), Is.True);
        });
    }

    [Test]
    public void SrcsetApiSupportsEveryOperationAndCandidateForm()
    {
        IHtmlSrcsetApi api = new HtmlSrcsetApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlSrcsetCandidate plain = api.CreateCandidate("plain.png");
        HtmlSrcsetCandidate width = api.CreateWidthCandidate(
            "wide.png", 800);
        HtmlSrcsetCandidate density = api.CreateDensityCandidate(
            "dense.png", 2.5);
        HtmlSrcsetCandidate[] candidates = [plain, width, density];
        string serialized = api.Serialize(candidates);
        HtmlSrcsetResult parsed = api.Parse(serialized);
        HtmlSrcsetResult bounded = api.ParseWithLimits(serialized, limits,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(serialized, Is.EqualTo(
                "plain.png, wide.png 800w, dense.png 2.5x"));
            Assert.That(api.SerializeWithLimits(candidates, limits,
                CancellationToken.None), Is.EqualTo(serialized));
            Assert.That(parsed.IsValid(), Is.True);
            Assert.That(bounded.IsComplete(), Is.True);
            Assert.That(plain.HasWidth(), Is.False);
            Assert.That(plain.HasDensity(), Is.False);
            Assert.That(width.GetWidth(), Is.EqualTo(800));
            Assert.That(density.GetDensity(), Is.EqualTo(2.5));
        });
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.CreateWidthCandidate("wide.png", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            api.CreateDensityCandidate("dense.png", double.NaN));
    }

    [Test]
    public void NumericLanguageAndRefreshApisSupportEveryOperation()
    {
        IHtmlNumbersApi numbers = new HtmlNumbersApi();
        IHtmlLanguageTagsApi languages = new HtmlLanguageTagsApi();
        IHtmlRefreshApi refresh = new HtmlRefreshApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlDocument document = Html.Parse(
            "<table><tr><td id=cell align=right></td></tr></table>"
            + "<ol id=list start=4 type=i><li></li><li value=9></li></ol>");
        HtmlNode list = document.FindById("list");

        Assert.Multiple(() =>
        {
            Assert.That(numbers.ParseInteger(" -7tail").GetValue(),
                Is.EqualTo(-7));
            Assert.That(numbers.ParseNonNegativeInteger("12").GetValue(),
                Is.EqualTo(12));
            Assert.That(numbers.ParseDimension("25%").GetUnit(),
                Is.EqualTo(HtmlDimensionUnit.Percentage));
            Assert.That(numbers.ParseColumnSpan("2000"), Is.EqualTo(1000));
            Assert.That(numbers.ParseRowSpan("70000"), Is.EqualTo(65534));
            Assert.That(numbers.ParseTableAlignment("middle"),
                Is.EqualTo(HtmlTableAlignment.Center));
            Assert.That(numbers.GetTableAlignment(document.FindById("cell")),
                Is.EqualTo(HtmlTableAlignment.Right));
            Assert.That(numbers.GetOrderedListStart(list), Is.EqualTo(4));
            Assert.That(numbers.GetOrderedListOrdinals(list),
                Is.EqualTo(new[] { 4, 9 }));
            Assert.That(numbers.GetOrderedListMarkerStyle(list),
                Is.EqualTo(HtmlOrderedListMarkerStyle.LowerRoman));
            Assert.That(languages.Parse("EN-latn-us").GetTag(),
                Is.EqualTo("en-Latn-US"));
            Assert.That(languages.ParseWithLimits("en-US", limits,
                CancellationToken.None).IsSuccess(), Is.True);
            Assert.That(refresh.Parse("5;url=next").GetUrl(),
                Is.EqualTo("next"));
            Assert.That(refresh.ParseWithLimits("5;url=next", limits,
                CancellationToken.None).IsSuccess(), Is.True);
            Assert.That(refresh.Serialize(5, "next"),
                Is.EqualTo("5; url=next"));
            Assert.That(refresh.SerializeWithLimits(5, "next", limits,
                CancellationToken.None), Is.EqualTo("5; url=next"));
        });
    }

    [Test]
    public void UrlApiSupportsEveryOperationAndCancellationPoint()
    {
        IHtmlUrlsApi api = new HtmlUrlsApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlUrlSchemeResult scheme = api.ExtractScheme(
            "java&#x73;cript:value");
        HtmlUrlSchemeResult boundedScheme = api.ExtractSchemeWithLimits(
            "HTTPS:value", limits, CancellationToken.None);
        HtmlUrlResult normalized = api.Normalize(
            "HTTP://Example.COM:80/a/../b");
        HtmlUrlResult boundedNormalized = api.NormalizeWithLimits(
            "https://example.test/path", limits, CancellationToken.None);
        HtmlUrlResult resolved = api.Resolve("../asset.png",
            "https://example.test/root/page/");
        HtmlUrlResult boundedResolved = api.ResolveWithLimits("child",
            "https://example.test/root/", limits,
            CancellationToken.None);
        CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.That(scheme.GetScheme(), Is.EqualTo("javascript"));
            Assert.That(boundedScheme.GetScheme(), Is.EqualTo("https"));
            Assert.That(normalized.GetUrl(),
                Is.EqualTo("http://example.com/b"));
            Assert.That(boundedNormalized.GetUrl(),
                Is.EqualTo("https://example.test/path"));
            Assert.That(resolved.GetUrl(),
                Is.EqualTo("https://example.test/root/asset.png"));
            Assert.That(boundedResolved.GetUrl(),
                Is.EqualTo("https://example.test/root/child"));
            Assert.Throws<OperationCanceledException>(() =>
                api.ExtractSchemeWithLimits("https:", limits,
                    cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                api.NormalizeWithLimits("https://example.test/", limits,
                    cancellation.Token));
            Assert.Throws<OperationCanceledException>(() =>
                api.ResolveWithLimits("child", "https://example.test/",
                    limits, cancellation.Token));
        });
    }

    [Test]
    public void DomUrlApiSupportsEveryResolutionAndDiscoveryOperation()
    {
        IHtmlDomUrlsApi api = new HtmlDomUrlsApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlDocument document = ParseWithBase(
            "<head><base id=base href=assets/></head><body>"
            + "<a id=link href=page.html></a>"
            + "<img id=image src=picture.png>"
            + "<svg><a id=svg-link xlink:href=vector.svg></a></svg>"
            + "</body>", "https://example.test/root/");
        HtmlNode link = document.FindById("link");
        HtmlNode svgLink = document.FindById("svg-link");
        HtmlAttribute href = GetAttribute(link, "", "href");
        HtmlAttribute xlinkHref = GetAttribute(svgLink, XlinkNamespace,
            "href");

        HtmlUrlResult effective = api.GetEffectiveBaseUrl(document);
        HtmlUrlResult boundedEffective = api.GetEffectiveBaseUrlWithLimits(
            document, limits, CancellationToken.None);
        HtmlUrlResult attribute = api.ResolveAttribute(link, href);
        HtmlUrlResult boundedAttribute = api.ResolveAttributeWithLimits(
            link, href, limits, CancellationToken.None);
        HtmlUrlResult named = api.ResolveNamedAttribute(link, "href");
        HtmlUrlResult boundedNamed = api.ResolveNamedAttributeWithLimits(
            link, "href", limits, CancellationToken.None);
        HtmlUrlResult namespaced = api.ResolveNamespacedAttribute(svgLink,
            XlinkNamespace, "href");
        HtmlUrlResult boundedNamespaced =
            api.ResolveNamespacedAttributeWithLimits(svgLink,
                XlinkNamespace, "href", limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(api.GetAttributeKind(link, href),
                Is.EqualTo(HtmlUrlAttributeKind.Single));
            Assert.That(api.GetNamedAttributeKind(link, "href"),
                Is.EqualTo(HtmlUrlAttributeKind.Single));
            Assert.That(api.GetAttributeKind(svgLink, xlinkHref),
                Is.EqualTo(HtmlUrlAttributeKind.Single));
            Assert.That(api.GetBaseElement(document).GetAttribute("id"),
                Is.EqualTo("base"));
            Assert.That(effective.GetUrl(),
                Is.EqualTo("https://example.test/root/assets/"));
            Assert.That(boundedEffective.GetUrl(), Is.EqualTo(
                effective.GetUrl()));
            Assert.That(new[] { attribute, boundedAttribute, named,
                boundedNamed }.Select(value => value.GetUrl()),
                Has.All.EqualTo(
                    "https://example.test/root/assets/page.html"));
            Assert.That(new[] { namespaced, boundedNamespaced }.Select(
                value => value.GetUrl()), Has.All.EqualTo(
                    "https://example.test/root/assets/vector.svg"));
            Assert.That(api.FindLinks(document.GetRoot()).Select(value =>
                value.GetAttribute("id")), Is.EqualTo(
                    new[] { "link", "svg-link" }));
            Assert.That(api.FindLinksWithLimits(document.GetRoot(), 1, 100),
                Has.Length.EqualTo(1));
            Assert.That(api.FindMedia(document.GetRoot()).Select(value =>
                value.GetAttribute("id")), Is.EqualTo(new[] { "image" }));
            Assert.That(api.FindMediaWithLimits(document.GetRoot(), 1, 100),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void SelectorApiSupportsEveryCompilationModeAndEscaper()
    {
        IHtmlSelectorsApi api = new HtmlSelectorsApi();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        HtmlSelectorNamespaceBinding[] bindings =
        [
            new HtmlSelectorNamespaceBinding("svg", SvgNamespace)
        ];

        HtmlSelectorCompileResult strict = api.Compile("div.hit");
        HtmlSelectorCompileResult strictLimited = api.CompileWithLimits(
            "div.hit", limits);
        HtmlSelectorCompileResult forgiving = api.CompileForgiving(
            "::before, div.hit");
        HtmlSelectorCompileResult forgivingLimited =
            api.CompileForgivingWithLimits("::before, div.hit", limits);
        HtmlSelectorCompileResult namespaced = api.CompileWithNamespaces(
            "svg|circle", bindings);
        HtmlSelectorCompileResult namespacedLimited =
            api.CompileWithNamespacesAndLimits("svg|circle", bindings,
                limits);
        HtmlSelectorCompileResult forgivingNamespaced =
            api.CompileForgivingWithNamespaces(
                "missing|x, svg|circle", bindings);
        HtmlSelectorCompileResult forgivingNamespacedLimited =
            api.CompileForgivingWithNamespacesAndLimits(
                "missing|x, svg|circle", bindings, limits);

        Assert.Multiple(() =>
        {
            Assert.That(strict.IsSuccess(), Is.True);
            Assert.That(strictLimited.IsSuccess(), Is.True);
            Assert.That(forgiving.IsSuccess(), Is.True);
            Assert.That(forgiving.IsPartial(), Is.True);
            Assert.That(forgivingLimited.IsSuccess(), Is.True);
            Assert.That(namespaced.IsSuccess(), Is.True);
            Assert.That(namespacedLimited.IsSuccess(), Is.True);
            Assert.That(forgivingNamespaced.IsSuccess(), Is.True);
            Assert.That(forgivingNamespaced.IsPartial(), Is.True);
            Assert.That(forgivingNamespacedLimited.IsSuccess(), Is.True);
            Assert.That(api.EscapeIdentifier("0 leading#id"),
                Is.EqualTo("\\30 \\ leading\\#id"));
            Assert.That(api.EscapeString("a\"b\\c"),
                Is.EqualTo("\"a\\\"b\\\\c\""));
        });
    }

    [Test]
    public void LegacySerializerApiHasStringWriterAndOptionsParity()
    {
        IHtmlSerializerApi api = new HtmlSerializerApi();
        HtmlNode paragraph = Html.Parse("<p>\u00e9<b>x</b></p>")
            .QuerySelector("p");
        HtmlSerializerOptions ascii = api.CreateAsciiOnlyOptions();
        StringWriter defaultNodeWriter = new StringWriter();
        StringWriter configuredNodeWriter = new StringWriter();
        StringWriter defaultChildrenWriter = new StringWriter();
        StringWriter configuredChildrenWriter = new StringWriter();

        string defaultNode = api.SerializeNode(paragraph);
        string configuredNode = api.SerializeNodeWithOptions(paragraph,
            ascii, CancellationToken.None);
        string defaultChildren = api.SerializeChildren(paragraph);
        string configuredChildren = api.SerializeChildrenWithOptions(
            paragraph, ascii, CancellationToken.None);
        api.WriteNode(paragraph, defaultNodeWriter);
        api.WriteNodeWithOptions(paragraph, configuredNodeWriter, ascii,
            CancellationToken.None);
        api.WriteChildren(paragraph, defaultChildrenWriter);
        api.WriteChildrenWithOptions(paragraph, configuredChildrenWriter,
            ascii, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(defaultNode, Is.EqualTo("<p>\u00e9<b>x</b></p>"));
            Assert.That(configuredNode,
                Is.EqualTo("<p>&eacute;<b>x</b></p>"));
            Assert.That(defaultChildren, Is.EqualTo("\u00e9<b>x</b>"));
            Assert.That(configuredChildren,
                Is.EqualTo("&eacute;<b>x</b>"));
            Assert.That(defaultNodeWriter.ToString(),
                Is.EqualTo(defaultNode));
            Assert.That(configuredNodeWriter.ToString(),
                Is.EqualTo(configuredNode));
            Assert.That(defaultChildrenWriter.ToString(),
                Is.EqualTo(defaultChildren));
            Assert.That(configuredChildrenWriter.ToString(),
                Is.EqualTo(configuredChildren));
        });
    }

    static HtmlDocument ParseWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None)
            .GetDocument();
    }

    static HtmlAttribute GetAttribute(HtmlNode element, string namespaceUri,
        string localName)
    {
        return element.GetAttributes().Single(value =>
            value.GetNamespaceUri() == namespaceUri
            && value.GetLocalName() == localName);
    }
}
