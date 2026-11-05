namespace icc;

internal class AdaptiveGainCurveDecodeResult
{
    internal string Error = string.Empty;
    internal IccAdaptiveGainCurveNode[] Nodes = new IccAdaptiveGainCurveNode[0];
}

internal class AdaptiveGainCurveStoredBlock
{
    internal int Offset;
    internal byte[] Bytes = new byte[0];
}

internal static class AdaptiveGainCurveTagCodec
{
    internal const int HeaderSize = 128;
    internal const int MaximumNodeCount = 32;

    internal static void DecodeAdaptiveGainCurve(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < HeaderSize + 28)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"An adaptiveGainCurveType payload requires a 128-byte header and at least one two-node curve, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload) || !HasZeroBytes(payload, 124, 4))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The adaptiveGainCurveType reserved bytes shall be zero.");
            return;
        }

        uint functionType = ReadUInt32At(payload, 8);
        if (functionType != 1)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Adaptive Gain Curve function type {functionType} is not the defined type 1 function.");
            return;
        }

        if (!HasValidHeaderFloat32Values(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "Adaptive Gain Curve header float values shall be finite normal float32Numbers.");
            return;
        }

        byte[] identifier = payload.Slice(12, 16).ToArray();
        bool isImageSpecific = HasNonZeroByte(identifier);
        string contextError = ValidateContext(state, isImageSpecific);
        if (contextError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, contextError);
            return;
        }

        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[3];
        BigEndianReader positionReader = new BigEndianReader(payload.Slice(100, 24), state.AbsolutePayloadOffset + 100);
        int index = 0;
        while (index < positions.Length)
        {
            uint encodedOffset = positionReader.ReadUInt32();
            uint encodedSize = positionReader.ReadUInt32();
            if (encodedOffset > int.MaxValue || encodedSize > int.MaxValue)
            {
                CompletePositionInvalid(state, payload, typeSignature, index, "exceeds the supported managed range");
                return;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)encodedOffset;
            position.Size = (int)encodedSize;
            if (position.Offset < HeaderSize || position.Offset % 4 != 0 || position.Size < 28 || position.Size > payload.Length - position.Offset)
            {
                CompletePositionInvalid(state, payload, typeSignature, index, "shall identify an aligned complete two-node curve beyond the header");
                return;
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = MultiProcessElementsTagCodec.ValidatePositionTopology(positions, HeaderSize, payload.Length);
        if (topologyError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The adaptive gain curve storage is invalid: {topologyError}");
            return;
        }

        IccAdaptiveGainCurveNode[][] curves = new IccAdaptiveGainCurveNode[3][];
        System.Collections.Generic.List<MultiProcessElementPosition> decodedPositions = new System.Collections.Generic.List<MultiProcessElementPosition>();
        System.Collections.Generic.List<IccAdaptiveGainCurveNode[]> decodedCurves = new System.Collections.Generic.List<IccAdaptiveGainCurveNode[]>();
        index = 0;
        while (index < positions.Length)
        {
            IccAdaptiveGainCurveNode[]? shared = FindDecodedCurve(decodedPositions, decodedCurves, positions[index]);
            if (shared != null)
            {
                curves[index] = shared;
            }
            else
            {
                AdaptiveGainCurveDecodeResult curveResult = DecodeCurve(payload.Slice(positions[index].Offset, positions[index].Size));
                if (curveResult.Error.Length > 0)
                {
                    TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Adaptive gain curve channel {index}: {curveResult.Error}");
                    return;
                }

                curves[index] = curveResult.Nodes;
                decodedPositions.Add(positions[index]);
                decodedCurves.Add(curveResult.Nodes);
            }

            index = index + 1;
        }

        IccAdaptiveGainCurve curve = new IccAdaptiveGainCurve();
        curve.Identifier = identifier;
        curve.TargetHeadroom = DecodeFloat32At(payload, 28);
        curve.AlternateHeadroom = DecodeFloat32At(payload, 32);
        curve.FunctionData = payload.Slice(36, 52).ToArray();
        curve.BackwardCompatibleTargetHeadrooms = new double[]
        {
            DecodeFloat32At(payload, 88),
            DecodeFloat32At(payload, 92),
            DecodeFloat32At(payload, 96)
        };
        curve.RedNodes = curves[0];
        curve.GreenNodes = curves[1];
        curve.BlueNodes = curves[2];
        curve.IsImageSpecific = isImageSpecific;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.AdaptiveGainCurve);
        data.AdaptiveGainCurve = curve;
        string message = isImageSpecific
            ? "Decoded image-specific adaptiveGainCurveType function 1 data."
            : "Decoded non-image-specific adaptiveGainCurveType function 1 data.";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static byte[] SerializeAdaptiveGainCurve(IccAdaptiveGainCurve curve, IccProfileClass profileClass, IccColorSpaceKind dataColorSpace, bool isEmbedded, bool requiresEmbeddedData)
    {
        if (curve == null || curve.Identifier == null || curve.Identifier.Length != 16 || curve.FunctionData == null || curve.FunctionData.Length != 52 || curve.BackwardCompatibleTargetHeadrooms == null || curve.BackwardCompatibleTargetHeadrooms.Length != 3)
        {
            throw new IccProfileWriteException("Adaptive Gain Curve identifier and fixed header arrays have invalid lengths.");
        }

        bool hasIdentifier = HasNonZeroByte(curve.Identifier);
        if (curve.IsImageSpecific != hasIdentifier)
        {
            throw new IccProfileWriteException("An image-specific Adaptive Gain Curve shall have a non-zero GUID, and a non-image-specific curve shall have an all-zero GUID.");
        }

        string contextError = ValidateWriteContext(profileClass, dataColorSpace, curve.IsImageSpecific, isEmbedded, requiresEmbeddedData);
        if (contextError.Length > 0)
        {
            throw new IccProfileWriteException(contextError);
        }

        if (!HasValidFunctionDataFloat32Values(curve.FunctionData))
        {
            throw new IccProfileWriteException("Adaptive Gain Curve function metadata float values shall be finite normal float32Numbers.");
        }

        ValidateFloatForWrite(curve.TargetHeadroom, "Adaptive Gain Curve target headroom");
        ValidateFloatForWrite(curve.AlternateHeadroom, "Adaptive Gain Curve alternate headroom");
        int index = 0;
        while (index < curve.BackwardCompatibleTargetHeadrooms.Length)
        {
            ValidateFloatForWrite(curve.BackwardCompatibleTargetHeadrooms[index], $"Adaptive Gain Curve backward-compatible target headroom {index}");
            index = index + 1;
        }

        IccAdaptiveGainCurveNode[][] nodeArrays = new IccAdaptiveGainCurveNode[][] { curve.RedNodes, curve.GreenNodes, curve.BlueNodes };
        byte[][] encodedCurves = new byte[3][];
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[3];
        System.Collections.Generic.List<AdaptiveGainCurveStoredBlock> blocks = new System.Collections.Generic.List<AdaptiveGainCurveStoredBlock>();
        int nextOffset = HeaderSize;
        index = 0;
        while (index < nodeArrays.Length)
        {
            string nodeError = ValidateNodes(nodeArrays[index]);
            if (nodeError.Length > 0)
            {
                throw new IccProfileWriteException($"Adaptive gain curve channel {index}: {nodeError}");
            }

            encodedCurves[index] = SerializeCurve(nodeArrays[index]);
            int sharedOffset = FindStoredBlockOffset(blocks, encodedCurves[index]);
            if (sharedOffset < 0)
            {
                if (encodedCurves[index].Length > int.MaxValue - nextOffset)
                {
                    throw new IccProfileWriteException("Adaptive Gain Curve data exceeds the supported managed size.");
                }

                AdaptiveGainCurveStoredBlock block = new AdaptiveGainCurveStoredBlock();
                block.Offset = nextOffset;
                block.Bytes = encodedCurves[index];
                blocks.Add(block);
                sharedOffset = nextOffset;
                nextOffset = nextOffset + encodedCurves[index].Length;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = sharedOffset;
            position.Size = encodedCurves[index].Length;
            positions[index] = position;
            index = index + 1;
        }

        byte[] payload = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.AdaptiveGainCurve);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32(1);
        writer.WriteExactBytes(curve.Identifier);
        WriteFloat32Value(writer, curve.TargetHeadroom);
        WriteFloat32Value(writer, curve.AlternateHeadroom);
        writer.WriteExactBytes(curve.FunctionData);
        index = 0;
        while (index < curve.BackwardCompatibleTargetHeadrooms.Length)
        {
            WriteFloat32Value(writer, curve.BackwardCompatibleTargetHeadrooms[index]);
            index = index + 1;
        }

        index = 0;
        while (index < positions.Length)
        {
            writer.WriteUInt32((uint)positions[index].Offset);
            writer.WriteUInt32((uint)positions[index].Size);
            index = index + 1;
        }

        writer.WriteZeroBytes(4);
        index = 0;
        while (index < blocks.Count)
        {
            AdaptiveGainCurveStoredBlock block = blocks[index];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(payload, block.Offset, block.Bytes.Length));
            index = index + 1;
        }

        return payload;
    }

    internal static double EvaluateCurve(IccAdaptiveGainCurveNode[] nodes, double input)
    {
        string nodeError = ValidateNodes(nodes);
        if (nodeError.Length > 0)
        {
            throw new IccProfileValidationException(nodeError);
        }

        if (double.IsNaN(input) || double.IsInfinity(input))
        {
            throw new IccProfileValidationException("Adaptive Gain Curve evaluation input shall be finite.");
        }

        if (input <= nodes[0].Input)
        {
            return nodes[0].Gain;
        }

        if (input >= nodes[nodes.Length - 1].Input)
        {
            return nodes[nodes.Length - 1].Gain;
        }

        int index = 0;
        while (index < nodes.Length - 2 && input > nodes[index + 1].Input)
        {
            index = index + 1;
        }

        IccAdaptiveGainCurveNode first = nodes[index];
        IccAdaptiveGainCurveNode second = nodes[index + 1];
        double width = second.Input - first.Input;
        double amount = (input - first.Input) / width;
        double amountSquared = amount * amount;
        double amountCubed = amountSquared * amount;
        double firstGainWeight = (2.0 * amountCubed) - (3.0 * amountSquared) + 1.0;
        double firstSlopeWeight = amountCubed - (2.0 * amountSquared) + amount;
        double secondGainWeight = (-2.0 * amountCubed) + (3.0 * amountSquared);
        double secondSlopeWeight = amountCubed - amountSquared;
        return (firstGainWeight * first.Gain)
            + (firstSlopeWeight * width * first.Slope)
            + (secondGainWeight * second.Gain)
            + (secondSlopeWeight * width * second.Slope);
    }

    internal static AdaptiveGainCurveDecodeResult DecodeCurve(System.ReadOnlyMemory<byte> encoded)
    {
        AdaptiveGainCurveDecodeResult result = new AdaptiveGainCurveDecodeResult();
        uint encodedCount = ReadUInt32At(encoded, 0);
        if (encodedCount < 2 || encodedCount > MaximumNodeCount)
        {
            result.Error = $"the node count shall be between 2 and {MaximumNodeCount}.";
            return result;
        }

        int nodeCount = (int)encodedCount;
        int expectedSize = 4 + (nodeCount * 12);
        if (encoded.Length != expectedSize)
        {
            result.Error = $"{nodeCount} nodes require exactly {expectedSize} bytes, but the curve contains {encoded.Length}.";
            return result;
        }

        result.Nodes = new IccAdaptiveGainCurveNode[nodeCount];
        int index = 0;
        while (index < nodeCount)
        {
            int offset = 4 + (index * 12);
            if (!IsValidFloat32At(encoded, offset) || !IsValidFloat32At(encoded, offset + 4) || !IsValidFloat32At(encoded, offset + 8))
            {
                result.Error = $"node {index} contains a value that is not a finite normal float32Number.";
                return result;
            }

            IccAdaptiveGainCurveNode node = new IccAdaptiveGainCurveNode();
            node.Input = DecodeFloat32At(encoded, offset);
            node.Gain = DecodeFloat32At(encoded, offset + 4);
            node.Slope = DecodeFloat32At(encoded, offset + 8);
            result.Nodes[index] = node;
            index = index + 1;
        }

        result.Error = ValidateNodes(result.Nodes);
        return result;
    }

    internal static byte[] SerializeCurve(IccAdaptiveGainCurveNode[] nodes)
    {
        byte[] encoded = new byte[4 + (nodes.Length * 12)];
        BigEndianWriter writer = new BigEndianWriter(encoded, 0);
        writer.WriteUInt32((uint)nodes.Length);
        int index = 0;
        while (index < nodes.Length)
        {
            WriteFloat32Value(writer, nodes[index].Input);
            WriteFloat32Value(writer, nodes[index].Gain);
            WriteFloat32Value(writer, nodes[index].Slope);
            index = index + 1;
        }

        return encoded;
    }

    internal static string ValidateNodes(IccAdaptiveGainCurveNode[] nodes)
    {
        if (nodes == null || nodes.Length < 2 || nodes.Length > MaximumNodeCount)
        {
            return $"the node count shall be between 2 and {MaximumNodeCount}.";
        }

        int index = 0;
        while (index < nodes.Length)
        {
            IccAdaptiveGainCurveNode node = nodes[index];
            if (node == null || double.IsNaN(node.Input) || double.IsInfinity(node.Input) || double.IsNaN(node.Gain) || double.IsInfinity(node.Gain) || double.IsNaN(node.Slope) || double.IsInfinity(node.Slope))
            {
                return $"node {index} values shall be finite.";
            }

            if (index > 0 && node.Input <= nodes[index - 1].Input)
            {
                return "successive node input coordinates shall be strictly increasing.";
            }

            index = index + 1;
        }

        return string.Empty;
    }

    internal static string ValidateContext(TagDecodeState state, bool isImageSpecific)
    {
        if (!state.HasProfileContext)
        {
            return string.Empty;
        }

        if (state.TagSignature != IccTagSignatures.AdaptiveGainCurve)
        {
            return "The adaptiveGainCurveType is permitted only for the ADGC tag.";
        }

        return ValidateWriteContext(state.ProfileClass, state.DataColorSpace, isImageSpecific, state.IsEmbedded, state.RequiresEmbeddedData);
    }

    internal static string ValidateWriteContext(IccProfileClass profileClass, IccColorSpaceKind dataColorSpace, bool isImageSpecific, bool isEmbedded, bool requiresEmbeddedData)
    {
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display)
        {
            return "The ADGC tag is permitted only in Input or Display profiles.";
        }

        if (dataColorSpace != IccColorSpaceKind.Rgb)
        {
            return "The ADGC tag is permitted only for the RGB profile data colour space.";
        }

        if (isImageSpecific && (!isEmbedded || !requiresEmbeddedData))
        {
            return "An image-specific ADGC tag requires the profile to be embedded and dependent on its embedded colour data.";
        }

        return string.Empty;
    }

    internal static IccAdaptiveGainCurveNode[]? FindDecodedCurve(System.Collections.Generic.List<MultiProcessElementPosition> positions, System.Collections.Generic.List<IccAdaptiveGainCurveNode[]> curves, MultiProcessElementPosition requested)
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

    internal static int FindStoredBlockOffset(System.Collections.Generic.List<AdaptiveGainCurveStoredBlock> blocks, byte[] bytes)
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

    internal static bool HasNonZeroByte(byte[] bytes)
    {
        int index = 0;
        while (index < bytes.Length)
        {
            if (bytes[index] != 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool HasZeroBytes(System.ReadOnlyMemory<byte> bytes, int offset, int count)
    {
        int index = 0;
        while (index < count)
        {
            if (bytes.Span[offset + index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsValidFloat32At(System.ReadOnlyMemory<byte> encoded, int offset)
    {
        uint bits = ReadUInt32At(encoded, offset);
        uint exponent = bits & 0x7f800000;
        uint fraction = bits & 0x007fffff;
        return exponent != 0x7f800000 && (exponent != 0 || fraction == 0);
    }

    internal static bool HasValidHeaderFloat32Values(System.ReadOnlyMemory<byte> payload)
    {
        int[] offsets = new int[] { 28, 32, 36, 40, 44, 48, 52, 56, 60, 64, 68, 88, 92, 96 };
        int index = 0;
        while (index < offsets.Length)
        {
            if (!IsValidFloat32At(payload, offsets[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool HasValidFunctionDataFloat32Values(byte[] functionData)
    {
        System.ReadOnlyMemory<byte> encoded = functionData;
        int offset = 0;
        while (offset <= 32)
        {
            if (!IsValidFloat32At(encoded, offset))
            {
                return false;
            }

            offset = offset + 4;
        }

        return true;
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

    internal static void CompletePositionInvalid(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, int index, string detail)
    {
        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Adaptive gain curve position {index} {detail}.");
    }
}
