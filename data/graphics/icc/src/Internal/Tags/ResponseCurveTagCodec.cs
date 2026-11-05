namespace icc;

internal class ResponseCurveStructureBounds
{
    internal int Offset;
    internal int Size;
    internal uint MeasurementUnitSignature;
    internal uint[] ResponseCounts;

    internal ResponseCurveStructureBounds()
    {
        Offset = 0;
        Size = 0;
        MeasurementUnitSignature = 0;
        ResponseCounts = new uint[0];
    }
}

internal static class ResponseCurveTagCodec
{
    internal static void DecodeResponseCurveSet16(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A responseCurveSet16Type payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The responseCurveSet16Type reserved bytes shall be zero.");
            return;
        }

        BigEndianReader headerReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        int channelCount = headerReader.ReadUInt16();
        int measurementTypeCount = headerReader.ReadUInt16();
        if (channelCount < 1 || channelCount > IccConstants.MaximumIcc1ChannelCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The responseCurveSet16Type channel count {channelCount} shall be between 1 and {IccConstants.MaximumIcc1ChannelCount}.");
            return;
        }

        if (state.HasProfileContext)
        {
            int expectedChannelCount = ColorSpaceChannelCount.Decode(state.DataColorSpaceSignature);
            if (expectedChannelCount == 0 || expectedChannelCount != channelCount)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The responseCurveSet16Type channel count {channelCount} does not match profile data colour-space signature 0x{state.DataColorSpaceSignature:X8}, which defines {expectedChannelCount} channels.");
                return;
            }
        }

        int maximumMeasurementTypeCount = (payload.Length - 12) / 4;
        if (measurementTypeCount > maximumMeasurementTypeCount)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The responseCurveSet16Type declares {measurementTypeCount} measurement types, but its offset table can contain at most {maximumMeasurementTypeCount}.");
            return;
        }

        int storageOffset = 12 + (measurementTypeCount * 4);
        ResponseCurveStructureBounds[] structures = new ResponseCurveStructureBounds[measurementTypeCount];
        System.Collections.Generic.Dictionary<uint, bool> measurementUnits = new System.Collections.Generic.Dictionary<uint, bool>();
        int index = 0;
        while (index < measurementTypeCount)
        {
            BigEndianReader offsetReader = new BigEndianReader(payload.Slice(12 + (index * 4), 4), state.AbsolutePayloadOffset + 12 + (index * 4));
            uint declaredOffset = offsetReader.ReadUInt32();
            if (declaredOffset > int.MaxValue)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve offset {index} exceeds the supported managed range.");
                return;
            }

            int curveOffset = (int)declaredOffset;
            int fixedStructureSize = 4 + (16 * channelCount);
            if (curveOffset < storageOffset || (curveOffset & 3) != 0 || curveOffset > payload.Length || fixedStructureSize > payload.Length - curveOffset)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve offset {index} does not reference an aligned, bounded curve structure.");
                return;
            }

            ResponseCurveStructureBounds structure = ReadStructureBounds(payload, curveOffset, channelCount);
            if (structure.Size == 0)
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve structure {index} declares more response16Number values than fit in the payload.");
                return;
            }

            if (!IsRegisteredMeasurementUnit(structure.MeasurementUnitSignature))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve structure {index} uses unregistered measurement-unit signature 0x{structure.MeasurementUnitSignature:X8}.");
                return;
            }

            if (measurementUnits.ContainsKey(structure.MeasurementUnitSignature))
            {
                TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve structure {index} duplicates measurement-unit signature 0x{structure.MeasurementUnitSignature:X8}.");
                return;
            }

            measurementUnits.Add(structure.MeasurementUnitSignature, true);
            structures[index] = structure;
            index = index + 1;
        }

        string topologyError = ValidateStructureTopology(payload.Length, structures, storageOffset);
        if (topologyError.Length > 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, topologyError);
            return;
        }

        IccResponseCurve[] curves = new IccResponseCurve[measurementTypeCount];
        index = 0;
        while (index < measurementTypeCount)
        {
            ResponseCurveStructureBounds structure = structures[index];
            IccResponseCurve curve = new IccResponseCurve();
            curve.MeasurementUnitSignature = structure.MeasurementUnitSignature;
            curve.MaximumColorantXyz = new IccXyzNumber[channelCount];
            curve.DeviceCodes = new ushort[channelCount][];
            curve.Measurements = new double[channelCount][];
            int xyzOffset = structure.Offset + 4 + (channelCount * 4);
            BigEndianReader xyzReader = new BigEndianReader(payload.Slice(xyzOffset, channelCount * 12), state.AbsolutePayloadOffset + xyzOffset);
            int channelIndex = 0;
            while (channelIndex < channelCount)
            {
                curve.MaximumColorantXyz[channelIndex] = IccNumberCodec.ReadXyzNumber(xyzReader);
                channelIndex = channelIndex + 1;
            }

            int responseOffset = structure.Offset + 4 + (channelCount * 16);
            channelIndex = 0;
            while (channelIndex < channelCount)
            {
                int responseCount = (int)structure.ResponseCounts[channelIndex];
                ushort[] deviceCodes = new ushort[responseCount];
                double[] measurements = new double[responseCount];
                BigEndianReader responseReader = new BigEndianReader(payload.Slice(responseOffset, responseCount * 8), state.AbsolutePayloadOffset + responseOffset);
                int responseIndex = 0;
                while (responseIndex < responseCount)
                {
                    IccResponse16Number response = IccNumberCodec.ReadResponse16Number(responseReader);
                    if (response.Reserved != 0)
                    {
                        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve structure {index}, channel {channelIndex}, response {responseIndex} has non-zero reserved bytes.");
                        return;
                    }

                    if (responseIndex > 0 && response.DeviceCode <= deviceCodes[responseIndex - 1])
                    {
                        TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Response curve structure {index}, channel {channelIndex} device codes shall be strictly increasing.");
                        return;
                    }

                    deviceCodes[responseIndex] = response.DeviceCode;
                    measurements[responseIndex] = response.Measurement;
                    responseIndex = responseIndex + 1;
                }

                curve.DeviceCodes[channelIndex] = deviceCodes;
                curve.Measurements[channelIndex] = measurements;
                responseOffset = responseOffset + (responseCount * 8);
                channelIndex = channelIndex + 1;
            }

            curves[index] = curve;
            index = index + 1;
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.ResponseCurves);
        data.ResponseCurveChannelCount = channelCount;
        data.ResponseCurves = curves;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded {measurementTypeCount} response measurement types for {channelCount} channels.");
    }

    internal static byte[] SerializeResponseCurveSet16(IccResponseCurve[] curves, uint dataColorSpaceSignature)
    {
        if (curves == null)
        {
            throw new IccProfileWriteException("The response curve collection cannot be null.");
        }

        if (curves.Length > ushort.MaxValue)
        {
            throw new IccProfileWriteException("A responseCurveSet16Type cannot contain more than 65535 measurement types.");
        }

        int channelCount = ColorSpaceChannelCount.Decode(dataColorSpaceSignature);
        if (channelCount < 1 || channelCount > IccConstants.MaximumIcc1ChannelCount)
        {
            throw new IccProfileWriteException($"Data colour-space signature 0x{dataColorSpaceSignature:X8} does not define between 1 and {IccConstants.MaximumIcc1ChannelCount} ICC.1 channels.");
        }

        int[] offsets = new int[curves.Length];
        int[] sizes = new int[curves.Length];
        System.Collections.Generic.Dictionary<uint, bool> measurementUnits = new System.Collections.Generic.Dictionary<uint, bool>();
        long payloadSize = 12L + ((long)curves.Length * 4L);
        int index = 0;
        while (index < curves.Length)
        {
            IccResponseCurve curve = curves[index];
            ValidateCurveForWrite(curve, index, channelCount, measurementUnits);
            long responseCount = 0;
            int channelIndex = 0;
            while (channelIndex < channelCount)
            {
                responseCount = responseCount + curve.DeviceCodes[channelIndex].Length;
                channelIndex = channelIndex + 1;
            }

            long structureSize = 4L + ((long)channelCount * 16L) + (responseCount * 8L);
            if (structureSize > int.MaxValue || payloadSize > int.MaxValue - structureSize)
            {
                throw new IccProfileWriteException($"Response curve structure {index} exceeds the supported managed payload size.");
            }

            offsets[index] = (int)payloadSize;
            sizes[index] = (int)structureSize;
            payloadSize = payloadSize + structureSize;
            index = index + 1;
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ResponseCurveSet16);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)channelCount);
        writer.WriteUInt16((ushort)curves.Length);
        index = 0;
        while (index < offsets.Length)
        {
            writer.WriteUInt32((uint)offsets[index]);
            index = index + 1;
        }

        index = 0;
        while (index < curves.Length)
        {
            IccResponseCurve curve = curves[index];
            writer.WriteUInt32(curve.MeasurementUnitSignature);
            int channelIndex = 0;
            while (channelIndex < channelCount)
            {
                writer.WriteUInt32((uint)curve.DeviceCodes[channelIndex].Length);
                channelIndex = channelIndex + 1;
            }

            channelIndex = 0;
            while (channelIndex < channelCount)
            {
                IccNumberCodec.WriteXyzNumber(writer, curve.MaximumColorantXyz[channelIndex]);
                channelIndex = channelIndex + 1;
            }

            channelIndex = 0;
            while (channelIndex < channelCount)
            {
                int responseIndex = 0;
                while (responseIndex < curve.DeviceCodes[channelIndex].Length)
                {
                    IccResponse16Number response = new IccResponse16Number(curve.DeviceCodes[channelIndex][responseIndex], 0, curve.Measurements[channelIndex][responseIndex]);
                    IccNumberCodec.WriteResponse16Number(writer, response);
                    responseIndex = responseIndex + 1;
                }

                channelIndex = channelIndex + 1;
            }

            if (writer.GetWritePosition() != offsets[index] + sizes[index])
            {
                throw new IccProfileWriteException($"Response curve structure {index} did not produce its preflighted byte size.");
            }

            index = index + 1;
        }

        return payload;
    }

    internal static ResponseCurveStructureBounds ReadStructureBounds(System.ReadOnlyMemory<byte> payload, int offset, int channelCount)
    {
        ResponseCurveStructureBounds structure = new ResponseCurveStructureBounds();
        structure.Offset = offset;
        BigEndianReader reader = new BigEndianReader(payload.Slice(offset, 4 + (channelCount * 4)), offset);
        structure.MeasurementUnitSignature = reader.ReadUInt32();
        structure.ResponseCounts = new uint[channelCount];
        long totalResponseCount = 0;
        int channelIndex = 0;
        while (channelIndex < channelCount)
        {
            uint count = reader.ReadUInt32();
            structure.ResponseCounts[channelIndex] = count;
            totalResponseCount = totalResponseCount + count;
            channelIndex = channelIndex + 1;
        }

        long structureSize = 4L + ((long)channelCount * 16L) + (totalResponseCount * 8L);
        if (structureSize > int.MaxValue || structureSize > payload.Length - offset)
        {
            return structure;
        }

        structure.Size = (int)structureSize;
        return structure;
    }

    internal static string ValidateStructureTopology(int payloadLength, ResponseCurveStructureBounds[] structures, int storageOffset)
    {
        ResponseCurveStructureBounds[] ordered = new ResponseCurveStructureBounds[structures.Length];
        int index = 0;
        while (index < structures.Length)
        {
            ResponseCurveStructureBounds structure = structures[index];
            int insertionIndex = index;
            while (insertionIndex > 0 && ordered[insertionIndex - 1].Offset > structure.Offset)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            ordered[insertionIndex] = structure;
            index = index + 1;
        }

        int expectedOffset = storageOffset;
        index = 0;
        while (index < ordered.Length)
        {
            ResponseCurveStructureBounds structure = ordered[index];
            if (structure.Offset < expectedOffset)
            {
                return $"Response curve structure at byte {structure.Offset} overlaps another curve structure.";
            }

            if (structure.Offset > expectedOffset)
            {
                return $"The responseCurveSet16Type contains an unexpected gap before byte {structure.Offset}.";
            }

            expectedOffset = structure.Offset + structure.Size;
            index = index + 1;
        }

        if (expectedOffset != payloadLength)
        {
            return $"The responseCurveSet16Type contains {payloadLength - expectedOffset} unexpected trailing bytes after its curve structures.";
        }

        return string.Empty;
    }

    internal static void ValidateCurveForWrite(IccResponseCurve curve, int curveIndex, int channelCount, System.Collections.Generic.Dictionary<uint, bool> measurementUnits)
    {
        if (curve == null || curve.MaximumColorantXyz == null || curve.DeviceCodes == null || curve.Measurements == null)
        {
            throw new IccProfileWriteException($"Response curve structure {curveIndex} and its channel arrays cannot be null.");
        }

        if (!IsRegisteredMeasurementUnit(curve.MeasurementUnitSignature))
        {
            throw new IccProfileWriteException($"Response curve structure {curveIndex} uses unregistered measurement-unit signature 0x{curve.MeasurementUnitSignature:X8}.");
        }

        if (measurementUnits.ContainsKey(curve.MeasurementUnitSignature))
        {
            throw new IccProfileWriteException($"Response curve structure {curveIndex} duplicates measurement-unit signature 0x{curve.MeasurementUnitSignature:X8}.");
        }

        measurementUnits.Add(curve.MeasurementUnitSignature, true);
        if (curve.MaximumColorantXyz.Length != channelCount || curve.DeviceCodes.Length != channelCount || curve.Measurements.Length != channelCount)
        {
            throw new IccProfileWriteException($"Response curve structure {curveIndex} does not contain exactly {channelCount} XYZ values and channel response arrays.");
        }

        int channelIndex = 0;
        while (channelIndex < channelCount)
        {
            if (curve.MaximumColorantXyz[channelIndex] == null || curve.DeviceCodes[channelIndex] == null || curve.Measurements[channelIndex] == null)
            {
                throw new IccProfileWriteException($"Response curve structure {curveIndex}, channel {channelIndex} contains a null XYZ value or response array.");
            }

            if (curve.DeviceCodes[channelIndex].Length != curve.Measurements[channelIndex].Length)
            {
                throw new IccProfileWriteException($"Response curve structure {curveIndex}, channel {channelIndex} has different device-code and measurement counts.");
            }

            int responseIndex = 0;
            while (responseIndex < curve.DeviceCodes[channelIndex].Length)
            {
                if (responseIndex > 0 && curve.DeviceCodes[channelIndex][responseIndex] <= curve.DeviceCodes[channelIndex][responseIndex - 1])
                {
                    throw new IccProfileWriteException($"Response curve structure {curveIndex}, channel {channelIndex} device codes shall be strictly increasing.");
                }

                IccNumberCodec.EncodeS15Fixed16(curve.Measurements[channelIndex][responseIndex]);
                responseIndex = responseIndex + 1;
            }

            IccNumberCodec.EncodeS15Fixed16(curve.MaximumColorantXyz[channelIndex].X);
            IccNumberCodec.EncodeS15Fixed16(curve.MaximumColorantXyz[channelIndex].Y);
            IccNumberCodec.EncodeS15Fixed16(curve.MaximumColorantXyz[channelIndex].Z);
            channelIndex = channelIndex + 1;
        }
    }

    internal static bool IsRegisteredMeasurementUnit(uint signature)
    {
        switch (signature)
        {
            case IccMeasurementUnitSignatures.StatusA:
            case IccMeasurementUnitSignatures.StatusE:
            case IccMeasurementUnitSignatures.StatusI:
            case IccMeasurementUnitSignatures.StatusT:
            case IccMeasurementUnitSignatures.StatusM:
            case IccMeasurementUnitSignatures.DinE:
            case IccMeasurementUnitSignatures.DinEPolarized:
            case IccMeasurementUnitSignatures.DinI:
            case IccMeasurementUnitSignatures.DinIPolarized:
                return true;
            default:
                return false;
        }
    }
}
