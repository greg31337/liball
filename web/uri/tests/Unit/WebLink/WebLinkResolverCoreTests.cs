namespace uri.Tests;

public class WebLinkResolverCoreTests
{
    [Test]
    public void RelativeTargetResolvesAgainstRepresentationContext()
    {
        WebLink link = ParseLink("<../next?x=1#part>");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://example.test/a/b/index?old=1",
                new OperationLimits());

        AssertSuccess(
            result,
            link,
            "https://example.test/a/next?x=1#part",
            "https://example.test/a/b/index?old=1",
            true);
    }

    [Test]
    public void RelativeAnchorOverridesContextBeforeTargetResolution()
    {
        WebLink link = ParseLink(
            "<child?target=yes#target>; " +
            "anchor=\"../context/base?anchor=yes#context\"");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://example.test/a/b/index?representation=yes",
                new OperationLimits());

        AssertSuccess(
            result,
            link,
            "https://example.test/a/context/child?target=yes#target",
            "https://example.test/a/context/base?anchor=yes#context",
            true);
    }

    [Test]
    public void AbsoluteAnchorEstablishesContextForAnonymousRepresentation()
    {
        WebLink link = ParseLink(
            "<../target>; " +
            "anchor=\"https://context.test/a/./b#section\"");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                null,
                new OperationLimits());

        AssertSuccess(
            result,
            link,
            "https://context.test/target",
            "https://context.test/a/b#section",
            true);
    }

    [Test]
    public void AbsoluteTargetSucceedsWithoutAnyLinkContext()
    {
        WebLink link = ParseLink(
            "<https://target.test/a/../resource?x#y>");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                null,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link, Is.SameAs(link));
            Assert.That(result.Target,
                Is.EqualTo("https://target.test/resource?x#y"));
            Assert.That(result.HasContext, Is.False);
            Assert.That(result.Context, Is.Empty);
            Assert.That(result.AnchorHonored, Is.True);
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    [Test]
    public void RelativeTargetWithoutContextReportsUnresolvableOutcome()
    {
        WebLink link = ParseLink("<relative>");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                null,
                new OperationLimits());

        AssertFailure(
            result,
            link,
            UriDiagnosticCode.UnresolvableReference,
            UriComponent.LinkTarget,
            true);
    }

    [Test]
    public void UnhonoredAnchorRetainsSourceAndIndependentAbsoluteTarget()
    {
        WebLink link = ParseLink(
            "<https://target.test/resource>; anchor=\"../context\"");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                null,
                new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                link,
                UriDiagnosticCode.UnhonoredAnchor,
                UriComponent.LinkContext,
                false);
            Assert.That(result.Target,
                Is.EqualTo("https://target.test/resource"));
            Assert.That(result.HasContext, Is.False);
            Assert.That(result.Context, Is.Empty);
            Assert.That(result.Diagnostic!.Message,
                Is.EqualTo(WebLinkResolverCore.UnhonoredAnchorMessage));
        });
    }

    [Test]
    public void InvalidAnchorNeverFallsBackToRepresentationContext()
    {
        WebLink link = new WebLink();
        link.Target = "relative-target";
        link.HasAnchor = true;
        link.Anchor = "bad anchor";

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://example.test/representation/base",
                new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(
                result,
                link,
                UriDiagnosticCode.UnhonoredAnchor,
                UriComponent.LinkContext,
                false);
            Assert.That(result.Target, Is.Empty);
            Assert.That(result.HasContext, Is.False);
            Assert.That(result.Context, Is.Empty);
        });
    }

    [Test]
    public void QueryAndFragmentStatesSurviveBothResolutionSteps()
    {
        WebLink link = ParseLink(
            "<#target>; anchor=\"?anchor=#context\"");

        WebLinkResolutionResult result =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://example.test/path/file?representation=",
                new OperationLimits());

        AssertSuccess(
            result,
            link,
            "https://example.test/path/file?anchor=#target",
            "https://example.test/path/file?anchor=#context",
            true);
    }

    [Test]
    public void BatchResolutionPreservesOrderAndReturnsOneOutcomePerLink()
    {
        WebLink first = ParseLink("<one>");
        WebLink second = ParseLink("<https://other.test/two>");
        WebLink third = ParseLink("<../three>; anchor=\"context/base\"");
        WebLinkSet linkSet = new WebLinkSet();
        linkSet.Links = new WebLink[] { first, second, third };

        WebLinkResolutionResult[] results =
            WebLinkResolverCore.ResolveWebLinks(
                linkSet,
                "https://example.test/a/index",
                new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(results.All(result => result.Success), Is.True);
            Assert.That(results.Select(result => result.Link),
                Is.EqualTo(new WebLink[] { first, second, third }));
            Assert.That(results.Select(result => result.Target),
                Is.EqualTo(new string[]
                {
                    "https://example.test/a/one",
                    "https://other.test/two",
                    "https://example.test/a/three"
                }));
            Assert.That(results[2].Context,
                Is.EqualTo("https://example.test/a/context/base"));
        });
    }

    [Test]
    public void InvalidRepresentationAndOutputLimitsProduceWebLinkDiagnostics()
    {
        WebLink link = ParseLink("<target>");
        WebLinkResolutionResult invalidContext =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "relative/context",
                new OperationLimits());

        OperationLimits limits = new OperationLimits();
        limits.MaxOutputCharacters =
            "https://example.test/base/target".Length - 1;
        WebLinkResolutionResult limited =
            WebLinkResolverCore.ResolveWebLink(
                link,
                "https://example.test/base/index",
                limits);

        Assert.Multiple(() =>
        {
            AssertFailure(
                invalidContext,
                link,
                UriDiagnosticCode.UnresolvableReference,
                UriComponent.LinkContext,
                true);
            Assert.That(invalidContext.Diagnostic!.Message,
                Is.EqualTo(
                    WebLinkResolverCore.InvalidRepresentationContextMessage));
            AssertFailure(
                limited,
                link,
                UriDiagnosticCode.LimitExceeded,
                UriComponent.Output,
                true);
        });
    }

    private static WebLink ParseLink(string text)
    {
        WebLinkParseResult parsed =
            WebLinkParserCore.ParseStrictFieldValueSpan(
                text.AsSpan(),
                new WebLinkParseOptions());
        Assert.That(parsed.Success, Is.True, parsed.Diagnostic?.Message);
        Assert.That(parsed.LinkSet.Links, Has.Length.EqualTo(1));
        return parsed.LinkSet.Links[0];
    }

    private static void AssertSuccess(
        WebLinkResolutionResult result,
        WebLink link,
        string target,
        string context,
        bool anchorHonored)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Link, Is.SameAs(link));
            Assert.That(result.Target, Is.EqualTo(target));
            Assert.That(result.HasContext, Is.True);
            Assert.That(result.Context, Is.EqualTo(context));
            Assert.That(result.AnchorHonored, Is.EqualTo(anchorHonored));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        WebLinkResolutionResult result,
        WebLink link,
        UriDiagnosticCode code,
        UriComponent component,
        bool anchorHonored)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Link, Is.SameAs(link));
            Assert.That(result.AnchorHonored, Is.EqualTo(anchorHonored));
            Assert.That(result.Diagnostic!.Domain,
                Is.EqualTo(DiagnosticDomain.WebLink));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
            Assert.That(result.Diagnostic.Component,
                Is.EqualTo(component));
        });
    }
}
