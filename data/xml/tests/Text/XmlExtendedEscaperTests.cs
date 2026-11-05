using xml;

namespace xml.Tests;

[TestFixture]
public class XmlExtendedEscaperTests
{
    [Test]
    public void ValidatesCommentAndProcessingInstructionDelimiters()
    {
        XmlEscaper escaper = new XmlEscaper();

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue("a < b & c", XmlVersion.Xml10, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo("a < b & c"));
            Assert.That(escaper.UnescapeXmlValue("a < b & c", XmlVersion.Xml10, XmlCharacterEscapeContext.Comment), Is.EqualTo("a < b & c"));
            Assert.That(escaper.EscapeXmlValue("name=value? yes", XmlVersion.Xml10, XmlCharacterEscapeContext.ProcessingInstruction, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo("name=value? yes"));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("bad--comment", XmlVersion.Xml10, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("trailing-", XmlVersion.Xml10, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("bad?>instruction", XmlVersion.Xml10, XmlCharacterEscapeContext.ProcessingInstruction, XmlInvalidCharacterPolicy.Error, '\0'));
        });
    }

    [Test]
    public void SplitsAndReassemblesCDataTerminators()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "first]]>second]]>third";

        string escaped = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.CData, XmlInvalidCharacterPolicy.Error, '\0');
        string unescaped = escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.CData);

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("first]]]]><![CDATA[>second]]]]><![CDATA[>third"));
            Assert.That(unescaped, Is.EqualTo(value));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("unsplit]]>value", XmlVersion.Xml10, XmlCharacterEscapeContext.CData));
        });
    }

    [Test]
    public void ValidatesPublicAndSystemLiteralsWithTheirSelectedQuotes()
    {
        XmlEscaper escaper = new XmlEscaper();
        string publicIdentifier = "-//Example's DTD 1.0//EN";
        string systemIdentifier = "https://example.test/a?<b>&c";

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue(publicIdentifier, XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '"'), Is.EqualTo(publicIdentifier));
            Assert.That(escaper.EscapeXmlValue("bad[value", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Replace, '"'), Is.EqualTo("bad?value"));
            Assert.That(escaper.EscapeXmlValue(systemIdentifier, XmlVersion.Xml10, XmlCharacterEscapeContext.SystemLiteralDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"'), Is.EqualTo(systemIdentifier));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("contains\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.SystemLiteralDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("resource.xml#part", XmlVersion.Xml10, XmlCharacterEscapeContext.SystemLiteralSingleQuoted, XmlInvalidCharacterPolicy.Error, '\''));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("contains\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '"'));
        });
    }

    [Test]
    public void DoublesAndCollapsesXPathStringQuotes()
    {
        XmlEscaper escaper = new XmlEscaper();
        string doubleValue = "He said \"hello\" & left";
        string singleValue = "it's XPath";

        string doubleEscaped = escaper.EscapeXmlValue(doubleValue, XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"');
        string singleEscaped = escaper.EscapeXmlValue(singleValue, XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringSingleQuoted, XmlInvalidCharacterPolicy.Error, '\'');

        Assert.Multiple(() =>
        {
            Assert.That(doubleEscaped, Is.EqualTo("He said \"\"hello\"\" & left"));
            Assert.That(singleEscaped, Is.EqualTo("it''s XPath"));
            Assert.That(escaper.UnescapeXmlValue(doubleEscaped, XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringDoubleQuoted), Is.EqualTo(doubleValue));
            Assert.That(escaper.UnescapeXmlValue(singleEscaped, XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringSingleQuoted), Is.EqualTo(singleValue));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("unmatched\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringDoubleQuoted));
        });
    }

    [Test]
    public void PercentEncodesAndStrictlyDecodesUtf8UriComponents()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "AZaz09-._~ /?:#é\U0001F600";

        string escaped = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.Error, '\0');

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("AZaz09-._~%20%2F%3F%3A%23%C3%A9%F0%9F%98%80"));
            Assert.That(escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Uri), Is.EqualTo(value));
            Assert.That(escaper.EscapeXmlValue("\uD800", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.Replace, '\0'), Is.EqualTo("%EF%BF%BD"));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%E2%82", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%GG", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
        });
    }

    [Test]
    public void EscapesAndUnescapesJsonStringsIncludingSurrogatePairs()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "\"\\/\b\f\n\r\t\u0001\u2028\U0001F600";

        string escaped = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Error, '"');
        string unicodeEscaped = "\\uD83D\\uDE00";

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("\\\"\\\\/\\b\\f\\n\\r\\t\\u0001\\u2028\U0001F600"));
            Assert.That(escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Json), Is.EqualTo(value));
            Assert.That(escaper.UnescapeXmlValue(unicodeEscaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Json), Is.EqualTo("\U0001F600"));
            Assert.That(escaper.EscapeXmlValue("\uD800", XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Replace, '"'), Is.EqualTo("\\uFFFD"));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\uD83D", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\q", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw\nline", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
        });
    }

    [Test]
    public void ExtendedXmlConstructsApplyDirectCharacterPolicies()
    {
        XmlEscaper escaper = new XmlEscaper();

        string replaced = escaper.EscapeXmlValue("a\u0001b", XmlVersion.Xml11, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Replace, '\0');

        Assert.Multiple(() =>
        {
            Assert.That(replaced, Is.EqualTo("a\uFFFDb"));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("a\u0001b", XmlVersion.Xml11, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("a\u0001b", XmlVersion.Xml11, XmlCharacterEscapeContext.CData, XmlInvalidCharacterPolicy.CharacterReference, '\0'));
            Assert.Throws<ArgumentException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<ArgumentException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '\0'));
        });
    }
}
