using whois;

namespace whois.Tests;

public class IdnNormalizerTests
{
    [Test]
    public void NormalizesAsciiCasingAndRemovesOptionalRootDot()
    {
        WhoisQuery query = IdnNormalizer.NormalizeDomain("Example.COM.");

        Assert.That(query.OriginalText, Is.EqualTo("Example.COM."));
        Assert.That(query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(query.RoutingKey, Is.EqualTo("example.com"));
        Assert.That(query.Kind, Is.EqualTo(WhoisQueryKind.Domain));
        Assert.That(query.IsValid, Is.True);
        Assert.That(query.ValidationMessage, Is.Empty);
    }

    [Test]
    public void ConvertsUnicodeLabelsWithBuiltInIdna()
    {
        WhoisQuery query = IdnNormalizer.NormalizeDomain("BÜCHER.Example");

        Assert.That(query.OriginalText, Is.EqualTo("BÜCHER.Example"));
        Assert.That(query.NormalizedText, Is.EqualTo("xn--bcher-kva.example"));
        Assert.That(query.RoutingKey, Is.EqualTo(query.NormalizedText));
    }

    [Test]
    public void RecognizesUnicodeRootSeparator()
    {
        WhoisQuery query = IdnNormalizer.NormalizeDomain("例え。テスト。");

        Assert.That(query.OriginalText, Is.EqualTo("例え。テスト。"));
        Assert.That(query.NormalizedText, Is.EqualTo("xn--r8jz45g.xn--zckzah"));
    }

    [Test]
    public void TrimsRoutingWhitespaceWhilePreservingOriginalText()
    {
        WhoisQuery query = IdnNormalizer.NormalizeDomain("  Example.COM.  ");

        Assert.That(query.OriginalText, Is.EqualTo("  Example.COM.  "));
        Assert.That(query.NormalizedText, Is.EqualTo("example.com"));
        Assert.That(query.RoutingKey, Is.EqualTo("example.com"));
    }

    [Test]
    public void AcceptsMaximumDnsLabelAndDomainLengths()
    {
        string first = new string('a', 63);
        string second = new string('b', 63);
        string third = new string('c', 63);
        string fourth = new string('d', 61);
        string domain = first + "." + second + "." + third + "." + fourth;

        WhoisQuery query = IdnNormalizer.NormalizeDomain(domain);

        Assert.That(query.NormalizedText, Has.Length.EqualTo(253));
        Assert.That(query.NormalizedText, Is.EqualTo(domain));
    }

    [Test]
    public void RejectsLabelLongerThanDnsLimit()
    {
        string domain = new string('a', 64) + ".example";

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => IdnNormalizer.NormalizeDomain(domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(domain));
    }

    [Test]
    public void RejectsDomainLongerThanDnsLimit()
    {
        string domain = new string('a', 63) + "." + new string('b', 63) + "." + new string('c', 63) + "." + new string('d', 62);

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => IdnNormalizer.NormalizeDomain(domain));

        Assert.That(domain, Has.Length.EqualTo(254));
        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(domain));
    }

    [TestCase(".")]
    [TestCase(".example")]
    [TestCase("example..com")]
    [TestCase("example.com..")]
    public void RejectsEmptyDomainLabels(string domain)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => IdnNormalizer.NormalizeDomain(domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(domain));
    }

    [TestCase("-example.com")]
    [TestCase("example-.com")]
    [TestCase("exam_ple.com")]
    [TestCase("example com")]
    public void RejectsInvalidAsciiLabelSyntax(string domain)
    {
        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => IdnNormalizer.NormalizeDomain(domain));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.Domain));
    }
}
