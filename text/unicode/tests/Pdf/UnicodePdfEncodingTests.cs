using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class UnicodePdfEncodingTests
{
    [Test]
    public void EncodeTextString_SelectsPdfDocEncodingWhenAllScalarsFit()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString("A\u20AC\uFB02");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x41, 0xA0, 0x94 }));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("A\u20AC\uFB02"));
        Assert.That(new UnicodePdfEncoding().GetTextStringEncoding("A\u20AC\uFB02"), Is.EqualTo(UnicodePdfTextEncoding.PdfDocEncoding));
    }

    [Test]
    public void EncodeTextString_FallsBackToUtf16BeWithBomWhenPdfDocEncodingCannotRepresentText()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString("A\u0100");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x01, 0x00 }));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("A\u0100"));
        Assert.That(new UnicodePdfEncoding().GetTextStringEncoding("A\u0100"), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
    }

    [Test]
    public void EncodeTextString_FallsBackToUtf16BeWithBomForInvalidUtf16Input()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeTextString("\uD800A");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFE, 0xFF, 0xFF, 0xFD, 0x00, 0x41 }));
        Assert.That(new UnicodePdfEncoding().GetTextStringEncoding("\uD800A"), Is.EqualTo(UnicodePdfTextEncoding.Utf16BeWithBom));
    }

    [Test]
    public void EncodeUtf16BeWithBom_PrependsBomAndPreservesSupplementaryScalars()
    {
        byte[] bytes = new UnicodePdfEncoding().EncodeUtf16BeWithBom("A\U0001F600");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00 }));
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("A\U0001F600"));
    }

    [Test]
    public void EncodeUtf16BePayload_WritesBigEndianCodeUnitsWithoutBom()
    {
        byte[] bytes = UnicodePdfEncoding.EncodeUtf16BePayload("A\U0001F600");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x00, 0x41, 0xD8, 0x3D, 0xDE, 0x00 }));
    }

    [Test]
    public void EncodeUtf16BePayload_ReplacesInvalidUtf16BeforeWriting()
    {
        byte[] bytes = UnicodePdfEncoding.EncodeUtf16BePayload("\uD800A");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0xFF, 0xFD, 0x00, 0x41 }));
    }

    [Test]
    public void DecodeUtf16BePayload_ReplacesOddTrailingByteAndInvalidSurrogates()
    {
        Assert.That(UnicodePdfEncoding.DecodeUtf16BePayload(new byte[] { 0x00, 0x41, 0x00 }, 0), Is.EqualTo("A\uFFFD"));
        Assert.That(UnicodePdfEncoding.DecodeUtf16BePayload(new byte[] { 0xD8, 0x00, 0x00, 0x41 }, 0), Is.EqualTo("\uFFFDA"));
    }

    [Test]
    public void Utf16BeCodeUnitHelpers_ReadAndWriteBigEndianCodeUnits()
    {
        byte[] bytes = new byte[] { 0x00, 0x00, 0x00, 0x00 };

        UnicodePdfEncoding.WriteUtf16BeCodeUnit(bytes, 0, 0x0041);
        UnicodePdfEncoding.WriteUtf16BeCodeUnit(bytes, 2, 0xD83D);

        Assert.That(bytes, Is.EqualTo(new byte[] { 0x00, 0x41, 0xD8, 0x3D }));
        Assert.That(UnicodePdfEncoding.ReadUtf16BeCodeUnit(bytes, 0), Is.EqualTo(0x0041));
        Assert.That(UnicodePdfEncoding.ReadUtf16BeCodeUnit(bytes, 2), Is.EqualTo(0xD83D));
    }

    [Test]
    public void DecodeTextString_UsesPdfDocEncodingWhenUtf16BeBomIsAbsent()
    {
        byte[] bytes = new byte[] { 0x41, 0xA0, 0x94 };

        Assert.That(UnicodePdfEncoding.HasUtf16BeBom(bytes), Is.False);
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("A\u20AC\uFB02"));
    }

    [Test]
    public void DecodeTextString_UsesUtf16BeWhenBomIsPresent()
    {
        byte[] bytes = new byte[] { 0xFE, 0xFF, 0x00, 0x41, 0x01, 0x00 };

        Assert.That(UnicodePdfEncoding.HasUtf16BeBom(bytes), Is.True);
        Assert.That(new UnicodePdfEncoding().DecodeTextString(bytes), Is.EqualTo("A\u0100"));
        Assert.That(UnicodePdfEncoding.DecodeUtf16BePayload(bytes, 2), Is.EqualTo("A\u0100"));
    }

    [Test]
    public void DecodeTextString_ReturnsEmptyStringForUtf16BeBomOnly()
    {
        Assert.That(new UnicodePdfEncoding().DecodeTextString(new byte[] { 0xFE, 0xFF }), Is.EqualTo(""));
        Assert.That(UnicodePdfEncoding.HasUtf16BeBom(new byte[] { 0xFE }), Is.False);
    }

    [Test]
    public void EncodePdfDocEncoding_EncodesIdentityAndSpecialMappings()
    {
        string text = "A\u00A9\u20AC\u2022\uFB01\u02D8";
        byte[] expected = new byte[] { 0x41, 0xA9, 0xA0, 0x80, 0x93, 0x18 };

        byte[] bytes = new UnicodePdfEncoding().EncodePdfDocEncoding(text);

        Assert.That(bytes, Is.EqualTo(expected));
        Assert.That(new UnicodePdfEncoding().DecodePdfDocEncoding(bytes), Is.EqualTo(text));
    }

    [Test]
    public void DecodePdfDocEncoding_MapsUndefinedBytesToReplacementCharacter()
    {
        byte[] bytes = new byte[] { 0x41, 0x7F, 0x9F, 0xAD, 0xFF };

        string text = new UnicodePdfEncoding().DecodePdfDocEncoding(bytes);

        Assert.That(text, Is.EqualTo("A\uFFFD\uFFFD\uFFFD\u00FF"));
    }

    [Test]
    public void CanEncodePdfDocEncoding_ReturnsFalseForUnmappedScalarsAndInvalidUtf16()
    {
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding("A\u20AC\uFB02"), Is.True);
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding(""), Is.True);
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding("\u00AD"), Is.False);
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding("\u0100"), Is.False);
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding("\uD800"), Is.False);
        Assert.That(new UnicodePdfEncoding().CanEncodePdfDocEncoding("\U0001F600"), Is.False);
    }

    [Test]
    public void FindPdfDocEncodingByte_UsesIdentityAndSpecialTables()
    {
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(0x0041), Is.EqualTo(0x41));
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(0x00FF), Is.EqualTo(0xFF));
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(0x20AC), Is.EqualTo(0xA0));
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(0xFB02), Is.EqualTo(0x94));
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(0x00AD), Is.EqualTo(-1));
        Assert.That(UnicodePdfEncoding.FindPdfDocEncodingByte(UnicodeScalar.ReplacementCharacter), Is.EqualTo(-1));
    }

    [Test]
    public void PdfDocEncoding_HandlesEmptyInputs()
    {
        Assert.That(new UnicodePdfEncoding().EncodeTextString(""), Is.Empty);
        Assert.That(new UnicodePdfEncoding().DecodeTextString(new byte[] { }), Is.EqualTo(""));
        Assert.That(new UnicodePdfEncoding().EncodePdfDocEncoding(""), Is.Empty);
        Assert.That(new UnicodePdfEncoding().DecodePdfDocEncoding(new byte[] { }), Is.EqualTo(""));
        Assert.That(new UnicodePdfEncoding().EncodeUtf16BeWithBom(""), Is.EqualTo(new byte[] { 0xFE, 0xFF }));
        Assert.That(UnicodePdfEncoding.EncodeUtf16BePayload(""), Is.Empty);
    }

    [Test]
    public void EncodePdfDocEncoding_ThrowsForUnencodableText()
    {
        Assert.Throws<ArgumentException>(() => new UnicodePdfEncoding().EncodePdfDocEncoding("\u0100"));
        Assert.Throws<ArgumentException>(() => new UnicodePdfEncoding().EncodePdfDocEncoding("\uD800"));
    }

    [Test]
    public void PdfDocEncodingHelpers_RejectNullArguments()
    {
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().EncodeTextString(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().DecodeTextString(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().GetTextStringEncoding(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().EncodeUtf16BeWithBom(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfEncoding.EncodeUtf16BePayload(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfEncoding.HasUtf16BeBom(null!));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfEncoding.DecodeUtf16BePayload(null!, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.DecodeUtf16BePayload(new byte[] { }, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.DecodeUtf16BePayload(new byte[] { }, 1));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfEncoding.ReadUtf16BeCodeUnit(null!, 0));
        Assert.Throws<ArgumentNullException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(null!, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.ReadUtf16BeCodeUnit(new byte[] { 0x00 }, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.ReadUtf16BeCodeUnit(new byte[] { 0x00 }, int.MaxValue));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.ReadUtf16BeCodeUnit(new byte[] { 0x00, 0x00 }, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(new byte[] { 0x00 }, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(new byte[] { 0x00 }, int.MaxValue, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(new byte[] { 0x00, 0x00 }, -1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(new byte[] { 0x00, 0x00 }, 0, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => UnicodePdfEncoding.WriteUtf16BeCodeUnit(new byte[] { 0x00, 0x00 }, 0, 0x10000));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().CanEncodePdfDocEncoding(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().EncodePdfDocEncoding(null!));
        Assert.Throws<ArgumentNullException>(() => new UnicodePdfEncoding().DecodePdfDocEncoding(null!));
    }
}
