namespace raw;

internal static class LosslessJpegCodec
{
    internal static LosslessJpegImage Decode(System.ReadOnlyMemory<byte> source, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        JpegDocument document = JpegParser.Parse(source, limits, cancellationToken);
        return Decode(document, limits, cancellationToken, false);
    }

    internal static LosslessJpegImage DecodeCanonSubsampled(System.ReadOnlyMemory<byte> source, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        JpegDocument document = JpegParser.Parse(source, limits, cancellationToken);
        return Decode(document, limits, cancellationToken, true);
    }

    internal static LosslessJpegImage Decode(JpegDocument? document, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        return Decode(document, limits, cancellationToken, false);
    }

    internal static LosslessJpegImage Decode(JpegDocument? document, RawLimits? limits, System.Threading.CancellationToken cancellationToken, bool allowSixteenBitPredictionWrap)
    {
        if (document == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A parsed JPEG document is required.", -1);
        }
        CheckedMath.FileSize(document.Source.Length, limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (document.Frames.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Lossless JPEG decoding requires exactly one frame.", -1);
        }
        JpegFrameHeader frame = document.Frames[0];
        if (frame.ProcessMarker != 0xc3)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The JPEG frame is not Huffman-coded lossless SOF3.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (frame.Height == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Lossless JPEG decoding does not support deferred DNL frame height.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (document.Scans.Count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A lossless JPEG frame must contain at least one scan.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }

        int maximumHorizontalSampling = GetMaximumHorizontalSampling(frame);
        int maximumVerticalSampling = GetMaximumVerticalSampling(frame);
        LosslessJpegImage image = CreateImage(frame, maximumHorizontalSampling, maximumVerticalSampling, limits);
        bool[] decodedPlanes = new bool[image.Planes.Count];
        int scanIndex = 0;
        while (scanIndex < document.Scans.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegScanHeader scan = document.Scans[scanIndex];
            if (scan.Frame != frame)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A lossless JPEG scan belongs to a different frame.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            ValidateScan(scan, frame);
            LosslessJpegPlane[] scanPlanes = new LosslessJpegPlane[scan.Components.Count];
            HuffmanTable[] tables = new HuffmanTable[scan.Components.Count];
            int componentIndex = 0;
            while (componentIndex < scan.Components.Count)
            {
                JpegScanComponent component = scan.Components[componentIndex];
                int planeIndex = GetPlaneIndex(image, component.Identifier);
                if (decodedPlanes[planeIndex])
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG component appears in more than one scan.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
                }
                LosslessJpegPlane plane = image.Planes[planeIndex];
                plane.Predictor = scan.SpectralStart;
                plane.PointTransform = scan.SuccessiveApproximationLow;
                scanPlanes[componentIndex] = plane;
                tables[componentIndex] = FindHuffmanTable(document, scan, component.DcTable);
                decodedPlanes[planeIndex] = true;
                componentIndex++;
            }
            DecodeScan(document.Source, frame, scan, scanPlanes, tables, maximumHorizontalSampling, maximumVerticalSampling, limits, cancellationToken, allowSixteenBitPredictionWrap);
            scanIndex++;
        }

        int resultPlaneIndex = 0;
        while (resultPlaneIndex < image.Planes.Count)
        {
            if (!decodedPlanes[resultPlaneIndex])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG frame component has no scan data.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            image.Planes[resultPlaneIndex].ReducedSamples = null;
            resultPlaneIndex++;
        }
        return image;
    }

    internal static LosslessJpegImage CreateImage(JpegFrameHeader frame, int maximumHorizontalSampling, int maximumVerticalSampling, RawLimits? limits)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = frame.Width;
        image.Height = frame.Height;
        image.Precision = frame.Precision;
        int mcuColumns = DivideRoundUp(frame.Width, maximumHorizontalSampling);
        int mcuRows = DivideRoundUp(frame.Height, maximumVerticalSampling);
        long totalBytes = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            JpegFrameComponent component = frame.Components[index];
            LosslessJpegPlane plane = new LosslessJpegPlane();
            plane.Identifier = component.Identifier;
            plane.HorizontalSampling = component.HorizontalSampling;
            plane.VerticalSampling = component.VerticalSampling;
            plane.Width = DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(frame.Width, component.HorizontalSampling)), maximumHorizontalSampling);
            plane.Height = DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(frame.Height, component.VerticalSampling)), maximumVerticalSampling);
            plane.WorkingWidth = CheckedMath.ToInt32(CheckedMath.Multiply(mcuColumns, component.HorizontalSampling));
            plane.WorkingHeight = CheckedMath.ToInt32(CheckedMath.Multiply(mcuRows, component.VerticalSampling));
            long sampleCount = CheckedMath.Multiply(plane.Width, plane.Height);
            long workingCount = CheckedMath.Multiply(plane.WorkingWidth, plane.WorkingHeight);
            totalBytes = CheckedMath.Add(totalBytes, CheckedMath.Multiply(sampleCount, 2));
            totalBytes = CheckedMath.Add(totalBytes, CheckedMath.Multiply(workingCount, 4));
            CheckedMath.AllocationSize(sampleCount, 2, limits);
            CheckedMath.AllocationSize(workingCount, 4, limits);
            image.Planes.Add(plane);
            index++;
        }
        CheckedMath.DecodedSize(totalBytes, 1, limits);
        index = 0;
        while (index < image.Planes.Count)
        {
            LosslessJpegPlane plane = image.Planes[index];
            int sampleCount = CheckedMath.ToInt32(CheckedMath.Multiply(plane.Width, plane.Height));
            int workingCount = CheckedMath.ToInt32(CheckedMath.Multiply(plane.WorkingWidth, plane.WorkingHeight));
            plane.Samples = new ushort[sampleCount];
            plane.ReducedSamples = new int[workingCount];
            index++;
        }
        return image;
    }

    internal static void ValidateScan(JpegScanHeader scan, JpegFrameHeader frame)
    {
        if (scan.SpectralStart < 1 || scan.SpectralStart > 7)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG predictor selection must be between one and seven.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (scan.SpectralEnd != 0 || scan.SuccessiveApproximationHigh != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan requires Se and Ah to be zero.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (scan.SuccessiveApproximationLow < 0 || scan.SuccessiveApproximationLow >= frame.Precision)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG point transform must be less than sample precision.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        int index = 0;
        while (index < scan.Components.Count)
        {
            JpegScanComponent scanComponent = scan.Components[index];
            if (scanComponent.AcTable != 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan cannot select an AC Huffman table.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            JpegFrameComponent frameComponent = GetFrameComponent(frame, scanComponent.Identifier);
            if (frameComponent.QuantizationTable != 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG frame component cannot select a quantization table.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            index++;
        }
    }

    internal static void DecodeScan(System.ReadOnlyMemory<byte> source, JpegFrameHeader frame, JpegScanHeader scan, LosslessJpegPlane[] planes, HuffmanTable[] tables, int maximumHorizontalSampling, int maximumVerticalSampling, RawLimits? limits, System.Threading.CancellationToken cancellationToken, bool allowSixteenBitPredictionWrap)
    {
        long mcuCount;
        int mcuColumns;
        if (planes.Length == 1)
        {
            mcuColumns = planes[0].Width;
            mcuCount = CheckedMath.Multiply(planes[0].Width, planes[0].Height);
        }
        else
        {
            mcuColumns = DivideRoundUp(frame.Width, maximumHorizontalSampling);
            int mcuRows = DivideRoundUp(frame.Height, maximumVerticalSampling);
            mcuCount = CheckedMath.Multiply(mcuColumns, mcuRows);
        }

        long expectedRestartCount = 0;
        if (scan.RestartInterval > 0 && mcuCount > 0)
        {
            expectedRestartCount = (mcuCount - 1) / scan.RestartInterval;
        }
        if (scan.RestartMarkers.Count != expectedRestartCount || scan.EntropySegments.Count != expectedRestartCount + 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Lossless JPEG restart markers do not match the configured MCU interval.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }

        long decodedMcus = 0;
        int segmentIndex = 0;
        while (segmentIndex < scan.EntropySegments.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegEntropySegment segment = scan.EntropySegments[segmentIndex];
            long segmentMcuCount = mcuCount - decodedMcus;
            if (segmentIndex < scan.EntropySegments.Count - 1)
            {
                segmentMcuCount = scan.RestartInterval;
            }
            byte[] entropy = JpegEntropyCodec.Unstuff(source, segment, limits, cancellationToken);
            BoundedInput input = BoundedInput.CreateWithAbsoluteOrigin(entropy, 0, entropy.Length, segment.Offset, limits);
            MsbBitReader reader = new MsbBitReader(input);
            bool[] initialPrediction = new bool[planes.Length];
            System.Array.Fill(initialPrediction, true);
            long localMcu = 0;
            while (localMcu < segmentMcuCount)
            {
                if ((localMcu & 4095) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                DecodeMcu(reader, frame, scan, planes, tables, initialPrediction, mcuColumns, decodedMcus + localMcu, allowSixteenBitPredictionWrap);
                localMcu++;
            }
            long remainingBits = reader.GetRemainingBitCount();
            if (remainingBits > 7)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG restart segment contains excess entropy data.", segment.Offset);
            }
            reader.ValidatePaddingToEnd(1);
            decodedMcus += segmentMcuCount;
            segmentIndex++;
        }
        if (decodedMcus != mcuCount)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "Lossless JPEG scan data does not cover every MCU.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
    }

    internal static void DecodeMcu(MsbBitReader reader, JpegFrameHeader frame, JpegScanHeader scan, LosslessJpegPlane[] planes, HuffmanTable[] tables, bool[] initialPrediction, int mcuColumns, long mcuIndex, bool allowSixteenBitPredictionWrap)
    {
        if (planes.Length == 1)
        {
            int x = (int)(mcuIndex % planes[0].Width);
            int y = (int)(mcuIndex / planes[0].Width);
            DecodeSample(reader, tables[0], planes[0], x, y, scan.SpectralStart, scan.SuccessiveApproximationLow, frame.Precision, initialPrediction, 0, allowSixteenBitPredictionWrap);
            return;
        }

        int mcuX = (int)(mcuIndex % mcuColumns);
        int mcuY = (int)(mcuIndex / mcuColumns);
        int componentIndex = 0;
        while (componentIndex < planes.Length)
        {
            LosslessJpegPlane plane = planes[componentIndex];
            int vertical = 0;
            while (vertical < plane.VerticalSampling)
            {
                int horizontal = 0;
                while (horizontal < plane.HorizontalSampling)
                {
                    int x = mcuX * plane.HorizontalSampling + horizontal;
                    int y = mcuY * plane.VerticalSampling + vertical;
                    DecodeSample(reader, tables[componentIndex], plane, x, y, scan.SpectralStart, scan.SuccessiveApproximationLow, frame.Precision, initialPrediction, componentIndex, allowSixteenBitPredictionWrap);
                    horizontal++;
                }
                vertical++;
            }
            componentIndex++;
        }
    }

    internal static void DecodeSample(MsbBitReader reader, HuffmanTable table, LosslessJpegPlane plane, int x, int y, int predictorSelection, int pointTransform, int precision, bool[] initialPrediction, int componentIndex, bool allowSixteenBitPredictionWrap)
    {
        int effectivePrecision = precision - pointTransform;
        int prediction;
        if (initialPrediction[componentIndex])
        {
            prediction = 1 << (effectivePrecision - 1);
            initialPrediction[componentIndex] = false;
        }
        else
        {
            prediction = GetPrediction(plane, x, y, predictorSelection, effectivePrecision);
        }
        int difference = DecodeDifference(reader, table);
        int sample = (prediction + difference) & 65535;
        int maximumSample = (1 << effectivePrecision) - 1;
        if (!allowSixteenBitPredictionWrap && (sample < 0 || sample > maximumSample))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG difference reconstructs a sample outside its precision.", reader.Core.GetCurrentByteOffset());
        }
        int workingIndex = y * plane.WorkingWidth + x;
        (plane.ReducedSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[workingIndex] = sample;
        if (x < plane.Width && y < plane.Height)
        {
            (plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[y * plane.Width + x] = (ushort)(sample << pointTransform);
        }
    }

    internal static int GetPrediction(LosslessJpegPlane plane, int x, int y, int predictorSelection, int effectivePrecision)
    {
        if (x == 0 && y == 0)
        {
            return 1 << (effectivePrecision - 1);
        }
        if (y == 0)
        {
            return (plane.ReducedSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[x - 1];
        }
        if (x == 0)
        {
            return (plane.ReducedSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[(y - 1) * plane.WorkingWidth];
        }
        int left = (plane.ReducedSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[y * plane.WorkingWidth + x - 1];
        int above = plane.ReducedSamples[(y - 1) * plane.WorkingWidth + x];
        int upperLeft = plane.ReducedSamples[(y - 1) * plane.WorkingWidth + x - 1];
        switch (predictorSelection)
        {
            case 1:
                return left;
            case 2:
                return above;
            case 3:
                return upperLeft;
            case 4:
                return left + above - upperLeft;
            case 5:
                return left + ((above - upperLeft) >> 1);
            case 6:
                return above + ((left - upperLeft) >> 1);
            case 7:
                return (left + above) >> 1;
            default:
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The lossless JPEG predictor selection is invalid.", -1);
        }
    }

    internal static int DecodeDifference(MsbBitReader reader, HuffmanTable table)
    {
        int category = table.DecodeSymbol(reader);
        if (category > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG difference category exceeds the defined range.", reader.Core.GetCurrentByteOffset());
        }
        if (category == 0)
        {
            return 0;
        }
        if (category == 16)
        {
            return 32768;
        }
        uint bits = reader.ReadBits(category);
        int threshold = 1 << (category - 1);
        if (bits < threshold)
        {
            return (int)bits - ((1 << category) - 1);
        }
        return (int)bits;
    }

    internal static HuffmanTable FindHuffmanTable(JpegDocument document, JpegScanHeader scan, int identifier)
    {
        HuffmanTable? table = null;
        int index = 0;
        while (index < document.HuffmanTables.Count)
        {
            JpegHuffmanTableDefinition definition = document.HuffmanTables[index];
            if ((definition.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset < (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset && definition.TableClass == 0 && definition.Identifier == identifier)
            {
                table = definition.Table;
            }
            index++;
        }
        if (table == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan references an undefined DC Huffman table.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        return table;
    }

    internal static int GetMaximumHorizontalSampling(JpegFrameHeader frame)
    {
        int maximum = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            if (frame.Components[index].HorizontalSampling > maximum)
            {
                maximum = frame.Components[index].HorizontalSampling;
            }
            index++;
        }
        return maximum;
    }

    internal static int GetMaximumVerticalSampling(JpegFrameHeader frame)
    {
        int maximum = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            if (frame.Components[index].VerticalSampling > maximum)
            {
                maximum = frame.Components[index].VerticalSampling;
            }
            index++;
        }
        return maximum;
    }

    internal static int GetPlaneIndex(LosslessJpegImage image, byte identifier)
    {
        int index = 0;
        while (index < image.Planes.Count)
        {
            if (image.Planes[index].Identifier == identifier)
            {
                return index;
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan references an unknown frame component.", -1);
    }

    internal static JpegFrameComponent GetFrameComponent(JpegFrameHeader frame, byte identifier)
    {
        int index = 0;
        while (index < frame.Components.Count)
        {
            if (frame.Components[index].Identifier == identifier)
            {
                return frame.Components[index];
            }
            index++;
        }
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG scan references an unknown frame component.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
    }

    internal static int DivideRoundUp(int value, int divisor)
    {
        if (value < 0 || divisor <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Lossless JPEG dimensions and sampling divisors must be valid.", -1);
        }
        return CheckedMath.ToInt32(CheckedMath.Add(value, divisor - 1) / divisor);
    }
}
