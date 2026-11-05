namespace png.Tests;

[TestFixture]
public class TextModelConversionTests
{
    [Test]
    public void Converter_PreservesEveryTextFamilyAndOriginalOrderingFields()
    {
        PngText latin1 = new PngText();
        latin1.Keyword = "Title";
        latin1.Text = "Café";
        PngCompressedText compressed = new PngCompressedText();
        compressed.Keyword = "Comment";
        compressed.CompressionMethod = 0;
        compressed.Text = "Compressed text";
        compressed.CompressedText = new byte[] { 1, 2, 3 };
        PngInternationalText international = new PngInternationalText();
        international.Keyword = "Description";
        international.CompressionFlag = 1;
        international.CompressionMethod = 0;
        international.LanguageTag = "ja";
        international.TranslatedKeyword = "説明";
        international.Text = "こんにちは";
        international.CompressedText = new byte[] { 4, 5, 6 };

        PngTextEntry first = PngTextModelConverter.CreateLatin1Entry(latin1, 2);
        PngTextEntry second = PngTextModelConverter.CreateInternationalEntry(international, 5);
        PngTextEntry third = PngTextModelConverter.CreateCompressedLatin1Entry(compressed, 8);
        PngMetadata metadata = new PngMetadata();
        metadata.TextEntries = new PngTextEntry[] { first, second, third };
        compressed.CompressedText[0] = 99;
        international.CompressedText[0] = 99;

        AssertEntry(first, PngTextKind.Latin1, 2, "Title", "Café", 0, 0, "", "", new byte[0]);
        AssertEntry(second, PngTextKind.InternationalUtf8, 5, "Description", "こんにちは", 1, 0, "ja", "説明", new byte[] { 4, 5, 6 });
        AssertEntry(third, PngTextKind.CompressedLatin1, 8, "Comment", "Compressed text", 1, 0, "", "", new byte[] { 1, 2, 3 });
        Assert.That(metadata.TextEntries.Select(value => value.OriginalChunkIndex), Is.EqualTo(new int[] { 2, 5, 8 }));
    }

    [Test]
    public void TextEntry_DefaultsDescribeNewUncompressedLatin1Text()
    {
        PngTextEntry entry = new PngTextEntry();

        Assert.That(entry.Kind, Is.EqualTo(PngTextKind.Latin1));
        Assert.That(entry.OriginalChunkIndex, Is.EqualTo(-1));
        Assert.That(entry.Keyword, Is.EqualTo(""));
        Assert.That(entry.CompressionFlag, Is.EqualTo(0));
        Assert.That(entry.CompressionMethod, Is.EqualTo(0));
        Assert.That(entry.LanguageTag, Is.EqualTo(""));
        Assert.That(entry.TranslatedKeyword, Is.EqualTo(""));
        Assert.That(entry.Text, Is.EqualTo(""));
        Assert.That(entry.CompressedText, Is.Not.Null);
        Assert.That(entry.CompressedText, Is.Empty);
    }

    [Test]
    public void Converter_RejectsMissingModelsAndInvalidOriginalIndices()
    {
        PngCompressedText nullCompressedBytes = new PngCompressedText();
        nullCompressedBytes.CompressedText = null!;
        PngInternationalText nullInternationalBytes = new PngInternationalText();
        nullInternationalBytes.CompressedText = null!;

        Assert.Throws<ArgumentNullException>(() => PngTextModelConverter.CreateLatin1Entry(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngTextModelConverter.CreateCompressedLatin1Entry(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngTextModelConverter.CreateInternationalEntry(null!, 0));
        Assert.Throws<ArgumentNullException>(() => PngTextModelConverter.CreateCompressedLatin1Entry(nullCompressedBytes, 0));
        Assert.Throws<ArgumentNullException>(() => PngTextModelConverter.CreateInternationalEntry(nullInternationalBytes, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextModelConverter.CreateLatin1Entry(new PngText(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextModelConverter.CreateCompressedLatin1Entry(new PngCompressedText(), -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PngTextModelConverter.CreateInternationalEntry(new PngInternationalText(), -1));
    }

    static void AssertEntry(PngTextEntry entry, PngTextKind kind, int originalChunkIndex, string keyword, string text, byte compressionFlag, byte compressionMethod, string languageTag, string translatedKeyword, byte[] compressedText)
    {
        Assert.That(entry.Kind, Is.EqualTo(kind));
        Assert.That(entry.OriginalChunkIndex, Is.EqualTo(originalChunkIndex));
        Assert.That(entry.Keyword, Is.EqualTo(keyword));
        Assert.That(entry.Text, Is.EqualTo(text));
        Assert.That(entry.CompressionFlag, Is.EqualTo(compressionFlag));
        Assert.That(entry.CompressionMethod, Is.EqualTo(compressionMethod));
        Assert.That(entry.LanguageTag, Is.EqualTo(languageTag));
        Assert.That(entry.TranslatedKeyword, Is.EqualTo(translatedKeyword));
        Assert.That(entry.CompressedText, Is.EqualTo(compressedText));
    }
}
