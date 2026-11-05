using xml;

namespace xml.Tests;

[TestFixture]
public class XmlEscaperPublicCoverageTests
{
    [TestCase(XmlVersion.Xml10)]
    [TestCase(XmlVersion.Xml11)]
    public void RoundTripsSafeValuesInEveryPublicEscapeContext(XmlVersion version)
    {
        IXmlEscaper escaper = new XmlEscaper();
        XmlCharacterEscapeContext[] contexts =
        [
            XmlCharacterEscapeContext.Text,
            XmlCharacterEscapeContext.AttributeDoubleQuoted,
            XmlCharacterEscapeContext.AttributeSingleQuoted,
            XmlCharacterEscapeContext.Comment,
            XmlCharacterEscapeContext.ProcessingInstruction,
            XmlCharacterEscapeContext.CData,
            XmlCharacterEscapeContext.PublicIdentifier,
            XmlCharacterEscapeContext.SystemLiteralDoubleQuoted,
            XmlCharacterEscapeContext.SystemLiteralSingleQuoted,
            XmlCharacterEscapeContext.XPathStringDoubleQuoted,
            XmlCharacterEscapeContext.XPathStringSingleQuoted,
            XmlCharacterEscapeContext.Uri,
            XmlCharacterEscapeContext.Json
        ];

        foreach (XmlCharacterEscapeContext context in contexts)
        {
            char quote = Quote(context);
            string value = SafeValue(context);
            string escaped = escaper.EscapeXmlValue(value, version, context, XmlInvalidCharacterPolicy.Error, quote);
            string unescaped = escaper.UnescapeXmlValue(escaped, version, context);
            Assert.That(unescaped, Is.EqualTo(value), context.ToString());
        }
    }

    [TestCase(XmlVersion.Xml10)]
    [TestCase(XmlVersion.Xml11)]
    public void AppliesBothAttributeQuoteModesAndPreservesWhitespace(XmlVersion version)
    {
        IXmlEscaper escaper = new XmlEscaper();
        string value = "\"'&<\t\n\r";

        string doubleQuoted = escaper.EscapeXmlValue(value, version, XmlCharacterEscapeContext.AttributeDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"');
        string singleQuoted = escaper.EscapeXmlValue(value, version, XmlCharacterEscapeContext.AttributeSingleQuoted, XmlInvalidCharacterPolicy.Error, '\'');

        Assert.Multiple(() =>
        {
            Assert.That(doubleQuoted, Is.EqualTo("&quot;'&amp;&lt;&#x9;&#xA;&#xD;"));
            Assert.That(singleQuoted, Is.EqualTo("\"&apos;&amp;&lt;&#x9;&#xA;&#xD;"));
            Assert.That(escaper.UnescapeXmlValue(doubleQuoted, version, XmlCharacterEscapeContext.AttributeDoubleQuoted), Is.EqualTo(value));
            Assert.That(escaper.UnescapeXmlValue(singleQuoted, version, XmlCharacterEscapeContext.AttributeSingleQuoted), Is.EqualTo(value));
        });
    }

    [Test]
    public void CoversPredefinedDecimalHexadecimalAndXmlVersionSpecificReferences()
    {
        IXmlEscaper escaper = new XmlEscaper();

        Assert.Multiple(() =>
        {
            Assert.That(escaper.UnescapeXmlValue("&amp;&lt;&gt;&apos;&quot;&#65;&#x41;&#x1F600;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text), Is.EqualTo("&<>'\"AA\U0001F600"));
            Assert.That(escaper.UnescapeXmlValue("&#x1;&#x7F;&#x85;&#x9F;", XmlVersion.Xml11, XmlCharacterEscapeContext.Text), Is.EqualTo("\u0001\u007F\u0085\u009F"));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#1;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#0;", XmlVersion.Xml11, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#xD800;", XmlVersion.Xml11, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#x110000;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#x;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#12", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#xG;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&unknown;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
        });
    }

    [Test]
    public void AppliesInvalidScalarPoliciesAcrossXmlUriAndJsonFamilies()
    {
        IXmlEscaper escaper = new XmlEscaper();
        string malformed = "a\uD800b\uDC00c";

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Replace, '\0'), Is.EqualTo("a\uFFFDb\uFFFDc"));
            Assert.That(escaper.EscapeXmlValue("\u0001", XmlVersion.Xml11, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.CharacterReference, '\0'), Is.EqualTo("&#x1;"));
            Assert.That(escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.Replace, '\0'), Is.EqualTo("a%EF%BF%BDb%EF%BF%BDc"));
            Assert.That(escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Replace, '"'), Is.EqualTo("a\\uFFFDb\\uFFFDc"));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("\u0001", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.CharacterReference, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.CharacterReference, '\0'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.CharacterReference, '"'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("\u0001", XmlVersion.Xml11, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.CharacterReference, '\0'));
        });
    }

    [Test]
    public void DetectsEveryMarkupDelimiterHazardAcrossArbitraryBoundaryPlacements()
    {
        IXmlEscaper escaper = new XmlEscaper();
        int[] boundaryOffsets = [0, 1, 2, 7, 31, 255, 1023, 1024, 4095, 4096, 8191];
        foreach (int offset in boundaryOffsets)
        {
            string prefix = new string('x', offset);
            Assert.That(escaper.EscapeXmlValue(prefix + "]]>", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo(prefix + "]]&gt;"), "text " + offset);
            Assert.That(escaper.EscapeXmlValue(prefix + "]]>", XmlVersion.Xml10, XmlCharacterEscapeContext.CData, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo(prefix + "]]]]><![CDATA[>"), "CDATA " + offset);
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(prefix + "--", XmlVersion.Xml10, XmlCharacterEscapeContext.Comment, XmlInvalidCharacterPolicy.Error, '\0'), "comment " + offset);
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(prefix + "?>", XmlVersion.Xml10, XmlCharacterEscapeContext.ProcessingInstruction, XmlInvalidCharacterPolicy.Error, '\0'), "PI " + offset);
        }
    }

    [Test]
    public void CoversPublicSystemAndXPathQuoteAndDelimiterRules()
    {
        IXmlEscaper escaper = new XmlEscaper();

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue("-//A's PUBLIC ID//EN", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '"'), Is.EqualTo("-//A's PUBLIC ID//EN"));
            Assert.That(escaper.EscapeXmlValue("-//A PUBLIC ID//EN", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '\''), Is.EqualTo("-//A PUBLIC ID//EN"));
            Assert.That(escaper.EscapeXmlValue("bad[value", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Replace, '"'), Is.EqualTo("bad?value"));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("bad\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '"'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("bad'quote", XmlVersion.Xml10, XmlCharacterEscapeContext.PublicIdentifier, XmlInvalidCharacterPolicy.Error, '\''));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("uri#fragment", XmlVersion.Xml10, XmlCharacterEscapeContext.SystemLiteralDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"'));
            Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("bad'quote", XmlVersion.Xml10, XmlCharacterEscapeContext.SystemLiteralSingleQuoted, XmlInvalidCharacterPolicy.Error, '\''));
            Assert.That(escaper.EscapeXmlValue("a\"b", XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"'), Is.EqualTo("a\"\"b"));
            Assert.That(escaper.EscapeXmlValue("a'b", XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringSingleQuoted, XmlInvalidCharacterPolicy.Error, '\''), Is.EqualTo("a''b"));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("a\"b", XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringDoubleQuoted));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("a'b", XmlVersion.Xml10, XmlCharacterEscapeContext.XPathStringSingleQuoted));
        });
    }

    [Test]
    public void CoversUriUnreservedReservedUnicodeAndMalformedUtf8Cases()
    {
        IXmlEscaper escaper = new XmlEscaper();
        string unreserved = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        string reserved = ":/?#[]@!$&'()*+,;= %\u00E9\U0001F600";
        string escapedReserved = "%3A%2F%3F%23%5B%5D%40%21%24%26%27%28%29%2A%2B%2C%3B%3D%20%25%C3%A9%F0%9F%98%80";

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue(unreserved, XmlVersion.Xml10, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo(unreserved));
            Assert.That(escaper.EscapeXmlValue(reserved, XmlVersion.Xml11, XmlCharacterEscapeContext.Uri, XmlInvalidCharacterPolicy.Error, '\0'), Is.EqualTo(escapedReserved));
            Assert.That(escaper.UnescapeXmlValue(escapedReserved, XmlVersion.Xml11, XmlCharacterEscapeContext.Uri), Is.EqualTo(reserved));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%GG", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%C0%80", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%E2%82", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%ED%A0%80", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("%F4%90%80%80", XmlVersion.Xml10, XmlCharacterEscapeContext.Uri));
        });
    }

    [Test]
    public void CoversEveryJsonEscapeAndMalformedUnicodeCase()
    {
        IXmlEscaper escaper = new XmlEscaper();
        string value = "\"\\/\b\f\n\r\t\u0000\u001F\u2028\u2029\U0001F600";
        string escaped = "\\\"\\\\/\\b\\f\\n\\r\\t\\u0000\\u001F\\u2028\\u2029\U0001F600";

        Assert.Multiple(() =>
        {
            Assert.That(escaper.EscapeXmlValue(value, XmlVersion.Xml11, XmlCharacterEscapeContext.Json, XmlInvalidCharacterPolicy.Error, '"'), Is.EqualTo(escaped));
            Assert.That(escaper.UnescapeXmlValue(escaped, XmlVersion.Xml11, XmlCharacterEscapeContext.Json), Is.EqualTo(value));
            Assert.That(escaper.UnescapeXmlValue("\\/\\u0041\\uD83D\\uDE00", XmlVersion.Xml10, XmlCharacterEscapeContext.Json), Is.EqualTo("/A\U0001F600"));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\q", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\u12G4", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\uD800", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("\\uDC00", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw\nline", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.Json));
        });
    }

    [Test]
    public void ValidatesEveryContextQuoteCombination()
    {
        IXmlEscaper escaper = new XmlEscaper();
        XmlCharacterEscapeContext[] contexts = (XmlCharacterEscapeContext[])Enum.GetValues(typeof(XmlCharacterEscapeContext));
        foreach (XmlCharacterEscapeContext context in contexts)
        {
            char expected = Quote(context);
            escaper.EscapeXmlValue("safe", XmlVersion.Xml10, context, XmlInvalidCharacterPolicy.Error, expected);
            if (context == XmlCharacterEscapeContext.PublicIdentifier)
            {
                escaper.EscapeXmlValue("safe", XmlVersion.Xml10, context, XmlInvalidCharacterPolicy.Error, expected == '"' ? '\'' : '"');
            }
            else
            {
                char wrong = expected == '\0' ? '"' : expected == '"' ? '\'' : '"';
                Assert.Throws<ArgumentException>(() => escaper.EscapeXmlValue("safe", XmlVersion.Xml10, context, XmlInvalidCharacterPolicy.Error, wrong), context.ToString());
            }
        }
    }

    private static char Quote(XmlCharacterEscapeContext context)
    {
        if (context == XmlCharacterEscapeContext.AttributeDoubleQuoted || context == XmlCharacterEscapeContext.SystemLiteralDoubleQuoted || context == XmlCharacterEscapeContext.XPathStringDoubleQuoted || context == XmlCharacterEscapeContext.Json || context == XmlCharacterEscapeContext.PublicIdentifier)
        {
            return '"';
        }

        if (context == XmlCharacterEscapeContext.AttributeSingleQuoted || context == XmlCharacterEscapeContext.SystemLiteralSingleQuoted || context == XmlCharacterEscapeContext.XPathStringSingleQuoted)
        {
            return '\'';
        }

        return '\0';
    }

    private static string SafeValue(XmlCharacterEscapeContext context)
    {
        if (context == XmlCharacterEscapeContext.PublicIdentifier)
        {
            return "-//Example PUBLIC 1.0//EN";
        }

        if (context == XmlCharacterEscapeContext.Uri)
        {
            return "path segment/\u00E9";
        }

        if (context == XmlCharacterEscapeContext.Json)
        {
            return "json \\ value";
        }

        return "safe & value \U0001F600";
    }
}
