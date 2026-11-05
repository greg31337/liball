namespace raw;

internal static class LosslessJpegEncoder
{
    internal static byte[] Encode(LosslessJpegImage? image, LosslessJpegEncodeParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        LosslessJpegImage workingImage = ValidateAndPrepare(image, parameters, limits, cancellationToken);
        long[] frequencies = new long[17];
        ProcessAllScans(workingImage, (parameters ?? throw new System.InvalidOperationException("A required value is unavailable.")), null, null, frequencies, cancellationToken);
        LosslessJpegHuffmanDefinition huffman;
        if (parameters.OptimizeHuffmanTable)
        {
            huffman = BuildOptimizedHuffmanTable(frequencies);
        }
        else
        {
            huffman = BuildFixedHuffmanTable();
        }

        LosslessJpegOutput counter = new LosslessJpegOutput(null);
        WriteFile(counter, workingImage, parameters, huffman, cancellationToken);
        int encodedLength = counter.Offset;
        CheckedMath.FileSize(encodedLength, limits);
        CheckedMath.AllocationSize(encodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[encodedLength];
        LosslessJpegOutput output = new LosslessJpegOutput(destination);
        WriteFile(output, workingImage, parameters, huffman, cancellationToken);
        if (output.Offset != encodedLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "Lossless JPEG output does not match its planned length.", output.Offset);
        }
        return destination;
    }

    internal static LosslessJpegImage ValidateAndPrepare(LosslessJpegImage? image, LosslessJpegEncodeParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (image == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A lossless JPEG image is required.", -1);
        }
        if (parameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG encoding parameters are required.", -1);
        }
        CheckedMath.RequireLimits(limits);
        if (parameters.Predictor < 1 || parameters.Predictor > 7)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The lossless JPEG predictor must be between one and seven.", -1);
        }
        if (image.Precision < 2 || image.Precision > 16)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG precision must be between 2 and 16 bits.", -1);
        }
        if (parameters.PointTransform < 0 || parameters.PointTransform >= image.Precision)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The lossless JPEG point transform must be less than sample precision.", -1);
        }
        if (parameters.RestartInterval < 0 || parameters.RestartInterval > 65535)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The lossless JPEG restart interval must fit an unsigned 16-bit value.", -1);
        }
        CheckedMath.PixelCount(image.Width, image.Height, limits);
        if (image.Planes == null || image.Planes.Count == 0 || image.Planes.Count > 255)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG encoding requires between one and 255 component planes.", -1);
        }
        if (parameters.InterleavedScan && image.Planes.Count > 4)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "An interleaved JPEG scan cannot contain more than four components.", -1);
        }

        bool[] identifiers = new bool[256];
        int maximumHorizontalSampling = 0;
        int maximumVerticalSampling = 0;
        int index = 0;
        while (index < image.Planes.Count)
        {
            LosslessJpegPlane plane = image.Planes[index];
            if (plane == null)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A lossless JPEG component plane is required.", -1);
            }
            if (identifiers[plane.Identifier])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG component identifiers must be unique.", -1);
            }
            if (plane.HorizontalSampling < 1 || plane.HorizontalSampling > 4 || plane.VerticalSampling < 1 || plane.VerticalSampling > 4)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG sampling factors must be between one and four.", -1);
            }
            identifiers[plane.Identifier] = true;
            if (plane.HorizontalSampling > maximumHorizontalSampling)
            {
                maximumHorizontalSampling = plane.HorizontalSampling;
            }
            if (plane.VerticalSampling > maximumVerticalSampling)
            {
                maximumVerticalSampling = plane.VerticalSampling;
            }
            index++;
        }

        LosslessJpegImage workingImage = new LosslessJpegImage();
        workingImage.Width = image.Width;
        workingImage.Height = image.Height;
        workingImage.Precision = image.Precision;
        int mcuColumns = LosslessJpegCodec.DivideRoundUp(image.Width, maximumHorizontalSampling);
        int mcuRows = LosslessJpegCodec.DivideRoundUp(image.Height, maximumVerticalSampling);
        long workingBytes = 0;
        index = 0;
        while (index < image.Planes.Count)
        {
            LosslessJpegPlane sourcePlane = image.Planes[index];
            int expectedWidth = LosslessJpegCodec.DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(image.Width, sourcePlane.HorizontalSampling)), maximumHorizontalSampling);
            int expectedHeight = LosslessJpegCodec.DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(image.Height, sourcePlane.VerticalSampling)), maximumVerticalSampling);
            long sampleCount = CheckedMath.Multiply(expectedWidth, expectedHeight);
            if (sourcePlane.Width != expectedWidth || sourcePlane.Height != expectedHeight)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A lossless JPEG component plane does not match its sampling geometry.", -1);
            }
            if (sourcePlane.Samples == null || sourcePlane.Samples.Length != sampleCount)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A lossless JPEG component sample count does not match its dimensions.", -1);
            }

            LosslessJpegPlane workingPlane = new LosslessJpegPlane();
            workingPlane.Identifier = sourcePlane.Identifier;
            workingPlane.Width = expectedWidth;
            workingPlane.Height = expectedHeight;
            workingPlane.HorizontalSampling = sourcePlane.HorizontalSampling;
            workingPlane.VerticalSampling = sourcePlane.VerticalSampling;
            workingPlane.Predictor = parameters.Predictor;
            workingPlane.PointTransform = parameters.PointTransform;
            workingPlane.Samples = sourcePlane.Samples;
            workingPlane.WorkingWidth = CheckedMath.ToInt32(CheckedMath.Multiply(mcuColumns, sourcePlane.HorizontalSampling));
            workingPlane.WorkingHeight = CheckedMath.ToInt32(CheckedMath.Multiply(mcuRows, sourcePlane.VerticalSampling));
            long workingCount = CheckedMath.Multiply(workingPlane.WorkingWidth, workingPlane.WorkingHeight);
            workingBytes = CheckedMath.Add(workingBytes, CheckedMath.Multiply(sampleCount, 2));
            workingBytes = CheckedMath.Add(workingBytes, CheckedMath.Multiply(workingCount, 4));
            CheckedMath.AllocationSize(workingCount, 4, limits);
            workingImage.Planes.Add(workingPlane);
            index++;
        }
        CheckedMath.DecodedSize(workingBytes, 1, limits);

        int maximumSample = (1 << image.Precision) - 1;
        int transformMask = (1 << parameters.PointTransform) - 1;
        index = 0;
        while (index < workingImage.Planes.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LosslessJpegPlane plane = workingImage.Planes[index];
            int workingCount = CheckedMath.ToInt32(CheckedMath.Multiply(plane.WorkingWidth, plane.WorkingHeight));
            plane.ReducedSamples = new int[workingCount];
            int y = 0;
            while (y < plane.WorkingHeight)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int sourceY = y;
                if (sourceY >= plane.Height)
                {
                    sourceY = plane.Height - 1;
                }
                int x = 0;
                while (x < plane.WorkingWidth)
                {
                    int sourceX = x;
                    if (sourceX >= plane.Width)
                    {
                        sourceX = plane.Width - 1;
                    }
                    int sample = (plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceY * plane.Width + sourceX];
                    if (sample > maximumSample)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG input sample exceeds its precision.", -1);
                    }
                    if ((sample & transformMask) != 0)
                    {
                        throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG point transform would discard nonzero sample bits.", -1);
                    }
                    plane.ReducedSamples[y * plane.WorkingWidth + x] = sample >> parameters.PointTransform;
                    x++;
                }
                y++;
            }
            index++;
        }
        return workingImage;
    }

    internal static void ProcessAllScans(LosslessJpegImage image, LosslessJpegEncodeParameters parameters, LosslessJpegHuffmanDefinition? huffman, LosslessJpegOutput? output, long[] frequencies, System.Threading.CancellationToken cancellationToken)
    {
        int maximumHorizontalSampling = GetMaximumHorizontalSampling(image);
        int maximumVerticalSampling = GetMaximumVerticalSampling(image);
        if (parameters.InterleavedScan || image.Planes.Count == 1)
        {
            LosslessJpegPlane[] planes = image.Planes.ToArray();
            ProcessScan(image, planes, parameters, huffman, output, frequencies, maximumHorizontalSampling, maximumVerticalSampling, cancellationToken);
            return;
        }

        int index = 0;
        while (index < image.Planes.Count)
        {
            LosslessJpegPlane[] planes = new LosslessJpegPlane[] { image.Planes[index] };
            ProcessScan(image, planes, parameters, huffman, output, frequencies, maximumHorizontalSampling, maximumVerticalSampling, cancellationToken);
            index++;
        }
    }

    internal static void ProcessScan(LosslessJpegImage image, LosslessJpegPlane[] planes, LosslessJpegEncodeParameters parameters, LosslessJpegHuffmanDefinition? huffman, LosslessJpegOutput? output, long[]? frequencies, int maximumHorizontalSampling, int maximumVerticalSampling, System.Threading.CancellationToken cancellationToken)
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
            mcuColumns = LosslessJpegCodec.DivideRoundUp(image.Width, maximumHorizontalSampling);
            int mcuRows = LosslessJpegCodec.DivideRoundUp(image.Height, maximumVerticalSampling);
            mcuCount = CheckedMath.Multiply(mcuColumns, mcuRows);
        }

        bool[] initialPrediction = new bool[planes.Length];
        System.Array.Fill(initialPrediction, true);
        int restartMarker = 0;
        long mcuIndex = 0;
        while (mcuIndex < mcuCount)
        {
            if ((mcuIndex & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            ProcessMcu(image, planes, parameters, huffman, output, frequencies, initialPrediction, mcuColumns, mcuIndex);
            mcuIndex++;
            if (parameters.RestartInterval > 0 && mcuIndex < mcuCount && mcuIndex % parameters.RestartInterval == 0)
            {
                if (output != null)
                {
                    output.FinishEntropy();
                    output.WriteRawByte(0xff);
                    output.WriteRawByte((byte)(0xd0 + restartMarker));
                }
                restartMarker = (restartMarker + 1) & 7;
                System.Array.Fill(initialPrediction, true);
            }
        }
        if (output != null)
        {
            output.FinishEntropy();
        }
    }

    internal static void ProcessMcu(LosslessJpegImage image, LosslessJpegPlane[] planes, LosslessJpegEncodeParameters parameters, LosslessJpegHuffmanDefinition? huffman, LosslessJpegOutput? output, long[]? frequencies, bool[] initialPrediction, int mcuColumns, long mcuIndex)
    {
        if (planes.Length == 1)
        {
            int x = (int)(mcuIndex % planes[0].Width);
            int y = (int)(mcuIndex / planes[0].Width);
            ProcessSample(planes[0], x, y, parameters, huffman, output, frequencies, initialPrediction, 0, image.Precision);
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
                    ProcessSample(plane, x, y, parameters, huffman, output, frequencies, initialPrediction, componentIndex, image.Precision);
                    horizontal++;
                }
                vertical++;
            }
            componentIndex++;
        }
    }

    internal static void ProcessSample(LosslessJpegPlane plane, int x, int y, LosslessJpegEncodeParameters parameters, LosslessJpegHuffmanDefinition? huffman, LosslessJpegOutput? output, long[]? frequencies, bool[] initialPrediction, int componentIndex, int precision)
    {
        int effectivePrecision = precision - parameters.PointTransform;
        int prediction;
        if (initialPrediction[componentIndex])
        {
            prediction = 1 << (effectivePrecision - 1);
            initialPrediction[componentIndex] = false;
        }
        else
        {
            prediction = LosslessJpegCodec.GetPrediction(plane, x, y, parameters.Predictor, effectivePrecision);
        }
        int sample = (plane.ReducedSamples ?? throw new System.InvalidOperationException("A required value is unavailable."))[y * plane.WorkingWidth + x];
        int difference = (sample - prediction) & 65535;
        if (difference > 32768)
        {
            difference -= 65536;
        }
        int category = GetDifferenceCategory(difference);
        if (frequencies != null)
        {
            frequencies[category]++;
        }
        if (output != null)
        {
            HuffmanTable? table = (huffman ?? throw new System.InvalidOperationException("A required value is unavailable.")).Table;
            output.WriteBits((table ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetEncodeCode((byte)category), table.GetEncodeCodeLength((byte)category));
            if (category != 0 && category != 16)
            {
                int amplitude = difference;
                if (difference < 0)
                {
                    amplitude += (1 << category) - 1;
                }
                output.WriteBits((uint)amplitude, category);
            }
        }
    }

    internal static int GetDifferenceCategory(int difference)
    {
        if (difference == 32768)
        {
            return 16;
        }
        int magnitude = difference < 0 ? -difference : difference;
        int category = 0;
        while (magnitude != 0)
        {
            category++;
            magnitude >>= 1;
        }
        return category;
    }

    internal static LosslessJpegHuffmanDefinition BuildFixedHuffmanTable()
    {
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = new byte[17];
        int index = 0;
        while (index < symbols.Length)
        {
            symbols[index] = (byte)index;
            index++;
        }
        return CreateHuffmanDefinition(counts, symbols);
    }

    internal static LosslessJpegHuffmanDefinition BuildOptimizedHuffmanTable(long[] categoryFrequencies)
    {
        if (categoryFrequencies == null || categoryFrequencies.Length != 17)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG category frequencies must contain 17 values.", -1);
        }
        long[] frequencies = new long[257];
        int[] codeSizes = new int[257];
        int[] others = new int[257];
        System.Array.Fill(others, -1);
        int index = 0;
        while (index < 17)
        {
            if (categoryFrequencies[index] < 0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Lossless JPEG category frequencies cannot be negative.", -1);
            }
            frequencies[index] = categoryFrequencies[index];
            index++;
        }
        frequencies[256] = 1;

        while (true)
        {
            int first = FindSmallestFrequency(frequencies, -1);
            int second = FindSmallestFrequency(frequencies, first);
            if (second < 0)
            {
                break;
            }
            frequencies[first] = CheckedMath.Add(frequencies[first], frequencies[second]);
            frequencies[second] = 0;
            IncrementCodeSizes(first, second, codeSizes, others);
        }

        int[] lengthCounts = new int[33];
        index = 0;
        while (index < codeSizes.Length)
        {
            if (codeSizes[index] > 32)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "Optimized lossless JPEG Huffman depth exceeds its planning bound.", -1);
            }
            if (codeSizes[index] > 0)
            {
                lengthCounts[codeSizes[index]]++;
            }
            index++;
        }
        int length = 32;
        while (length > 16)
        {
            while (lengthCounts[length] > 0)
            {
                int shorter = length - 2;
                while (shorter > 0 && lengthCounts[shorter] == 0)
                {
                    shorter--;
                }
                if (shorter == 0 || lengthCounts[length] < 2)
                {
                    throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "Optimized lossless JPEG Huffman lengths cannot be limited to 16 bits.", -1);
                }
                lengthCounts[length] -= 2;
                lengthCounts[length - 1]++;
                lengthCounts[shorter + 1] += 2;
                lengthCounts[shorter]--;
            }
            length--;
        }
        length = 16;
        while (length > 0 && lengthCounts[length] == 0)
        {
            length--;
        }
        if (length == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "Optimized lossless JPEG Huffman planning produced no symbols.", -1);
        }
        lengthCounts[length]--;

        System.Collections.Generic.List<byte> orderedSymbols = new System.Collections.Generic.List<byte>();
        int size = 1;
        while (size <= 32)
        {
            index = 0;
            while (index < 17)
            {
                if (codeSizes[index] == size)
                {
                    orderedSymbols.Add((byte)index);
                }
                index++;
            }
            size++;
        }
        int symbolCount = 0;
        size = 1;
        while (size <= 16)
        {
            symbolCount += lengthCounts[size];
            size++;
        }
        if (symbolCount != orderedSymbols.Count || symbolCount == 0)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "Optimized lossless JPEG Huffman symbol counts are inconsistent.", -1);
        }
        byte[] counts = new byte[16];
        size = 1;
        while (size <= 16)
        {
            counts[size - 1] = (byte)lengthCounts[size];
            size++;
        }
        return CreateHuffmanDefinition(counts, orderedSymbols.ToArray());
    }

    internal static int FindSmallestFrequency(long[] frequencies, int excluded)
    {
        int selected = -1;
        int index = 0;
        while (index < frequencies.Length)
        {
            if (index != excluded && frequencies[index] > 0)
            {
                if (selected < 0 || frequencies[index] < frequencies[selected] || frequencies[index] == frequencies[selected] && index > selected)
                {
                    selected = index;
                }
            }
            index++;
        }
        return selected;
    }

    internal static void IncrementCodeSizes(int first, int second, int[] codeSizes, int[] others)
    {
        int current = first;
        codeSizes[current]++;
        while (others[current] >= 0)
        {
            current = others[current];
            codeSizes[current]++;
        }
        others[current] = second;
        current = second;
        codeSizes[current]++;
        while (others[current] >= 0)
        {
            current = others[current];
            codeSizes[current]++;
        }
    }

    internal static LosslessJpegHuffmanDefinition CreateHuffmanDefinition(byte[] counts, byte[] symbols)
    {
        LosslessJpegHuffmanDefinition definition = new LosslessJpegHuffmanDefinition();
        definition.CodeLengthCounts = counts;
        definition.Symbols = symbols;
        definition.Table = new HuffmanTable(counts, symbols, 8);
        if (definition.Table.IsComplete())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "A lossless JPEG Huffman table cannot assign an all-ones code.", -1);
        }
        return definition;
    }

    internal static void WriteFile(LosslessJpegOutput output, LosslessJpegImage image, LosslessJpegEncodeParameters parameters, LosslessJpegHuffmanDefinition huffman, System.Threading.CancellationToken cancellationToken)
    {
        WriteStandaloneMarker(output, 0xd8);
        WriteHuffmanMarker(output, huffman);
        WriteFrameMarker(output, image);
        if (parameters.RestartInterval > 0)
        {
            output.WriteRawByte(0xff);
            output.WriteRawByte(0xdd);
            output.WriteRawUInt16(4);
            output.WriteRawUInt16(parameters.RestartInterval);
        }
        if (parameters.InterleavedScan || image.Planes.Count == 1)
        {
            LosslessJpegPlane[] planes = image.Planes.ToArray();
            WriteScanMarker(output, planes, parameters);
            ProcessScan(image, planes, parameters, huffman, output, null, GetMaximumHorizontalSampling(image), GetMaximumVerticalSampling(image), cancellationToken);
        }
        else
        {
            int index = 0;
            while (index < image.Planes.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();
                LosslessJpegPlane[] planes = new LosslessJpegPlane[] { image.Planes[index] };
                WriteScanMarker(output, planes, parameters);
                ProcessScan(image, planes, parameters, huffman, output, null, GetMaximumHorizontalSampling(image), GetMaximumVerticalSampling(image), cancellationToken);
                index++;
            }
        }
        WriteStandaloneMarker(output, 0xd9);
    }

    internal static void WriteHuffmanMarker(LosslessJpegOutput output, LosslessJpegHuffmanDefinition huffman)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xc4);
        output.WriteRawUInt16(19 + (huffman.Symbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length);
        output.WriteRawByte(0x00);
        int index = 0;
        while (index < (huffman.CodeLengthCounts ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            output.WriteRawByte(huffman.CodeLengthCounts[index]);
            index++;
        }
        index = 0;
        while (index < huffman.Symbols.Length)
        {
            output.WriteRawByte(huffman.Symbols[index]);
            index++;
        }
    }

    internal static void WriteFrameMarker(LosslessJpegOutput output, LosslessJpegImage image)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xc3);
        output.WriteRawUInt16(8 + image.Planes.Count * 3);
        output.WriteRawByte((byte)image.Precision);
        output.WriteRawUInt16(image.Height);
        output.WriteRawUInt16(image.Width);
        output.WriteRawByte((byte)image.Planes.Count);
        int index = 0;
        while (index < image.Planes.Count)
        {
            LosslessJpegPlane plane = image.Planes[index];
            output.WriteRawByte(plane.Identifier);
            output.WriteRawByte((byte)((plane.HorizontalSampling << 4) | plane.VerticalSampling));
            output.WriteRawByte(0);
            index++;
        }
    }

    internal static void WriteScanMarker(LosslessJpegOutput output, LosslessJpegPlane[] planes, LosslessJpegEncodeParameters parameters)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xda);
        output.WriteRawUInt16(6 + planes.Length * 2);
        output.WriteRawByte((byte)planes.Length);
        int index = 0;
        while (index < planes.Length)
        {
            output.WriteRawByte(planes[index].Identifier);
            output.WriteRawByte(0x00);
            index++;
        }
        output.WriteRawByte((byte)parameters.Predictor);
        output.WriteRawByte(0);
        output.WriteRawByte((byte)parameters.PointTransform);
    }

    internal static void WriteStandaloneMarker(LosslessJpegOutput output, byte marker)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(marker);
    }

    internal static int GetMaximumHorizontalSampling(LosslessJpegImage image)
    {
        int maximum = 0;
        int index = 0;
        while (index < image.Planes.Count)
        {
            if (image.Planes[index].HorizontalSampling > maximum)
            {
                maximum = image.Planes[index].HorizontalSampling;
            }
            index++;
        }
        return maximum;
    }

    internal static int GetMaximumVerticalSampling(LosslessJpegImage image)
    {
        int maximum = 0;
        int index = 0;
        while (index < image.Planes.Count)
        {
            if (image.Planes[index].VerticalSampling > maximum)
            {
                maximum = image.Planes[index].VerticalSampling;
            }
            index++;
        }
        return maximum;
    }
}
