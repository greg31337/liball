namespace raw;

internal static class YCbCrConverter
{
    internal static RgbByteImage Convert(BaselineJpegImage source, YCbCrConversionParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A baseline JPEG image is required for YCbCr conversion.", -1);
        }
        if (source.Planes == null || source.Planes.Count != 3)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr conversion requires exactly three baseline JPEG component planes.", -1);
        }
        BaselineJpegPlane y = source.Planes[0];
        BaselineJpegPlane cb = source.Planes[1];
        BaselineJpegPlane cr = source.Planes[2];
        if (y == null || cb == null || cr == null || cb.HorizontalSampling == 0 || cb.VerticalSampling == 0 || cr.HorizontalSampling == 0 || cr.VerticalSampling == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr component sampling is invalid.", -1);
        }
        if (y.HorizontalSampling % cb.HorizontalSampling != 0 || y.VerticalSampling % cb.VerticalSampling != 0 || y.HorizontalSampling % cr.HorizontalSampling != 0 || y.VerticalSampling % cr.VerticalSampling != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr component sampling ratios must be integral.", -1);
        }
        int horizontalSubsampling = y.HorizontalSampling / cb.HorizontalSampling;
        int verticalSubsampling = y.VerticalSampling / cb.VerticalSampling;
        if (horizontalSubsampling != y.HorizontalSampling / cr.HorizontalSampling || verticalSubsampling != y.VerticalSampling / cr.VerticalSampling)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Cb and Cr sampling ratios must match.", -1);
        }

        YCbCrImageData image = new YCbCrImageData();
        image.Width = source.Width;
        image.Height = source.Height;
        image.Precision = source.Precision;
        image.HorizontalSubsampling = horizontalSubsampling;
        image.VerticalSubsampling = verticalSubsampling;
        image.Y = CreateBytePlane(y.Width, y.Height, y.Samples);
        image.Cb = CreateBytePlane(cb.Width, cb.Height, cb.Samples);
        image.Cr = CreateBytePlane(cr.Width, cr.Height, cr.Samples);
        return Convert(image, parameters, limits, cancellationToken);
    }

    internal static RgbByteImage Convert(LosslessJpegImage source, YCbCrConversionParameters parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A lossless JPEG image is required for YCbCr conversion.", -1);
        }
        if (source.Planes == null || source.Planes.Count != 3)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr conversion requires exactly three lossless JPEG component planes.", -1);
        }
        LosslessJpegPlane y = source.Planes[0];
        LosslessJpegPlane cb = source.Planes[1];
        LosslessJpegPlane cr = source.Planes[2];
        if (y == null || cb == null || cr == null || cb.HorizontalSampling == 0 || cb.VerticalSampling == 0 || cr.HorizontalSampling == 0 || cr.VerticalSampling == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr component sampling is invalid.", -1);
        }
        if (y.HorizontalSampling % cb.HorizontalSampling != 0 || y.VerticalSampling % cb.VerticalSampling != 0 || y.HorizontalSampling % cr.HorizontalSampling != 0 || y.VerticalSampling % cr.VerticalSampling != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr component sampling ratios must be integral.", -1);
        }
        int horizontalSubsampling = y.HorizontalSampling / cb.HorizontalSampling;
        int verticalSubsampling = y.VerticalSampling / cb.VerticalSampling;
        if (horizontalSubsampling != y.HorizontalSampling / cr.HorizontalSampling || verticalSubsampling != y.VerticalSampling / cr.VerticalSampling)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Cb and Cr sampling ratios must match.", -1);
        }

        YCbCrImageData image = new YCbCrImageData();
        image.Width = source.Width;
        image.Height = source.Height;
        image.Precision = source.Precision;
        image.HorizontalSubsampling = horizontalSubsampling;
        image.VerticalSubsampling = verticalSubsampling;
        image.Y = CreateWordPlane(y.Width, y.Height, y.Samples);
        image.Cb = CreateWordPlane(cb.Width, cb.Height, cb.Samples);
        image.Cr = CreateWordPlane(cr.Width, cr.Height, cr.Samples);
        return Convert(image, parameters, limits, cancellationToken);
    }

    internal static RgbByteImage Convert(YCbCrImageData? source, YCbCrConversionParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        Validate(source, parameters, limits);
        cancellationToken.ThrowIfCancellationRequested();
        long pixelCount = CheckedMath.PixelCount((source ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width, source.Height, limits);
        long outputLength = CheckedMath.Multiply(pixelCount, 3);
        long inputSamples = CheckedMath.Add((source.Y ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetSampleCount(), (source.Cb ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetSampleCount());
        inputSamples = CheckedMath.Add(inputSamples, (source.Cr ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetSampleCount());
        long totalBytes = CheckedMath.Add(CheckedMath.Multiply(inputSamples, 2), outputLength);
        CheckedMath.DecodedSize(totalBytes, 1, limits);
        CheckedMath.AllocationSize(outputLength, 1, limits);

        RgbByteImage result = new RgbByteImage();
        result.Width = source.Width;
        result.Height = source.Height;
        result.Samples = new byte[CheckedMath.ToInt32(outputLength)];
        double redScale = 2.0 - 2.0 * (parameters ?? throw new System.InvalidOperationException("A required value is unavailable.")).RedLumaWeight;
        double blueScale = 2.0 - 2.0 * parameters.BlueLumaWeight;
        double greenDenominator = 1.0 - parameters.RedLumaWeight - parameters.BlueLumaWeight;
        double greenCbScale = parameters.BlueLumaWeight * blueScale / greenDenominator;
        double greenCrScale = parameters.RedLumaWeight * redScale / greenDenominator;

        int y = 0;
        while (y < source.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < source.Width)
            {
                int sourceY = source.Y.GetSample(y * source.Y.Width + x);
                double luma = (sourceY - parameters.YBlack) * 255.0 / (parameters.YWhite - parameters.YBlack);
                double cbSample = SampleChroma(source.Cb, x, y, source.HorizontalSubsampling, source.VerticalSubsampling, parameters.Positioning, parameters.Upsampling);
                double crSample = SampleChroma(source.Cr, x, y, source.HorizontalSubsampling, source.VerticalSubsampling, parameters.Positioning, parameters.Upsampling);
                double cb = (cbSample - parameters.CbBlack) * 127.0 / (parameters.CbWhite - parameters.CbBlack);
                double cr = (crSample - parameters.CrBlack) * 127.0 / (parameters.CrWhite - parameters.CrBlack);
                int outputOffset = (y * source.Width + x) * 3;
                result.Samples[outputOffset] = Clip(luma + redScale * cr);
                result.Samples[outputOffset + 1] = Clip(luma - greenCbScale * cb - greenCrScale * cr);
                result.Samples[outputOffset + 2] = Clip(luma + blueScale * cb);
                x++;
            }
            y++;
        }
        return result;
    }

    internal static void Validate(YCbCrImageData? source, YCbCrConversionParameters? parameters, RawLimits limits)
    {
        if (source == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr image data is required.", -1);
        }
        if (parameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr conversion parameters are required.", -1);
        }
        CheckedMath.RequireLimits(limits);
        if (!IsFinite(parameters.RedLumaWeight) || !IsFinite(parameters.BlueLumaWeight) || parameters.RedLumaWeight <= 0.0 || parameters.BlueLumaWeight <= 0.0 || parameters.RedLumaWeight + parameters.BlueLumaWeight >= 1.0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr luma weights must be finite, positive, and sum to less than one.", -1);
        }
        if (!IsFinite(parameters.YBlack) || !IsFinite(parameters.YWhite) || !IsFinite(parameters.CbBlack) || !IsFinite(parameters.CbWhite) || !IsFinite(parameters.CrBlack) || !IsFinite(parameters.CrWhite) || parameters.YWhite <= parameters.YBlack || parameters.CbWhite <= parameters.CbBlack || parameters.CrWhite <= parameters.CrBlack)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr reference ranges must be finite and increasing.", -1);
        }
        if (parameters.Positioning != YCbCrChromaPositioning.Centered && parameters.Positioning != YCbCrChromaPositioning.Cosited)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr chroma positioning is invalid.", -1);
        }
        if (parameters.Upsampling != YCbCrUpsamplingMode.NearestNeighbor && parameters.Upsampling != YCbCrUpsamplingMode.Bilinear)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "YCbCr upsampling mode is invalid.", -1);
        }
        if (source.Precision < 1 || source.Precision > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr precision must be between one and 16 bits.", -1);
        }
        CheckedMath.PixelCount(source.Width, source.Height, limits);
        if (source.HorizontalSubsampling < 1 || source.HorizontalSubsampling > 4 || source.VerticalSubsampling < 1 || source.VerticalSubsampling > 4)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr subsampling factors must be between one and four.", -1);
        }
        if (source.Y == null || source.Cb == null || source.Cr == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "YCbCr conversion requires Y, Cb, and Cr planes.", -1);
        }
        int chromaWidth = DivideRoundUp(source.Width, source.HorizontalSubsampling);
        int chromaHeight = DivideRoundUp(source.Height, source.VerticalSubsampling);
        ValidatePlane(source.Y, source.Width, source.Height, source.Precision, limits);
        ValidatePlane(source.Cb, chromaWidth, chromaHeight, source.Precision, limits);
        ValidatePlane(source.Cr, chromaWidth, chromaHeight, source.Precision, limits);
    }

    internal static void ValidatePlane(YCbCrPlaneData plane, int expectedWidth, int expectedHeight, int precision, RawLimits limits)
    {
        if (plane.Width != expectedWidth || plane.Height != expectedHeight)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A YCbCr plane does not match its sampling geometry.", -1);
        }
        long sampleCount = CheckedMath.Multiply(plane.Width, plane.Height);
        bool bytes = plane.ByteSamples != null;
        bool words = plane.WordSamples != null;
        if (bytes == words || bytes && (plane.ByteSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length != sampleCount || words && (plane.WordSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length != sampleCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A YCbCr plane must contain exactly one correctly sized sample array.", -1);
        }
        CheckedMath.AllocationSize(sampleCount, bytes ? 1 : 2, limits);
        int maximum = precision == 16 ? 65535 : (1 << precision) - 1;
        int index = 0;
        while (index < sampleCount)
        {
            if (plane.GetSample(index) > maximum)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A YCbCr sample exceeds the declared precision.", -1);
            }
            index++;
        }
    }

    internal static double SampleChroma(YCbCrPlaneData plane, int outputX, int outputY, int horizontalSubsampling, int verticalSubsampling, YCbCrChromaPositioning positioning, YCbCrUpsamplingMode upsampling)
    {
        double sourceX;
        double sourceY;
        if (positioning == YCbCrChromaPositioning.Centered)
        {
            sourceX = (outputX + 0.5) / horizontalSubsampling - 0.5;
            sourceY = (outputY + 0.5) / verticalSubsampling - 0.5;
        }
        else
        {
            sourceX = (double)outputX / horizontalSubsampling;
            sourceY = (double)outputY / verticalSubsampling;
        }
        if (upsampling == YCbCrUpsamplingMode.NearestNeighbor)
        {
            int nearestX = ClampIndex((int)System.Math.Floor(sourceX + 0.5), plane.Width);
            int nearestY = ClampIndex((int)System.Math.Floor(sourceY + 0.5), plane.Height);
            return plane.GetSample(nearestY * plane.Width + nearestX);
        }

        int left = (int)System.Math.Floor(sourceX);
        int top = (int)System.Math.Floor(sourceY);
        double horizontalFraction = sourceX - left;
        double verticalFraction = sourceY - top;
        int x0 = ClampIndex(left, plane.Width);
        int x1 = ClampIndex(left + 1, plane.Width);
        int y0 = ClampIndex(top, plane.Height);
        int y1 = ClampIndex(top + 1, plane.Height);
        double topValue = plane.GetSample(y0 * plane.Width + x0) * (1.0 - horizontalFraction) + plane.GetSample(y0 * plane.Width + x1) * horizontalFraction;
        double bottomValue = plane.GetSample(y1 * plane.Width + x0) * (1.0 - horizontalFraction) + plane.GetSample(y1 * plane.Width + x1) * horizontalFraction;
        return topValue * (1.0 - verticalFraction) + bottomValue * verticalFraction;
    }

    internal static byte Clip(double value)
    {
        int rounded = (int)System.Math.Floor(value + 0.5);
        if (rounded < 0)
        {
            return 0;
        }
        if (rounded > 255)
        {
            return 255;
        }
        return (byte)rounded;
    }

    internal static int ClampIndex(int value, int length)
    {
        if (value < 0)
        {
            return 0;
        }
        if (value >= length)
        {
            return length - 1;
        }
        return value;
    }

    internal static bool IsFinite(double value)
    {
        return !double.IsNaN(value) && !double.IsInfinity(value);
    }

    internal static int DivideRoundUp(int value, int divisor)
    {
        if (value < 0 || divisor <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "YCbCr dimensions and sampling divisors must be valid.", -1);
        }
        return CheckedMath.ToInt32(CheckedMath.Add(value, divisor - 1) / divisor);
    }

    internal static YCbCrPlaneData CreateBytePlane(int width, int height, byte[]? samples)
    {
        YCbCrPlaneData plane = new YCbCrPlaneData();
        plane.Width = width;
        plane.Height = height;
        plane.ByteSamples = samples;
        plane.WordSamples = null;
        return plane;
    }

    internal static YCbCrPlaneData CreateWordPlane(int width, int height, ushort[]? samples)
    {
        YCbCrPlaneData plane = new YCbCrPlaneData();
        plane.Width = width;
        plane.Height = height;
        plane.ByteSamples = null;
        plane.WordSamples = samples;
        return plane;
    }
}
