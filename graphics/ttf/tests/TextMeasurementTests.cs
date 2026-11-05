using ttf;

namespace ttf.Tests;

internal class TextMeasurementTests
{
    [Test]
    public void MeasuresScaledGlyphAdvanceAndExactCurveBounds()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();

        TextMeasurement measurement = instance.MeasureGlyph(0, 100);
        TextInkBounds bounds = measurement.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(measurement.GetPointSize(), Is.EqualTo(100));
            Assert.That(measurement.GetXAdvance(), Is.EqualTo(60));
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(bounds.IsEmpty(), Is.False);
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(2));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(1));
            Assert.That(bounds.GetWidth(), Is.EqualTo(2));
            Assert.That(bounds.GetHeight(), Is.EqualTo(1));
        });
    }

    [Test]
    public void SeparatesPositionedRunAdvanceFromExactInkUnion()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        ShapedRun run = Run(
            new int[] { 0, 2 },
            new double[] { 500, 700 },
            new double[] { -10, 5 },
            new double[] { 20, -4 },
            ShapingDirection.LeftToRight);

        TextMeasurement measurement = run.Measure(instance, 100);
        TextInkBounds bounds = measurement.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(measurement.GetXAdvance(), Is.EqualTo(120));
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-1));
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(52.5));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(3));
        });
    }

    [Test]
    public void UsesVerticalOriginsForVerticalRunInk()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        ShapedRun run = Run(
            new int[] { 2 },
            new double[] { 0 },
            new double[] { 0 },
            new double[] { 0 },
            ShapingDirection.TopToBottom);

        TextMeasurement measurement = run.Measure(instance, 100);
        TextInkBounds bounds = measurement.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(measurement.GetXAdvance(), Is.Zero);
            Assert.That(measurement.GetYAdvance(), Is.EqualTo(-100));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-39));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-99.6));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(-38));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(-99.1));
        });
    }

    [Test]
    public void MeasuresMixedFontRunsAtTheirResolvedPositions()
    {
        FontFace firstFace = FontInstanceTests.CreateFace();
        FontFace secondFace = FontMatchingTests.CloneFaceWithCmap(
            firstFace,
            "Second",
            BuildEmptyCmap());
        FontInstance first = firstFace.CreateDefaultInstance();
        FontInstance second = secondFace.CreateDefaultInstance();
        TextShapingRun firstRun = ShapingRun(first, 0, 600, 0);
        TextShapingRun secondRun = ShapingRun(second, 2, 1000, 1);
        TextShapingResult result = new TextShapingResult(
            new TextShapingRun[] { firstRun, secondRun },
            new FontDiagnostic[0],
            2,
            2,
            2,
            1,
            1);

        TextMeasurement measurement = result.Measure(100);
        TextInkBounds bounds = measurement.GetInkBounds();
        TextStyle style = new TextStyle(100);
        style.SetCharacterSpacing(2);
        style.SetHorizontalScale(2);
        style.SetBaselineShift(3);
        TextMeasurement styled = result.MeasureWithStyle(style);
        TextInkBounds styledBounds = styled.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(firstRun.GetFontFace(), Is.SameAs(firstFace));
            Assert.That(secondRun.GetFontFace(), Is.SameAs(secondFace));
            Assert.That(measurement.GetXAdvance(), Is.EqualTo(160));
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(62));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(1));
            Assert.That(styled.GetXAdvance(), Is.EqualTo(328));
            Assert.That(styled.GetYAdvance(), Is.Zero);
            Assert.That(styledBounds.GetXMinimum(), Is.Zero);
            Assert.That(styledBounds.GetYMinimum(), Is.EqualTo(3));
            Assert.That(styledBounds.GetXMaximum(), Is.EqualTo(128));
            Assert.That(styledBounds.GetYMaximum(), Is.EqualTo(4));
        });
    }

    [Test]
    public void PreservesEmptyInkAndValidatesMeasurementInputs()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        TextMeasurement emptyGlyph = instance.MeasureGlyph(3, 100);
        TextInkBounds bounds = emptyGlyph.GetInkBounds();
        TextShapingResult emptyResult = new TextShapingResult(
            new TextShapingRun[0],
            new FontDiagnostic[0],
            0,
            0,
            0,
            0,
            0);

        Assert.Multiple(() =>
        {
            Assert.That(emptyGlyph.GetXAdvance(), Is.EqualTo(100));
            Assert.That(bounds.IsEmpty(), Is.True);
            Assert.Throws<InvalidOperationException>(() =>
                bounds.GetXMinimum());
            Assert.That(emptyResult.Measure(20).GetInkBounds().IsEmpty(),
                Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.MeasureGlyph(-1, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.MeasureGlyph(0, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                instance.MeasureGlyph(0, double.NaN));
        });
    }

    [Test]
    public void AppliesExplicitGeometryToAdvanceAndExactCurveInk()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        TextStyle style = new TextStyle(100);
        style.SetHorizontalScale(2);
        style.SetBaselineShift(3);

        TextMeasurement upright = instance.MeasureStyledGlyph(0, style);
        style.SetSyntheticSlant(45);
        TextMeasurement slanted = instance.MeasureStyledGlyph(0, style);
        TextInkBounds bounds = slanted.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(style.IsSyntheticSlantRequested(), Is.True);
            Assert.That(style.GetSyntheticSlant(), Is.EqualTo(45));
            Assert.That(slanted.GetPointSize(), Is.EqualTo(100));
            Assert.That(slanted.GetXAdvance(), Is.EqualTo(120));
            Assert.That(slanted.GetYAdvance(), Is.Zero);
            Assert.That(upright.GetInkBounds().GetXMaximum(), Is.EqualTo(4));
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(4.5).Within(0.000000001));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(3));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(4));
        });

        style.ClearSyntheticSlant();
        Assert.Multiple(() =>
        {
            Assert.That(style.IsSyntheticSlantRequested(), Is.False);
            Assert.That(style.GetSyntheticSlant(), Is.Zero);
            Assert.That(instance.MeasureStyledGlyph(0, style)
                .GetInkBounds().GetXMaximum(), Is.EqualTo(4));
        });
    }

    [Test]
    public void AppliesCharacterAndUnicodeSpaceSpacingPerShapedCluster()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        ShapedRun run = Run(
            "a b",
            new int[] { 0, 3, 0 },
            new double[] { 500, 500, 500 },
            new double[] { 0, 0, 0 },
            new double[] { 0, 0, 0 },
            ShapingDirection.LeftToRight);
        TextStyle style = new TextStyle(100);
        style.SetCharacterSpacing(2);
        style.SetWordSpacing(3);
        style.SetHorizontalScale(1.5);

        TextMeasurement measurement = run.MeasureWithStyle(instance, style);
        TextInkBounds bounds = measurement.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(measurement.GetXAdvance(),
                Is.EqualTo(238.5).Within(0.000000001));
            Assert.That(measurement.GetYAdvance(), Is.Zero);
            Assert.That(bounds.GetXMinimum(), Is.Zero);
            Assert.That(bounds.GetXMaximum(),
                Is.EqualTo(163.5).Within(0.000000001));
            Assert.That(bounds.GetYMinimum(), Is.Zero);
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(1));
        });
    }

    [Test]
    public void AppliesVerticalSpacingAndPerpendicularBaselineShift()
    {
        FontInstance instance = FontInstanceTests.CreateFace()
            .CreateDefaultInstance();
        ShapedRun run = Run(
            new int[] { 2 },
            new double[] { 0 },
            new double[] { 0 },
            new double[] { 0 },
            ShapingDirection.TopToBottom);
        TextStyle style = new TextStyle(100);
        style.SetCharacterSpacing(2);
        style.SetHorizontalScale(2);
        style.SetBaselineShift(5);

        TextMeasurement measurement = run.MeasureWithStyle(instance, style);
        TextInkBounds bounds = measurement.GetInkBounds();

        Assert.Multiple(() =>
        {
            Assert.That(measurement.GetXAdvance(), Is.Zero);
            Assert.That(measurement.GetYAdvance(), Is.EqualTo(-102));
            Assert.That(bounds.GetXMinimum(), Is.EqualTo(-73));
            Assert.That(bounds.GetXMaximum(), Is.EqualTo(-71));
            Assert.That(bounds.GetYMinimum(), Is.EqualTo(-99.6));
            Assert.That(bounds.GetYMaximum(), Is.EqualTo(-99.1));
        });
    }

    [Test]
    public void ValidatesStyleValues()
    {
        TextStyle style = new TextStyle(12);

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new TextStyle(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetPointSize(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetCharacterSpacing(double.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetWordSpacing(double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetHorizontalScale(0));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetBaselineShift(double.NegativeInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                style.SetSyntheticSlant(90));
        });
    }

    static TextShapingRun ShapingRun(
        FontInstance instance,
        int glyphId,
        double advance,
        int utf16Start)
    {
        return new TextShapingRun(
            Run(
                new int[] { glyphId },
                new double[] { advance },
                new double[] { 0 },
                new double[] { 0 },
                ShapingDirection.LeftToRight),
            instance,
            "Latn",
            0,
            0,
            0,
            utf16Start,
            1,
            TextShapingPlanKind.Default,
            0);
    }

    static ShapedRun Run(
        int[] glyphIds,
        double[] advances,
        double[] xOffsets,
        double[] yOffsets,
        ShapingDirection direction)
    {
        string text = new string('a', glyphIds.Length);
        return Run(
            text,
            glyphIds,
            advances,
            xOffsets,
            yOffsets,
            direction);
    }

    static ShapedRun Run(
        string text,
        int[] glyphIds,
        double[] advances,
        double[] xOffsets,
        double[] yOffsets,
        ShapingDirection direction)
    {
        RuneBuffer source = Utf16Decoder.Decode(
            text,
            Utf16DecodingMode.Strict,
            text.Length);
        ShapingBuffer buffer = new ShapingBuffer(source, direction);
        SfntGposPositionBuffer positions =
            SfntGposPositionBuffer.Create(glyphIds);
        int index = 0;
        while (index < glyphIds.Length)
        {
            buffer.SetGlyphId(index, glyphIds[index]);
            if (direction == ShapingDirection.TopToBottom ||
                direction == ShapingDirection.BottomToTop)
            {
                positions.ApplyAdjustment(
                    index,
                    new SfntGposAdjustment(
                        xOffsets[index],
                        yOffsets[index],
                        0,
                        direction == ShapingDirection.TopToBottom ?
                            -1000 : 1000));
            }
            else
            {
                positions.ApplyAdjustment(
                    index,
                    new SfntGposAdjustment(
                        xOffsets[index],
                        yOffsets[index],
                        advances[index],
                        0));
            }

            index += 1;
        }

        return ShapedRunBuilder.Build(buffer, source, positions);
    }

    static byte[] BuildEmptyCmap()
    {
        byte[] subtable = new TestBinaryBuilder()
            .WriteUInt16(12)
            .WriteUInt16(0)
            .WriteUInt32(16)
            .WriteUInt32(0)
            .WriteUInt32(0)
            .CopyBytes();
        return new TestBinaryBuilder()
            .WriteUInt16(0)
            .WriteUInt16(1)
            .WriteUInt16(0)
            .WriteUInt16(4)
            .WriteUInt32(12)
            .WriteBytes(subtable)
            .CopyBytes();
    }
}
