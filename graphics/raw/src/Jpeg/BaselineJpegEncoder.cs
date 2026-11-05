namespace raw;

internal static class BaselineJpegEncoder
{
    internal static readonly int[] LuminanceQuantizationBase = new int[]
    {
        16, 11, 10, 16, 24, 40, 51, 61,
        12, 12, 14, 19, 26, 58, 60, 55,
        14, 13, 16, 24, 40, 57, 69, 56,
        14, 17, 22, 29, 51, 87, 80, 62,
        18, 22, 37, 56, 68, 109, 103, 77,
        24, 35, 55, 64, 81, 104, 113, 92,
        49, 64, 78, 87, 103, 121, 120, 101,
        72, 92, 95, 98, 112, 100, 103, 99
    };
    internal static readonly int[] ChrominanceQuantizationBase = new int[]
    {
        17, 18, 24, 47, 99, 99, 99, 99,
        18, 21, 26, 66, 99, 99, 99, 99,
        24, 26, 56, 99, 99, 99, 99, 99,
        47, 66, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99,
        99, 99, 99, 99, 99, 99, 99, 99
    };

    internal static byte[] Encode(BaselineJpegImage? image, BaselineJpegEncodeParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        BaselineJpegImage workingImage = ValidateAndPrepare(image, parameters, limits, cancellationToken);
        byte[] luminanceQuantization = BuildQuantizationTable((parameters ?? throw new System.InvalidOperationException("A required value is unavailable.")).Quality, false);
        byte[] chrominanceQuantization = BuildQuantizationTable(parameters.Quality, true);
        BaselineJpegHuffmanDefinition dcHuffman = BuildDcHuffmanTable();
        BaselineJpegHuffmanDefinition acHuffman = BuildAcHuffmanTable();

        BaselineJpegOutput counter = new BaselineJpegOutput(null);
        WriteFile(counter, workingImage, parameters, luminanceQuantization, chrominanceQuantization, dcHuffman, acHuffman, cancellationToken);
        int encodedLength = counter.Offset;
        CheckedMath.FileSize(encodedLength, limits);
        CheckedMath.AllocationSize(encodedLength, 1, limits);
        cancellationToken.ThrowIfCancellationRequested();
        byte[] destination = new byte[encodedLength];
        BaselineJpegOutput output = new BaselineJpegOutput(destination);
        WriteFile(output, workingImage, parameters, luminanceQuantization, chrominanceQuantization, dcHuffman, acHuffman, cancellationToken);
        if (output.Offset != encodedLength)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidLength, "Baseline JPEG output does not match its planned length.", output.Offset);
        }
        return destination;
    }

    internal static BaselineJpegImage ValidateAndPrepare(BaselineJpegImage? image, BaselineJpegEncodeParameters? parameters, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        if (image == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A baseline JPEG image is required.", -1);
        }
        if (parameters == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG encoding parameters are required.", -1);
        }
        CheckedMath.RequireLimits(limits);
        if (parameters.Quality < 1 || parameters.Quality > 100)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG quality must be between one and 100.", -1);
        }
        bool commonSampling = parameters.HorizontalSampling == 1 && parameters.VerticalSampling == 1 || parameters.HorizontalSampling == 2 && parameters.VerticalSampling == 1 || parameters.HorizontalSampling == 2 && parameters.VerticalSampling == 2;
        if (!commonSampling)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG sampling must be 4:4:4, 4:2:2, or 4:2:0.", -1);
        }
        if (parameters.RestartInterval < 0 || parameters.RestartInterval > 65535)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The baseline JPEG restart interval must fit an unsigned 16-bit value.", -1);
        }
        if (image.Precision != 8)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG encoding requires eight-bit sample precision.", -1);
        }
        CheckedMath.PixelCount(image.Width, image.Height, limits);
        if (image.Planes == null || image.Planes.Count != 1 && image.Planes.Count != 3)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG encoding requires one grayscale plane or three YCbCr planes.", -1);
        }
        if (image.Planes.Count == 1 && (parameters.HorizontalSampling != 1 || parameters.VerticalSampling != 1))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Grayscale baseline JPEG encoding requires one-by-one sampling.", -1);
        }

        BaselineJpegImage workingImage = new BaselineJpegImage();
        workingImage.Width = image.Width;
        workingImage.Height = image.Height;
        workingImage.Precision = image.Precision;
        bool[] identifiers = new bool[256];
        long totalSamples = 0;
        int index = 0;
        while (index < image.Planes.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BaselineJpegPlane sourcePlane = image.Planes[index];
            if (sourcePlane == null)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A baseline JPEG component plane is required.", -1);
            }
            if (identifiers[sourcePlane.Identifier])
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG component identifiers must be unique.", -1);
            }
            identifiers[sourcePlane.Identifier] = true;
            int horizontalSampling = image.Planes.Count == 1 ? 1 : index == 0 ? parameters.HorizontalSampling : 1;
            int verticalSampling = image.Planes.Count == 1 ? 1 : index == 0 ? parameters.VerticalSampling : 1;
            int maximumHorizontalSampling = image.Planes.Count == 1 ? 1 : parameters.HorizontalSampling;
            int maximumVerticalSampling = image.Planes.Count == 1 ? 1 : parameters.VerticalSampling;
            int expectedWidth = BaselineJpegCodec.DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(image.Width, horizontalSampling)), maximumHorizontalSampling);
            int expectedHeight = BaselineJpegCodec.DivideRoundUp(CheckedMath.ToInt32(CheckedMath.Multiply(image.Height, verticalSampling)), maximumVerticalSampling);
            long sampleCount = CheckedMath.Multiply(expectedWidth, expectedHeight);
            if (sourcePlane.HorizontalSampling != horizontalSampling || sourcePlane.VerticalSampling != verticalSampling || sourcePlane.Width != expectedWidth || sourcePlane.Height != expectedHeight)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A baseline JPEG component plane does not match the selected sampling geometry.", -1);
            }
            if (sourcePlane.Samples == null || sourcePlane.Samples.Length != sampleCount)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "A baseline JPEG component sample count does not match its dimensions.", -1);
            }
            CheckedMath.AllocationSize(sampleCount, 1, limits);
            totalSamples = CheckedMath.Add(totalSamples, sampleCount);
            BaselineJpegPlane plane = new BaselineJpegPlane();
            plane.Identifier = sourcePlane.Identifier;
            plane.Width = sourcePlane.Width;
            plane.Height = sourcePlane.Height;
            plane.HorizontalSampling = horizontalSampling;
            plane.VerticalSampling = verticalSampling;
            plane.Samples = sourcePlane.Samples;
            workingImage.Planes.Add(plane);
            index++;
        }
        CheckedMath.DecodedSize(totalSamples, 1, limits);
        return workingImage;
    }

    internal static byte[] BuildQuantizationTable(int quality, bool chrominance)
    {
        if (quality < 1 || quality > 100)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Baseline JPEG quality must be between one and 100.", -1);
        }
        int scale = quality < 50 ? 5000 / quality : 200 - quality * 2;
        int[] source = chrominance ? ChrominanceQuantizationBase : LuminanceQuantizationBase;
        byte[] table = new byte[64];
        int index = 0;
        while (index < table.Length)
        {
            int value = (source[index] * scale + 50) / 100;
            if (value < 1)
            {
                value = 1;
            }
            else if (value > 255)
            {
                value = 255;
            }
            table[index] = (byte)value;
            index++;
        }
        return table;
    }

    internal static BaselineJpegHuffmanDefinition BuildDcHuffmanTable()
    {
        byte[] counts = new byte[16];
        counts[3] = 12;
        byte[] symbols = new byte[12];
        int index = 0;
        while (index < symbols.Length)
        {
            symbols[index] = (byte)index;
            index++;
        }
        return CreateHuffmanDefinition(0, 0, counts, symbols);
    }

    internal static BaselineJpegHuffmanDefinition BuildAcHuffmanTable()
    {
        byte[] counts = new byte[16];
        counts[7] = 162;
        byte[] symbols = new byte[162];
        symbols[0] = 0;
        int offset = 1;
        int run = 0;
        while (run < 16)
        {
            int size = 1;
            while (size <= 10)
            {
                symbols[offset] = (byte)((run << 4) | size);
                offset++;
                size++;
            }
            run++;
        }
        symbols[offset] = 0xf0;
        return CreateHuffmanDefinition(1, 0, counts, symbols);
    }

    internal static BaselineJpegHuffmanDefinition CreateHuffmanDefinition(byte tableClass, byte identifier, byte[] counts, byte[] symbols)
    {
        BaselineJpegHuffmanDefinition definition = new BaselineJpegHuffmanDefinition();
        definition.TableClass = tableClass;
        definition.Identifier = identifier;
        definition.CodeLengthCounts = counts;
        definition.Symbols = symbols;
        definition.Table = new HuffmanTable(counts, symbols, 8);
        if (definition.Table.IsComplete())
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG Huffman table cannot assign an all-ones code.", -1);
        }
        return definition;
    }

    internal static void WriteFile(BaselineJpegOutput output, BaselineJpegImage image, BaselineJpegEncodeParameters parameters, byte[] luminanceQuantization, byte[] chrominanceQuantization, BaselineJpegHuffmanDefinition dcHuffman, BaselineJpegHuffmanDefinition acHuffman, System.Threading.CancellationToken cancellationToken)
    {
        WriteStandaloneMarker(output, 0xd8);
        WriteQuantizationMarker(output, luminanceQuantization, chrominanceQuantization, image.Planes.Count == 3);
        WriteHuffmanMarker(output, dcHuffman, acHuffman);
        WriteFrameMarker(output, image);
        if (parameters.RestartInterval > 0)
        {
            output.WriteRawByte(0xff);
            output.WriteRawByte(0xdd);
            output.WriteRawUInt16(4);
            output.WriteRawUInt16(parameters.RestartInterval);
        }
        WriteScanMarker(output, image);
        ProcessScan(output, image, parameters, luminanceQuantization, chrominanceQuantization, dcHuffman.Table, acHuffman.Table, cancellationToken);
        WriteStandaloneMarker(output, 0xd9);
    }

    internal static void ProcessScan(BaselineJpegOutput output, BaselineJpegImage image, BaselineJpegEncodeParameters parameters, byte[] luminanceQuantization, byte[] chrominanceQuantization, HuffmanTable? dcHuffman, HuffmanTable? acHuffman, System.Threading.CancellationToken cancellationToken)
    {
        int maximumHorizontalSampling = image.Planes.Count == 1 ? 1 : parameters.HorizontalSampling;
        int maximumVerticalSampling = image.Planes.Count == 1 ? 1 : parameters.VerticalSampling;
        int mcuColumns = BaselineJpegCodec.DivideRoundUp(image.Width, maximumHorizontalSampling * 8);
        int mcuRows = BaselineJpegCodec.DivideRoundUp(image.Height, maximumVerticalSampling * 8);
        long mcuCount = CheckedMath.Multiply(mcuColumns, mcuRows);
        int[] dcPredictions = new int[image.Planes.Count];
        int[] coefficients = new int[64];
        double[] workspace = new double[64];
        int restartMarker = 0;
        long mcuIndex = 0;
        while (mcuIndex < mcuCount)
        {
            if ((mcuIndex & 4095) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            int mcuX = (int)(mcuIndex % mcuColumns);
            int mcuY = (int)(mcuIndex / mcuColumns);
            int componentIndex = 0;
            while (componentIndex < image.Planes.Count)
            {
                BaselineJpegPlane plane = image.Planes[componentIndex];
                byte[] quantization = componentIndex == 0 ? luminanceQuantization : chrominanceQuantization;
                int vertical = 0;
                while (vertical < plane.VerticalSampling)
                {
                    int horizontal = 0;
                    while (horizontal < plane.HorizontalSampling)
                    {
                        int blockX = mcuX * plane.HorizontalSampling + horizontal;
                        int blockY = mcuY * plane.VerticalSampling + vertical;
                        ForwardDct(plane, blockX, blockY, quantization, coefficients, workspace);
                        EncodeBlock(output, coefficients, dcPredictions, componentIndex, dcHuffman, acHuffman);
                        horizontal++;
                    }
                    vertical++;
                }
                componentIndex++;
            }
            mcuIndex++;
            if (parameters.RestartInterval > 0 && mcuIndex < mcuCount && mcuIndex % parameters.RestartInterval == 0)
            {
                output.FinishEntropy();
                output.WriteRawByte(0xff);
                output.WriteRawByte((byte)(0xd0 + restartMarker));
                restartMarker = (restartMarker + 1) & 7;
                System.Array.Clear(dcPredictions, 0, dcPredictions.Length);
            }
        }
        output.FinishEntropy();
    }

    internal static void ForwardDct(BaselineJpegPlane plane, int blockX, int blockY, byte[] quantization, int[] coefficients, double[] workspace)
    {
        int y = 0;
        while (y < 8)
        {
            int sourceY = blockY * 8 + y;
            if (sourceY >= plane.Height)
            {
                sourceY = plane.Height - 1;
            }
            int frequencyX = 0;
            while (frequencyX < 8)
            {
                double sum = 0;
                int x = 0;
                while (x < 8)
                {
                    int sourceX = blockX * 8 + x;
                    if (sourceX >= plane.Width)
                    {
                        sourceX = plane.Width - 1;
                    }
                    int sample = (plane.Samples ?? throw new System.InvalidOperationException("A required value is unavailable."))[sourceY * plane.Width + sourceX] - 128;
                    sum += sample * BaselineJpegCodec.InverseTransform[x * 8 + frequencyX];
                    x++;
                }
                workspace[y * 8 + frequencyX] = sum;
                frequencyX++;
            }
            y++;
        }

        int frequencyY = 0;
        while (frequencyY < 8)
        {
            int frequencyX = 0;
            while (frequencyX < 8)
            {
                double sum = 0;
                y = 0;
                while (y < 8)
                {
                    sum += workspace[y * 8 + frequencyX] * BaselineJpegCodec.InverseTransform[y * 8 + frequencyY];
                    y++;
                }
                double scaled = sum / quantization[frequencyY * 8 + frequencyX];
                int quantized = scaled < 0 ? (int)System.Math.Ceiling(scaled - 0.5) : (int)System.Math.Floor(scaled + 0.5);
                coefficients[frequencyY * 8 + frequencyX] = quantized;
                frequencyX++;
            }
            frequencyY++;
        }
    }

    internal static void EncodeBlock(BaselineJpegOutput output, int[] coefficients, int[] dcPredictions, int componentIndex, HuffmanTable? dcHuffman, HuffmanTable? acHuffman)
    {
        int difference = coefficients[0] - dcPredictions[componentIndex];
        dcPredictions[componentIndex] = coefficients[0];
        int category = GetCategory(difference);
        if (category > 11)
        {
            throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG DC difference exceeds eleven bits.", output.Offset);
        }
        WriteHuffmanValue(output, dcHuffman, difference, category);

        int zeroRun = 0;
        int index = 1;
        while (index < 64)
        {
            int value = coefficients[BaselineJpegCodec.ZigZag[index]];
            if (value == 0)
            {
                zeroRun++;
                index++;
                continue;
            }
            while (zeroRun >= 16)
            {
                output.WriteBits((acHuffman ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetEncodeCode(0xf0), acHuffman.GetEncodeCodeLength(0xf0));
                zeroRun -= 16;
            }
            category = GetCategory(value);
            if (category > 10)
            {
                throw new RawWriteException(RawDiagnosticCode.InvalidCodecData, "A baseline JPEG AC coefficient exceeds ten bits.", output.Offset);
            }
            byte symbol = (byte)((zeroRun << 4) | category);
            WriteHuffmanValue(output, acHuffman, value, symbol);
            zeroRun = 0;
            index++;
        }
        if (zeroRun != 0)
        {
            output.WriteBits((acHuffman ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetEncodeCode(0), acHuffman.GetEncodeCodeLength(0));
        }
    }

    internal static void WriteHuffmanValue(BaselineJpegOutput output, HuffmanTable? table, int value, int symbol)
    {
        output.WriteBits((table ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetEncodeCode((byte)symbol), table.GetEncodeCodeLength((byte)symbol));
        int category = GetCategory(value);
        if (category != 0)
        {
            int amplitude = value;
            if (value < 0)
            {
                amplitude += (1 << category) - 1;
            }
            output.WriteBits((uint)amplitude, category);
        }
    }

    internal static int GetCategory(int value)
    {
        int magnitude = value < 0 ? -value : value;
        int category = 0;
        while (magnitude != 0)
        {
            category++;
            magnitude >>= 1;
        }
        return category;
    }

    internal static void WriteQuantizationMarker(BaselineJpegOutput output, byte[] luminance, byte[] chrominance, bool includeChrominance)
    {
        int tableCount = includeChrominance ? 2 : 1;
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xdb);
        output.WriteRawUInt16(2 + tableCount * 65);
        WriteQuantizationTable(output, 0, luminance);
        if (includeChrominance)
        {
            WriteQuantizationTable(output, 1, chrominance);
        }
    }

    internal static void WriteQuantizationTable(BaselineJpegOutput output, int identifier, byte[] table)
    {
        output.WriteRawByte((byte)identifier);
        int index = 0;
        while (index < 64)
        {
            output.WriteRawByte(table[BaselineJpegCodec.ZigZag[index]]);
            index++;
        }
    }

    internal static void WriteHuffmanMarker(BaselineJpegOutput output, BaselineJpegHuffmanDefinition dc, BaselineJpegHuffmanDefinition ac)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xc4);
        output.WriteRawUInt16(2 + 17 + (dc.Symbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length + 17 + (ac.Symbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length);
        WriteHuffmanTable(output, dc);
        WriteHuffmanTable(output, ac);
    }

    internal static void WriteHuffmanTable(BaselineJpegOutput output, BaselineJpegHuffmanDefinition definition)
    {
        output.WriteRawByte((byte)((definition.TableClass << 4) | definition.Identifier));
        int index = 0;
        while (index < (definition.CodeLengthCounts ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            output.WriteRawByte(definition.CodeLengthCounts[index]);
            index++;
        }
        index = 0;
        while (index < (definition.Symbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
        {
            output.WriteRawByte(definition.Symbols[index]);
            index++;
        }
    }

    internal static void WriteFrameMarker(BaselineJpegOutput output, BaselineJpegImage image)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xc0);
        output.WriteRawUInt16(8 + image.Planes.Count * 3);
        output.WriteRawByte(8);
        output.WriteRawUInt16(image.Height);
        output.WriteRawUInt16(image.Width);
        output.WriteRawByte((byte)image.Planes.Count);
        int index = 0;
        while (index < image.Planes.Count)
        {
            BaselineJpegPlane plane = image.Planes[index];
            output.WriteRawByte(plane.Identifier);
            output.WriteRawByte((byte)((plane.HorizontalSampling << 4) | plane.VerticalSampling));
            output.WriteRawByte((byte)(index == 0 ? 0 : 1));
            index++;
        }
    }

    internal static void WriteScanMarker(BaselineJpegOutput output, BaselineJpegImage image)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(0xda);
        output.WriteRawUInt16(6 + image.Planes.Count * 2);
        output.WriteRawByte((byte)image.Planes.Count);
        int index = 0;
        while (index < image.Planes.Count)
        {
            output.WriteRawByte(image.Planes[index].Identifier);
            output.WriteRawByte(0);
            index++;
        }
        output.WriteRawByte(0);
        output.WriteRawByte(63);
        output.WriteRawByte(0);
    }

    internal static void WriteStandaloneMarker(BaselineJpegOutput output, byte marker)
    {
        output.WriteRawByte(0xff);
        output.WriteRawByte(marker);
    }
}
