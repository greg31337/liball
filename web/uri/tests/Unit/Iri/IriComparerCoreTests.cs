namespace uri.Tests;

public class IriComparerCoreTests
{
    [Test]
    public void ExactComparisonAcceptsIdenticalUnicodeReferences()
    {
        string input = "x://usér@éxample.test:443/café?q=résumé#élève";

        UriComparisonResult result = IriComparerCore.CompareExact(
            input.AsSpan(),
            input.AsSpan(),
            new OperationLimits());

        AssertEquality(result, UriComparisonLevel.Exact);
    }

    [TestCase("x:a", "y:a", UriComponent.Scheme)]
    [TestCase("x:/a", "x://a", UriComponent.Authority)]
    [TestCase("x://u@h", "x://v@h", UriComponent.UserInfo)]
    [TestCase("x://é.test", "x://e.test", UriComponent.Host)]
    [TestCase("x://h:80", "x://h:81", UriComponent.Port)]
    [TestCase("x:/café", "x:/cafe", UriComponent.Path)]
    [TestCase("x:a?é", "x:a?e", UriComponent.Query)]
    [TestCase("x:a#é", "x:a#e", UriComponent.Fragment)]
    public void ExactComparisonReportsTheFirstDifferentComponent(
        string left,
        string right,
        UriComponent component)
    {
        UriComparisonResult result = IriComparerCore.CompareExact(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());

        AssertDifference(result, UriComparisonLevel.Exact, component);
    }

    [Test]
    public void ExactComparisonDistinguishesCanonicalUnicodeSequences()
    {
        UriComparisonResult result = IriComparerCore.CompareExact(
            "x:/café".AsSpan(),
            "x:/cafe\u0301".AsSpan(),
            new OperationLimits());

        AssertDifference(result, UriComparisonLevel.Exact, UriComponent.Path);
    }

    [Test]
    public void UnicodeComparisonAppliesNfcToEveryInternationalizedComponent()
    {
        string decomposed = "x://use\u0301r@e\u0301xample.test:443/" +
            "cafe\u0301?q=e\u0301#f=e\u0301";
        string composed = "x://usér@éxample.test:443/café?q=é#f=é";

        UriComparisonResult result = IriComparerCore.CompareUnicode(
            decomposed.AsSpan(),
            composed.AsSpan(),
            new OperationLimits());

        AssertEquality(result, UriComparisonLevel.IriUnicode);
    }

    [TestCase("X:a", "x:a", UriComponent.Scheme)]
    [TestCase("x://EXAMPLE.test", "x://example.test", UriComponent.Host)]
    [TestCase("x:/é", "x:/%C3%A9", UriComponent.Path)]
    [TestCase("x:/%7e", "x:/%7E", UriComponent.Path)]
    [TestCase("x:/a/../b", "x:/b", UriComponent.Path)]
    public void UnicodeComparisonDoesNotApplyUriSyntaxNormalization(
        string left,
        string right,
        UriComponent component)
    {
        UriComparisonResult result = IriComparerCore.CompareUnicode(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());

        AssertDifference(result, UriComparisonLevel.IriUnicode, component);
    }

    [TestCase("x:a", "x:a?", UriComponent.Query)]
    [TestCase("x:a", "x:a#", UriComponent.Fragment)]
    [TestCase("x://host", "x://host/", UriComponent.Path)]
    public void UnicodeComparisonPreservesDelimiterAndEmptyComponentState(
        string left,
        string right,
        UriComponent component)
    {
        UriComparisonResult result = IriComparerCore.CompareUnicode(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());

        AssertDifference(result, UriComparisonLevel.IriUnicode, component);
    }

    [TestCase("X://EXAMPLE.test/a", "x://example.test/a")]
    [TestCase("x:/é", "x:/%c3%a9")]
    [TestCase("x:/%7e", "x:/~")]
    [TestCase("x:/a/../b", "x:/b")]
    [TestCase("x://[2001:0db8::1]/", "x://[2001:db8::1]/")]
    public void MappedUriComparisonAppliesMappingAndGenericSyntaxNormalization(
        string left,
        string right)
    {
        UriComparisonResult result = IriComparerCore.CompareAsUri(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());

        AssertEquality(result, UriComparisonLevel.IriMappedUri);
    }

    [TestCase("x:/é", "x:/e\u0301", UriComponent.Path)]
    [TestCase("x:/a%2Fb", "x:/a/b", UriComponent.Path)]
    [TestCase("http://example.test:80/", "http://example.test/", UriComponent.Port)]
    [TestCase("x:a", "x:a?", UriComponent.Query)]
    [TestCase("x:a", "x:a#", UriComponent.Fragment)]
    public void MappedUriComparisonDoesNotAddNfcProfileOrDelimiterEquivalence(
        string left,
        string right,
        UriComponent component)
    {
        UriComparisonResult result = IriComparerCore.CompareAsUri(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());

        AssertDifference(result, UriComparisonLevel.IriMappedUri, component);
    }

    [Test]
    public void ComparisonOrderStopsAtTheFirstDifferentComponent()
    {
        UriComparisonResult exact = IriComparerCore.CompareExact(
            "x://u@h:80/a?q#f".AsSpan(),
            "y://v@j:81/b?r#g".AsSpan(),
            new OperationLimits());
        UriComparisonResult unicode = IriComparerCore.CompareUnicode(
            "x://u@h:80/a?q#f".AsSpan(),
            "x://v@j:81/b?r#g".AsSpan(),
            new OperationLimits());
        UriComparisonResult mapped = IriComparerCore.CompareAsUri(
            "x://u@h:80/a?q#f".AsSpan(),
            "x://u@j:81/b?r#g".AsSpan(),
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertDifference(exact, UriComparisonLevel.Exact, UriComponent.Scheme);
            AssertDifference(unicode, UriComparisonLevel.IriUnicode, UriComponent.UserInfo);
            AssertDifference(mapped, UriComparisonLevel.IriMappedUri, UriComponent.Host);
        });
    }

    [Test]
    public void EveryModeReturnsIriDiagnosticsForInvalidInput()
    {
        string malformed = "x:" + '\uD800';

        UriComparisonResult exact = IriComparerCore.CompareExact(
            malformed.AsSpan(),
            "x:a".AsSpan(),
            new OperationLimits());
        UriComparisonResult unicode = IriComparerCore.CompareUnicode(
            "x:a".AsSpan(),
            malformed.AsSpan(),
            new OperationLimits());
        UriComparisonResult mapped = IriComparerCore.CompareAsUri(
            malformed.AsSpan(),
            "x:a".AsSpan(),
            new OperationLimits());

        Assert.Multiple(() =>
        {
            AssertFailure(exact, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidUnicode);
            AssertFailure(unicode, UriComparisonLevel.IriUnicode, UriDiagnosticCode.InvalidUnicode);
            AssertFailure(mapped, UriComparisonLevel.IriMappedUri, UriDiagnosticCode.InvalidUnicode);
        });
    }

    [Test]
    public void EveryModeRejectsMissingLimits()
    {
        UriComparisonResult exact = IriComparerCore.CompareExact(
            "x:a".AsSpan(),
            "x:a".AsSpan(),
            null);
        UriComparisonResult unicode = IriComparerCore.CompareUnicode(
            "x:a".AsSpan(),
            "x:a".AsSpan(),
            null);
        UriComparisonResult mapped = IriComparerCore.CompareAsUri(
            "x:a".AsSpan(),
            "x:a".AsSpan(),
            null);

        Assert.Multiple(() =>
        {
            AssertFailure(exact, UriComparisonLevel.Exact, UriDiagnosticCode.InvalidInput);
            AssertFailure(unicode, UriComparisonLevel.IriUnicode, UriDiagnosticCode.InvalidInput);
            AssertFailure(mapped, UriComparisonLevel.IriMappedUri, UriDiagnosticCode.InvalidInput);
        });
    }

    [Test]
    public void ComparisonModesEnforceTheirInputAndGeneratedOutputLimits()
    {
        OperationLimits inputLimits = new OperationLimits();
        inputLimits.MaxInputCharacters = 2;
        OperationLimits unicodeOutputLimits = new OperationLimits();
        unicodeOutputLimits.MaxOutputCharacters = 2;
        OperationLimits mappedOutputLimits = new OperationLimits();
        mappedOutputLimits.MaxOutputCharacters = 7;

        UriComparisonResult exact = IriComparerCore.CompareExact(
            "x:a".AsSpan(),
            "x:a".AsSpan(),
            inputLimits);
        UriComparisonResult unicode = IriComparerCore.CompareUnicode(
            "x:e\u0301".AsSpan(),
            "x:é".AsSpan(),
            unicodeOutputLimits);
        UriComparisonResult mapped = IriComparerCore.CompareAsUri(
            "x:é".AsSpan(),
            "x:%C3%A9".AsSpan(),
            mappedOutputLimits);

        Assert.Multiple(() =>
        {
            AssertFailure(exact, UriComparisonLevel.Exact, UriDiagnosticCode.LimitExceeded);
            AssertFailure(unicode, UriComparisonLevel.IriUnicode, UriDiagnosticCode.LimitExceeded);
            AssertFailure(mapped, UriComparisonLevel.IriMappedUri, UriDiagnosticCode.LimitExceeded);
        });
    }

    [Test]
    public void SpanComparisonsConsumeOnlyTheProvidedSlices()
    {
        string exactText = "x:/café";
        string unicodeText = "x:/cafe\u0301";
        string mappedText = "x:/caf%C3%A9";
        string exactPadded = "before" + exactText + "after";
        string unicodePadded = "before" + unicodeText + "after";
        string mappedPadded = "before" + mappedText + "after";
        OperationLimits limits = new OperationLimits();

        UriComparisonResult exact = IriComparerCore.CompareExact(
            exactPadded.AsSpan(6, exactText.Length),
            exactText.AsSpan(),
            limits);
        UriComparisonResult unicode = IriComparerCore.CompareUnicode(
            unicodePadded.AsSpan(6, unicodeText.Length),
            exactText.AsSpan(),
            limits);
        UriComparisonResult mapped = IriComparerCore.CompareAsUri(
            exactText.AsSpan(),
            mappedPadded.AsSpan(6, mappedText.Length),
            limits);

        Assert.Multiple(() =>
        {
            AssertEquality(exact, UriComparisonLevel.Exact);
            AssertEquality(unicode, UriComparisonLevel.IriUnicode);
            AssertEquality(mapped, UriComparisonLevel.IriMappedUri);
        });
    }

    private static void AssertEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.True);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(component));
            Assert.That(result.Diagnostic, Is.Null);
        });
    }

    private static void AssertFailure(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriDiagnosticCode code)
    {
        Assert.That(result.Diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.Equal, Is.False);
            Assert.That(result.Level, Is.EqualTo(level));
            Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
            Assert.That(result.Diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Iri));
            Assert.That(result.Diagnostic.Code, Is.EqualTo(code));
        });
    }
}
