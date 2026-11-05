namespace uri.Tests;

public class Rfc8141LocatorConformanceTests
{
    [Test]
    public void SectionTwoWeatherExampleCopiesQComponentToLocatorQuery()
    {
        const string urn =
            "urn:example:weather?=op=map&lat=39.56&lon=-104.85&datetime=1969-07-21T02:56:15Z";
        const string locator = "https://weatherapp.example";
        const string expected =
            "https://weatherapp.example?op=map&lat=39.56&lon=-104.85&datetime=1969-07-21T02:56:15Z";

        string result = Apply(
            urn,
            locator,
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(expected));
    }

    [TestCase(UrnLocatorCollisionMode.Reject)]
    [TestCase(UrnLocatorCollisionMode.KeepLocator)]
    [TestCase(UrnLocatorCollisionMode.ReplaceLocator)]
    public void EveryQueryPolicyCopiesWhenLocatorQueryIsAbsent(
        UrnLocatorCollisionMode mode)
    {
        string result = Apply(
            "urn:example:value?=urn-query",
            "https://example.test/resource",
            mode,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(
            "https://example.test/resource?urn-query"));
    }

    [TestCase(UrnLocatorCollisionMode.Reject)]
    [TestCase(UrnLocatorCollisionMode.KeepLocator)]
    [TestCase(UrnLocatorCollisionMode.ReplaceLocator)]
    public void EveryFragmentPolicyCopiesWhenLocatorFragmentIsAbsent(
        UrnLocatorCollisionMode mode)
    {
        string result = Apply(
            "urn:example:value#urn-fragment",
            "https://example.test/resource",
            UrnLocatorCollisionMode.Reject,
            mode);

        Assert.That(result, Is.EqualTo(
            "https://example.test/resource#urn-fragment"));
    }

    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource?locator-query")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource?urn-query")]
    public void NonRejectingQueryPoliciesChooseTheirDeclaredSource(
        UrnLocatorCollisionMode mode,
        string expected)
    {
        string result = Apply(
            "urn:example:value?=urn-query",
            "https://example.test/resource?locator-query",
            mode,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RejectQueryPolicyReportsAQueryCollision()
    {
        UriResolutionException exception = Assert.Throws<
            UriResolutionException>(() => Apply(
                "urn:example:value?=urn-query",
                "https://example.test/resource?locator-query",
                UrnLocatorCollisionMode.Reject,
                UrnLocatorCollisionMode.Reject))!;

        AssertCollision(exception, UriComponent.Query);
    }

    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource#locator-fragment")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource#urn-fragment")]
    public void NonRejectingFragmentPoliciesChooseTheirDeclaredSource(
        UrnLocatorCollisionMode mode,
        string expected)
    {
        string result = Apply(
            "urn:example:value#urn-fragment",
            "https://example.test/resource#locator-fragment",
            UrnLocatorCollisionMode.Reject,
            mode);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void RejectFragmentPolicyReportsAFragmentCollision()
    {
        UriResolutionException exception = Assert.Throws<
            UriResolutionException>(() => Apply(
                "urn:example:value#urn-fragment",
                "https://example.test/resource#locator-fragment",
                UrnLocatorCollisionMode.Reject,
                UrnLocatorCollisionMode.Reject))!;

        AssertCollision(exception, UriComponent.Fragment);
    }

    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource?locator-query#locator-fragment")]
    [TestCase(
        UrnLocatorCollisionMode.KeepLocator,
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource?locator-query#urn-fragment")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        UrnLocatorCollisionMode.KeepLocator,
        "https://example.test/resource?urn-query#locator-fragment")]
    [TestCase(
        UrnLocatorCollisionMode.ReplaceLocator,
        UrnLocatorCollisionMode.ReplaceLocator,
        "https://example.test/resource?urn-query#urn-fragment")]
    public void QueryAndFragmentPoliciesOperateIndependently(
        UrnLocatorCollisionMode queryMode,
        UrnLocatorCollisionMode fragmentMode,
        string expected)
    {
        string result = Apply(
            "urn:example:value?=urn-query#urn-fragment",
            "https://example.test/resource?locator-query#locator-fragment",
            queryMode,
            fragmentMode);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void QueryRejectionPrecedesFragmentRejection()
    {
        UriResolutionException exception = Assert.Throws<
            UriResolutionException>(() => Apply(
                "urn:example:value?=urn-query#urn-fragment",
                "https://example.test/resource?locator-query#locator-fragment",
                UrnLocatorCollisionMode.Reject,
                UrnLocatorCollisionMode.Reject))!;

        AssertCollision(exception, UriComponent.Query);
    }

    [Test]
    public void FragmentRejectionStillAppliesAfterKeepingTheQuery()
    {
        UriResolutionException exception = Assert.Throws<
            UriResolutionException>(() => Apply(
                "urn:example:value?=urn-query#urn-fragment",
                "https://example.test/resource?locator-query#locator-fragment",
                UrnLocatorCollisionMode.KeepLocator,
                UrnLocatorCollisionMode.Reject))!;

        AssertCollision(exception, UriComponent.Fragment);
    }

    [Test]
    public void ExistingEmptyQueryDelimiterCountsAsPresent()
    {
        UriResolutionException rejected = Assert.Throws<
            UriResolutionException>(() => Apply(
                "urn:example:value?=urn-query",
                "https://example.test/resource?",
                UrnLocatorCollisionMode.Reject,
                UrnLocatorCollisionMode.Reject))!;
        string kept = Apply(
            "urn:example:value?=urn-query",
            "https://example.test/resource?",
            UrnLocatorCollisionMode.KeepLocator,
            UrnLocatorCollisionMode.Reject);
        string replaced = Apply(
            "urn:example:value?=urn-query",
            "https://example.test/resource?",
            UrnLocatorCollisionMode.ReplaceLocator,
            UrnLocatorCollisionMode.Reject);

        Assert.Multiple(() =>
        {
            AssertCollision(rejected, UriComponent.Query);
            Assert.That(kept, Is.EqualTo(
                "https://example.test/resource?"));
            Assert.That(replaced, Is.EqualTo(
                "https://example.test/resource?urn-query"));
        });
    }

    [Test]
    public void EmptyUrnFragmentIsPresentAndParticipatesInPolicy()
    {
        string copied = Apply(
            "urn:example:value#",
            "https://example.test/resource",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);
        string kept = Apply(
            "urn:example:value#",
            "https://example.test/resource#locator-fragment",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.KeepLocator);
        string replaced = Apply(
            "urn:example:value#",
            "https://example.test/resource#locator-fragment",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.ReplaceLocator);

        Assert.Multiple(() =>
        {
            Assert.That(copied, Is.EqualTo(
                "https://example.test/resource#"));
            Assert.That(kept, Is.EqualTo(
                "https://example.test/resource#locator-fragment"));
            Assert.That(replaced, Is.EqualTo(
                "https://example.test/resource#"));
        });
    }

    [Test]
    public void AbsentUrnQAndFComponentsLeaveLocatorDelimitersUntouched()
    {
        string result = Apply(
            "urn:example:value",
            "https://example.test/resource?#",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(
            "https://example.test/resource?#"));
    }

    [Test]
    public void QAndFTransferPreservesTheirLexicalData()
    {
        string result = Apply(
            "urn:example:value?=a/b?c%2f%AF#d/e?f%2a",
            "https://example.test/resource",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(
            "https://example.test/resource?a/b?c%2f%AF#d/e?f%2a"));
    }

    [TestCase("urn:example:value?+CCResolve:cc=uk")]
    [TestCase("urn:example:value?+service/path?parameter=value")]
    [TestCase("urn:example:value?+r%2f%AF")]
    public void RComponentAloneNeverChangesTheResolvedLocator(string urn)
    {
        const string locator =
            "https://example.test/resource?locator-query#locator-fragment";

        string result = Apply(
            urn,
            locator,
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(locator));
    }

    [Test]
    public void DifferentRComponentsProduceTheSameGenericLocatorResult()
    {
        const string locator = "https://example.test/resource";

        string first = Apply(
            "urn:example:value?+first-service",
            locator,
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);
        string second = Apply(
            "urn:example:value?+second-service?parameter=value",
            locator,
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(locator));
            Assert.That(second, Is.EqualTo(locator));
            Assert.That(second, Is.EqualTo(first));
        });
    }

    [Test]
    public void RComponentDoesNotInterfereWithFollowingQAndFTransfer()
    {
        string result = Apply(
            "urn:example:value?+resolver-data?=urn-query#urn-fragment",
            "https://example.test/resource",
            UrnLocatorCollisionMode.Reject,
            UrnLocatorCollisionMode.Reject);

        Assert.That(result, Is.EqualTo(
            "https://example.test/resource?urn-query#urn-fragment"));
    }

    private static string Apply(
        string urn,
        string locator,
        UrnLocatorCollisionMode queryMode,
        UrnLocatorCollisionMode fragmentMode)
    {
        UrnLocatorMergeOptions options = new UrnLocatorMergeOptions();
        options.QueryCollisionMode = queryMode;
        options.FragmentCollisionMode = fragmentMode;
        IUrnLocatorUtility utility = new UrnLocatorUtility();
        return utility.ApplyUrnComponentsToLocator(
            urn,
            locator,
            options);
    }

    private static void AssertCollision(
        UriResolutionException exception,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic.Domain, Is.EqualTo(
                DiagnosticDomain.Urn));
            Assert.That(exception.Diagnostic.Code, Is.EqualTo(
                UriDiagnosticCode.UnresolvableReference));
            Assert.That(exception.Diagnostic.Severity, Is.EqualTo(
                DiagnosticSeverity.Error));
            Assert.That(exception.Diagnostic.Component, Is.EqualTo(
                component));
        });
    }
}
