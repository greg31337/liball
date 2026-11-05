namespace jpeg.Tests;

internal class McuLayoutCalculatorTests
{
    [Test]
    public void Layout_CalculatesStandardFourTwoZeroMcuGrid()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();

        global::jpeg.JpegMcuLayout layout = calculator.Calculate(frame);

        Assert.That(layout.Width, Is.EqualTo(32));
        Assert.That(layout.Height, Is.EqualTo(16));
        Assert.That(layout.ComponentCount, Is.EqualTo(3));
        Assert.That(layout.MaxHorizontalSamplingFactor, Is.EqualTo(2));
        Assert.That(layout.MaxVerticalSamplingFactor, Is.EqualTo(2));
        Assert.That(layout.McuWidth, Is.EqualTo(16));
        Assert.That(layout.McuHeight, Is.EqualTo(16));
        Assert.That(layout.McusPerRow, Is.EqualTo(2));
        Assert.That(layout.McuRows, Is.EqualTo(1));
        Assert.That(layout.McuCount, Is.EqualTo(2));
        Assert.That(layout.BlocksPerMcu, Is.EqualTo(6));

        AssertComponent(layout.Components[0], 0, 1, 2, 2, 32, 16, 4, 2, 8, 0, 4);
        AssertComponent(layout.Components[1], 1, 2, 1, 1, 16, 8, 2, 1, 2, 4, 1);
        AssertComponent(layout.Components[2], 2, 3, 1, 1, 16, 8, 2, 1, 2, 5, 1);
    }

    [Test]
    public void Layout_CeilsPartialMcuAndComponentBlocks()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Width = 17;
        frame.Height = 17;
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();

        global::jpeg.JpegMcuLayout layout = calculator.Calculate(frame);

        Assert.That(layout.McusPerRow, Is.EqualTo(2));
        Assert.That(layout.McuRows, Is.EqualTo(2));
        Assert.That(layout.McuCount, Is.EqualTo(4));
        Assert.That(layout.Components[0].SampleWidth, Is.EqualTo(17));
        Assert.That(layout.Components[0].SampleHeight, Is.EqualTo(17));
        Assert.That(layout.Components[0].BlocksPerRow, Is.EqualTo(3));
        Assert.That(layout.Components[0].BlockRows, Is.EqualTo(3));
        Assert.That(layout.Components[0].PaddedSampleWidth, Is.EqualTo(24));
        Assert.That(layout.Components[0].PaddedSampleHeight, Is.EqualTo(24));
        Assert.That(layout.Components[1].SampleWidth, Is.EqualTo(9));
        Assert.That(layout.Components[1].SampleHeight, Is.EqualTo(9));
        Assert.That(layout.Components[1].BlocksPerRow, Is.EqualTo(2));
        Assert.That(layout.Components[1].BlockRows, Is.EqualTo(2));
    }

    [Test]
    public void Layout_CalculatesArbitraryLegalSamplingFactors()
    {
        global::jpeg.JpegFrameHeader frame = CreateFrame(35, 25);
        frame.Components.Add(FrameHeaderWriterTests.CreateComponent(1, 4, 3, 0));
        frame.Components.Add(FrameHeaderWriterTests.CreateComponent(2, 3, 2, 1));
        frame.Components.Add(FrameHeaderWriterTests.CreateComponent(3, 1, 1, 1));
        frame.ComponentCount = 3;
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();

        global::jpeg.JpegMcuLayout layout = calculator.Calculate(frame);

        Assert.That(layout.MaxHorizontalSamplingFactor, Is.EqualTo(4));
        Assert.That(layout.MaxVerticalSamplingFactor, Is.EqualTo(3));
        Assert.That(layout.McuWidth, Is.EqualTo(32));
        Assert.That(layout.McuHeight, Is.EqualTo(24));
        Assert.That(layout.McusPerRow, Is.EqualTo(2));
        Assert.That(layout.McuRows, Is.EqualTo(2));
        Assert.That(layout.BlocksPerMcu, Is.EqualTo(19));
        AssertComponent(layout.Components[0], 0, 1, 4, 3, 35, 25, 5, 4, 20, 0, 12);
        AssertComponent(layout.Components[1], 1, 2, 3, 2, 27, 17, 4, 3, 12, 12, 6);
        AssertComponent(layout.Components[2], 2, 3, 1, 1, 9, 9, 2, 2, 4, 18, 1);
    }

    [Test]
    public void Layout_AllowsZeroHeightDnlCompatibleFrame()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Height = 0;
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();

        global::jpeg.JpegMcuLayout layout = calculator.Calculate(frame);

        Assert.That(layout.McusPerRow, Is.EqualTo(2));
        Assert.That(layout.McuRows, Is.EqualTo(0));
        Assert.That(layout.McuCount, Is.EqualTo(0));
        Assert.That(layout.Components[0].SampleHeight, Is.EqualTo(0));
        Assert.That(layout.Components[0].BlockRows, Is.EqualTo(0));
        Assert.That(layout.Components[0].TotalBlockCount, Is.EqualTo(0));
    }

    [Test]
    public void Layout_FindsComponentsAndExposesMathHelpers()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();

        global::jpeg.JpegMcuLayout layout = calculator.Calculate(frame);
        global::jpeg.JpegMcuComponentLayout component = calculator.FindComponentLayout(layout, 2);

        Assert.That(component.Identifier, Is.EqualTo(2));
        Assert.That(component.McuBlockOffset, Is.EqualTo(4));
        Assert.That(calculator.DivideRoundUp(17, 8), Is.EqualTo(3));
        Assert.That(calculator.ScaleDimension(35, 3, 4), Is.EqualTo(27));
        Assert.That(calculator.FindMaxHorizontalSamplingFactor(frame), Is.EqualTo(2));
        Assert.That(calculator.FindMaxVerticalSamplingFactor(frame), Is.EqualTo(2));
    }

    [Test]
    public void Layout_RejectsInvalidFramesComponentsAndMath()
    {
        global::jpeg.JpegMcuLayoutCalculator calculator = new global::jpeg.JpegMcuLayoutCalculator();
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                calculator.Calculate(null!);
            });

        global::jpeg.JpegFrameHeader badWidth = FrameHeaderWriterTests.CreateBaselineFrame();
        badWidth.Width = 0;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                calculator.Calculate(badWidth);
            });

        global::jpeg.JpegFrameHeader mismatch = FrameHeaderWriterTests.CreateBaselineFrame();
        mismatch.ComponentCount = 2;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                calculator.Calculate(mismatch);
            });

        global::jpeg.JpegFrameHeader badSampling = FrameHeaderWriterTests.CreateBaselineFrame();
        badSampling.Components[0].VerticalSamplingFactor = 5;
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                calculator.Calculate(badSampling);
            });

        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                calculator.FindComponentLayout(calculator.Calculate(frame), 99);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                calculator.DivideRoundUp(-1, 8);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                calculator.ScaleDimension(16, 3, 2);
            });
    }

    public static global::jpeg.JpegFrameHeader CreateFrame(int width, int height)
    {
        global::jpeg.JpegFrameHeader frame = new global::jpeg.JpegFrameHeader();
        frame.Marker = global::jpeg.JpegMarker.StartOfFrame0;
        frame.Value = global::jpeg.JpegConstants.MarkerStartOfFrame0;
        frame.Process = global::jpeg.JpegProcess.BaselineDct;
        frame.Precision = 8;
        frame.Width = width;
        frame.Height = height;
        return frame;
    }

    public static void AssertComponent(
        global::jpeg.JpegMcuComponentLayout component,
        int componentIndex,
        int identifier,
        int horizontal,
        int vertical,
        int sampleWidth,
        int sampleHeight,
        int blocksPerRow,
        int blockRows,
        int totalBlockCount,
        int mcuBlockOffset,
        int mcuBlockCount)
    {
        Assert.That(component.ComponentIndex, Is.EqualTo(componentIndex));
        Assert.That(component.Identifier, Is.EqualTo(identifier));
        Assert.That(component.HorizontalSamplingFactor, Is.EqualTo(horizontal));
        Assert.That(component.VerticalSamplingFactor, Is.EqualTo(vertical));
        Assert.That(component.SampleWidth, Is.EqualTo(sampleWidth));
        Assert.That(component.SampleHeight, Is.EqualTo(sampleHeight));
        Assert.That(component.BlocksPerRow, Is.EqualTo(blocksPerRow));
        Assert.That(component.BlockRows, Is.EqualTo(blockRows));
        Assert.That(component.PaddedSampleWidth, Is.EqualTo(blocksPerRow * global::jpeg.JpegConstants.BlockWidth));
        Assert.That(component.PaddedSampleHeight, Is.EqualTo(blockRows * global::jpeg.JpegConstants.BlockHeight));
        Assert.That(component.TotalBlockCount, Is.EqualTo(totalBlockCount));
        Assert.That(component.McuBlockOffset, Is.EqualTo(mcuBlockOffset));
        Assert.That(component.McuBlockCount, Is.EqualTo(mcuBlockCount));
    }
}
