namespace odf.Tests;

public class OdfLanguageRulesTests
{
    [TestCase("en")]
    [TestCase("eng")]
    [TestCase("abcdefg")]
    [TestCase("ZXX")]
    public void LanguageAcceptsAsciiLanguageSubtags(string value)
    {
        Assert.That(OdfLanguageRules.IsLanguage(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("e")]
    [TestCase("language9")]
    [TestCase("éñ")]
    [TestCase("toolongggg")]
    public void LanguageRejectsInvalidLanguageSubtags(string? value)
    {
        Assert.That(OdfLanguageRules.IsLanguage(value), Is.False);
    }

    [Test]
    public void CountryAndScriptRulesValidateAndNormalizeRegisteredShapes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLanguageRules.IsCountry("US"), Is.True);
            Assert.That(OdfLanguageRules.IsCountry("gb"), Is.True);
            Assert.That(OdfLanguageRules.IsCountry("419"), Is.True);
            Assert.That(OdfLanguageRules.IsCountry("USA"), Is.False);
            Assert.That(OdfLanguageRules.IsCountry("U1"), Is.False);
            Assert.That(OdfLanguageRules.NormalizeCountry("gb"), Is.EqualTo("GB"));
            Assert.That(OdfLanguageRules.NormalizeCountry("419"), Is.EqualTo("419"));
            Assert.That(OdfLanguageRules.IsScript("Latn"), Is.True);
            Assert.That(OdfLanguageRules.IsScript("cyrl"), Is.True);
            Assert.That(OdfLanguageRules.IsScript("Latin"), Is.False);
            Assert.That(OdfLanguageRules.NormalizeScript("cYRL"), Is.EqualTo("Cyrl"));
        });
    }

    [TestCase("en")]
    [TestCase("en-US")]
    [TestCase("zh-Hans-CN")]
    [TestCase("zh-cmn-Hans-CN")]
    [TestCase("de-CH-1901")]
    [TestCase("sl-rozaj-biske-1994")]
    [TestCase("en-US-u-ca-gregory")]
    [TestCase("x-private")]
    [TestCase("i-klingon")]
    [TestCase("sgn-BE-FR")]
    public void LanguageTagAcceptsRfc5646Shapes(string value)
    {
        Assert.That(OdfLanguageRules.IsLanguageTag(value), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("e")]
    [TestCase("en_US")]
    [TestCase("en--US")]
    [TestCase("en-")]
    [TestCase("-en")]
    [TestCase("en-a")]
    [TestCase("en-US-x")]
    [TestCase("en-Latn-USA")]
    [TestCase("sl-rozaj-rozaj")]
    [TestCase("en-a-foo-a-bar")]
    [TestCase("x")]
    public void LanguageTagRejectsMalformedShapes(string? value)
    {
        Assert.That(OdfLanguageRules.IsLanguageTag(value), Is.False);
    }

    [TestCase("EN-latn-us", "en-Latn-US")]
    [TestCase("zh-hANS-cn", "zh-Hans-CN")]
    [TestCase("EN-us-U-CA-GREGORY", "en-US-u-ca-gregory")]
    [TestCase("X-PRIVATE-Value", "x-private-value")]
    [TestCase("I-KLINGON", "i-klingon")]
    public void LanguageTagNormalizationUsesCanonicalSubtagCasing(string value, string expected)
    {
        Assert.That(OdfLanguageRules.NormalizeLanguageTag(value), Is.EqualTo(expected));
    }

    [Test]
    public void InvalidLanguageValuesArePreservedByNormalization()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfLanguageRules.NormalizeLanguageTag("invalid_tag"), Is.EqualTo("invalid_tag"));
            Assert.That(OdfLanguageRules.NormalizeCountry("USA"), Is.EqualTo("USA"));
            Assert.That(OdfLanguageRules.NormalizeScript("Latin"), Is.EqualTo("Latin"));
            Assert.That(OdfLanguageRules.NormalizeLanguageTag(null), Is.Empty);
        });
    }
}
