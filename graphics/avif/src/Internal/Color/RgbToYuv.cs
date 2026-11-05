using System;

namespace avif;

internal static class RgbToYuv
{
    internal static AvifResultCode Convert(
        AvifPixelBuffer source,
        AvifImage destination,
        AvifColorConversionOptions? options)
    {
        AvifResultCode validation = Validate(
            source, destination, options);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }
        if (options == null)
        {
            return AvifResultCode.InvalidArgument;
        }

        ColorConversionContext context = new ColorConversionContext(
            destination.GetColorInfo());
        if (!context.IsSupported())
        {
            return AvifResultCode.UnsupportedFeature;
        }

        bool hasAlpha = destination.GetPlane(AvifPlaneKind.Alpha) != null;
        AvifChromaFormat workingFormat = destination.GetChromaFormat()
            == AvifChromaFormat.Monochrome
            ? AvifChromaFormat.Monochrome
            : AvifChromaFormat.Yuv444;
        AvifImage full = AvifImage.Create(
            destination.GetWidth(),
            destination.GetHeight(),
            16,
            workingFormat,
            hasAlpha);
        if (full.GetCode() != AvifResultCode.Success)
        {
            return full.GetCode();
        }

        AvifResultCode result = FillFullRange(
            source, full, destination, context, options);
        if (result != AvifResultCode.Success)
        {
            full.Close();
            return result;
        }

        AvifImage sampled = full;
        if (destination.GetChromaFormat() == AvifChromaFormat.Yuv420
            || destination.GetChromaFormat() == AvifChromaFormat.Yuv422)
        {
            sampled = AvifImage.Create(
                destination.GetWidth(),
                destination.GetHeight(),
                16,
                destination.GetChromaFormat(),
                AvifRange.Full,
                destination.GetChromaSamplePosition(),
                hasAlpha ? AvifAlphaMode.Straight : AvifAlphaMode.None,
                1,
                new AvifSecurityLimits());
            result = sampled.GetCode();
            if (result == AvifResultCode.Success)
            {
                result = (sampled.GetPlane(AvifPlaneKind.Y) ?? throw new InvalidOperationException("Invalid internal state.")).CopyFrom(
                    full.GetPlane(AvifPlaneKind.Y));
            }
            if (result == AvifResultCode.Success && hasAlpha)
            {
                result = (sampled.GetPlane(AvifPlaneKind.Alpha) ?? throw new InvalidOperationException("Invalid internal state.")).CopyFrom(
                    full.GetPlane(AvifPlaneKind.Alpha));
            }
            if (result == AvifResultCode.Success)
            {
                result = ChromaResampler.ResampleImageChroma(
                    full, sampled, options.ResamplingFilter);
            }
            if (result != AvifResultCode.Success)
            {
                sampled.Close();
                full.Close();
                return result;
            }
        }

        int sourcePrecision = source.IsFloatingPoint()
            ? 16 : source.GetBitDepth();
        AvifDitherMode ditherMode = destination.GetBitDepth()
                < sourcePrecision
            ? options.DitherMode : AvifDitherMode.None;
        result = BitDepthConverter.ConvertImage(
            sampled, destination, ditherMode);
        if (sampled != full)
        {
            sampled.Close();
        }
        full.Close();
        return result;
    }

    static AvifResultCode FillFullRange(
        AvifPixelBuffer source,
        AvifImage full,
        AvifImage destination,
        ColorConversionContext context,
        AvifColorConversionOptions options)
    {
        AvifPlane? yPlane = full.GetPlane(AvifPlaneKind.Y);
        AvifPlane? uPlane = full.GetPlane(AvifPlaneKind.U);
        AvifPlane? vPlane = full.GetPlane(AvifPlaneKind.V);
        AvifPlane? alphaPlane = full.GetPlane(AvifPlaneKind.Alpha);
        bool direct = ColorConversionContext.UsesDirectComponents(
            context.Matrix);
        AvifIccProfile? iccProfile = options.UseEmbeddedIccProfile
            ? destination.GetIccProfile() : null;
        bool useIcc = iccProfile != null
            && iccProfile.GetChannelCount() == 3
            && iccProfile.CanTransformFromPcs();
        ColorVector d50 = new ColorVector(0.9642, 1, 0.8249);

        int y = 0;
        while (y < source.GetHeight())
        {
            Span<byte> sourceRow = source.GetRowSpan(y);
            Span<byte> yRow = (yPlane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y);
            Span<byte> uRow = uPlane == null
                ? Span<byte>.Empty : uPlane.GetRowSpan(y);
            Span<byte> vRow = vPlane == null
                ? Span<byte>.Empty : vPlane.GetRowSpan(y);
            Span<byte> alphaRow = alphaPlane == null
                ? Span<byte>.Empty : alphaPlane.GetRowSpan(y);
            int x = 0;
            while (x < source.GetWidth())
            {
                PackedColorSample packed = PackedPixelUtility.Read(
                    source, sourceRow, x);
                double alpha = options.PreserveAlpha ? packed.Alpha : 1;
                if (destination.GetAlphaMode() == AvifAlphaMode.Premultiplied)
                {
                    packed.Red *= alpha;
                    packed.Green *= alpha;
                    packed.Blue *= alpha;
                }
                ColorVector linear;
                if (source.IsFloatingPoint())
                {
                    linear = new ColorVector(
                        packed.Red, packed.Green, packed.Blue);
                }
                else
                {
                    linear = new ColorVector(
                        TransferFunctions.ToLinear(
                            packed.Red,
                            AvifTransferCharacteristics.Srgb),
                        TransferFunctions.ToLinear(
                            packed.Green,
                            AvifTransferCharacteristics.Srgb),
                        TransferFunctions.ToLinear(
                            packed.Blue,
                            AvifTransferCharacteristics.Srgb));
                }
                ColorVector? destinationLinear;
                if (useIcc)
                {
                    ColorVector? xyz = ColorTables.ConvertPrimariesToXyz(
                        linear, AvifColorPrimaries.Bt709, d50);
                    double[] device = new double[3];
                    AvifResultCode iccResult = (iccProfile ?? throw new InvalidOperationException("Invalid internal state.")).TransformFromPcs(
                        new double[] { (xyz ?? throw new InvalidOperationException("Invalid internal state.")).First, xyz.Second, xyz.Third },
                        device,
                        iccProfile.GetRenderingIntent());
                    if (iccResult != AvifResultCode.Success)
                    {
                        return iccResult;
                    }
                    destinationLinear = new ColorVector(
                        TransferFunctions.ToLinear(
                            device[0], context.Transfer),
                        TransferFunctions.ToLinear(
                            device[1], context.Transfer),
                        TransferFunctions.ToLinear(
                            device[2], context.Transfer));
                }
                else
                {
                    destinationLinear = ColorTables.ConvertPrimaries(
                        linear, AvifColorPrimaries.Bt709, context.Primaries);
                }
                if (destinationLinear == null)
                {
                    return AvifResultCode.UnsupportedFeature;
                }
                ColorVector? components = ColorMatrixCoefficients
                    .LinearRgbToComponents(
                        destinationLinear,
                        context.Matrix,
                        context.Primaries,
                        context.Transfer);
                if (components == null)
                {
                    return AvifResultCode.UnsupportedFeature;
                }

                WriteSample(yRow, x, QuantizeLuma(components.First));
                if (uPlane != null)
                {
                    WriteSample(
                        uRow,
                        x,
                        direct
                            ? QuantizeLuma(components.Second)
                            : QuantizeChroma(components.Second));
                    WriteSample(
                        vRow,
                        x,
                        direct
                            ? QuantizeLuma(components.Third)
                            : QuantizeChroma(components.Third));
                }
                if (alphaPlane != null)
                {
                    WriteSample(alphaRow, x, QuantizeLuma(alpha));
                }
                x++;
            }
            y++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode Validate(
        AvifPixelBuffer source,
        AvifImage destination,
        AvifColorConversionOptions? options)
    {
        if (source == null || destination == null || options == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode state = source.GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        state = destination.GetCode();
        if (state != AvifResultCode.Success)
        {
            return state;
        }
        if (source.GetWidth() != destination.GetWidth()
            || source.GetHeight() != destination.GetHeight()
            || options.WorkerCount <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        if ((destination.GetColorInfo() ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients
                == AvifMatrixCoefficients.Identity
            && (destination.GetChromaFormat() != AvifChromaFormat.Yuv444
                || destination.GetRange() != AvifRange.Full))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static int QuantizeLuma(double value)
    {
        value = System.Math.Max(0, System.Math.Min(1, value));
        return (int)System.Math.Floor(value * 65535 + 0.5);
    }

    static int QuantizeChroma(double value)
    {
        value = System.Math.Max(-0.5, System.Math.Min(0.5, value));
        return (int)System.Math.Floor((value + 0.5) * 65535 + 0.5);
    }

    static void WriteSample(Span<byte> row, int x, int value)
    {
        int offset = x * 2;
        row[offset] = IntegerConversion.ToByte(value);
        row[offset + 1] = IntegerConversion.ToByte(value >> 8);
    }
}
