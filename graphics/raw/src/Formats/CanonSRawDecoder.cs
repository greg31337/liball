namespace raw;

internal static class CanonSRawDecoder
{
    internal const string SubsampledQuirk = "canon-sraw-subsampled";
    internal const string Layout422Quirk = "canon-sraw-layout-422";
    internal const string Layout420Quirk = "canon-sraw-layout-420";
    internal const string Generation0Quirk = "canon-sraw-generation-0";
    internal const string Generation1Quirk = "canon-sraw-generation-1";
    internal const string Generation2Quirk = "canon-sraw-generation-2";
    internal const string NewHueQuirk = "canon-sraw-new-hue";
    internal const string InvertWhiteBalanceQuirk = "canon-sraw-invert-white-balance";

    internal static RawSensorImage Decode(RawDocument document, RawImageInfo info, RawDecodeOptions options)
    {
        RawCanonCr2Metadata? metadata = (document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon;
        if (metadata == null || !metadata.IsSubsampled)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "Canon sRAW/mRAW decoding requires profile-qualified subsampling metadata.", -1);
        }
        if (options.PreserveStoredSamples)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedFormat, "Canon sRAW/mRAW packed samples cannot be represented as the reconstructed RGB image's exact stored buffer.", -1);
        }
        RawPayloadBlock payload = Cr2LosslessJpegDecoder.FindRawPayload(document, info.Index, options.PlaneIndex);
        byte[] encoded = document.ExtractPayload(payload);
        LosslessJpegImage jpeg = LosslessJpegCodec.DecodeCanonSubsampled(encoded, options.Limits, options.CancellationToken);
        ushort[] packed = DecodePacked(jpeg, metadata, options.Limits, options.CancellationToken);
        ushort[] rgb = Reconstruct(packed, metadata, options.Limits, options.CancellationToken);
        RawBlackLevel black = RawBlackLevel.CreateScalar(0.0, 3);
        return new RawSensorImage(info, rgb, null, CheckedMath.ToInt32(CheckedMath.Multiply(info.Width, 3)), true, null, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, document.Diagnostics);
    }

    internal static ushort[] DecodePacked(LosslessJpegImage jpeg, RawCanonCr2Metadata? metadata, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateMetadata(metadata);
        if (jpeg == null || jpeg.Planes == null || jpeg.Planes.Count != 3 || jpeg.Precision != 15)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Canon sRAW/mRAW requires one 15-bit three-component lossless JPEG frame.", -1);
        }
        LosslessJpegPlane yPlane = jpeg.Planes[0];
        LosslessJpegPlane cbPlane = jpeg.Planes[1];
        LosslessJpegPlane crPlane = jpeg.Planes[2];
        int horizontal = (metadata ?? throw new System.InvalidOperationException("A required value is unavailable.")).SubsamplingHorizontal;
        int vertical = metadata.SubsamplingVertical;
        if (!ValidPlane(yPlane, horizontal, vertical, 1) || !ValidPlane(cbPlane, 1, 1, 1) || !ValidPlane(crPlane, 1, 1, 1) || yPlane.Width % horizontal != 0 || yPlane.Height % vertical != 0 || cbPlane.Width != yPlane.Width / horizontal || cbPlane.Height != yPlane.Height / vertical || crPlane.Width != cbPlane.Width || crPlane.Height != cbPlane.Height)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Canon sRAW/mRAW JPEG component sampling does not match the selected profile layout.", -1);
        }
        int mcuColumns = cbPlane.Width;
        int mcuRows = cbPlane.Height;
        int componentsPerMcu = 2 + CheckedMath.ToInt32(CheckedMath.Multiply(horizontal, vertical));
        long flattenedCount = CheckedMath.Multiply(mcuColumns, mcuRows);
        flattenedCount = CheckedMath.Multiply(flattenedCount, componentsPerMcu);
        CheckedMath.DecodedSize(flattenedCount, 2, limits);
        CheckedMath.AllocationSize(flattenedCount, 2, limits);
        ushort[] flattened = new ushort[CheckedMath.ToInt32(flattenedCount)];
        int destination = 0;
        int mcuY = 0;
        while (mcuY < mcuRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int mcuX = 0;
            while (mcuX < mcuColumns)
            {
                int localY = 0;
                while (localY < vertical)
                {
                    int localX = 0;
                    while (localX < horizontal)
                    {
                        int sourceX = mcuX * horizontal + localX;
                        int sourceY = mcuY * vertical + localY;
                        flattened[destination] = (yPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceY * yPlane.Width + sourceX];
                        destination++;
                        localX++;
                    }
                    localY++;
                }
                flattened[destination] = (cbPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[mcuY * mcuColumns + mcuX];
                flattened[destination + 1] = (crPlane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[mcuY * mcuColumns + mcuX];
                destination += 2;
                mcuX++;
            }
            mcuY++;
        }
        int packedWidth = GetPackedWidth(metadata);
        int packedHeight = metadata.SensorHeight / vertical;
        if (destination != flattened.Length || flattened.Length != CheckedMath.Multiply(packedWidth, packedHeight))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Canon sRAW/mRAW JPEG samples do not exactly cover the profile's packed geometry.", -1);
        }
        return Cr2LosslessJpegDecoder.Unslice(flattened, packedWidth, packedHeight, metadata.SliceWidths, limits, cancellationToken);
    }

    internal static ushort[] Reconstruct(ushort[] packed, RawCanonCr2Metadata? metadata, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateMetadata(metadata);
        int packedWidth = GetPackedWidth((metadata ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        int packedHeight = metadata.SensorHeight / metadata.SubsamplingVertical;
        long packedCount = CheckedMath.Multiply(packedWidth, packedHeight);
        if (packed == null || packed.LongLength != packedCount)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Canon sRAW/mRAW reconstruction requires the exact packed sample geometry.", -1);
        }
        long pixelCount = CheckedMath.PixelCount(metadata.SensorWidth, metadata.SensorHeight, limits);
        long outputCount = CheckedMath.Multiply(pixelCount, 3);
        CheckedMath.DecodedSize(CheckedMath.Add(CheckedMath.Multiply(packedCount, 2), CheckedMath.Multiply(outputCount, 2)), 1, limits);
        CheckedMath.AllocationSize(outputCount, 2, limits);
        ushort[] output = new ushort[CheckedMath.ToInt32(outputCount)];
        int componentsPerMcu = 2 + metadata.SubsamplingHorizontal * metadata.SubsamplingVertical;
        int lumaPerMcu = componentsPerMcu - 2;
        RawSrawInterpolationMode interpolationMode = RawSrawInterpolator.SelectMode(metadata);
        int y = 0;
        while (y < metadata.SensorHeight)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < metadata.SensorWidth)
            {
                int groupX = x / metadata.SubsamplingHorizontal;
                int groupY = y / metadata.SubsamplingVertical;
                int groupOffset = groupY * packedWidth + groupX * componentsPerMcu;
                int localX = x % metadata.SubsamplingHorizontal;
                int localY = y % metadata.SubsamplingVertical;
                int luminance = packed[groupOffset + localY * metadata.SubsamplingHorizontal + localX];
                int cb = RawSrawInterpolator.InterpolateChroma(packed, metadata, interpolationMode, groupX, groupY, localX, localY, lumaPerMcu);
                int cr = RawSrawInterpolator.InterpolateChroma(packed, metadata, interpolationMode, groupX, groupY, localX, localY, lumaPerMcu + 1);
                int destination = (y * metadata.SensorWidth + x) * 3;
                ConvertPixel(luminance, cb, cr, metadata.SRawCoefficients, metadata.SRawGeneration, output, destination);
                x++;
            }
            y++;
        }
        return output;
    }

    internal static int[] ExtractCoefficients(ushort[] colorData, bool invert)
    {
        if (colorData == null || colorData.Length <= 81)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Canon sRAW/mRAW ColorData does not contain reconstruction coefficients.", -1);
        }
        int red = colorData[78];
        int green = (colorData[79] + colorData[80] + 1) >> 1;
        int blue = colorData[81];
        if (red <= 0 || green <= 0 || blue <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Canon sRAW/mRAW reconstruction coefficients must be positive.", -1);
        }
        if (invert)
        {
            red = 1048576 / red;
            blue = 1048576 / blue;
        }
        return new int[] { red, green, blue };
    }

    internal static void ConvertPixel(int luminance, int cb, int cr, int[] coefficients, int generation, ushort[] output, int destination)
    {
        long red;
        long green;
        long blue;
        if (generation == 0)
        {
            red = (long)coefficients[0] * (luminance + cr - 512);
            green = (long)coefficients[1] * (luminance + ((-778L * cb - 2048L * cr) >> 12) - 512);
            blue = (long)coefficients[2] * (luminance + cb - 512);
        }
        else if (generation == 1)
        {
            red = (long)coefficients[0] * (luminance + ((50L * cb + 22929L * cr) >> 12));
            green = (long)coefficients[1] * (luminance + ((-5640L * cb - 11751L * cr) >> 12));
            blue = (long)coefficients[2] * (luminance + ((29040L * cb - 101L * cr) >> 12));
        }
        else if (generation == 2)
        {
            red = (long)coefficients[0] * (luminance + cr);
            green = (long)coefficients[1] * (luminance + ((-778L * cb - 2048L * cr) >> 12));
            blue = (long)coefficients[2] * (luminance + cb);
        }
        else
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The Canon sRAW/mRAW reconstruction generation is not defined.", -1);
        }
        output[destination] = Clamp(red >> 8);
        output[destination + 1] = Clamp(green >> 8);
        output[destination + 2] = Clamp(blue >> 8);
    }

    internal static int GetGeneration(RawProfileCodec codec)
    {
        if (CanonEarlyRawLayout.HasQuirk(codec, Generation0Quirk)) return 0;
        if (CanonEarlyRawLayout.HasQuirk(codec, Generation1Quirk)) return 1;
        if (CanonEarlyRawLayout.HasQuirk(codec, Generation2Quirk)) return 2;
        return -1;
    }

    internal static int GetPackedWidth(RawCanonCr2Metadata metadata)
    {
        long groups = metadata.SensorWidth / metadata.SubsamplingHorizontal;
        return CheckedMath.ToInt32(CheckedMath.Multiply(groups, 2 + metadata.SubsamplingHorizontal * metadata.SubsamplingVertical));
    }

    internal static bool ValidPlane(LosslessJpegPlane plane, int horizontal, int vertical, int predictor)
    {
        return plane != null && plane.Samples != null && plane.Width > 0 && plane.Height > 0 && plane.HorizontalSampling == horizontal && plane.VerticalSampling == vertical && plane.Predictor == predictor && plane.PointTransform == 0 && plane.Samples.LongLength == CheckedMath.Multiply(plane.Width, plane.Height);
    }

    internal static void ValidateMetadata(RawCanonCr2Metadata? metadata)
    {
        if (metadata == null || !metadata.IsSubsampled || metadata.SensorWidth <= 0 || metadata.SensorHeight <= 0 || metadata.SubsamplingHorizontal != 2 || metadata.SubsamplingVertical != 1 && metadata.SubsamplingVertical != 2 || metadata.SensorWidth % metadata.SubsamplingHorizontal != 0 || metadata.SensorHeight % metadata.SubsamplingVertical != 0 || metadata.SRawGeneration < 0 || metadata.SRawGeneration > 2 || metadata.SRawHue < 0 || metadata.SRawCoefficients == null || metadata.SRawCoefficients.Length != 3 || metadata.SRawCoefficients[0] <= 0 || metadata.SRawCoefficients[1] <= 0 || metadata.SRawCoefficients[2] <= 0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Complete profile-qualified Canon sRAW/mRAW geometry and coefficients are required.", -1);
        }
    }

    internal static ushort Clamp(long value)
    {
        if (value < 0) return 0;
        if (value > ushort.MaxValue) return ushort.MaxValue;
        return (ushort)value;
    }
}
