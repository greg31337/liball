using System;

namespace avif;

internal static class YuvToRgb
{
    internal static AvifResultCode Convert(
        AvifImage source,
        AvifPixelBuffer destination,
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
            source.GetColorInfo());
        if (!context.IsSupported())
        {
            return AvifResultCode.UnsupportedFeature;
        }

        AvifPlane? expandedU = null;
        AvifPlane? expandedV = null;
        AvifPlane? sourceU = source.GetPlane(AvifPlaneKind.U);
        AvifPlane? sourceV = source.GetPlane(AvifPlaneKind.V);
        if (source.GetChromaFormat() != AvifChromaFormat.Monochrome
            && source.GetChromaFormat() != AvifChromaFormat.Yuv444)
        {
            expandedU = AvifPlane.Create(
                source.GetWidth(), source.GetHeight(),
                source.GetBitDepth(), AvifPlaneKind.U);
            expandedV = AvifPlane.Create(
                source.GetWidth(), source.GetHeight(),
                source.GetBitDepth(), AvifPlaneKind.V);
            AvifResultCode allocation = expandedU.GetCode();
            if (allocation == AvifResultCode.Success)
            {
                allocation = expandedV.GetCode();
            }
            if (allocation == AvifResultCode.Success)
            {
                allocation = ChromaResampler.ResamplePlane(
                    sourceU,
                    expandedU,
                    source.GetWidth(),
                    source.GetHeight(),
                    source.GetChromaFormat(),
                    source.GetChromaSamplePosition(),
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).ResamplingFilter);
            }
            if (allocation == AvifResultCode.Success)
            {
                allocation = ChromaResampler.ResamplePlane(
                    sourceV,
                    expandedV,
                    source.GetWidth(),
                    source.GetHeight(),
                    source.GetChromaFormat(),
                    source.GetChromaSamplePosition(),
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    (options ?? throw new InvalidOperationException("Invalid internal state.")).ResamplingFilter);
            }
            if (allocation != AvifResultCode.Success)
            {
                expandedU.Close();
                expandedV.Close();
                return allocation;
            }
            sourceU = expandedU;
            sourceV = expandedV;
        }

        AvifResultCode result = ConvertRows(
            source, sourceU, sourceV, destination, context, options);
        if (expandedU != null)
        {
            expandedU.Close();
            (expandedV ?? throw new InvalidOperationException("Invalid internal state.")).Close();
        }
        return result;
    }

    static AvifResultCode ConvertRows(
        AvifImage source,
        AvifPlane? sourceU,
        AvifPlane? sourceV,
        AvifPixelBuffer destination,
        ColorConversionContext context,
        AvifColorConversionOptions options)
    {
        AvifPlane? sourceY = source.GetPlane(AvifPlaneKind.Y);
        AvifPlane? sourceAlpha = source.GetPlane(AvifPlaneKind.Alpha);
        bool direct = ColorConversionContext.UsesDirectComponents(
            context.Matrix);
        AvifIccProfile? iccProfile = options.UseEmbeddedIccProfile
            ? source.GetIccProfile() : null;
        bool useIcc = iccProfile != null
            && iccProfile.GetChannelCount() == 3
            && iccProfile.CanTransformToPcs();
        ColorVector d50 = new ColorVector(0.9642, 1, 0.8249);
        bool reduceDepth = !destination.IsFloatingPoint()
            && (destination.GetPixelFormat() == AvifPixelFormat.Rgb565
                || destination.GetBitDepth() < source.GetBitDepth());
        PackedDitherState dither = new PackedDitherState(
            options.DitherMode, source.GetWidth());

        int y = 0;
        while (y < source.GetHeight())
        {
            Span<byte> yRow = (sourceY ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y);
            Span<byte> uRow = sourceU == null
                ? Span<byte>.Empty : sourceU.GetRowSpan(y);
            Span<byte> vRow = sourceV == null
                ? Span<byte>.Empty : sourceV.GetRowSpan(y);
            Span<byte> alphaRow = sourceAlpha == null
                ? Span<byte>.Empty : sourceAlpha.GetRowSpan(y);
            Span<byte> destinationRow = destination.GetRowSpan(y);

            int x = 0;
            while (x < source.GetWidth())
            {
                double first = NormalizeLuma(
                    ReadSample(yRow, x, source.GetBitDepth()),
                    source.GetBitDepth(), source.GetRange());
                double second = 0;
                double third = 0;
                if (sourceU != null)
                {
                    int u = ReadSample(uRow, x, source.GetBitDepth());
                    int v = ReadSample(vRow, x, source.GetBitDepth());
                    second = direct
                        ? NormalizeLuma(
                            u, source.GetBitDepth(), source.GetRange())
                        : NormalizeChroma(
                            u, source.GetBitDepth(), source.GetRange());
                    third = direct
                        ? NormalizeLuma(
                            v, source.GetBitDepth(), source.GetRange())
                        : NormalizeChroma(
                            v, source.GetBitDepth(), source.GetRange());
                }

                ColorVector? linear = ColorMatrixCoefficients
                    .ComponentsToLinearRgb(
                        new ColorVector(first, second, third),
                        context.Matrix,
                        context.Primaries,
                        context.Transfer);
                ColorVector? outputLinear;
                if (useIcc)
                {
                    double[] device = new double[]
                    {
                        TransferFunctions.FromLinear(
                            (linear ?? throw new InvalidOperationException("Invalid internal state.")).First, context.Transfer),
                        TransferFunctions.FromLinear(
                            linear.Second, context.Transfer),
                        TransferFunctions.FromLinear(
                            linear.Third, context.Transfer)
                    };
                    double[] pcs = new double[3];
                    AvifResultCode iccResult = (iccProfile ?? throw new InvalidOperationException("Invalid internal state.")).TransformToPcs(
                        device, pcs, iccProfile.GetRenderingIntent());
                    if (iccResult != AvifResultCode.Success)
                    {
                        return iccResult;
                    }
                    outputLinear = ColorTables.ConvertXyzToPrimaries(
                        new ColorVector(pcs[0], pcs[1], pcs[2]),
                        d50,
                        AvifColorPrimaries.Bt709);
                }
                else
                {
                    outputLinear = ColorTables.ConvertPrimaries(
                        linear, context.Primaries, AvifColorPrimaries.Bt709);
                }
                if (outputLinear == null)
                {
                    return AvifResultCode.UnsupportedFeature;
                }

                double alpha = sourceAlpha == null
                    ? 1
                    : ReadSample(alphaRow, x, source.GetBitDepth())
                        / (double)((1 << source.GetBitDepth()) - 1);
                bool sourcePremultiplied = source.GetAlphaMode()
                    == AvifAlphaMode.Premultiplied;
                bool outputPremultiplied = options.OutputPremultipliedAlpha
                    && destination.HasAlpha() && options.PreserveAlpha;

                PackedColorSample packed;
                if (destination.IsFloatingPoint())
                {
                    packed = new PackedColorSample(
                        outputLinear.First,
                        outputLinear.Second,
                        outputLinear.Third,
                        options.PreserveAlpha ? alpha : 1);
                }
                else
                {
                    packed = new PackedColorSample(
                        TransferFunctions.FromLinear(
                            outputLinear.First,
                            AvifTransferCharacteristics.Srgb),
                        TransferFunctions.FromLinear(
                            outputLinear.Second,
                            AvifTransferCharacteristics.Srgb),
                        TransferFunctions.FromLinear(
                            outputLinear.Third,
                            AvifTransferCharacteristics.Srgb),
                        options.PreserveAlpha ? alpha : 1);
                }
                ApplyAlphaAssociation(
                    packed,
                    alpha,
                    sourcePremultiplied,
                    outputPremultiplied);
                packed.Red = ClampIfRequested(packed.Red, options);
                packed.Green = ClampIfRequested(packed.Green, options);
                packed.Blue = ClampIfRequested(packed.Blue, options);
                PackedPixelUtility.Write(
                    destination,
                    destinationRow,
                    x,
                    y,
                    packed,
                    dither,
                    reduceDepth);
                x++;
            }
            dither.FinishRow();
            y++;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode Validate(
        AvifImage source,
        AvifPixelBuffer destination,
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
            || source.GetHeight() != destination.GetHeight())
        {
            return AvifResultCode.InvalidArgument;
        }
        if (!IsValidFilter(options.ResamplingFilter)
            || !IsValidDither(options.DitherMode)
            || options.WorkerCount <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifColorInfo? information = source.GetColorInfo();
        if ((information ?? throw new InvalidOperationException("Invalid internal state.")).MatrixCoefficients == AvifMatrixCoefficients.Identity
            && (source.GetChromaFormat() != AvifChromaFormat.Yuv444
                || source.GetRange() != AvifRange.Full))
        {
            return AvifResultCode.InvalidData;
        }
        return AvifResultCode.Success;
    }

    static double NormalizeLuma(
        int sample,
        int depth,
        AvifRange range)
    {
        int minimum = RangeConverter.GetMinimum(depth, range);
        int maximum = RangeConverter.GetMaximum(depth, range, false);
        sample = System.Math.Max(minimum, System.Math.Min(maximum, sample));
        return (sample - minimum) / (double)(maximum - minimum);
    }

    static double NormalizeChroma(
        int sample,
        int depth,
        AvifRange range)
    {
        int minimum = RangeConverter.GetMinimum(depth, range);
        int maximum = RangeConverter.GetMaximum(depth, range, true);
        int neutral = 1 << (depth - 1);
        sample = System.Math.Max(minimum, System.Math.Min(maximum, sample));
        return (sample - neutral) / (double)(maximum - minimum);
    }

    static int ReadSample(Span<byte> row, int x, int depth)
    {
        if (depth <= 8)
        {
            return row[x];
        }
        int offset = x * 2;
        return row[offset] | row[offset + 1] << 8;
    }

    static double ClampIfRequested(
        double value,
        AvifColorConversionOptions options)
    {
        return options.ClampOutOfGamut
            ? System.Math.Max(0, System.Math.Min(1, value))
            : value;
    }

    static void ApplyAlphaAssociation(
        PackedColorSample sample,
        double alpha,
        bool sourcePremultiplied,
        bool outputPremultiplied)
    {
        if (sourcePremultiplied && !outputPremultiplied)
        {
            if (alpha > 0)
            {
                sample.Red /= alpha;
                sample.Green /= alpha;
                sample.Blue /= alpha;
            }
            else
            {
                sample.Red = 0;
                sample.Green = 0;
                sample.Blue = 0;
            }
        }
        else if (!sourcePremultiplied && outputPremultiplied)
        {
            sample.Red *= alpha;
            sample.Green *= alpha;
            sample.Blue *= alpha;
        }
    }

    static bool IsValidFilter(AvifResamplingFilter value)
    {
        return value == AvifResamplingFilter.Nearest
            || value == AvifResamplingFilter.Bilinear
            || value == AvifResamplingFilter.Bicubic
            || value == AvifResamplingFilter.Lanczos3;
    }

    static bool IsValidDither(AvifDitherMode value)
    {
        return value == AvifDitherMode.None
            || value == AvifDitherMode.Ordered
            || value == AvifDitherMode.ErrorDiffusion;
    }
}
