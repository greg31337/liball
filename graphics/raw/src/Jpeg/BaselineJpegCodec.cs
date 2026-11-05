namespace raw;

internal static class BaselineJpegCodec
{
    internal static readonly int[] ZigZag = new int[]
    {
        0, 1, 8, 16, 9, 2, 3, 10,
        17, 24, 32, 25, 18, 11, 4, 5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6, 7, 14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    };
    internal static readonly double[] InverseTransform = CreateInverseTransform();

    internal static BaselineJpegImage Decode(System.ReadOnlyMemory<byte> source, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        JpegDocument document = JpegParser.Parse(source, limits, cancellationToken);
        return Decode(document, limits, cancellationToken);
    }

    internal static BaselineJpegImage Decode(JpegDocument? document, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (document == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A parsed JPEG document is required.", -1);
        }
        CheckedMath.FileSize(document.Source.Length, limits);
        cancellationToken.ThrowIfCancellationRequested();
        if (document.Frames.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "Baseline JPEG decoding requires exactly one frame.", -1);
        }
        JpegFrameHeader frame = document.Frames[0];
        if (frame.ProcessMarker != 0xc0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "The JPEG frame is not baseline sequential Huffman SOF0.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (frame.Height == 0)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Baseline JPEG decoding does not support deferred DNL frame height.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (frame.Precision != 8)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Baseline JPEG decoding requires eight-bit sample precision.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        ValidateFrameComponents(frame);
        if (document.Scans.Count == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A baseline JPEG frame must contain at least one scan.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }

        int maximumHorizontalSampling = GetMaximumHorizontalSampling(frame);
        int maximumVerticalSampling = GetMaximumVerticalSampling(frame);
        BaselineJpegImage image = CreateImage(frame, maximumHorizontalSampling, maximumVerticalSampling, limits);
        bool[] decodedPlanes = new bool[image.Planes.Count];
        int scanIndex = 0;
        while (scanIndex < document.Scans.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            JpegScanHeader scan = document.Scans[scanIndex];
            if (scan.Frame != frame)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidContainer, "A baseline JPEG scan belongs to a different frame.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            ValidateScan(scan, frame);
            if (scan.Components.Count > 1 && scan.Components.Count != frame.Components.Count)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "A baseline JPEG interleaved scan must contain every frame component.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }

            BaselineJpegPlane[] planes = new BaselineJpegPlane[scan.Components.Count];
            HuffmanTable[] dcTables = new HuffmanTable[scan.Components.Count];
            HuffmanTable[] acTables = new HuffmanTable[scan.Components.Count];
            JpegQuantizationTable[] quantizationTables = new JpegQuantizationTable[scan.Components.Count];
            int componentIndex = 0;
            while (componentIndex < scan.Components.Count)
            {
                JpegScanComponent scanComponent = scan.Components[componentIndex];
                int planeIndex = GetPlaneIndex(image, scanComponent.Identifier);
                if (decodedPlanes[planeIndex])
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG component appears in more than one scan.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
                }
                JpegFrameComponent frameComponent = GetFrameComponent(frame, scanComponent.Identifier);
                planes[componentIndex] = image.Planes[planeIndex];
                dcTables[componentIndex] = FindHuffmanTable(document, scan, 0, scanComponent.DcTable);
                acTables[componentIndex] = FindHuffmanTable(document, scan, 1, scanComponent.AcTable);
                quantizationTables[componentIndex] = FindQuantizationTable(document, scan, frameComponent.QuantizationTable);
                decodedPlanes[planeIndex] = true;
                componentIndex++;
            }
            DecodeScan(document.Source, frame, scan, planes, dcTables, acTables, quantizationTables, maximumHorizontalSampling, maximumVerticalSampling, limits, cancellationToken);
            scanIndex++;
        }

        int resultPlaneIndex = 0;
        while (resultPlaneIndex < decodedPlanes.Length)
        {
            if (!decodedPlanes[resultPlaneIndex])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG frame component has no scan data.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            resultPlaneIndex++;
        }
        return image;
    }

    internal static void ValidateFrameComponents(JpegFrameHeader frame)
    {
        if (frame.Components.Count != 1 && frame.Components.Count != 3)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "Baseline JPEG decoding supports grayscale or three-component YCbCr frames.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (frame.Components.Count == 1)
        {
            JpegFrameComponent gray = frame.Components[0];
            if (gray.HorizontalSampling != 1 || gray.VerticalSampling != 1)
            {
                throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "A grayscale baseline JPEG component must use one-by-one sampling.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
            }
            return;
        }

        JpegFrameComponent luma = frame.Components[0];
        JpegFrameComponent firstChroma = frame.Components[1];
        JpegFrameComponent secondChroma = frame.Components[2];
        bool commonLuma = luma.HorizontalSampling == 1 && luma.VerticalSampling == 1 || luma.HorizontalSampling == 2 && luma.VerticalSampling == 1 || luma.HorizontalSampling == 2 && luma.VerticalSampling == 2;
        if (!commonLuma || firstChroma.HorizontalSampling != 1 || firstChroma.VerticalSampling != 1 || secondChroma.HorizontalSampling != 1 || secondChroma.VerticalSampling != 1)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "A YCbCr baseline JPEG must use 4:4:4, 4:2:2, or 4:2:0 sampling.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
    }

    internal static BaselineJpegImage CreateImage(JpegFrameHeader frame, int maximumHorizontalSampling, int maximumVerticalSampling, RawLimits limits)
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = frame.Width;
        image.Height = frame.Height;
        image.Precision = frame.Precision;
        long totalSamples = 0;
        int index = 0;
        while (index < frame.Components.Count)
        {
            JpegFrameComponent component = frame.Components[index];
            BaselineJpegPlane plane = new BaselineJpegPlane();
            plane.Identifier = component.Identifier;
            plane.HorizontalSampling = component.HorizontalSampling;
            plane.VerticalSampling = component.VerticalSampling;
            plane.Width = DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(frame.Width, component.HorizontalSampling)), maximumHorizontalSampling);
            plane.Height = DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(frame.Height, component.VerticalSampling)), maximumVerticalSampling);
            long sampleCount = CheckedMath.Multiply(plane.Width, plane.Height);
            totalSamples = CheckedMath.Add(totalSamples, sampleCount);
            CheckedMath.AllocationSize(sampleCount, 1, limits);
            plane.Samples = new byte[CheckedMath.ToInt32(sampleCount)];
            image.Planes.Add(plane);
            index++;
        }
        CheckedMath.DecodedSize(totalSamples, 1, limits);
        return image;
    }

    internal static void ValidateScan(JpegScanHeader scan, JpegFrameHeader frame)
    {
        if (scan.SpectralStart != 0 || scan.SpectralEnd != 63 || scan.SuccessiveApproximationHigh != 0 || scan.SuccessiveApproximationLow != 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline sequential JPEG scan requires Ss zero, Se 63, and no successive approximation.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        int index = 0;
        while (index < scan.Components.Count)
        {
            GetFrameComponent(frame, scan.Components[index].Identifier);
            index++;
        }
    }

    internal static void DecodeScan(System.ReadOnlyMemory<byte> source, JpegFrameHeader frame, JpegScanHeader scan, BaselineJpegPlane[] planes, HuffmanTable[] dcTables, HuffmanTable[] acTables, JpegQuantizationTable[] quantizationTables, int maximumHorizontalSampling, int maximumVerticalSampling, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        int mcuColumns;
        int mcuRows;
        if (planes.Length == 1)
        {
            mcuColumns = DivideRoundUp(planes[0].Width, 8);
            mcuRows = DivideRoundUp(planes[0].Height, 8);
        }
        else
        {
            mcuColumns = DivideRoundUp(frame.Width, maximumHorizontalSampling * 8);
            mcuRows = DivideRoundUp(frame.Height, maximumVerticalSampling * 8);
        }
        long mcuCount = CheckedMath.Multiply(mcuColumns, mcuRows);
        long expectedRestartCount = 0;
        if (scan.RestartInterval > 0 && mcuCount > 0)
        {
            expectedRestartCount = (mcuCount - 1) / scan.RestartInterval;
        }
        if (scan.RestartMarkers.Count != expectedRestartCount || scan.EntropySegments.Count != expectedRestartCount + 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "Baseline JPEG restart markers do not match the configured MCU interval.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }

        int[] coefficients = new int[64];
        double[] workspace = new double[64];
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
            int[] dcPredictions = new int[planes.Length];
            long localMcu = 0;
            while (localMcu < segmentMcuCount)
            {
                if ((localMcu & 4095) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                DecodeMcu(reader, planes, dcTables, acTables, quantizationTables, dcPredictions, coefficients, workspace, mcuColumns, decodedMcus + localMcu);
                localMcu++;
            }
            long remainingBits = reader.GetRemainingBitCount();
            if (remainingBits > 7)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG restart segment contains excess entropy data.", segment.Offset);
            }
            reader.ValidatePaddingToEnd(1);
            decodedMcus += segmentMcuCount;
            segmentIndex++;
        }
        if (decodedMcus != mcuCount)
        {
            throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "Baseline JPEG scan data does not cover every MCU.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
    }

    internal static void DecodeMcu(MsbBitReader reader, BaselineJpegPlane[] planes, HuffmanTable[] dcTables, HuffmanTable[] acTables, JpegQuantizationTable[] quantizationTables, int[] dcPredictions, int[] coefficients, double[] workspace, int mcuColumns, long mcuIndex)
    {
        int mcuX = (int)(mcuIndex % mcuColumns);
        int mcuY = (int)(mcuIndex / mcuColumns);
        if (planes.Length == 1)
        {
            DecodeBlock(reader, planes[0], dcTables[0], acTables[0], quantizationTables[0], dcPredictions, 0, coefficients, workspace, mcuX, mcuY);
            return;
        }

        int componentIndex = 0;
        while (componentIndex < planes.Length)
        {
            BaselineJpegPlane plane = planes[componentIndex];
            int vertical = 0;
            while (vertical < plane.VerticalSampling)
            {
                int horizontal = 0;
                while (horizontal < plane.HorizontalSampling)
                {
                    int blockX = mcuX * plane.HorizontalSampling + horizontal;
                    int blockY = mcuY * plane.VerticalSampling + vertical;
                    DecodeBlock(reader, plane, dcTables[componentIndex], acTables[componentIndex], quantizationTables[componentIndex], dcPredictions, componentIndex, coefficients, workspace, blockX, blockY);
                    horizontal++;
                }
                vertical++;
            }
            componentIndex++;
        }
    }

    internal static void DecodeBlock(MsbBitReader reader, BaselineJpegPlane plane, HuffmanTable dcTable, HuffmanTable acTable, JpegQuantizationTable quantizationTable, int[] dcPredictions, int componentIndex, int[] coefficients, double[] workspace, int blockX, int blockY)
    {
        System.Array.Clear(coefficients, 0, coefficients.Length);
        int dcCategory = dcTable.DecodeSymbol(reader);
        if (dcCategory > 11)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG DC category exceeds eleven bits.", reader.Core.GetCurrentByteOffset());
        }
        int dcDifference = DecodeAmplitude(reader, dcCategory);
        long dcValue = (long)dcPredictions[componentIndex] + dcDifference;
        if (dcValue < int.MinValue || dcValue > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A baseline JPEG DC prediction overflowed.", reader.Core.GetCurrentByteOffset());
        }
        dcPredictions[componentIndex] = (int)dcValue;
        coefficients[0] = MultiplyCoefficient(dcPredictions[componentIndex], (quantizationTable.Values ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], reader);

        int coefficientIndex = 1;
        while (coefficientIndex < 64)
        {
            int symbol = acTable.DecodeSymbol(reader);
            int zeroRun = symbol >> 4;
            int category = symbol & 15;
            if (category == 0)
            {
                if (zeroRun == 0)
                {
                    break;
                }
                if (zeroRun != 15)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG AC zero-size symbol is invalid.", reader.Core.GetCurrentByteOffset());
                }
                coefficientIndex += 16;
                if (coefficientIndex > 64)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG AC zero run exceeds its block.", reader.Core.GetCurrentByteOffset());
                }
                continue;
            }
            if (category > 10)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG AC category exceeds ten bits.", reader.Core.GetCurrentByteOffset());
            }
            coefficientIndex += zeroRun;
            if (coefficientIndex >= 64)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG AC run exceeds its block.", reader.Core.GetCurrentByteOffset());
            }
            int amplitude = DecodeAmplitude(reader, category);
            int naturalIndex = ZigZag[coefficientIndex];
            coefficients[naturalIndex] = MultiplyCoefficient(amplitude, quantizationTable.Values[coefficientIndex], reader);
            coefficientIndex++;
        }
        InverseDct(coefficients, workspace, plane, blockX, blockY);
    }

    internal static int DecodeAmplitude(MsbBitReader reader, int category)
    {
        if (category == 0)
        {
            return 0;
        }
        uint bits = reader.ReadBits(category);
        int threshold = 1 << (category - 1);
        if (bits < threshold)
        {
            return (int)bits - ((1 << category) - 1);
        }
        return (int)bits;
    }

    internal static int MultiplyCoefficient(int value, ushort quantization, MsbBitReader reader)
    {
        long result = (long)value * quantization;
        if (result < int.MinValue || result > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "A baseline JPEG dequantized coefficient overflowed.", reader.Core.GetCurrentByteOffset());
        }
        return (int)result;
    }

    internal static void InverseDct(int[] coefficients, double[] workspace, BaselineJpegPlane plane, int blockX, int blockY)
    {
        int frequencyY = 0;
        while (frequencyY < 8)
        {
            int x = 0;
            while (x < 8)
            {
                double sum = 0;
                int frequencyX = 0;
                while (frequencyX < 8)
                {
                    sum += coefficients[frequencyY * 8 + frequencyX] * InverseTransform[x * 8 + frequencyX];
                    frequencyX++;
                }
                workspace[frequencyY * 8 + x] = sum;
                x++;
            }
            frequencyY++;
        }

        int y = 0;
        while (y < 8)
        {
            int sampleY = blockY * 8 + y;
            int x = 0;
            while (x < 8)
            {
                int sampleX = blockX * 8 + x;
                if (sampleX < plane.Width && sampleY < plane.Height)
                {
                    double sum = 0;
                    int frequency = 0;
                    while (frequency < 8)
                    {
                        sum += workspace[frequency * 8 + x] * InverseTransform[y * 8 + frequency];
                        frequency++;
                    }
                    int sample = (int)System.Math.Floor(sum + 128.5);
                    if (sample < 0)
                    {
                        sample = 0;
                    }
                    else if (sample > 255)
                    {
                        sample = 255;
                    }
                    (plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[sampleY * plane.Width + sampleX] = (byte)sample;
                }
                x++;
            }
            y++;
        }
    }

    internal static double[] CreateInverseTransform()
    {
        double[] transform = new double[64];
        int sample = 0;
        while (sample < 8)
        {
            int frequency = 0;
            while (frequency < 8)
            {
                double scale = frequency == 0 ? 1.0 / System.Math.Sqrt(2.0) : 1.0;
                transform[sample * 8 + frequency] = 0.5 * scale * System.Math.Cos((2 * sample + 1) * frequency * System.Math.PI / 16.0);
                frequency++;
            }
            sample++;
        }
        return transform;
    }

    internal static HuffmanTable FindHuffmanTable(JpegDocument document, JpegScanHeader scan, int tableClass, int identifier)
    {
        HuffmanTable? table = null;
        int index = 0;
        while (index < document.HuffmanTables.Count)
        {
            JpegHuffmanTableDefinition definition = document.HuffmanTables[index];
            if ((definition.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset < (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset && definition.TableClass == tableClass && definition.Identifier == identifier)
            {
                table = definition.Table;
            }
            index++;
        }
        if (table == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG scan references an undefined Huffman table.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        return table;
    }

    internal static JpegQuantizationTable FindQuantizationTable(JpegDocument document, JpegScanHeader scan, int identifier)
    {
        JpegQuantizationTable? table = null;
        int index = 0;
        while (index < document.QuantizationTables.Count)
        {
            JpegQuantizationTable candidate = document.QuantizationTables[index];
            if ((candidate.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset < (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset && candidate.Identifier == identifier)
            {
                table = candidate;
            }
            index++;
        }
        if (table == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG frame references an undefined quantization table.", (scan.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
        }
        if (table.Precision != 8)
        {
            throw new RawUnsupportedException(RawDiagnosticCode.UnsupportedCodec, "A baseline JPEG quantization table must use eight-bit values.", (table.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
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

    internal static int GetPlaneIndex(BaselineJpegImage image, byte identifier)
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
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG scan references an unknown frame component.", -1);
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
        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG scan references an unknown frame component.", (frame.Marker ?? throw new System.InvalidOperationException("A required value is unavailable.")).Offset);
    }

    internal static int DivideRoundUp(int value, int divisor)
    {
        if (value < 0 || divisor <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "Baseline JPEG dimensions and sampling divisors must be valid.", -1);
        }
        return CheckedMath.ToInt32(CheckedMath.Add(value, divisor - 1) / divisor);
    }
}
