using whois;

namespace whois.Tests;

public class QueryValidationTests
{
    [Test]
    public void RejectsMissingLookupRequest()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => classifier.Classify(null!));

        Assert.That(exception.ParamName, Is.EqualTo("request"));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase("\t")]
    public void RejectsEmptyAndWhitespaceQueryText(string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.Automatic));
    }

    [TestCase("example.com\r\nnext.example")]
    [TestCase("example.com\nnext.example")]
    [TestCase("example.com\0hidden")]
    [TestCase("example.com\u001b")]
    [TestCase("example.com\u007f")]
    public void RejectsMultilineNulAndControlCharacters(string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = WhoisQueryKind.FreeForm;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
    }

    [Test]
    public void RejectsOversizedDomainLabelThroughClassifier()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = new string('a', 64) + ".example";
        request.Kind = WhoisQueryKind.Domain;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(request.Text));
    }

    [Test]
    public void RejectsOversizedDomainThroughClassifier()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = new string('a', 63) + "." + new string('b', 63) + "." + new string('c', 63) + "." + new string('d', 62);
        request.Kind = WhoisQueryKind.Domain;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(request.Text, Has.Length.EqualTo(254));
        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
    }

    [Test]
    public void RejectsMalformedUnicodeIdnLabels()
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        string unpairedHigh = "bad" + '\ud800' + ".example";
        string unpairedLow = "bad" + '\udc00' + ".example";
        string[] inputs = new[] { unpairedHigh, unpairedLow, "bad\ufffd.example" };

        foreach (string input in inputs)
        {
            WhoisLookupRequest request = new WhoisLookupRequest();
            request.Text = input;
            request.Kind = WhoisQueryKind.Domain;
            WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

            Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
            Assert.That(exception.QueryText, Is.EqualTo(input));
            Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.Domain));
        }
    }

    [TestCase(WhoisQueryKind.IPv4Address, "999.1.1.1")]
    [TestCase(WhoisQueryKind.IPv4Address, "192.0.2")]
    [TestCase(WhoisQueryKind.IPv4Address, "192.00.2.1")]
    [TestCase(WhoisQueryKind.IPv6Address, "2001:db8::g")]
    [TestCase(WhoisQueryKind.IPv6Address, "[2001:db8::1]")]
    [TestCase(WhoisQueryKind.IPv6Address, "fe80::1%4")]
    public void RejectsMalformedOrDecoratedIpLiterals(WhoisQueryKind kind, string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = kind;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.QueryText, Is.EqualTo(input));
        Assert.That(exception.QueryKind, Is.EqualTo(kind));
    }

    [TestCase(WhoisQueryKind.Automatic, "192.0.2.1/33")]
    [TestCase(WhoisQueryKind.Automatic, "192.0.2/24")]
    [TestCase(WhoisQueryKind.IPv4Prefix, "192.0.2.1/a")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "2001:db8::1/129")]
    [TestCase(WhoisQueryKind.IPv6Prefix, "2001:db8::1/64/1")]
    public void RejectsMalformedOrOutOfRangePrefixes(WhoisQueryKind kind, string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = kind;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
    }

    [TestCase("4294967296")]
    [TestCase("AS4294967296")]
    [TestCase("99999999999")]
    [TestCase("AS-1")]
    public void RejectsOutOfRangeOrMalformedAsn(string input)
    {
        WhoisQueryClassifier classifier = new WhoisQueryClassifier();
        WhoisLookupRequest request = new WhoisLookupRequest();
        request.Text = input;
        request.Kind = WhoisQueryKind.AutonomousSystem;

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => classifier.Classify(request));

        Assert.That(exception.InvalidParameter, Is.EqualTo("QueryText"));
        Assert.That(exception.QueryText, Is.EqualTo(input));
        Assert.That(exception.QueryKind, Is.EqualTo(WhoisQueryKind.AutonomousSystem));
    }
}
