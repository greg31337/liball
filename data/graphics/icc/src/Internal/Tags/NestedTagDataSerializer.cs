namespace icc;

internal static class NestedTagDataSerializer
{
    internal static byte[] Serialize(IccTagData data, int parentDepth, HierarchicalWriteContext context)
    {
        if (data == null)
        {
            throw new IccProfileWriteException("Nested tag data cannot be null.");
        }

        if (context.Encoded.ContainsKey(data))
        {
            return context.Encoded[data];
        }

        if (context.Active.ContainsKey(data))
        {
            throw new IccProfileWriteException("A hierarchical tag cannot contain a cyclic tag-data graph.");
        }

        context.Active.Add(data, true);
        byte[] encoded = SerializeTyped(data, parentDepth, context);
        if (encoded.Length < 8)
        {
            throw new IccProfileWriteException("A nested tag payload shall contain at least an eight-byte type header.");
        }

        context.Active.Remove(data);
        context.Encoded.Add(data, encoded);
        return encoded;
    }

    internal static byte[] SerializeTyped(IccTagData data, int parentDepth, HierarchicalWriteContext context)
    {
        uint typeSignature = ResolveTypeSignature(data);
        if (data.Kind == IccTagDataKind.Raw && !context.PreserveUnknownData)
        {
            throw new IccProfileWriteException("Nested raw tag data cannot be written when unknown-data preservation is disabled.");
        }

        switch (data.Kind)
        {
            case IccTagDataKind.Raw:
                return SerializeRaw(data.RawData, typeSignature);
            case IccTagDataKind.IntegerArray:
                return SerializeIntegerArray(data.IntegerValues, typeSignature);
            case IccTagDataKind.FixedPointArray:
                return SerializeFixedPointArray(data.FixedPointValues, typeSignature);
            case IccTagDataKind.FloatingPointArray:
                return Icc2ArrayTagCodec.SerializeFloatingPointArrayForMode(typeSignature, data.FloatingPointValues, context.Mode);
            case IccTagDataKind.XyzArray:
                return SerializeXyzArray(data.XyzValues);
            case IccTagDataKind.Signature:
                return SerializeSignatureValue(data.Signature);
            case IccTagDataKind.DateTime:
                return SerializeDateTimeValue(data.DateTime);
            case IccTagDataKind.Binary:
                return SerializeBinaryData(data.BinaryData, typeSignature);
            case IccTagDataKind.Text:
                return SerializeTextDataForMode(data.Text, typeSignature, context.Mode);
            case IccTagDataKind.LocalizedText:
                return LocalizedUnicodeTagCodec.SerializeMultiLocalizedUnicodeForMode(data.LocalizedStrings, context.Mode);
            case IccTagDataKind.Dictionary:
                return DictionaryTagCodec.SerializeDictionaryForMode(data.Metadata, context.Mode);
            case IccTagDataKind.Curve:
                return SerializeCurveData(data, typeSignature);
            case IccTagDataKind.Lut:
                return SerializeLutData(data, typeSignature);
            case IccTagDataKind.MultiProcessElements:
                return SerializeMultiProcessElementsData(data.MultiProcessElements);
            case IccTagDataKind.Measurement:
                return SerializeMeasurementData(data.Measurement);
            case IccTagDataKind.ViewingConditions:
                return SerializeViewingConditionsData(data.ViewingConditions);
            case IccTagDataKind.Chromaticity:
                return SerializeChromaticityData(data.Chromaticity);
            case IccTagDataKind.SpectralViewingConditions:
                return SerializeSpectralViewingConditionsData(data.SpectralViewingConditions);
            case IccTagDataKind.SpectralRange:
                return SerializeSpectralRangeData(data.SpectralRanges);
            case IccTagDataKind.EmbeddedProfile:
                return SerializeEmbeddedProfileData(data.EmbeddedProfile);
            case IccTagDataKind.SparseMatrixArray:
                return SerializeSparseMatrixArrayData(data.SparseMatrixArray);
            case IccTagDataKind.EmbeddedImage:
                return SerializeEmbeddedImageData(data.EmbeddedImage, typeSignature);
            case IccTagDataKind.TagArray:
                return SerializeNestedArray(data.TagArray, parentDepth + 1, context);
            case IccTagDataKind.TagStructure:
                return SerializeNestedStructure(data.TagStructure, parentDepth + 1, context);
            default:
                break;
        }

        if (data.RawData != null && data.RawData.Bytes != null && data.RawData.Bytes.Length >= 8)
        {
            if (!context.PreserveUnknownData)
            {
                throw new IccProfileWriteException("Nested unknown tag data cannot be written when unknown-data preservation is disabled.");
            }

            return SerializeRaw(data.RawData, typeSignature);
        }

        throw new IccProfileWriteException($"Nested tag-data kind {data.Kind} does not have a context-free canonical serializer or retained raw payload.");
    }

    internal static uint ResolveTypeSignature(IccTagData data)
    {
        if (data.RawData != null && data.RawData.HasTypeSignature)
        {
            return data.RawData.TypeSignature;
        }

        if (data.Kind == IccTagDataKind.Text && data.Text != null && data.Text.TypeSignature != 0)
        {
            return data.Text.TypeSignature;
        }

        switch (data.Kind)
        {
            case IccTagDataKind.XyzArray:
                return IccTypeSignatures.Xyz;
            case IccTagDataKind.Signature:
                return IccTypeSignatures.Signature;
            case IccTagDataKind.DateTime:
                return IccTypeSignatures.DateTime;
            case IccTagDataKind.LocalizedText:
                return IccTypeSignatures.MultiLocalizedUnicode;
            case IccTagDataKind.Dictionary:
                return IccTypeSignatures.Dictionary;
            case IccTagDataKind.MultiProcessElements:
                return IccTypeSignatures.MultiProcessElements;
            case IccTagDataKind.Measurement:
                return IccTypeSignatures.Measurement;
            case IccTagDataKind.ViewingConditions:
                return IccTypeSignatures.ViewingConditions;
            case IccTagDataKind.Chromaticity:
                return IccTypeSignatures.Chromaticity;
            case IccTagDataKind.SpectralViewingConditions:
                return IccTypeSignatures.SpectralViewingConditions;
            case IccTagDataKind.SpectralRange:
                return IccTypeSignatures.SpectralRange;
            case IccTagDataKind.EmbeddedProfile:
                return IccTypeSignatures.EmbeddedProfile;
            case IccTagDataKind.SparseMatrixArray:
                return IccTypeSignatures.SparseMatrixArray;
            case IccTagDataKind.TagArray:
                return IccTypeSignatures.TagArray;
            case IccTagDataKind.TagStructure:
                return IccTypeSignatures.TagStructure;
            default:
                break;
        }

        if (data.Kind == IccTagDataKind.Curve && data.Curve != null)
        {
            return data.Curve.Kind == IccCurveKind.Parametric ? IccTypeSignatures.ParametricCurve : IccTypeSignatures.Curve;
        }

        throw new IccProfileWriteException($"Nested tag-data kind {data.Kind} requires an explicit type signature.");
    }

    internal static byte[] SerializeEmbeddedImageData(IccEmbeddedImage? value, uint typeSignature)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested embedded-image data cannot be null.");
        }

        return EmbeddedImageTagCodec.SerializeEmbeddedImage(value, typeSignature);
    }

    internal static byte[] SerializeSpectralRangeData(IccSpectralRanges? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested spectral-range data cannot be null.");
        }

        return SpectralRangeTagCodec.SerializeSpectralRanges(value);
    }

    internal static byte[] SerializeEmbeddedProfileData(IccProfile? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested embedded-profile data cannot be null.");
        }

        return EmbeddedProfileTagCodec.SerializeEmbeddedProfile(value);
    }

    internal static byte[] SerializeRaw(IccRawData raw, uint typeSignature)
    {
        if (raw == null || raw.Bytes == null || raw.Bytes.Length < 8)
        {
            throw new IccProfileWriteException("A raw nested tag payload shall contain at least eight bytes.");
        }

        uint encodedType = HierarchicalTagCodec.ReadUInt32FromArray(raw.Bytes, 0);
        if ((raw.HasTypeSignature && raw.TypeSignature != encodedType) || (typeSignature != 0 && typeSignature != encodedType))
        {
            throw new IccProfileWriteException("The retained raw nested payload does not match its declared type signature.");
        }

        return CloneBytes(raw.Bytes);
    }

    internal static byte[] SerializeIntegerArray(long[] values, uint typeSignature)
    {
        if (values == null)
        {
            throw new IccProfileWriteException("Nested integer-array values cannot be null.");
        }

        int width = ArrayTagCodec.GetIntegerWidth(typeSignature);
        if (typeSignature != IccTypeSignatures.UInt8Array && typeSignature != IccTypeSignatures.UInt16Array
            && typeSignature != IccTypeSignatures.UInt32Array && typeSignature != IccTypeSignatures.UInt64Array)
        {
            throw new IccProfileWriteException("The nested integer-array type signature is not defined.");
        }

        long payloadSize = 8L + ((long)values.Length * width);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The nested integer-array payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(typeSignature);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < values.Length)
        {
            long value = values[index];
            if (typeSignature == IccTypeSignatures.UInt8Array)
            {
                if (value < 0 || value > byte.MaxValue)
                {
                    throw new IccProfileWriteException($"Nested integer-array value {index} does not fit in an unsigned byte.");
                }

                writer.WriteUInt8((byte)value);
            }
            else if (typeSignature == IccTypeSignatures.UInt16Array)
            {
                if (value < 0 || value > ushort.MaxValue)
                {
                    throw new IccProfileWriteException($"Nested integer-array value {index} does not fit in an unsigned 16-bit integer.");
                }

                writer.WriteUInt16((ushort)value);
            }
            else if (typeSignature == IccTypeSignatures.UInt32Array)
            {
                if (value < 0 || value > uint.MaxValue)
                {
                    throw new IccProfileWriteException($"Nested integer-array value {index} does not fit in an unsigned 32-bit integer.");
                }

                writer.WriteUInt32((uint)value);
            }
            else
            {
                writer.WriteInt64(value);
            }

            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeFixedPointArray(double[] values, uint typeSignature)
    {
        if (values == null || (typeSignature != IccTypeSignatures.S15Fixed16Array && typeSignature != IccTypeSignatures.U16Fixed16Array))
        {
            throw new IccProfileWriteException("Nested fixed-point-array values and type signature are invalid.");
        }

        long payloadSize = 8L + ((long)values.Length * 4L);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The nested fixed-point-array payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(typeSignature);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < values.Length)
        {
            if (typeSignature == IccTypeSignatures.S15Fixed16Array)
            {
                IccNumberCodec.WriteS15Fixed16(writer, values[index]);
            }
            else
            {
                IccNumberCodec.WriteU16Fixed16(writer, values[index]);
            }

            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeXyzArray(IccXyzNumber[] values)
    {
        if (values == null)
        {
            throw new IccProfileWriteException("Nested XYZ values cannot be null.");
        }

        long payloadSize = 8L + ((long)values.Length * 12L);
        if (payloadSize > System.Array.MaxLength)
        {
            throw new IccProfileWriteException("The nested XYZ payload exceeds the supported managed array size.");
        }

        byte[] payload = new byte[(int)payloadSize];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Xyz);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < values.Length)
        {
            IccNumberCodec.WriteXyzNumber(writer, values[index]);
            index = index + 1;
        }

        return payload;
    }

    internal static byte[] SerializeSignatureValue(uint value)
    {
        byte[] payload = new byte[12];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Signature);
        writer.WriteUInt32(0);
        writer.WriteUInt32(value);
        return payload;
    }

    internal static byte[] SerializeDateTimeValue(IccDateTime? value)
    {
        if (value == null || !DateTimeTagCodec.IsValidDateTime(value))
        {
            throw new IccProfileWriteException("A nested date/time value shall use valid ICC dateTimeNumber ranges.");
        }

        byte[] payload = new byte[20];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.DateTime);
        writer.WriteUInt32(0);
        IccNumberCodec.WriteDateTimeNumber(writer, value);
        return payload;
    }

    internal static byte[] SerializeBinaryData(byte[] values, uint typeSignature)
    {
        if (values == null || typeSignature != IccTypeSignatures.Data)
        {
            throw new IccProfileWriteException("Nested binary data requires a dataType signature and non-null bytes.");
        }

        byte[] payload = new byte[12 + values.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Data);
        writer.WriteUInt32(0);
        writer.WriteUInt32(1);
        writer.WriteExactBytes(values);
        return payload;
    }

    internal static byte[] SerializeTextData(IccTextData? text, uint typeSignature)
    {
        return SerializeTextDataForMode(text, typeSignature, IccWriteMode.Preservation);
    }

    internal static byte[] SerializeTextDataForMode(IccTextData? text, uint typeSignature, IccWriteMode mode)
    {
        if (text == null || text.Text == null)
        {
            throw new IccProfileWriteException("Nested text data and decoded text cannot be null.");
        }

        if (typeSignature == IccTypeSignatures.Utf8 || typeSignature == IccTypeSignatures.Utf16
            || typeSignature == IccTypeSignatures.ZippedUtf8 || Icc2TextTagCodec.IsZippedXml(typeSignature))
        {
            IccTextData canonical = new IccTextData();
            canonical.TypeSignature = typeSignature;
            canonical.Text = DeterministicTagSerialization.NormalizeUnicode(text.Text, mode, "ICC.2 text");
            return Icc2TextTagCodec.SerializeIcc2Text(canonical);
        }

        if (typeSignature == IccTypeSignatures.TextDescription)
        {
            return LegacyTagCodec.SerializeTextDescription(text);
        }

        if (typeSignature == IccTypeSignatures.Text)
        {
            return SerializeAsciiText(text.Text);
        }

        if (typeSignature == IccTypeSignatures.Data)
        {
            return SerializeUtf8Data(DeterministicTagSerialization.NormalizeUnicode(text.Text, mode, "ICC.2 dataType text"));
        }

        throw new IccProfileWriteException("The nested text type signature is not supported.");
    }

    internal static byte[] SerializeAsciiText(string text)
    {
        byte[] payload = new byte[9 + text.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Text);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 0 || text[index] > 127)
            {
                throw new IccProfileWriteException("A textType value shall contain only non-NULL 7-bit ASCII characters before its terminator.");
            }

            writer.WriteUInt8((byte)text[index]);
            index = index + 1;
        }

        writer.WriteUInt8(0);
        return payload;
    }

    internal static byte[] SerializeUtf8Data(string text)
    {
        if (!Icc2TextTagCodec.IsValidUnicodeString(text))
        {
            throw new IccProfileWriteException("Nested dataType text contains an invalid Unicode surrogate sequence.");
        }

        if (text.IndexOf('\0') >= 0)
        {
            throw new IccProfileWriteException("Nested dataType text cannot contain a NULL character before its terminator.");
        }

        byte[] encoded = System.Text.Encoding.UTF8.GetBytes(text);
        byte[] payload = new byte[13 + encoded.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Data);
        writer.WriteUInt32(0);
        writer.WriteUInt32(0);
        writer.WriteExactBytes(encoded);
        writer.WriteUInt8(0);
        return payload;
    }

    internal static byte[] SerializeAsciiData(string text)
    {
        if (text == null)
        {
            throw new IccProfileWriteException(
                "ICC.1 dataType text cannot be null.");
        }

        byte[] payload = new byte[13 + text.Length];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Data);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32(0);
        int index = 0;
        while (index < text.Length)
        {
            if (text[index] == 0 || text[index] > 127)
            {
                throw new IccProfileWriteException(
                    "ICC.1 dataType text shall contain only non-NULL 7-bit ASCII characters before its terminator.");
            }

            writer.WriteUInt8((byte)text[index]);
            index = index + 1;
        }

        writer.WriteUInt8(0);
        return payload;
    }

    internal static byte[] SerializeCurveData(IccTagData data, uint typeSignature)
    {
        if (data.Curve == null)
        {
            throw new IccProfileWriteException("Nested curve data cannot be null.");
        }

        if (typeSignature == StructuredTagProjectionCodec.SegmentedCurveType)
        {
            return Icc2ProcessingElementTagCodec.SerializeIcc2SegmentedCurve(
                data.Curve, 0);
        }

        if (typeSignature == IccTypeSignatures.ParametricCurve)
        {
            if (data.Curve.Parametric == null)
            {
                throw new IccProfileWriteException("Nested parametric-curve data cannot be null.");
            }

            return CurveTagCodec.SerializeParametricCurve(data.Curve.Parametric);
        }

        if (typeSignature == IccTypeSignatures.Curve)
        {
            return CurveTagCodec.SerializeCurve(data.Curve);
        }

        throw new IccProfileWriteException("The nested curve type signature is invalid.");
    }

    internal static byte[] SerializeLutData(IccTagData data, uint typeSignature)
    {
        if (typeSignature == IccTypeSignatures.Lut8)
        {
            if (data.Lut == null)
            {
                throw new IccProfileWriteException("Nested lut8 data cannot be null.");
            }

            return LutTagCodec.SerializeLut8(data.Lut);
        }

        if (typeSignature == IccTypeSignatures.Lut16)
        {
            if (data.Lut == null)
            {
                throw new IccProfileWriteException("Nested lut16 data cannot be null.");
            }

            return LutTagCodec.SerializeLut16(data.Lut);
        }

        if (typeSignature == IccTypeSignatures.LutAToB)
        {
            if (data.MultiStageLut == null)
            {
                throw new IccProfileWriteException("Nested lutAToB data cannot be null.");
            }

            return MultiStageLutTagCodec.SerializeLutAToB(data.MultiStageLut);
        }

        if (typeSignature == IccTypeSignatures.LutBToA)
        {
            if (data.MultiStageLut == null)
            {
                throw new IccProfileWriteException("Nested lutBToA data cannot be null.");
            }

            return MultiStageLutTagCodec.SerializeLutBToA(data.MultiStageLut);
        }

        throw new IccProfileWriteException("The nested lookup-table type signature is invalid.");
    }

    internal static byte[] SerializeMultiProcessElementsData(IccMultiProcessElements? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested multi-process-elements data cannot be null.");
        }

        return MultiProcessElementsTagCodec.SerializeMultiProcessElementsVersion5(value);
    }

    internal static byte[] SerializeMeasurementData(IccMeasurement? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested measurement data cannot be null.");
        }

        return MeasurementTagCodec.SerializeMeasurement(value);
    }

    internal static byte[] SerializeViewingConditionsData(IccViewingConditions? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested viewing-conditions data cannot be null.");
        }

        return ViewingConditionsTagCodec.SerializeViewingConditions(value);
    }

    internal static byte[] SerializeChromaticityData(IccChromaticity? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested chromaticity data cannot be null.");
        }

        return ChromaticityTagCodec.SerializeChromaticity(value, IccVersionKind.Version5);
    }

    internal static byte[] SerializeSpectralViewingConditionsData(IccSpectralViewingConditions? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested spectral viewing conditions cannot be null.");
        }

        return SpectralViewingConditionsTagCodec.SerializeSpectralViewingConditions(value);
    }

    internal static byte[] SerializeSparseMatrixArrayData(IccSparseMatrixArray? value)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested sparse-matrix-array data cannot be null.");
        }

        return Icc2ArrayTagCodec.SerializeSparseMatrixArray(value);
    }

    internal static byte[] SerializeNestedArray(IccTagArray? value, int depth, HierarchicalWriteContext context)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested tag-array data cannot be null.");
        }

        return HierarchicalTagCodec.SerializeArrayContainer(value, depth, context);
    }

    internal static byte[] SerializeNestedStructure(IccTagStructure? value, int depth, HierarchicalWriteContext context)
    {
        if (value == null)
        {
            throw new IccProfileWriteException("Nested tag-structure data cannot be null.");
        }

        return HierarchicalTagCodec.SerializeStructureContainer(value, depth, context);
    }

    internal static byte[] CloneBytes(byte[] source)
    {
        byte[] clone = new byte[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            clone[index] = source[index];
            index = index + 1;
        }

        return clone;
    }
}
