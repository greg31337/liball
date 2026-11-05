namespace icc;

internal static class Icc2ProcessingElementTagCodec
{
    internal const uint SingleSampledCurveSignature = 0x736e6766;
    internal const uint ToneMapFunctionSignature = 0x6d617066;

    internal static void DecodeStandaloneSegmentedCurve(TagDecodeState state,
        System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        MultiProcessCurveDecodeResult result = DecodeIcc2SegmentedCurve(payload,
            state.AbsolutePayloadOffset);
        if (result.Error.Length != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature,
                result.Error);
            return;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload,
            typeSignature, IccTagDataKind.Curve);
        data.Curve = result.Curve;
        TagCodecSupport.CompleteSuccessfulDecode(state, data,
            "Decoded an ICC.2 segmented curve.");
    }

    internal static bool IsBodyCodecAvailable(IccProcessingElementKind kind)
    {
        switch (kind)
        {
            case IccProcessingElementKind.CurveSet:
            case IccProcessingElementKind.Matrix:
            case IccProcessingElementKind.Clut:
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
            case IccProcessingElementKind.ExtendedClut:
            case IccProcessingElementKind.SparseMatrix:
            case IccProcessingElementKind.TintArray:
            case IccProcessingElementKind.ToneMap:
            case IccProcessingElementKind.XyzToJab:
            case IccProcessingElementKind.JabToXyz:
            case IccProcessingElementKind.EmissionClut:
            case IccProcessingElementKind.EmissionMatrix:
            case IccProcessingElementKind.EmissionObserver:
            case IccProcessingElementKind.InverseEmissionMatrix:
            case IccProcessingElementKind.ReflectanceClut:
            case IccProcessingElementKind.ReflectanceObserver:
            case IccProcessingElementKind.Calculator:
                return true;
            default:
                return false;
        }
    }

    internal static string DecodeKnownElement(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset, TagDecodeState state, int nestingDepth)
    {
        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                return DecodeCurveSet(encoded, element, absoluteOffset);
            case IccProcessingElementKind.Matrix:
                return MultiProcessElementTagCodec.DecodeMatrix(encoded, element, absoluteOffset);
            case IccProcessingElementKind.Clut:
                return MultiProcessElementTagCodec.DecodeClut(encoded, element, absoluteOffset);
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
                return MultiProcessElementTagCodec.DecodeAcs(encoded, element);
            case IccProcessingElementKind.ExtendedClut:
                return DecodeExtendedClut(encoded, element, absoluteOffset);
            case IccProcessingElementKind.SparseMatrix:
                return DecodeSparseMatrix(encoded, element, absoluteOffset);
            case IccProcessingElementKind.TintArray:
                return DecodeTintArray(encoded, element, absoluteOffset);
            case IccProcessingElementKind.ToneMap:
                return DecodeToneMap(encoded, element, absoluteOffset);
            case IccProcessingElementKind.XyzToJab:
            case IccProcessingElementKind.JabToXyz:
                return DecodeCamElement(encoded, element);
            case IccProcessingElementKind.EmissionClut:
            case IccProcessingElementKind.ReflectanceClut:
                return DecodeSpectralClut(encoded, element, absoluteOffset);
            case IccProcessingElementKind.EmissionMatrix:
            case IccProcessingElementKind.InverseEmissionMatrix:
                return DecodeSpectralMatrix(encoded, element, absoluteOffset);
            case IccProcessingElementKind.EmissionObserver:
            case IccProcessingElementKind.ReflectanceObserver:
                return DecodeSpectralObserver(encoded, element, absoluteOffset);
            case IccProcessingElementKind.Calculator:
                return CalculatorElementCodec.DecodeCalculator(encoded, element, absoluteOffset, state, nestingDepth);
            default:
                return string.Empty;
        }
    }

    internal static byte[] SerializeKnownElement(IccProcessingElement element, int elementIndex)
    {
        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                return SerializeCurveSet(element, elementIndex);
            case IccProcessingElementKind.Matrix:
                return MultiProcessElementTagCodec.SerializeMatrix(element, elementIndex);
            case IccProcessingElementKind.Clut:
                return MultiProcessElementTagCodec.SerializeClut(element, elementIndex);
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
                return MultiProcessElementTagCodec.SerializeAcs(element, elementIndex);
            case IccProcessingElementKind.ExtendedClut:
                return SerializeExtendedClut(element, elementIndex);
            case IccProcessingElementKind.SparseMatrix:
                return SerializeSparseMatrix(element, elementIndex);
            case IccProcessingElementKind.TintArray:
                return SerializeTintArray(element, elementIndex);
            case IccProcessingElementKind.ToneMap:
                return SerializeToneMap(element, elementIndex);
            case IccProcessingElementKind.XyzToJab:
            case IccProcessingElementKind.JabToXyz:
                return SerializeCamElement(element, elementIndex);
            case IccProcessingElementKind.EmissionClut:
            case IccProcessingElementKind.ReflectanceClut:
                return SerializeSpectralClut(element, elementIndex);
            case IccProcessingElementKind.EmissionMatrix:
            case IccProcessingElementKind.InverseEmissionMatrix:
                return SerializeSpectralMatrix(element, elementIndex);
            case IccProcessingElementKind.EmissionObserver:
            case IccProcessingElementKind.ReflectanceObserver:
                return SerializeSpectralObserver(element, elementIndex);
            case IccProcessingElementKind.Calculator:
                return CalculatorElementCodec.SerializeCalculator(element, elementIndex);
            default:
                MultiProcessElementsTagCodec.ValidateElementForWriteVersion5(element, elementIndex);
                return element.RawData.Bytes;
        }
    }

    internal static string DecodeCurveSet(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels != element.OutputChannels)
        {
            return "an ICC.2 curve-set element shall have equal input and output channel counts.";
        }

        int dataStart = 12 + (element.InputChannels * 8);
        if (element.InputChannels > (int.MaxValue - 12) / 8 || dataStart > encoded.Length)
        {
            return "the ICC.2 curve-set position table extends beyond the element.";
        }

        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.InputChannels];
        BigEndianReader reader = new BigEndianReader(encoded.Slice(12, element.InputChannels * 8), absoluteOffset + 12);
        int index = 0;
        while (index < positions.Length)
        {
            uint offsetValue = reader.ReadUInt32();
            uint sizeValue = reader.ReadUInt32();
            if (offsetValue > int.MaxValue || sizeValue > int.MaxValue)
            {
                return $"curve position {index} exceeds the supported managed range.";
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)offsetValue;
            position.Size = (int)sizeValue;
            if (position.Offset < dataStart || position.Offset % 4 != 0 || position.Size < 12 || position.Size > encoded.Length - position.Offset)
            {
                return $"curve position {index} shall identify a complete aligned ICC.2 curve.";
            }

            int paddedSize = MultiProcessElementsTagCodec.AlignFour(position.Size);
            if (paddedSize > encoded.Length - position.Offset || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, position.Offset + position.Size, paddedSize - position.Size))
            {
                return $"curve position {index} has missing or non-zero alignment padding.";
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = MultiProcessElementsTagCodec.ValidatePositionTopology(positions, dataStart, encoded.Length);
        if (topologyError.Length > 0)
        {
            return $"the curve-set storage is invalid: {topologyError}";
        }

        IccCurve[] curves = new IccCurve[element.InputChannels];
        System.Collections.Generic.List<MultiProcessElementPosition> decodedPositions = new System.Collections.Generic.List<MultiProcessElementPosition>();
        System.Collections.Generic.List<IccCurve> decodedCurves = new System.Collections.Generic.List<IccCurve>();
        index = 0;
        while (index < positions.Length)
        {
            IccCurve? shared = MultiProcessElementTagCodec.FindDecodedCurve(decodedPositions, decodedCurves, positions[index]);
            if (shared != null)
            {
                curves[index] = shared;
            }
            else
            {
                MultiProcessCurveDecodeResult result = DecodeIcc2Curve(encoded.Slice(positions[index].Offset, positions[index].Size), absoluteOffset + positions[index].Offset);
                if (result.Error.Length > 0)
                {
                    return $"curve {index}: {result.Error}";
                }

                curves[index] = result.Curve;
                decodedPositions.Add(positions[index]);
                decodedCurves.Add(result.Curve);
            }

            index = index + 1;
        }

        element.Curves = curves;
        return string.Empty;
    }

    internal static MultiProcessCurveDecodeResult DecodeIcc2Curve(System.ReadOnlyMemory<byte> encoded, long absoluteOffset)
    {
        if (encoded.Length >= 4 && MultiProcessElementTagCodec.ReadUInt32At(encoded, 0) == SingleSampledCurveSignature)
        {
            return DecodeSingleSampledCurve(encoded, absoluteOffset);
        }

        return DecodeIcc2SegmentedCurve(encoded, absoluteOffset);
    }

    internal static MultiProcessCurveDecodeResult DecodeSingleSampledCurve(System.ReadOnlyMemory<byte> encoded, long absoluteOffset)
    {
        MultiProcessCurveDecodeResult result = new MultiProcessCurveDecodeResult();
        if (encoded.Length < 26 || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 4))
        {
            result.Error = "a single sampled curve requires a valid 24-byte header and at least two values.";
            return result;
        }

        uint countValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, 8);
        int extension = MultiProcessElementTagCodec.ReadUInt16At(encoded, 20);
        int encodingValue = MultiProcessElementTagCodec.ReadUInt16At(encoded, 22);
        if (countValue < 2 || countValue > int.MaxValue)
        {
            result.Error = "a single sampled curve shall contain at least two values in the supported managed range.";
            return result;
        }

        if (extension < 0 || extension > 1 || encodingValue < 0 || encodingValue > 3)
        {
            result.Error = "the single sampled curve extension or value-encoding selector is not defined by ICC.2.";
            return result;
        }

        if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, 12) || !MultiProcessElementTagCodec.IsValidFloat32At(encoded, 16))
        {
            result.Error = "the single sampled curve input domain shall contain finite normal float32Number values.";
            return result;
        }

        double minimum = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 12);
        double maximum = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 16);
        if (minimum >= maximum)
        {
            result.Error = "the single sampled curve first input value shall be less than its last input value.";
            return result;
        }

        IccValueEncoding encoding = (IccValueEncoding)encodingValue;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long expectedSize = 24L + ((long)countValue * valueSize);
        if (expectedSize != encoded.Length)
        {
            result.Error = $"the single sampled curve requires exactly {expectedSize} bytes.";
            return result;
        }

        int count = (int)countValue;
        if (!EncodedValuesAreValid(encoded, 24, count, encoding))
        {
            result.Error = "the single sampled curve contains a non-finite or subnormal floating-point value.";
            return result;
        }

        BigEndianReader reader = new BigEndianReader(encoded.Slice(24), absoluteOffset + 24);
        double[] samples = new double[count];
        int index = 0;
        while (index < count)
        {
            samples[index] = IccNumberCodec.ReadEncodedValue(reader, encoding);
            index = index + 1;
        }

        result.Curve.Kind = IccCurveKind.SingleSampled;
        result.Curve.Samples = samples;
        result.Curve.InputMinimum = minimum;
        result.Curve.InputMaximum = maximum;
        result.Curve.Extension = extension;
        result.Curve.Encoding = encodingValue;
        return result;
    }

    internal static MultiProcessCurveDecodeResult DecodeIcc2SegmentedCurve(System.ReadOnlyMemory<byte> encoded, long absoluteOffset)
    {
        MultiProcessCurveDecodeResult result = new MultiProcessCurveDecodeResult();
        if (encoded.Length < 40 || MultiProcessElementTagCodec.ReadUInt32At(encoded, 0) != MultiProcessElementTagCodec.SegmentedCurveSignature || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 4) || encoded.Span[10] != 0 || encoded.Span[11] != 0)
        {
            result.Error = "the ICC.2 segmented-curve header is invalid.";
            return result;
        }

        int segmentCount = MultiProcessElementTagCodec.ReadUInt16At(encoded, 8);
        if (segmentCount < 1)
        {
            result.Error = "an ICC.2 segmented curve shall contain at least one segment.";
            return result;
        }

        long segmentStartValue = 12L + ((long)(segmentCount - 1) * 4L);
        if (segmentStartValue > encoded.Length || segmentStartValue > int.MaxValue)
        {
            result.Error = "the segmented-curve breakpoint array extends beyond the curve.";
            return result;
        }

        double[] breakPoints = new double[segmentCount - 1];
        int index = 0;
        while (index < breakPoints.Length)
        {
            int offset = 12 + (index * 4);
            if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, offset))
            {
                result.Error = $"breakpoint {index} is not a finite normal float32Number.";
                return result;
            }

            breakPoints[index] = MultiProcessElementTagCodec.DecodeFloat32At(encoded, offset);
            if (index > 0 && breakPoints[index] < breakPoints[index - 1])
            {
                result.Error = "successive segmented-curve breakpoints shall not decrease.";
                return result;
            }

            index = index + 1;
        }

        IccCurveSegment[] segments = new IccCurveSegment[segmentCount];
        int current = (int)segmentStartValue;
        index = 0;
        while (index < segmentCount)
        {
            MultiProcessSegmentDecodeResult segmentResult = DecodeIcc2CurveSegment(encoded, current);
            if (segmentResult.Error.Length > 0)
            {
                result.Error = $"segment {index}: {segmentResult.Error}";
                return result;
            }

            if ((index == 0 || index == segmentCount - 1) && segmentResult.Segment.Kind != IccCurveSegmentKind.Formula)
            {
                result.Error = "the first and last segmented-curve segments shall be formula segments.";
                return result;
            }

            segmentResult.Segment.BreakPoint = index == 0 ? double.NegativeInfinity : breakPoints[index - 1];
            segments[index] = segmentResult.Segment;
            current = current + segmentResult.Size;
            index = index + 1;
        }

        if (current != encoded.Length)
        {
            result.Error = $"the segmented curve contains {encoded.Length - current} trailing bytes.";
            return result;
        }

        result.Curve.Kind = IccCurveKind.Segmented;
        result.Curve.Segments = segments;
        return result;
    }

    internal static MultiProcessSegmentDecodeResult DecodeIcc2CurveSegment(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        MultiProcessSegmentDecodeResult result = new MultiProcessSegmentDecodeResult();
        if (offset > encoded.Length - 12)
        {
            result.Error = "the segment header extends beyond the curve.";
            return result;
        }

        uint signature = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset);
        if (!MultiProcessElementsTagCodec.HasZeroBytes(encoded, offset + 4, 4))
        {
            result.Error = "reserved bytes shall be zero.";
            return result;
        }

        if (signature == MultiProcessElementTagCodec.FormulaSegmentSignature)
        {
            if (encoded.Span[offset + 10] != 0 || encoded.Span[offset + 11] != 0)
            {
                result.Error = "formula-segment reserved bytes shall be zero.";
                return result;
            }

            int formulaType = MultiProcessElementTagCodec.ReadUInt16At(encoded, offset + 8);
            int parameterCount = GetIcc2FormulaParameterCount(formulaType);
            if (parameterCount < 0)
            {
                result.Error = $"formula selector {formulaType} is not an ICC.2 selector from zero through seven.";
                return result;
            }

            int size = 12 + (parameterCount * 4);
            if (size > encoded.Length - offset)
            {
                result.Error = "formula parameters extend beyond the curve.";
                return result;
            }

            result.Segment.Kind = IccCurveSegmentKind.Formula;
            result.Segment.FormulaType = formulaType;
            result.Segment.Parameters = new double[parameterCount];
            int index = 0;
            while (index < parameterCount)
            {
                int parameterOffset = offset + 12 + (index * 4);
                if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, parameterOffset))
                {
                    result.Error = $"formula parameter {index} is not a finite normal float32Number.";
                    return result;
                }

                result.Segment.Parameters[index] = MultiProcessElementTagCodec.DecodeFloat32At(encoded, parameterOffset);
                index = index + 1;
            }

            result.Size = size;
            return result;
        }

        if (signature == MultiProcessElementTagCodec.SampledSegmentSignature)
        {
            uint countValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, offset + 8);
            long sizeValue = 12L + ((long)countValue * 4L);
            if (countValue < 1 || countValue > int.MaxValue || sizeValue > encoded.Length - offset || sizeValue > int.MaxValue)
            {
                result.Error = "sampled-segment values extend beyond the curve or have an invalid count.";
                return result;
            }

            int count = (int)countValue;
            result.Segment.Kind = IccCurveSegmentKind.Sampled;
            result.Segment.Samples = new double[count];
            int index = 0;
            while (index < count)
            {
                int sampleOffset = offset + 12 + (index * 4);
                if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, sampleOffset))
                {
                    result.Error = $"sample {index} is not a finite normal float32Number.";
                    return result;
                }

                result.Segment.Samples[index] = MultiProcessElementTagCodec.DecodeFloat32At(encoded, sampleOffset);
                index = index + 1;
            }

            result.Size = (int)sizeValue;
            return result;
        }

        result.Error = $"segment signature 0x{signature:X8} is not a formula or sampled segment.";
        return result;
    }

    internal static string DecodeExtendedClut(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels > 16 || encoded.Length < 32 || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 3))
        {
            return "an extended CLUT supports one to sixteen inputs and requires a valid 32-byte header.";
        }

        int hint = encoded.Span[7];
        uint encodingValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, 12);
        if (hint > 2 || encodingValue > 3)
        {
            return "the extended CLUT interpolation hint or value encoding is not defined by ICC.2.";
        }

        return DecodeEncodedClut(encoded, element, absoluteOffset, 16, 32, (int)encodingValue, hint);
    }

    internal static string DecodeEncodedClut(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset, int gridOffset, int valueOffset, int encodingValue, int hint)
    {
        int valueCount = element.OutputChannels;
        int index = 0;
        while (index < element.InputChannels)
        {
            int grid = encoded.Span[gridOffset + index];
            if (grid < 2)
            {
                return $"CLUT grid dimension {index} shall contain between 2 and 255 points.";
            }

            if (valueCount > int.MaxValue / grid)
            {
                return "the CLUT dimensions exceed the supported managed size.";
            }

            valueCount = valueCount * grid;
            index = index + 1;
        }

        while (index < 16)
        {
            if (encoded.Span[gridOffset + index] != 0)
            {
                return "unused CLUT grid-point bytes shall be zero.";
            }

            index = index + 1;
        }

        IccValueEncoding encoding = (IccValueEncoding)encodingValue;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long expectedSize = (long)valueOffset + ((long)valueCount * valueSize);
        if (expectedSize != encoded.Length)
        {
            return $"the encoded CLUT dimensions require exactly {expectedSize} bytes.";
        }

        if (!EncodedValuesAreValid(encoded, valueOffset, valueCount, encoding))
        {
            return "the encoded CLUT contains a non-finite or subnormal floating-point value.";
        }

        IccClut clut = new IccClut();
        clut.InputChannels = element.InputChannels;
        clut.OutputChannels = element.OutputChannels;
        clut.GridPoints = new int[element.InputChannels];
        index = 0;
        while (index < element.InputChannels)
        {
            clut.GridPoints[index] = encoded.Span[gridOffset + index];
            index = index + 1;
        }

        clut.Values = new double[valueCount];
        BigEndianReader reader = new BigEndianReader(encoded.Slice(valueOffset), absoluteOffset + valueOffset);
        index = 0;
        while (index < valueCount)
        {
            clut.Values[index] = IccNumberCodec.ReadEncodedValue(reader, encoding);
            index = index + 1;
        }

        clut.Encoding = encodingValue;
        clut.InterpolationHint = hint;
        clut.Interpolation = hint == 2 ? IccInterpolationKind.Tetrahedral : IccInterpolationKind.Multilinear;
        element.Clut = clut;
        return string.Empty;
    }

    internal static string DecodeTintArray(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels != 1 || encoded.Length < 20 || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 12 + 4, 4))
        {
            return "a tint-array element requires one input channel and a valid 20-byte header.";
        }

        uint encodingValue = MultiProcessElementTagCodec.ReadUInt32At(encoded, 12);
        if (encodingValue > 3)
        {
            return "the tint-array value encoding is not defined by ICC.2.";
        }

        IccValueEncoding encoding = (IccValueEncoding)encodingValue;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        int byteCount = encoded.Length - 20;
        if (byteCount % valueSize != 0)
        {
            return "the tint-array byte count is not divisible by its value-encoding size.";
        }

        int valueCount = byteCount / valueSize;
        if (valueCount < element.OutputChannels * 2 || valueCount % element.OutputChannels != 0)
        {
            return "a tint array shall contain a whole number of samples and at least two samples per output channel.";
        }

        if (!EncodedValuesAreValid(encoded, 20, valueCount, encoding))
        {
            return "the tint array contains a non-finite or subnormal floating-point value.";
        }

        IccClut clut = new IccClut();
        clut.InputChannels = 1;
        clut.OutputChannels = element.OutputChannels;
        clut.GridPoints = new int[] { valueCount / element.OutputChannels };
        clut.Values = new double[valueCount];
        clut.Interpolation = IccInterpolationKind.Linear;
        clut.Encoding = (int)encodingValue;
        BigEndianReader reader = new BigEndianReader(encoded.Slice(20), absoluteOffset + 20);
        int index = 0;
        while (index < valueCount)
        {
            clut.Values[index] = IccNumberCodec.ReadEncodedValue(reader, encoding);
            index = index + 1;
        }

        element.Clut = clut;
        return string.Empty;
    }

    internal static string DecodeSparseMatrix(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels > 16 || encoded.Length < 32 || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 12 + 2, 2))
        {
            return "a sparse-matrix element supports one to sixteen inputs and requires a valid 32-byte header.";
        }

        int encodingValue = MultiProcessElementTagCodec.ReadUInt16At(encoded, 12);
        if (encodingValue < 1 || encodingValue > 4)
        {
            return "the sparse-matrix encoding selector is not defined by ICC.2.";
        }

        int matrixCount = 1;
        int[] grids = new int[element.InputChannels];
        int index = 0;
        while (index < element.InputChannels)
        {
            int grid = encoded.Span[16 + index];
            if (grid < 2 || matrixCount > int.MaxValue / grid)
            {
                return $"sparse-matrix grid dimension {index} is invalid or exceeds the supported managed size.";
            }

            grids[index] = grid;
            matrixCount = matrixCount * grid;
            index = index + 1;
        }

        while (index < 16)
        {
            if (encoded.Span[16 + index] != 0)
            {
                return "unused sparse-matrix grid-point bytes shall be zero.";
            }

            index = index + 1;
        }

        IccSparseMatrix[] matrices = new IccSparseMatrix[matrixCount];
        int offset = 32;
        int rows = 0;
        int columns = 0;
        index = 0;
        while (index < matrixCount)
        {
            int matrixSize = Icc2ArrayTagCodec.ValidateCompactSparseMatrix(encoded, offset, encodingValue);
            if (matrixSize < 0)
            {
                return $"sparse matrix {index} is malformed.";
            }

            BigEndianReader reader = new BigEndianReader(encoded.Slice(offset, matrixSize), absoluteOffset + offset);
            IccSparseMatrixNumber number = IccNumberCodec.ReadSparseMatrix(reader, (IccSparseMatrixEncoding)encodingValue, matrixSize);
            if (index == 0)
            {
                rows = number.Rows;
                columns = number.Columns;
                if ((long)rows * columns != element.OutputChannels)
                {
                    return "the sparse-matrix element equivalent output-channel count shall equal matrix rows multiplied by columns.";
                }
            }
            else if (number.Rows != rows || number.Columns != columns)
            {
                return "all matrices in a sparse-matrix element shall have identical dimensions.";
            }

            matrices[index] = Icc2ArrayTagCodec.ConvertSparseMatrix(number);
            offset = offset + matrixSize;
            index = index + 1;
        }

        if (offset != encoded.Length)
        {
            return $"the sparse-matrix element contains {encoded.Length - offset} trailing bytes.";
        }

        IccSparseMatrixArray array = new IccSparseMatrixArray();
        array.EquivalentOutputChannels = element.OutputChannels;
        array.Encoding = encodingValue;
        array.GridPoints = grids;
        array.Matrices = matrices;
        element.SparseMatrixLut = array;
        return string.Empty;
    }

    internal static string DecodeToneMap(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels != element.OutputChannels + 1)
        {
            return "a tone-map element shall have one more input channel than output channels and no more than sixteen outputs.";
        }

        int dataStart = 20 + (element.OutputChannels * 8);
        if (dataStart > encoded.Length)
        {
            return "the tone-map position table extends beyond the element.";
        }

        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.OutputChannels + 1];
        BigEndianReader reader = new BigEndianReader(encoded.Slice(12, 8 + (element.OutputChannels * 8)), absoluteOffset + 12);
        int index = 0;
        while (index < positions.Length)
        {
            uint offsetValue = reader.ReadUInt32();
            uint sizeValue = reader.ReadUInt32();
            if (offsetValue > int.MaxValue || sizeValue > int.MaxValue)
            {
                return $"tone-map data position {index} exceeds the supported managed range.";
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)offsetValue;
            position.Size = (int)sizeValue;
            int minimumSize = index == 0 ? 12 : 24;
            if (position.Offset < dataStart || position.Offset % 4 != 0 || position.Size < minimumSize || position.Size > encoded.Length - position.Offset)
            {
                return $"tone-map data position {index} is not a complete aligned object.";
            }

            int paddedSize = MultiProcessElementsTagCodec.AlignFour(position.Size);
            if (paddedSize > encoded.Length - position.Offset || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, position.Offset + position.Size, paddedSize - position.Size))
            {
                return $"tone-map data position {index} has invalid alignment padding.";
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = MultiProcessElementsTagCodec.ValidatePositionTopology(positions, dataStart, encoded.Length);
        if (topologyError.Length > 0)
        {
            return $"the tone-map storage is invalid: {topologyError}";
        }

        MultiProcessCurveDecodeResult curveResult = DecodeIcc2Curve(encoded.Slice(positions[0].Offset, positions[0].Size), absoluteOffset + positions[0].Offset);
        if (curveResult.Error.Length > 0)
        {
            return $"luminance curve: {curveResult.Error}";
        }

        IccToneMapFunction[] functions = new IccToneMapFunction[element.OutputChannels];
        System.Collections.Generic.List<MultiProcessElementPosition> decodedPositions = new System.Collections.Generic.List<MultiProcessElementPosition>();
        System.Collections.Generic.List<IccToneMapFunction> decodedFunctions = new System.Collections.Generic.List<IccToneMapFunction>();
        index = 0;
        while (index < functions.Length)
        {
            MultiProcessElementPosition position = positions[index + 1];
            IccToneMapFunction? shared = FindToneMapFunction(decodedPositions, decodedFunctions, position);
            if (shared != null)
            {
                functions[index] = shared;
            }
            else
            {
                IccToneMapFunction function = new IccToneMapFunction();
                string error = DecodeToneMapFunction(encoded.Slice(position.Offset, position.Size), function);
                if (error.Length > 0)
                {
                    return $"tone-map function {index}: {error}";
                }

                functions[index] = function;
                decodedPositions.Add(position);
                decodedFunctions.Add(function);
            }

            index = index + 1;
        }

        IccToneMap toneMap = new IccToneMap();
        toneMap.LuminanceCurve = curveResult.Curve;
        toneMap.Functions = functions;
        element.ToneMap = toneMap;
        return string.Empty;
    }

    internal static string DecodeCamElement(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element)
    {
        if (element.InputChannels != 3 || element.OutputChannels != 3)
        {
            return "an XYZ-to-Jab or Jab-to-XYZ element shall have exactly three input and three output channels.";
        }

        if (encoded.Length != 44)
        {
            return $"an XYZ-to-Jab or Jab-to-XYZ element requires exactly 44 bytes, but this element contains {encoded.Length}.";
        }

        int index = 0;
        while (index < 8)
        {
            if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, 12 + (index * 4)))
            {
                return $"CIECAM02 viewing-condition value {index} is not a finite normal float32Number.";
            }

            index = index + 1;
        }

        double impactOfSurround = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 32);
        if (impactOfSurround < 0.0 || impactOfSurround > 1.0)
        {
            return "the CIECAM02 impact-of-surround value shall range from zero to one.";
        }

        IccCamViewingConditions conditions = new IccCamViewingConditions();
        conditions.WhitePoint.X = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 12);
        conditions.WhitePoint.Y = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 16);
        conditions.WhitePoint.Z = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 20);
        conditions.Luminance = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 24);
        conditions.BackgroundLuminance = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 28);
        conditions.ImpactOfSurround = impactOfSurround;
        conditions.ChromaticInductionFactor = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 36);
        conditions.AdaptationFactor = MultiProcessElementTagCodec.DecodeFloat32At(encoded, 40);
        element.CamViewingConditions = conditions;
        return string.Empty;
    }

    internal static string DecodeToneMapFunction(System.ReadOnlyMemory<byte> encoded, IccToneMapFunction function)
    {
        if (encoded.Length != 24 || MultiProcessElementTagCodec.ReadUInt32At(encoded, 0) != ToneMapFunctionSignature || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 4) || encoded.Span[10] != 0 || encoded.Span[11] != 0)
        {
            return "the standard tone-map function requires exactly 24 bytes with zero reserved fields.";
        }

        int functionType = MultiProcessElementTagCodec.ReadUInt16At(encoded, 8);
        if (functionType != 0)
        {
            return $"mapping-function selector {functionType} is not defined by ICC.2.";
        }

        function.FunctionType = functionType;
        function.Parameters = new double[3];
        int index = 0;
        while (index < 3)
        {
            int offset = 12 + (index * 4);
            if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, offset))
            {
                return $"parameter {index} is not a finite normal float32Number.";
            }

            function.Parameters[index] = MultiProcessElementTagCodec.DecodeFloat32At(encoded, offset);
            index = index + 1;
        }

        return string.Empty;
    }

    internal static IccToneMapFunction? FindToneMapFunction(System.Collections.Generic.List<MultiProcessElementPosition> positions, System.Collections.Generic.List<IccToneMapFunction> functions, MultiProcessElementPosition requested)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index].Offset == requested.Offset && positions[index].Size == requested.Size)
            {
                return functions[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static byte[] SerializeCurveSet(IccProcessingElement element, int elementIndex)
    {
        if (element.Signature != IccProcessingElementSignatures.CurveSet || element.InputChannels < 1 || element.InputChannels > ushort.MaxValue || element.OutputChannels != element.InputChannels || element.Curves == null || element.Curves.Length != element.InputChannels)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} ICC.2 curve-set channels and curve count are invalid.");
        }

        int dataStart = 12 + (element.InputChannels * 8);
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.Curves.Length];
        System.Collections.Generic.List<MultiProcessCurveBlock> blocks = new System.Collections.Generic.List<MultiProcessCurveBlock>();
        int nextOffset = dataStart;
        int index = 0;
        while (index < element.Curves.Length)
        {
            byte[] bytes = SerializeIcc2Curve(element.Curves[index], index);
            int offset = MultiProcessElementTagCodec.FindCurveBlockOffset(blocks, bytes);
            if (offset < 0)
            {
                int paddedSize = MultiProcessElementsTagCodec.AlignFour(bytes.Length);
                if (paddedSize > int.MaxValue - nextOffset)
                {
                    throw new IccProfileWriteException($"Processing element {elementIndex} curve data exceeds the supported managed size.");
                }

                MultiProcessCurveBlock block = new MultiProcessCurveBlock();
                block.Offset = nextOffset;
                block.Bytes = bytes;
                blocks.Add(block);
                offset = nextOffset;
                nextOffset = nextOffset + paddedSize;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = offset;
            position.Size = bytes.Length;
            positions[index] = position;
            index = index + 1;
        }

        byte[] encoded = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        index = 0;
        while (index < positions.Length)
        {
            writer.WriteUInt32((uint)positions[index].Offset);
            writer.WriteUInt32((uint)positions[index].Size);
            index = index + 1;
        }

        index = 0;
        while (index < blocks.Count)
        {
            MultiProcessCurveBlock block = blocks[index];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(encoded, block.Offset, block.Bytes.Length));
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeIcc2Curve(IccCurve curve, int curveIndex)
    {
        if (curve == null)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} cannot be null.");
        }

        if (curve.Kind == IccCurveKind.SingleSampled)
        {
            return SerializeSingleSampledCurve(curve, curveIndex);
        }

        return SerializeIcc2SegmentedCurve(curve, curveIndex);
    }

    internal static byte[] SerializeSingleSampledCurve(IccCurve curve, int curveIndex)
    {
        if (curve == null || curve.Samples == null || curve.Samples.Length < 2 || curve.Extension < 0 || curve.Extension > 1 || curve.Encoding < 0 || curve.Encoding > 3)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} single sampled data or selectors are invalid.");
        }

        MultiProcessElementTagCodec.ValidateFloatForWrite(curve.InputMinimum, $"ICC.2 curve-set curve {curveIndex} input minimum");
        MultiProcessElementTagCodec.ValidateFloatForWrite(curve.InputMaximum, $"ICC.2 curve-set curve {curveIndex} input maximum");
        if (curve.InputMinimum >= curve.InputMaximum)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} input minimum shall be less than its input maximum.");
        }

        IccValueEncoding encoding = (IccValueEncoding)curve.Encoding;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long size = 24L + ((long)curve.Samples.Length * valueSize);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(SingleSampledCurveSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)curve.Samples.Length);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, curve.InputMinimum);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, curve.InputMaximum);
        writer.WriteUInt16((ushort)curve.Extension);
        writer.WriteUInt16((ushort)curve.Encoding);
        int index = 0;
        while (index < curve.Samples.Length)
        {
            IccNumberCodec.ValidateEncodedValueForWrite(encoding, curve.Samples[index]);
            IccNumberCodec.WriteEncodedValue(writer, encoding, curve.Samples[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeIcc2SegmentedCurve(IccCurve curve, int curveIndex)
    {
        if (curve == null || curve.Kind != IccCurveKind.Segmented || curve.Segments == null || curve.Segments.Length < 1 || curve.Segments.Length > ushort.MaxValue)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} shall be single sampled or contain segmented-curve data.");
        }

        if (curve.Segments[0] == null || !double.IsNegativeInfinity(curve.Segments[0].BreakPoint))
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} first segment shall start at negative infinity.");
        }

        byte[][] segments = new byte[curve.Segments.Length][];
        long size = 12L + ((long)(curve.Segments.Length - 1) * 4L);
        double previous = double.NegativeInfinity;
        int index = 0;
        while (index < curve.Segments.Length)
        {
            IccCurveSegment segment = curve.Segments[index];
            if (segment == null || ((index == 0 || index == curve.Segments.Length - 1) && segment.Kind != IccCurveSegmentKind.Formula))
            {
                throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} has an invalid first, last, or null segment.");
            }

            if (index > 0)
            {
                MultiProcessElementTagCodec.ValidateFloatForWrite(segment.BreakPoint, $"ICC.2 curve-set curve {curveIndex} breakpoint");
                if (segment.BreakPoint < previous)
                {
                    throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} breakpoints shall not decrease.");
                }

                previous = segment.BreakPoint;
            }

            segments[index] = SerializeIcc2CurveSegment(segment, curveIndex, index);
            size = size + segments[index].Length;
            if (size > int.MaxValue)
            {
                throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} exceeds the supported managed size.");
            }

            index = index + 1;
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(MultiProcessElementTagCodec.SegmentedCurveSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)curve.Segments.Length);
        writer.WriteZeroBytes(2);
        index = 1;
        while (index < curve.Segments.Length)
        {
            MultiProcessElementTagCodec.WriteFloat32Value(writer, curve.Segments[index].BreakPoint);
            index = index + 1;
        }

        index = 0;
        while (index < segments.Length)
        {
            writer.WriteExactBytes(segments[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeIcc2CurveSegment(IccCurveSegment segment, int curveIndex, int segmentIndex)
    {
        if (segment.Kind == IccCurveSegmentKind.Sampled)
        {
            return MultiProcessElementTagCodec.SerializeCurveSegment(segment, curveIndex, segmentIndex);
        }

        if (segment.Kind != IccCurveSegmentKind.Formula || segment.Parameters == null)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} segment {segmentIndex} has an unsupported kind.");
        }

        int parameterCount = GetIcc2FormulaParameterCount(segment.FormulaType);
        if (parameterCount < 0 || segment.Parameters.Length != parameterCount)
        {
            throw new IccProfileWriteException($"ICC.2 curve-set curve {curveIndex} formula segment {segmentIndex} has an invalid selector or parameter count.");
        }

        byte[] encoded = new byte[12 + (parameterCount * 4)];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(MultiProcessElementTagCodec.FormulaSegmentSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)segment.FormulaType);
        writer.WriteZeroBytes(2);
        int index = 0;
        while (index < parameterCount)
        {
            MultiProcessElementTagCodec.ValidateFloatForWrite(segment.Parameters[index], $"ICC.2 curve-set curve {curveIndex} formula parameter {index}");
            MultiProcessElementTagCodec.WriteFloat32Value(writer, segment.Parameters[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static int GetIcc2FormulaParameterCount(int formulaType)
    {
        switch (formulaType)
        {
            case 0:
                return 4;
            case 1:
            case 2:
            case 3:
            case 4:
                return 5;
            case 5:
            case 7:
                return 6;
            case 6:
                return 7;
            default:
                return -1;
        }
    }

    internal static byte[] SerializeExtendedClut(IccProcessingElement element, int elementIndex)
    {
        ValidateEncodedClutForWrite(element, elementIndex, IccProcessingElementSignatures.ExtendedClut);
        IccClut? clut = element.Clut;
        if (clut == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} extended CLUT data cannot be null.");
        }

        if (clut.InterpolationHint < 0 || clut.InterpolationHint > 2)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} extended CLUT interpolation hint is invalid.");
        }

        return SerializeEncodedClut(element, 32, clut.InterpolationHint);
    }

    internal static byte[] SerializeEncodedClut(IccProcessingElement element, int headerSize, int hint)
    {
        IccClut? optionalClut = element.Clut;
        if (optionalClut == null)
        {
            throw new IccProfileWriteException("Encoded CLUT data cannot be null.");
        }

        IccClut clut = optionalClut;
        IccValueEncoding encoding = (IccValueEncoding)clut.Encoding;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long size = (long)headerSize + ((long)clut.Values.Length * valueSize);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("Encoded CLUT data exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(element.Signature);
        writer.WriteZeroBytes(3);
        writer.WriteUInt8((byte)hint);
        writer.WriteUInt16((ushort)element.InputChannels);
        writer.WriteUInt16((ushort)element.OutputChannels);
        writer.WriteUInt32((uint)clut.Encoding);
        int index = 0;
        while (index < element.InputChannels)
        {
            writer.WriteUInt8((byte)clut.GridPoints[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(16 - element.InputChannels);
        index = 0;
        while (index < clut.Values.Length)
        {
            IccNumberCodec.ValidateEncodedValueForWrite(encoding, clut.Values[index]);
            IccNumberCodec.WriteEncodedValue(writer, encoding, clut.Values[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static void ValidateEncodedClutForWrite(IccProcessingElement element, int elementIndex, uint signature)
    {
        if (element.Signature != signature || element.InputChannels < 1 || element.InputChannels > 16 || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || element.Clut == null || element.Clut.GridPoints == null || element.Clut.Values == null || element.Clut.Encoding < 0 || element.Clut.Encoding > 3)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} encoded CLUT data, selectors, or channels are invalid.");
        }

        if (element.Clut.InputChannels != element.InputChannels || element.Clut.OutputChannels != element.OutputChannels || element.Clut.GridPoints.Length != element.InputChannels)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} encoded CLUT dimensions shall match its channels.");
        }

        int valueCount = element.OutputChannels;
        int index = 0;
        while (index < element.InputChannels)
        {
            int grid = element.Clut.GridPoints[index];
            if (grid < 2 || grid > 255 || valueCount > int.MaxValue / grid)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} encoded CLUT grid dimension {index} is invalid.");
            }

            valueCount = valueCount * grid;
            index = index + 1;
        }

        if (element.Clut.Values.Length != valueCount)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} encoded CLUT requires exactly {valueCount} values.");
        }
    }

    internal static byte[] SerializeTintArray(IccProcessingElement element, int elementIndex)
    {
        if (element.Signature != IccProcessingElementSignatures.TintArray || element.InputChannels != 1 || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || element.Clut == null || element.Clut.GridPoints == null || element.Clut.GridPoints.Length != 1 || element.Clut.GridPoints[0] < 2 || element.Clut.Values == null || element.Clut.Encoding < 0 || element.Clut.Encoding > 3)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} tint-array data, selectors, or channels are invalid.");
        }

        int expectedCount = element.Clut.GridPoints[0] * element.OutputChannels;
        if (element.Clut.InputChannels != 1 || element.Clut.OutputChannels != element.OutputChannels || element.Clut.Values.Length != expectedCount)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} tint-array dimensions do not match its values.");
        }

        IccValueEncoding encoding = (IccValueEncoding)element.Clut.Encoding;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long size = 20L + ((long)expectedCount * valueSize);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} tint array exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        writer.WriteUInt32((uint)element.Clut.Encoding);
        writer.WriteZeroBytes(4);
        int index = 0;
        while (index < expectedCount)
        {
            IccNumberCodec.ValidateEncodedValueForWrite(encoding, element.Clut.Values[index]);
            IccNumberCodec.WriteEncodedValue(writer, encoding, element.Clut.Values[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeSparseMatrix(IccProcessingElement element, int elementIndex)
    {
        IccSparseMatrixArray? array = element.SparseMatrixLut;
        if (element.Signature != IccProcessingElementSignatures.SparseMatrix || element.InputChannels < 1 || element.InputChannels > 16 || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || array == null || array.GridPoints == null || array.GridPoints.Length != element.InputChannels || array.Matrices == null || array.Encoding < 1 || array.Encoding > 4 || array.EquivalentOutputChannels != element.OutputChannels)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} sparse-matrix data, selectors, or channels are invalid.");
        }

        int matrixCount = 1;
        int index = 0;
        while (index < array.GridPoints.Length)
        {
            int grid = array.GridPoints[index];
            if (grid < 2 || grid > 255 || matrixCount > int.MaxValue / grid)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} sparse-matrix grid dimension {index} is invalid.");
            }

            matrixCount = matrixCount * grid;
            index = index + 1;
        }

        if (array.Matrices.Length != matrixCount)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} sparse-matrix LUT requires exactly {matrixCount} matrices.");
        }

        IccSparseMatrixEncoding encoding = (IccSparseMatrixEncoding)array.Encoding;
        IccSparseMatrixNumber[] numbers = new IccSparseMatrixNumber[matrixCount];
        int[] sizes = new int[matrixCount];
        long totalSize = 32;
        int rows = 0;
        int columns = 0;
        index = 0;
        while (index < matrixCount)
        {
            IccSparseMatrix matrix = array.Matrices[index];
            if (matrix == null || matrix.Encoding != array.Encoding)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} sparse matrix {index} is null or uses a different encoding.");
            }

            if (index == 0)
            {
                rows = matrix.Rows;
                columns = matrix.Columns;
                if ((long)rows * columns != element.OutputChannels)
                {
                    throw new IccProfileWriteException($"Processing element {elementIndex} equivalent output channels shall equal sparse matrix rows multiplied by columns.");
                }
            }
            else if (matrix.Rows != rows || matrix.Columns != columns)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} sparse matrices shall have identical dimensions.");
            }

            numbers[index] = Icc2ArrayTagCodec.ConvertSparseMatrixForWrite(matrix, encoding, index);
            IccNumberCodec.ValidateSparseMatrixForWrite(numbers[index]);
            sizes[index] = IccNumberCodec.GetSparseMatrixEncodedSize(matrix.Rows, matrix.Values.Length, IccNumberCodec.GetSparseMatrixElementSize(encoding), System.Array.MaxLength);
            totalSize = totalSize + sizes[index];
            if (totalSize > int.MaxValue)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} sparse-matrix LUT exceeds the supported managed size.");
            }

            index = index + 1;
        }

        byte[] encoded = new byte[(int)totalSize];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        writer.WriteUInt16((ushort)array.Encoding);
        writer.WriteZeroBytes(2);
        index = 0;
        while (index < array.GridPoints.Length)
        {
            writer.WriteUInt8((byte)array.GridPoints[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(16 - array.GridPoints.Length);
        index = 0;
        while (index < numbers.Length)
        {
            IccNumberCodec.WriteSparseMatrix(writer, numbers[index], sizes[index]);
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeToneMap(IccProcessingElement element, int elementIndex)
    {
        IccToneMap? toneMap = element.ToneMap;
        if (element.Signature != IccProcessingElementSignatures.ToneMap || element.OutputChannels < 1 || element.OutputChannels >= ushort.MaxValue || element.InputChannels != element.OutputChannels + 1 || toneMap == null || toneMap.LuminanceCurve == null || toneMap.Functions == null || toneMap.Functions.Length != element.OutputChannels)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} tone-map data or channels are invalid.");
        }

        int dataStart = 20 + (element.OutputChannels * 8);
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.OutputChannels + 1];
        System.Collections.Generic.List<MultiProcessCurveBlock> blocks = new System.Collections.Generic.List<MultiProcessCurveBlock>();
        byte[] curve = SerializeIcc2Curve(toneMap.LuminanceCurve, 0);
        MultiProcessCurveBlock curveBlock = new MultiProcessCurveBlock();
        curveBlock.Offset = dataStart;
        curveBlock.Bytes = curve;
        blocks.Add(curveBlock);
        positions[0] = new MultiProcessElementPosition();
        positions[0].Offset = dataStart;
        positions[0].Size = curve.Length;
        if (curve.Length > int.MaxValue - 3)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} luminance curve exceeds the supported aligned size.");
        }

        int paddedCurveSize = MultiProcessElementsTagCodec.AlignFour(curve.Length);
        if (paddedCurveSize > int.MaxValue - dataStart)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} tone-map data exceeds the supported managed size.");
        }

        int nextOffset = dataStart + paddedCurveSize;
        int index = 0;
        while (index < toneMap.Functions.Length)
        {
            byte[] bytes = SerializeToneMapFunction(toneMap.Functions[index], index);
            int offset = MultiProcessElementTagCodec.FindCurveBlockOffset(blocks, bytes);
            if (offset < 0)
            {
                MultiProcessCurveBlock block = new MultiProcessCurveBlock();
                block.Offset = nextOffset;
                block.Bytes = bytes;
                blocks.Add(block);
                offset = nextOffset;
                nextOffset = nextOffset + MultiProcessElementsTagCodec.AlignFour(bytes.Length);
            }

            positions[index + 1] = new MultiProcessElementPosition();
            positions[index + 1].Offset = offset;
            positions[index + 1].Size = bytes.Length;
            index = index + 1;
        }

        byte[] encoded = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        index = 0;
        while (index < positions.Length)
        {
            writer.WriteUInt32((uint)positions[index].Offset);
            writer.WriteUInt32((uint)positions[index].Size);
            index = index + 1;
        }

        index = 0;
        while (index < blocks.Count)
        {
            MultiProcessCurveBlock block = blocks[index];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(encoded, block.Offset, block.Bytes.Length));
            index = index + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeCamElement(IccProcessingElement element, int elementIndex)
    {
        bool isXyzToJab = element.Kind == IccProcessingElementKind.XyzToJab && element.Signature == IccProcessingElementSignatures.XyzToJab;
        bool isJabToXyz = element.Kind == IccProcessingElementKind.JabToXyz && element.Signature == IccProcessingElementSignatures.JabToXyz;
        IccCamViewingConditions? conditions = element.CamViewingConditions;
        if ((!isXyzToJab && !isJabToXyz) || element.InputChannels != 3 || element.OutputChannels != 3 || conditions == null || conditions.WhitePoint == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} appearance kind, signature, channels, or CIECAM02 viewing conditions are invalid.");
        }

        ValidateCamValueForWrite(conditions.WhitePoint.X, elementIndex, "white-point X");
        ValidateCamValueForWrite(conditions.WhitePoint.Y, elementIndex, "white-point Y");
        ValidateCamValueForWrite(conditions.WhitePoint.Z, elementIndex, "white-point Z");
        ValidateCamValueForWrite(conditions.Luminance, elementIndex, "luminance");
        ValidateCamValueForWrite(conditions.BackgroundLuminance, elementIndex, "background luminance");
        ValidateCamValueForWrite(conditions.ImpactOfSurround, elementIndex, "impact of surround");
        ValidateCamValueForWrite(conditions.ChromaticInductionFactor, elementIndex, "chromatic-induction factor");
        ValidateCamValueForWrite(conditions.AdaptationFactor, elementIndex, "adaptation factor");
        if (conditions.ImpactOfSurround < 0.0 || conditions.ImpactOfSurround > 1.0)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} CIECAM02 impact-of-surround value shall range from zero to one.");
        }

        byte[] encoded = new byte[44];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.WhitePoint.X);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.WhitePoint.Y);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.WhitePoint.Z);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.Luminance);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.BackgroundLuminance);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.ImpactOfSurround);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.ChromaticInductionFactor);
        MultiProcessElementTagCodec.WriteFloat32Value(writer, conditions.AdaptationFactor);
        return encoded;
    }

    internal static void ValidateCamValueForWrite(double value, int elementIndex, string name)
    {
        MultiProcessElementTagCodec.ValidateFloatForWrite(value, $"Processing element {elementIndex} CIECAM02 {name}");
    }

    internal static string DecodeSpectralClut(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        bool isReflectance = element.Kind == IccProcessingElementKind.ReflectanceClut;
        int rangeOffset = isReflectance ? 16 : 12;
        int encodingOffset = isReflectance ? 22 : 18;
        int gridOffset = isReflectance ? 24 : 20;
        int valueOffset = isReflectance ? 40 : 36;
        if (element.InputChannels > 16 || element.OutputChannels != 3 || encoded.Length < valueOffset)
        {
            return "a spectral CLUT requires one to sixteen inputs, three outputs, and a complete header.";
        }

        int hint = encoded.Span[7];
        int encodingValue = MultiProcessElementTagCodec.ReadUInt16At(encoded, encodingOffset);
        if (hint > 2 || encodingValue > 3)
        {
            return "the spectral CLUT interpolation hint or value encoding is not defined by ICC.2.";
        }

        IccSpectralRange? optionalRange = DecodeRequiredSpectralRange(encoded, rangeOffset, absoluteOffset);
        if (optionalRange == null)
        {
            return "the spectral CLUT wavelength range is invalid or contains fewer than two steps.";
        }

        IccSpectralRange range = optionalRange;
        int gridCount = 1;
        int index = 0;
        while (index < element.InputChannels)
        {
            int grid = encoded.Span[gridOffset + index];
            if (grid < 2 || gridCount > int.MaxValue / grid)
            {
                return $"spectral CLUT grid dimension {index} is invalid or exceeds the supported managed size.";
            }

            gridCount = gridCount * grid;
            index = index + 1;
        }

        while (index < 16)
        {
            if (encoded.Span[gridOffset + index] != 0)
            {
                return "unused spectral CLUT grid-point bytes shall be zero.";
            }

            index = index + 1;
        }

        if (gridCount > int.MaxValue / range.Steps)
        {
            return "the spectral CLUT sample count exceeds the supported managed size.";
        }

        int clutValueCount = gridCount * range.Steps;
        int totalValueCount = clutValueCount + range.Steps;
        IccValueEncoding encoding = (IccValueEncoding)encodingValue;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        long expectedSize = (long)valueOffset + ((long)totalValueCount * valueSize);
        if (expectedSize != encoded.Length)
        {
            return $"the spectral CLUT dimensions require exactly {expectedSize} bytes.";
        }

        if (!EncodedValuesAreValid(encoded, valueOffset, totalValueCount, encoding))
        {
            return "the spectral CLUT contains a non-finite floating-point value.";
        }

        BigEndianReader reader = new BigEndianReader(encoded.Slice(valueOffset), absoluteOffset + valueOffset);
        IccClut clut = new IccClut();
        clut.InputChannels = element.InputChannels;
        clut.OutputChannels = range.Steps;
        clut.GridPoints = new int[element.InputChannels];
        index = 0;
        while (index < element.InputChannels)
        {
            clut.GridPoints[index] = encoded.Span[gridOffset + index];
            index = index + 1;
        }

        clut.Values = ReadEncodedValueArray(reader, clutValueCount, encoding);
        clut.Encoding = encodingValue;
        clut.InterpolationHint = hint;
        clut.Interpolation = hint == 2 ? IccInterpolationKind.Tetrahedral : IccInterpolationKind.Multilinear;
        IccSpectralProcessingData spectral = new IccSpectralProcessingData();
        spectral.Flags = isReflectance ? MultiProcessElementTagCodec.ReadUInt32At(encoded, 12) : 0;
        spectral.White.Range = range;
        spectral.White.Values = ReadEncodedValueArray(reader, range.Steps, encoding);
        spectral.White.SpectrumCount = 1;
        element.Clut = clut;
        element.SpectralProcessingData = spectral;
        return string.Empty;
    }

    internal static string DecodeSpectralMatrix(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        bool isInverse = element.Kind == IccProcessingElementKind.InverseEmissionMatrix;
        if (element.InputChannels != 3 || element.OutputChannels != 3 || encoded.Length < 20)
        {
            return "a spectral matrix requires exactly three input and three output channels.";
        }

        IccSpectralRange? optionalRange = DecodeRequiredSpectralRange(encoded, 12, absoluteOffset);
        int flags = MultiProcessElementTagCodec.ReadUInt16At(encoded, 18);
        if (optionalRange == null || (isInverse && flags != 0) || (!isInverse && flags > 1))
        {
            return "the spectral matrix wavelength range, absolute flag, or reserved field is invalid.";
        }

        IccSpectralRange range = optionalRange;
        int vectorCount = element.OutputChannels + 1;
        if (vectorCount > int.MaxValue / range.Steps)
        {
            return "the spectral matrix dimensions exceed the supported managed size.";
        }

        int matrixValueCount = vectorCount * range.Steps;
        int totalValueCount = range.Steps + matrixValueCount;
        long expectedSize = 20L + ((long)totalValueCount * 4L);
        if (expectedSize != encoded.Length)
        {
            return $"the spectral matrix dimensions require exactly {expectedSize} bytes.";
        }

        if (!EncodedValuesAreValid(encoded, 20, totalValueCount, IccValueEncoding.Float32))
        {
            return "the spectral matrix contains a non-finite or subnormal float32Number value.";
        }

        BigEndianReader reader = new BigEndianReader(encoded.Slice(20), absoluteOffset + 20);
        IccSpectralProcessingData spectral = new IccSpectralProcessingData();
        spectral.Flags = (uint)flags;
        spectral.White.Range = range;
        spectral.White.Values = ReadEncodedValueArray(reader, range.Steps, IccValueEncoding.Float32);
        spectral.White.SpectrumCount = 1;
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = vectorCount;
        matrix.Columns = range.Steps;
        matrix.Values = ReadEncodedValueArray(reader, matrixValueCount, IccValueEncoding.Float32);
        element.Matrix = matrix;
        element.SpectralProcessingData = spectral;
        return string.Empty;
    }

    internal static string DecodeSpectralObserver(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.OutputChannels != 3 || encoded.Length < 20)
        {
            return "a spectral observer requires three output channels and a complete header.";
        }

        IccSpectralRange? optionalRange = DecodeRequiredSpectralRange(encoded, 12, absoluteOffset);
        int flags = MultiProcessElementTagCodec.ReadUInt16At(encoded, 18);
        if (optionalRange == null || flags > 1)
        {
            return "the spectral observer wavelength range or absolute flag is invalid.";
        }

        IccSpectralRange range = optionalRange;
        if (element.InputChannels != range.Steps)
        {
            return "a spectral observer input-channel count shall equal its wavelength sample count.";
        }

        long expectedSize = 20L + ((long)range.Steps * 4L);
        if (expectedSize != encoded.Length)
        {
            return $"the spectral observer dimensions require exactly {expectedSize} bytes.";
        }

        if (!EncodedValuesAreValid(encoded, 20, range.Steps, IccValueEncoding.Float32))
        {
            return "the spectral observer contains a non-finite or subnormal float32Number value.";
        }

        BigEndianReader reader = new BigEndianReader(encoded.Slice(20), absoluteOffset + 20);
        IccSpectralProcessingData spectral = new IccSpectralProcessingData();
        spectral.Flags = (uint)flags;
        spectral.White.Range = range;
        spectral.White.Values = ReadEncodedValueArray(reader, range.Steps, IccValueEncoding.Float32);
        spectral.White.SpectrumCount = 1;
        element.SpectralProcessingData = spectral;
        return string.Empty;
    }

    internal static byte[] SerializeSpectralClut(IccProcessingElement element, int elementIndex)
    {
        bool isReflectance = element.Kind == IccProcessingElementKind.ReflectanceClut;
        uint expectedSignature = isReflectance ? IccProcessingElementSignatures.ReflectanceClut : IccProcessingElementSignatures.EmissionClut;
        IccSpectralProcessingData? optionalSpectral = element.SpectralProcessingData;
        IccClut? optionalClut = element.Clut;
        if (element.Signature != expectedSignature || element.InputChannels < 1 || element.InputChannels > 16 || element.OutputChannels != 3 || optionalSpectral == null || optionalClut == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral CLUT data, signature, or channels are invalid.");
        }

        IccSpectralProcessingData spectral = optionalSpectral;
        IccClut clut = optionalClut;
        int steps = ValidateSpectralWhiteForWrite(spectral, elementIndex);
        if ((!isReflectance && spectral.Flags != 0) || clut.InputChannels != element.InputChannels || clut.OutputChannels != steps || clut.GridPoints == null || clut.GridPoints.Length != element.InputChannels || clut.Values == null || clut.Encoding < 0 || clut.Encoding > 3 || clut.InterpolationHint < 0 || clut.InterpolationHint > 2)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral CLUT dimensions, flags, or selectors are invalid.");
        }

        int gridCount = 1;
        int index = 0;
        while (index < element.InputChannels)
        {
            int grid = clut.GridPoints[index];
            if (grid < 2 || grid > 255 || gridCount > int.MaxValue / grid)
            {
                throw new IccProfileWriteException($"Processing element {elementIndex} spectral CLUT grid dimension {index} is invalid.");
            }

            gridCount = gridCount * grid;
            index = index + 1;
        }

        if (gridCount > int.MaxValue / steps || clut.Values.Length != gridCount * steps)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral CLUT sample count is invalid.");
        }

        IccValueEncoding encoding = (IccValueEncoding)clut.Encoding;
        int valueSize = IccNumberCodec.GetValueEncodingSize(encoding);
        int valueOffset = isReflectance ? 40 : 36;
        long size = (long)valueOffset + ((long)(clut.Values.Length + steps) * valueSize);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral CLUT exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(element.Signature);
        writer.WriteZeroBytes(3);
        writer.WriteUInt8((byte)clut.InterpolationHint);
        writer.WriteUInt16((ushort)element.InputChannels);
        writer.WriteUInt16((ushort)element.OutputChannels);
        if (isReflectance)
        {
            writer.WriteUInt32(spectral.Flags);
        }

        SpectralViewingConditionsTagCodec.WriteSpectralRange(writer, spectral.White);
        writer.WriteUInt16((ushort)clut.Encoding);
        index = 0;
        while (index < clut.GridPoints.Length)
        {
            writer.WriteUInt8((byte)clut.GridPoints[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(16 - clut.GridPoints.Length);
        WriteSpectralEncodedValues(writer, clut.Values, encoding, elementIndex);
        WriteSpectralEncodedValues(writer, spectral.White.Values, encoding, elementIndex);
        return encoded;
    }

    internal static byte[] SerializeSpectralMatrix(IccProcessingElement element, int elementIndex)
    {
        bool isInverse = element.Kind == IccProcessingElementKind.InverseEmissionMatrix;
        uint expectedSignature = isInverse ? IccProcessingElementSignatures.InverseEmissionMatrix : IccProcessingElementSignatures.EmissionMatrix;
        IccSpectralProcessingData? optionalSpectral = element.SpectralProcessingData;
        IccMatrix? optionalMatrix = element.Matrix;
        if (element.Signature != expectedSignature || element.InputChannels != 3 || element.OutputChannels != 3 || optionalSpectral == null || optionalMatrix == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral matrix data, signature, or channels are invalid.");
        }

        IccSpectralProcessingData spectral = optionalSpectral;
        IccMatrix matrix = optionalMatrix;
        int steps = ValidateSpectralWhiteForWrite(spectral, elementIndex);
        int vectorCount = element.OutputChannels + 1;
        if ((isInverse && spectral.Flags != 0) || (!isInverse && spectral.Flags > 1) || matrix.Rows != vectorCount || matrix.Columns != steps || matrix.Values == null || matrix.Values.Length != vectorCount * steps)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral matrix dimensions or absolute flag are invalid.");
        }

        long size = 20L + ((long)(steps + matrix.Values.Length) * 4L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral matrix exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        SpectralViewingConditionsTagCodec.WriteSpectralRange(writer, spectral.White);
        writer.WriteUInt16((ushort)spectral.Flags);
        WriteSpectralFloat32Values(writer, spectral.White.Values, elementIndex);
        WriteSpectralFloat32Values(writer, matrix.Values, elementIndex);
        return encoded;
    }

    internal static byte[] SerializeSpectralObserver(IccProcessingElement element, int elementIndex)
    {
        bool isReflectance = element.Kind == IccProcessingElementKind.ReflectanceObserver;
        uint expectedSignature = isReflectance ? IccProcessingElementSignatures.ReflectanceObserver : IccProcessingElementSignatures.EmissionObserver;
        IccSpectralProcessingData? optionalSpectral = element.SpectralProcessingData;
        if (element.Signature != expectedSignature || element.InputChannels < 1 || element.OutputChannels != 3 || optionalSpectral == null)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral observer data, signature, or channels are invalid.");
        }

        IccSpectralProcessingData spectral = optionalSpectral;
        int steps = ValidateSpectralWhiteForWrite(spectral, elementIndex);
        if (spectral.Flags > 1 || element.InputChannels != steps)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral observer channel count or absolute flag is invalid.");
        }

        byte[] encoded = new byte[20 + (steps * 4)];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        MultiProcessElementTagCodec.WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        SpectralViewingConditionsTagCodec.WriteSpectralRange(writer, spectral.White);
        writer.WriteUInt16((ushort)spectral.Flags);
        WriteSpectralFloat32Values(writer, spectral.White.Values, elementIndex);
        return encoded;
    }

    internal static IccSpectralRange? DecodeRequiredSpectralRange(System.ReadOnlyMemory<byte> encoded, int offset, long absoluteOffset)
    {
        if (!EncodedValuesAreValid(encoded, offset, 2, IccValueEncoding.Float16))
        {
            return null;
        }

        BigEndianReader reader = new BigEndianReader(encoded.Slice(offset, 6), absoluteOffset + offset);
        IccSpectralRangeNumber rangeNumber = IccNumberCodec.ReadSpectralRange(reader);
        if (rangeNumber.Steps < 2 || !SpectralViewingConditionsTagCodec.IsValidRange(rangeNumber))
        {
            return null;
        }

        IccSpectralRange range = new IccSpectralRange();
        range.StartWavelength = rangeNumber.StartWavelength;
        range.EndWavelength = rangeNumber.EndWavelength;
        range.Steps = rangeNumber.Steps;
        return range;
    }

    internal static int ValidateSpectralWhiteForWrite(IccSpectralProcessingData spectral, int elementIndex)
    {
        if (spectral.White == null || spectral.White.Range == null || spectral.White.Values == null || spectral.White.SpectrumCount != 1)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} shall contain one reference-white spectrum.");
        }

        IccSpectralRange range = spectral.White.Range;
        if (range.Steps < 2 || range.Steps > ushort.MaxValue || double.IsNaN(range.StartWavelength) || double.IsInfinity(range.StartWavelength) || double.IsNaN(range.EndWavelength) || double.IsInfinity(range.EndWavelength) || range.StartWavelength >= range.EndWavelength)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral wavelength range is invalid.");
        }

        float encodedStart = IccNumberCodec.ConvertDoubleToFloat16Source(range.StartWavelength);
        float encodedEnd = IccNumberCodec.ConvertDoubleToFloat16Source(range.EndWavelength);
        if (encodedStart >= encodedEnd || spectral.White.Values.Length != range.Steps)
        {
            throw new IccProfileWriteException($"Processing element {elementIndex} spectral wavelength range or reference-white sample count is invalid.");
        }

        return range.Steps;
    }

    internal static double[] ReadEncodedValueArray(BigEndianReader reader, int count, IccValueEncoding encoding)
    {
        double[] values = new double[count];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = IccNumberCodec.ReadEncodedValue(reader, encoding);
            index = index + 1;
        }

        return values;
    }

    internal static void WriteSpectralEncodedValues(BigEndianWriter writer, double[] values, IccValueEncoding encoding, int elementIndex)
    {
        int index = 0;
        while (index < values.Length)
        {
            IccNumberCodec.ValidateEncodedValueForWrite(encoding, values[index]);
            IccNumberCodec.WriteEncodedValue(writer, encoding, values[index]);
            index = index + 1;
        }
    }

    internal static void WriteSpectralFloat32Values(BigEndianWriter writer, double[] values, int elementIndex)
    {
        int index = 0;
        while (index < values.Length)
        {
            MultiProcessElementTagCodec.ValidateFloatForWrite(values[index], $"Processing element {elementIndex} spectral sample {index}");
            MultiProcessElementTagCodec.WriteFloat32Value(writer, values[index]);
            index = index + 1;
        }
    }

    internal static byte[] SerializeToneMapFunction(IccToneMapFunction function, int index)
    {
        if (function == null || function.FunctionType != 0 || function.Parameters == null || function.Parameters.Length != 3)
        {
            throw new IccProfileWriteException($"Tone-map function {index} shall use selector zero with three parameters.");
        }

        byte[] encoded = new byte[24];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(ToneMapFunctionSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16(0);
        writer.WriteZeroBytes(2);
        int parameterIndex = 0;
        while (parameterIndex < 3)
        {
            MultiProcessElementTagCodec.ValidateFloatForWrite(function.Parameters[parameterIndex], $"Tone-map function {index} parameter {parameterIndex}");
            MultiProcessElementTagCodec.WriteFloat32Value(writer, function.Parameters[parameterIndex]);
            parameterIndex = parameterIndex + 1;
        }

        return encoded;
    }

    internal static bool EncodedValuesAreValid(System.ReadOnlyMemory<byte> encoded, int offset, int count, IccValueEncoding encoding)
    {
        if (encoding == IccValueEncoding.UInt8 || encoding == IccValueEncoding.UInt16)
        {
            return true;
        }

        int index = 0;
        while (index < count)
        {
            if (encoding == IccValueEncoding.Float16)
            {
                int bits = MultiProcessElementTagCodec.ReadUInt16At(encoded, offset + (index * 2));
                if ((bits & 0x7c00) == 0x7c00)
                {
                    return false;
                }
            }
            else if (!MultiProcessElementTagCodec.IsValidFloat32At(encoded, offset + (index * 4)))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
