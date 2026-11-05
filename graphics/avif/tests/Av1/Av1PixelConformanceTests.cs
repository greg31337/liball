namespace avif.Tests;

public class Av1PixelConformanceTests
{
    [Test]
    public void ExactComparisonCoversOddSubsampledTenBitPlanes()
    {
        ParsedAv1SequenceHeader sequence = CreateSequence(
            0, 10, false, true, true);
        PlaneBuffer[] expected = CreatePlanes(5, 3, 2, true, true);
        FillPlanes(expected, 10);
        PlaneBuffer[] actual = CopyPlanes(expected);

        Av1PixelComparisonResult exact = Av1PixelConformance.Compare(
            sequence, 5, 3, expected, actual);
        SetSample(actual[1], 2, 1, 511);
        Av1PixelComparisonResult mismatch = Av1PixelConformance.Compare(
            sequence, 5, 3, expected, actual);

        Assert.Multiple(() =>
        {
            Assert.That(exact.Code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(exact.Matches, Is.True);
            Assert.That(exact.ComparedSampleCount, Is.EqualTo(27));
            Assert.That(mismatch.Code,
                Is.EqualTo(AvifResultCode.NonConformingData));
            Assert.That(mismatch.Matches, Is.False);
            Assert.That(mismatch.Plane, Is.EqualTo(1));
            Assert.That(mismatch.X, Is.EqualTo(2));
            Assert.That(mismatch.Y, Is.EqualTo(1));
            Assert.That(mismatch.Expected, Is.Not.EqualTo(mismatch.Actual));
        });

        ClosePlanes(expected);
        ClosePlanes(actual);
    }

    [Test]
    public void ValidationRejectsIllegalFormatsLayoutsAndSampleRanges()
    {
        ParsedAv1SequenceHeader illegalProfile = CreateSequence(
            1, 10, false, true, true);
        ParsedAv1SequenceHeader illegalSubsampling = CreateSequence(
            2, 12, false, false, true);
        ParsedAv1SequenceHeader valid = CreateSequence(
            0, 10, false, true, true);
        PlaneBuffer[] planes = CreatePlanes(4, 4, 2, true, true);
        FillPlanes(planes, 10);
        PlaneBuffer[] wrongLayout = new PlaneBuffer[]
        {
            new PlaneBuffer(4, 4, 2, 1, 1024, true)
        };

        AvifResultCode formatCode = Av1PixelConformance.Validate(
            illegalProfile, 4, 4, planes);
        AvifResultCode layoutCode = Av1PixelConformance.Validate(
            valid, 4, 4, wrongLayout);
        AvifResultCode subsamplingCode = Av1PixelConformance.Validate(
            illegalSubsampling, 4, 4, planes);
        SetSample(planes[2], 0, 0, 1024);
        AvifResultCode sampleCode = Av1PixelConformance.Validate(
            valid, 4, 4, planes);

        Assert.Multiple(() =>
        {
            Assert.That(formatCode,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(layoutCode,
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(subsamplingCode,
                Is.EqualTo(AvifResultCode.InvalidAv1Bitstream));
            Assert.That(sampleCode,
                Is.EqualTo(AvifResultCode.NonConformingData));
        });

        ClosePlanes(planes);
        ClosePlanes(wrongLayout);
    }

    static ParsedAv1SequenceHeader CreateSequence(
        int profile,
        int bitDepth,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY)
    {
        Av1SequenceFeatures features = new Av1SequenceFeatures();
        return new ParsedAv1SequenceHeader(
            profile,
            true,
            true,
            null,
            null,
            false,
            new Av1OperatingPoint[] { new Av1OperatingPoint(0, 4, 0) },
            64,
            64,
            features,
            bitDepth > 8,
            bitDepth == 12,
            monochrome,
            subsamplingX,
            subsamplingY,
            subsamplingX && subsamplingY ? 1 : 0,
            2,
            2,
            2,
            true,
            false,
            false,
            false,
            System.Array.Empty<byte>());
    }

    static PlaneBuffer[] CreatePlanes(
        int width,
        int height,
        int bytesPerSample,
        bool subsamplingX,
        bool subsamplingY)
    {
        return new PlaneBuffer[]
        {
            new PlaneBuffer(width, height, bytesPerSample, 1, 4096, true),
            new PlaneBuffer(
                (width + (subsamplingX ? 1 : 0))
                    >> (subsamplingX ? 1 : 0),
                (height + (subsamplingY ? 1 : 0))
                    >> (subsamplingY ? 1 : 0),
                bytesPerSample,
                1,
                4096,
                true),
            new PlaneBuffer(
                (width + (subsamplingX ? 1 : 0))
                    >> (subsamplingX ? 1 : 0),
                (height + (subsamplingY ? 1 : 0))
                    >> (subsamplingY ? 1 : 0),
                bytesPerSample,
                1,
                4096,
                true)
        };
    }

    static PlaneBuffer[] CopyPlanes(PlaneBuffer[] source)
    {
        PlaneBuffer[] copy = new PlaneBuffer[source.Length];
        for (int plane = 0; plane < source.Length; plane++)
        {
            copy[plane] = source[plane].CreateCopy();
        }
        return copy;
    }

    static void FillPlanes(PlaneBuffer[] planes, int bitDepth)
    {
        int maximum = (1 << bitDepth) - 1;
        for (int plane = 0; plane < planes.Length; plane++)
        {
            for (int y = 0; y < planes[plane].Height; y++)
            {
                for (int x = 0; x < planes[plane].Width; x++)
                {
                    SetSample(
                        planes[plane],
                        x,
                        y,
                        (plane * 97 + y * 31 + x * 17) & maximum);
                }
            }
        }
    }

    static void SetSample(
        PlaneBuffer plane,
        int x,
        int y,
        int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = (byte)(value >> 8);
        }
    }

    static void ClosePlanes(PlaneBuffer[] planes)
    {
        for (int plane = 0; plane < planes.Length; plane++)
        {
            planes[plane].Close();
        }
    }
}
