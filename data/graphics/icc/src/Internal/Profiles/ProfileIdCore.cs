namespace icc;

internal static class ProfileIdCore
{
    internal const int ProfileFlagsOffset = 44;
    internal const int RenderingIntentOffset = 64;
    internal const int ProfileIdOffset = 84;

    internal static IccProfileId Compute(System.ReadOnlySpan<byte> profileBytes)
    {
        ValidateProfileBytes(profileBytes);
        byte[] prepared = profileBytes.ToArray();
        System.Array.Clear(prepared, ProfileFlagsOffset, 4);
        System.Array.Clear(prepared, RenderingIntentOffset, 4);
        System.Array.Clear(prepared, ProfileIdOffset, IccConstants.ProfileIdSize);
        byte[] digest = System.Security.Cryptography.MD5.HashData(prepared);
        IccProfileId result = new IccProfileId();
        result.Bytes = digest;
        result.IsPresent = true;
        return result;
    }

    internal static void ValidateProfileBytes(System.ReadOnlySpan<byte> profileBytes)
    {
        if (profileBytes.Length < IccConstants.MinimumProfileSize)
        {
            throw new IccProfileFormatException("Profile ID calculation requires a complete ICC profile of at least 132 bytes.");
        }

        uint declaredSize = ReadUInt32(profileBytes, 0);
        if (declaredSize != (uint)profileBytes.Length)
        {
            throw new IccProfileFormatException($"The profile declares {declaredSize} bytes, but Profile ID calculation received {profileBytes.Length} bytes.");
        }

        if (ReadUInt32(profileBytes, 36) != IccConstants.ProfileFileSignature)
        {
            throw new IccProfileFormatException("Profile ID calculation requires the ICC 'acsp' header signature.");
        }

        IccVersion version = HeaderCodec.DecodeVersion(ReadUInt32(profileBytes, 8));
        if (!version.IsSupported || (version.Kind != IccVersionKind.Version4 && version.Kind != IccVersionKind.Version5))
        {
            throw new IccUnsupportedFeatureException("ICC Profile IDs are supported only for directly supported version 4 and version 5 profiles.");
        }
    }

    internal static uint ReadUInt32(System.ReadOnlySpan<byte> bytes, int offset)
    {
        return ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) | ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
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

    internal static IccOperationResult CreateResult(IccOperationStatus status, string message)
    {
        IccOperationResult result = new IccOperationResult();
        result.Status = status;
        result.Message = message;
        result.Location.Offset = ProfileIdOffset;
        result.Location.Length = IccConstants.ProfileIdSize;
        return result;
    }
}
