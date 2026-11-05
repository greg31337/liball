using System.Globalization;
using countrycodes;

namespace CountryCodes.Tests;

[TestFixture]
public sealed class LocaleTagUtilityTests
{
    [Test]
    public void StructuralValidationAcceptsRegularPrivateUseAndGrandfatheredTags()
    {
        Assert.That(LocaleTagUtility.IsStructurallyValid("en"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("zh-Hant-TW"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("es-419"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("sl-rozaj-biske-1994"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("en-US-u-ca-gregory-x-test"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("x-company-private"), Is.True);
        Assert.That(LocaleTagUtility.IsStructurallyValid("i-default"), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("en_US")]
    [TestCase("-en")]
    [TestCase("en-")]
    [TestCase("en--US")]
    [TestCase("e-US")]
    [TestCase("én-US")]
    [TestCase("en-abc1")]
    [TestCase("en-u")]
    [TestCase("en-u-ca-U-nu-latn")]
    [TestCase("sl-rozaj-ROZAJ")]
    [TestCase("x")]
    [TestCase("x-private-toolongpart")]
    public void StructuralValidationRejectsMalformedTags(string? value)
    {
        Assert.That(LocaleTagUtility.IsStructurallyValid(value), Is.False);
        Assert.That(LocaleTagUtility.NormalizeLocaleTag(value), Is.Null);
    }

    [Test]
    public void NormalizationAppliesCanonicalSubtagCasing()
    {
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("EN-lATN-us"), Is.EqualTo("en-Latn-US"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("ES-419"), Is.EqualTo("es-419"));
        Assert.That(
            LocaleTagUtility.NormalizeLocaleTag("SL-ROZAJ-BISKE-1994"),
            Is.EqualTo("sl-rozaj-biske-1994"));
        Assert.That(
            LocaleTagUtility.NormalizeLocaleTag("EN-us-U-CA-GREGORY-X-TEST"),
            Is.EqualTo("en-US-u-ca-gregory-x-test"));
        Assert.That(
            LocaleTagUtility.NormalizeLocaleTag("X-COMPANY-PRIVATE"),
            Is.EqualTo("x-company-private"));
    }

    [Test]
    public void NormalizationAppliesPinnedIanaPreferredValues()
    {
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("iw-IL"), Is.EqualTo("he-IL"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("in-ID"), Is.EqualTo("id-ID"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("ajp"), Is.EqualTo("apc"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("my-BU"), Is.EqualTo("my-MM"));
        Assert.That(
            LocaleTagUtility.NormalizeLocaleTag("ja-Latn-hepburn-heploc"),
            Is.EqualTo("ja-Latn-hepburn-alalc97"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("zh-cmn-Hant"), Is.EqualTo("cmn-Hant"));
    }

    [Test]
    public void NormalizationHandlesGrandfatheredTags()
    {
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("I-KLINGON"), Is.EqualTo("tlh"));
        Assert.That(
            LocaleTagUtility.NormalizeLocaleTag("EN-gb-OED"),
            Is.EqualTo("en-GB-oxendict"));
        Assert.That(LocaleTagUtility.NormalizeLocaleTag("I-DEFAULT"), Is.EqualTo("i-default"));
    }

    [Test]
    public void CanonicalCheckIncludesCasingAndPreferredValueReplacement()
    {
        Assert.That(LocaleTagUtility.IsCanonicalLocaleTag("en-Latn-US"), Is.True);
        Assert.That(LocaleTagUtility.IsCanonicalLocaleTag("en-latn-US"), Is.False);
        Assert.That(LocaleTagUtility.IsCanonicalLocaleTag("iw-IL"), Is.False);
        Assert.That(LocaleTagUtility.IsCanonicalLocaleTag("he-IL"), Is.True);
        Assert.That(LocaleTagUtility.IsCanonicalLocaleTag("not_valid"), Is.False);
    }

    [Test]
    public void FallbackChainUsesCanonicalRfcLookupTruncation()
    {
        Assert.That(
            LocaleTagUtility.GetFallbackChain("ZH-hant-tw"),
            Is.EqualTo(new[] { "zh-Hant-TW", "zh-Hant", "zh" }));
        Assert.That(
            LocaleTagUtility.GetFallbackChain("en-US-u-ca-gregory-x-test"),
            Is.EqualTo(new[]
            {
                "en-US-u-ca-gregory-x-test",
                "en-US-u-ca-gregory",
                "en-US-u-ca",
                "en-US",
                "en"
            }));
        Assert.That(
            LocaleTagUtility.GetFallbackChain("i-default"),
            Is.EqualTo(new[] { "i-default" }));
        Assert.That(LocaleTagUtility.GetFallbackChain("invalid_tag"), Is.Empty);
    }

    [Test]
    public void ResultsDoNotDependOnAmbientCulture()
    {
        CultureInfo originalCulture = CultureInfo.CurrentCulture;
        CultureInfo originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
            CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");

            Assert.That(LocaleTagUtility.NormalizeLocaleTag("IW-il"), Is.EqualTo("he-IL"));
            Assert.That(
                LocaleTagUtility.GetFallbackChain("ZH-hant-tw"),
                Is.EqualTo(new[] { "zh-Hant-TW", "zh-Hant", "zh" }));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }
}
