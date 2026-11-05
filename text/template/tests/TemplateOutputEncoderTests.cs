using System;
using System.Globalization;
using System.IO;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateOutputEncoderTests
{
    [Test]
    public void Encode_None_ReturnsInputUnchanged()
    {
        Assert.That(TemplateOutputEncoder.Encode("a < b & c", TemplateEscapingMode.None), Is.EqualTo("a < b & c"));
    }

    [Test]
    public void EncodeHtml_EscapesHtmlSpecialCharacters()
    {
        string encoded = TemplateOutputEncoder.EncodeHtml("<a title=\"Tom & 'Ada'\">");

        Assert.That(encoded, Is.EqualTo("&lt;a title=&quot;Tom &amp; &#39;Ada&#39;&quot;&gt;"));
        Assert.That(TemplateOutputEncoder.Encode("<", TemplateEscapingMode.Html), Is.EqualTo("&lt;"));
    }

    [Test]
    public void EncodeXml_EscapesXmlSpecialCharacters()
    {
        string encoded = TemplateOutputEncoder.EncodeXml("<a title=\"Tom & 'Ada'\">");

        Assert.That(encoded, Is.EqualTo("&lt;a title=&quot;Tom &amp; &apos;Ada&apos;&quot;&gt;"));
        Assert.That(TemplateOutputEncoder.Encode("'", TemplateEscapingMode.Xml), Is.EqualTo("&apos;"));
    }

    [Test]
    public void EncodeJsonString_EscapesJsonStringContentWithoutWrappingQuotes()
    {
        string value = "quote \" slash \\ line\n tab\t control " + ((char)1).ToString();
        string encoded = TemplateOutputEncoder.EncodeJsonString(value);

        Assert.That(encoded, Is.EqualTo("quote \\\" slash \\\\ line\\n tab\\t control \\u0001"));
        Assert.That(TemplateOutputEncoder.Encode("\"", TemplateEscapingMode.JsonString), Is.EqualTo("\\\""));
    }

    [Test]
    public void EncodeJavaScriptString_EscapesStringAndHtmlSensitiveCharacters()
    {
        string value = "'\"\\\n</script>&=-";
        string encoded = TemplateOutputEncoder.EncodeJavaScriptString(value);

        Assert.That(encoded, Is.EqualTo("\\'\\\"\\\\\\n\\u003C/script\\u003E\\u0026\\u003D\\u002D"));
        Assert.That(TemplateOutputEncoder.Encode("<", TemplateEscapingMode.JavaScriptString), Is.EqualTo("\\u003C"));
    }

    [Test]
    public void EncodeUrl_UsesPercentEncoding()
    {
        Assert.That(TemplateOutputEncoder.EncodeUrl("a b&c=/"), Is.EqualTo("a%20b%26c%3D%2F"));
        Assert.That(TemplateOutputEncoder.Encode("a b", TemplateEscapingMode.Url), Is.EqualTo("a%20b"));
    }

    [Test]
    public void EncodeCsvField_QuotesOnlyWhenRequiredAndDoublesQuotes()
    {
        Assert.That(TemplateOutputEncoder.EncodeCsvField("plain"), Is.EqualTo("plain"));
        Assert.That(TemplateOutputEncoder.EncodeCsvField("a,b"), Is.EqualTo("\"a,b\""));
        Assert.That(TemplateOutputEncoder.EncodeCsvField("a\"b"), Is.EqualTo("\"a\"\"b\""));
        Assert.That(TemplateOutputEncoder.EncodeCsvField("a\nb"), Is.EqualTo("\"a\nb\""));
        Assert.That(TemplateOutputEncoder.Encode("a,b", TemplateEscapingMode.CsvField), Is.EqualTo("\"a,b\""));
    }

    [Test]
    public void WriteEncoded_WritesEncodedTextThroughTemplateWriterLimits()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumOutputLength = 4;
        TemplateWriter writer = TemplateWriter.Create(inner, policy);

        TemplateOutputEncoder.WriteEncoded(writer, "<", TemplateEscapingMode.Html);

        Assert.That(inner.ToString(), Is.EqualTo("&lt;"));
        Assert.That(writer.WrittenLength, Is.EqualTo(4));
        Assert.Throws<InvalidOperationException>(() => TemplateOutputEncoder.WriteEncoded(writer, ">", TemplateEscapingMode.Html));
        Assert.That(inner.ToString(), Is.EqualTo("&lt;"));
    }

    [Test]
    public void EncodeValue_EscapesOrdinaryStringsButLeavesSafeStringsUnchanged()
    {
        TemplateValue ordinary = TemplateValue.FromString("<b>Ada & Grace</b>");
        TemplateValue safe = TemplateValue.FromSafeString("<b>Ada & Grace</b>");

        Assert.That(TemplateOutputEncoder.EncodeValue(ordinary, TemplateEscapingMode.Html), Is.EqualTo("&lt;b&gt;Ada &amp; Grace&lt;/b&gt;"));
        Assert.That(TemplateOutputEncoder.EncodeValue(safe, TemplateEscapingMode.Html), Is.EqualTo("<b>Ada & Grace</b>"));
        Assert.That(TemplateOutputEncoder.EncodeValue(safe, TemplateEscapingMode.JsonString), Is.EqualTo("<b>Ada & Grace</b>"));
    }

    [Test]
    public void EncodeValue_RawOutputBypassesSelectedEscapingMode()
    {
        TemplateValue value = TemplateValue.FromString("<b>raw & ready</b>");

        Assert.That(TemplateOutputEncoder.EncodeValue(value, TemplateEscapingMode.Html, true), Is.EqualTo("<b>raw & ready</b>"));
        Assert.That(TemplateOutputEncoder.EncodeValue(value, TemplateEscapingMode.JsonString, true), Is.EqualTo("<b>raw & ready</b>"));
        Assert.That(TemplateOutputEncoder.EncodeValue(value, TemplateEscapingMode.Html, false), Is.EqualTo("&lt;b&gt;raw &amp; ready&lt;/b&gt;"));
    }

    [Test]
    public void ConvertValueToText_UsesScalarTextAndTreatsMissingNullAsEmpty()
    {
        Assert.That(TemplateOutputEncoder.ConvertValueToText(TemplateValue.FromNumber(1.25m)), Is.EqualTo("1.25"));
        Assert.That(TemplateOutputEncoder.ConvertValueToText(TemplateValue.FromBoolean(false)), Is.EqualTo("false"));
        Assert.That(TemplateOutputEncoder.ConvertValueToText(TemplateValue.Null()), Is.EqualTo(""));
        Assert.That(TemplateOutputEncoder.ConvertValueToText(TemplateValue.Missing()), Is.EqualTo(""));
    }

    [Test]
    public void WriteEncodedValue_UsesSafeAndRawRulesBeforeOutputLimitChecks()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.MaximumOutputLength = 8;
        TemplateWriter writer = TemplateWriter.Create(inner, policy);

        TemplateOutputEncoder.WriteEncodedValue(writer, TemplateValue.FromSafeString("<b>"), TemplateEscapingMode.Html);
        TemplateOutputEncoder.WriteEncodedValue(writer, TemplateValue.FromString("<i>"), TemplateEscapingMode.Html, true);

        Assert.That(inner.ToString(), Is.EqualTo("<b><i>"));
        Assert.That(writer.WrittenLength, Is.EqualTo(6));
        Assert.Throws<InvalidOperationException>(() => TemplateOutputEncoder.WriteEncodedValue(writer, TemplateValue.FromString("<"), TemplateEscapingMode.Html));
        Assert.That(inner.ToString(), Is.EqualTo("<b><i>"));
    }

    [Test]
    public void Encoders_RejectNullInputs()
    {
        StringWriter inner = new StringWriter(CultureInfo.InvariantCulture);
        TemplateWriter writer = new TemplateWriter(inner);

        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.Encode(null!, TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeHtml(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeXml(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeJsonString(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeJavaScriptString(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeUrl(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeCsvField(null!));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.WriteEncoded(null!, "x", TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.WriteEncoded(writer, null!, TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.EncodeValue(null!, TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.WriteEncodedValue(null!, TemplateValue.FromString("x"), TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.WriteEncodedValue(writer, null!, TemplateEscapingMode.None));
        Assert.Throws<ArgumentNullException>(() => TemplateOutputEncoder.ConvertValueToText(null!));
    }
}
