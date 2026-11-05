namespace icc;

internal static class CurveTagCodec
{
    internal static void DecodeCurve(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A curveType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The curveType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader countReader = new BigEndianReader(payload.Slice(8, 4), state.AbsolutePayloadOffset + 8);
        uint declaredCount = countReader.ReadUInt32();
        long expectedSize = 12L + ((long)declaredCount * 2L);
        long alignedSize = (expectedSize + 3L) & -4L;
        if (expectedSize > payload.Length || payload.Length > alignedSize
            || !HasZeroCurvePadding(payload, (int)expectedSize))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The curveType count {declaredCount} requires exactly {expectedSize} bytes, optionally followed by enough zero bytes for four-byte alignment, but this payload contains {payload.Length} bytes.");
            return;
        }

        IccCurve curve = new IccCurve();
        string message;
        if (declaredCount == 0)
        {
            curve.Kind = IccCurveKind.Identity;
            curve.Gamma = 1.0;
            message = "Decoded an identity curveType.";
        }
        else if (declaredCount == 1)
        {
            BigEndianReader gammaReader = new BigEndianReader(payload.Slice(12, 2), state.AbsolutePayloadOffset + 12);
            curve.Kind = IccCurveKind.Gamma;
            curve.Gamma = IccNumberCodec.ReadU8Fixed8(gammaReader);
            message = "Decoded a curveType gamma exponent.";
        }
        else
        {
            int sampleCount = (int)declaredCount;
            double[] samples = new double[sampleCount];
            BigEndianReader sampleReader = new BigEndianReader(payload.Slice(12), state.AbsolutePayloadOffset + 12);
            int index = 0;
            while (index < sampleCount)
            {
                samples[index] = sampleReader.ReadUInt16() / 65535.0;
                index = index + 1;
            }

            curve.Kind = IccCurveKind.Sampled;
            curve.Samples = samples;
            message = $"Decoded a curveType with {sampleCount} sampled values.";
        }

        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Curve);
        data.Curve = curve;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static bool HasZeroCurvePadding(System.ReadOnlyMemory<byte> payload, int expectedSize)
    {
        int index = expectedSize;
        while (index < payload.Length)
        {
            if (payload.Span[index] != 0)
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static void DecodeParametricCurve(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length < 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A parametricCurveType payload requires at least 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload) || payload.Span[10] != 0 || payload.Span[11] != 0)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The parametricCurveType reserved bytes shall be zero.");
            return;
        }

        BigEndianReader functionReader = new BigEndianReader(payload.Slice(8, 2), state.AbsolutePayloadOffset + 8);
        int functionType = functionReader.ReadUInt16();
        int parameterCount = GetParameterCount(functionType);
        if (parameterCount < 0)
        {
            if (state.ProfileVersion == IccVersionKind.Version5)
            {
                UnknownTagCodec.DecodeFallbackPayload(state, payload, typeSignature, true, TagCodecDispatcher.DescribeProfileGeneration(state.ProfileVersion));
                return;
            }

            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The parametricCurveType function selector {functionType} is not one of the ICC.1 function types 0 through 4.");
            return;
        }

        int expectedSize = 12 + (parameterCount * 4);
        if (payload.Length != expectedSize)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"Parametric curve function {functionType} requires exactly {parameterCount} parameters and {expectedSize} bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        double[] parameters = new double[parameterCount];
        BigEndianReader parameterReader = new BigEndianReader(payload.Slice(12), state.AbsolutePayloadOffset + 12);
        int index = 0;
        while (index < parameterCount)
        {
            parameters[index] = IccNumberCodec.ReadS15Fixed16(parameterReader);
            index = index + 1;
        }

        IccParametricCurve parametric = new IccParametricCurve();
        parametric.FunctionType = functionType;
        parametric.Parameters = parameters;
        IccCurve curve = new IccCurve();
        curve.Kind = IccCurveKind.Parametric;
        curve.Parametric = parametric;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Curve);
        data.Curve = curve;
        TagCodecSupport.CompleteSuccessfulDecode(state, data, $"Decoded ICC.1 parametric curve function {functionType} with {parameterCount} parameters using the corrected ICC.1:2022 definition.");
    }

    internal static byte[] SerializeCurve(IccCurve curve)
    {
        if (curve == null || curve.Samples == null)
        {
            throw new IccProfileWriteException("Curve data and its sample array cannot be null.");
        }

        int valueCount;
        switch (curve.Kind)
        {
            case IccCurveKind.Identity:
                valueCount = 0;
                break;
            case IccCurveKind.Gamma:
                valueCount = 1;
                break;
            case IccCurveKind.Sampled:
                if (curve.Samples.Length < 2)
                {
                    throw new IccProfileWriteException("A sampled curveType requires at least two samples.");
                }

                valueCount = curve.Samples.Length;
                break;
            default:
                throw new IccProfileWriteException("Only identity, gamma, and sampled curves can be encoded as curveType.");
        }

        long size = 12L + ((long)valueCount * 2L);
        if (size > int.MaxValue)
        {
            throw new IccProfileWriteException("The curveType payload exceeds the supported managed size.");
        }

        byte[] payload = new byte[(int)size];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Curve);
        writer.WriteZeroBytes(4);
        writer.WriteUInt32((uint)valueCount);
        if (curve.Kind == IccCurveKind.Gamma)
        {
            if (!IsFinite(curve.Gamma) || curve.Gamma < 0.0 || curve.Gamma > IccNumberCodec.U8Fixed8Maximum)
            {
                throw new IccProfileWriteException("A curveType gamma exponent must fit the finite unsigned 8.8 fixed-point range.");
            }

            IccNumberCodec.WriteU8Fixed8(writer, curve.Gamma);
        }
        else if (curve.Kind == IccCurveKind.Sampled)
        {
            int index = 0;
            while (index < curve.Samples.Length)
            {
                double sample = curve.Samples[index];
                if (!IsFinite(sample) || sample < 0.0 || sample > 1.0)
                {
                    throw new IccProfileWriteException($"Curve sample {index} is outside the finite normalized range.");
                }

                double rounded = System.Math.Round(sample * 65535.0, System.MidpointRounding.AwayFromZero);
                writer.WriteUInt16((ushort)rounded);
                index = index + 1;
            }
        }

        return payload;
    }

    internal static byte[] SerializeParametricCurve(IccParametricCurve curve)
    {
        if (curve == null || curve.Parameters == null)
        {
            throw new IccProfileWriteException("Parametric curve data and its parameters cannot be null.");
        }

        int parameterCount = GetParameterCount(curve.FunctionType);
        if (parameterCount < 0)
        {
            throw new IccProfileWriteException("An ICC.1 parametric curve function selector must be between 0 and 4.");
        }

        if (curve.Parameters.Length != parameterCount)
        {
            throw new IccProfileWriteException($"Parametric curve function {curve.FunctionType} requires exactly {parameterCount} parameters.");
        }

        byte[] payload = new byte[12 + (parameterCount * 4)];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.ParametricCurve);
        writer.WriteZeroBytes(4);
        writer.WriteUInt16((ushort)curve.FunctionType);
        writer.WriteZeroBytes(2);
        int index = 0;
        while (index < parameterCount)
        {
            double parameter = curve.Parameters[index];
            if (!IsFinite(parameter) || parameter < IccNumberCodec.S15Fixed16Minimum || parameter > IccNumberCodec.S15Fixed16Maximum)
            {
                throw new IccProfileWriteException($"Parametric curve parameter {index} is outside the finite signed 15.16 fixed-point range.");
            }

            IccNumberCodec.WriteS15Fixed16(writer, parameter);
            index = index + 1;
        }

        return payload;
    }

    internal static int GetParameterCount(int functionType)
    {
        switch (functionType)
        {
            case 0:
                return 1;
            case 1:
                return 3;
            case 2:
                return 4;
            case 3:
                return 5;
            case 4:
                return 7;
            default:
                return -1;
        }
    }

    internal static bool IsFinite(double value)
    {
        return !System.Double.IsNaN(value) && !System.Double.IsInfinity(value);
    }
}
