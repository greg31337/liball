namespace icc;

internal static class CicpTagCodec
{
    internal static void DecodeCicp(TagDecodeState state, System.ReadOnlyMemory<byte> payload, uint typeSignature)
    {
        if (payload.Length != 12)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"A cicpType payload requires exactly 12 bytes, but this payload contains {payload.Length} bytes.");
            return;
        }

        if (!TagCodecSupport.HasZeroReservedBytes(payload))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, "The cicpType reserved bytes shall be zero.");
            return;
        }

        System.ReadOnlySpan<byte> bytes = payload.Span;
        int colorPrimaries = bytes[8];
        int transferCharacteristics = bytes[9];
        int matrixCoefficients = bytes[10];
        int fullRange = bytes[11];
        if (fullRange > 1)
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, $"The cicpType VideoFullRangeFlag shall be zero or one, but is {fullRange}.");
            return;
        }

        if (state.HasProfileContext && !IsPermittedContext(state.ProfileClass, state.DataColorSpace, matrixCoefficients))
        {
            TagCodecSupport.CompleteInvalidDecode(state, payload, typeSignature, CreateContextError(state.ProfileClass, state.DataColorSpace, matrixCoefficients));
            return;
        }

        IccCicp cicp = new IccCicp();
        cicp.ColorPrimaries = colorPrimaries;
        cicp.TransferCharacteristics = transferCharacteristics;
        cicp.MatrixCoefficients = matrixCoefficients;
        cicp.FullRange = fullRange == 1;
        cicp.UsesProfilePrimaries = colorPrimaries == 2
            && state.ProfileVersion == IccVersionKind.Version4
            && state.HasProfileContext
            && state.ProfileClass != IccProfileClass.Unknown
            && state.DataColorSpace == IccColorSpaceKind.Rgb
            && state.HasMatrixPrimaries;
        IccTagData data = TagCodecSupport.CreateTypedData(state, payload, typeSignature, IccTagDataKind.Cicp);
        data.Cicp = cicp;
        string message = cicp.UsesProfilePrimaries
            ? "Decoded cicpType metadata; unspecified colour primaries resolve to the profile's RGB matrix columns under the ICC amendment."
            : "Decoded cicpType metadata.";
        TagCodecSupport.CompleteSuccessfulDecode(state, data, message);
    }

    internal static byte[] SerializeCicp(IccCicp cicp, IccProfileClass profileClass, IccColorSpaceKind dataColorSpace)
    {
        if (cicp == null)
        {
            throw new IccProfileWriteException("CICP data cannot be null.");
        }

        if (cicp.ColorPrimaries < 0 || cicp.ColorPrimaries > byte.MaxValue
            || cicp.TransferCharacteristics < 0 || cicp.TransferCharacteristics > byte.MaxValue
            || cicp.MatrixCoefficients < 0 || cicp.MatrixCoefficients > byte.MaxValue)
        {
            throw new IccProfileWriteException("CICP code points shall fit unsigned 8-bit fields.");
        }

        if (!IsPermittedContext(profileClass, dataColorSpace, cicp.MatrixCoefficients))
        {
            throw new IccProfileWriteException(CreateContextError(profileClass, dataColorSpace, cicp.MatrixCoefficients));
        }

        byte[] payload = new byte[12];
        BigEndianWriter writer = new BigEndianWriter(payload, 0);
        writer.WriteUInt32(IccTypeSignatures.Cicp);
        writer.WriteZeroBytes(4);
        writer.WriteUInt8((byte)cicp.ColorPrimaries);
        writer.WriteUInt8((byte)cicp.TransferCharacteristics);
        writer.WriteUInt8((byte)cicp.MatrixCoefficients);
        writer.WriteUInt8(cicp.FullRange ? (byte)1 : (byte)0);
        return payload;
    }

    internal static bool IsPermittedContext(IccProfileClass profileClass, IccColorSpaceKind dataColorSpace, int matrixCoefficients)
    {
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display)
        {
            return false;
        }

        if (dataColorSpace == IccColorSpaceKind.Rgb || dataColorSpace == IccColorSpaceKind.Xyz)
        {
            return matrixCoefficients == 0;
        }

        if (dataColorSpace == IccColorSpaceKind.YCbCr)
        {
            return matrixCoefficients != 0;
        }

        return false;
    }

    internal static string CreateContextError(IccProfileClass profileClass, IccColorSpaceKind dataColorSpace, int matrixCoefficients)
    {
        if (profileClass != IccProfileClass.Input && profileClass != IccProfileClass.Display)
        {
            return "The cicpTag is permitted only in Input or Display profiles.";
        }

        if (dataColorSpace != IccColorSpaceKind.Rgb && dataColorSpace != IccColorSpaceKind.YCbCr && dataColorSpace != IccColorSpaceKind.Xyz)
        {
            return "The cicpTag is permitted only for RGB, YCbCr, or XYZ profile data colour spaces.";
        }

        if ((dataColorSpace == IccColorSpaceKind.Rgb || dataColorSpace == IccColorSpaceKind.Xyz) && matrixCoefficients != 0)
        {
            return "RGB and XYZ cicpTag data require MatrixCoefficients to be zero.";
        }

        return "YCbCr cicpTag data require MatrixCoefficients to be non-zero.";
    }
}
