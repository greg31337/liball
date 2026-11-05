using iso20022;

namespace iso20022.Tests;

public class XmlNameAndUriLexicalTests
{
    [TestCase("Document")]
    [TestCase("_private")]
    [TestCase(":qualified")]
    [TestCase("a:b:c")]
    [TestCase("na-me.2")]
    [TestCase("élement")]
    [TestCase("名")]
    [TestCase("\U00010000node")]
    [TestCase("\U000EFFFF")]
    public void XmlNameValidatorAcceptsXml10FifthEditionNames(string text)
    {
        Assert.That(XmlNameLexical.IsXmlName(text.AsSpan()), Is.True);
    }

    [TestCase("")]
    [TestCase("1name")]
    [TestCase("-name")]
    [TestCase("name value")]
    [TestCase("\u0301name")]
    public void XmlNameValidatorRejectsInvalidStartsCharactersAndSurrogates(string text)
    {
        Assert.That(XmlNameLexical.IsXmlName(text.AsSpan()), Is.False);
    }

    [Test]
    public void XmlNameValidatorRejectsMalformedSurrogatesAndCodePointsAboveTheXmlNameRange()
    {
        string highSurrogate = new string(new char[] { '\uD800' });
        string lowSurrogate = new string(new char[] { '\uDC00' });
        string aboveNameRange = char.ConvertFromUtf32(983040);

        Assert.Multiple(() =>
        {
            Assert.That(XmlNameLexical.IsXmlName(highSurrogate.AsSpan()), Is.False);
            Assert.That(XmlNameLexical.IsXmlName(lowSurrogate.AsSpan()), Is.False);
            Assert.That(XmlNameLexical.IsXmlName(aboveNameRange.AsSpan()), Is.False);
        });
    }

    [TestCase("Document")]
    [TestCase("_private")]
    [TestCase("na-me.2")]
    [TestCase("élement")]
    [TestCase("a\u0301")]
    [TestCase("\U00010000node")]
    public void NcNameValidatorAcceptsNamesWithoutColons(string text)
    {
        Assert.That(XmlNameLexical.IsNcName(text.AsSpan()), Is.True);
    }

    [TestCase(":name")]
    [TestCase("prefix:name")]
    [TestCase("a:b:c")]
    [TestCase("1name")]
    [TestCase("name value")]
    public void NcNameValidatorRejectsColonsAndInvalidNames(string text)
    {
        Assert.That(XmlNameLexical.IsNcName(text.AsSpan()), Is.False);
    }

    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("../schema/message.xsd")]
    [TestCase("?query=value")]
    [TestCase("https://example.test/schema/message.xsd")]
    [TestCase("https://user@example.test:8443/a%20b?q=x#part")]
    [TestCase("http://[2001:db8::1]/schema")]
    [TestCase("http://[::ffff:192.0.2.1]/schema")]
    [TestCase("http://[v1.fe.example]/schema")]
    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10")]
    public void UriReferenceValidatorAcceptsRfc3986References(string text)
    {
        Assert.That(UriLexical.IsUriReference(text.AsSpan()), Is.True);
    }

    [TestCase("1http:value")]
    [TestCase("http://example.test/a b")]
    [TestCase("http://example.test/%ZZ")]
    [TestCase("http://example.test/%1")]
    [TestCase("http://example.test/a\\b")]
    [TestCase("http://example.test/a[b]")]
    [TestCase("http://[2001:db8::1/schema")]
    [TestCase("http://[gggg::1]/schema")]
    [TestCase("http://[2001:db8::1::2]/schema")]
    [TestCase("http://[::ffff:192.0.2.999]/schema")]
    [TestCase("http://example.test:not-a-port/path")]
    [TestCase("http://user@@example.test/path")]
    [TestCase("http://example.test/#one#two")]
    [TestCase("https://example.test/é")]
    public void UriReferenceValidatorRejectsMalformedReferences(string text)
    {
        Assert.That(UriLexical.IsUriReference(text.AsSpan()), Is.False);
    }

    [TestCase("https://example.test/path")]
    [TestCase("mailto:operations@example.test")]
    [TestCase("urn:example:asset")]
    [TestCase("custom:")]
    public void AbsoluteUriValidatorRequiresAValidScheme(string text)
    {
        Assert.That(UriLexical.IsAbsoluteUri(text.AsSpan()), Is.True);
    }

    [TestCase("")]
    [TestCase("relative/path")]
    [TestCase("../schema.xsd")]
    [TestCase("?query")]
    public void AbsoluteUriValidatorRejectsRelativeReferences(string text)
    {
        Assert.That(UriLexical.IsAbsoluteUri(text.AsSpan()), Is.False);
    }

    [Test]
    public void NamespaceUriValidatorRejectsOnlyEmptyOrMalformedUriReferences()
    {
        Assert.Multiple(() =>
        {
            Assert.That(UriLexical.IsNamespaceUri("urn:iso:std:iso:20022:2013:ecore".AsSpan()), Is.True);
            Assert.That(UriLexical.IsNamespaceUri("relative/namespace".AsSpan()), Is.True);
            Assert.That(UriLexical.IsNamespaceUri(ReadOnlySpan<char>.Empty), Is.False);
            Assert.That(UriLexical.IsNamespaceUri("namespace with spaces".AsSpan()), Is.False);
        });
    }

    [TestCase("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10")]
    [TestCase("URN:example:animal:ferret:nose")]
    [TestCase("urn:example:a123,456")]
    [TestCase("urn:example:a%2Fb")]
    [TestCase("urn:example:foo-bar-baz-qux?+CCResolve:cc=uk")]
    [TestCase("urn:example:foo?+resolve?=query/part#fragment")]
    [TestCase("urn:example:foo#")]
    [TestCase("urn:example:foo#/part?query")]
    public void UrnValidatorAcceptsRfc8141AssignedNamesAndComponents(string text)
    {
        Assert.That(UriLexical.IsUrn(text.AsSpan()), Is.True);
    }

    [TestCase("urn:x:value")]
    [TestCase("urn:-bad:value")]
    [TestCase("urn:bad-:value")]
    [TestCase("urn:urn:value")]
    [TestCase("urn:example:")]
    [TestCase("urn:example:/value")]
    [TestCase("urn:example:value?query")]
    [TestCase("urn:example:value?+")]
    [TestCase("urn:example:value?=")]
    [TestCase("urn:example:value?+/resolve")]
    [TestCase("urn:example:value?=?query")]
    [TestCase("urn:example:value#one#two")]
    [TestCase("urn:example:value with space")]
    [TestCase("http://example.test/value")]
    public void UrnValidatorRejectsMalformedAssignedNamesAndComponents(string text)
    {
        Assert.That(UriLexical.IsUrn(text.AsSpan()), Is.False);
    }
}
