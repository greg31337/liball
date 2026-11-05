using System;

namespace avif;

internal class Av1PixelFormatDescription
{
    internal int Profile;
    internal int BitDepth;
    internal bool Monochrome;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int PlaneCount;
    internal int BytesPerSample;
}

internal class Av1PixelComparisonResult
{
    internal AvifResultCode Code;
    internal bool Matches;
    internal long ComparedSampleCount;
    internal int Plane;
    internal int X;
    internal int Y;
    internal int Expected;
    internal int Actual;

    internal Av1PixelComparisonResult(AvifResultCode code)
    {
        Code = code;
        Matches = code == AvifResultCode.Success;
        ComparedSampleCount = 0;
        Plane = -1;
        X = -1;
        Y = -1;
        Expected = -1;
        Actual = -1;
    }
}

// Exact decoded-plane validation used by decode wrap-up and differential tests.
internal static class Av1PixelConformance
{
    internal static Av1PixelFormatDescription? Describe(
        ParsedAv1SequenceHeader? sequence)
    {
        if (sequence == null)
        {
            return null;
        }

        int profile = sequence.GetSequenceProfile();
        int bitDepth = sequence.GetBitDepth();
        bool monochrome = sequence.IsMonochrome();
        bool subsamplingX = sequence.HasChromaSubsamplingX();
        bool subsamplingY = sequence.HasChromaSubsamplingY();
        if (!IsLegalFormat(
            profile,
            bitDepth,
            monochrome,
            subsamplingX,
            subsamplingY))
        {
            return null;
        }

        Av1PixelFormatDescription description =
            new Av1PixelFormatDescription();
        description.Profile = profile;
        description.BitDepth = bitDepth;
        description.Monochrome = monochrome;
        description.SubsamplingX = subsamplingX;
        description.SubsamplingY = subsamplingY;
        description.PlaneCount = monochrome ? 1 : 3;
        description.BytesPerSample = bitDepth > 8 ? 2 : 1;
        return description;
    }

    internal static AvifResultCode Validate(
        ParsedAv1SequenceHeader? sequence,
        int width,
        int height,
        PlaneBuffer[] planes)
    {
        if (sequence == null || width <= 0 || height <= 0
            || planes == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1PixelFormatDescription? format = Describe(sequence);
        if (format == null)
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }
        if (!HasValidLayout(format, width, height, planes))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (!HasValidSamples(format, planes))
        {
            return AvifResultCode.NonConformingData;
        }
        return AvifResultCode.Success;
    }

    internal static Av1PixelComparisonResult Compare(
        ParsedAv1SequenceHeader? sequence,
        int width,
        int height,
        PlaneBuffer[] expected,
        PlaneBuffer[] actual)
    {
        AvifResultCode expectedCode = Validate(
            sequence, width, height, expected);
        if (expectedCode != AvifResultCode.Success)
        {
            return new Av1PixelComparisonResult(expectedCode);
        }
        AvifResultCode actualCode = Validate(
            sequence, width, height, actual);
        if (actualCode != AvifResultCode.Success)
        {
            return new Av1PixelComparisonResult(actualCode);
        }

        Av1PixelFormatDescription? format = Describe(sequence);
        Av1PixelComparisonResult result = new Av1PixelComparisonResult(
            AvifResultCode.Success);
        int plane = 0;
        while (plane < (format ?? throw new InvalidOperationException("Invalid internal state.")).PlaneCount)
        {
            int y = 0;
            while (y < expected[plane].Height)
            {
                Span<byte> expectedRow = expected[plane].GetRowSpan(y);
                Span<byte> actualRow = actual[plane].GetRowSpan(y);
                int x = 0;
                while (x < expected[plane].Width)
                {
                    int expectedValue = ReadSample(
                        expectedRow, x, format.BytesPerSample);
                    int actualValue = ReadSample(
                        actualRow, x, format.BytesPerSample);
                    result.ComparedSampleCount++;
                    if (expectedValue != actualValue)
                    {
                        result.Code = AvifResultCode.NonConformingData;
                        result.Matches = false;
                        result.Plane = plane;
                        result.X = x;
                        result.Y = y;
                        result.Expected = expectedValue;
                        result.Actual = actualValue;
                        return result;
                    }
                    x++;
                }
                y++;
            }
            plane++;
        }
        return result;
    }

    internal static bool IsLegalFormat(
        int profile,
        int bitDepth,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY)
    {
        if (bitDepth != 8 && bitDepth != 10 && bitDepth != 12)
        {
            return false;
        }
        if (!monochrome && !subsamplingX && subsamplingY)
        {
            return false;
        }
        if (profile == 0)
        {
            return bitDepth != 12
                && (monochrome || subsamplingX && subsamplingY);
        }
        if (profile == 1)
        {
            return bitDepth != 12 && !monochrome
                && !subsamplingX && !subsamplingY;
        }
        if (profile != 2)
        {
            return false;
        }
        if (monochrome || bitDepth == 12)
        {
            return true;
        }
        return subsamplingX && !subsamplingY;
    }

    static bool HasValidLayout(
        Av1PixelFormatDescription format,
        int width,
        int height,
        PlaneBuffer[] planes)
    {
        if (planes.Length != format.PlaneCount)
        {
            return false;
        }
        int plane = 0;
        while (plane < planes.Length)
        {
            int shiftX = plane == 0 || !format.SubsamplingX ? 0 : 1;
            int shiftY = plane == 0 || !format.SubsamplingY ? 0 : 1;
            int planeWidth = (width + shiftX) >> shiftX;
            int planeHeight = (height + shiftY) >> shiftY;
            PlaneBuffer buffer = planes[plane];
            if (buffer == null || buffer.Closed
                || buffer.Code != AvifResultCode.Success
                || buffer.Width != planeWidth
                || buffer.Height != planeHeight
                || buffer.BytesPerSample != format.BytesPerSample)
            {
                return false;
            }
            plane++;
        }
        return true;
    }

    static bool HasValidSamples(
        Av1PixelFormatDescription format,
        PlaneBuffer[] planes)
    {
        int maximum = (1 << format.BitDepth) - 1;
        int plane = 0;
        while (plane < planes.Length)
        {
            int y = 0;
            while (y < planes[plane].Height)
            {
                Span<byte> row = planes[plane].GetRowSpan(y);
                int x = 0;
                while (x < planes[plane].Width)
                {
                    if (ReadSample(row, x, format.BytesPerSample) > maximum)
                    {
                        return false;
                    }
                    x++;
                }
                y++;
            }
            plane++;
        }
        return true;
    }

    static int ReadSample(
        Span<byte> row,
        int x,
        int bytesPerSample)
    {
        int offset = x * bytesPerSample;
        return bytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }
}
