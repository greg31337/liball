using ttf;

namespace ttf.Tests;

internal class TextShapingOptionsPublicTests
{
    [Test]
    public void DefaultsAreStrictNormalizedAutomaticAndFinite()
    {
        TextShapingOptions options = new TextShapingOptions();
        TextShapingLimits limits = options.GetLimits();

        Assert.Multiple(() =>
        {
            Assert.That(options.GetDecodingMode(),
                Is.EqualTo(Utf16DecodingMode.Strict));
            Assert.That(options.GetNormalizationMode(),
                Is.EqualTo(UnicodeNormalizationMode.Nfc));
            Assert.That(options.GetDirection(),
                Is.EqualTo(TextShapingDirection.Auto));
            Assert.That(options.HasScript(), Is.False);
            Assert.That(options.GetScript(), Is.Empty);
            Assert.That(options.GetLanguageTag(), Is.EqualTo("und"));
            Assert.That(options.GetFeatureCount(), Is.Zero);
            Assert.That(options.GetClusterPolicy(),
                Is.EqualTo(TextShapingClusterPolicy.GraphemeClusters));
            Assert.That(options.GetVerticalBehavior(),
                Is.EqualTo(TextShapingVerticalBehavior.Automatic));
            Assert.That(options.GetHorizontalPixelsPerEm(), Is.Zero);
            Assert.That(options.GetVerticalPixelsPerEm(), Is.Zero);
            Assert.That(limits.GetMaximumUtf16CodeUnits(), Is.GreaterThan(0));
            Assert.That(limits.GetMaximumRunes(), Is.GreaterThan(0));
            Assert.That(limits.GetMaximumClusters(), Is.GreaterThan(0));
            Assert.That(limits.GetMaximumRuns(), Is.GreaterThan(0));
            Assert.That(limits.GetMaximumGlyphs(), Is.GreaterThan(0));
            Assert.That(limits.GetMaximumLayoutOperations(), Is.GreaterThan(0));
        });
    }

    [Test]
    public void StoresEveryCallerSelectedShapingControl()
    {
        TextShapingOptions options = new TextShapingOptions();
        FontLayoutFeatureSetting liga = new FontLayoutFeatureSetting(
            Tag("liga"), 0, 2, 8);

        options.SetDecodingMode(Utf16DecodingMode.Replacement);
        options.SetNormalizationMode(UnicodeNormalizationMode.Nfkd);
        options.SetDirection(TextShapingDirection.BottomToTop);
        options.SetScript("lAtN");
        options.SetLanguageTag("EN-latn-us");
        options.SetFeatures(new FontLayoutFeatureSetting[] { liga });
        options.SetClusterPolicy(TextShapingClusterPolicy.UnicodeScalars);
        options.SetVerticalBehavior(TextShapingVerticalBehavior.Sideways);
        options.SetHorizontalPixelsPerEm(16);
        options.SetVerticalPixelsPerEm(18);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetDecodingMode(),
                Is.EqualTo(Utf16DecodingMode.Replacement));
            Assert.That(options.GetNormalizationMode(),
                Is.EqualTo(UnicodeNormalizationMode.Nfkd));
            Assert.That(options.GetDirection(),
                Is.EqualTo(TextShapingDirection.BottomToTop));
            Assert.That(options.HasScript(), Is.True);
            Assert.That(options.GetScript(), Is.EqualTo("Latn"));
            Assert.That(options.GetLanguageTag(), Is.EqualTo("en-Latn-US"));
            Assert.That(options.GetFeatureCount(), Is.EqualTo(1));
            Assert.That(options.GetFeature(0), Is.SameAs(liga));
            Assert.That(options.GetClusterPolicy(),
                Is.EqualTo(TextShapingClusterPolicy.UnicodeScalars));
            Assert.That(options.GetVerticalBehavior(),
                Is.EqualTo(TextShapingVerticalBehavior.Sideways));
            Assert.That(options.GetHorizontalPixelsPerEm(), Is.EqualTo(16));
            Assert.That(options.GetVerticalPixelsPerEm(), Is.EqualTo(18));
        });

        options.ClearScript();
        options.ClearFeatures();
        Assert.Multiple(() =>
        {
            Assert.That(options.HasScript(), Is.False);
            Assert.That(options.GetFeatureCount(), Is.Zero);
        });
    }

    [Test]
    public void OwnsFeatureArraysAndLimitValues()
    {
        TextShapingOptions options = new TextShapingOptions();
        FontLayoutFeatureSetting liga = new FontLayoutFeatureSetting(
            Tag("liga"), 1, 0, 4);
        FontLayoutFeatureSetting kern = new FontLayoutFeatureSetting(
            Tag("kern"), 0, 0, 4);
        FontLayoutFeatureSetting[] features =
            new FontLayoutFeatureSetting[] { liga };
        options.SetFeatures(features);
        features[0] = kern;

        TextShapingLimits supplied = new TextShapingLimits();
        supplied.SetMaximumRunes(123);
        supplied.SetMaximumLayoutOperations(456);
        options.SetLimits(supplied);
        supplied.SetMaximumRunes(999);
        TextShapingLimits returned = options.GetLimits();
        returned.SetMaximumRunes(777);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetFeature(0), Is.SameAs(liga));
            Assert.That(options.GetLimits().GetMaximumRunes(),
                Is.EqualTo(123));
            Assert.That(options.GetLimits().GetMaximumLayoutOperations(),
                Is.EqualTo(456));
        });
    }

    [Test]
    public void PublicControlsDriveTheExistingShapingPipeline()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        TextShapingOptions options = new TextShapingOptions();
        options.SetDecodingMode(Utf16DecodingMode.Replacement);
        TextShapingResult replacement = TextShaper.Shape(
            "\uD800",
            instance,
            options);

        options.SetNormalizationMode(UnicodeNormalizationMode.None);
        TextShapingResult graphemes = TextShaper.Shape(
            "a\u0301",
            instance,
            options);
        options.SetClusterPolicy(TextShapingClusterPolicy.UnicodeScalars);
        TextShapingResult scalars = TextShaper.Shape(
            "a\u0301",
            instance,
            options);
        options.SetScript("Latn");
        TextShapingResult overridden = TextShaper.Shape(
            "\u05D0",
            instance,
            options);

        TextShapingLimits limits = options.GetLimits();
        limits.SetMaximumUtf16CodeUnits(1);
        options.SetLimits(limits);
        TextShapingOptions operationBounded = new TextShapingOptions();
        TextShapingLimits operationLimits = operationBounded.GetLimits();
        operationLimits.SetMaximumLayoutOperations(1);
        operationBounded.SetLimits(operationLimits);

        Assert.Multiple(() =>
        {
            Assert.That(replacement.GetSourceRuneCount(), Is.EqualTo(1));
            Assert.That(graphemes.GetGraphemeClusterCount(), Is.EqualTo(1));
            Assert.That(scalars.GetGraphemeClusterCount(), Is.EqualTo(2));
            Assert.That(overridden.GetRun(0).GetScript(), Is.EqualTo("Latn"));
            Assert.Throws<FontLimitException>(() =>
                TextShaper.Shape("AB", instance, options));
            Assert.Throws<FontLimitException>(() =>
                TextShaper.Shape("office", instance, operationBounded));
        });
    }

    [Test]
    public void RejectsInvalidOptionsAndNonpositiveLimits()
    {
        TextShapingOptions options = new TextShapingOptions();
        TextShapingLimits limits = new TextShapingLimits();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetDecodingMode((Utf16DecodingMode)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetNormalizationMode((UnicodeNormalizationMode)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetDirection((TextShapingDirection)9));
            Assert.Throws<ArgumentNullException>(() => options.SetScript(null!));
            Assert.Throws<ArgumentException>(() => options.SetScript("lat"));
            Assert.Throws<ArgumentException>(() => options.SetScript("la1n"));
            Assert.Throws<ArgumentException>(() =>
                options.SetLanguageTag("en--US"));
            Assert.Throws<ArgumentNullException>(() =>
                options.SetFeatures(null!));
            Assert.Throws<ArgumentException>(() => options.SetFeatures(
                new FontLayoutFeatureSetting[] { null! }));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetClusterPolicy((TextShapingClusterPolicy)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVerticalBehavior((TextShapingVerticalBehavior)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetHorizontalPixelsPerEm(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVerticalPixelsPerEm(65536));
            Assert.Throws<ArgumentNullException>(() => options.SetLimits(null!));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumUtf16CodeUnits(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumRunes(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumClusters(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumRuns(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumGlyphs(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                limits.SetMaximumLayoutOperations(0));
        });
    }

    static int Tag(string value)
    {
        return (int)TestSfntEncoding.EncodeTag(value);
    }
}
