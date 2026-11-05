namespace icc;

internal class MultiStageElementRange
{
    internal int Offset;
    internal int Size;
    internal bool IsCurve;
}

internal class MultiStageCurveReadResult
{
    internal string Error = string.Empty;
    internal IccCurve[] Curves = new IccCurve[0];
    internal System.Collections.Generic.List<MultiStageElementRange> Ranges = new System.Collections.Generic.List<MultiStageElementRange>();
}

internal class MultiStageWriteBlock
{
    internal int Offset;
    internal byte[] Bytes = new byte[0];
    internal bool IsCurve;
}

internal static class MultiStageLutTagCodec
{
    internal static void DecodeLutAToB(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        DecodeMultiStage(state, payload, typeSignature, true);
    }

    internal static void DecodeLutBToA(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        DecodeMultiStage(state, payload, typeSignature, false);
    }

    internal static byte[] SerializeLutAToB(IccMultiStageLut lut)
    {
        return SerializeMultiStage(lut, true);
    }

    internal static byte[] SerializeLutBToA(IccMultiStageLut lut)
    {
        return SerializeMultiStage(lut, false);
    }

    internal static void DecodeMultiStage(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, bool aToB)
    {
        string typeName = aToB ? "lutAToBType" : "lutBToAType";
        if (payload.Length < 32)
        {
            CompleteInvalid(state, payload, typeSignature, $"A {typeName} payload requires at least 32 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        System.ReadOnlySpan<byte> bytes = payload.Span;
        if (!TagCodecSupport.HasZeroReservedBytes(payload) || bytes[10] != 0 || bytes[11] != 0)
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} reserved bytes shall be zero.");
            return;
        }

        int inputChannels = bytes[8];
        int outputChannels = bytes[9];
        if (inputChannels < 1 || inputChannels > IccConstants.MaximumIcc1ChannelCount || outputChannels < 1 || outputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} input and output channel counts shall each be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
            return;
        }

        if (!aToB && state.HasProfileContext && state.ProfileConnectionSpace != IccPcsKind.Xyz && state.ProfileConnectionSpace != IccPcsKind.Lab)
        {
            CompleteInvalid(state, payload, typeSignature, "A lutBToAType shall only be used when the profile connection space is PCSXYZ or PCSLAB.");
            return;
        }

        uint bOffsetValue = ReadUInt32At(payload, 12, state.AbsolutePayloadOffset);
        uint matrixOffsetValue = ReadUInt32At(payload, 16, state.AbsolutePayloadOffset);
        uint mOffsetValue = ReadUInt32At(payload, 20, state.AbsolutePayloadOffset);
        uint clutOffsetValue = ReadUInt32At(payload, 24, state.AbsolutePayloadOffset);
        uint aOffsetValue = ReadUInt32At(payload, 28, state.AbsolutePayloadOffset);
        if (bOffsetValue == 0 || !IsRepresentableOffset(bOffsetValue) || !IsRepresentableOffset(matrixOffsetValue) || !IsRepresentableOffset(mOffsetValue) || !IsRepresentableOffset(clutOffsetValue) || !IsRepresentableOffset(aOffsetValue))
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} shall contain a B-curve offset and all offsets shall fit the supported managed range.");
            return;
        }

        int bOffset = (int)bOffsetValue;
        int matrixOffset = (int)matrixOffsetValue;
        int mOffset = (int)mOffsetValue;
        int clutOffset = (int)clutOffsetValue;
        int aOffset = (int)aOffsetValue;
        bool hasMatrix = matrixOffset != 0;
        bool hasM = mOffset != 0;
        bool hasClut = clutOffset != 0;
        bool hasA = aOffset != 0;
        if (hasMatrix != hasM || hasClut != hasA)
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} requires matrix and M-curve stages together, and CLUT and A-curve stages together.");
            return;
        }

        if (!hasClut && inputChannels != outputChannels)
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} requires a CLUT when its input and output channel counts differ.");
            return;
        }

        if (hasMatrix && ((aToB && outputChannels != 3) || (!aToB && inputChannels != 3)))
        {
            CompleteInvalid(state, payload, typeSignature, $"The {typeName} matrix stage requires three channels at the matrix and M-curve transition.");
            return;
        }

        if (!IsValidDataOffset(bOffset, payload.Length) || (hasMatrix && !IsValidDataOffset(matrixOffset, payload.Length)) || (hasM && !IsValidDataOffset(mOffset, payload.Length)) || (hasClut && !IsValidDataOffset(clutOffset, payload.Length)) || (hasA && !IsValidDataOffset(aOffset, payload.Length)))
        {
            CompleteInvalid(state, payload, typeSignature, $"Every present {typeName} element offset shall be four-byte aligned and refer beyond the 32-byte header.");
            return;
        }

        int bCount = aToB ? outputChannels : inputChannels;
        int mCount = aToB ? outputChannels : inputChannels;
        int aCount = aToB ? inputChannels : outputChannels;
        System.Collections.Generic.List<MultiStageElementRange> ranges = new System.Collections.Generic.List<MultiStageElementRange>();
        MultiStageCurveReadResult bResult = ReadCurveSet(payload, bOffset, bCount, state);
        if (bResult.Error.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, bResult.Error);
            return;
        }

        AddRanges(ranges, bResult.Ranges);
        IccMatrix? matrix = null;
        if (hasMatrix)
        {
            if (matrixOffset > payload.Length - 48)
            {
                CompleteInvalid(state, payload, typeSignature, $"The {typeName} matrix extends beyond the payload.");
                return;
            }

            matrix = ReadMatrix(payload, matrixOffset, state.AbsolutePayloadOffset);
            AddRange(ranges, matrixOffset, 48, false);
        }

        IccCurve[] mCurves = new IccCurve[0];
        if (hasM)
        {
            MultiStageCurveReadResult mResult = ReadCurveSet(payload, mOffset, mCount, state);
            if (mResult.Error.Length > 0)
            {
                CompleteInvalid(state, payload, typeSignature, mResult.Error);
                return;
            }

            mCurves = mResult.Curves;
            AddRanges(ranges, mResult.Ranges);
        }

        IccClut? clut = null;
        if (hasClut)
        {
            string clutError = ReadClut(payload, clutOffset, inputChannels, outputChannels, ranges);
            if (clutError.Length > 0)
            {
                CompleteInvalid(state, payload, typeSignature, clutError);
                return;
            }

            clut = CreateClut(payload, clutOffset, inputChannels, outputChannels);
        }

        IccCurve[] aCurves = new IccCurve[0];
        if (hasA)
        {
            MultiStageCurveReadResult aResult = ReadCurveSet(payload, aOffset, aCount, state);
            if (aResult.Error.Length > 0)
            {
                CompleteInvalid(state, payload, typeSignature, aResult.Error);
                return;
            }

            aCurves = aResult.Curves;
            AddRanges(ranges, aResult.Ranges);
        }

        string topologyError = ValidateRanges(ranges, payload.Length, typeName);
        if (topologyError.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, topologyError);
            return;
        }

        IccMultiStageLut lut = new IccMultiStageLut();
        lut.InputChannels = inputChannels;
        lut.OutputChannels = outputChannels;
        lut.ACurves = aCurves;
        lut.BCurves = bResult.Curves;
        lut.MCurves = mCurves;
        lut.Matrix = matrix;
        lut.Clut = clut;
        lut.PcsEncoding = IccPcsEncodingKind.Version4;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Lut);
        data.MultiStageLut = lut;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {typeName} with {inputChannels} input channels and {outputChannels} output channels.");
    }

    internal static MultiStageCurveReadResult ReadCurveSet(System.ReadOnlyMemory<byte> payload, int offset, int count, TagDecodeState state)
    {
        MultiStageCurveReadResult result = new MultiStageCurveReadResult();
        result.Curves = new IccCurve[count];
        int current = offset;
        int index = 0;
        while (index < count)
        {
            if (current > payload.Length - 12)
            {
                result.Error = $"Curve {index} at offset {current} does not contain a complete curve header.";
                return result;
            }

            uint signature = ReadUInt32At(payload, current, state.AbsolutePayloadOffset);
            if (!HasZeroBytes(payload, current + 4, 4))
            {
                result.Error = $"Curve {index} at offset {current} has non-zero reserved bytes.";
                return result;
            }

            int logicalSize;
            IccCurve curve = new IccCurve();
            if (signature == IccTypeSignatures.Curve)
            {
                uint declaredCount = ReadUInt32At(payload, current + 8, state.AbsolutePayloadOffset);
                long size = 12L + ((long)declaredCount * 2L);
                if (size > int.MaxValue || size > payload.Length - current)
                {
                    result.Error = $"Curve {index} at offset {current} has a count that extends beyond the payload.";
                    return result;
                }

                logicalSize = (int)size;
                if (declaredCount == 0)
                {
                    curve.Kind = IccCurveKind.Identity;
                    curve.Gamma = 1.0;
                }
                else if (declaredCount == 1)
                {
                    BigEndianReader gammaReader = new BigEndianReader(payload.Slice(current + 12, 2), state.AbsolutePayloadOffset + current + 12);
                    curve.Kind = IccCurveKind.Gamma;
                    curve.Gamma = IccNumberCodec.ReadU8Fixed8(gammaReader);
                }
                else
                {
                    int sampleCount = (int)declaredCount;
                    curve.Kind = IccCurveKind.Sampled;
                    curve.Samples = LutTagCodec.DecodeTable(payload, current + 12, sampleCount, true);
                }
            }
            else if (signature == IccTypeSignatures.ParametricCurve)
            {
                if (payload.Span[current + 10] != 0 || payload.Span[current + 11] != 0)
                {
                    result.Error = $"Parametric curve {index} at offset {current} has non-zero reserved bytes.";
                    return result;
                }

                int functionType = ReadUInt16At(payload, current + 8, state.AbsolutePayloadOffset);
                int parameterCount = CurveTagCodec.GetParameterCount(functionType);
                if (parameterCount < 0)
                {
                    result.Error = $"Parametric curve {index} at offset {current} uses unsupported ICC.1 function selector {functionType}.";
                    return result;
                }

                logicalSize = 12 + (parameterCount * 4);
                if (logicalSize > payload.Length - current)
                {
                    result.Error = $"Parametric curve {index} at offset {current} extends beyond the payload.";
                    return result;
                }

                IccParametricCurve parametric = new IccParametricCurve();
                parametric.FunctionType = functionType;
                parametric.Parameters = new double[parameterCount];
                BigEndianReader parameterReader = new BigEndianReader(payload.Slice(current + 12, parameterCount * 4), state.AbsolutePayloadOffset + current + 12);
                int parameterIndex = 0;
                while (parameterIndex < parameterCount)
                {
                    parametric.Parameters[parameterIndex] = IccNumberCodec.ReadS15Fixed16(parameterReader);
                    parameterIndex = parameterIndex + 1;
                }

                curve.Kind = IccCurveKind.Parametric;
                curve.Parametric = parametric;
            }
            else
            {
                result.Error = $"Curve {index} at offset {current} does not use curveType or parametricCurveType.";
                return result;
            }

            if (logicalSize > int.MaxValue - 3)
            {
                result.Error = $"Curve {index} at offset {current} exceeds the supported aligned size.";
                return result;
            }

            int paddedSize = AlignFour(logicalSize);
            if (paddedSize > payload.Length - current || !HasZeroBytes(payload, current + logicalSize, paddedSize - logicalSize))
            {
                result.Error = $"Curve {index} at offset {current} has missing or non-zero alignment padding.";
                return result;
            }

            result.Curves[index] = curve;
            AddRange(result.Ranges, current, paddedSize, true);
            current = current + paddedSize;
            index = index + 1;
        }

        return result;
    }

    internal static IccMatrix ReadMatrix(System.ReadOnlyMemory<byte> payload, int offset, long absoluteOffset)
    {
        IccMatrix matrix = new IccMatrix();
        matrix.Rows = 3;
        matrix.Columns = 4;
        matrix.Values = new double[12];
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 48), absoluteOffset + offset);
        int index = 0;
        while (index < 9)
        {
            int row = index / 3;
            int column = index % 3;
            matrix.Values[(row * 4) + column] = IccNumberCodec.ReadS15Fixed16(reader);
            index = index + 1;
        }

        matrix.Values[3] = IccNumberCodec.ReadS15Fixed16(reader);
        matrix.Values[7] = IccNumberCodec.ReadS15Fixed16(reader);
        matrix.Values[11] = IccNumberCodec.ReadS15Fixed16(reader);

        return matrix;
    }

    internal static string ReadClut(System.ReadOnlyMemory<byte> payload, int offset, int inputChannels, int outputChannels, System.Collections.Generic.List<MultiStageElementRange> ranges)
    {
        if (offset > payload.Length - 20)
        {
            return $"The CLUT at offset {offset} does not contain its complete header.";
        }

        int index = 0;
        int valueCount = outputChannels;
        while (index < inputChannels)
        {
            int gridPointCount = payload.Span[offset + index];
            if (gridPointCount < 2)
            {
                return $"CLUT grid dimension {index} shall contain between 2 and 255 points.";
            }

            if (valueCount > int.MaxValue / gridPointCount)
            {
                return "The CLUT dimensions exceed the supported managed size.";
            }

            valueCount = valueCount * gridPointCount;
            index = index + 1;
        }

        while (index < 16)
        {
            if (payload.Span[offset + index] != 0)
            {
                return "Unused CLUT grid-point bytes shall be zero.";
            }

            index = index + 1;
        }

        int precision = payload.Span[offset + 16];
        if ((precision != 1 && precision != 2) || !HasZeroBytes(payload, offset + 17, 3))
        {
            return "The CLUT precision shall be one or two bytes and its reserved bytes shall be zero.";
        }

        long logicalSizeValue = 20L + ((long)valueCount * precision);
        if (logicalSizeValue > int.MaxValue || logicalSizeValue > payload.Length - offset)
        {
            return "The CLUT values extend beyond the payload or exceed the supported managed size.";
        }

        int logicalSize = (int)logicalSizeValue;
        if (logicalSize > int.MaxValue - 3)
        {
            return "The aligned CLUT size exceeds the supported managed size.";
        }

        int paddedSize = AlignFour(logicalSize);
        if (paddedSize > payload.Length - offset || !HasZeroBytes(payload, offset + logicalSize, paddedSize - logicalSize))
        {
            return "The CLUT has missing or non-zero alignment padding.";
        }

        AddRange(ranges, offset, paddedSize, false);
        return string.Empty;
    }

    internal static IccClut CreateClut(System.ReadOnlyMemory<byte> payload, int offset, int inputChannels, int outputChannels)
    {
        IccClut clut = new IccClut();
        clut.InputChannels = inputChannels;
        clut.OutputChannels = outputChannels;
        clut.GridPoints = new int[inputChannels];
        int valueCount = outputChannels;
        int index = 0;
        while (index < inputChannels)
        {
            clut.GridPoints[index] = payload.Span[offset + index];
            valueCount = valueCount * clut.GridPoints[index];
            index = index + 1;
        }

        int precision = payload.Span[offset + 16];
        clut.Values = LutTagCodec.DecodeTable(payload, offset + 20, valueCount, precision == 2);
        clut.Interpolation = IccInterpolationKind.Multilinear;
        return clut;
    }

    internal static string ValidateRanges(System.Collections.Generic.List<MultiStageElementRange> ranges, int payloadLength, string typeName)
    {
        System.Collections.Generic.List<MultiStageElementRange> unique = new System.Collections.Generic.List<MultiStageElementRange>();
        int rangeIndex = 0;
        while (rangeIndex < ranges.Count)
        {
            MultiStageElementRange candidate = ranges[rangeIndex];
            bool duplicate = false;
            int uniqueIndex = 0;
            while (uniqueIndex < unique.Count)
            {
                MultiStageElementRange existing = unique[uniqueIndex];
                if (candidate.Offset == existing.Offset && candidate.Size == existing.Size)
                {
                    if (!candidate.IsCurve || !existing.IsCurve)
                    {
                        return $"Only curve data elements may share offsets in a {typeName} payload.";
                    }

                    duplicate = true;
                }
                else if (candidate.Offset < existing.Offset + existing.Size && existing.Offset < candidate.Offset + candidate.Size)
                {
                    return $"The {typeName} data elements overlap illegally.";
                }

                uniqueIndex = uniqueIndex + 1;
            }

            if (!duplicate)
            {
                unique.Add(candidate);
            }

            rangeIndex = rangeIndex + 1;
        }

        int first = 0;
        while (first < unique.Count)
        {
            int smallest = first;
            int second = first + 1;
            while (second < unique.Count)
            {
                if (unique[second].Offset < unique[smallest].Offset)
                {
                    smallest = second;
                }

                second = second + 1;
            }

            if (smallest != first)
            {
                MultiStageElementRange swap = unique[first];
                unique[first] = unique[smallest];
                unique[smallest] = swap;
            }

            first = first + 1;
        }

        int expectedOffset = 32;
        rangeIndex = 0;
        while (rangeIndex < unique.Count)
        {
            if (unique[rangeIndex].Offset != expectedOffset)
            {
                return $"The {typeName} contains a gap or hidden unreferenced data at offset {expectedOffset}.";
            }

            expectedOffset = expectedOffset + unique[rangeIndex].Size;
            rangeIndex = rangeIndex + 1;
        }

        if (expectedOffset != payloadLength)
        {
            return $"The {typeName} contains trailing or unreferenced data after offset {expectedOffset}.";
        }

        return string.Empty;
    }

    internal static byte[] SerializeMultiStage(IccMultiStageLut lut, bool aToB)
    {
        string typeName = aToB ? "lutAToBType" : "lutBToAType";
        ValidateForWrite(lut, aToB, typeName);
        System.Collections.Generic.List<MultiStageWriteBlock> blocks = new System.Collections.Generic.List<MultiStageWriteBlock>();
        int nextOffset = 32;
        byte[] bBytes = SerializeCurveSet(lut.BCurves);
        int bOffset = AddWriteBlock(blocks, bBytes, true, nextOffset);
        nextOffset = CalculateNextOffset(blocks);

        int matrixOffset = 0;
        if (lut.Matrix != null)
        {
            byte[] matrixBytes = SerializeMatrix(lut.Matrix);
            matrixOffset = AddWriteBlock(blocks, matrixBytes, false, nextOffset);
            nextOffset = CalculateNextOffset(blocks);
        }

        int mOffset = 0;
        if (lut.MCurves.Length > 0)
        {
            byte[] mBytes = SerializeCurveSet(lut.MCurves);
            mOffset = AddWriteBlock(blocks, mBytes, true, nextOffset);
            nextOffset = CalculateNextOffset(blocks);
        }

        int clutOffset = 0;
        if (lut.Clut != null)
        {
            byte[] clutBytes = SerializeClut(lut.Clut);
            clutOffset = AddWriteBlock(blocks, clutBytes, false, nextOffset);
            nextOffset = CalculateNextOffset(blocks);
        }

        int aOffset = 0;
        if (lut.ACurves.Length > 0)
        {
            byte[] aBytes = SerializeCurveSet(lut.ACurves);
            aOffset = AddWriteBlock(blocks, aBytes, true, nextOffset);
            nextOffset = CalculateNextOffset(blocks);
        }

        byte[] payload = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(aToB ? IccTypeSignatures.LutAToB : IccTypeSignatures.LutBToA);
        writer.WriteZeroBytes(4);
        writer.WriteUInt8((byte)lut.InputChannels);
        writer.WriteUInt8((byte)lut.OutputChannels);
        writer.WriteZeroBytes(2);
        writer.WriteUInt32((uint)bOffset);
        writer.WriteUInt32((uint)matrixOffset);
        writer.WriteUInt32((uint)mOffset);
        writer.WriteUInt32((uint)clutOffset);
        writer.WriteUInt32((uint)aOffset);
        int blockIndex = 0;
        while (blockIndex < blocks.Count)
        {
            MultiStageWriteBlock block = blocks[blockIndex];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(payload, block.Offset, block.Bytes.Length));
            blockIndex = blockIndex + 1;
        }

        return payload;
    }

    internal static void ValidateForWrite(IccMultiStageLut lut, bool aToB, string typeName)
    {
        if (lut == null || lut.ACurves == null || lut.BCurves == null || lut.MCurves == null)
        {
            throw new IccProfileWriteException($"The {typeName} data and curve arrays cannot be null.");
        }

        if (lut.InputChannels < 1 || lut.InputChannels > IccConstants.MaximumIcc1ChannelCount || lut.OutputChannels < 1 || lut.OutputChannels > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"The {typeName} input and output channel counts shall each be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
        }

        int bCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int mCount = aToB ? lut.OutputChannels : lut.InputChannels;
        int aCount = aToB ? lut.InputChannels : lut.OutputChannels;
        if (lut.BCurves.Length != bCount)
        {
            throw new IccProfileWriteException($"The {typeName} shall contain exactly {bCount} B curves.");
        }

        bool hasMatrix = lut.Matrix != null;
        bool hasM = lut.MCurves.Length > 0;
        bool hasClut = lut.Clut != null;
        bool hasA = lut.ACurves.Length > 0;
        if (hasMatrix != hasM || hasClut != hasA)
        {
            throw new IccProfileWriteException($"The {typeName} requires matrix and M-curve stages together, and CLUT and A-curve stages together.");
        }

        if (hasM && lut.MCurves.Length != mCount)
        {
            throw new IccProfileWriteException($"The {typeName} shall contain exactly {mCount} M curves when that stage is present.");
        }

        if (hasA && lut.ACurves.Length != aCount)
        {
            throw new IccProfileWriteException($"The {typeName} shall contain exactly {aCount} A curves when that stage is present.");
        }

        if (!hasClut && lut.InputChannels != lut.OutputChannels)
        {
            throw new IccProfileWriteException($"The {typeName} requires a CLUT when its input and output channel counts differ.");
        }

        if (hasMatrix)
        {
            if ((aToB && lut.OutputChannels != 3) || (!aToB && lut.InputChannels != 3))
            {
                throw new IccProfileWriteException($"The {typeName} matrix stage requires three channels at the matrix and M-curve transition.");
            }

            if (lut.Matrix == null || lut.Matrix.Values == null || lut.Matrix.Rows != 3 || lut.Matrix.Columns != 4 || lut.Matrix.Values.Length != 12)
            {
                throw new IccProfileWriteException($"The {typeName} matrix shall contain exactly three rows, four columns, and twelve values including translation terms.");
            }

            LutTagCodec.ValidateValues(lut.Matrix.Values, false);
        }

        if (hasClut)
        {
            ValidateClutForWrite(lut.Clut, lut.InputChannels, lut.OutputChannels, typeName);
        }

        ValidateCurvesForWrite(lut.BCurves);
        ValidateCurvesForWrite(lut.MCurves);
        ValidateCurvesForWrite(lut.ACurves);
    }

    internal static void ValidateClutForWrite(IccClut? clut, int inputChannels, int outputChannels, string typeName)
    {
        if (clut == null || clut.GridPoints == null || clut.Values == null || clut.InputChannels != inputChannels || clut.OutputChannels != outputChannels || clut.GridPoints.Length != inputChannels)
        {
            throw new IccProfileWriteException($"The {typeName} CLUT dimensions shall match the LUT input and output channel counts.");
        }

        int valueCount = outputChannels;
        int index = 0;
        while (index < inputChannels)
        {
            int gridPointCount = clut.GridPoints[index];
            if (gridPointCount < 2 || gridPointCount > 255)
            {
                throw new IccProfileWriteException($"The {typeName} CLUT grid dimension {index} shall contain between 2 and 255 points.");
            }

            if (valueCount > int.MaxValue / gridPointCount)
            {
                throw new IccProfileWriteException($"The {typeName} CLUT dimensions exceed the supported managed size.");
            }

            valueCount = valueCount * gridPointCount;
            index = index + 1;
        }

        if (clut.Values.Length != valueCount)
        {
            throw new IccProfileWriteException($"The {typeName} CLUT requires exactly {valueCount} values, but it contains {clut.Values.Length}.");
        }

        LutTagCodec.ValidateValues(clut.Values, true);
    }

    internal static void ValidateCurvesForWrite(IccCurve[] curves)
    {
        int index = 0;
        while (index < curves.Length)
        {
            SerializeCurveData(curves[index]);
            index = index + 1;
        }
    }

    internal static byte[] SerializeCurveSet(IccCurve[] curves)
    {
        byte[][] encoded = new byte[curves.Length][];
        long size = 0;
        int index = 0;
        while (index < curves.Length)
        {
            encoded[index] = SerializeCurveData(curves[index]);
            size = size + AlignFour(encoded[index].Length);
            if (size > int.MaxValue)
            {
                throw new IccProfileWriteException("The multi-stage LUT curve data exceeds the supported managed size.");
            }

            index = index + 1;
        }

        byte[] bytes = new byte[(int)size];
        int offset = 0;
        index = 0;
        while (index < encoded.Length)
        {
            new System.ReadOnlySpan<byte>(encoded[index]).CopyTo(new System.Span<byte>(bytes, offset, encoded[index].Length));
            offset = offset + AlignFour(encoded[index].Length);
            index = index + 1;
        }

        return bytes;
    }

    internal static byte[] SerializeCurveData(IccCurve curve)
    {
        if (curve == null)
        {
            throw new IccProfileWriteException("A multi-stage LUT curve cannot be null.");
        }

        if (curve.Kind == IccCurveKind.Parametric)
        {
            if (curve.Parametric == null)
            {
                throw new IccProfileWriteException("An active parametric curve shall contain parametric curve data.");
            }

            return CurveTagCodec.SerializeParametricCurve(curve.Parametric);
        }

        return CurveTagCodec.SerializeCurve(curve);
    }

    internal static byte[] SerializeMatrix(IccMatrix matrix)
    {
        byte[] bytes = new byte[48];
        BigEndianWriter writer = new BigEndianWriter(bytes, 0);
        int index = 0;
        while (index < 9)
        {
            int row = index / 3;
            int column = index % 3;
            IccNumberCodec.WriteS15Fixed16(writer, matrix.Values[(row * 4) + column]);
            index = index + 1;
        }

        IccNumberCodec.WriteS15Fixed16(writer, matrix.Values[3]);
        IccNumberCodec.WriteS15Fixed16(writer, matrix.Values[7]);
        IccNumberCodec.WriteS15Fixed16(writer, matrix.Values[11]);

        return bytes;
    }

    internal static byte[] SerializeClut(IccClut clut)
    {
        long logicalSizeValue = 20L + ((long)clut.Values.Length * 2L);
        if (logicalSizeValue > int.MaxValue - 3)
        {
            throw new IccProfileWriteException("The multi-stage LUT CLUT exceeds the supported aligned size.");
        }

        int logicalSize = (int)logicalSizeValue;
        byte[] bytes = new byte[AlignFour(logicalSize)];
        BigEndianWriter writer = new BigEndianWriter(bytes, 0);
        int index = 0;
        while (index < clut.GridPoints.Length)
        {
            writer.WriteUInt8((byte)clut.GridPoints[index]);
            index = index + 1;
        }

        writer.WriteZeroBytes(16 - clut.GridPoints.Length);
        writer.WriteUInt8(2);
        writer.WriteZeroBytes(3);
        LutTagCodec.WriteValues(writer, clut.Values, true);
        writer.AlignWritePosition(4);
        return bytes;
    }

    internal static int AddWriteBlock(System.Collections.Generic.List<MultiStageWriteBlock> blocks, byte[] bytes, bool isCurve, int nextOffset)
    {
        if (isCurve)
        {
            int index = 0;
            while (index < blocks.Count)
            {
                if (blocks[index].IsCurve && BytesEqual(blocks[index].Bytes, bytes))
                {
                    return blocks[index].Offset;
                }

                index = index + 1;
            }
        }

        if (bytes.Length > int.MaxValue - nextOffset)
        {
            throw new IccProfileWriteException("The multi-stage LUT payload exceeds the supported managed size.");
        }

        MultiStageWriteBlock block = new MultiStageWriteBlock();
        block.Offset = nextOffset;
        block.Bytes = bytes;
        block.IsCurve = isCurve;
        blocks.Add(block);
        return nextOffset;
    }

    internal static int CalculateNextOffset(System.Collections.Generic.List<MultiStageWriteBlock> blocks)
    {
        if (blocks.Count == 0)
        {
            return 32;
        }

        MultiStageWriteBlock last = blocks[blocks.Count - 1];
        return last.Offset + last.Bytes.Length;
    }

    internal static bool BytesEqual(byte[] first, byte[] second)
    {
        if (first.Length != second.Length)
        {
            return false;
        }

        int index = 0;
        while (index < first.Length)
        {
            if (first[index] != second[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsRepresentableOffset(uint value)
    {
        return value == 0 || value <= int.MaxValue;
    }

    internal static bool IsValidDataOffset(int offset, int payloadLength)
    {
        return offset >= 32 && offset < payloadLength && offset % 4 == 0;
    }

    internal static uint ReadUInt32At(System.ReadOnlyMemory<byte> payload, int offset, long absoluteOffset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 4), absoluteOffset + offset);
        return reader.ReadUInt32();
    }

    internal static int ReadUInt16At(System.ReadOnlyMemory<byte> payload, int offset, long absoluteOffset)
    {
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 2), absoluteOffset + offset);
        return reader.ReadUInt16();
    }

    internal static bool HasZeroBytes(System.ReadOnlyMemory<byte> payload, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (payload.Span[offset + index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static int AlignFour(int value)
    {
        return (value + 3) & -4;
    }

    internal static void AddRange(System.Collections.Generic.List<MultiStageElementRange> ranges, int offset, int size, bool isCurve)
    {
        MultiStageElementRange range = new MultiStageElementRange();
        range.Offset = offset;
        range.Size = size;
        range.IsCurve = isCurve;
        ranges.Add(range);
    }

    internal static void AddRanges(System.Collections.Generic.List<MultiStageElementRange> destination, System.Collections.Generic.List<MultiStageElementRange> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CompleteInvalid(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, message);
    }
}
