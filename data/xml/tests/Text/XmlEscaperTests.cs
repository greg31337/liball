using xml;

namespace xml.Tests;

[TestFixture]
public class XmlEscaperTests
{
    [Test]
    public void EscapesAndUnescapesXml10TextWithoutChangingItsValue()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "a&<b]]>c\r\t\n\U0001F600";

        string escaped = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0');
        string unescaped = escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Text);

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo("a&amp;&lt;b]]&gt;c&#xD;\t\n\U0001F600"));
            Assert.That(unescaped, Is.EqualTo(value));
        });
    }

    [Test]
    public void EscapesTheSelectedAttributeQuoteAndNormalizationSensitiveWhitespace()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "\"'&<\t\n\r";

        string doubleQuoted = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeDoubleQuoted, XmlInvalidCharacterPolicy.Error, '"');
        string singleQuoted = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeSingleQuoted, XmlInvalidCharacterPolicy.Error, '\'');

        Assert.Multiple(() =>
        {
            Assert.That(doubleQuoted, Is.EqualTo("&quot;'&amp;&lt;&#x9;&#xA;&#xD;"));
            Assert.That(singleQuoted, Is.EqualTo("\"&apos;&amp;&lt;&#x9;&#xA;&#xD;"));
            Assert.That(escaper.UnescapeXmlValue(doubleQuoted, XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeDoubleQuoted), Is.EqualTo(value));
            Assert.That(escaper.UnescapeXmlValue(singleQuoted, XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeSingleQuoted), Is.EqualTo(value));
        });
    }

    [Test]
    public void AppliesXml11ReferenceErrorAndReplacementPolicies()
    {
        XmlEscaper escaper = new XmlEscaper();
        string value = "\u0001\u0085\u2028";

        string referenced = escaper.EscapeXmlValue(value, XmlVersion.Xml11, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.CharacterReference, '\0');
        string replaced = escaper.EscapeXmlValue(value, XmlVersion.Xml11, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Replace, '\0');
        XmlSerializationException failure = Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(value, XmlVersion.Xml11, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'))!;

        Assert.Multiple(() =>
        {
            Assert.That(referenced, Is.EqualTo("&#x1;&#x85;&#x2028;"));
            Assert.That(escaper.UnescapeXmlValue(referenced, XmlVersion.Xml11, XmlCharacterEscapeContext.Text), Is.EqualTo(value));
            Assert.That(replaced, Is.EqualTo("\uFFFD&#x85;&#x2028;"));
            Assert.That(failure.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.InvalidCharacter));
            Assert.That(failure.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Serialization));
        });
    }

    [Test]
    public void ReplacesMalformedUtf16ButNeverEmitsIllegalXml10References()
    {
        XmlEscaper escaper = new XmlEscaper();
        string malformed = "\uD800X\uDC00\0";

        string replaced = escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Replace, '\0');

        Assert.That(replaced, Is.EqualTo("\uFFFDX\uFFFD\uFFFD"));
        Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue("\u0001", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.CharacterReference, '\0'));
        Assert.Throws<XmlSerializationException>(() => escaper.EscapeXmlValue(malformed, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'));
    }

    [Test]
    public void ResolvesPredefinedDecimalHexadecimalAndSupplementaryReferences()
    {
        XmlEscaper escaper = new XmlEscaper();
        string escaped = "&amp;&lt;&gt;&apos;&quot;&#65;&#x1F600;";

        string result = escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Text);

        Assert.That(result, Is.EqualTo("&<>'\"A\U0001F600"));
    }

    [Test]
    public void RejectsMalformedReferencesAndUnescapedContextDelimiters()
    {
        XmlEscaper escaper = new XmlEscaper();

        Assert.Multiple(() =>
        {
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&unknown;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&amp", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("&#x1;", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw<markup", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw\"quote", XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeDoubleQuoted));
            Assert.Throws<XmlSerializationException>(() => escaper.UnescapeXmlValue("raw]]>text", XmlVersion.Xml10, XmlCharacterEscapeContext.Text));
            Assert.That(escaper.UnescapeXmlValue("&#x5D;]>", XmlVersion.Xml10, XmlCharacterEscapeContext.Text), Is.EqualTo("]]>"));
        });
    }

    [Test]
    public void PreservesScalarsAndDelimiterDetectionAtConventionalChunkBoundaries()
    {
        XmlEscaper escaper = new XmlEscaper();
        string prefix = new string('a', 4095);
        string value = prefix + "\U0001F600]]>tail";

        string escaped = escaper.EscapeXmlValue(value, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0');

        Assert.Multiple(() =>
        {
            Assert.That(escaped, Is.EqualTo(prefix + "\U0001F600]]&gt;tail"));
            Assert.That(escaper.UnescapeXmlValue(escaped, XmlVersion.Xml10, XmlCharacterEscapeContext.Text), Is.EqualTo(value));
        });
    }

    [Test]
    public void ValidatesVersionsContextsPoliciesAndQuoteChoices()
    {
        XmlEscaper escaper = new XmlEscaper();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => escaper.EscapeXmlValue(null!, XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<ArgumentOutOfRangeException>(() => escaper.EscapeXmlValue("value", (XmlVersion)99, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<ArgumentOutOfRangeException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, (XmlCharacterEscapeContext)99, XmlInvalidCharacterPolicy.Error, '\0'));
            Assert.Throws<ArgumentOutOfRangeException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, (XmlInvalidCharacterPolicy)99, '\0'));
            Assert.Throws<ArgumentException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, XmlCharacterEscapeContext.Text, XmlInvalidCharacterPolicy.Error, '"'));
            Assert.Throws<ArgumentException>(() => escaper.EscapeXmlValue("value", XmlVersion.Xml10, XmlCharacterEscapeContext.AttributeDoubleQuoted, XmlInvalidCharacterPolicy.Error, '\''));
        });
    }
}
