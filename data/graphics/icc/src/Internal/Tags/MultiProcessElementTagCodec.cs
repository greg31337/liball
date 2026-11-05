namespace icc;

internal class MultiProcessCurveDecodeResult
{
    internal string Error = string.Empty;
    internal IccCurve Curve = new IccCurve();
}

internal class MultiProcessSegmentDecodeResult
{
    internal string Error = string.Empty;
    internal IccCurveSegment Segment = new IccCurveSegment();
    internal int Size;
}

internal class MultiProcessCurveBlock
{
    internal int Offset;
    internal byte[] Bytes = new byte[0];
}

internal static class MultiProcessElementTagCodec
{
    internal const uint SegmentedCurveSignature = 0x63757266;
    internal const uint FormulaSegmentSignature = 0x70617266;
    internal const uint SampledSegmentSignature = 0x73616d66;

    internal static string DecodeKnownElement(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                return DecodeCurveSet(encoded, element, absoluteOffset);
            case IccProcessingElementKind.Matrix:
                return DecodeMatrix(encoded, element, absoluteOffset);
            case IccProcessingElementKind.Clut:
                return DecodeClut(encoded, element, absoluteOffset);
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
                return DecodeAcs(encoded, element);
            default:
                return string.Empty;
        }
    }

    internal static byte[] SerializeKnownElement(IccProcessingElement element, int index)
    {
        if (element == null)
        {
            throw new IccProfileWriteException($"Processing element {index} cannot be null.");
        }

        switch (element.Kind)
        {
            case IccProcessingElementKind.CurveSet:
                return SerializeCurveSet(element, index);
            case IccProcessingElementKind.Matrix:
                return SerializeMatrix(element, index);
            case IccProcessingElementKind.Clut:
                return SerializeClut(element, index);
            case IccProcessingElementKind.BeginAcs:
            case IccProcessingElementKind.EndAcs:
                return SerializeAcs(element, index);
            default:
                return SerializeRawElement(element, index);
        }
    }

    internal static string DecodeCurveSet(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels != element.OutputChannels)
        {
            return "a curve-set element shall have equal input and output channel counts.";
        }

        long dataStartValue = 12L + ((long)element.InputChannels * 8L);
        if (dataStartValue > encoded.Length || dataStartValue > int.MaxValue)
        {
            return "the curve-set position table extends beyond the element.";
        }

        int dataStart = (int)dataStartValue;
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.InputChannels];
        BigEndianReader reader = new BigEndianReader(encoded.Slice(12, element.InputChannels * 8), absoluteOffset + 12);
        int index = 0;
        while (index < positions.Length)
        {
            uint encodedOffset = reader.ReadUInt32();
            uint encodedSize = reader.ReadUInt32();
            if (encodedOffset > int.MaxValue || encodedSize > int.MaxValue)
            {
                return $"curve position {index} exceeds the supported managed range.";
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)encodedOffset;
            position.Size = (int)encodedSize;
            if (position.Offset < dataStart || position.Offset % 4 != 0 || position.Size < 12 || position.Size > encoded.Length - position.Offset)
            {
                return $"curve position {index} shall identify a complete curve at an aligned offset beyond the curve table.";
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
            IccCurve? shared = FindDecodedCurve(decodedPositions, decodedCurves, positions[index]);
            if (shared != null)
            {
                curves[index] = shared;
            }
            else
            {
                MultiProcessCurveDecodeResult curveResult = DecodeSegmentedCurve(encoded.Slice(positions[index].Offset, positions[index].Size), absoluteOffset + positions[index].Offset);
                if (curveResult.Error.Length > 0)
                {
                    return $"curve {index}: {curveResult.Error}";
                }

                curves[index] = curveResult.Curve;
                decodedPositions.Add(positions[index]);
                decodedCurves.Add(curveResult.Curve);
            }

            index = index + 1;
        }

        element.Curves = curves;
        return string.Empty;
    }

    internal static MultiProcessCurveDecodeResult DecodeSegmentedCurve(System.ReadOnlyMemory<byte> encoded, long absoluteOffset)
    {
        MultiProcessCurveDecodeResult result = new MultiProcessCurveDecodeResult();
        if (encoded.Length < 40)
        {
            result.Error = "a segmented curve requires at least one complete formula segment.";
            return result;
        }

        if (ReadUInt32At(encoded, 0) != SegmentedCurveSignature || !MultiProcessElementsTagCodec.HasZeroBytes(encoded, 4, 4) || encoded.Span[10] != 0 || encoded.Span[11] != 0)
        {
            result.Error = "the segmented-curve signature and reserved bytes are invalid.";
            return result;
        }

        int segmentCount = ReadUInt16At(encoded, 8);
        if (segmentCount < 1)
        {
            result.Error = "a segmented curve shall contain at least one segment.";
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
            if (!IsValidFloat32At(encoded, offset))
            {
                result.Error = $"breakpoint {index} is not a finite normal float32Number.";
                return result;
            }

            breakPoints[index] = DecodeFloat32At(encoded, offset);
            if (index > 0 && breakPoints[index] <= breakPoints[index - 1])
            {
                result.Error = "successive segmented-curve breakpoints shall strictly increase.";
                return result;
            }

            index = index + 1;
        }

        IccCurveSegment[] segments = new IccCurveSegment[segmentCount];
        int current = (int)segmentStartValue;
        index = 0;
        while (index < segmentCount)
        {
            MultiProcessSegmentDecodeResult segmentResult = DecodeCurveSegment(encoded, current);
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

    internal static MultiProcessSegmentDecodeResult DecodeCurveSegment(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        MultiProcessSegmentDecodeResult result = new MultiProcessSegmentDecodeResult();
        if (offset > encoded.Length - 12)
        {
            result.Error = "the segment header extends beyond the curve.";
            return result;
        }

        uint signature = ReadUInt32At(encoded, offset);
        if (!MultiProcessElementsTagCodec.HasZeroBytes(encoded, offset + 4, 4))
        {
            result.Error = "reserved bytes shall be zero.";
            return result;
        }

        if (signature == FormulaSegmentSignature)
        {
            if (encoded.Span[offset + 10] != 0 || encoded.Span[offset + 11] != 0)
            {
                result.Error = "formula-segment reserved bytes shall be zero.";
                return result;
            }

            int formulaType = ReadUInt16At(encoded, offset + 8);
            int parameterCount = GetFormulaParameterCount(formulaType);
            if (parameterCount < 0)
            {
                result.Error = $"formula selector {formulaType} is not an ICC.1 selector from zero through two.";
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
            int parameterIndex = 0;
            while (parameterIndex < parameterCount)
            {
                int parameterOffset = offset + 12 + (parameterIndex * 4);
                if (!IsValidFloat32At(encoded, parameterOffset))
                {
                    result.Error = $"formula parameter {parameterIndex} is not a finite normal float32Number.";
                    return result;
                }

                result.Segment.Parameters[parameterIndex] = DecodeFloat32At(encoded, parameterOffset);
                parameterIndex = parameterIndex + 1;
            }

            result.Size = size;
            return result;
        }

        if (signature == SampledSegmentSignature)
        {
            uint encodedCount = ReadUInt32At(encoded, offset + 8);
            if (encodedCount == 0 || encodedCount > int.MaxValue)
            {
                result.Error = "a sampled segment shall contain at least one sample and fit the supported managed range.";
                return result;
            }

            long sizeValue = 12L + ((long)encodedCount * 4L);
            if (sizeValue > encoded.Length - offset || sizeValue > int.MaxValue)
            {
                result.Error = "sampled-segment values extend beyond the curve.";
                return result;
            }

            int sampleCount = (int)encodedCount;
            result.Segment.Kind = IccCurveSegmentKind.Sampled;
            result.Segment.Samples = new double[sampleCount];
            int sampleIndex = 0;
            while (sampleIndex < sampleCount)
            {
                int sampleOffset = offset + 12 + (sampleIndex * 4);
                if (!IsValidFloat32At(encoded, sampleOffset))
                {
                    result.Error = $"sample {sampleIndex} is not a finite normal float32Number.";
                    return result;
                }

                result.Segment.Samples[sampleIndex] = DecodeFloat32At(encoded, sampleOffset);
                sampleIndex = sampleIndex + 1;
            }

            result.Size = (int)sizeValue;
            return result;
        }

        result.Error = $"segment signature 0x{signature:X8} is not a formula or sampled segment.";
        return result;
    }

    internal static string DecodeMatrix(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        long valueCount = (long)element.OutputChannels * (element.InputChannels + 1L);
        long expectedSize = 12L + (valueCount * 4L);
        if (expectedSize != encoded.Length || valueCount > int.MaxValue)
        {
            return $"a matrix element with {element.InputChannels} inputs and {element.OutputChannels} outputs requires exactly {expectedSize} bytes.";
        }

        IccMatrix matrix = new IccMatrix();
        matrix.Rows = element.OutputChannels;
        matrix.Columns = element.InputChannels + 1;
        matrix.Values = new double[(int)valueCount];
        int coefficientCount = element.InputChannels * element.OutputChannels;
        int index = 0;
        while (index < coefficientCount)
        {
            int encodedOffset = 12 + (index * 4);
            if (!IsValidFloat32At(encoded, encodedOffset))
            {
                return $"matrix coefficient {index} is not a finite normal float32Number.";
            }

            int row = index / element.InputChannels;
            int column = index % element.InputChannels;
            matrix.Values[(row * matrix.Columns) + column] = DecodeFloat32At(encoded, encodedOffset);
            index = index + 1;
        }

        index = 0;
        while (index < element.OutputChannels)
        {
            int encodedOffset = 12 + ((coefficientCount + index) * 4);
            if (!IsValidFloat32At(encoded, encodedOffset))
            {
                return $"matrix constant {index} is not a finite normal float32Number.";
            }

            matrix.Values[(index * matrix.Columns) + element.InputChannels] = DecodeFloat32At(encoded, encodedOffset);
            index = index + 1;
        }

        element.Matrix = matrix;
        return string.Empty;
    }

    internal static string DecodeClut(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element, long absoluteOffset)
    {
        if (element.InputChannels > 16 || encoded.Length < 28)
        {
            return "a CLUT element supports between one and sixteen input dimensions and requires a 28-byte header.";
        }

        int valueCount = element.OutputChannels;
        int index = 0;
        while (index < element.InputChannels)
        {
            int gridPointCount = encoded.Span[12 + index];
            if (gridPointCount < 2)
            {
                return $"CLUT grid dimension {index} shall contain between 2 and 255 points.";
            }

            if (valueCount > int.MaxValue / gridPointCount)
            {
                return "the CLUT dimensions exceed the supported managed size.";
            }

            valueCount = valueCount * gridPointCount;
            index = index + 1;
        }

        while (index < 16)
        {
            if (encoded.Span[12 + index] != 0)
            {
                return "unused CLUT grid-point bytes shall be zero.";
            }

            index = index + 1;
        }

        long expectedSize = 28L + ((long)valueCount * 4L);
        if (expectedSize != encoded.Length)
        {
            return $"the CLUT dimensions require exactly {expectedSize} bytes, but the element contains {encoded.Length}.";
        }

        IccClut clut = new IccClut();
        clut.InputChannels = element.InputChannels;
        clut.OutputChannels = element.OutputChannels;
        clut.GridPoints = new int[element.InputChannels];
        index = 0;
        while (index < element.InputChannels)
        {
            clut.GridPoints[index] = encoded.Span[12 + index];
            index = index + 1;
        }

        clut.Values = new double[valueCount];
        index = 0;
        while (index < valueCount)
        {
            int valueOffset = 28 + (index * 4);
            if (!IsValidFloat32At(encoded, valueOffset))
            {
                return $"CLUT value {index} is not a finite normal float32Number.";
            }

            clut.Values[index] = DecodeFloat32At(encoded, valueOffset);
            index = index + 1;
        }

        clut.Interpolation = IccInterpolationKind.Multilinear;
        element.Clut = clut;
        return string.Empty;
    }

    internal static string DecodeAcs(System.ReadOnlyMemory<byte> encoded, IccProcessingElement element)
    {
        if (element.InputChannels != element.OutputChannels)
        {
            return "a begin or end ACS element shall have equal input and output channel counts.";
        }

        if (encoded.Length < 16)
        {
            return "a begin or end ACS element requires a 16-byte header.";
        }

        element.AcsSignature = ReadUInt32At(encoded, 12);
        element.AcsData = encoded.Slice(16).ToArray();
        return string.Empty;
    }

    internal static byte[] SerializeCurveSet(IccProcessingElement element, int index)
    {
        if (element.Signature != IccProcessingElementSignatures.CurveSet || element.InputChannels < 1 || element.InputChannels > ushort.MaxValue || element.OutputChannels != element.InputChannels || element.Curves == null || element.Curves.Length != element.InputChannels)
        {
            throw new IccProfileWriteException($"Processing element {index} curve-set channels and curve count shall be equal and fit unsigned 16-bit values.");
        }

        long dataStartValue = 12L + ((long)element.InputChannels * 8L);
        if (dataStartValue > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {index} curve position table exceeds the supported managed size.");
        }

        int dataStart = (int)dataStartValue;
        byte[][] curves = new byte[element.Curves.Length][];
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[element.Curves.Length];
        System.Collections.Generic.List<MultiProcessCurveBlock> blocks = new System.Collections.Generic.List<MultiProcessCurveBlock>();
        int nextOffset = dataStart;
        int curveIndex = 0;
        while (curveIndex < element.Curves.Length)
        {
            curves[curveIndex] = SerializeSegmentedCurve(element.Curves[curveIndex], curveIndex);
            int sharedOffset = FindCurveBlockOffset(blocks, curves[curveIndex]);
            if (sharedOffset < 0)
            {
                if (curves[curveIndex].Length > int.MaxValue - nextOffset)
                {
                    throw new IccProfileWriteException($"Processing element {index} curve data exceeds the supported managed size.");
                }

                MultiProcessCurveBlock block = new MultiProcessCurveBlock();
                block.Offset = nextOffset;
                block.Bytes = curves[curveIndex];
                blocks.Add(block);
                sharedOffset = nextOffset;
                nextOffset = nextOffset + curves[curveIndex].Length;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = sharedOffset;
            position.Size = curves[curveIndex].Length;
            positions[curveIndex] = position;
            curveIndex = curveIndex + 1;
        }

        byte[] encoded = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        WriteElementHeader(writer, IccProcessingElementSignatures.CurveSet, element.InputChannels, element.OutputChannels);
        curveIndex = 0;
        while (curveIndex < positions.Length)
        {
            writer.WriteUInt32((uint)positions[curveIndex].Offset);
            writer.WriteUInt32((uint)positions[curveIndex].Size);
            curveIndex = curveIndex + 1;
        }

        curveIndex = 0;
        while (curveIndex < blocks.Count)
        {
            MultiProcessCurveBlock block = blocks[curveIndex];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(encoded, block.Offset, block.Bytes.Length));
            curveIndex = curveIndex + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeSegmentedCurve(IccCurve curve, int curveIndex)
    {
        if (curve == null || curve.Kind != IccCurveKind.Segmented || curve.Segments == null || curve.Segments.Length < 1 || curve.Segments.Length > ushort.MaxValue)
        {
            throw new IccProfileWriteException($"Curve-set curve {curveIndex} shall contain between one and 65535 segmented-curve segments.");
        }

        if (curve.Segments[0] == null || !double.IsNegativeInfinity(curve.Segments[0].BreakPoint))
        {
            throw new IccProfileWriteException($"Curve-set curve {curveIndex} first segment shall start at negative infinity.");
        }

        byte[][] segments = new byte[curve.Segments.Length][];
        long size = 12L + ((long)(curve.Segments.Length - 1) * 4L);
        int index = 0;
        double previousBreakPoint = double.NegativeInfinity;
        while (index < curve.Segments.Length)
        {
            IccCurveSegment segment = curve.Segments[index];
            if (segment == null)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} segment {index} cannot be null.");
            }

            if ((index == 0 || index == curve.Segments.Length - 1) && segment.Kind != IccCurveSegmentKind.Formula)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} first and last segments shall be formula segments.");
            }

            if (index > 0)
            {
                ValidateFloatForWrite(segment.BreakPoint, $"Curve-set curve {curveIndex} breakpoint {index - 1}");
                if (segment.BreakPoint <= previousBreakPoint)
                {
                    throw new IccProfileWriteException($"Curve-set curve {curveIndex} successive breakpoints shall strictly increase.");
                }

                previousBreakPoint = segment.BreakPoint;
            }

            segments[index] = SerializeCurveSegment(segment, curveIndex, index);
            size = size + segments[index].Length;
            if (size > int.MaxValue)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} exceeds the supported managed size.");
            }

            index = index + 1;
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32(SegmentedCurveSignature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)curve.Segments.Length);
        writer.WriteZeroBytes(2);
        index = 1;
        while (index < curve.Segments.Length)
        {
            WriteFloat32Value(writer, curve.Segments[index].BreakPoint);
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

    internal static byte[] SerializeCurveSegment(IccCurveSegment segment, int curveIndex, int segmentIndex)
    {
        if (segment.Kind == IccCurveSegmentKind.Formula)
        {
            if (segment.Parameters == null)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} formula segment {segmentIndex} parameters cannot be null.");
            }

            int parameterCount = GetFormulaParameterCount(segment.FormulaType);
            if (parameterCount < 0 || segment.Parameters.Length != parameterCount)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} formula segment {segmentIndex} has an invalid selector or parameter count.");
            }

            byte[] encoded = new byte[12 + (parameterCount * 4)];
            BigEndianWriter writer = new BigEndianWriter(encoded, 0);
            writer.WriteUInt32(FormulaSegmentSignature);
            writer.WriteZeroBytes(4);
            writer.WriteUInt16((ushort)segment.FormulaType);
            writer.WriteZeroBytes(2);
            int index = 0;
            while (index < segment.Parameters.Length)
            {
                ValidateFloatForWrite(segment.Parameters[index], $"Curve-set curve {curveIndex} formula parameter {index}");
                WriteFloat32Value(writer, segment.Parameters[index]);
                index = index + 1;
            }

            return encoded;
        }

        if (segment.Kind == IccCurveSegmentKind.Sampled)
        {
            if (segment.Samples == null || segment.Samples.Length < 1)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} sampled segment {segmentIndex} shall contain at least one sample.");
            }

            long size = 12L + ((long)segment.Samples.Length * 4L);
            if (size > int.MaxValue)
            {
                throw new IccProfileWriteException($"Curve-set curve {curveIndex} sampled segment {segmentIndex} exceeds the supported managed size.");
            }

            byte[] encoded = new byte[(int)size];
            BigEndianWriter writer = new BigEndianWriter(encoded, 0);
            writer.WriteUInt32(SampledSegmentSignature);
            writer.WriteZeroBytes(4);
            writer.WriteUInt32((uint)segment.Samples.Length);
            int index = 0;
            while (index < segment.Samples.Length)
            {
                ValidateFloatForWrite(segment.Samples[index], $"Curve-set curve {curveIndex} sample {index}");
                WriteFloat32Value(writer, segment.Samples[index]);
                index = index + 1;
            }

            return encoded;
        }

        throw new IccProfileWriteException($"Curve-set curve {curveIndex} segment {segmentIndex} has an unsupported segment kind.");
    }

    internal static byte[] SerializeMatrix(IccProcessingElement element, int index)
    {
        if (element.Signature != IccProcessingElementSignatures.Matrix || element.InputChannels < 1 || element.InputChannels > ushort.MaxValue || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || element.Matrix == null || element.Matrix.Values == null)
        {
            throw new IccProfileWriteException($"Processing element {index} matrix data and channel counts are invalid.");
        }

        long valueCount = (long)element.OutputChannels * (element.InputChannels + 1L);
        if (valueCount > int.MaxValue || element.Matrix.Rows != element.OutputChannels || element.Matrix.Columns != element.InputChannels + 1 || element.Matrix.Values.Length != (int)valueCount)
        {
            throw new IccProfileWriteException($"Processing element {index} matrix shall be a row-major output-by-(input-plus-one) affine matrix.");
        }

        long size = 12L + (valueCount * 4L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {index} matrix exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        WriteElementHeader(writer, IccProcessingElementSignatures.Matrix, element.InputChannels, element.OutputChannels);
        int row = 0;
        while (row < element.OutputChannels)
        {
            int column = 0;
            while (column < element.InputChannels)
            {
                double value = element.Matrix.Values[(row * element.Matrix.Columns) + column];
                ValidateFloatForWrite(value, $"Processing element {index} matrix coefficient");
                WriteFloat32Value(writer, value);
                column = column + 1;
            }

            row = row + 1;
        }

        row = 0;
        while (row < element.OutputChannels)
        {
            double value = element.Matrix.Values[(row * element.Matrix.Columns) + element.InputChannels];
            ValidateFloatForWrite(value, $"Processing element {index} matrix constant");
            WriteFloat32Value(writer, value);
            row = row + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeClut(IccProcessingElement element, int index)
    {
        if (element.Signature != IccProcessingElementSignatures.Clut || element.InputChannels < 1 || element.InputChannels > 16 || element.OutputChannels < 1 || element.OutputChannels > ushort.MaxValue || element.Clut == null || element.Clut.GridPoints == null || element.Clut.Values == null)
        {
            throw new IccProfileWriteException($"Processing element {index} CLUT data and channel counts are invalid.");
        }

        if (element.Clut.InputChannels != element.InputChannels || element.Clut.OutputChannels != element.OutputChannels || element.Clut.GridPoints.Length != element.InputChannels)
        {
            throw new IccProfileWriteException($"Processing element {index} CLUT dimensions shall match its element channels.");
        }

        int valueCount = element.OutputChannels;
        int gridIndex = 0;
        while (gridIndex < element.InputChannels)
        {
            int gridPointCount = element.Clut.GridPoints[gridIndex];
            if (gridPointCount < 2 || gridPointCount > 255)
            {
                throw new IccProfileWriteException($"Processing element {index} CLUT grid dimension {gridIndex} shall contain between 2 and 255 points.");
            }

            if (valueCount > int.MaxValue / gridPointCount)
            {
                throw new IccProfileWriteException($"Processing element {index} CLUT dimensions exceed the supported managed size.");
            }

            valueCount = valueCount * gridPointCount;
            gridIndex = gridIndex + 1;
        }

        if (element.Clut.Values.Length != valueCount)
        {
            throw new IccProfileWriteException($"Processing element {index} CLUT requires exactly {valueCount} values.");
        }

        long size = 28L + ((long)valueCount * 4L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException($"Processing element {index} CLUT exceeds the supported managed size.");
        }

        byte[] encoded = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        WriteElementHeader(writer, IccProcessingElementSignatures.Clut, element.InputChannels, element.OutputChannels);
        gridIndex = 0;
        while (gridIndex < element.InputChannels)
        {
            writer.WriteUInt8((byte)element.Clut.GridPoints[gridIndex]);
            gridIndex = gridIndex + 1;
        }

        writer.WriteZeroBytes(16 - element.InputChannels);
        int valueIndex = 0;
        while (valueIndex < element.Clut.Values.Length)
        {
            ValidateFloatForWrite(element.Clut.Values[valueIndex], $"Processing element {index} CLUT value {valueIndex}");
            WriteFloat32Value(writer, element.Clut.Values[valueIndex]);
            valueIndex = valueIndex + 1;
        }

        return encoded;
    }

    internal static byte[] SerializeAcs(IccProcessingElement element, int index)
    {
        bool isBegin = element.Kind == IccProcessingElementKind.BeginAcs && element.Signature == IccProcessingElementSignatures.BeginAcs;
        bool isEnd = element.Kind == IccProcessingElementKind.EndAcs && element.Signature == IccProcessingElementSignatures.EndAcs;
        if ((!isBegin && !isEnd) || element.InputChannels < 1 || element.InputChannels > ushort.MaxValue || element.OutputChannels != element.InputChannels || element.AcsData == null)
        {
            throw new IccProfileWriteException($"Processing element {index} ACS kind, signature, channels, or vendor data are invalid.");
        }

        if (element.AcsData.Length > int.MaxValue - 16)
        {
            throw new IccProfileWriteException($"Processing element {index} ACS vendor data exceeds the supported managed size.");
        }

        byte[] encoded = new byte[16 + element.AcsData.Length];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        WriteElementHeader(writer, element.Signature, element.InputChannels, element.OutputChannels);
        writer.WriteUInt32(element.AcsSignature);
        writer.WriteExactBytes(element.AcsData);
        return encoded;
    }

    internal static byte[] SerializeRawElement(IccProcessingElement element, int index)
    {
        MultiProcessElementsTagCodec.ValidateElementForWrite(element, index);
        return element.RawData.Bytes;
    }

    internal static void WriteElementHeader(BigEndianWriter writer, uint signature, int inputChannels, int outputChannels)
    {
        writer.WriteUInt32(signature);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)inputChannels);
        writer.WriteUInt16((ushort)outputChannels);
    }

    internal static IccCurve? FindDecodedCurve(System.Collections.Generic.List<MultiProcessElementPosition> positions, System.Collections.Generic.List<IccCurve> curves, MultiProcessElementPosition requested)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index].Offset == requested.Offset && positions[index].Size == requested.Size)
            {
                return curves[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static int FindCurveBlockOffset(System.Collections.Generic.List<MultiProcessCurveBlock> blocks, byte[] bytes)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            if (MultiProcessElementsTagCodec.BytesEqual(blocks[index].Bytes, bytes))
            {
                return blocks[index].Offset;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static int GetFormulaParameterCount(int formulaType)
    {
        switch (formulaType)
        {
            case 0:
                return 4;
            case 1:
            case 2:
                return 5;
            default:
                return -1;
        }
    }

    internal static bool IsValidFloat32At(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        uint bits = ReadUInt32At(encoded, offset);
        uint exponent = bits & 0x7f800000;
        uint fraction = bits & 0x007fffff;
        return exponent != 0x7f800000 && (exponent != 0 || fraction == 0);
    }

    internal static double DecodeFloat32At(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        uint bits = ReadUInt32At(encoded, offset);
        int signedBits = IccNumberCodec.ConvertUInt32BitsToInt32(bits);
        return System.BitConverter.Int32BitsToSingle(signedBits);
    }

    internal static void ValidateFloatForWrite(double value, string name)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new IccProfileWriteException($"{name} shall be finite.");
        }

        IccNumberCodec.ConvertDoubleToFloat32(value);
    }

    internal static void WriteFloat32Value(BigEndianWriter writer, double value)
    {
        float converted = IccNumberCodec.ConvertDoubleToFloat32(value);
        IccNumberCodec.WriteFloat32(writer, converted);
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        BigEndianReader reader = new BigEndianReader(encoded.Slice(offset, 4), offset);
        return reader.ReadUInt32();
    }

    internal static int ReadUInt16At(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        BigEndianReader reader = new BigEndianReader(encoded.Slice(offset, 2), offset);
        return reader.ReadUInt16();
    }
}
