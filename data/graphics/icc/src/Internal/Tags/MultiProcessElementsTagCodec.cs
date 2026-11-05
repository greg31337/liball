namespace icc;

internal class MultiProcessElementPosition
{
    internal int Offset;
    internal int Size;
}

internal class MultiProcessStoredBlock
{
    internal int Offset;
    internal byte[] Bytes = new byte[0];
}

internal class MultiProcessElementDecodeResult
{
    internal string Error = string.Empty;
    internal IccProcessingElement Element = new IccProcessingElement();
}

internal static class MultiProcessElementsTagCodec
{
    internal static void DecodeMultiProcessElements(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (state.ProfileVersion != IccVersionKind.Version4 && state.ProfileVersion != IccVersionKind.Version5)
        {
            UnknownTagCodec.DecodeFallbackPayload(state, payload, typeSignature, true, TagCodecDispatcher.DescribeProfileGeneration(state.ProfileVersion));
            return;
        }

        if (payload.Length < 24)
        {
            CompleteInvalid(state, payload, typeSignature, $"A multiProcessElementsType payload requires at least 24 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            CompleteInvalid(state, payload, typeSignature, "The multiProcessElementsType reserved bytes shall be zero.");
            return;
        }

        int inputChannels = ReadUInt16At(payload, 8, state.AbsolutePayloadOffset);
        int outputChannels = ReadUInt16At(payload, 10, state.AbsolutePayloadOffset);
        if (inputChannels < 1 || outputChannels < 1)
        {
            CompleteInvalid(state, payload, typeSignature, "The multiProcessElementsType input and output channel counts shall each be at least one.");
            return;
        }

        string contextError = ValidateProfileContext(state, inputChannels, outputChannels);
        if (contextError.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, contextError);
            return;
        }

        uint encodedCount = ReadUInt32At(payload, 12, state.AbsolutePayloadOffset);
        if (encodedCount == 0 || encodedCount > int.MaxValue)
        {
            CompleteInvalid(state, payload, typeSignature, "A multiProcessElementsType shall contain at least one processing element and its count shall fit the supported managed range.");
            return;
        }

        long dataStartValue = 16L + ((long)encodedCount * 8L);
        if (dataStartValue > payload.Length || dataStartValue > int.MaxValue)
        {
            CompleteInvalid(state, payload, typeSignature, "The multiProcessElementsType position table extends beyond the payload.");
            return;
        }

        int elementCount = (int)encodedCount;
        int dataStart = (int)dataStartValue;
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[elementCount];
        BigEndianReader positionReader = new BigEndianReader(payload.Slice(16, elementCount * 8), state.AbsolutePayloadOffset + 16);
        int index = 0;
        while (index < elementCount)
        {
            uint encodedOffset = positionReader.ReadUInt32();
            uint encodedSize = positionReader.ReadUInt32();
            if (encodedOffset > int.MaxValue || encodedSize > int.MaxValue)
            {
                CompleteInvalid(state, payload, typeSignature, $"Processing element position {index} exceeds the supported managed range.");
                return;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = (int)encodedOffset;
            position.Size = (int)encodedSize;
            if (position.Offset < dataStart || position.Offset % 4 != 0 || position.Size < 12 || position.Size > payload.Length - position.Offset)
            {
                CompleteInvalid(state, payload, typeSignature, $"Processing element position {index} shall identify at least 12 bytes at a four-byte-aligned offset beyond the position table.");
                return;
            }

            if (position.Size > int.MaxValue - 3)
            {
                CompleteInvalid(state, payload, typeSignature, $"Processing element position {index} exceeds the supported aligned size.");
                return;
            }

            int paddedSize = AlignFour(position.Size);
            if (paddedSize > payload.Length - position.Offset || !HasZeroBytes(payload, position.Offset + position.Size, paddedSize - position.Size))
            {
                CompleteInvalid(state, payload, typeSignature, $"Processing element position {index} has missing or non-zero alignment padding.");
                return;
            }

            positions[index] = position;
            index = index + 1;
        }

        string topologyError = ValidatePositionTopology(positions, dataStart, payload.Length);
        if (topologyError.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, topologyError);
            return;
        }

        IccProcessingElement[] elements = new IccProcessingElement[elementCount];
        System.Collections.Generic.List<MultiProcessElementPosition> decodedPositions = new System.Collections.Generic.List<MultiProcessElementPosition>();
        System.Collections.Generic.List<IccProcessingElement> decodedElements = new System.Collections.Generic.List<IccProcessingElement>();
        int unavailableCount = 0;
        int futureCount = 0;
        int acsCount = 0;
        index = 0;
        while (index < elementCount)
        {
            MultiProcessElementPosition position = positions[index];
            IccProcessingElement? shared = FindDecodedElement(decodedPositions, decodedElements, position);
            IccProcessingElement element;
            if (shared == null)
            {
                MultiProcessElementDecodeResult elementResult = DecodeElementHeader(payload, position, state.AbsolutePayloadOffset, state, state.CurrentNestingDepth);
                if (elementResult.Error.Length > 0)
                {
                    CompleteInvalid(state, payload, typeSignature, $"Processing element {index}: {elementResult.Error}");
                    return;
                }

                element = elementResult.Element;
                decodedPositions.Add(position);
                decodedElements.Add(element);
            }
            else
            {
                element = shared;
            }

            if (element.Kind == IccProcessingElementKind.Unknown)
            {
                futureCount = futureCount + 1;
                unavailableCount = unavailableCount + 1;
            }
            else if (element.Kind == IccProcessingElementKind.BeginAcs || element.Kind == IccProcessingElementKind.EndAcs)
            {
                acsCount = acsCount + 1;
            }
            else if (state.ProfileVersion == IccVersionKind.Version5 && (!Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(element.Kind)
                || (element.Kind == IccProcessingElementKind.Calculator && (element.Calculator == null || !element.Calculator.IsExecutable))))
            {
                unavailableCount = unavailableCount + 1;
            }

            elements[index] = element;
            index = index + 1;
        }

        string channelError = ValidateChannelContinuity(elements, inputChannels, outputChannels);
        if (channelError.Length > 0)
        {
            CompleteInvalid(state, payload, typeSignature, channelError);
            return;
        }

        IccMultiProcessElements multiProcessElements = new IccMultiProcessElements();
        multiProcessElements.InputChannels = inputChannels;
        multiProcessElements.OutputChannels = outputChannels;
        multiProcessElements.Elements = elements;
        multiProcessElements.InputRanges = CloneRanges(elements[0].InputRanges);
        multiProcessElements.OutputRanges = CloneRanges(elements[elementCount - 1].OutputRanges);
        multiProcessElements.IsExecutable = unavailableCount == 0;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.MultiProcessElements);
        data.MultiProcessElements = multiProcessElements;
        string message;
        if (futureCount > 0)
        {
            message = $"Decoded multiProcessElementsType with {elementCount} processing elements; {futureCount} unknown or future element references were retained losslessly, so the pipeline is not executable.";
        }
        else if (unavailableCount > 0)
        {
            message = $"Decoded ICC.2 multiProcessElementsType with {elementCount} processing elements; {unavailableCount} recognized element references were retained losslessly until their ICC.2 body codecs are available, so the pipeline is not executable.";
        }
        else if (acsCount > 0)
        {
            message = $"Decoded multiProcessElementsType with {elementCount} processing elements; {acsCount} ACS element references were retained and treated as pass-through stages.";
        }
        else
        {
            message = $"Decoded multiProcessElementsType with {elementCount} processing elements.";
        }

        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static MultiProcessElementDecodeResult DecodeElementHeader(System.ReadOnlyMemory<byte> payload, MultiProcessElementPosition position, long absoluteOffset, TagDecodeState state, int nestingDepth)
    {
        MultiProcessElementDecodeResult result = new MultiProcessElementDecodeResult();
        IccProcessingElement element = result.Element;
        System.ReadOnlyMemory<byte> encoded = payload.Slice(position.Offset, position.Size);
        uint signature = ReadUInt32At(encoded, 0, absoluteOffset + position.Offset);
        bool hasInterpolationHint = signature == IccProcessingElementSignatures.ExtendedClut
            || signature == IccProcessingElementSignatures.EmissionClut
            || signature == IccProcessingElementSignatures.ReflectanceClut;
        int reservedCount = state.ProfileVersion == IccVersionKind.Version5 && hasInterpolationHint ? 3 : 4;
        if (!HasZeroBytes(encoded, 4, reservedCount))
        {
            result.Error = "reserved bytes shall be zero.";
            return result;
        }

        int inputChannels = ReadUInt16At(encoded, 8, absoluteOffset + position.Offset);
        int outputChannels = ReadUInt16At(encoded, 10, absoluteOffset + position.Offset);
        if (inputChannels < 1 || outputChannels < 1)
        {
            result.Error = "input and output channel counts shall each be at least one.";
            return result;
        }

        element.Signature = signature;
        element.Kind = state.ProfileVersion == IccVersionKind.Version5 ? IdentifyVersion5Kind(signature) : IdentifyVersion4Kind(signature);
        element.InputChannels = inputChannels;
        element.OutputChannels = outputChannels;
        element.InputRanges = state.ProfileVersion == IccVersionKind.Version4 ? CreateInputRanges(element.Kind, inputChannels) : CreateFullFloatRanges(inputChannels);
        element.OutputRanges = CreateFullFloatRanges(outputChannels);
        element.RawData.TypeSignature = signature;
        element.RawData.HasTypeSignature = true;
        element.RawData.Bytes = encoded.ToArray();
        string bodyError = string.Empty;
        if (state.ProfileVersion == IccVersionKind.Version4)
        {
            bodyError = MultiProcessElementTagCodec.DecodeKnownElement(encoded, element, absoluteOffset + position.Offset);
        }
        else if (Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(element.Kind))
        {
            bodyError = Icc2ProcessingElementTagCodec.DecodeKnownElement(encoded, element, absoluteOffset + position.Offset, state, nestingDepth);
        }
        if (bodyError.Length > 0)
        {
            result.Error = bodyError;
        }

        return result;
    }

    internal static IccProcessingElementKind IdentifyVersion4Kind(uint signature)
    {
        switch (signature)
        {
            case IccProcessingElementSignatures.CurveSet:
                return IccProcessingElementKind.CurveSet;
            case IccProcessingElementSignatures.Matrix:
                return IccProcessingElementKind.Matrix;
            case IccProcessingElementSignatures.Clut:
                return IccProcessingElementKind.Clut;
            case IccProcessingElementSignatures.BeginAcs:
                return IccProcessingElementKind.BeginAcs;
            case IccProcessingElementSignatures.EndAcs:
                return IccProcessingElementKind.EndAcs;
            default:
                return IccProcessingElementKind.Unknown;
        }
    }

    internal static IccProcessingElementKind IdentifyVersion5Kind(uint signature)
    {
        switch (signature)
        {
            case IccProcessingElementSignatures.CurveSet:
                return IccProcessingElementKind.CurveSet;
            case IccProcessingElementSignatures.Matrix:
                return IccProcessingElementKind.Matrix;
            case IccProcessingElementSignatures.Clut:
                return IccProcessingElementKind.Clut;
            case IccProcessingElementSignatures.BeginAcs:
                return IccProcessingElementKind.BeginAcs;
            case IccProcessingElementSignatures.EndAcs:
                return IccProcessingElementKind.EndAcs;
            case IccProcessingElementSignatures.Calculator:
                return IccProcessingElementKind.Calculator;
            case IccProcessingElementSignatures.ExtendedClut:
                return IccProcessingElementKind.ExtendedClut;
            case IccProcessingElementSignatures.SparseMatrix:
                return IccProcessingElementKind.SparseMatrix;
            case IccProcessingElementSignatures.TintArray:
                return IccProcessingElementKind.TintArray;
            case IccProcessingElementSignatures.ToneMap:
                return IccProcessingElementKind.ToneMap;
            case IccProcessingElementSignatures.XyzToJab:
                return IccProcessingElementKind.XyzToJab;
            case IccProcessingElementSignatures.JabToXyz:
                return IccProcessingElementKind.JabToXyz;
            case IccProcessingElementSignatures.EmissionClut:
                return IccProcessingElementKind.EmissionClut;
            case IccProcessingElementSignatures.EmissionMatrix:
                return IccProcessingElementKind.EmissionMatrix;
            case IccProcessingElementSignatures.EmissionObserver:
                return IccProcessingElementKind.EmissionObserver;
            case IccProcessingElementSignatures.InverseEmissionMatrix:
                return IccProcessingElementKind.InverseEmissionMatrix;
            case IccProcessingElementSignatures.ReflectanceClut:
                return IccProcessingElementKind.ReflectanceClut;
            case IccProcessingElementSignatures.ReflectanceObserver:
                return IccProcessingElementKind.ReflectanceObserver;
            default:
                return IccProcessingElementKind.Unknown;
        }
    }

    internal static string ValidateProfileContext(TagDecodeState state, int inputChannels, int outputChannels)
    {
        if (!state.HasProfileContext || state.ProfileVersion != IccVersionKind.Version5)
        {
            return string.Empty;
        }

        int dataChannels = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
        int pcsChannels = ColorSpaceChannelCount.Decode(state.ProfileConnectionSpaceSignature);
        int spectralChannels = (int)(state.SpectralPcsSignature & 0x0000ffff);
        int materialChannels = (int)(state.MaterialConnectionSpaceSignature & 0x0000ffff);
        int expectedInput = 0;
        int expectedOutput = 0;

        if (IsIntentTag(state.TagSignature, IccTagSignatures.AToB0))
        {
            expectedInput = dataChannels;
            expectedOutput = pcsChannels;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.BToA0))
        {
            expectedInput = pcsChannels;
            expectedOutput = dataChannels;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.DToB0))
        {
            expectedInput = dataChannels;
            expectedOutput = spectralChannels;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.BToD0))
        {
            expectedInput = spectralChannels;
            expectedOutput = dataChannels;
        }
        else if (state.TagSignature == IccTagSignatures.AToM0)
        {
            expectedInput = dataChannels;
            expectedOutput = materialChannels;
        }
        else if (state.TagSignature == IccTagSignatures.MToA0)
        {
            expectedInput = materialChannels;
            expectedOutput = dataChannels;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.MToB0))
        {
            expectedInput = materialChannels;
            expectedOutput = pcsChannels;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.MToS0))
        {
            expectedInput = materialChannels;
            expectedOutput = spectralChannels;
        }
        else if (state.TagSignature == IccTagSignatures.CustomToStandardPcc || state.TagSignature == IccTagSignatures.StandardToCustomPcc)
        {
            expectedInput = 3;
            expectedOutput = 3;
        }
        else if (IsIntentTag(state.TagSignature, IccTagSignatures.HdrToSdr0))
        {
            expectedInput = pcsChannels;
            expectedOutput = pcsChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfAToB0) || IsAngularTransformTag(state.TagSignature, IccTagSignatures.DirectionalAToB0))
        {
            expectedInput = AddGeometryChannels(dataChannels);
            expectedOutput = pcsChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfBToA0) || IsAngularTransformTag(state.TagSignature, IccTagSignatures.DirectionalBToA0))
        {
            expectedInput = AddGeometryChannels(pcsChannels);
            expectedOutput = dataChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfDToB0) || IsAngularTransformTag(state.TagSignature, IccTagSignatures.DirectionalDToB0))
        {
            expectedInput = AddGeometryChannels(dataChannels);
            expectedOutput = spectralChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfBToD0) || IsAngularTransformTag(state.TagSignature, IccTagSignatures.DirectionalBToD0))
        {
            expectedInput = AddGeometryChannels(spectralChannels);
            expectedOutput = dataChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfMToB0))
        {
            expectedInput = AddGeometryChannels(materialChannels);
            expectedOutput = pcsChannels;
        }
        else if (IsAngularTransformTag(state.TagSignature, IccTagSignatures.BrdfMToS0))
        {
            expectedInput = AddGeometryChannels(materialChannels);
            expectedOutput = spectralChannels;
        }
        else
        {
            return string.Empty;
        }

        if (expectedInput < 1 || expectedOutput < 1)
        {
            return $"ICC.2 transform tag '{FourCcCodec.Format(state.TagSignature)}' requires channel information that is absent from the profile header.";
        }

        if (inputChannels != expectedInput || outputChannels != expectedOutput)
        {
            return $"ICC.2 transform tag '{FourCcCodec.Format(state.TagSignature)}' requires {expectedInput} input and {expectedOutput} output channels, but the multiProcessElementsType declares {inputChannels} input and {outputChannels} output channels.";
        }

        return string.Empty;
    }

    internal static bool IsContextualTransformTag(uint signature)
    {
        return IsIntentTag(signature, IccTagSignatures.AToB0)
            || IsIntentTag(signature, IccTagSignatures.BToA0)
            || IsIntentTag(signature, IccTagSignatures.DToB0)
            || IsIntentTag(signature, IccTagSignatures.BToD0)
            || signature == IccTagSignatures.AToM0
            || signature == IccTagSignatures.MToA0
            || IsIntentTag(signature, IccTagSignatures.MToB0)
            || IsIntentTag(signature, IccTagSignatures.MToS0)
            || IsIntentTag(signature, IccTagSignatures.HdrToSdr0)
            || signature == IccTagSignatures.CustomToStandardPcc
            || signature == IccTagSignatures.StandardToCustomPcc
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfAToB0)
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfBToA0)
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfBToD0)
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfDToB0)
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfMToB0)
            || IsAngularTransformTag(signature, IccTagSignatures.BrdfMToS0)
            || IsAngularTransformTag(signature, IccTagSignatures.DirectionalAToB0)
            || IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToA0)
            || IsAngularTransformTag(signature, IccTagSignatures.DirectionalBToD0)
            || IsAngularTransformTag(signature, IccTagSignatures.DirectionalDToB0);
    }

    internal static bool IsIntentTag(uint signature, uint zeroSignature)
    {
        return signature >= zeroSignature && signature <= zeroSignature + 3;
    }

    internal static bool IsAngularTransformTag(uint signature, uint zeroSignature)
    {
        return IsIntentTag(signature, zeroSignature);
    }

    internal static int AddGeometryChannels(int channelCount)
    {
        if (channelCount < 1 || channelCount > ushort.MaxValue - 4)
        {
            return 0;
        }

        return channelCount + 4;
    }

    internal static IccEncodingRange[] CreateInputRanges(IccProcessingElementKind kind, int count)
    {
        if (kind == IccProcessingElementKind.Clut)
        {
            return CreateNormalizedRanges(count);
        }

        return CreateFullFloatRanges(count);
    }

    internal static IccEncodingRange[] CreateNormalizedRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            ranges[index] = new IccEncodingRange();
            index = index + 1;
        }

        return ranges;
    }

    internal static IccEncodingRange[] CreateFullFloatRanges(int count)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[count];
        int index = 0;
        while (index < count)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = float.MinValue;
            range.Maximum = float.MaxValue;
            ranges[index] = range;
            index = index + 1;
        }

        return ranges;
    }

    internal static IccEncodingRange[] CloneRanges(IccEncodingRange[] source)
    {
        IccEncodingRange[] ranges = new IccEncodingRange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            IccEncodingRange range = new IccEncodingRange();
            range.Minimum = source[index].Minimum;
            range.Maximum = source[index].Maximum;
            ranges[index] = range;
            index = index + 1;
        }

        return ranges;
    }

    internal static IccProcessingElement? FindDecodedElement(System.Collections.Generic.List<MultiProcessElementPosition> positions, System.Collections.Generic.List<IccProcessingElement> elements, MultiProcessElementPosition requested)
    {
        int index = 0;
        while (index < positions.Count)
        {
            if (positions[index].Offset == requested.Offset && positions[index].Size == requested.Size)
            {
                return elements[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal static string ValidatePositionTopology(MultiProcessElementPosition[] positions, int dataStart, int payloadLength)
    {
        System.Collections.Generic.List<MultiProcessElementPosition> unique = new System.Collections.Generic.List<MultiProcessElementPosition>();
        int index = 0;
        while (index < positions.Length)
        {
            MultiProcessElementPosition candidate = positions[index];
            bool duplicate = false;
            int uniqueIndex = 0;
            while (uniqueIndex < unique.Count)
            {
                MultiProcessElementPosition existing = unique[uniqueIndex];
                if (candidate.Offset == existing.Offset)
                {
                    if (candidate.Size != existing.Size)
                    {
                        return "Shared processing-element offsets shall declare the same element size.";
                    }

                    duplicate = true;
                }
                else
                {
                    int candidateEnd = candidate.Offset + AlignFour(candidate.Size);
                    int existingEnd = existing.Offset + AlignFour(existing.Size);
                    if (candidate.Offset < existingEnd && existing.Offset < candidateEnd)
                    {
                        return "The multiProcessElementsType processing elements overlap illegally.";
                    }
                }

                uniqueIndex = uniqueIndex + 1;
            }

            if (!duplicate)
            {
                unique.Add(candidate);
            }

            index = index + 1;
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
                MultiProcessElementPosition swap = unique[first];
                unique[first] = unique[smallest];
                unique[smallest] = swap;
            }

            first = first + 1;
        }

        int expectedOffset = dataStart;
        index = 0;
        while (index < unique.Count)
        {
            if (unique[index].Offset != expectedOffset)
            {
                return $"The multiProcessElementsType contains a gap or hidden unreferenced data at offset {expectedOffset}.";
            }

            expectedOffset = expectedOffset + AlignFour(unique[index].Size);
            index = index + 1;
        }

        if (expectedOffset != payloadLength)
        {
            return $"The multiProcessElementsType contains trailing or unreferenced data after offset {expectedOffset}.";
        }

        return string.Empty;
    }

    internal static string ValidateChannelContinuity(IccProcessingElement[] elements, int inputChannels, int outputChannels)
    {
        if (elements[0].InputChannels != inputChannels)
        {
            return $"The first processing element has {elements[0].InputChannels} input channels, but the containing multiProcessElementsType declares {inputChannels}.";
        }

        int index = 1;
        while (index < elements.Length)
        {
            if (elements[index].InputChannels != elements[index - 1].OutputChannels)
            {
                return $"Processing element {index} has {elements[index].InputChannels} input channels, but the previous element has {elements[index - 1].OutputChannels} output channels.";
            }

            index = index + 1;
        }

        if (elements[elements.Length - 1].OutputChannels != outputChannels)
        {
            return $"The last processing element has {elements[elements.Length - 1].OutputChannels} output channels, but the containing multiProcessElementsType declares {outputChannels}.";
        }

        return string.Empty;
    }

    internal static byte[] SerializeMultiProcessElements(IccMultiProcessElements multiProcessElements)
    {
        return SerializeMultiProcessElementsForVersion(multiProcessElements, IccVersionKind.Version4);
    }

    internal static byte[] SerializeMultiProcessElementsVersion5(IccMultiProcessElements multiProcessElements)
    {
        return SerializeMultiProcessElementsForVersion(multiProcessElements, IccVersionKind.Version5);
    }

    internal static byte[] SerializeMultiProcessElementsForVersion(IccMultiProcessElements multiProcessElements, IccVersionKind version)
    {
        if (multiProcessElements == null || multiProcessElements.Elements == null || multiProcessElements.InputRanges == null || multiProcessElements.OutputRanges == null)
        {
            throw new IccProfileWriteException("Multi-process-elements data, ranges, and elements cannot be null.");
        }

        if (multiProcessElements.InputChannels < 1 || multiProcessElements.InputChannels > ushort.MaxValue || multiProcessElements.OutputChannels < 1 || multiProcessElements.OutputChannels > ushort.MaxValue)
        {
            throw new IccProfileWriteException("Multi-process-elements input and output channel counts shall each fit a non-zero unsigned 16-bit value.");
        }

        if (multiProcessElements.Elements.Length < 1)
        {
            throw new IccProfileWriteException("A multiProcessElementsType shall contain at least one processing element.");
        }

        long dataStartValue = 16L + ((long)multiProcessElements.Elements.Length * 8L);
        if (dataStartValue > int.MaxValue)
        {
            throw new IccProfileWriteException("The multiProcessElementsType position table exceeds the supported managed size.");
        }

        int dataStart = (int)dataStartValue;
        MultiProcessElementPosition[] positions = new MultiProcessElementPosition[multiProcessElements.Elements.Length];
        System.Collections.Generic.List<MultiProcessStoredBlock> blocks = new System.Collections.Generic.List<MultiProcessStoredBlock>();
        int nextOffset = dataStart;
        int index = 0;
        while (index < multiProcessElements.Elements.Length)
        {
            IccProcessingElement element = multiProcessElements.Elements[index];
            byte[] bytes = SerializeElementForVersion(element, index, version);
            int blockOffset = FindWriteBlockOffset(blocks, bytes);
            if (blockOffset < 0)
            {
                if (bytes.Length > int.MaxValue - 3)
                {
                    throw new IccProfileWriteException($"Processing element {index} exceeds the supported aligned size.");
                }

                int paddedSize = AlignFour(bytes.Length);
                if (paddedSize > int.MaxValue - nextOffset)
                {
                    throw new IccProfileWriteException("The multiProcessElementsType payload exceeds the supported managed size.");
                }

                MultiProcessStoredBlock block = new MultiProcessStoredBlock();
                block.Offset = nextOffset;
                block.Bytes = bytes;
                blocks.Add(block);
                blockOffset = nextOffset;
                nextOffset = nextOffset + paddedSize;
            }

            MultiProcessElementPosition position = new MultiProcessElementPosition();
            position.Offset = blockOffset;
            position.Size = bytes.Length;
            positions[index] = position;
            index = index + 1;
        }

        string channelError = ValidateChannelContinuity(multiProcessElements.Elements, multiProcessElements.InputChannels, multiProcessElements.OutputChannels);
        if (channelError.Length > 0)
        {
            throw new IccProfileWriteException(channelError);
        }

        byte[] payload = new byte[nextOffset];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.MultiProcessElements);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)multiProcessElements.InputChannels);
        writer.WriteUInt16((ushort)multiProcessElements.OutputChannels);
        writer.WriteUInt32((uint)multiProcessElements.Elements.Length);
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
            MultiProcessStoredBlock block = blocks[index];
            new System.ReadOnlySpan<byte>(block.Bytes).CopyTo(new System.Span<byte>(payload, block.Offset, block.Bytes.Length));
            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeElementForVersion(IccProcessingElement element, int index, IccVersionKind version)
    {
        if (element == null)
        {
            throw new IccProfileWriteException($"Processing element {index} cannot be null.");
        }

        if (version == IccVersionKind.Version4)
        {
            return MultiProcessElementTagCodec.SerializeKnownElement(element, index);
        }

        if (version != IccVersionKind.Version5)
        {
            throw new IccProfileWriteException("A multiProcessElementsType can be written only for ICC.1 version 4 or ICC.2 version 5.");
        }

        if (Icc2ProcessingElementTagCodec.IsBodyCodecAvailable(element.Kind))
        {
            return Icc2ProcessingElementTagCodec.SerializeKnownElement(element, index);
        }

        ValidateElementForWriteVersion5(element, index);
        return element.RawData.Bytes;
    }

    internal static void ValidateElementForWrite(IccProcessingElement element, int index)
    {
        ValidateElementForWriteVersion(element, index, IccVersionKind.Version4);
    }

    internal static void ValidateElementForWriteVersion5(IccProcessingElement element, int index)
    {
        ValidateElementForWriteVersion(element, index, IccVersionKind.Version5);
    }

    internal static void ValidateElementForWriteVersion(IccProcessingElement element, int index, IccVersionKind version)
    {
        if (element == null || element.RawData == null || element.RawData.Bytes == null || element.RawData.Bytes.Length < 12)
        {
            throw new IccProfileWriteException($"Processing element {index} shall contain a complete raw element header.");
        }

        byte[] bytes = element.RawData.Bytes;
        System.ReadOnlyMemory<byte> encoded = new System.ReadOnlyMemory<byte>(bytes);
        uint signature = ReadUInt32At(encoded, 0, 0);
        int inputChannels = ReadUInt16At(encoded, 8, 0);
        int outputChannels = ReadUInt16At(encoded, 10, 0);
        if (!HasZeroBytes(encoded, 4, 4))
        {
            throw new IccProfileWriteException($"Processing element {index} reserved bytes shall be zero.");
        }

        if (inputChannels < 1 || outputChannels < 1 || element.InputChannels != inputChannels || element.OutputChannels != outputChannels)
        {
            throw new IccProfileWriteException($"Processing element {index} raw and modeled channel counts shall match and be non-zero.");
        }

        if (element.Signature != signature || (element.RawData.HasTypeSignature && element.RawData.TypeSignature != signature))
        {
            throw new IccProfileWriteException($"Processing element {index} raw and modeled signatures shall match.");
        }

        IccProcessingElementKind expectedKind = version == IccVersionKind.Version5 ? IdentifyVersion5Kind(signature) : IdentifyVersion4Kind(signature);
        if (element.Kind != IccProcessingElementKind.Unknown && element.Kind != expectedKind)
        {
            throw new IccProfileWriteException($"Processing element {index} kind does not match its signature.");
        }
    }

    internal static int FindWriteBlockOffset(System.Collections.Generic.List<MultiProcessStoredBlock> blocks, byte[] bytes)
    {
        int index = 0;
        while (index < blocks.Count)
        {
            if (BytesEqual(blocks[index].Bytes, bytes))
            {
                return blocks[index].Offset;
            }

            index = index + 1;
        }

        return -1;
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

    internal static void CompleteInvalid(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature, string message)
    {
        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, message);
    }
}
