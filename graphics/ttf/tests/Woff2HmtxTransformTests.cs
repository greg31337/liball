using System.Buffers.Binary;
using ttf;

namespace ttf.Tests;

internal class Woff2HmtxTransformTests
{
    [Test]
    public void ReconstructsBothBearingRunsFromTransformedGlyphBounds()
    {
        Woff2Font font = Parse(CreateTransformedFont(
            new short[] { 10, -5, 0 },
            2,
            3,
            new ushort[] { 500, 600 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            10));

        Woff2HmtxReconstruction result = Woff2HmtxTransform.Reconstruct(
            font,
            FontLoadOptions.CreateStrict());
        byte[] expected = new TestBinaryBuilder()
            .WriteUInt16(500)
            .WriteInt16(10)
            .WriteUInt16(600)
            .WriteInt16(-5)
            .WriteInt16(0)
            .CopyBytes();

        Assert.Multiple(() =>
        {
            Assert.That(result.GetGlyphCount(), Is.EqualTo(3));
            Assert.That(result.GetMetricCount(), Is.EqualTo(2));
            Assert.That(result.GetLength(), Is.EqualTo(10));
            Assert.That(result.CopyData(), Is.EqualTo(expected));
            Assert.That(result.GetAdvanceWidth(0), Is.EqualTo(500));
            Assert.That(result.GetAdvanceWidth(1), Is.EqualTo(600));
            Assert.That(result.GetAdvanceWidth(2), Is.EqualTo(600));
            Assert.That(result.GetLeftSideBearing(0), Is.EqualTo(10));
            Assert.That(result.GetLeftSideBearing(1), Is.EqualTo(-5));
            Assert.That(result.GetLeftSideBearing(2), Is.Zero);
        });
    }

    [Test]
    public void PreservesEitherEncodedBearingRunWhileReconstructingTheOther()
    {
        Woff2HmtxReconstruction omittedProportional = Woff2HmtxTransform.Reconstruct(
            Parse(CreateTransformedFont(
                new short[] { 10, -5, 30 },
                2,
                1,
                new ushort[] { 500, 600 },
                Array.Empty<short>(),
                new short[] { 123 },
                10)),
            FontLoadOptions.CreateStrict());
        Woff2HmtxReconstruction omittedTrailing = Woff2HmtxTransform.Reconstruct(
            Parse(CreateTransformedFont(
                new short[] { 10, -5, 30 },
                2,
                2,
                new ushort[] { 500, 600 },
                new short[] { 7, 8 },
                Array.Empty<short>(),
                10)),
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(omittedProportional.CopyData(), Is.EqualTo(
                new TestBinaryBuilder()
                    .WriteUInt16(500).WriteInt16(10)
                    .WriteUInt16(600).WriteInt16(-5)
                    .WriteInt16(123)
                    .CopyBytes()));
            Assert.That(omittedTrailing.CopyData(), Is.EqualTo(
                new TestBinaryBuilder()
                    .WriteUInt16(500).WriteInt16(7)
                    .WriteUInt16(600).WriteInt16(8)
                    .WriteInt16(30)
                    .CopyBytes()));
        });
    }

    [Test]
    public void ReconstructsBearingsFromNullTransformGlyfAndLoca()
    {
        Woff2Builder builder = AddMetricDependencies(
            new Woff2Builder()
                .AddKnownTable(1, BuildHead(0))
                .AddKnownTable(10, BuildRawGlyf(new short[] { -12, 20 }))
                .AddKnownTable(11, new TestBinaryBuilder()
                    .WriteUInt16(0)
                    .WriteUInt16(5)
                    .WriteUInt16(10)
                    .WriteUInt16(10)
                    .CopyBytes()),
            3,
            2);
        builder.AddTransformedKnownTable(
            3,
            1,
            10,
            BuildTransformedHmtx(
                3,
                new ushort[] { 700, 800 },
                Array.Empty<short>(),
                Array.Empty<short>()));

        Woff2HmtxReconstruction result = Woff2HmtxTransform.Reconstruct(
            Parse(builder.Build()),
            FontLoadOptions.CreateStrict());

        Assert.That(result.CopyData(), Is.EqualTo(
            new TestBinaryBuilder()
                .WriteUInt16(700).WriteInt16(-12)
                .WriteUInt16(800).WriteInt16(20)
                .WriteInt16(0)
                .CopyBytes()));
    }

    [Test]
    public void ResolvesFaceLocalDependenciesInCollections()
    {
        byte[] firstGlyf = BuildTransformedGlyf(new short[] { 4, -6 });
        byte[] secondGlyf = BuildTransformedGlyf(new short[] { 4, -6 });
        Woff2Builder builder = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, (uint)firstGlyf.Length, firstGlyf)
            .AddTransformedKnownTable(11, 0, 6, Array.Empty<byte>())
            .AddKnownTable(2, BuildHhea(1))
            .AddKnownTable(4, BuildMaxp(2))
            .AddTransformedKnownTable(
                3,
                1,
                6,
                BuildTransformedHmtx(
                    3,
                    new ushort[] { 900 },
                    Array.Empty<short>(),
                    Array.Empty<short>()))
            .AddTransformedKnownTable(10, 0, (uint)secondGlyf.Length, secondGlyf)
            .AddTransformedKnownTable(11, 0, 6, Array.Empty<byte>())
            .AddCollectionFace(0x00010000U, 0, 1, 2, 3, 4)
            .AddCollectionFace(0x00010000U, 5, 6, 2, 3, 4);
        Woff2Font font = Parse(builder.Build());

        Woff2HmtxReconstruction first = Woff2HmtxTransform.Reconstruct(
            font,
            0,
            FontLoadOptions.CreateStrict());
        Woff2HmtxReconstruction second = Woff2HmtxTransform.Reconstruct(
            font,
            1,
            FontLoadOptions.CreateStrict());

        Assert.Multiple(() =>
        {
            Assert.That(first.CopyData(), Is.EqualTo(second.CopyData()));
            Assert.That(first.GetLeftSideBearing(0), Is.EqualTo(4));
            Assert.That(first.GetLeftSideBearing(1), Is.EqualTo(-6));
        });
    }

    [TestCase(0)]
    [TestCase(4)]
    [TestCase(255)]
    public void RejectsInvalidTransformFlags(int flags)
    {
        byte[] fontData = CreateTransformedFont(
            new short[] { 1 },
            1,
            flags,
            new ushort[] { 500 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            4);

        Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
            Parse(fontData),
            FontLoadOptions.CreateStrict()));
    }

    [Test]
    public void RejectsTruncatedTrailingAndWrongOriginalLengthData()
    {
        byte[] truncated = CreateTransformedFont(
            new short[] { 1, 2 },
            1,
            3,
            Array.Empty<ushort>(),
            Array.Empty<short>(),
            Array.Empty<short>(),
            6);
        byte[] trailing = CreateTransformedFont(
            new short[] { 1 },
            1,
            3,
            new ushort[] { 500 },
            Array.Empty<short>(),
            new short[] { 9 },
            4);
        byte[] wrongLength = CreateTransformedFont(
            new short[] { 1 },
            1,
            3,
            new ushort[] { 500 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            6);

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                Parse(truncated), FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                Parse(trailing), FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                Parse(wrongLength), FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void RejectsMissingAndInconsistentDependencies()
    {
        byte[] glyf = BuildTransformedGlyf(new short[] { 1 });
        Woff2Font missingHhea = Parse(new Woff2Builder()
            .AddTransformedKnownTable(10, 0, (uint)glyf.Length, glyf)
            .AddTransformedKnownTable(11, 0, 4, Array.Empty<byte>())
            .AddKnownTable(4, BuildMaxp(1))
            .AddTransformedKnownTable(3, 1, 4, new byte[] { 3, 1, 244 })
            .Build());
        Woff2Font mismatchedGlyphCount = Parse(CreateTransformedFont(
            new short[] { 1 },
            2,
            3,
            new ushort[] { 500 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            6));
        Woff2Font invalidMetricCount = Parse(CreateTransformedFont(
            new short[] { 1 },
            0,
            3,
            new ushort[] { 500 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            4));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                missingHhea, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                mismatchedGlyphCount, FontLoadOptions.CreateStrict()));
            Assert.Throws<FontFormatException>(() => Woff2HmtxTransform.Reconstruct(
                invalidMetricCount, FontLoadOptions.CreateStrict()));
        });
    }

    [Test]
    public void EnforcesOutputLimitCancellationArgumentsAndIndexes()
    {
        Woff2Font font = Parse(CreateTransformedFont(
            new short[] { 1 },
            1,
            3,
            new ushort[] { 500 },
            Array.Empty<short>(),
            Array.Empty<short>(),
            4));
        Woff2HmtxReconstruction result = Woff2HmtxTransform.Reconstruct(
            font,
            FontLoadOptions.CreateStrict());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();
        Woff2Builder limitedBuilder = AddMetricDependencies(
            new Woff2Builder()
                .AddKnownTable(1, BuildHead(0))
                .AddKnownTable(10, BuildRawGlyf(new short[] { 1 }))
                .AddKnownTable(11, new byte[] { 0, 0, 0, 5 }),
            1,
            1);
        limitedBuilder.AddTransformedKnownTable(
            3,
            1,
            4,
            BuildTransformedHmtx(
                3,
                new ushort[] { 500 },
                Array.Empty<short>(),
                Array.Empty<short>()));
        Woff2Font outputLimitedFont = Parse(limitedBuilder.Build());

        Assert.Multiple(() =>
        {
            Assert.Throws<FontLimitException>(() => Woff2HmtxTransform.Reconstruct(
                outputLimitedFont, CreateOptions(3)));
            Assert.Throws<OperationCanceledException>(() => Woff2HmtxTransform.Reconstruct(
                font, FontLoadOptions.CreateStrict(cancellation.Token)));
            Assert.Throws<ArgumentNullException>(() => Woff2HmtxTransform.Reconstruct(
                null!, FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentNullException>(() => Woff2HmtxTransform.Reconstruct(font, null!));
            Assert.Throws<ArgumentOutOfRangeException>(() => Woff2HmtxTransform.Reconstruct(
                font, 1, FontLoadOptions.CreateStrict()));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetAdvanceWidth(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => result.GetLeftSideBearing(1));
        });
    }

    static byte[] CreateTransformedFont(
        short[] xMins,
        int metricCount,
        int flags,
        ushort[] advances,
        short[] proportionalBearings,
        short[] trailingBearings,
        uint originalHmtxLength)
    {
        byte[] glyf = BuildTransformedGlyf(xMins);
        Woff2Builder builder = new Woff2Builder()
            .AddTransformedKnownTable(10, 0, (uint)glyf.Length, glyf)
            .AddTransformedKnownTable(11, 0, (uint)((xMins.Length + 1) * 2), Array.Empty<byte>());
        AddMetricDependencies(builder, xMins.Length, metricCount);
        builder.AddTransformedKnownTable(
            3,
            1,
            originalHmtxLength,
            BuildTransformedHmtx(flags, advances, proportionalBearings, trailingBearings));
        return builder.Build();
    }

    static Woff2Builder AddMetricDependencies(
        Woff2Builder builder,
        int glyphCount,
        int metricCount)
    {
        return builder
            .AddKnownTable(2, BuildHhea(metricCount))
            .AddKnownTable(4, BuildMaxp(glyphCount));
    }

    static byte[] BuildTransformedHmtx(
        int flags,
        ushort[] advances,
        short[] proportionalBearings,
        short[] trailingBearings)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder().WriteUInt8((byte)flags);

        foreach (ushort advance in advances)
        {
            builder.WriteUInt16(advance);
        }

        foreach (short bearing in proportionalBearings)
        {
            builder.WriteInt16(bearing);
        }

        foreach (short bearing in trailingBearings)
        {
            builder.WriteInt16(bearing);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildTransformedGlyf(short[] xMins)
    {
        TestBinaryBuilder contours = new();
        TestBinaryBuilder points = new();
        TestBinaryBuilder flags = new();
        TestBinaryBuilder glyph = new();

        foreach (short xMin in xMins)
        {
            if (xMin < -255 || xMin > 255)
            {
                throw new ArgumentOutOfRangeException(nameof(xMins));
            }

            contours.WriteInt16(1);
            points.WriteUInt8(1);
            flags.WriteUInt8((byte)(xMin < 0 ? 10 : 11));
            glyph.WriteUInt8((byte)Math.Abs(xMin));
            glyph.WriteUInt8(0);
        }

        byte[] contourData = contours.CopyBytes();
        byte[] pointData = points.CopyBytes();
        byte[] flagData = flags.CopyBytes();
        byte[] glyphData = glyph.CopyBytes();
        byte[] bbox = new byte[((xMins.Length + 31) / 32) * 4];

        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(0)
            .WriteUInt16((ushort)xMins.Length)
            .WriteUInt16(0)
            .WriteUInt32((uint)contourData.Length)
            .WriteUInt32((uint)pointData.Length)
            .WriteUInt32((uint)flagData.Length)
            .WriteUInt32((uint)glyphData.Length)
            .WriteUInt32(0)
            .WriteUInt32((uint)bbox.Length)
            .WriteUInt32(0)
            .WriteBytes(contourData)
            .WriteBytes(pointData)
            .WriteBytes(flagData)
            .WriteBytes(glyphData)
            .WriteBytes(bbox)
            .CopyBytes();
    }

    static byte[] BuildRawGlyf(short[] xMins)
    {
        TestBinaryBuilder builder = new();

        foreach (short xMin in xMins)
        {
            builder
                .WriteInt16(1)
                .WriteInt16(xMin)
                .WriteInt16(0)
                .WriteInt16(xMin)
                .WriteInt16(0);
        }

        return builder.CopyBytes();
    }

    static byte[] BuildHead(int indexFormat)
    {
        byte[] data = new byte[54];
        BinaryPrimitives.WriteInt16BigEndian(data.AsSpan(50, 2), (short)indexFormat);
        return data;
    }

    static byte[] BuildHhea(int metricCount)
    {
        byte[] data = new byte[36];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(34, 2), (ushort)metricCount);
        return data;
    }

    static byte[] BuildMaxp(int glyphCount)
    {
        byte[] data = new byte[32];
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(0, 4), 0x00010000U);
        BinaryPrimitives.WriteUInt16BigEndian(data.AsSpan(4, 2), (ushort)glyphCount);
        return data;
    }

    static Woff2Font Parse(byte[] data)
    {
        return Woff2Parser.Parse(FontData.Create(data), FontLoadOptions.CreateStrict());
    }

    static FontLoadOptions CreateOptions(int maxOutputBytes)
    {
        FontLimits limits = FontLimits.Create(
            1_000_000,
            maxOutputBytes,
            10,
            100,
            100,
            100,
            100,
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
