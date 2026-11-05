namespace jpeg.Tests;

internal class EdgePaddingTests
{
    [Test]
    public void Padding_ReplicatesRightAndBottomEdgesForBytePlanes()
    {
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();
        byte[] source = new byte[]
        {
            1, 2, 3,
            4, 5, 6
        };

        byte[] padded = padding.PadBytePlane(source, 3, 2, 5, 4);

        Assert.That(padded, Is.EqualTo(new byte[]
        {
            1, 2, 3, 3, 3,
            4, 5, 6, 6, 6,
            4, 5, 6, 6, 6,
            4, 5, 6, 6, 6
        }));
        Assert.That(padding.GetClampedSourceIndex(4, 3, 3, 2), Is.EqualTo(5));
    }

    [Test]
    public void Padding_ReplicatesRightAndBottomEdgesForIntPlanes()
    {
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();
        int[] source = new int[]
        {
            10, 20,
            30, 40,
            50, 60
        };

        int[] padded = padding.PadIntPlane(source, 2, 3, 4, 5);

        Assert.That(padded, Is.EqualTo(new int[]
        {
            10, 20, 20, 20,
            30, 40, 40, 40,
            50, 60, 60, 60,
            50, 60, 60, 60,
            50, 60, 60, 60
        }));
    }

    [Test]
    public void Padding_ReturnsCopyWhenNoPaddingIsRequired()
    {
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();
        byte[] source = new byte[] { 1, 2, 3, 4 };

        byte[] padded = padding.PadBytePlane(source, 2, 2, 2, 2);

        Assert.That(padded, Is.EqualTo(source));
        Assert.That(object.ReferenceEquals(source, padded), Is.False);
    }

    [Test]
    public void Padding_PadsUsingMcuComponentLayout()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Width = 17;
        frame.Height = 17;
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(frame);
        global::jpeg.JpegMcuComponentLayout component = layout.Components[1];
        byte[] source = CreateSequentialBytePlane(component.SampleWidth, component.SampleHeight);
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();

        byte[] padded = padding.PadByteComponent(source, component);

        Assert.That(component.SampleWidth, Is.EqualTo(9));
        Assert.That(component.SampleHeight, Is.EqualTo(9));
        Assert.That(component.PaddedSampleWidth, Is.EqualTo(16));
        Assert.That(component.PaddedSampleHeight, Is.EqualTo(16));
        Assert.That(padded.Length, Is.EqualTo(256));
        Assert.That(padded[0], Is.EqualTo(source[0]));
        Assert.That(padded[8], Is.EqualTo(source[8]));
        Assert.That(padded[15], Is.EqualTo(source[8]));
        Assert.That(padded[15 * 16], Is.EqualTo(source[8 * 9]));
        Assert.That(padded[15 * 16 + 15], Is.EqualTo(source[8 * 9 + 8]));
    }

    [Test]
    public void Padding_AllowsZeroHeightLayoutWithEmptyOutput()
    {
        global::jpeg.JpegFrameHeader frame = FrameHeaderWriterTests.CreateBaselineFrame();
        frame.Height = 0;
        global::jpeg.JpegMcuLayout layout = new global::jpeg.JpegMcuLayoutCalculator().Calculate(frame);
        global::jpeg.JpegMcuComponentLayout component = layout.Components[0];
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();

        byte[] padded = padding.PadByteComponent(new byte[0], component);

        Assert.That(component.SampleWidth, Is.EqualTo(32));
        Assert.That(component.SampleHeight, Is.EqualTo(0));
        Assert.That(component.PaddedSampleHeight, Is.EqualTo(0));
        Assert.That(padded.Length, Is.EqualTo(0));
    }

    [Test]
    public void Padding_CopyMethodsWriteOnlyPaddedPlane()
    {
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();
        byte[] source = new byte[] { 1, 2, 3, 4 };
        byte[] destination = new byte[10];
        destination[9] = 99;

        padding.CopyPaddedBytePlane(source, 2, 2, 3, 3, destination);

        Assert.That(destination[0], Is.EqualTo(1));
        Assert.That(destination[2], Is.EqualTo(2));
        Assert.That(destination[6], Is.EqualTo(3));
        Assert.That(destination[8], Is.EqualTo(4));
        Assert.That(destination[9], Is.EqualTo(99));
    }

    [Test]
    public void Padding_RejectsInvalidInputs()
    {
        global::jpeg.JpegEdgePadding padding = new global::jpeg.JpegEdgePadding();

        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                padding.PadBytePlane(null!, 1, 1, 1, 1);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                padding.PadBytePlane(new byte[1], -1, 1, 1, 1);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                padding.PadBytePlane(new byte[4], 2, 2, 1, 2);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                padding.PadBytePlane(new byte[1], 2, 2, 2, 2);
            });
        Assert.Throws<global::jpeg.JpegInvalidDataException>(
            delegate
            {
                padding.PadBytePlane(new byte[0], 0, 0, 1, 1);
            });
        Assert.Throws<System.ArgumentOutOfRangeException>(
            delegate
            {
                padding.GetClampedSourceIndex(-1, 0, 1, 1);
            });
        Assert.Throws<System.ArgumentNullException>(
            delegate
            {
                padding.PadIntComponent(new int[0], null!);
            });
    }

    public static byte[] CreateSequentialBytePlane(int width, int height)
    {
        byte[] samples = new byte[width * height];
        int index = 0;
        while (index < samples.Length)
        {
            samples[index] = (byte)(index + 1);
            index = index + 1;
        }

        return samples;
    }
}
