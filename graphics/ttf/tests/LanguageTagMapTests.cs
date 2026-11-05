namespace ttf.Tests;

public class LanguageTagMapTests
{
    [Test]
    public void ParsesCanonicalCaseExtensionsAndLookupFallbacks()
    {
        Bcp47LanguageTag result = Bcp47LanguageTagParser.Parse(
            "DE-latn-de-1996-b-FOO-a-BAR-x-Private");

        Assert.That(result.GetCanonicalTag(), Is.EqualTo(
            "de-Latn-DE-1996-a-bar-b-foo-x-private"));
        Assert.That(result.GetLanguage(), Is.EqualTo("de"));
        Assert.That(result.GetScript(), Is.EqualTo("Latn"));
        Assert.That(result.GetRegion(), Is.EqualTo("DE"));
        Assert.That(result.GetVariantCount(), Is.EqualTo(1));
        Assert.That(result.GetVariant(0), Is.EqualTo("1996"));
        Assert.That(result.GetExtensionCount(), Is.EqualTo(2));
        Assert.That(result.GetExtension(0), Is.EqualTo("a-bar"));
        Assert.That(result.GetExtension(1), Is.EqualTo("b-foo"));
        Assert.That(result.GetPrivateUse(0), Is.EqualTo("private"));
        Assert.That(result.GetFallbackCount(), Is.EqualTo(7));
        Assert.That(result.GetFallback(0), Is.EqualTo(
            "de-Latn-DE-1996-a-bar-b-foo-x-private"));
        Assert.That(result.GetFallback(1), Is.EqualTo(
            "de-Latn-DE-1996-a-bar-b-foo"));
        Assert.That(result.GetFallback(2), Is.EqualTo(
            "de-Latn-DE-1996-a-bar"));
        Assert.That(result.GetFallback(3), Is.EqualTo("de-Latn-DE-1996"));
        Assert.That(result.GetFallback(4), Is.EqualTo("de-Latn-DE"));
        Assert.That(result.GetFallback(5), Is.EqualTo("de-Latn"));
        Assert.That(result.GetFallback(6), Is.EqualTo("de"));
    }

    [Test]
    public void ParsesExtendedLanguagesPrivateUseAndGrandfatheredAliases()
    {
        Bcp47LanguageTag extended = Bcp47LanguageTagParser.Parse(
            "zh-yue-Hant-HK");
        Bcp47LanguageTag privateUse = Bcp47LanguageTagParser.Parse("x-ACME-Test");
        Bcp47LanguageTag klingon = Bcp47LanguageTagParser.Parse("i-klingon");
        Bcp47LanguageTag legacyEnglish = Bcp47LanguageTagParser.Parse(
            "en-GB-oed");
        Bcp47LanguageTag deprecated = Bcp47LanguageTagParser.Parse("IW-il");

        Assert.That(extended.GetExtendedLanguageCount(), Is.EqualTo(1));
        Assert.That(extended.GetExtendedLanguage(0), Is.EqualTo("yue"));
        Assert.That(extended.GetCanonicalTag(), Is.EqualTo("zh-yue-Hant-HK"));
        Assert.That(privateUse.GetCanonicalTag(), Is.EqualTo("x-acme-test"));
        Assert.That(privateUse.GetPrivateUseCount(), Is.EqualTo(2));
        Assert.That(klingon.GetCanonicalTag(), Is.EqualTo("tlh"));
        Assert.That(legacyEnglish.GetCanonicalTag(),
            Is.EqualTo("en-GB-oxendict"));
        Assert.That(deprecated.GetCanonicalTag(), Is.EqualTo("he-IL"));
    }

    [TestCase("")]
    [TestCase("a")]
    [TestCase("en_")]
    [TestCase("en-")]
    [TestCase("en--US")]
    [TestCase("123")]
    [TestCase("de-1901-1901")]
    [TestCase("en-a-foo-a-bar")]
    [TestCase("en-a")]
    [TestCase("en-x")]
    public void RejectsLanguageTagsOutsideTheBcp47Grammar(string languageTag)
    {
        Assert.Throws<ArgumentException>(() =>
            Bcp47LanguageTagParser.Parse(languageTag));
    }

    [Test]
    public void MapsIsoLanguageCodesFromTheGeneratedRegistry()
    {
        OpenTypeLanguageTagResolution english =
            OpenTypeLanguageTagMap.Resolve("en-US");
        OpenTypeLanguageTagResolution french =
            OpenTypeLanguageTagMap.Resolve("fra-CA");
        OpenTypeLanguageTagResolution german =
            OpenTypeLanguageTagMap.Resolve("de");
        OpenTypeLanguageTagResolution unknown =
            OpenTypeLanguageTagMap.Resolve("qaa");

        AssertTags(english, "ENG ");
        AssertTags(french, "FRA ");
        AssertTags(german, "DEU ");
        Assert.That(unknown.GetLanguageSystemTagCount(), Is.Zero);
    }

    [Test]
    public void MapsContextSensitiveChineseAndMoldavianTagsInFallbackOrder()
    {
        OpenTypeLanguageTagResolution hongKong =
            OpenTypeLanguageTagMap.Resolve("zh-Hant-HK");
        OpenTypeLanguageTagResolution macao =
            OpenTypeLanguageTagMap.Resolve("zh-Hant-MO");
        OpenTypeLanguageTagResolution bopomofo =
            OpenTypeLanguageTagMap.Resolve("zh-Bopo-TW");
        OpenTypeLanguageTagResolution moldavian =
            OpenTypeLanguageTagMap.Resolve("ro-MD");

        AssertTags(hongKong, "ZHH ", "ZHT ", "ZHS ");
        AssertTags(macao, "ZHTM", "ZHT ", "ZHS ");
        AssertTags(bopomofo, "ZHP ", "ZHS ");
        AssertTags(moldavian, "MOL ", "ROM ");
    }

    [Test]
    public void MapsPhoneticVariantsAndSyriacScriptVariants()
    {
        OpenTypeLanguageTagResolution ipa =
            OpenTypeLanguageTagMap.Resolve("en-fonipa");
        OpenTypeLanguageTagResolution uralic =
            OpenTypeLanguageTagMap.Resolve("fi-fonupa");
        OpenTypeLanguageTagResolution estrangela =
            OpenTypeLanguageTagMap.Resolve("syr-Syre");

        AssertTags(ipa, "IPPH", "ENG ");
        AssertTags(uralic, "UPPH", "FIN ");
        AssertTags(estrangela, "SYRE", "SYR ");
    }

    [Test]
    public void ValidatesNullsAndResultIndexes()
    {
        Bcp47LanguageTag parsed = Bcp47LanguageTagParser.Parse("en");
        OpenTypeLanguageTagResolution result =
            OpenTypeLanguageTagMap.Resolve("en");

        Assert.Throws<ArgumentNullException>(() =>
            Bcp47LanguageTagParser.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => parsed.HasVariant(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => parsed.GetVariant(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => parsed.GetFallback(1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetLanguageSystemTag(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            result.GetLanguageSystemTag(1));
    }

    static void AssertTags(
        OpenTypeLanguageTagResolution result,
        params string[] expected)
    {
        Assert.That(result.GetLanguageSystemTagCount(),
            Is.EqualTo(expected.Length));

        int index = 0;

        while (index < expected.Length)
        {
            Assert.That(result.GetLanguageSystemTag(index),
                Is.EqualTo(Tag(expected[index])));
            index++;
        }
    }

    static int Tag(string value)
    {
        return value[0] << 24 |
            value[1] << 16 |
            value[2] << 8 |
            value[3];
    }
}
