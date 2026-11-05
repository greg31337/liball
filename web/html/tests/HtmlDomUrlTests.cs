
namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlDomUrlTests
{
    const string XLinkNamespace = "http://www.w3.org/1999/xlink";

    [Test]
    public void ClassifiesUrlAttributesByElementAndNamespace()
    {
        HtmlDocument document = Html.Parse(
            "<meta id=refresh http-equiv=' Refresh ' content='0;url=/next'>"
            + "<form id=form action=/submit></form>"
            + "<a id=anchor href=/page ping='/one /two'></a>"
            + "<img id=image src=/image.png srcset='a.png 1x' usemap=#map>"
            + "<table id=table background=/texture.png></table>"
            + "<div id=div href=/not-a-url itemtype='https://schema.test/T'>"
            + "<svg><a id=svg-link xlink:href=/vector></a></svg>");
        HtmlNode refresh = document.FindById("refresh");
        HtmlNode form = document.FindById("form");
        HtmlNode anchor = document.FindById("anchor");
        HtmlNode image = document.FindById("image");
        HtmlNode table = document.FindById("table");
        HtmlNode div = document.FindById("div");
        HtmlNode svgLink = document.FindById("svg-link");

        Assert.Multiple(() =>
        {
            AssertKind(refresh, "content", HtmlUrlAttributeKind.Refresh);
            AssertKind(form, "action", HtmlUrlAttributeKind.Single);
            AssertKind(anchor, "href", HtmlUrlAttributeKind.Single);
            AssertKind(anchor, "ping",
                HtmlUrlAttributeKind.SpaceSeparated);
            AssertKind(image, "srcset", HtmlUrlAttributeKind.SourceSet);
            AssertKind(image, "usemap", HtmlUrlAttributeKind.None);
            AssertKind(table, "background", HtmlUrlAttributeKind.Single);
            AssertKind(div, "href", HtmlUrlAttributeKind.None);
            AssertKind(div, "itemtype",
                HtmlUrlAttributeKind.SpaceSeparated);
            Assert.That(HtmlDomUrls.GetAttributeKind(svgLink,
                GetAttribute(svgLink, XLinkNamespace, "href")),
                Is.EqualTo(HtmlUrlAttributeKind.Single));
        });
    }

    [Test]
    public void FirstBaseHrefControlsTheEffectiveDocumentBase()
    {
        HtmlDocument document = ParseWithBase(
            "<head><base id=first href='../assets/'>"
            + "<base id=second href='https://ignored.test/'></head>"
            + "<body><a id=link href='images/pic.png'></a></body>",
            "https://origin.test/root/page.html");
        HtmlNode first = document.FindById("first");
        HtmlNode second = document.FindById("second");
        HtmlNode link = document.FindById("link");

        Assert.Multiple(() =>
        {
            Assert.That(document.GetBaseElement(), Is.SameAs(first));
            Assert.That(document.GetEffectiveBaseUrl().GetUrl(),
                Is.EqualTo("https://origin.test/assets/"));
            Assert.That(first.GetAbsoluteUrl("href").GetUrl(),
                Is.EqualTo("https://origin.test/assets/"));
            Assert.That(link.GetAbsoluteUrl("href").GetUrl(), Is.EqualTo(
                "https://origin.test/assets/images/pic.png"));
            Assert.That(second.GetAbsoluteUrl("href").GetUrl(),
                Is.EqualTo("https://ignored.test/"));
        });
    }

    [Test]
    public void InvalidOrUnsafeFirstBaseFallsBackAndLaterBasesStayIgnored()
    {
        HtmlDocument document = ParseWithBase(
            "<base id=first href='javascript:alert(1)'>"
            + "<base id=second href='https://ignored.test/'>"
            + "<a id=link href=child></a>",
            "https://fallback.test/root/");
        HtmlNode first = document.FindById("first");
        HtmlNode second = document.FindById("second");
        HtmlNode link = document.FindById("link");

        Assert.That(document.GetEffectiveBaseUrl().GetUrl(),
            Is.EqualTo("https://fallback.test/root/"));
        Assert.That(link.GetAbsoluteUrl("href").GetUrl(),
            Is.EqualTo("https://fallback.test/root/child"));

        first.Remove();

        Assert.Multiple(() =>
        {
            Assert.That(document.GetBaseElement(), Is.SameAs(second));
            Assert.That(document.GetEffectiveBaseUrl().GetUrl(),
                Is.EqualTo("https://ignored.test/"));
            Assert.That(link.GetAbsoluteUrl("href").GetUrl(),
                Is.EqualTo("https://ignored.test/child"));
        });
    }

    [Test]
    public void BaseSelectionAndAbsoluteUrlsRefreshAfterMutation()
    {
        HtmlDocument document = ParseWithBase(
            "<head><base id=base href=/one/></head>"
            + "<body><a id=link href=item></a></body>",
            "https://example.test/root/page");
        HtmlNode baseElement = document.FindById("base");
        HtmlNode link = document.FindById("link");

        Assert.That(link.GetAbsoluteUrl("href").GetUrl(),
            Is.EqualTo("https://example.test/one/item"));

        baseElement.SetAttribute("href", "/two/");
        link.SetAttribute("href", "changed");

        Assert.That(link.GetAbsoluteUrl("href").GetUrl(),
            Is.EqualTo("https://example.test/two/changed"));

        baseElement.RemoveAttribute("href");

        Assert.Multiple(() =>
        {
            Assert.That(document.GetBaseElement(), Is.Null);
            Assert.That(document.GetEffectiveBaseUrl().GetUrl(),
                Is.EqualTo("https://example.test/root/page"));
            Assert.That(link.GetAbsoluteUrl("href").GetUrl(),
                Is.EqualTo("https://example.test/root/changed"));
        });
    }

    [Test]
    public void BaseUrlLimitFailsClosedButAbsoluteAttributesIgnoreTheBase()
    {
        HtmlDocument document = ParseWithBase(
            "<base href='this-relative-base-is-too-long/'>"
            + "<a id=relative href=child></a>"
            + "<a id=absolute href='https://other.test/path'></a>",
            "https://x.test/");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(24);
        HtmlUrlResult documentBase = HtmlDomUrls.GetEffectiveBaseUrl(document,
            limits, CancellationToken.None);
        HtmlUrlResult relative = document.FindById("relative")
            .GetAbsoluteUrlWithLimits("href", limits, CancellationToken.None);
        HtmlUrlResult absolute = document.FindById("absolute")
            .GetAbsoluteUrlWithLimits("href", limits, CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(documentBase.WasLimitExceeded(), Is.True);
            Assert.That(relative.WasLimitExceeded(), Is.True);
            Assert.That(absolute.IsSuccess(), Is.True);
            Assert.That(absolute.GetUrl(),
                Is.EqualTo("https://other.test/path"));
        });
    }

    [Test]
    public void ResolvesHtmlAndForeignSingleUrlAttributes()
    {
        HtmlDocument document = ParseWithBase(
            "<blockquote id=quote cite=notes></blockquote>"
            + "<img id=image srcset='one.png 1x'>"
            + "<div id=div href=no></div>"
            + "<svg><image id=svg-image xlink:href=vector.svg></image></svg>",
            "https://example.test/base/");
        HtmlNode quote = document.FindById("quote");
        HtmlNode image = document.FindById("image");
        HtmlNode div = document.FindById("div");
        HtmlNode svgImage = document.FindById("svg-image");

        HtmlUrlResult sourceSet = image.GetAbsoluteUrl("srcset");
        HtmlUrlResult nonUrl = div.GetAbsoluteUrl("href");
        HtmlUrlResult missing = quote.GetAbsoluteUrl("href");

        Assert.Multiple(() =>
        {
            Assert.That(quote.GetAbsoluteUrl("cite").GetUrl(),
                Is.EqualTo("https://example.test/base/notes"));
            Assert.That(svgImage.GetAbsoluteUrlInNamespace(XLinkNamespace, "href")
                .GetUrl(), Is.EqualTo(
                    "https://example.test/base/vector.svg"));
            AssertInvalid(sourceSet);
            AssertInvalid(nonUrl);
            AssertInvalid(missing);
        });
    }

    [Test]
    public void LinkAndMediaDiscoveryReturnsDocumentOrderSnapshots()
    {
        HtmlDocument document = Html.Parse(
            "<head><link id=stylesheet href=style.css></head><body>"
            + "<a id=anchor href=/a></a><a id=empty></a>"
            + "<area id=area href=/map>"
            + "<img id=image src=pic.png><video id=video poster=poster.png>"
            + "<source id=source srcset='movie.mp4 1x'></video>"
            + "<audio id=empty-audio></audio><object id=object data=file.bin>"
            + "</object><svg><a id=svg-a href=/vector></a>"
            + "<image id=svg-image href=vector.png></image></svg></body>");

        HtmlNode[] links = document.FindLinks();
        HtmlNode[] media = document.FindMedia();
        HtmlNode[] bodyLinks = document.GetBody().FindLinks();
        links[0] = null;
        media[0] = null;

        Assert.Multiple(() =>
        {
            Assert.That(GetIds(document.FindLinks()), Is.EqualTo(new[]
            {
                "stylesheet", "anchor", "area", "svg-a"
            }));
            Assert.That(GetIds(document.FindMedia()), Is.EqualTo(new[]
            {
                "image", "video", "source", "object", "svg-image"
            }));
            Assert.That(GetIds(bodyLinks), Is.EqualTo(new[]
            {
                "anchor", "area", "svg-a"
            }));
        });
    }

    [Test]
    public void DiscoveryReevaluatesMutationsAndHonorsSearchLimits()
    {
        HtmlDocument document = Html.Parse(
            "<body><a id=first href=/one></a><a id=second href=/two></a>"
            + "</body>");
        HtmlNode first = document.FindById("first");
        HtmlNode second = document.FindById("second");
        HtmlNode[] snapshot = document.FindLinks();

        first.RemoveAttribute("href");
        second.SetAttribute("href", "/changed");

        Assert.Multiple(() =>
        {
            Assert.That(GetIds(snapshot), Is.EqualTo(new[]
            {
                "first", "second"
            }));
            Assert.That(GetIds(document.FindLinks()),
                Is.EqualTo(new[] { "second" }));
            Assert.That(document.FindLinksWithLimits(1, 100),
                Is.EqualTo(new[] { second }));
            Assert.Throws<InvalidOperationException>(() =>
                document.FindLinksWithLimits(2, 1));
        });
    }

    [Test]
    public void AbsoluteUrlFullOverloadHonorsLimitsAndCancellation()
    {
        HtmlDocument document = ParseWithBase("<a id=link href=child></a>",
            "https://example.test/base/");
        HtmlNode link = document.FindById("link");
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(10);
        HtmlLimits traversalLimits = HtmlLimits.CreateDefault();
        traversalLimits.SetMaximumVisits(1);
        CancellationTokenSource source = new CancellationTokenSource();
        source.Cancel();

        HtmlUrlResult limited = link.GetAbsoluteUrlWithLimits("href", limits,
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(limited.IsSuccess(), Is.False);
            Assert.That(limited.WasLimitExceeded(), Is.True);
            Assert.Throws<OperationCanceledException>(() =>
                link.GetAbsoluteUrlWithLimits("href", HtmlLimits.CreateDefault(),
                    source.Token));
            Assert.Throws<OperationCanceledException>(() =>
                HtmlDomUrls.GetEffectiveBaseUrl(document,
                    traversalLimits, source.Token));
            Assert.Throws<InvalidOperationException>(() =>
                HtmlDomUrls.GetEffectiveBaseUrl(document, traversalLimits,
                    CancellationToken.None));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlDomUrls.GetAttributeKind(null,
                    GetAttribute(link, "href")));
            Assert.Throws<ArgumentNullException>(() =>
                HtmlDomUrls.ResolveAttribute(link, (string)null));
        });
    }

    static HtmlDocument ParseWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }

    static HtmlAttribute GetAttribute(HtmlNode element, string name)
    {
        return element.GetAttributes().Single(attribute =>
            attribute.GetName() == name);
    }

    static HtmlAttribute GetAttribute(HtmlNode element, string namespaceUri,
        string localName)
    {
        return element.GetAttributes().Single(attribute =>
            attribute.GetNamespaceUri() == namespaceUri
                && attribute.GetLocalName() == localName);
    }

    static void AssertKind(HtmlNode element, string attributeName,
        HtmlUrlAttributeKind expected)
    {
        Assert.That(HtmlDomUrls.GetAttributeKind(element,
            GetAttribute(element, attributeName)), Is.EqualTo(expected));
    }

    static void AssertInvalid(HtmlUrlResult result)
    {
        Assert.That(result.IsSuccess(), Is.False);
        Assert.That(result.GetDiagnostics().Single().GetCode(),
            Is.EqualTo(HtmlDiagnosticCode.UrlInvalid));
    }

    static string[] GetIds(HtmlNode[] nodes)
    {
        return nodes.Select(node => node.GetAttribute("id")).ToArray();
    }
}
