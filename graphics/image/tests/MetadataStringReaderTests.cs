using System.Collections.Generic;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class MetadataStringReaderTests
{
    [Test]
    public void DecodeAsciiAndUtf8_ReturnExpectedText()
    {
        byte[] ascii = new byte[] { 65, 66, 67 };
        byte[] utf8 = new byte[] { 99, 97, 102, 195, 169 };

        string asciiText = MetadataStringReader.DecodeAscii(ascii);
        string utf8Text = MetadataStringReader.DecodeUtf8(utf8);

        Assert.That(asciiText, Is.EqualTo("ABC"));
        Assert.That(utf8Text, Is.EqualTo("caf" + '\u00e9'));
    }

    [Test]
    public void DecodeUtf8WithLatin1Fallback_UsesLatin1ForInvalidUtf8()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] latin1 = new byte[] { 99, 97, 102, 233 };

        string text = MetadataStringReader.DecodeUtf8WithLatin1Fallback(latin1, diagnostics, "tEXt");

        Assert.That(text, Is.EqualTo("caf" + '\u00e9'));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Warning));
        Assert.That(diagnostics[0].Code, Is.EqualTo("string.utf8.invalid"));
    }

    [Test]
    public void DecodeUtf16_HandlesBomAndExplicitEndian()
    {
        byte[] bomLittle = new byte[] { 255, 254, 72, 0, 105, 0 };
        byte[] big = new byte[] { 0, 72, 0, 105 };

        string fromBom = MetadataStringReader.DecodeUtf16(bomLittle);
        string fromBig = MetadataStringReader.DecodeUtf16BigEndian(big);

        Assert.That(fromBom, Is.EqualTo("Hi"));
        Assert.That(fromBig, Is.EqualTo("Hi"));
    }

    [Test]
    public void DecodeUtf16OddByteCount_IgnoresTrailingByteAndAddsDiagnostic()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] bytes = new byte[] { 72, 0, 105 };

        string text = MetadataStringReader.DecodeUtf16LittleEndianRange(bytes, 0, 3, diagnostics, "APP1");

        Assert.That(text, Is.EqualTo("H"));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Code, Is.EqualTo("string.utf16.odd.length"));
    }

    [Test]
    public void NullTerminatedDecoders_StopBeforeTerminator()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] ascii = new byte[] { 97, 98, 99, 0, 100 };
        byte[] utf16 = new byte[] { 65, 0, 0, 0, 66, 0 };

        string asciiText = MetadataStringReader.DecodeNullTerminatedAscii(ascii, 0, ascii.Length, diagnostics, "COM");
        string utf16Text = MetadataStringReader.DecodeNullTerminatedUtf16LittleEndian(utf16, 0, utf16.Length, diagnostics, "APP1");

        Assert.That(asciiText, Is.EqualTo("abc"));
        Assert.That(utf16Text, Is.EqualTo("A"));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void PascalDecoders_UseDeclaredPayloadLength()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] ascii = new byte[] { 3, 99, 97, 116, 0 };
        byte[] utf8 = new byte[] { 5, 99, 97, 102, 195, 169 };

        string asciiText = MetadataStringReader.DecodePascalAscii(ascii, 0, ascii.Length, diagnostics, "IRB");
        string utf8Text = MetadataStringReader.DecodePascalUtf8(utf8, 0, utf8.Length, diagnostics, "IRB");

        Assert.That(asciiText, Is.EqualTo("cat"));
        Assert.That(utf8Text, Is.EqualTo("caf" + '\u00e9'));
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void PascalDecoder_WhenDeclaredLengthExceedsAvailablePayload_ClampsAndWarns()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] ascii = new byte[] { 5, 104, 105 };

        string text = MetadataStringReader.DecodePascalAscii(ascii, 0, ascii.Length, diagnostics, "IRB");

        Assert.That(text, Is.EqualTo("hi"));
        Assert.That(diagnostics, Has.Count.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(MetadataSeverity.Truncated));
        Assert.That(diagnostics[0].Code, Is.EqualTo("string.pascal.payload.truncated"));
    }

    [Test]
    public void RangeDecoders_ClampRangesAndReportDiagnostics()
    {
        List<MetadataDiagnostic> diagnostics = new List<MetadataDiagnostic>();
        byte[] ascii = new byte[] { 65, 66, 67 };

        string truncated = MetadataStringReader.DecodeAsciiRange(ascii, 1, 10, diagnostics, "tag");
        string outside = MetadataStringReader.DecodeAsciiRange(ascii, 8, 2, diagnostics, "tag");
        string negative = MetadataStringReader.DecodeAsciiRange(ascii, -1, 2, diagnostics, "tag");

        Assert.That(truncated, Is.EqualTo("BC"));
        Assert.That(outside, Is.EqualTo(""));
        Assert.That(negative, Is.EqualTo(""));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.Truncated), Is.EqualTo(2));
        Assert.That(MetadataDiagnostic.CountSeverity(diagnostics, MetadataSeverity.RecoverableError), Is.EqualTo(1));
    }

    [Test]
    public void Utf8Validator_DetectsValidAndInvalidSequences()
    {
        byte[] valid = new byte[] { 226, 130, 172 };
        byte[] invalid = new byte[] { 226, 40, 172 };

        Assert.That(MetadataStringReader.IsValidUtf8(valid), Is.True);
        Assert.That(MetadataStringReader.IsValidUtf8(invalid), Is.False);
    }
}
