using ttf;

namespace ttf.Tests;

internal class SfntPathBoundsTests
{
    [Test]
    public void CalculatesExactLineQuadraticAndCubicExtremaAndCachesResult()
    {
        SfntGlyphOutline outline = CreateOutline(
            Move(0, 0),
            Line(-5, 5),
            Quadratic(-5, 25, 15, 5),
            Cubic(15, -35, 45, -35, 45, 5),
            Close());

        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(bounds.IsEmpty(), Is.False);
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-5));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-25).Within(0.000000001));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(45));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(15).Within(0.000000001));
            Assert.That(outline.GetBounds(), Is.SameAs(bounds));
        });
    }

    [Test]
    public void FindsBothInteriorCubicExtrema()
    {
        SfntPathBounds bounds = CreateOutline(
            Move(0, 0),
            Cubic(10, 0, -10, 0, 0, 0),
            Close()).GetBounds();
        double expected = 5 * Math.Sqrt(3) / 3;

        Assert.Multiple(() =>
        {
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-expected).Within(0.000000001));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(expected).Within(0.000000001));
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetYMaximum(), Is.Zero);
        });
    }

    [Test]
    public void FindsCubicExtremumWhenDerivativeDegeneratesToLine()
    {
        SfntPathBounds bounds = CreateOutline(
            Move(0, 0),
            Cubic(2, 0, 2, 0, 0, 0),
            Close()).GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(1.5));
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetYMaximum(), Is.Zero);
        });
    }

    [Test]
    public void HandlesDegenerateCurvesMovesAndClosedSubpaths()
    {
        SfntGlyphOutline outline = CreateOutline(
            Move(2, 3),
            Quadratic(4, 5, 6, 7),
            Cubic(8, 9, 10, 11, 12, 13),
            Close(),
            Move(-4, -6),
            Close());
        SfntPathBounds bounds = outline.GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-4));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-6));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(12));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(13));
        });
    }

    [Test]
    public void RepresentsEmptyPathsWithoutInventingCoordinates()
    {
        SfntPathBounds bounds = CreateOutline().GetBounds();

        Assert.Multiple(() =>
        {
            Assert.That(bounds.IsEmpty(), Is.True);
            Assert.Throws<InvalidOperationException>(() => bounds.GetXMinimum());
            Assert.Throws<InvalidOperationException>(() => bounds.GetYMinimum());
            Assert.Throws<InvalidOperationException>(() => bounds.GetXMaximum());
            Assert.Throws<InvalidOperationException>(() => bounds.GetYMaximum());
        });
    }

    [Test]
    public void RejectsInvalidPathSequencesKindsAndCoordinates()
    {
        SfntGlyphOutline lineFirst = CreateOutline(Line(1, 1));
        SfntGlyphOutline lineAfterClose = CreateOutline(
            Move(0, 0),
            Close(),
            Line(1, 1));
        SfntGlyphOutline unknown = CreateOutline(
            Move(0, 0),
            SfntPathCommand.Create((SfntPathCommandKind)99, 0, 0, 1, 1));
        SfntGlyphOutline nonFinite = CreateOutline(Move(double.NaN, 0));
        SfntGlyphOutline nonFiniteControl = CreateOutline(
            Move(0, 0),
            Quadratic(double.PositiveInfinity, 0, 1, 1));

        Assert.Multiple(() =>
        {
            Assert.Throws<FontFormatException>(() => lineFirst.GetBounds());
            Assert.Throws<FontFormatException>(() => lineAfterClose.GetBounds());
            Assert.Throws<FontFormatException>(() => unknown.GetBounds());
            Assert.Throws<FontFormatException>(() => nonFinite.GetBounds());
            Assert.Throws<FontFormatException>(() => nonFiniteControl.GetBounds());
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.Calculate(null!));
        });
    }

    [Test]
    public void ExposesCubicControlsWithoutChangingOtherCommandContracts()
    {
        SfntPathCommand cubic = Cubic(1, 2, 3, 4, 5, 6);
        SfntPathCommand quadratic = Quadratic(7, 8, 9, 10);
        SfntPathCommand line = Line(11, 12);

        Assert.Multiple(() =>
        {
            Assert.That(cubic.GetControlX(), Is.EqualTo(1));
            Assert.That(cubic.GetControlY(), Is.EqualTo(2));
            Assert.That(cubic.GetSecondControlX(), Is.EqualTo(3));
            Assert.That(cubic.GetSecondControlY(), Is.EqualTo(4));
            Assert.That(cubic.GetEndX(), Is.EqualTo(5));
            Assert.That(cubic.GetEndY(), Is.EqualTo(6));
            Assert.Throws<InvalidOperationException>(() =>
                quadratic.GetSecondControlX());
            Assert.Throws<InvalidOperationException>(() => line.GetControlX());
        });
    }

    [Test]
    public void AppliesStoredCalculatedAndRequiredMatchPolicies()
    {
        SfntPathBounds stored = new SfntPathBounds(false, 0, 0, 20, 20);
        SfntPathBounds calculated = new SfntPathBounds(false, 0, 0, 20, 10);
        SfntBoundsComparison kept = SfntPathBoundsCalculator.Compare(
            stored,
            calculated,
            SfntBoundsPolicy.KeepStored);
        SfntBoundsComparison replaced = SfntPathBoundsCalculator.Compare(
            stored,
            calculated,
            SfntBoundsPolicy.UseCalculated);

        Assert.Multiple(() =>
        {
            Assert.That(kept.IsMatch(), Is.False);
            Assert.That(kept.GetStoredBounds(), Is.SameAs(stored));
            Assert.That(kept.GetCalculatedBounds(), Is.SameAs(calculated));
            Assert.That(kept.GetEffectiveBounds(), Is.SameAs(stored));
            Assert.That(replaced.GetEffectiveBounds(), Is.SameAs(calculated));
            Assert.Throws<FontFormatException>(() =>
                SfntPathBoundsCalculator.Compare(
                    stored,
                    calculated,
                    SfntBoundsPolicy.RequireMatch));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                SfntPathBoundsCalculator.Compare(
                    stored,
                    calculated,
                    (SfntBoundsPolicy)99));
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.Compare(
                    null!,
                    calculated,
                    SfntBoundsPolicy.KeepStored));
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.Compare(
                    stored,
                    null!,
                    SfntBoundsPolicy.KeepStored));
        });
    }

    [Test]
    public void ComparesExactGlyphBoundsWithStoredGlyfHeader()
    {
        byte[] curvedGlyph = BuildSimpleGlyph(
            new int[] { 0, 10, 20 },
            new int[] { 0, 20, 0 },
            new bool[] { true, false, true },
            0,
            0,
            20,
            20);
        SfntLocaTable table = CreateTable(new byte[][] { curvedGlyph });
        SfntBoundsComparison kept = table.GetGlyphBoundsComparison(
            0,
            SfntBoundsPolicy.KeepStored);
        SfntBoundsComparison calculated = table.GetGlyphBoundsComparison(
            0,
            SfntBoundsPolicy.UseCalculated);

        Assert.Multiple(() =>
        {
            Assert.That(kept.IsMatch(), Is.False);
            Assert.That(kept.GetStoredBounds().GetYMaximum(), Is.EqualTo(20));
            Assert.That(kept.GetCalculatedBounds().GetYMaximum(), Is.EqualTo(10));
            Assert.That(kept.GetEffectiveBounds().GetYMaximum(), Is.EqualTo(20));
            Assert.That(calculated.GetEffectiveBounds().GetYMaximum(), Is.EqualTo(10));
            Assert.Throws<FontFormatException>(() =>
                table.GetGlyphBoundsComparison(0, SfntBoundsPolicy.RequireMatch));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                table.GetGlyphBoundsComparison(-1, SfntBoundsPolicy.KeepStored));
        });
    }

    [Test]
    public void TreatsEmptyGlyphAndZeroStoredBoundsAsMatching()
    {
        SfntLocaTable table = CreateTable(new byte[][] { Array.Empty<byte>() });
        SfntBoundsComparison comparison = table.GetGlyphBoundsComparison(
            0,
            SfntBoundsPolicy.RequireMatch);

        Assert.Multiple(() =>
        {
            Assert.That(comparison.IsMatch(), Is.True);
            Assert.That(comparison.GetCalculatedBounds().IsEmpty(), Is.True);
            Assert.That(comparison.GetStoredBounds().IsEmpty(), Is.False);
            Assert.That(comparison.GetEffectiveBounds().GetXMaximum(), Is.Zero);
        });
    }

    [Test]
    public void UnionsEveryResolvedGlyphForGlobalHeadComparison()
    {
        byte[] first = BuildSimpleGlyph(
            new int[] { -5, 10 },
            new int[] { 2, 12 },
            new bool[] { true, true },
            -5,
            2,
            10,
            12);
        byte[] second = BuildSimpleGlyph(
            new int[] { 20, 30 },
            new int[] { -8, 4 },
            new bool[] { true, true },
            20,
            -8,
            30,
            4);
        SfntLocaTable table = CreateTable(new byte[][] { first, second });
        SfntBoundsComparison match = table.GetGlobalBoundsComparison(
            CreateHead(-5, -8, 30, 12),
            SfntBoundsPolicy.RequireMatch);
        SfntBoundsComparison mismatch = table.GetGlobalBoundsComparison(
            CreateHead(-10, -10, 40, 20),
            SfntBoundsPolicy.UseCalculated);

        Assert.Multiple(() =>
        {
            Assert.That(match.IsMatch(), Is.True);
            Assert.That(match.GetCalculatedBounds().GetXMinimum(), Is.EqualTo(-5));
            Assert.That(match.GetCalculatedBounds().GetYMinimum(), Is.EqualTo(-8));
            Assert.That(match.GetCalculatedBounds().GetXMaximum(), Is.EqualTo(30));
            Assert.That(match.GetCalculatedBounds().GetYMaximum(), Is.EqualTo(12));
            Assert.That(mismatch.IsMatch(), Is.False);
            Assert.That(mismatch.GetEffectiveBounds().GetXMaximum(), Is.EqualTo(30));
        });
    }

    [Test]
    public void GlobalComparisonValidatesDependencies()
    {
        SfntLocaTable table = CreateTable(new byte[][] { Array.Empty<byte>() });
        SfntHeadTable head = CreateHead(0, 0, 0, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.CompareFont(
                    null!,
                    head,
                    SfntBoundsPolicy.KeepStored));
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.CompareFont(
                    table,
                    null!,
                    SfntBoundsPolicy.KeepStored));
            Assert.Throws<ArgumentNullException>(() =>
                SfntPathBoundsCalculator.CompareGlyph(
                    null!,
                    null,
                    SfntBoundsPolicy.KeepStored));
        });
    }

    static SfntGlyphOutline CreateOutline(params SfntPathCommand[] commands)
    {
        return new SfntGlyphOutline(
            4,
            Array.Empty<SfntOutlineContour>(),
            commands,
            0,
            0);
    }

    static SfntPathCommand Move(double x, double y)
    {
        return SfntPathCommand.Create(SfntPathCommandKind.Move, 0, 0, x, y);
    }

    static SfntPathCommand Line(double x, double y)
    {
        return SfntPathCommand.Create(SfntPathCommandKind.Line, 0, 0, x, y);
    }

    static SfntPathCommand Quadratic(
        double controlX,
        double controlY,
        double endX,
        double endY)
    {
        return SfntPathCommand.Create(
            SfntPathCommandKind.Quadratic,
            controlX,
            controlY,
            endX,
            endY);
    }

    static SfntPathCommand Cubic(
        double firstControlX,
        double firstControlY,
        double secondControlX,
        double secondControlY,
        double endX,
        double endY)
    {
        return SfntPathCommand.Create(
            SfntPathCommandKind.Cubic,
            firstControlX,
            firstControlY,
            secondControlX,
            secondControlY,
            endX,
            endY);
    }

    static SfntPathCommand Close()
    {
        return SfntPathCommand.Create(SfntPathCommandKind.Close, 0, 0, 0, 0);
    }

    static SfntLocaTable CreateTable(byte[][] glyphs)
    {
        int[] offsets = new int[glyphs.Length + 1];
        int totalLength = 0;
        int emptyCount = 0;

        for (int index = 0; index < glyphs.Length; index += 1)
        {
            offsets[index] = totalLength;
            totalLength += glyphs[index].Length;

            if (glyphs[index].Length == 0)
            {
                emptyCount += 1;
            }
        }

        offsets[glyphs.Length] = totalLength;
        byte[] allData = new byte[totalLength];
        int destinationOffset = 0;

        foreach (byte[] glyph in glyphs)
        {
            glyph.CopyTo(allData, destinationOffset);
            destinationOffset += glyph.Length;
        }

        FontData data = FontData.Create(allData);
        SfntGlyfRecord[] records = new SfntGlyfRecord[glyphs.Length];

        for (int glyphId = 0; glyphId < glyphs.Length; glyphId += 1)
        {
            int length = offsets[glyphId + 1] - offsets[glyphId];
            records[glyphId] = new SfntGlyfRecord(
                glyphId,
                offsets[glyphId],
                data.GetSlice(offsets[glyphId], length));
        }

        SfntMaxpTable maxp = new SfntMaxpTable(
            0x00010000U,
            SfntMaxpKind.TrueType,
            glyphs.Length,
            100,
            10,
            100,
            10,
            1,
            0,
            0,
            0,
            0,
            0,
            0,
            10,
            10);
        return new SfntLocaTable(
            SfntLocaFormat.Long,
            offsets,
            records,
            totalLength,
            emptyCount,
            maxp,
            FontLimits.Create());
    }

    static byte[] BuildSimpleGlyph(
        int[] xs,
        int[] ys,
        bool[] onCurve,
        int xMinimum,
        int yMinimum,
        int xMaximum,
        int yMaximum)
    {
        TestBinaryBuilder builder = new TestBinaryBuilder()
            .WriteInt16(1)
            .WriteInt16((short)xMinimum)
            .WriteInt16((short)yMinimum)
            .WriteInt16((short)xMaximum)
            .WriteInt16((short)yMaximum)
            .WriteUInt16((ushort)(xs.Length - 1))
            .WriteUInt16(0);

        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteUInt8(onCurve[index] ? (byte)1 : (byte)0);
        }

        int previous = 0;

        for (int index = 0; index < xs.Length; index += 1)
        {
            builder.WriteInt16((short)(xs[index] - previous));
            previous = xs[index];
        }

        previous = 0;

        for (int index = 0; index < ys.Length; index += 1)
        {
            builder.WriteInt16((short)(ys[index] - previous));
            previous = ys[index];
        }

        if ((builder.GetLength() & 1) != 0)
        {
            builder.WriteUInt8(0);
        }

        return builder.CopyBytes();
    }

    static SfntHeadTable CreateHead(
        int xMinimum,
        int yMinimum,
        int xMaximum,
        int yMaximum)
    {
        return new SfntHeadTable(
            1,
            0,
            0,
            0,
            0x5F0F3CF5U,
            0,
            1000,
            0,
            DateTime.UnixEpoch,
            0,
            DateTime.UnixEpoch,
            xMinimum,
            yMinimum,
            xMaximum,
            yMaximum,
            0,
            0,
            0,
            SfntLocaFormat.Long,
            0,
            0);
    }
}
