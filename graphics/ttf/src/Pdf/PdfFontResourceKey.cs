namespace ttf;

internal static class PdfFontResourceKeyBuilder
{
    internal static string Build(
        PdfFontUsage usage,
        PdfCidAllocator allocator,
        PdfFontKind fontKind,
        byte[] embeddedFont,
        PdfToUnicodeMappingArtifact mappings,
        FontSubsetEmbeddingDecision decision)
    {
        if (usage == null || allocator == null || embeddedFont == null ||
            mappings == null || decision == null)
        {
            throw new System.ArgumentNullException("values");
        }

        byte[] digest;

        using (System.Security.Cryptography.IncrementalHash hash =
            System.Security.Cryptography.IncrementalHash.CreateHash(
                new System.Security.Cryptography.HashAlgorithmName("SHA256")))
        {
            AppendString(hash, "ttf-pdf-font-resource-v1");
            AppendString(hash, usage.GetKey());
            AppendUInt32(hash, (uint)fontKind);
            AppendDecision(hash, decision);
            AppendIntegers(
                hash,
                allocator.CopySourceGlyphIdsInCidOrder());
            AppendMappings(hash, usage.CopyMappings());
            AppendBytes(hash, embeddedFont);
            AppendBytes(hash, mappings.GetCMap().GetBytes());
            digest = hash.GetHashAndReset();
        }

        return "PFR1-" + System.Convert.ToHexString(digest);
    }

    static void AppendDecision(
        System.Security.Cryptography.IncrementalHash hash,
        FontSubsetEmbeddingDecision decision)
    {
        AppendUInt32(
            hash,
            (uint)decision.GetRequestedEmbeddingMode());
        AppendUInt32(
            hash,
            (uint)decision.GetEffectiveEmbeddingMode());
        AppendUInt32(hash, (uint)decision.GetPolicy());
        AppendBoolean(hash, decision.HasPolicyConflict());
        AppendBoolean(hash, decision.WasConflictReported());
        AppendBoolean(hash, decision.WasFullFontForced());
    }

    static void AppendMappings(
        System.Security.Cryptography.IncrementalHash hash,
        PdfFontUsageMapping[] mappings)
    {
        AppendUInt32(hash, (uint)mappings.Length);
        int index = 0;

        while (index < mappings.Length)
        {
            AppendUInt32(hash, (uint)mappings[index].GetGlyphId());
            AppendIntegers(
                hash,
                mappings[index].CopyUnicodeSequence());
            index += 1;
        }
    }

    static void AppendIntegers(
        System.Security.Cryptography.IncrementalHash hash,
        int[] values)
    {
        AppendUInt32(hash, (uint)values.Length);
        int index = 0;

        while (index < values.Length)
        {
            AppendUInt32(
                hash,
                CheckedFontMath.ReinterpretInt32AsUInt32(values[index]));
            index += 1;
        }
    }

    static void AppendString(
        System.Security.Cryptography.IncrementalHash hash,
        string value)
    {
        AppendBytes(hash, System.Text.Encoding.UTF8.GetBytes(value));
    }

    static void AppendBytes(
        System.Security.Cryptography.IncrementalHash hash,
        byte[] values)
    {
        AppendUInt32(hash, (uint)values.Length);
        hash.AppendData(values);
    }

    static void AppendBoolean(
        System.Security.Cryptography.IncrementalHash hash,
        bool value)
    {
        AppendUInt32(hash, value ? 1U : 0U);
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
