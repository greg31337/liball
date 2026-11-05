using ttf;

namespace ttf.Tests;

internal class PdfFontOptionsTests
{
    [Test]
    public void EnumValuesAreStable()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)PdfFontKind.Automatic, Is.Zero);
            Assert.That((int)PdfFontKind.Type0CidFontType2, Is.EqualTo(1));
            Assert.That((int)PdfFontKind.Type0CidFontType0, Is.EqualTo(2));
            Assert.That((int)PdfFontKind.Type0OpenType, Is.EqualTo(3));
            Assert.That((int)PdfFontKind.SimpleTrueType, Is.EqualTo(4));
            Assert.That((int)PdfVersion.Pdf1_0, Is.Zero);
            Assert.That((int)PdfVersion.Pdf1_7, Is.EqualTo(7));
            Assert.That((int)PdfVersion.Pdf2_0, Is.EqualTo(8));
            Assert.That((int)PdfWritingMode.Horizontal, Is.Zero);
            Assert.That((int)PdfWritingMode.Vertical, Is.EqualTo(1));
            Assert.That((int)PdfFontEmbeddingMode.Subset, Is.Zero);
            Assert.That((int)PdfFontEmbeddingMode.FullFont, Is.EqualTo(1));
            Assert.That((int)PdfFontRightsPolicy.Reject, Is.Zero);
            Assert.That((int)PdfFontRightsPolicy.Report, Is.EqualTo(1));
            Assert.That((int)PdfFontRightsPolicy.EmbedFullFont, Is.EqualTo(2));
            Assert.That((int)PdfArchivalProfile.None, Is.Zero);
            Assert.That((int)PdfArchivalProfile.PdfA4, Is.EqualTo(4));
            Assert.That((int)PdfAccessibilityProfile.None, Is.Zero);
            Assert.That((int)PdfAccessibilityProfile.PdfUa2, Is.EqualTo(2));
            Assert.That((int)PdfCidAllocationMode.Dense, Is.Zero);
            Assert.That((int)PdfCidAllocationMode
                .PreserveSourceGlyphIdentifiers, Is.EqualTo(1));
            Assert.That((int)PdfSimpleFontPolicy.ExplicitOnly, Is.Zero);
            Assert.That((int)PdfSimpleFontPolicy.Disallow, Is.EqualTo(1));
            Assert.That((int)PdfStaticInstancePolicy
                .MaterializeSelectedInstance, Is.Zero);
            Assert.That((int)PdfStaticInstancePolicy.RequireStaticSource,
                Is.EqualTo(1));
            Assert.That((int)PdfVersionCompatibilityPolicy.Reject, Is.Zero);
            Assert.That((int)PdfVersionCompatibilityPolicy.Report,
                Is.EqualTo(1));
            Assert.That((int)PdfProfileCompatibilityPolicy.Report, Is.Zero);
            Assert.That((int)PdfProfileCompatibilityPolicy.Reject,
                Is.EqualTo(1));
        });
    }

    [Test]
    public void DefaultAndCompatibleProfilesUseType0SafeChoices()
    {
        PdfFontOptions defaults = PdfFontOptions.Create();
        PdfFontOptions compatible = PdfFontOptions.CreateCompatible();

        AssertCompatible(defaults);
        AssertCompatible(compatible);
        Assert.That(compatible, Is.Not.SameAs(defaults));
    }

    [Test]
    public void StrictProfileRejectsRightsConflicts()
    {
        PdfFontOptions options = PdfFontOptions.CreateStrict();

        Assert.Multiple(() =>
        {
            Assert.That(options.GetFontKind(),
                Is.EqualTo(PdfFontKind.Automatic));
            Assert.That(options.GetVersion(),
                Is.EqualTo(PdfVersion.Pdf1_7));
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(PdfFontEmbeddingMode.Subset));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(PdfFontRightsPolicy.Reject));
        });
    }

    [Test]
    public void FactoryAndSettersPreserveEveryChoice()
    {
        PdfFontOptions constructed = PdfFontOptions.Create(
            PdfFontKind.Type0OpenType,
            PdfVersion.Pdf2_0,
            PdfWritingMode.Vertical,
            PdfFontEmbeddingMode.FullFont,
            PdfFontRightsPolicy.Report,
            PdfArchivalProfile.PdfA4,
            PdfAccessibilityProfile.PdfUa2);
        PdfFontOptions assigned = PdfFontOptions.Create();
        assigned.SetFontKind(PdfFontKind.SimpleTrueType);
        assigned.SetVersion(PdfVersion.Pdf1_4);
        assigned.SetWritingMode(PdfWritingMode.Vertical);
        assigned.SetEmbeddingMode(PdfFontEmbeddingMode.FullFont);
        assigned.SetRightsPolicy(PdfFontRightsPolicy.Report);
        assigned.SetArchivalProfile(PdfArchivalProfile.PdfA1);
        assigned.SetAccessibilityProfile(PdfAccessibilityProfile.PdfUa1);
        assigned.SetCidAllocationMode(
            PdfCidAllocationMode.PreserveSourceGlyphIdentifiers);
        assigned.SetSubsetPrefixOverride("ABCDEF");
        assigned.SetHintPolicy(FontSubsetHintPolicy.Retain);
        assigned.SetLayoutPolicy(FontSubsetLayoutPolicy.Retain);
        assigned.SetVerticalPolicy(FontSubsetVerticalPolicy.Retain);
        assigned.SetColorPolicy(FontSubsetColorPolicy.Retain);
        assigned.SetMetadataPolicy(FontSubsetMetadataPolicy.Minimal);
        assigned.SetSimpleFontPolicy(PdfSimpleFontPolicy.Disallow);
        assigned.SetStaticInstancePolicy(
            PdfStaticInstancePolicy.RequireStaticSource);
        assigned.SetVersionCompatibilityPolicy(
            PdfVersionCompatibilityPolicy.Report);
        assigned.SetProfileCompatibilityPolicy(
            PdfProfileCompatibilityPolicy.Reject);

        Assert.Multiple(() =>
        {
            AssertOptions(
                constructed,
                PdfFontKind.Type0OpenType,
                PdfVersion.Pdf2_0,
                PdfArchivalProfile.PdfA4,
                PdfAccessibilityProfile.PdfUa2);
            AssertOptions(
                assigned,
                PdfFontKind.SimpleTrueType,
                PdfVersion.Pdf1_4,
                PdfArchivalProfile.PdfA1,
                PdfAccessibilityProfile.PdfUa1);
            Assert.That(assigned.GetCidAllocationMode(),
                Is.EqualTo(PdfCidAllocationMode
                    .PreserveSourceGlyphIdentifiers));
            Assert.That(assigned.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.That(assigned.GetHintPolicy(),
                Is.EqualTo(FontSubsetHintPolicy.Retain));
            Assert.That(assigned.GetLayoutPolicy(),
                Is.EqualTo(FontSubsetLayoutPolicy.Retain));
            Assert.That(assigned.GetVerticalPolicy(),
                Is.EqualTo(FontSubsetVerticalPolicy.Retain));
            Assert.That(assigned.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
            Assert.That(assigned.GetMetadataPolicy(),
                Is.EqualTo(FontSubsetMetadataPolicy.Minimal));
            Assert.That(assigned.GetSimpleFontPolicy(),
                Is.EqualTo(PdfSimpleFontPolicy.Disallow));
            Assert.That(assigned.GetStaticInstancePolicy(),
                Is.EqualTo(PdfStaticInstancePolicy.RequireStaticSource));
            Assert.That(assigned.GetVersionCompatibilityPolicy(),
                Is.EqualTo(PdfVersionCompatibilityPolicy.Report));
            Assert.That(assigned.GetProfileCompatibilityPolicy(),
                Is.EqualTo(PdfProfileCompatibilityPolicy.Reject));
        });
    }

    [Test]
    public void CopyIsAnIndependentSnapshot()
    {
        PdfFontOptions source = PdfFontOptions.Create();
        source.SetWritingMode(PdfWritingMode.Vertical);
        source.SetArchivalProfile(PdfArchivalProfile.PdfA2);
        source.SetSubsetPrefixOverride("ZYXWVU");
        source.SetColorPolicy(FontSubsetColorPolicy.Retain);
        PdfFontOptions copy = source.Copy();

        source.SetWritingMode(PdfWritingMode.Horizontal);
        source.SetArchivalProfile(PdfArchivalProfile.None);
        source.SetSubsetPrefixOverride(null);
        source.SetColorPolicy(FontSubsetColorPolicy.Drop);

        Assert.Multiple(() =>
        {
            Assert.That(copy, Is.Not.SameAs(source));
            Assert.That(copy.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Vertical));
            Assert.That(copy.GetArchivalProfile(),
                Is.EqualTo(PdfArchivalProfile.PdfA2));
            Assert.That(copy.GetSubsetPrefixOverride(),
                Is.EqualTo("ZYXWVU"));
            Assert.That(copy.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
        });
    }

    [Test]
    public void RejectsUndefinedValuesWithoutChangingOptions()
    {
        PdfFontOptions options = PdfFontOptions.Create();

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetFontKind((PdfFontKind)(-1)));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVersion((PdfVersion)9));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetWritingMode((PdfWritingMode)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetEmbeddingMode((PdfFontEmbeddingMode)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetRightsPolicy((PdfFontRightsPolicy)3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetArchivalProfile((PdfArchivalProfile)5));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetAccessibilityProfile(
                    (PdfAccessibilityProfile)3));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetCidAllocationMode((PdfCidAllocationMode)2));
            Assert.Throws<ArgumentException>(() =>
                options.SetSubsetPrefixOverride("abc"));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetHintPolicy((FontSubsetHintPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetLayoutPolicy((FontSubsetLayoutPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVerticalPolicy((FontSubsetVerticalPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetColorPolicy((FontSubsetColorPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetMetadataPolicy((FontSubsetMetadataPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetSimpleFontPolicy((PdfSimpleFontPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetStaticInstancePolicy((PdfStaticInstancePolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetVersionCompatibilityPolicy(
                    (PdfVersionCompatibilityPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                options.SetProfileCompatibilityPolicy(
                    (PdfProfileCompatibilityPolicy)2));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                PdfFontOptions.Create(
                    PdfFontKind.Automatic,
                    PdfVersion.Pdf1_7,
                    PdfWritingMode.Horizontal,
                    PdfFontEmbeddingMode.Subset,
                    PdfFontRightsPolicy.Reject,
                    (PdfArchivalProfile)(-1),
                    PdfAccessibilityProfile.None));
        });

        AssertCompatible(options);
    }

    static void AssertCompatible(PdfFontOptions options)
    {
        Assert.Multiple(() =>
        {
            Assert.That(options.GetFontKind(),
                Is.EqualTo(PdfFontKind.Automatic));
            Assert.That(options.GetVersion(),
                Is.EqualTo(PdfVersion.Pdf1_7));
            Assert.That(options.GetWritingMode(),
                Is.EqualTo(PdfWritingMode.Horizontal));
            Assert.That(options.GetEmbeddingMode(),
                Is.EqualTo(PdfFontEmbeddingMode.Subset));
            Assert.That(options.GetRightsPolicy(),
                Is.EqualTo(PdfFontRightsPolicy.EmbedFullFont));
            Assert.That(options.GetArchivalProfile(),
                Is.EqualTo(PdfArchivalProfile.None));
            Assert.That(options.GetAccessibilityProfile(),
                Is.EqualTo(PdfAccessibilityProfile.None));
            Assert.That(options.GetCidAllocationMode(),
                Is.EqualTo(PdfCidAllocationMode.Dense));
            Assert.That(options.GetSubsetPrefixOverride(), Is.Null);
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
            Assert.That(options.GetSimpleFontPolicy(),
                Is.EqualTo(PdfSimpleFontPolicy.ExplicitOnly));
            Assert.That(options.GetStaticInstancePolicy(),
                Is.EqualTo(PdfStaticInstancePolicy
                    .MaterializeSelectedInstance));
            Assert.That(options.GetVersionCompatibilityPolicy(),
                Is.EqualTo(PdfVersionCompatibilityPolicy.Reject));
            Assert.That(options.GetProfileCompatibilityPolicy(),
                Is.EqualTo(PdfProfileCompatibilityPolicy.Report));
        });
    }

    static void AssertOptions(
        PdfFontOptions options,
        PdfFontKind fontKind,
        PdfVersion version,
        PdfArchivalProfile archivalProfile,
        PdfAccessibilityProfile accessibilityProfile)
    {
        Assert.That(options.GetFontKind(), Is.EqualTo(fontKind));
        Assert.That(options.GetVersion(), Is.EqualTo(version));
        Assert.That(options.GetWritingMode(),
            Is.EqualTo(PdfWritingMode.Vertical));
        Assert.That(options.GetEmbeddingMode(),
            Is.EqualTo(PdfFontEmbeddingMode.FullFont));
        Assert.That(options.GetRightsPolicy(),
            Is.EqualTo(PdfFontRightsPolicy.Report));
        Assert.That(options.GetArchivalProfile(),
            Is.EqualTo(archivalProfile));
        Assert.That(options.GetAccessibilityProfile(),
            Is.EqualTo(accessibilityProfile));
    }
}
