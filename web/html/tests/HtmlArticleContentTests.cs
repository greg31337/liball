using System.Threading;

namespace UniversalHtml.Tests;

[TestFixture]
public class HtmlArticleContentTests
{
    const string XLinkNamespace = "http://www.w3.org/1999/xlink";

    [Test]
    public void PreservesMeaningfulStructuredAndPlaceholderContent()
    {
        HtmlDocument document = Html.Parse(
            "<article id=article>"
            + "<figure id=figure class=media><img id=placeholder alt=Lead>"
            + "<figcaption id=caption>Lead image</figcaption></figure>"
            + "<table id=table class=related><caption>Facts</caption>"
            + "<tr><td>Value</td></tr></table>"
            + "<pre id=pre class=sidebar><code id=code>sample()</code></pre>"
            + "<math id=math class=sponsor><mi>x</mi></math>"
            + "<p>" + Prose() + "</p></article>");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());
        HtmlNode content = result.GetContent();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(content.FindById("figure"), Is.Not.Null);
            Assert.That(content.FindById("caption"), Is.Not.Null);
            Assert.That(content.FindById("table"), Is.Not.Null);
            Assert.That(content.FindById("pre"), Is.Not.Null);
            Assert.That(content.FindById("code"), Is.Not.Null);
            Assert.That(content.FindById("math"), Is.Not.Null);
            Assert.That(content.FindById("placeholder"), Is.Not.Null);
            Assert.That(content.FindById("placeholder").HasAttribute("src"),
                Is.False);
        });
    }

    [Test]
    public void RewritesRelativeUrlsUsingTheSourceEffectiveBase()
    {
        HtmlDocument document = ParseWithBase(
            "<head><base href='/assets/'></head><body>"
            + "<article id=article>"
            + "<p><a id=link href='../story#part' "
            + "ping='track /audit'>Story</a> " + Prose() + "</p>"
            + "<figure class=media><img id=image src='lead.jpg' "
            + "srcset='small.jpg 1x, /large.jpg 2x'>"
            + "<figcaption>Lead</figcaption></figure>"
            + "<video id=video poster='../poster.jpg'>"
            + "<source id=source src='movie.mp4'></video>"
            + "<blockquote id=quote cite='/source'>Quote</blockquote>"
            + "<object id=object data='object.bin'></object>"
            + "<table id=table background='texture.png'>"
            + "<tr><td>Cell</td></tr></table>"
            + "<svg><image id=vector xlink:href='vector.svg'></image></svg>"
            + "<p><a id=absolute href='HTTPS://CDN.Example/Keep'>"
            + "Absolute</a></p></article></body>",
            "https://origin.test/news/page.html");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());
        HtmlNode content = result.GetContent();
        HtmlNode image = content.FindById("image");
        HtmlNode vector = content.FindById("vector");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetRewrittenUrlCount(), Is.EqualTo(10));
            Assert.That(content.GetOwnerDocument().GetBaseUrl(),
                Is.EqualTo("https://origin.test/assets/"));
            Assert.That(content.FindById("link").GetAttribute("href"),
                Is.EqualTo("https://origin.test/story#part"));
            Assert.That(content.FindById("link").GetAttribute("ping"),
                Is.EqualTo("https://origin.test/assets/track "
                    + "https://origin.test/audit"));
            Assert.That(image.GetAttribute("src"),
                Is.EqualTo("https://origin.test/assets/lead.jpg"));
            Assert.That(image.GetAttribute("srcset"), Is.EqualTo(
                "https://origin.test/assets/small.jpg 1x, "
                + "https://origin.test/large.jpg 2x"));
            Assert.That(content.FindById("video").GetAttribute("poster"),
                Is.EqualTo("https://origin.test/poster.jpg"));
            Assert.That(content.FindById("source").GetAttribute("src"),
                Is.EqualTo("https://origin.test/assets/movie.mp4"));
            Assert.That(content.FindById("quote").GetAttribute("cite"),
                Is.EqualTo("https://origin.test/source"));
            Assert.That(content.FindById("object").GetAttribute("data"),
                Is.EqualTo("https://origin.test/assets/object.bin"));
            Assert.That(content.FindById("table").GetAttribute("background"),
                Is.EqualTo("https://origin.test/assets/texture.png"));
            Assert.That(vector.GetAttributeInNamespace(XLinkNamespace, "href"),
                Is.EqualTo("https://origin.test/assets/vector.svg"));
            Assert.That(content.FindById("absolute").GetAttribute("href"),
                Is.EqualTo("HTTPS://CDN.Example/Keep"));
            Assert.That(document.FindById("link").GetAttribute("href"),
                Is.EqualTo("../story#part"));
        });
    }

    [Test]
    public void LeavesInvalidSourceSetsAndAbsoluteUrlsUnchanged()
    {
        HtmlDocument document = ParseWithBase(
            "<article id=article><img id=image src=relative.jpg "
            + "srcset=',, broken.png 1x'>"
            + "<p><a id=mail href='mailto:reader@example.test'>Mail</a> "
            + Prose() + "</p></article>",
            "https://example.test/base/");

        HtmlArticleCleanupResult result = Clean(document,
            HtmlArticleOptions.CreateDefault());
        HtmlNode content = result.GetContent();

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess(), Is.True);
            Assert.That(result.GetRewrittenUrlCount(), Is.EqualTo(1));
            Assert.That(content.FindById("image").GetAttribute("src"),
                Is.EqualTo("https://example.test/base/relative.jpg"));
            Assert.That(content.FindById("image").GetAttribute("srcset"),
                Is.EqualTo(",, broken.png 1x"));
            Assert.That(content.FindById("mail").GetAttribute("href"),
                Is.EqualTo("mailto:reader@example.test"));
        });
    }

    [Test]
    public void MissingBasesStayRelativeAndUrlLimitsAreIncomplete()
    {
        HtmlDocument withoutBase = Html.Parse(
            "<article id=article><p><a id=relative href=child>Link</a> "
            + Prose() + "</p></article>");
        HtmlArticleCleanupResult unchanged = Clean(withoutBase,
            HtmlArticleOptions.CreateDefault());
        HtmlDocument limitedDocument = ParseWithBase(
            "<article id=article><p>" + Prose()
            + "</p><img src=child.png></article>",
            "https://example.test/a/base/that/is/too/long/");
        HtmlArticleOptions limitedOptions = HtmlArticleOptions.CreateDefault();
        HtmlLimits limits = HtmlLimits.CreateDefault();
        limits.SetMaximumUrlCharacters(24);
        limitedOptions.SetLimits(limits);
        HtmlArticleCleanupResult limited = Clean(limitedDocument,
            limitedOptions);

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.IsSuccess(), Is.True);
            Assert.That(unchanged.GetRewrittenUrlCount(), Is.EqualTo(0));
            Assert.That(unchanged.GetContent().FindById("relative")
                .GetAttribute("href"), Is.EqualTo("child"));
            Assert.That(limited.IsSuccess(), Is.False);
            Assert.That(limited.IsComplete(), Is.False);
            Assert.That(limited.GetContent(), Is.Null);
        });
    }

    static HtmlArticleCleanupResult Clean(HtmlDocument document,
        HtmlArticleOptions options)
    {
        HtmlNode candidate = document.FindById("article");
        HtmlArticleCandidateSelection selection =
            new HtmlArticleCandidateSelection(true, true, candidate,
                new HtmlNode[]
                {
                    candidate
                }, new HtmlArticleCandidate[0]);
        return HtmlArticleExtractor.CleanCandidate(selection, options,
            CancellationToken.None);
    }

    static HtmlDocument ParseWithBase(string html, string baseUrl)
    {
        HtmlParseOptions options = HtmlParseOptions.CreateDefault();
        options.SetBaseUrl(baseUrl);
        return Html.Parse(html, options, CancellationToken.None).GetDocument();
    }

    static string Prose()
    {
        return new string('a', 260) + ",,,,,,,,,, useful ending.";
    }
}
