using System.Text;
using email;

namespace email.Tests;

public class EmailRelatedIndexTests
{
    [Test]
    public void UriNormalizationCanonicalizesContentIdsAndLocations()
    {
        Assert.That(new EmailRelatedUri().NormalizeContentIdWithValue(" CID:Logo%40Example.TEST "), Is.EqualTo("Logo@example.test"));
        Assert.That(new EmailRelatedUri().NormalizeContentIdWithValue("<Local@[IPv6:ABCD::1]>"), Is.EqualTo("Local@[IPv6:ABCD::1]"));
        Assert.That(
            new EmailRelatedUri().NormalizeLocation(" HTTPS://User@Example.COM/a/./b/../logo%2epng?X=%7e#preview "),
            Is.EqualTo("https://User@example.com/a/logo.png?X=~"));
        Assert.That(new EmailRelatedUri().NormalizeLocation("images/a/../logo%2Flarge.png#x"), Is.EqualTo("images/logo%2Flarge.png"));
        Assert.That(new EmailRelatedUri().NormalizeLocation("//CDN.Example/a/../logo.png"), Is.EqualTo("//cdn.example/logo.png"));
        Assert.That(new EmailRelatedUri().ResolveLocation("../images/logo.png", "https://EXAMPLE.test/pages/mail/index.html"), Is.EqualTo("https://example.test/pages/images/logo.png"));
        Assert.That(new EmailRelatedUri().ResolveLocation("/logo.png", "https://EXAMPLE.test/pages/index.html"), Is.EqualTo("https://example.test/logo.png"));
        Assert.That(new EmailRelatedUri().IsAbsolute("cid:logo@example.test"), Is.True);
        Assert.That(new EmailRelatedUri().IsAbsolute("images/logo.png"), Is.False);

        Assert.Throws<ArgumentException>(() => new EmailRelatedUri().NormalizeContentIdWithValue("not-an-id"));
        Assert.Throws<ArgumentException>(() => new EmailRelatedUri().NormalizeLocation("#only-a-fragment"));
    }

    [Test]
    public void HtmlDiscoveryFindsAttributesSrcSetsAndCssInDocumentOrder()
    {
        const string html = "<IMG SRC='cid:one@example.test' srcset=\"small.png 1x, large.png 2x\" "
            + "style=\"background: URL(&quot;inline.png&quot;)\">"
            + "<style>.hero { background-image: url('hero.png'); }</style>"
            + "<a href=\"docs/readme.html?x=1&amp;y=2\">read</a>"
            + "<svg xlink:href='icons.svg#mark'></svg>"
            + "<a href='#local'>local</a>"
            + "text url('not-css.png')<script>url('not-script.png')</script>"
            + "<!-- <img src='ignored.png'> -->";

        List<string> references = new EmailRelatedIndexOperations().FindHtmlReferences(html);

        Assert.That(references, Is.EqualTo(new[]
        {
            "cid:one@example.test",
            "small.png",
            "large.png",
            "inline.png",
            "hero.png",
            "docs/readme.html?x=1&y=2",
            "icons.svg#mark"
        }));
    }

    [Test]
    public void IndexResolvesResourcesReportsDuplicatesAndDeduplicatesMissingReferences()
    {
        RelatedTree tree = CreateRelatedTree();

        EmailRelatedIndex index = new EmailRelatedIndexOperations().BuildWithRoot(tree.Root);

        Assert.That(index.ResolveContentId("cid:Logo%40EXAMPLE.TEST"), Is.SameAs(tree.FirstLogo));
        Assert.That(index.GetByContentId("<Logo@example.test>"), Is.EqualTo(new[] { tree.FirstLogo, tree.SecondLogo }));
        Assert.That(index.ResolveContentId("logo@example.test"), Is.Null);
        Assert.That(index.ResolveLocation("images/logo.png#ignored"), Is.SameAs(tree.FirstLogo));
        Assert.That(index.ResolveLocationAgainstBase("styles/bg.png", "pages/index.html"), Is.SameAs(tree.Background));
        Assert.That(index.GetByLocation("images/./logo%2epng").Count, Is.EqualTo(2));

        Assert.That(index.GetDuplicateCount(), Is.EqualTo(2));
        Assert.That(index.GetDuplicateAt(0).GetKind(), Is.EqualTo(EmailRelatedReferenceKind.ContentId));
        Assert.That(index.GetDuplicateAt(0).GetNormalizedValue(), Is.EqualTo("Logo@example.test"));
        Assert.That(index.GetDuplicateAt(0).GetEntities(), Is.EqualTo(new[] { tree.FirstLogo, tree.SecondLogo }));
        Assert.That(index.GetDuplicateAt(1).GetKind(), Is.EqualTo(EmailRelatedReferenceKind.ContentLocation));

        Assert.That(index.GetReferenceCount(), Is.EqualTo(8));
        Assert.That(index.GetReferenceAt(0).GetResolvedEntity(), Is.SameAs(tree.FirstLogo));
        Assert.That(index.GetReferenceAt(0).GetKind(), Is.EqualTo(EmailRelatedReferenceKind.ContentId));
        Assert.That(index.GetReferenceAt(0).GetSourceEntity(), Is.SameAs(tree.Html));
        Assert.That(index.GetReferenceAt(0).GetSourcePath().GetDisplayText(), Is.EqualTo("root/1"));
        Assert.That(index.GetReferenceAt(1).GetResolvedEntity(), Is.SameAs(tree.FirstLogo));
        Assert.That(index.GetReferenceAt(5).GetKind(), Is.EqualTo(EmailRelatedReferenceKind.External));
        Assert.That(index.GetReferenceAt(5).IsMissing(), Is.False);
        Assert.That(index.GetReferenceAt(6).GetResolvedEntity(), Is.SameAs(tree.Background));
        Assert.That(index.GetReferenceAt(7).GetKind(), Is.EqualTo(EmailRelatedReferenceKind.External));

        List<EmailRelatedReference> missing = index.GetMissingReferences();
        Assert.That(missing.Count, Is.EqualTo(2));
        Assert.That(missing[0].GetNormalizedValue(), Is.EqualTo("missing@example.test"));
        Assert.That(missing[0].GetKind(), Is.EqualTo(EmailRelatedReferenceKind.ContentId));
        Assert.That(missing[1].GetNormalizedValue(), Is.EqualTo("pages/missing/file.png?x=1&y=2"));
        Assert.That(missing[1].GetKind(), Is.EqualTo(EmailRelatedReferenceKind.ContentLocation));

        index.GetReferences().Clear();
        index.GetDuplicates().Clear();
        missing.Clear();
        Assert.That(index.GetReferenceCount(), Is.EqualTo(8));
        Assert.That(index.GetDuplicateCount(), Is.EqualTo(2));
        Assert.That(index.GetMissingReferences().Count, Is.EqualTo(2));
    }

    [Test]
    public void IndexDecodesTransferEncodedHtmlAndAcceptsMessages()
    {
        string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes("<img src='cid:asset@example.test'>"));
        MimeEntity html = ParseEntity("Content-Type: text/html; charset=utf-8\r\nContent-Transfer-Encoding: base64\r\n\r\n" + encoded);
        MimeEntity asset = ParseEntity("Content-Type: image/png\r\nContent-ID: <asset@EXAMPLE.TEST>\r\n\r\npng");
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { html, asset });
        EmailMessage message = new EmailMessageOperations().CreateWith();
        message.SetBody(root);

        EmailRelatedIndex index = new EmailRelatedIndexOperations().BuildWithMessage(message);

        Assert.That(index.GetReferenceCount(), Is.EqualTo(1));
        Assert.That(index.GetReferenceAt(0).GetResolvedEntity(), Is.SameAs(asset));
        Assert.That(index.GetMissingReferences(), Is.Empty);
    }

    [Test]
    public void OptionsAreCopiedAndAllDiscoveryBoundsAreEnforced()
    {
        EmailRelatedIndexOptions options = new EmailRelatedIndexOptions();
        options.SetMaximumDepth(3);
        options.SetMaximumEntityCount(20);
        options.SetMaximumHtmlBytes(100);
        options.SetMaximumReferences(1);
        options.SetMaximumUrlCharacters(20);
        EmailRelatedIndexOptions clone = options.Clone();
        options.SetMaximumReferences(5);

        Assert.That(clone.GetMaximumDepth(), Is.EqualTo(3));
        Assert.That(clone.GetMaximumEntityCount(), Is.EqualTo(20));
        Assert.That(clone.GetMaximumHtmlBytes(), Is.EqualTo(100));
        Assert.That(clone.GetMaximumReferences(), Is.EqualTo(1));
        Assert.That(clone.GetMaximumUrlCharacters(), Is.EqualTo(20));

        MimeEntity twoReferences = new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<img src='a'><img src='b'>", "html", "utf-8");
        EmailLimitException referenceLimit = Assert.Throws<EmailLimitException>(() => new EmailRelatedIndexOperations().BuildWithRootAndOptions(twoReferences, clone));
        Assert.That(referenceLimit.GetLimitName(), Is.EqualTo("MaxRelatedReferenceCount"));

        EmailRelatedIndexOptions urlOptions = new EmailRelatedIndexOptions();
        urlOptions.SetMaximumUrlCharacters(3);
        EmailLimitException urlLimit = Assert.Throws<EmailLimitException>(() => new EmailRelatedIndexOperations().BuildWithRootAndOptions(new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("<img src='long'>", "html", "utf-8"), urlOptions));
        Assert.That(urlLimit.GetLimitName(), Is.EqualTo("MaxRelatedUrlCharacters"));

        EmailRelatedIndexOptions htmlOptions = new EmailRelatedIndexOptions();
        htmlOptions.SetMaximumHtmlBytes(4);
        EmailLimitException htmlLimit = Assert.Throws<EmailLimitException>(() => new EmailRelatedIndexOperations().BuildWithRootAndOptions(new EmailEntityFactory().CreateTextWithValueAndSubtypeAndCharset("12345", "html", "utf-8"), htmlOptions));
        Assert.That(htmlLimit.GetLimitName(), Is.EqualTo("MaxRelatedHtmlBytes"));

        EmailRelatedIndexOptions traversalOptions = new EmailRelatedIndexOptions();
        traversalOptions.SetMaximumEntityCount(1);
        MimeEntity multipart = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { new EmailEntityFactory().CreateTextWithValue("body") });
        EmailLimitException traversalLimit = Assert.Throws<EmailLimitException>(() => new EmailRelatedIndexOperations().BuildWithRootAndOptions(multipart, traversalOptions));
        Assert.That(traversalLimit.GetLimitName(), Is.EqualTo("MaxTraversalEntityCount"));

        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumDepth(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.SetMaximumReferences(0));
        Assert.Throws<ArgumentNullException>(() => new EmailRelatedIndexOperations().BuildWithRoot((MimeEntity)null!));
        Assert.Throws<ArgumentException>(() => new EmailRelatedIndexOperations().BuildWithMessage(new EmailMessageOperations().CreateWith()));
    }

    static RelatedTree CreateRelatedTree()
    {
        const string markup = "<img src='cid:Logo%40EXAMPLE.TEST'>"
            + "<img src='images/logo%2Epng#preview'>"
            + "<img src='cid:missing@EXAMPLE.TEST'>"
            + "<img src='cid:missing@example.test'>"
            + "<a href='missing/file.png?x=1&amp;y=2'>missing</a>"
            + "<img src='https://remote.example/image.png'>"
            + "<div style=\"background:url('styles/bg.png')\"></div>"
            + "<img src='data:image/png;base64,AA=='>"
            + "<!-- <img src='ignored.png'> -->";
        MimeEntity html = ParseEntity("Content-Type: text/html; charset=utf-8\r\nContent-Location: pages/index.html\r\n\r\n" + markup);
        MimeEntity firstLogo = ParseEntity("Content-Type: image/png\r\nContent-ID: <Logo@Example.TEST>\r\nContent-Location: images/./logo%2epng\r\n\r\none");
        MimeEntity secondLogo = ParseEntity("Content-Type: image/png\r\nContent-ID: <Logo@example.test>\r\nContent-Location: images/logo.png#copy\r\n\r\ntwo");
        MimeEntity background = ParseEntity("Content-Type: image/png\r\nContent-ID: <background@example.test>\r\nContent-Location: pages/styles/bg.png\r\n\r\nbg");
        MimeEntity root = new EmailEntityFactory().CreateMultipartWithSubtypeAndChildren("related", new List<MimeEntity> { html, firstLogo, secondLogo, background });
        return new RelatedTree(root, html, firstLogo, secondLogo, background);
    }

    static MimeEntity ParseEntity(string source)
    {
        EmailEntityParseResult result = new EmailParser().ParseEntityWithInputUsingValueArray(Encoding.UTF8.GetBytes(source));
        Assert.That(result.IsSuccess(), Is.True);
        return result.GetEntity()!;
    }
}

class RelatedTree
{
    public MimeEntity Root { get; }
    public MimeEntity Html { get; }
    public MimeEntity FirstLogo { get; }
    public MimeEntity SecondLogo { get; }
    public MimeEntity Background { get; }

    public RelatedTree(MimeEntity root, MimeEntity html, MimeEntity firstLogo, MimeEntity secondLogo, MimeEntity background)
    {
        Root = root;
        Html = html;
        FirstLogo = firstLogo;
        SecondLogo = secondLogo;
        Background = background;
    }
}
