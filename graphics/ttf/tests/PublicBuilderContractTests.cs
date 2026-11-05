using ttf;

namespace ttf.Tests;

internal class PublicBuilderContractTests
{
    [Test]
    public void SubsetBuilderExposesItsSourceAndDefensiveOptionSnapshot()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        FontSubsetOptions options = new FontSubsetOptions();
        options.SetColorPolicy(FontSubsetColorPolicy.Retain);
        options.SetSubsetPrefixOverride("ABCDEF");
        FontSubsetBuilder builder = new FontSubsetBuilder(instance, options);

        options.SetColorPolicy(FontSubsetColorPolicy.Drop);
        options.SetSubsetPrefixOverride("UVWXYZ");
        FontSubsetOptions first = builder.GetEffectiveOptions();
        first.SetColorPolicy(FontSubsetColorPolicy.Drop);
        first.SetSubsetPrefixOverride("GHIJKL");
        builder.Build();
        FontSubsetOptions second = builder.GetEffectiveOptions();

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetSourceInstance(), Is.SameAs(instance));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(second.GetColorPolicy(),
                Is.EqualTo(FontSubsetColorPolicy.Retain));
            Assert.That(second.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
        });
    }

    [Test]
    public void PdfBuilderExposesItsSourceAndDefensiveOptionSnapshot()
    {
        FontInstance instance = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontOptions options = new PdfFontOptions();
        options.SetFontKind(PdfFontKind.Type0CidFontType2);
        options.SetSubsetPrefixOverride("ABCDEF");
        PdfFontBuilder builder = new PdfFontBuilder(instance, options);

        options.SetFontKind(PdfFontKind.Type0OpenType);
        options.SetSubsetPrefixOverride("UVWXYZ");
        PdfFontOptions first = builder.GetFontOptions();
        first.SetFontKind(PdfFontKind.SimpleTrueType);
        first.SetSubsetPrefixOverride("GHIJKL");
        builder.AddGlyph(13, new int[] { 0x43 });
        PdfFontResource resource = builder.FinalizeFont();
        PdfFontOptions second = builder.GetFontOptions();

        Assert.Multiple(() =>
        {
            Assert.That(builder.GetSourceInstance(), Is.SameAs(instance));
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(second.GetFontKind(),
                Is.EqualTo(PdfFontKind.Type0CidFontType2));
            Assert.That(second.GetSubsetPrefixOverride(),
                Is.EqualTo("ABCDEF"));
            Assert.That(resource.GetFontOptions().GetFontKind(),
                Is.EqualTo(second.GetFontKind()));
        });
    }
}
