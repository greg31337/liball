using ttf;

namespace ttf.Tests;

internal class PdfNameTests
{
    [Test]
    public void EscapesPdfDelimitersWhitespaceAndUtf8Bytes()
    {
        PdfName name = new PdfName("A B/#()<>[]{}%\tÅ😀");

        Assert.Multiple(() =>
        {
            Assert.That(name.GetValue(), Is.EqualTo(
                "A B/#()<>[]{}%\tÅ😀"));
            Assert.That(name.GetEscapedValue(), Is.EqualTo(
                "A#20B#2F#23#28#29#3C#3E#5B#5D#7B#7D#25#09#C3#85#F0#9F#98#80"));
            Assert.That(name.GetToken(), Is.EqualTo(
                "/A#20B#2F#23#28#29#3C#3E#5B#5D#7B#7D#25#09#C3#85#F0#9F#98#80"));
            Assert.That(name.GetByteLength(), Is.EqualTo(21));
        });
    }

    [Test]
    public void ValidatesUnicodeNullAndDecodedByteLength()
    {
        string maximumAscii = new string('A', 127);
        string maximumMultibyte = new string('é', 63);

        Assert.Multiple(() =>
        {
            Assert.That(PdfName.IsValid("Regular-Name"), Is.True);
            Assert.That(PdfName.IsValid(maximumAscii), Is.True);
            Assert.That(PdfName.IsValid(maximumMultibyte), Is.True);
            Assert.That(PdfName.IsValid(new string('A', 128)), Is.False);
            Assert.That(PdfName.IsValid(new string('é', 64)), Is.False);
            Assert.That(PdfName.IsValid(null!), Is.False);
            Assert.That(PdfName.IsValid(""), Is.False);
            Assert.That(PdfName.IsValid("A\0B"), Is.False);
            Assert.That(PdfName.IsValid("\uD800"), Is.False);
            Assert.That(PdfName.IsValid("\uDC00"), Is.False);
            Assert.That(PdfName.IsValid("\uD800A"), Is.False);
            Assert.Throws<ArgumentNullException>(() => new PdfName(null!));
            Assert.Throws<ArgumentException>(() => new PdfName(""));
            Assert.Throws<ArgumentException>(() => new PdfName("A\0B"));
            Assert.Throws<ArgumentException>(() =>
                new PdfName(new string('A', 128)));
        });
    }

    [Test]
    public void SharesOneEscapedNameAcrossEveryFontConsumer()
    {
        PdfFontResourceNames names = PdfFontResourceNamesBuilder.Build(
            "ABCDEF+Hash#Font");
        PdfName descriptor = names.GetDescriptorFontName();

        Assert.Multiple(() =>
        {
            Assert.That(names.GetBaseFontName(),
                Is.EqualTo("ABCDEF+Hash#Font"));
            Assert.That(descriptor.GetToken(),
                Is.EqualTo("/ABCDEF+Hash#23Font"));
            Assert.That(names.GetCompositeBaseFontName(),
                Is.SameAs(descriptor));
            Assert.That(names.GetDescendantBaseFontName(),
                Is.SameAs(descriptor));
            Assert.That(names.GetEmbeddedProgramName(),
                Is.EqualTo(descriptor.GetValue()));
            Assert.Throws<ArgumentNullException>(() =>
                PdfFontResourceNamesBuilder.Build(null!));
            Assert.Throws<FontFormatException>(() =>
                PdfFontResourceNamesBuilder.Build("Bad Name"));
        });
    }

    [Test]
    public void FontArtifactsExposeOneDeterministicEmbeddedBaseName()
    {
        FontInstance trueType = HarfBuzzLayoutDifferentialTests.Load()
            .CreateDefaultInstance();
        PdfFontUsage type2Usage = CreateUsage(
            trueType,
            PdfFontOptions.Create());
        type2Usage.AddGlyph(2, new int[] { 0x41 });
        PdfCidFontType2Artifact type2 = PdfCidFontType2Builder.Build(
            type2Usage,
            PdfCidAllocator.Create(type2Usage));
        PdfCidFontType2Artifact repeatedType2 = PdfCidFontType2Builder.Build(
            type2Usage,
            PdfCidAllocator.Create(type2Usage));
        FontFace reopenedType2 = FontLoader.Load(
            type2.CopyFontFile2(),
            FontLoadOptions.CreateStrict()).GetFace(0);

        FontInstance cff = FontInstanceCffTests.CreateFace(
            FontOutlineKind.Cff1,
            FontTags.Cff,
            CffCharsetEncodingTests.BuildCidCff(false),
            4).CreateDefaultInstance();
        PdfFontUsage type0Usage = CreateUsage(cff, PdfFontOptions.Create());
        PdfCidFontType0Artifact type0 = PdfCidFontType0Builder.Build(
            type0Usage,
            PdfCidAllocator.Create(type0Usage));

        PdfFontOptions openTypeOptions = PdfFontOptions.Create();
        openTypeOptions.SetFontKind(PdfFontKind.Type0OpenType);
        openTypeOptions.SetVersion(PdfVersion.Pdf1_6);
        PdfFontUsage openTypeUsage = CreateUsage(
            trueType,
            openTypeOptions);
        openTypeUsage.AddGlyph(2, new int[] { 0x41 });
        PdfOpenTypeFontArtifact openType = PdfOpenTypeFontBuilder.Build(
            openTypeUsage,
            PdfCidAllocator.Create(openTypeUsage));
        FontFace reopenedOpenType = FontLoader.Load(
            openType.CopyFontFile3(),
            FontLoadOptions.CreateStrict()).GetFace(0);

        AssertArtifactNames(type2.GetBaseFontName(),
            type2.GetResourceNames());
        AssertArtifactNames(type0.GetBaseFontName(),
            type0.GetResourceNames());
        AssertArtifactNames(openType.GetBaseFontName(),
            openType.GetResourceNames());

        Assert.Multiple(() =>
        {
            Assert.That(repeatedType2.GetBaseFontName(),
                Is.EqualTo(type2.GetBaseFontName()));
            Assert.That(reopenedType2.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(type2.GetResourceNames()
                    .GetEmbeddedProgramName()));
            Assert.That(reopenedOpenType.GetNames().GetPdfPostScriptName(),
                Is.EqualTo(openType.GetResourceNames()
                    .GetEmbeddedProgramName()));
            Assert.That(GetCffProgramName(type0.CopyFontFile3()),
                Is.EqualTo(type0.GetResourceNames()
                    .GetEmbeddedProgramName()));
        });
    }

    static void AssertArtifactNames(
        string artifactBaseName,
        PdfFontResourceNames names)
    {
        PdfName descriptor = names.GetDescriptorFontName();

        Assert.Multiple(() =>
        {
            Assert.That(artifactBaseName,
                Is.EqualTo(names.GetBaseFontName()));
            Assert.That(names.GetCompositeBaseFontName(),
                Is.SameAs(descriptor));
            Assert.That(names.GetDescendantBaseFontName(),
                Is.SameAs(descriptor));
            Assert.That(names.GetEmbeddedProgramName(),
                Is.EqualTo(descriptor.GetValue()));
            Assert.That(descriptor.GetToken(),
                Does.StartWith("/"));
            Assert.That(descriptor.GetValue(),
                Does.Match("^[A-Z]{6}\\+"));
        });
    }

    static PdfFontUsage CreateUsage(
        FontInstance instance,
        PdfFontOptions options)
    {
        return new PdfFontUsageAccumulator().GetOrAdd(instance, options);
    }

    static string GetCffProgramName(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        SfntTableData table = new SfntTableData(
            FontTags.Cff,
            FontChecksum.CalculateTable(bytes),
            data.GetSlice(0, data.GetLength()),
            SfntTableClassification.Known,
            true);
        SfntTableCache cache = SfntTableCache.Create(
            new SfntTableStore(
                new SfntTableData[] { table },
                Array.Empty<FontDiagnostic>()),
            FontLoadMode.Compatible,
            FontLimits.CreateCompatible());
        CffIndex names = SfntCffTableParser.FindTable(cache)!.GetNameIndex();
        return System.Text.Encoding.ASCII.GetString(names.CopyObject(0));
    }
}
