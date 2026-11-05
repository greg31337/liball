namespace uri.Tests;

public class UriSyntaxNormalizationTests
{
    [Test]
    public void FullNormalizationAppliesEveryGenericSyntaxTransformation()
    {
        const string input = "HTTP://User%7e@EXAMPLE%2eCOM:080/a/%7e/../b?Q=%7e%2f#F=%7e%2f";

        string normalized = NormalizeText(input);

        Assert.That(
            normalized,
            Is.EqualTo("http://User~@example.com:080/a/b?Q=~%2F#F=~%2F"));
    }

    [TestCase("x:/a/%2e%2e/b", "x:/b")]
    [TestCase("x:/a/%2E/./b", "x:/a/b")]
    [TestCase("x:/a%2fb/../c", "x:/c")]
    [TestCase("x:/%252e/../b", "x:/b")]
    public void PercentNormalizationPrecedesDotSegmentRemoval(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("x://[2001:0DB8:0000:0000:0000:0000:0000:0001]/", "x://[2001:db8::1]/")]
    [TestCase("x://[0:0:0:0:0:FFFF:192.0.2.128]/", "x://[::ffff:192.0.2.128]/")]
    [TestCase("x://[2001:db8::192.0.2.128]/", "x://[2001:db8::c000:280]/")]
    [TestCase("x://192.0.2.128/", "x://192.0.2.128/")]
    public void FullNormalizationUsesCanonicalParsedIpAddresses(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("?")]
    [TestCase("#")]
    [TestCase("?#")]
    [TestCase("x:")]
    [TestCase("x://")]
    [TestCase("x://host")]
    [TestCase("x://host:")]
    [TestCase("x://host?")]
    [TestCase("x://host#")]
    [TestCase("x://host?#")]
    [TestCase("x://@:")]
    public void GenericNormalizationPreservesAbsentAndEmptyDistinctions(string text)
    {
        UriReference source = Parse(text);

        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(source);
        string output = Compose(normalized);

        Assert.Multiple(() =>
        {
            Assert.That(output, Is.EqualTo(text), text);
            Assert.That(normalized.HasAuthority, Is.EqualTo(source.HasAuthority), text);
            Assert.That(normalized.Authority.HasUserInfo, Is.EqualTo(source.Authority.HasUserInfo), text);
            Assert.That(normalized.Authority.HasPort, Is.EqualTo(source.Authority.HasPort), text);
            Assert.That(normalized.Authority.Port, Is.EqualTo(source.Authority.Port), text);
            Assert.That(normalized.HasQuery, Is.EqualTo(source.HasQuery), text);
            Assert.That(normalized.Query, Is.EqualTo(source.Query), text);
            Assert.That(normalized.HasFragment, Is.EqualTo(source.HasFragment), text);
            Assert.That(normalized.Fragment, Is.EqualTo(source.Fragment), text);
        });
    }

    [TestCase("http://example.com", "http://example.com")]
    [TestCase("http://example.com:80", "http://example.com:80")]
    [TestCase("https://example.com:443", "https://example.com:443")]
    [TestCase("x://[VF.ABC:DEF]", "x://[VF.ABC:DEF]")]
    public void GenericNormalizationDoesNotApplySchemeProfileSemantics(
        string input,
        string expected)
    {
        Assert.That(NormalizeText(input), Is.EqualTo(expected));
    }

    [TestCase("a/..", "/", UriReferenceKind.AbsolutePathReference)]
    [TestCase("./", "", UriReferenceKind.EmptyReference)]
    [TestCase("./?q", "?q", UriReferenceKind.RelativeReference)]
    [TestCase(".#f", "#f", UriReferenceKind.SameDocumentReference)]
    [TestCase("../a", "a", UriReferenceKind.RelativePathReference)]
    [TestCase("x:a/..", "x:/", UriReferenceKind.Uri)]
    [TestCase("//host/a/..", "//host/", UriReferenceKind.NetworkPathReference)]
    public void NormalizationUpdatesClassificationWhenThePathFormChanges(
        string input,
        string expected,
        UriReferenceKind expectedKind)
    {
        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(Parse(input));

        Assert.Multiple(() =>
        {
            Assert.That(Compose(normalized), Is.EqualTo(expected), input);
            Assert.That(normalized.Kind, Is.EqualTo(expectedKind), input);
        });
    }

    [Test]
    public void AbsoluteUriClassificationIsPreserved()
    {
        UriParseResult parsed = UriParserCore.ParseAbsoluteUri(
            "HTTP://EXAMPLE.COM/a/./b".AsSpan());

        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(
            parsed.Reference);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.Success, Is.True);
            Assert.That(normalized.Kind, Is.EqualTo(UriReferenceKind.AbsoluteUri));
            Assert.That(Compose(normalized), Is.EqualTo("http://example.com/a/b"));
        });
    }

    [Test]
    public void NormalizationReturnsADeepCloneAndDoesNotMutateTheSource()
    {
        const string input = "HTTP://User%7e@[2001:0DB8:0:0:0:0:0:1]:080/a/./b?Q=%7e#F=%7e";
        UriReference source = Parse(input);
        byte[] sourceAddress = (byte[])source.Authority.AddressBytes.Clone();

        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(source);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.Not.SameAs(source));
            Assert.That(normalized.Authority, Is.Not.SameAs(source.Authority));
            Assert.That(normalized.Authority.AddressBytes, Is.Not.SameAs(source.Authority.AddressBytes));
            Assert.That(source.Authority.AddressBytes, Is.EqualTo(sourceAddress));
            Assert.That(source.Scheme, Is.EqualTo("HTTP"));
            Assert.That(source.Authority.UserInfo, Is.EqualTo("User%7e"));
            Assert.That(source.Authority.Host, Is.EqualTo("2001:0DB8:0:0:0:0:0:1"));
            Assert.That(source.Path, Is.EqualTo("/a/./b"));
            Assert.That(source.Query, Is.EqualTo("Q=%7e"));
            Assert.That(source.Fragment, Is.EqualTo("F=%7e"));
            Assert.That(source.OriginalText, Is.EqualTo(input));
            Assert.That(normalized.OriginalText, Is.EqualTo(input));
            Assert.That(Compose(normalized), Is.EqualTo("http://User~@[2001:db8::1]:080/a/b?Q=~#F=~"));
        });

        normalized.Authority.AddressBytes[0] = 255;
        Assert.That(source.Authority.AddressBytes, Is.EqualTo(sourceAddress));
    }

    [TestCase("HTTP://EXAMPLE.COM/a/./b?x=%7e#f=%2f")]
    [TestCase("x://[0:0:0:0:0:FFFF:192.0.2.128]/a/../")]
    [TestCase("a/%2e%2e/b?Qq#Ff")]
    [TestCase("?#")]
    public void FullSyntaxNormalizationIsIdempotent(string input)
    {
        UriReference firstReference = UriNormalizerCore.NormalizeSyntaxReference(
            Parse(input));
        string firstText = Compose(firstReference);
        UriReference secondReference = UriNormalizerCore.NormalizeSyntaxReference(
            Parse(firstText));
        string secondText = Compose(secondReference);

        Assert.That(secondText, Is.EqualTo(firstText), input);
    }

    [TestCase("./this:that", "./this:that")]
    [TestCase("../this:that", "./this:that")]
    [TestCase("./this:that/./leaf", "./this:that/leaf")]
    [TestCase("./this:that/%7e?Q=%7e#F=%7e", "./this:that/~?Q=~#F=~")]
    public void RelativeColonBearingFirstSegmentsKeepAnUnambiguousPrefix(
        string input,
        string expected)
    {
        string once = NormalizeText(input);
        string twice = NormalizeText(once);

        Assert.Multiple(() =>
        {
            Assert.That(once, Is.EqualTo(expected));
            Assert.That(twice, Is.EqualTo(once));
            Assert.That(Parse(once).HasScheme, Is.False);
            Assert.That(Parse(once).Kind, Is.EqualTo(UriReferenceKind.RelativePathReference));
        });
    }

    [Test]
    public void AnalysisDoesNotReportARestoredRelativePathGuardAsAChange()
    {
        IUriNormalizer normalizer = new UriNormalizer();

        UriNormalizationChange[] unchanged = normalizer.AnalyzeNormalization(
            "./this:that",
            new UriNormalizationOptions());
        UriNormalizationChange[] percentOnly = normalizer.AnalyzeNormalization(
            "./this:that/%7e",
            new UriNormalizationOptions());

        Assert.Multiple(() =>
        {
            Assert.That(unchanged, Is.Empty);
            Assert.That(
                percentOnly,
                Is.EqualTo(new UriNormalizationChange[]
                {
                    UriNormalizationChange.PathPercentEncoding
                }));
        });
    }

    [Test]
    public void NormalizationRejectsNullAndMalformedCallerCreatedComponents()
    {
        UriReference malformed = Parse("x:path");
        malformed.Query = "%GG";
        malformed.HasQuery = true;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeSyntaxReference(null));
            Assert.Throws<ArgumentException>(() =>
                UriNormalizerCore.NormalizeSyntaxReference(malformed));
        });
    }

    private static string NormalizeText(string text)
    {
        UriReference normalized = UriNormalizerCore.NormalizeSyntaxReference(Parse(text));
        return Compose(normalized);
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
}
