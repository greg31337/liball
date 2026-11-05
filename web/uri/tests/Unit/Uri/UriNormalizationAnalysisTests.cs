namespace uri.Tests;

public class UriNormalizationAnalysisTests
{
    [Test]
    public void AnalysisReportsEveryChangedComponentInUriOrder()
    {
        UriReference reference = Parse(
            "HTTP://User%7e@EXAMPLE%2eCOM/a/%7e/../b?Q=%7e#F=%7e");

        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(reference);

        Assert.That(changes, Is.EqualTo(new UriComponent[]
        {
            UriComponent.Scheme,
            UriComponent.UserInfo,
            UriComponent.Host,
            UriComponent.Path,
            UriComponent.Query,
            UriComponent.Fragment
        }));
    }

    [TestCase("HTTP:path", UriComponent.Scheme)]
    [TestCase("x://User%7e@host/", UriComponent.UserInfo)]
    [TestCase("x://EXAMPLE.com/", UriComponent.Host)]
    [TestCase("x://%45XAMPLE.%43OM/", UriComponent.Host)]
    [TestCase("x://[2001:0DB8:0:0:0:0:0:1]/", UriComponent.Host)]
    [TestCase("x:/a/./b", UriComponent.Path)]
    [TestCase("x:/A%7e", UriComponent.Path)]
    [TestCase("x:?Q=%7e", UriComponent.Query)]
    [TestCase("x:#F=%7e", UriComponent.Fragment)]
    public void AnalysisReportsEachIndividuallyChangedComponent(
        string text,
        UriComponent expected)
    {
        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(
            Parse(text));

        Assert.That(changes, Is.EqualTo(new UriComponent[] { expected }), text);
    }

    [TestCase("http://User@example.com:080/Aa?Qq#Ff")]
    [TestCase("x://host:?#")]
    [TestCase("x://192.0.2.128/")]
    [TestCase("x://[2001:db8::1]/")]
    [TestCase("x://[VF.ABC:DEF]/")]
    [TestCase("")]
    public void CanonicalAndCaseSensitiveComponentsReportNoChanges(string text)
    {
        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(
            Parse(text));

        Assert.That(changes, Is.Empty, text);
    }

    [TestCase("x:/a/%2e%2e/b")]
    [TestCase("x:/a%7e/./b")]
    [TestCase("x:/a%2fb/../c")]
    public void MultiplePathTransformationsReportPathOnlyOnce(string text)
    {
        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(
            Parse(text));

        Assert.That(changes, Is.EqualTo(new UriComponent[] { UriComponent.Path }));
    }

    [TestCase("x:/a%2fb")]
    [TestCase("x:?a=%2f")]
    [TestCase("x:#a%2f")]
    [TestCase("x://a%2fb/")]
    public void LowercaseHexOnReservedTripletsIsReported(string text)
    {
        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(
            Parse(text));

        Assert.That(changes, Has.Length.EqualTo(1), text);
    }

    [TestCase("x://[2001:db8::192.0.2.128]/")]
    [TestCase("x://[0:0:0:0:0:ffff:c000:280]/")]
    public void NonCanonicalIpv6HostsAreReported(string text)
    {
        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(
            Parse(text));

        Assert.That(changes, Is.EqualTo(new UriComponent[] { UriComponent.Host }));
    }

    [Test]
    public void AnalysisDoesNotMutateTheReferenceOrParsedAddressBytes()
    {
        const string text = "HTTP://User%7e@[2001:0DB8:0:0:0:0:0:1]:080/a/./b?Q=%7e#F=%7e";
        UriReference reference = Parse(text);
        byte[] originalAddressBytes = (byte[])reference.Authority.AddressBytes.Clone();

        UriComponent[] changes = UriNormalizerCore.AnalyzeSyntaxNormalization(reference);
        UriCompositionResult recomposed = UriComposerCore.RecomposeParsedReference(
            reference,
            new OperationLimits());

        Assert.Multiple(() =>
        {
            Assert.That(changes, Is.Not.Empty);
            Assert.That(reference.Scheme, Is.EqualTo("HTTP"));
            Assert.That(reference.Authority.UserInfo, Is.EqualTo("User%7e"));
            Assert.That(reference.Authority.Host, Is.EqualTo("2001:0DB8:0:0:0:0:0:1"));
            Assert.That(reference.Authority.AddressBytes, Is.EqualTo(originalAddressBytes));
            Assert.That(reference.Path, Is.EqualTo("/a/./b"));
            Assert.That(reference.Query, Is.EqualTo("Q=%7e"));
            Assert.That(reference.Fragment, Is.EqualTo("F=%7e"));
            Assert.That(reference.OriginalText, Is.EqualTo(text));
            Assert.That(recomposed.Text, Is.EqualTo(text));
        });
    }

    [Test]
    public void RepeatedAnalysisIsStableAndReturnsIndependentArrays()
    {
        UriReference reference = Parse("HTTP://EXAMPLE.COM/a/./b?x=%7e");

        UriComponent[] first = UriNormalizerCore.AnalyzeSyntaxNormalization(reference);
        UriComponent[] second = UriNormalizerCore.AnalyzeSyntaxNormalization(reference);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(second, Is.Not.SameAs(first));
        });
    }

    [Test]
    public void AnalysisRejectsNullAndMalformedCallerCreatedComponents()
    {
        UriReference malformed = Parse("x:path");
        malformed.Path = "%GG";

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.AnalyzeSyntaxNormalization(null));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.AnalyzeSyntaxNormalization(malformed));
        });
    }

    private static UriReference Parse(string text)
    {
        UriParseResult result = UriParserCore.ParseReference(text.AsSpan());
        Assert.That(result.Success, Is.True, text);
        return result.Reference;
    }
}
