using fake;

namespace fake.Tests;

[TestFixture]
public class TextTransformTests
{
    [Test]
    public void CasingUsesCapturedAndExplicitLocales()
    {
        TextProvider text = Fake.Create("tr-TR", 3601L).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Lower("Iİ"), Is.EqualTo("ıi"));
            Assert.That(text.Upper("istanbul ı"), Is.EqualTo("İSTANBUL I"));
            Assert.That(text.TitleCase("istanbul izmir"), Is.EqualTo("İstanbul İzmir"));
            Assert.That(text.LowerFromValueAndLocale("I", "en-US"), Is.EqualTo("i"));
            Assert.That(text.UpperFromValueAndLocale("i", "en-US"), Is.EqualTo("I"));
        });
    }

    [Test]
    public void FoldAsciiDecomposesLatinTextAndOmitsUnsupportedScalars()
    {
        TextProvider text = Fake.Create(3602).Text();

        Assert.Multiple(() =>
        {
            Assert.That(
                text.FoldAscii("Crème brûlée, Æsir, Łódź, Straße, ﬁle"),
                Is.EqualTo("Creme brulee, AEsir, Lodz, Strasse, file"));
            Assert.That(text.FoldAscii("東京🙂"), Is.Empty);
            Assert.That(text.FoldAscii("e\u0301"), Is.EqualTo("e"));
        });
    }

    [Test]
    public void DeclaredTransliterationTablesAreStableAndNarrow()
    {
        TextProvider text = Fake.Create(3603).Text();
        string[] tables = text.TransliterationTables();

        Assert.That(tables, Is.EqualTo(new[]
        {
            "latin-ascii",
            "greek-latin",
            "cyrillic-latin",
            "arabic-latin",
            "hebrew-latin"
        }));
        tables[0] = "changed";
        Assert.That(text.TransliterationTables()[0], Is.EqualTo("latin-ascii"));

        Assert.Multiple(() =>
        {
            Assert.That(text.Transliterate("Αθήνα", "greek-latin"), Is.EqualTo("Athina"));
            Assert.That(text.Transliterate("Ёж", "cyrillic-latin"), Is.EqualTo("Yozh"));
            Assert.That(text.Transliterate("العربية", "arabic-latin"), Is.EqualTo("alrbyh"));
            Assert.That(text.Transliterate("שלום", "hebrew-latin"), Is.EqualTo("shlvm"));
            Assert.That(text.Transliterate("東京 Αθήνα", "greek-latin"), Is.EqualTo("東京 Athina"));
        });
    }

    [Test]
    public void SlugProducesCollapsedLowercaseAsciiWords()
    {
        TextProvider text = Fake.Create(3604).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Slug("  Crème brûlée — Αθήνα / Ёж  "), Is.EqualTo("creme-brulee-athina-yozh"));
            Assert.That(text.SlugFromValueAndSeparator("one...two", '_'), Is.EqualTo("one_two"));
            Assert.That(text.Slug("العربية"), Is.EqualTo("alrbyh"));
            Assert.That(text.Slug("🙂 !!!"), Is.Empty);
        });
    }

    [Test]
    public void IdentifierProducesAConservativeAsciiIdentifier()
    {
        TextProvider text = Fake.Create(3605).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Identifier("  42 Crème brûlée! "), Is.EqualTo("_42_Creme_brulee"));
            Assert.That(text.Identifier("_Already__safe"), Is.EqualTo("_Already_safe"));
            Assert.That(text.Identifier("Αθήνα Ёж"), Is.EqualTo("Athina_Yozh"));
            Assert.That(text.Identifier("🙂"), Is.EqualTo("_"));
        });
    }

    [Test]
    public void DirectionClassifiesNeutralLtrRtlAndMixedText()
    {
        TextProvider text = Fake.Create(3606).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Direction("123 --"), Is.EqualTo(WritingDirection.Neutral));
            Assert.That(text.Direction("hello 123"), Is.EqualTo(WritingDirection.LeftToRight));
            Assert.That(text.Direction("שלום 123"), Is.EqualTo(WritingDirection.RightToLeft));
            Assert.That(text.Direction("hello שלום"), Is.EqualTo(WritingDirection.Mixed));
        });
    }

    [Test]
    public void MixedScriptDetectionIgnoresCommonTextAndGroupsJapaneseScripts()
    {
        TextProvider text = Fake.Create(3607).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.HasMixedScripts("раypal"), Is.True);
            Assert.That(text.HasMixedScripts("hello, world 123"), Is.False);
            Assert.That(text.HasMixedScripts("Αθήνα"), Is.False);
            Assert.That(text.HasMixedScripts("東京カタカナ"), Is.False);
            Assert.That(text.HasMixedScripts("hello שלום"), Is.True);
        });
    }

    [Test]
    public void BidiIsolationUsesDirectionAndControlsCanBeRemoved()
    {
        TextProvider text = Fake.Create(3608).Text();
        string empty = new string(new char[0]);

        Assert.Multiple(() =>
        {
            Assert.That(text.BidiIsolate("abc"), Is.EqualTo("\u2066abc\u2069"));
            Assert.That(text.BidiIsolate("שלום"), Is.EqualTo("\u2067שלום\u2069"));
            Assert.That(text.BidiIsolate("abc שלום"), Is.EqualTo("\u2068abc שלום\u2069"));
            Assert.That(text.BidiIsolate("123"), Is.EqualTo("\u2068123\u2069"));
            Assert.That(text.BidiIsolate(empty), Is.SameAs(empty));
            Assert.That(text.StripBidiControls("a\u202Eb\u202C\u2067ג\u2069"), Is.EqualTo("abג"));
        });
    }

    [Test]
    public void TextTransformsValidateMalformedTextTablesLocalesAndSeparators()
    {
        TextProvider text = Fake.Create(3609).Text();
        string malformed = new string(new[] { 'a', '\uD800' });

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Lower(null)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Upper(malformed)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.TitleCaseFromValueAndLocale("value", "not_a_locale")).ParamName, Is.EqualTo("locale"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Transliterate("value", "unknown")).ParamName, Is.EqualTo("table"));
            Assert.That(Assert.Throws<ArgumentNullException>(() => text.Transliterate("value", null)).ParamName, Is.EqualTo("table"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.SlugFromValueAndSeparator("value", '.')).ParamName, Is.EqualTo("separator"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Identifier(malformed)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.Direction(malformed)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.HasMixedScripts(malformed)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.BidiIsolate(malformed)).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentException>(() => text.StripBidiControls(malformed)).ParamName, Is.EqualTo("value"));
        });
    }

    [Test]
    public void TextTransformsHonorConfiguredOutputLimit()
    {
        FakeOptions options = FakeOptions.Create(3610L);
        options.MaximumOutputSize = 4;
        TextProvider text = Fake.Create(options).Text();

        Assert.Multiple(() =>
        {
            Assert.That(text.Upper("abcd"), Is.EqualTo("ABCD"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Upper("abcde")).ParamName, Is.EqualTo("value"));
            Assert.That(text.Transliterate("Щ", "cyrillic-latin"), Is.EqualTo("Shch"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.Transliterate("Ща", "cyrillic-latin")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.FoldAscii("ßßß")).ParamName, Is.EqualTo("value"));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => text.BidiIsolate("abc")).ParamName, Is.EqualTo("value"));
            Assert.That(text.Identifier("🙂"), Is.EqualTo("_"));
        });
    }

    [Test]
    public void TextTransformsDoNotConsumeTextEntropy()
    {
        TextProvider actual = Fake.Create(3611).Text();
        TextProvider expectedPosition = Fake.Create(3611).Text();

        actual.LowerFromValueAndLocale("I", "tr-TR");
        actual.UpperFromValueAndLocale("i", "tr-TR");
        actual.TitleCase("hello world");
        actual.FoldAscii("Crème");
        actual.Transliterate("Αθήνα", "greek-latin");
        actual.Slug("Crème brûlée");
        actual.Identifier("42 values");
        actual.Direction("hello שלום");
        actual.HasMixedScripts("раypal");
        actual.BidiIsolate("שלום");
        actual.StripBidiControls("a\u200Fb");

        Assert.That(actual.Clamp("", 30, 30), Is.EqualTo(expectedPosition.Clamp("", 30, 30)));
    }
}
