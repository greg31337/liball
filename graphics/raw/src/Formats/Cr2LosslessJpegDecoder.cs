namespace raw;

internal static class Cr2LosslessJpegDecoder
{
    internal static RawSensorImage Decode(RawDocument document, RawImageInfo info, RawDecodeOptions options)
    {
        RawCanonCr2Metadata? metadata = (document.FormatDetails ?? throw new System.InvalidOperationException("A required value is unavailable.")).Canon;
        if (metadata == null || metadata.ProfileId != CanonCr2MetadataParser.Eos6DMarkIiProfile && metadata.ProfileId != CanonCr2MetadataParser.Eos6DMarkIiMRawProfile && metadata.ProfileId != CanonCr2MetadataParser.Eos6DMarkIiSRawProfile && metadata.ProfileId != CanonCr2MetadataParser.Eos20dProfile && metadata.ProfileId != CanonCr2MetadataParser.Eos1dProfile)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedProfile, "CR2 raw decoding requires the reviewed Canon profile metadata.", -1);
        }
        if (metadata.SensorWidth != info.Width || metadata.SensorHeight != info.Height)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Canon raw image geometry does not match its reviewed profile metadata.", -1);
        }
        if (metadata.IsSubsampled)
        {
            return CanonSRawDecoder.Decode(document, info, options);
        }
        RawPayloadBlock payload = FindRawPayload(document, info.Index, options.PlaneIndex);
        byte[] encoded = document.ExtractPayload(payload);
        LosslessJpegImage jpeg = LosslessJpegCodec.Decode(encoded, options.Limits, options.CancellationToken);
        int expectedComponents = metadata.LosslessJpegComponentCount == 0 ? 4 : metadata.LosslessJpegComponentCount;
        ushort[] storedSamples = DecodeSamples(jpeg, info.Width, info.Height, metadata.SliceWidths, expectedComponents, info.BitDepth, 1, options.Limits, options.CancellationToken);
        ushort[] normalized = Normalize(storedSamples, info.BitDepth, options.Limits, options.CancellationToken);
        RawStoredImage? stored = null;
        if (options.PreserveStoredSamples)
        {
            uint[] exact = new uint[storedSamples.Length];
            int index = 0;
            while (index < storedSamples.Length)
            {
                exact[index] = storedSamples[index];
                index++;
            }
            stored = new RawStoredImage(info, exact, null, null, info.Width, true, null);
        }
        RawBlackLevel? black = CreateBlackLevel(metadata.BlackLevels);
        return new RawSensorImage(info, normalized, null, info.Width, true, stored, null, metadata.MaskedAreas, black, metadata.WhiteLevels, null, null, document.Diagnostics);
    }

    internal static ushort[] DecodeSamples(LosslessJpegImage jpeg, int width, int height, int[] sliceWidths, int expectedComponents, int precision, int predictor, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateJpeg(jpeg, expectedComponents, precision, predictor);
        int sampleCount = GetSampleCount(width, height, limits);
        ushort[] flattened = FlattenComponents(jpeg, sampleCount, limits, cancellationToken);
        return Unslice(flattened, width, height, sliceWidths, limits, cancellationToken);
    }

    internal static void ValidateJpeg(LosslessJpegImage jpeg, int expectedComponents, int precision, int predictor)
    {
        if (jpeg == null || expectedComponents <= 0 || jpeg.Planes == null || jpeg.Planes.Count != expectedComponents || jpeg.Precision != precision)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The CR2 lossless JPEG component count or precision does not match its profile.", -1);
        }
        LosslessJpegPlane first = jpeg.Planes[0];
        if (first == null || first.Width <= 0 || first.Height <= 0 || jpeg.Width != first.Width || jpeg.Height != first.Height)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The CR2 lossless JPEG has invalid component geometry.", -1);
        }
        int index = 0;
        while (index < jpeg.Planes.Count)
        {
            LosslessJpegPlane plane = jpeg.Planes[index];
            if (plane == null || plane.Width != first.Width || plane.Height != first.Height || plane.HorizontalSampling != 1 || plane.VerticalSampling != 1 || plane.Predictor != predictor || plane.PointTransform != 0 || plane.Samples == null || plane.Samples.LongLength != (long)plane.Width * plane.Height)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The CR2 lossless JPEG component mapping, predictor, transform, or sample count is invalid.", -1);
            }
            index++;
        }
    }

    internal static ushort[] FlattenComponents(LosslessJpegImage jpeg, int expectedSamples, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        LosslessJpegPlane first = jpeg.Planes[0];
        long decodedCount = CheckedMath.Multiply(first.Width, first.Height);
        decodedCount = CheckedMath.Multiply(decodedCount, jpeg.Planes.Count);
        if (decodedCount != expectedSamples)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The CR2 lossless JPEG decoded sample count does not exactly match the stored sensor geometry.", -1);
        }
        CheckedMath.DecodedSize(decodedCount, 2, limits);
        CheckedMath.AllocationSize(decodedCount, 2, limits);
        ushort[] result = new ushort[expectedSamples];
        int position = 0;
        int y = 0;
        while (y < first.Height)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int x = 0;
            while (x < first.Width)
            {
                int component = 0;
                while (component < jpeg.Planes.Count)
                {
                    result[position] = (jpeg.Planes[component].Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[y * first.Width + x];
                    position++;
                    component++;
                }
                x++;
            }
            y++;
        }
        if (position != expectedSamples)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The CR2 component mapper did not produce the exact expected sample count.", -1);
        }
        return result;
    }

    internal static ushort[] Unslice(ushort[] source, int width, int height, int[] sliceWidths, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        int expected = GetSampleCount(width, height, limits);
        if (source == null || source.Length != expected)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "CR2 unslicing requires the exact stored sensor sample count.", -1);
        }
        ValidateSlices(sliceWidths, width);
        CheckedMath.AllocationSize(expected, 2, limits);
        ushort[] result = new ushort[expected];
        int sourceIndex = 0;
        int sliceX = 0;
        int sliceIndex = 0;
        while (sliceIndex < sliceWidths.Length)
        {
            int sliceWidth = sliceWidths[sliceIndex];
            int row = 0;
            while (row < height)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int destination = row * width + sliceX;
                System.Array.Copy(source, sourceIndex, result, destination, sliceWidth);
                sourceIndex += sliceWidth;
                row++;
            }
            sliceX += sliceWidth;
            sliceIndex++;
        }
        if (sourceIndex != expected || sliceX != width)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "CR2 unslicing did not consume the exact sample geometry.", -1);
        }
        return result;
    }

    internal static void ValidateSlices(int[] sliceWidths, int width)
    {
        if (sliceWidths == null || sliceWidths.Length == 0 || width <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "CR2 slice widths and stored width are required.", -1);
        }
        long sum = 0;
        int index = 0;
        while (index < sliceWidths.Length)
        {
            if (sliceWidths[index] <= 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "Every CR2 slice width must be positive.", -1);
            }
            sum = CheckedMath.Add(sum, sliceWidths[index]);
            index++;
        }
        if (sum != width)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidMetadata, "CR2 slice widths do not exactly cover the stored sensor width.", -1);
        }
    }

    internal static void ValidateProfileSlices(int[] observed, int[] expected, int width)
    {
        ValidateSlices(observed, width);
        if (expected == null || observed.Length != expected.Length)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The CR2 slice table does not match the selected profile.", -1);
        }
        int index = 0;
        while (index < observed.Length)
        {
            if (observed[index] != expected[index])
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The CR2 slice table does not match the selected profile.", -1);
            }
            index++;
        }
    }

    internal static ushort[] Normalize(ushort[] source, int precision, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        if (precision < 1 || precision > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "CR2 normalization requires one through sixteen bits of precision.", -1);
        }
        CheckedMath.AllocationSize(source.Length, 2, limits);
        ushort[] result = new ushort[source.Length];
        uint maximum = precision == 16 ? ushort.MaxValue : (1U << precision) - 1U;
        int index = 0;
        while (index < source.Length)
        {
            if ((index & 65535) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (source[index] > maximum)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A decoded CR2 sample exceeds its profile precision.", -1);
            }
            result[index] = (ushort)(((uint)source[index] * ushort.MaxValue + maximum / 2U) / maximum);
            index++;
        }
        return result;
    }

    internal static RawPayloadBlock FindRawPayload(RawDocument document, int imageIndex, int planeIndex)
    {
        RawPayloadBlock? match = null;
        int index = 0;
        while (index < document.PayloadMap.Blocks.Length)
        {
            RawPayloadBlock block = document.PayloadMap.Blocks[index];
            if (block != null && block.Kind == RawPayloadKind.RawImage && block.ImageIndex == imageIndex && block.PlaneIndex == planeIndex)
            {
                if (match != null)
                {
                    throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The reviewed CR2 decoder requires one complete raw JPEG payload.", block.Offset);
                }
                match = block;
            }
            index++;
        }
        if (match == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "The CR2 raw JPEG payload is missing.", -1);
        }
        return match;
    }

    internal static RawBlackLevel? CreateBlackLevel(double[] values)
    {
        if (values == null || values.Length != 4)
        {
            return null;
        }
        double[] copy = new double[4];
        System.Array.Copy(values, copy, 4);
        return new RawBlackLevel(2, 2, 1, copy, null, null);
    }

    internal static int GetSampleCount(int width, int height, RawLimits? limits)
    {
        if (width <= 0 || height <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "CR2 stored sensor dimensions must be positive.", -1);
        }
        long count = CheckedMath.Multiply(width, height);
        CheckedMath.DecodedSize(count, 2, limits);
        CheckedMath.AllocationSize(count, 2, limits);
        return CheckedMath.ToInt32(count);
    }
}
