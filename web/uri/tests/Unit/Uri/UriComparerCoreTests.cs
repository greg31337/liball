namespace uri.Tests;

public class UriComparerCoreTests
{
    [TestCase("")]
    [TestCase("x:a")]
    [TestCase("//user@host:80/path?query#fragment")]
    [TestCase("x://[2001:0DB8::1]:080/a%2fb?#")]
    public void ExactComparisonMatchesIdenticalSerializedReferences(string text)
    {
        UriComparisonCoreResult result = UriComparerCore.CompareExact(
            Parse(text),
            Parse(text));

        AssertEqual(result);
    }

    [TestCase("x:a", "y:a", UriComponent.Scheme)]
    [TestCase("x:a", "a", UriComponent.Scheme)]
    [TestCase("x:/a", "x://a", UriComponent.Authority)]
    [TestCase("x://user@host/", "x://host/", UriComponent.UserInfo)]
    [TestCase("x://@host/", "x://host/", UriComponent.UserInfo)]
    [TestCase("x://host-a/", "x://host-b/", UriComponent.Host)]
    [TestCase("x://[2001:db8::1]/", "x://[2001:0db8::1]/", UriComponent.Host)]
    [TestCase("x://host/", "x://host:/", UriComponent.Port)]
    [TestCase("x://host:80/", "x://host:81/", UriComponent.Port)]
    [TestCase("x://host/a", "x://host/b", UriComponent.Path)]
    [TestCase("x://host/a", "x://host/a?", UriComponent.Query)]
    [TestCase("x://host/a?q", "x://host/a?r", UriComponent.Query)]
    [TestCase("x://host/a?q", "x://host/a?q#", UriComponent.Fragment)]
    [TestCase("x://host/a?q#f", "x://host/a?q#g", UriComponent.Fragment)]
    public void ExactComparisonReportsTheFirstDifferentSerializedComponent(
        string left,
        string right,
        UriComponent expectedComponent)
    {
        UriComparisonCoreResult result = UriComparerCore.CompareExact(
            Parse(left),
            Parse(right));

        AssertDifferent(result, expectedComponent);
    }

    [Test]
    public void ExactComparisonUsesComponentStateInsteadOfProvenanceMetadata()
    {
        UriReference left = Parse("x://host/path");
        UriParseResult absolute = UriParserCore.ParseAbsoluteUri(
            "x://host/path".AsSpan());
        UriReference right = absolute.Reference;
        left.OriginalText = "left provenance";
        right.OriginalText = "right provenance";

        UriComparisonCoreResult result = UriComparerCore.CompareExact(left, right);

        Assert.Multiple(() =>
        {
            Assert.That(absolute.Success, Is.True);
            Assert.That(left.Kind, Is.Not.EqualTo(right.Kind));
            AssertEqual(result);
        });
    }

    [Test]
    public void MultipleExactDifferencesReportTheFirstInUriOrder()
    {
        UriComparisonCoreResult result = UriComparerCore.CompareExact(
            Parse("x://left@host-a:80/a?q#f"),
            Parse("x://right@host-b:81/b?r#g"));

        AssertDifferent(result, UriComponent.UserInfo);
    }

    [TestCase("HTTP://EXAMPLE.COM/%7eA/./b%2f?x=%41#%7e", "http://example.com/~A/b%2F?x=A#~")]
    [TestCase("x://%45XAMPLE.%43OM/", "x://example.com/")]
    [TestCase("x:/a/%2e%2e/b", "x:/b")]
    [TestCase("x://[2001:0DB8:0:0:0:0:0:1]/", "x://[2001:db8::1]/")]
    [TestCase("x://[0:0:0:0:0:FFFF:c000:0280]/", "x://[::ffff:192.0.2.128]/")]
    public void SyntaxComparisonRecognizesGenericNormalizationEquivalence(
        string left,
        string right)
    {
        UriComparisonCoreResult exact = UriComparerCore.CompareExact(
            Parse(left),
            Parse(right));
        UriComparisonCoreResult syntax = UriComparerCore.CompareSyntax(
            Parse(left),
            Parse(right));

        Assert.Multiple(() =>
        {
            Assert.That(exact.Equal, Is.False, left);
            AssertEqual(syntax);
        });
    }

    [TestCase("x:/a%2Fb", "x:/a/b", UriComponent.Path)]
    [TestCase("x:?a=1&b=2", "x:?b=2&a=1", UriComponent.Query)]
    [TestCase("x:?Query", "x:?query", UriComponent.Query)]
    [TestCase("x:#Fragment", "x:#fragment", UriComponent.Fragment)]
    [TestCase("x:", "x:?", UriComponent.Query)]
    [TestCase("x:", "x:#", UriComponent.Fragment)]
    [TestCase("x://host", "x://host/", UriComponent.Path)]
    [TestCase("x://host", "x://host:", UriComponent.Port)]
    [TestCase("http://host/", "http://host:80/", UriComponent.Port)]
    public void SyntaxComparisonPreservesDistinctionsNotGenericallyEquivalent(
        string left,
        string right,
        UriComponent expectedComponent)
    {
        UriComparisonCoreResult result = UriComparerCore.CompareSyntax(
            Parse(left),
            Parse(right));

        AssertDifferent(result, expectedComponent);
    }

    [Test]
    public void SyntaxComparisonReportsTheFirstDifferenceAfterNormalization()
    {
        UriComparisonCoreResult result = UriComparerCore.CompareSyntax(
            Parse("HTTP://EXAMPLE.COM/a/./b?same"),
            Parse("http://example.com/a/c?different"));

        AssertDifferent(result, UriComponent.Path);
    }

    [TestCase("http://host", "http://host/")]
    [TestCase("http://host:/", "http://host/")]
    [TestCase("http://host:80/", "http://host/")]
    [TestCase("http://host:080", "http://host/")]
    public void ExplicitProfileComparisonRecognizesSelectedSchemeRules(
        string left,
        string right)
    {
        UriSchemeProfile profile = CreateHttpProfile();

        UriComparisonCoreResult result = UriComparerCore.CompareWithSchemeProfile(
            Parse(left),
            Parse(right),
            profile);

        AssertEqual(result);
    }

    [TestCase("x:", "x:?")]
    [TestCase("x:", "x:#")]
    [TestCase("x:", "x:?#")]
    public void ExplicitProfileCanSelectEmptyDelimiterCleanup(
        string left,
        string right)
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "x";
        profile.RemoveEmptyQuery = true;
        profile.RemoveEmptyFragment = true;

        UriComparisonCoreResult result = UriComparerCore.CompareWithSchemeProfile(
            Parse(left),
            Parse(right),
            profile);

        AssertEqual(result);
    }

    [TestCase("https://host:80", "https://host", UriComponent.Port)]
    [TestCase("https://host", "https://host/", UriComponent.Path)]
    [TestCase("http://host?value", "http://host", UriComponent.Query)]
    [TestCase("http://host#value", "http://host", UriComponent.Fragment)]
    public void ExplicitProfilePreservesUnselectedOrMismatchedDistinctions(
        string left,
        string right,
        UriComponent expectedComponent)
    {
        UriSchemeProfile profile = CreateHttpProfile();

        UriComparisonCoreResult result = UriComparerCore.CompareWithSchemeProfile(
            Parse(left),
            Parse(right),
            profile);

        AssertDifferent(result, expectedComponent);
    }

    [TestCase("http://host", "http://host/")]
    [TestCase("https://host:443", "https://host/")]
    [TestCase("ws://host:80", "ws://host/")]
    [TestCase("wss://host:443", "wss://host/")]
    [TestCase("ftp://host:21", "ftp://host/")]
    public void BuiltInProfileComparisonUsesEachSchemesDeclaredDefaults(
        string left,
        string right)
    {
        UriComparisonCoreResult result = UriComparerCore.CompareWithBuiltInSchemeProfiles(
            Parse(left),
            Parse(right));

        AssertEqual(result);
    }

    [Test]
    public void BuiltInComparisonDoesNotApplyProfilesToUnsupportedSchemes()
    {
        UriComparisonCoreResult result = UriComparerCore.CompareWithBuiltInSchemeProfiles(
            Parse("x://host"),
            Parse("x://host/"));

        AssertDifferent(result, UriComponent.Path);
    }

    [Test]
    public void ComparisonsDoNotMutateOperandsOrProfiles()
    {
        UriReference left = Parse("HTTP://EXAMPLE.COM:080/a/./b?Q=%7e#F=%7e");
        UriReference right = Parse("http://example.com/a/b?Q=~#F=~");
        UriSchemeProfile profile = CreateHttpProfile();
        string leftBefore = Compose(left);
        string rightBefore = Compose(right);

        UriComparisonCoreResult syntax = UriComparerCore.CompareSyntax(left, right);
        UriComparisonCoreResult profiled = UriComparerCore.CompareWithSchemeProfile(
            left,
            right,
            profile);

        Assert.Multiple(() =>
        {
            AssertDifferent(syntax, UriComponent.Port);
            AssertEqual(profiled);
            Assert.That(Compose(left), Is.EqualTo(leftBefore));
            Assert.That(Compose(right), Is.EqualTo(rightBefore));
            Assert.That(profile.Scheme, Is.EqualTo("http"));
            Assert.That(profile.DefaultPort, Is.EqualTo(80));
            Assert.That(profile.RemoveEmptyQuery, Is.False);
            Assert.That(profile.RemoveEmptyFragment, Is.False);
        });
    }

    [Test]
    public void ComparisonIsSymmetricAndReportsTheSameComponent()
    {
        UriReference left = Parse("HTTP://EXAMPLE.COM/a?left");
        UriReference right = Parse("http://example.com/a?right");

        UriComparisonCoreResult forward = UriComparerCore.CompareSyntax(left, right);
        UriComparisonCoreResult reverse = UriComparerCore.CompareSyntax(right, left);

        Assert.Multiple(() =>
        {
            AssertDifferent(forward, UriComponent.Query);
            AssertDifferent(reverse, UriComponent.Query);
        });
    }

    [Test]
    public void ComparersRejectMissingReferencesOrProfile()
    {
        UriReference reference = Parse("x:a");

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriComparerCore.CompareExact(null, reference));
            Assert.Throws<ArgumentException>(() =>
                UriComparerCore.CompareSyntax(reference, null));
            Assert.Throws<ArgumentException>(() =>
                UriComparerCore.CompareWithSchemeProfile(reference, reference, null));
            Assert.Throws<ArgumentException>(() =>
                UriComparerCore.CompareWithBuiltInSchemeProfiles(null, reference));
        });
    }

    private static UriSchemeProfile CreateHttpProfile()
    {
        UriSchemeProfile profile = new UriSchemeProfile();
        profile.Scheme = "http";
        profile.NormalizeEmptyAuthorityPath = true;
        profile.RemoveEmptyPort = true;
        profile.HasDefaultPort = true;
        profile.DefaultPort = 80;
        return profile;
    }

    private static UriReference Parse(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }

    private static string Compose(UriReference reference)
    {
        UriCompositionResult result = UriComposerCore.RecomposeParsedReference(
            reference,
            new OperationLimits());
        Assert.That(result.Success, Is.True, result.Diagnostic?.Message);
        return result.Text;
    }

    private static void AssertEqual(UriComparisonCoreResult result)
    {
        Assert.That(result.Equal, Is.True);
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
    }

    private static void AssertDifferent(
        UriComparisonCoreResult result,
        UriComponent component)
    {
        Assert.That(result.Equal, Is.False);
        Assert.That(result.DifferentComponent, Is.EqualTo(component));
    }
}
