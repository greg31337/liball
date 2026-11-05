namespace uri.Tests;

public class WebLinkResolutionBehaviorTests
{
    [TestCase("g?y#s", "http://a/b/c/g?y#s")]
    [TestCase("?y", "http://a/b/c/d;p?y")]
    [TestCase("#s", "http://a/b/c/d;p?q#s")]
    [TestCase("", "http://a/b/c/d;p?q")]
    [TestCase("?#", "http://a/b/c/d;p?#")]
    public void RelativeTargetFormsPreserveQueryAndFragmentState(
        string target,
        string expected)
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLink link = ParseLink("<" + target + ">");

        WebLinkResolutionResult result = resolver.ResolveWebLink(
            link,
            "http://a/b/c/d;p?q",
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Target, Is.EqualTo(expected));
            Assert.That(result.HasContext, Is.True);
            Assert.That(result.Context, Is.EqualTo("http://a/b/c/d;p?q"));
            Assert.That(result.AnchorHonored, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void RelativeAnchorResolvesBeforeTargetWithIndependentQfStates()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLink link = ParseLink(
            "<child?target=#target-fragment>; " +
            "anchor=\"../context/base?anchor=#context-fragment\"");

        WebLinkResolutionResult result = resolver.ResolveWebLink(
            link,
            "https://example.test/a/b/index?representation=",
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Context, Is.EqualTo(
                "https://example.test/a/context/base" +
                "?anchor=#context-fragment"));
            Assert.That(result.Target, Is.EqualTo(
                "https://example.test/a/context/child" +
                "?target=#target-fragment"));
            Assert.That(result.AnchorHonored, Is.True);
        });
    }

    [Test]
    public void AnonymousContextProducesIndependentBatchOutcomes()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLinkSet linkSet = ParseSet(
            "<relative>, " +
            "<https://target.test/absolute?x#f>, " +
            "<child>; anchor=\"https://context.test/base?c#f\"");

        WebLinkResolutionResult[] results = resolver.ResolveWebLinks(
            linkSet,
            null,
            new OperationLimits());

        Assert.That(results[0].Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results[0].Success, Is.False);
            Assert.That(results[0].Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.UnresolvableReference));
            Assert.That(results[0].Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkTarget));
            Assert.That(results[1].Success, Is.True);
            Assert.That(results[1].Target,
                Is.EqualTo("https://target.test/absolute?x#f"));
            Assert.That(results[1].HasContext, Is.False);
            Assert.That(results[2].Success, Is.True);
            Assert.That(results[2].Target,
                Is.EqualTo("https://context.test/child"));
            Assert.That(results[2].Context,
                Is.EqualTo("https://context.test/base?c#f"));
        });
    }

    [Test]
    public void UnhonoredRelativeAnchorDoesNotFallBackOrHideAbsoluteTarget()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLink link = ParseLink(
            "<https://target.test/resource?x#f>; " +
            "anchor=\"../relative-context\"");

        WebLinkResolutionResult result = resolver.ResolveWebLink(
            link,
            null,
            new OperationLimits());

        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Target,
                Is.EqualTo("https://target.test/resource?x#f"));
            Assert.That(result.HasContext, Is.False);
            Assert.That(result.Context, Is.Empty);
            Assert.That(result.AnchorHonored, Is.False);
            Assert.That(result.Diagnostic!.Code,
                Is.EqualTo(UriDiagnosticCode.UnhonoredAnchor));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(UriComponent.LinkContext));
        });
    }

    [Test]
    public void IriRepresentationAnchorAndTargetResolveToUriOutput()
    {
        IWebLinkResolver resolver = new WebLinkResolver();
        WebLink link = new WebLink();
        link.Target = "子?問=#片";
        link.HasAnchor = true;
        link.Anchor = "../文脈/base?錨=#節";

        WebLinkResolutionResult result = resolver.ResolveWebLink(
            link,
            "https://例え.テスト/基点/path/index?表現=",
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Context, Is.EqualTo(
                "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9" +
                "%E3%83%88/%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/base" +
                "?%E9%8C%A8=#%E7%AF%80"));
            Assert.That(result.Target, Is.EqualTo(
                "https://%E4%BE%8B%E3%81%88.%E3%83%86%E3%82%B9" +
                "%E3%83%88/%E5%9F%BA%E7%82%B9/%E6%96%87%E8%84%88/" +
                "%E5%AD%90?%E5%95%8F=#%E7%89%87"));
            Assert.That(result.AnchorHonored, Is.True);
            Assert.That(link.Target, Is.EqualTo("子?問=#片"));
            Assert.That(link.Anchor, Is.EqualTo("../文脈/base?錨=#節"));
        });
    }

    private static WebLink ParseLink(string fieldValue)
    {
        return ParseSet(fieldValue).Links[0];
    }

    private static WebLinkSet ParseSet(string fieldValue)
    {
        IWebLinkParser parser = new WebLinkParser();
        return parser.ParseLinkFieldValues(
            new string[] { fieldValue },
            new WebLinkParseOptions());
    }
}
