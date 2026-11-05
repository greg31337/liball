using ttf;

namespace ttf.Tests;

internal class FontSubsetOptionsTests
{
    [Test]
    public void PolicyEnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FontSubsetEmbeddingMode.Subset, Is.Zero);
            Assert.That((int)FontSubsetEmbeddingMode.FullFont, Is.EqualTo(1));
            Assert.That((int)FontSubsetGlyphRetentionMode.Compact, Is.Zero);
            Assert.That((int)FontSubsetGlyphRetentionMode.Preserve, Is.EqualTo(1));
            Assert.That((int)FontSubsetRecommendedGlyphPolicy.RequestedOnly,
                Is.Zero);
            Assert.That(
                (int)FontSubsetRecommendedGlyphPolicy.IncludeRecommended,
                Is.EqualTo(1));
            Assert.That((int)FontSubsetHintPolicy.Drop, Is.Zero);
            Assert.That((int)FontSubsetHintPolicy.Retain, Is.EqualTo(1));
            Assert.That((int)FontSubsetLayoutPolicy.Drop, Is.Zero);
            Assert.That((int)FontSubsetLayoutPolicy.Retain, Is.EqualTo(1));
            Assert.That((int)FontSubsetVerticalPolicy.Drop, Is.Zero);
            Assert.That((int)FontSubsetVerticalPolicy.Retain, Is.EqualTo(1));
            Assert.That((int)FontSubsetColorPolicy.Drop, Is.Zero);
            Assert.That((int)FontSubsetColorPolicy.Retain, Is.EqualTo(1));
            Assert.That((int)FontSubsetMetadataPolicy.Minimal, Is.Zero);
            Assert.That((int)FontSubsetMetadataPolicy.RetainSafe, Is.EqualTo(1));
            Assert.That((int)FontSubsetUnknownTablePolicy.Drop, Is.Zero);
            Assert.That((int)FontSubsetUnknownTablePolicy.RetainSafe, Is.EqualTo(1));
            Assert.That((int)FontSubsetRightsPolicy.Reject, Is.Zero);
            Assert.That((int)FontSubsetRightsPolicy.Report, Is.EqualTo(1));
            Assert.That((int)FontSubsetRightsPolicy.EmbedFullFont, Is.EqualTo(2));
        });
    }

    [Test]
    public void DefaultAndCompatibleProfilesUsePdfOrientedSafeChoices()
    {
        FontSubsetOptions defaults = FontSubsetOptions.Create();
        FontSubsetOptions compatible = FontSubsetOptions.CreateCompatible();

        AssertCompatibleProfile(defaults);
        AssertCompatibleProfile(compatible);
        Assert.That(compatible, Is.Not.SameAs(defaults));
    }

    [Test]
    public void StrictProfileRejectsRightsConflictsAndUsesMinimalMetadata()
    {
        FontSubsetOptions options = FontSubsetOptions.CreateStrict();

        Assert.Multiple(() =>
        {
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(options.GetGlyphRetentionMode(),
                Is.EqualTo(FontSubsetGlyphRetentionMode.Compact));
            Assert.That(options.GetRecommendedGlyphPolicy(),
                Is.EqualTo(FontSubsetRecommendedGlyphPolicy.RequestedOnly));
            Assert.That(options.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(options.GetUnknownTablePolicy(),
                Is.EqualTo(FontSubsetUnknownTablePolicy.Drop));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.Reject));
        });
    }

    [Test]
    public void ConfiguredFactoryPreservesEveryChoice()
    {
        FontSubsetOptions options = FontSubsetOptions.Create(
            FontSubsetEmbeddingMode.FullFont,
            FontSubsetGlyphRetentionMode.Preserve,
            FontSubsetRecommendedGlyphPolicy.RequestedOnly,
            FontSubsetHintPolicy.Retain,
            FontSubsetLayoutPolicy.Retain,
            FontSubsetVerticalPolicy.Retain,
            FontSubsetColorPolicy.Retain,
            FontSubsetMetadataPolicy.Minimal,
            FontSubsetUnknownTablePolicy.RetainSafe,
            FontSubsetRightsPolicy.Report);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(options.GetGlyphRetentionMode(),
                Is.EqualTo(FontSubsetGlyphRetentionMode.Preserve));
            Assert.That(options.GetRecommendedGlyphPolicy(),
                Is.EqualTo(FontSubsetRecommendedGlyphPolicy.RequestedOnly));
            Assert.That(options.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Retain));
            Assert.That(options.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Retain));
            Assert.That(options.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Retain));
            Assert.That(options.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
            Assert.That(options.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(options.GetUnknownTablePolicy(),
                Is.EqualTo(FontSubsetUnknownTablePolicy.RetainSafe));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.Report));
        });
    }

    [Test]
    public void SettersReplaceEveryChoice()
    {
        FontSubsetOptions options = FontSubsetOptions.Create();
        options.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Preserve);
        options.SetRecommendedGlyphPolicy(
            FontSubsetRecommendedGlyphPolicy.RequestedOnly);
        options.SetHintPolicy(FontSubsetHintPolicy.Retain);
        options.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        options.SetVerticalPolicy(FontSubsetVerticalPolicy.Retain);
        options.SetColorPolicy(FontSubsetColorPolicy.Retain);
        options.SetMetadataPolicy(FontSubsetMetadataPolicy.Minimal);
        options.SetUnknownTablePolicy(FontSubsetUnknownTablePolicy.RetainSafe);
        options.SetRightsPolicy(FontSubsetRightsPolicy.Report);
        options.SetSubsetPrefixOverride("ABCDEF");
        options.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
            Assert.That(options.GetGlyphRetentionMode(),
                Is.EqualTo(FontSubsetGlyphRetentionMode.Preserve));
            Assert.That(options.GetRecommendedGlyphPolicy(),
                Is.EqualTo(FontSubsetRecommendedGlyphPolicy.RequestedOnly));
            Assert.That(options.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Retain));
            Assert.That(options.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Retain));
            Assert.That(options.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Retain));
            Assert.That(options.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
            Assert.That(options.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(options.GetUnknownTablePolicy(),
                Is.EqualTo(FontSubsetUnknownTablePolicy.RetainSafe));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.Report));
            Assert.That(options.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
        });
    }

    [Test]
    public void ValidatesAndClearsSubsetPrefixOverride()
    {
        FontSubsetOptions options = FontSubsetOptions.Create();

        Assert.That(options.GetSubsetPrefixOverride(), Is.Null);
        options.SetSubsetPrefixOverride("ABCDEF");
        Assert.That(options.GetSubsetPrefixOverride(), Is.EqualTo("ABCDEF"));
        options.SetSubsetPrefixOverride(null);

        Assert.Multiple(() =>
        {
            Assert.That(options.GetSubsetPrefixOverride(), Is.Null);
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride(""));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("ABCDE"));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("ABCDEFG"));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("abcDEF"));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("ABCDE1"));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("ÅBCDEF"));
            Assert.That(options.GetSubsetPrefixOverride(), Is.Null);
        });
    }

    [Test]
    public void RejectsUnknownPolicyValuesInFactoryAndSetters()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                embeddingMode: (FontSubsetEmbeddingMode)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                glyphMode: (FontSubsetGlyphRetentionMode)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                recommendedPolicy: (FontSubsetRecommendedGlyphPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                hintPolicy: (FontSubsetHintPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                layoutPolicy: (FontSubsetLayoutPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                verticalPolicy: (FontSubsetVerticalPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                colorPolicy: (FontSubsetColorPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                metadataPolicy: (FontSubsetMetadataPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                unknownPolicy: (FontSubsetUnknownTablePolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() => Create(
                rightsPolicy: (FontSubsetRightsPolicy)3));

            FontSubsetOptions options = FontSubsetOptions.Create();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetEmbeddingMode((FontSubsetEmbeddingMode)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetGlyphRetentionMode((FontSubsetGlyphRetentionMode)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetRecommendedGlyphPolicy(
                    (FontSubsetRecommendedGlyphPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetHintPolicy((FontSubsetHintPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetLayoutPolicy((FontSubsetLayoutPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVerticalPolicy((FontSubsetVerticalPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetColorPolicy((FontSubsetColorPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMetadataPolicy((FontSubsetMetadataPolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetUnknownTablePolicy((FontSubsetUnknownTablePolicy)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetRightsPolicy((FontSubsetRightsPolicy)(-1)));
        });
    }

    [Test]
    public void UnknownTableRetentionRequiresStableGlyphIdentifiersForSubsets()
    {
        Assert.Throws<ArgumentException>(() => Create(
            unknownPolicy: FontSubsetUnknownTablePolicy.RetainSafe));
        FontSubsetOptions subset = FontSubsetOptions.Create();
        subset.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Preserve);
        subset.SetUnknownTablePolicy(FontSubsetUnknownTablePolicy.RetainSafe);

        Assert.Throws<ArgumentException>(() =>
            subset.SetGlyphRetentionMode(FontSubsetGlyphRetentionMode.Compact));
        Assert.That(subset.GetGlyphRetentionMode(),
            Is.EqualTo(FontSubsetGlyphRetentionMode.Preserve));

        FontSubsetOptions full = FontSubsetOptions.Create();
        full.SetEmbeddingMode(FontSubsetEmbeddingMode.FullFont);
        full.SetUnknownTablePolicy(FontSubsetUnknownTablePolicy.RetainSafe);

        Assert.DoesNotThrow(() => full.SetGlyphRetentionMode(
            FontSubsetGlyphRetentionMode.Compact));
        Assert.Throws<ArgumentException>(() =>
            full.SetEmbeddingMode(FontSubsetEmbeddingMode.Subset));
        Assert.That(full.GetEmbeddingMode(),
            Is.EqualTo(FontSubsetEmbeddingMode.FullFont));
    }

    static FontSubsetOptions Create(
        FontSubsetEmbeddingMode embeddingMode = FontSubsetEmbeddingMode.Subset,
        FontSubsetGlyphRetentionMode glyphMode = FontSubsetGlyphRetentionMode.Compact,
        FontSubsetRecommendedGlyphPolicy recommendedPolicy =
            FontSubsetRecommendedGlyphPolicy.IncludeRecommended,
        FontSubsetHintPolicy hintPolicy = FontSubsetHintPolicy.Drop,
        FontSubsetLayoutPolicy layoutPolicy = FontSubsetLayoutPolicy.Drop,
        FontSubsetVerticalPolicy verticalPolicy = FontSubsetVerticalPolicy.Drop,
        FontSubsetColorPolicy colorPolicy = FontSubsetColorPolicy.Drop,
        FontSubsetMetadataPolicy metadataPolicy = FontSubsetMetadataPolicy.RetainSafe,
        FontSubsetUnknownTablePolicy unknownPolicy = FontSubsetUnknownTablePolicy.Drop,
        FontSubsetRightsPolicy rightsPolicy = FontSubsetRightsPolicy.EmbedFullFont)
    {
        return FontSubsetOptions.Create(
            embeddingMode,
            glyphMode,
            recommendedPolicy,
            hintPolicy,
            layoutPolicy,
            verticalPolicy,
            colorPolicy,
            metadataPolicy,
            unknownPolicy,
            rightsPolicy);
    }

    static void AssertCompatibleProfile(FontSubsetOptions options)
    {
        Assert.Multiple(() =>
        {
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(FontSubsetEmbeddingMode.Subset));
            Assert.That(options.GetGlyphRetentionMode(),
                Is.EqualTo(FontSubsetGlyphRetentionMode.Compact));
            Assert.That(options.GetRecommendedGlyphPolicy(), Is.EqualTo(
                FontSubsetRecommendedGlyphPolicy.IncludeRecommended));
            Assert.That(options.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Drop));
            Assert.That(options.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Drop));
            Assert.That(options.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Drop));
            Assert.That(options.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Drop));
            Assert.That(options.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.RetainSafe));
            Assert.That(options.GetUnknownTablePolicy(),
                Is.EqualTo(FontSubsetUnknownTablePolicy.Drop));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(FontSubsetRightsPolicy.EmbedFullFont));
            Assert.That(options.GetSubsetPrefixOverride(), Is.Null);
        });
    }
}
