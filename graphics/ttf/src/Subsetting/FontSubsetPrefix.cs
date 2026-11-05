namespace ttf;

internal static class FontSubsetPrefix
{
    const ulong PrefixSpace = 308915776UL;

    internal static string Create(
        FontInstance instance,
        FontSubsetOptions options,
        int[] oldToNewGlyphIds,
        string postScriptName)
    {
        if (instance == null || options == null ||
            oldToNewGlyphIds == null || postScriptName == null)
        {
            throw new System.ArgumentNullException("values");
        }

        string? prefixOverride = options.GetSubsetPrefixOverride();

        if (prefixOverride != null)
        {
            return prefixOverride;
        }

        byte[] digest;

        using (System.Security.Cryptography.IncrementalHash hash =
            System.Security.Cryptography.IncrementalHash.CreateHash(
                new System.Security.Cryptography.HashAlgorithmName("SHA256")))
        {
            AppendString(hash, FontSetFaceIdentity.Create(instance.GetFace()));
            AppendString(hash, instance.GetCoordinates().GetKey());
            AppendString(hash, postScriptName);
            AppendPolicies(hash, options);
            AppendGlyphSet(hash, oldToNewGlyphIds);
            digest = hash.GetHashAndReset();
        }

        ulong value = 0;
        int index = 0;

        while (index < 8)
        {
            value = value << 8 | digest[index];
            index += 1;
        }

        value %= PrefixSpace;
        char[] prefix = new char[6];
        index = prefix.Length - 1;

        while (index >= 0)
        {
            prefix[index] = (char)('A' + value % 26UL);
            value /= 26UL;
            index -= 1;
        }

        return new string(prefix);
    }

    internal static void ValidateOverride(string? value)
    {
        if (value == null)
        {
            return;
        }

        if (value.Length != 6)
        {
            throw new System.ArgumentException(
                "A subset prefix override must contain exactly six ASCII uppercase letters.",
                "value");
        }

        int index = 0;

        while (index < value.Length)
        {
            if (value[index] < 'A' || value[index] > 'Z')
            {
                throw new System.ArgumentException(
                    "A subset prefix override must contain exactly six ASCII uppercase letters.",
                    "value");
            }

            index += 1;
        }
    }

    static void AppendPolicies(
        System.Security.Cryptography.IncrementalHash hash,
        FontSubsetOptions options)
    {
        AppendUInt32(hash, (uint)options.GetEmbeddingMode());
        AppendUInt32(hash, (uint)options.GetGlyphRetentionMode());
        AppendUInt32(hash, (uint)options.GetRecommendedGlyphPolicy());
        AppendUInt32(hash, (uint)options.GetHintPolicy());
        AppendUInt32(hash, (uint)options.GetLayoutPolicy());
        AppendUInt32(hash, (uint)options.GetVerticalPolicy());
        AppendUInt32(hash, (uint)options.GetColorPolicy());
        AppendUInt32(hash, (uint)options.GetMetadataPolicy());
        AppendUInt32(hash, (uint)options.GetUnknownTablePolicy());
        AppendUInt32(hash, (uint)options.GetRightsPolicy());
    }

    static void AppendGlyphSet(
        System.Security.Cryptography.IncrementalHash hash,
        int[] oldToNewGlyphIds)
    {
        int retainedCount = 0;
        int index = 0;

        while (index < oldToNewGlyphIds.Length)
        {
            if (oldToNewGlyphIds[index] >= 0)
            {
                retainedCount += 1;
            }

            index += 1;
        }

        AppendUInt32(hash, (uint)retainedCount);
        index = 0;

        while (index < oldToNewGlyphIds.Length)
        {
            if (oldToNewGlyphIds[index] >= 0)
            {
                AppendUInt32(hash, (uint)index);
            }

            index += 1;
        }
    }

    static void AppendString(
        System.Security.Cryptography.IncrementalHash hash,
        string value)
    {
        byte[] encoded = System.Text.Encoding.UTF8.GetBytes(value);
        AppendUInt32(hash, (uint)encoded.Length);
        hash.AppendData(encoded);
    }

    static void AppendUInt32(
        System.Security.Cryptography.IncrementalHash hash,
        uint value)
    {
        byte[] encoded = new byte[4];
        encoded[0] = (byte)((value >> 24) & 0xFFU);
        encoded[1] = (byte)((value >> 16) & 0xFFU);
        encoded[2] = (byte)((value >> 8) & 0xFFU);
        encoded[3] = (byte)(value & 0xFFU);
        hash.AppendData(encoded);
    }
}
