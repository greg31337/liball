using System.Security.Cryptography;
using System.Text;
using System.Xml;
using ttf;

namespace ttf.Tests;

internal class ColorFontAssetTests
{
    const int GradientGlyphId = 90;
    const int CycleGlyphId = 178;
    const int SbixGlyphId = 19;

    [Test]
    public void NormalizesRealVariableColrGradientGraphs()
    {
        FontFace face = LoadFace("test_glyphs-glyf_colr_1_variable.ttf");
        FontInstance defaultInstance = face.CreateDefaultInstance();
        ColorGlyph defaultGlyph = new ColorGlyphResolver().ResolveGlyph(
            defaultInstance,
            GradientGlyphId);
        ColorPaint? defaultGradient = FindPaint(
            defaultGlyph.GetPaintGraph()!.GetRoot(),
            ColorPaintKind.LinearGradient);
        FontCoordinates coordinates = new FontCoordinates();
        coordinates.Set((int)TestSfntEncoding.EncodeTag("GRX0"), 500);
        FontInstance variedInstance = face.CreateInstance(coordinates);
        ColorGlyph variedGlyph = new ColorGlyphResolver().ResolveGlyph(
            variedInstance,
            GradientGlyphId);
        ColorPaint? variedGradient = FindPaint(
            variedGlyph.GetPaintGraph()!.GetRoot(),
            ColorPaintKind.LinearGradient);

        Assert.Multiple(() =>
        {
            Assert.That(defaultGlyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Colr));
            Assert.That(defaultGlyph.GetPaintGraph()!.GetColrVersion(), Is.EqualTo(1));
            Assert.That(defaultGlyph.GetPaintGraph()!.GetNodeCount(), Is.GreaterThan(2));
            Assert.That(defaultGlyph.GetPaintGraph()!.GetMaximumDepth(), Is.GreaterThan(1));
            Assert.That(defaultGradient, Is.Not.Null);
            Assert.That(defaultGradient!.GetStopCount(), Is.EqualTo(3));
            Assert.That(defaultGradient.GetStop(0).GetColor().GetColor(), Is.Not.Null);
            Assert.That(variedGradient, Is.Not.Null);
            Assert.That(variedGradient!.GetValue(0),
                Is.Not.EqualTo(defaultGradient.GetValue(0)));
            Assert.That(variedInstance.GetCoordinates().GetNormalizedValue(
                FindAxis(variedInstance.GetCoordinates(), "GRX0")),
                Is.GreaterThan(0));
        });
    }

    [Test]
    public void RejectsTheRealColrPaintGlyphCycle()
    {
        FontInstance instance = LoadFace(
            "test_glyphs-glyf_colr_1_variable.ttf").CreateDefaultInstance();

        FontFormatException? exception = Assert.Throws<FontFormatException>(() =>
            new ColorGlyphResolver().ResolveGlyph(instance, CycleGlyphId));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("cycle").IgnoreCase);
            Assert.That(exception.GetDiagnosticCode(),
                Is.EqualTo(FontDiagnosticCode.ColorPaintCycle));
        });
    }

    [Test]
    public void LoadsRealSvgAndProhibitsInjectedExternalEntities()
    {
        string path = FindAsset("samples-untouchedsvg.ttf");
        byte[] original = File.ReadAllBytes(path);
        int svgOffset = FindTableOffset(original, FontTags.Svg);
        int documentList = svgOffset + ReadUInt32(original, svgOffset + 2);
        int firstRecord = documentList + 2;
        int glyphId = ReadUInt16(original, firstRecord);
        int documentOffset = documentList + ReadUInt32(original, firstRecord + 4);
        int documentLength = ReadUInt32(original, firstRecord + 8);
        FontFace validFace = LoadFace("samples-untouchedsvg.ttf");
        ColorGlyph validGlyph = new ColorGlyphResolver().ResolveGlyph(
            validFace.CreateDefaultInstance(),
            glyphId);
        byte[] malicious = Encoding.UTF8.GetBytes(
            "<!DOCTYPE svg [<!ENTITY xxe SYSTEM \"file:///definitely-not-read\">]>" +
            "<svg xmlns=\"http://www.w3.org/2000/svg\">&xxe;</svg>");

        Assert.That(malicious.Length, Is.LessThan(documentLength));
        Array.Copy(malicious, 0, original, documentOffset, malicious.Length);
        WriteUInt32(original, firstRecord + 8, malicious.Length);
        FontFace maliciousFace = FontLoader.Load(
            original,
            FontLoadOptions.CreateCompatible()).GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(validGlyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Svg));
            Assert.That(validGlyph.GetSvgDocument()!.LoadXml().DocumentElement!.LocalName,
                Is.EqualTo("svg"));
            Assert.Throws<XmlException>(() =>
                new ColorGlyphResolver().ResolveGlyph(
                    maliciousFace.CreateDefaultInstance(),
                    glyphId));
        });
    }

    [Test]
    public void ReturnsExactRealSbixPayloadAndRejectsOutOfBoundsOffsets()
    {
        string path = FindAsset("samples-sbix.ttf");
        byte[] original = File.ReadAllBytes(path);
        FontFace face = FontLoader.Load(
            original,
            FontLoadOptions.CreateCompatible()).GetFace(0);
        ColorGlyph glyph = new ColorGlyphResolver().ResolveGlyph(
            face.CreateDefaultInstance(),
            SbixGlyphId);
        ColorBitmapStrike? strike = glyph.GetSelectedBitmapStrike();
        byte[] payload = strike!.CopyData();
        byte[] changedCopy = strike.CopyData();
        changedCopy[0] = 0;
        string payloadHash = Convert.ToHexString(
            SHA256.HashData(payload)).ToLowerInvariant();
        int sbixOffset = FindTableOffset(original, FontTags.Sbix);
        int strikeOffset = sbixOffset + ReadUInt32(original, sbixOffset + 8);
        int glyphEndOffset = strikeOffset + 4 + (SbixGlyphId + 1) * 4;
        WriteUInt32(original, glyphEndOffset, original.Length);
        FontFace malformedFace = FontLoader.Load(
            original,
            FontLoadOptions.CreateCompatible()).GetFace(0);

        Assert.Multiple(() =>
        {
            Assert.That(glyph.GetSelectedRepresentation(),
                Is.EqualTo(ColorGlyphRepresentation.Sbix));
            Assert.That(strike.GetPixelsPerEmX(), Is.EqualTo(109));
            Assert.That(strike.GetPixelsPerInch(), Is.EqualTo(72));
            Assert.That(strike.IsPng(), Is.True);
            Assert.That(payload, Has.Length.EqualTo(2516));
            Assert.That(payload.AsSpan(0, 8).ToArray(), Is.EqualTo(
                new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }));
            Assert.That(payloadHash, Is.EqualTo(
                "cfcd0bf5358e27749577041bbafeeec17479f6a1595c570a7cb4fc91e7e8b035"));
            Assert.That(strike.CopyData()[0], Is.EqualTo(0x89));
            Assert.Throws<FontFormatException>(() =>
                new ColorGlyphResolver().ResolveGlyph(
                    malformedFace.CreateDefaultInstance(),
                    SbixGlyphId));
        });
    }

    static ColorPaint? FindPaint(ColorPaint paint, ColorPaintKind kind)
    {
        if (paint.GetKind() == kind)
        {
            return paint;
        }

        int index = 0;

        while (index < paint.GetChildCount())
        {
            ColorPaint? found = FindPaint(paint.GetChild(index), kind);

            if (found != null)
            {
                return found;
            }

            index += 1;
        }

        return null;
    }

    static int FindAxis(NormalizedFontCoordinates coordinates, string tag)
    {
        int expected = (int)TestSfntEncoding.EncodeTag(tag);
        int index = 0;

        while (index < coordinates.GetAxisCount())
        {
            if (coordinates.GetAxisTag(index) == expected)
            {
                return index;
            }

            index += 1;
        }

        throw new AssertionException("The fixture does not contain axis " + tag + ".");
    }

    static FontFace LoadFace(string name)
    {
        return FontLoader.LoadFile(
            FindAsset(name),
            FontLoadOptions.CreateCompatible()).GetFace(0);
    }

    static string FindAsset(string name)
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);

        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "tests",
                "Assets",
                "ColorFonts",
                name);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new AssertionException("The color-font fixture could not be located: " + name);
    }

    static int FindTableOffset(byte[] bytes, int tag)
    {
        int tableCount = ReadUInt16(bytes, 4);
        int index = 0;

        while (index < tableCount)
        {
            int record = 12 + index * 16;

            if (ReadUInt32(bytes, record) == tag)
            {
                return ReadUInt32(bytes, record + 8);
            }

            index += 1;
        }

        throw new AssertionException("The expected table is absent from the fixture.");
    }

    static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    static int ReadUInt32(byte[] bytes, int offset)
    {
        uint value = (uint)bytes[offset] << 24 |
            (uint)bytes[offset + 1] << 16 |
            (uint)bytes[offset + 2] << 8 |
            bytes[offset + 3];

        if (value > int.MaxValue)
        {
            throw new AssertionException("A fixture offset exceeds the test address space.");
        }

        return (int)value;
    }

    static void WriteUInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }
}
