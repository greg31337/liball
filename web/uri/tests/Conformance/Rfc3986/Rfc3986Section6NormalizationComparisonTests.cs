namespace uri.Tests;

public class Rfc3986Section6NormalizationComparisonTests
{
    [Test]
    public void SectionSixSyntaxExampleNormalizesToOneForm()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();
        string first = "example://a/b/c/%7Bfoo%7D";
        string second = "eXAMPLE://a/./b/../b/%63/%7bfoo%7d";

        string normalizedFirst = normalizer.NormalizeSyntax(
            first,
            new UriNormalizationOptions());
        string normalizedSecond = normalizer.NormalizeSyntax(
            second,
            new UriNormalizationOptions());
        UriComparisonResult exact = comparer.CompareExact(
            first,
            second,
            new UriComparisonOptions());
        UriComparisonResult syntax = comparer.CompareSyntax(
            first,
            second,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalizedFirst, Is.EqualTo("example://a/b/c/%7Bfoo%7D"));
            Assert.That(normalizedSecond, Is.EqualTo("example://a/b/c/%7Bfoo%7D"));
            AssertDifference(exact, UriComparisonLevel.Exact, UriComponent.Scheme);
            AssertEquality(syntax, UriComparisonLevel.Syntax);
        });
    }

    [Test]
    public void SectionSixCaseNormalizationAppliesOnlyWhereGenericSyntaxAllowsIt()
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult schemeAndHost = comparer.CompareSyntax(
            "HTTP://www.EXAMPLE.com/",
            "http://www.example.com/",
            new UriComparisonOptions());
        UriComparisonResult userInfo = comparer.CompareSyntax(
            "http://User@example.com/",
            "http://user@example.com/",
            new UriComparisonOptions());
        UriComparisonResult path = comparer.CompareSyntax(
            "http://example.com/Case",
            "http://example.com/case",
            new UriComparisonOptions());
        UriComparisonResult query = comparer.CompareSyntax(
            "http://example.com/?Query",
            "http://example.com/?query",
            new UriComparisonOptions());
        UriComparisonResult fragment = comparer.CompareSyntax(
            "http://example.com/#Fragment",
            "http://example.com/#fragment",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            AssertEquality(schemeAndHost, UriComparisonLevel.Syntax);
            AssertDifference(userInfo, UriComparisonLevel.Syntax, UriComponent.UserInfo);
            AssertDifference(path, UriComparisonLevel.Syntax, UriComponent.Path);
            AssertDifference(query, UriComparisonLevel.Syntax, UriComponent.Query);
            AssertDifference(fragment, UriComparisonLevel.Syntax, UriComponent.Fragment);
        });
    }

    [TestCase("x://%75ser@example.com/", "x://user@example.com/")]
    [TestCase("x://%65xample.com/", "x://example.com/")]
    [TestCase("x:/%7Euser/%41", "x:/~user/A")]
    [TestCase("x:?value=%41%7E", "x:?value=A~")]
    [TestCase("x:#value=%41%7E", "x:#value=A~")]
    public void PercentEncodedUnreservedCharactersAreSyntaxEquivalent(
        string encoded,
        string literal)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult exact = comparer.CompareExact(
            encoded,
            literal,
            new UriComparisonOptions());
        UriComparisonResult syntax = comparer.CompareSyntax(
            encoded,
            literal,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(exact.Success, Is.True);
            Assert.That(exact.Equal, Is.False);
            AssertEquality(syntax, UriComparisonLevel.Syntax);
        });
    }

    [Test]
    public void PercentTripletHexCaseIsNormalizedWithoutDecodingReservedCharacters()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        string normalized = normalizer.NormalizeSyntax(
            "x:/a%2fb?value=%3f#value=%23",
            new UriNormalizationOptions());
        UriComparisonResult equivalent = comparer.CompareSyntax(
            "x:/a%2fb?value=%3f#value=%23",
            "x:/a%2Fb?value=%3F#value=%23",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo("x:/a%2Fb?value=%3F#value=%23"));
            AssertEquality(equivalent, UriComparisonLevel.Syntax);
        });
    }

    [TestCase("x:/a%2Fb", "x:/a/b", UriComponent.Path)]
    [TestCase("x:/a%3Fb", "x:/a?b", UriComponent.Path)]
    [TestCase("x:/a%23b", "x:/a#b", UriComponent.Path)]
    [TestCase("x:?a%26b", "x:?a&b", UriComponent.Query)]
    [TestCase("x://user%40host/", "x://user@host/", UriComponent.UserInfo)]
    public void PercentEncodedReservedCharactersRemainDistinctFromDelimiters(
        string encoded,
        string delimiter,
        UriComponent component)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareSyntax(
            encoded,
            delimiter,
            new UriComparisonOptions());

        AssertDifference(result, UriComparisonLevel.Syntax, component);
    }

    [Test]
    public void PercentNormalizationDoesNotDecodeMoreThanOnce()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        string normalized = normalizer.NormalizeSyntax(
            "x:/%252F/%257E",
            new UriNormalizationOptions());
        UriComparisonResult encodedSlash = comparer.CompareSyntax(
            "x:/%252F",
            "x:/%2F",
            new UriComparisonOptions());
        UriComparisonResult encodedTilde = comparer.CompareSyntax(
            "x:/%257E",
            "x:/~",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo("x:/%252F/%257E"));
            AssertDifference(encodedSlash, UriComparisonLevel.Syntax, UriComponent.Path);
            AssertDifference(encodedTilde, UriComparisonLevel.Syntax, UriComponent.Path);
        });
    }

    [Test]
    public void PercentEncodedDotsAreDecodedBeforeDotSegmentRemoval()
    {
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        string normalized = normalizer.NormalizeSyntax(
            "x:/a/%2E/b/%2e%2E/c",
            new UriNormalizationOptions());
        UriComparisonResult result = comparer.CompareSyntax(
            "x:/a/%2E/b/%2e%2E/c",
            "x:/a/c",
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo("x:/a/c"));
            AssertEquality(result, UriComparisonLevel.Syntax);
        });
    }

    [Test]
    public void HttpSchemeProfileRecognizesSectionSixEquivalentForms()
    {
        string[] forms = new string[]
        {
            "http://example.com",
            "http://example.com/",
            "http://example.com:/",
            "http://example.com:80/"
        };
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        for (int index = 0; index < forms.Length; index += 1)
        {
            string normalized = normalizer.NormalizeWithProfile(
                forms[index],
                new UriNormalizationOptions());
            Assert.That(normalized, Is.EqualTo("http://example.com/"), forms[index]);

            for (int other = 0; other < forms.Length; other += 1)
            {
                UriComparisonResult result = comparer.CompareWithProfile(
                    forms[index],
                    forms[other],
                    new UriComparisonOptions());
                AssertEquality(result, UriComparisonLevel.SchemeProfile);
            }
        }
    }

    [TestCase("http://example.com", "http://example.com/", UriComponent.Path)]
    [TestCase("http://example.com:/", "http://example.com/", UriComponent.Port)]
    [TestCase("http://example.com:80/", "http://example.com/", UriComponent.Port)]
    public void GenericSyntaxComparisonDoesNotApplyHttpSchemeRules(
        string left,
        string right,
        UriComponent component)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareSyntax(
            left,
            right,
            new UriComparisonOptions());

        AssertDifference(result, UriComparisonLevel.Syntax, component);
    }

    [TestCase("http://example.com/", "http://example.com/?", UriComponent.Query)]
    [TestCase("http://example.com/", "http://example.com/#", UriComponent.Fragment)]
    [TestCase("http://example.com/", "http://@example.com/", UriComponent.UserInfo)]
    public void BuiltInProfilesPreserveSignificantEmptyDelimiters(
        string absent,
        string present,
        UriComponent component)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareWithProfile(
            absent,
            present,
            new UriComparisonOptions());

        AssertDifference(result, UriComparisonLevel.SchemeProfile, component);
    }

    [TestCase("http://example.com/data", "http://example.com/data/", UriComponent.Path)]
    [TestCase("http://example.com/", "http://www.example.com/", UriComponent.Host)]
    [TestCase("http://example.com/", "https://example.com/", UriComponent.Scheme)]
    [TestCase("http://example.com/?a=1&b=2", "http://example.com/?b=2&a=1", UriComponent.Query)]
    [TestCase("http://example.com/#one", "http://example.com/#two", UriComponent.Fragment)]
    [TestCase("mailto:Joe@Example.COM", "mailto:Joe@example.com", UriComponent.Path)]
    public void ComparisonDoesNotInventProtocolOrResourceEquivalence(
        string left,
        string right,
        UriComponent component)
    {
        IUriComparer comparer = new UriComparer();

        UriComparisonResult result = comparer.CompareWithProfile(
            left,
            right,
            new UriComparisonOptions());

        AssertDifference(result, UriComparisonLevel.SchemeProfile, component);
    }

    private static void AssertEquality(
        UriComparisonResult result,
        UriComparisonLevel level)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.True);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(UriComponent.None));
        Assert.That(result.Diagnostic, Is.Null);
    }

    private static void AssertDifference(
        UriComparisonResult result,
        UriComparisonLevel level,
        UriComponent component)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Equal, Is.False);
        Assert.That(result.Level, Is.EqualTo(level));
        Assert.That(result.DifferentComponent, Is.EqualTo(component));
        Assert.That(result.Diagnostic, Is.Null);
    }
}
