using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class Woff2GlyfTransformTests
{
    [Test]
    public void ReconstructsTripletCategoriesInstructionsOverlapBoundsAndShortLoca()
    {
        byte[] nContours = new TestBinaryBuilder()
            .WriteInt16(2)
            .WriteInt16(0)
            .CopyBytes();
        byte[] nPoints = new byte[] { 3, 3 };
        byte[] flags = new byte[] { 1, 11, 0x97, 85, 123, 127 };
        byte[] glyph = new byte[]
        {
            5,
            7,
            0x12,
            3, 4,
            0, 0, 0x12,
            0, 10, 0, 20,
            3
        };
        byte[] bbox = new byte[4];
        byte[] instructions = new byte[] { 0xAA, 0xBB, 0xCC };
        byte[] transformed = BuildTransform(
            1,
            2,
            0,
            nContours,
            nPoints,
            flags,
            glyph,
            Array.Empty<byte>(),
            bbox,
            instructions,
            new byte[] { 0x80 });

        Woff2GlyfReconstruction result = Reconstruct(transformed, 6);
        byte[] expectedGlyf = new TestBinaryBuilder()
            .WriteInt16(2)
            .WriteInt16(0)
            .WriteInt16(3)
            .WriteInt16(24)
            .WriteInt16(25)
            .WriteUInt16(2)
            .WriteUInt16(5)
            .WriteUInt16(3)
            .WriteBytes(instructions)
            .WriteBytes(new byte[] { 0x75, 0x33, 0x36, 0x17, 0x37, 0x37 })
            .WriteBytes(new byte[] { 7, 2, 4, 1, 10 })
            .WriteBytes(new byte[] { 5, 3, 5, 2, 20 })
            .WriteUInt8(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetGlyphCount(), Is.EqualTo(2));
            Assert.That(result.GetIndexFormat(), Is.Zero);
            Assert.That(result.CopyGlyfData(), Is.EqualTo(expectedGlyf));
            Assert.That(result.CopyLocaData(), Is.EqualTo(new byte[] { 0, 0, 0, 18, 0, 18 }));
            Assert.That(result.GetXMin(0), Is.Zero);
            Assert.That(result.GetYMin(0), Is.EqualTo(3));
            Assert.That(result.GetXMax(0), Is.EqualTo(24));
            Assert.That(result.GetYMax(0), Is.EqualTo(25));
            Assert.That(result.GetXMin(1), Is.Zero);
            Assert.That((result.CopyGlyfData()[19] & 0x40), Is.EqualTo(0x40));
            Assert.That((result.CopyGlyfData()[21] & 1), Is.Zero);
        });
    }

    [Test]
    public void ReconstructsCompositeComponentsExplicitBoundsInstructionsAndLongLoca()
    {
        byte[] nContours = new TestBinaryBuilder()
            .WriteInt16(0)
            .WriteInt16(-1)
            .CopyBytes();
        byte[] glyph = new byte[] { 2 };
        byte[] component = new TestBinaryBuilder()
            .WriteUInt16(0x050B)
            .WriteUInt16(0)
            .WriteInt16(10)
            .WriteInt16(-20)
            .WriteInt16(0x2000)
            .CopyBytes();
        byte[] bbox = new TestBinaryBuilder()
            .WriteBytes(new byte[] { 0x40, 0, 0, 0 })
            .WriteInt16(-3)
            .WriteInt16(-20)
            .WriteInt16(40)
            .WriteInt16(50)
            .CopyBytes();
        byte[] instructions = new byte[] { 0xB0, 7 };
        byte[] transformed = BuildTransform(
            0,
            2,
            1,
            nContours,
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            glyph,
            component,
            bbox,
            instructions,
            Array.Empty<byte>());

        Woff2GlyfReconstruction result = Reconstruct(transformed, 12);
        byte[] expectedGlyf = new TestBinaryBuilder()
            .WriteInt16(-1)
            .WriteInt16(-3)
            .WriteInt16(-20)
            .WriteInt16(40)
            .WriteInt16(50)
            .WriteBytes(component)
            .WriteUInt16(2)
            .WriteBytes(instructions)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetIndexFormat(), Is.EqualTo(1));
            Assert.That(result.CopyGlyfData(), Is.EqualTo(expectedGlyf));
            Assert.That(result.CopyLocaData(), Is.EqualTo(new byte[]
            {
                0, 0, 0, 0,
                0, 0, 0, 0,
                0, 0, 0, 24
            }));
            Assert.That(result.GetXMin(1), Is.EqualTo(-3));
            Assert.That(result.GetYMin(1), Is.EqualTo(-20));
            Assert.That(result.GetXMax(1), Is.EqualTo(40));
            Assert.That(result.GetYMax(1), Is.EqualTo(50));
        });
    }

    [Test]
    public void NormalizesInstructionFlagOntoLastCompositeComponent()
    {
        byte[] component = new TestBinaryBuilder()
            .WriteUInt16(0x0122)
            .WriteUInt16(0)
            .WriteUInt8(1)
            .WriteUInt8(2)
            .WriteUInt16(0x0002)
            .WriteUInt16(0)
            .WriteUInt8(3)
            .WriteUInt8(4)
            .CopyBytes();
        byte[] transformed = BuildTransform(
            0,
            1,
            1,
            new TestBinaryBuilder().WriteInt16(-1).CopyBytes(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[] { 1 },
            component,
            new TestBinaryBuilder()
                .WriteBytes(new byte[] { 0x80, 0, 0, 0 })
                .WriteInt16(0)
                .WriteInt16(0)
                .WriteInt16(0)
                .WriteInt16(0)
                .CopyBytes(),
            new byte[] { 0x2B },
            Array.Empty<byte>());

        byte[] glyf = Reconstruct(transformed, 8).CopyGlyfData();

        Assert.Multiple(() =>
        {
            Assert.That(ReadUInt16(glyf, 10), Is.EqualTo(0x0122));
            Assert.That(ReadUInt16(glyf, 16), Is.EqualTo(0x0102));
            Assert.That(ReadUInt16(glyf, 22), Is.EqualTo(1));
            Assert.That(glyf[24], Is.EqualTo(0x2B));
        });
    }

    [Test]
    public void RejectsMalformedStreamLayoutBoundsCoordinatesAndLocaLength()
    {
        byte[] emptyGlyph = BuildTransform(
            0,
            1,
            0,
            new TestBinaryBuilder().WriteInt16(0).CopyBytes(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[4],
            Array.Empty<byte>(),
            Array.Empty<byte>());
        byte[] trailingContour = emptyGlyph.ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(trailingContour.AsSpan(8, 4), 4);
        byte[] explicitEmpty = emptyGlyph.ToArray();
        explicitEmpty[36 + 2] = 0x80;
        byte[] overflowingCoordinate = BuildTransform(
            0,
            1,
            0,
            new TestBinaryBuilder().WriteInt16(1).CopyBytes(),
            new byte[] { 2 },
            new byte[] { 127, 127 },
            new byte[] { 0x7F, 0xFF, 0, 0, 1, 0, 0, 0, 0, 0 },
            Array.Empty<byte>(),
            new byte[4],
            Array.Empty<byte>(),
            Array.Empty<byte>());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Reconstruct(trailingContour, 4));
            Assert.Throws<FontFormatException>(() => Reconstruct(explicitEmpty, 4));
            Assert.Throws<FontFormatException>(() => Reconstruct(overflowingCoordinate, 4));
            Assert.Throws<FontFormatException>(() => Reconstruct(emptyGlyph, 6));
            Assert.Throws<FontFormatException>(() => Reconstruct(emptyGlyph.AsSpan(0, 39).ToArray(), 4));
        });
    }

    [Test]
    public void EnforcesLimitsCancellationArgumentsAndResultIndexes()
    {
        byte[] transformed = BuildTransform(
            0,
            1,
            0,
            new TestBinaryBuilder().WriteInt16(0).CopyBytes(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[4],
            Array.Empty<byte>(),
            Array.Empty<byte>());
        Woff2Font font = Parse(transformed, 4);
        Woff2GlyfReconstruction result = Woff2GlyfTransform.Reconstruct(
            font.GetTable(0),
            font.GetTable(1),
            FontLoadOptions.CreateStrict());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<OperationCanceledException>(() => Woff2GlyfTransform.Reconstruct(
                font.GetTable(0),
                font.GetTable(1),
                FontLoadOptions.CreateStrict(cancellation.Token)));
            Assert.Throws<ArgumentNullException>(() => Woff2GlyfTransform.Reconstruct(
                null!,
                font.GetTable(1),
                FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() => Woff2GlyfTransform.Reconstruct(
                font.GetTable(0),
                null!,
                FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() => Woff2GlyfTransform.Reconstruct(
                font.GetTable(0),
                font.GetTable(1),
                null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetXMin(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetYMax(1));
        });
    }

    [Test]
    public void EnforcesGlyphPointContourAndCombinedOutputLimits()
    {
        byte[] twoEmptyGlyphs = BuildTransform(
            0,
            2,
            0,
            new TestBinaryBuilder().WriteInt16(0).WriteInt16(0).CopyBytes(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            new byte[4],
            Array.Empty<byte>(),
            Array.Empty<byte>());
        byte[] oneContour = BuildSimpleTransform(1, new byte[] { 2 });
        byte[] twoContours = BuildSimpleTransform(2, new byte[] { 1, 1 });

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Reconstruct(
                Parse(twoEmptyGlyphs, 6),
                CreateOptions(1, 10, 10, 100)));
            Assert.Throws<FontLimitException>(() => Reconstruct(
                Parse(oneContour, 4),
                CreateOptions(10, 1, 10, 100)));
            Assert.Throws<FontLimitException>(() => Reconstruct(
                Parse(twoContours, 4),
                CreateOptions(10, 10, 1, 100)));
            Assert.Throws<FontLimitException>(() => Reconstruct(
                Parse(oneContour, 4),
                CreateOptions(10, 10, 10, 20)));
        });
    }

    static byte[] BuildTransform(
        ushort optionFlags,
        ushort glyphCount,
        ushort indexFormat,
        byte[] nContours,
        byte[] nPoints,
        byte[] flags,
        byte[] glyph,
        byte[] composite,
        byte[] bbox,
        byte[] instructions,
        byte[] overlap)
    {
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(optionFlags)
            .WriteUInt16(glyphCount)
            .WriteUInt16(indexFormat)
            .WriteUInt32((uint)nContours.Length)
            .WriteUInt32((uint)nPoints.Length)
            .WriteUInt32((uint)flags.Length)
            .WriteUInt32((uint)glyph.Length)
            .WriteUInt32((uint)composite.Length)
            .WriteUInt32((uint)bbox.Length)
            .WriteUInt32((uint)instructions.Length)
            .WriteBytes(nContours)
            .WriteBytes(nPoints)
            .WriteBytes(flags)
            .WriteBytes(glyph)
            .WriteBytes(composite)
            .WriteBytes(bbox)
            .WriteBytes(instructions)
            .WriteBytes(overlap)
            .CopyBytes();
    }

    static Woff2GlyfReconstruction Reconstruct(byte[] transformed, uint locaOriginalLength)
    {
        Woff2Font font = Parse(transformed, locaOriginalLength);
        return Reconstruct(font, FontLoadOptions.CreateStrict());
    }

    static Woff2GlyfReconstruction Reconstruct(Woff2Font font, FontLoadOptions options)
    {
        return Woff2GlyfTransform.Reconstruct(font, options);
    }

    static byte[] BuildSimpleTransform(short contourCount, byte[] contourPoints)
    {
        byte[] flags = new byte[] { 1, 1 };
        byte[] glyph = new byte[] { 1, 1, 0 };
        return BuildTransform(
            0,
            1,
            0,
            new TestBinaryBuilder().WriteInt16(contourCount).CopyBytes(),
            contourPoints,
            flags,
            glyph,
            Array.Empty<byte>(),
            new byte[4],
            Array.Empty<byte>(),
            Array.Empty<byte>());
    }

    static Woff2Font Parse(byte[] transformed, uint locaOriginalLength)
    {
        byte[] woff = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, (uint)transformed.Length, transformed)
            .AddTransformedKnownTable(11, 0, locaOriginalLength, Array.Empty<byte>())
            .Build();
        return Woff2Parser.Parse(FontData.Create(woff), FontLoadOptions.CreateStrict());
    }

    static ushort ReadUInt16(byte[] data, int offset)
    {
        return BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(offset, 2));
    }

    static FontLoadOptions CreateOptions(
        int maxGlyphs,
        int maxPoints,
        int maxContours,
        int maxOutputBytes)
    {
        FontLimits limits = FontLimits.Create(
            1_000_000,
            maxOutputBytes,
            10,
            100,
            maxGlyphs,
            maxPoints,
            maxContours,
            10,
            10,
            10,
            100,
            100,
            100,
            100,
            1_000_000,
            100,
            100,
            10,
            100);
        return FontLoadOptions.Create(
            FontLoadMode.Strict,
            FontInputOwnership.Copy,
            true,
            FontRepairPolicy.None,
            FontChecksumPolicy.RequireValid,
            limits,
            CancellationToken.None);
    }
}
