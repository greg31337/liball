namespace png;

internal class PngColorMetadataPrecedenceResolver
{
    internal const int NoColorMetadataPrecedence = 0;
    internal const int CodingIndependentCodePointsPrecedence = 1;
    internal const int IccProfilePrecedence = 2;
    internal const int StandardRgbPrecedence = 3;
    internal const int ChromaticitiesGammaPrecedence = 4;
    internal const string ChromaticitiesGammaSource = "cHRM/gAMA";

    internal static PngColorMetadata Resolve(PngMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Chunks);

        return Resolve(metadata, new PngSecurityLimits());
    }

    internal static PngColorMetadata Resolve(PngMetadata metadata, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(metadata.Chunks);

        if (metadata.Chunks.Length == 0)
        {
            return ResolveTypedMetadata(metadata);
        }

        return Resolve(metadata.Chunks, limits);
    }

    internal static PngColorMetadata ResolveTypedMetadata(PngMetadata metadata)
    {
        if (metadata.CodingIndependentCodePoints != null)
        {
            PngColorMetadata result = CreateMetadata(PngConstants.ChunkCodingIndependentCodePoints, CodingIndependentCodePointsPrecedence);
            result.HasCodingIndependentCodePoints = true;
            result.CodingIndependentCodePoints = metadata.CodingIndependentCodePoints;
            return result;
        }

        if (metadata.IccProfile != null)
        {
            PngColorMetadata result = CreateMetadata(PngConstants.ChunkIccProfile, IccProfilePrecedence);
            result.HasIccProfile = true;
            result.IccProfile = metadata.IccProfile;
            return result;
        }

        if (metadata.StandardRgb != null)
        {
            PngColorMetadata result = CreateMetadata(PngConstants.ChunkStandardRgb, StandardRgbPrecedence);
            result.HasStandardRgb = true;
            result.StandardRgb = metadata.StandardRgb;
            return result;
        }

        if (metadata.Chromaticities != null || metadata.Gamma != null)
        {
            PngColorMetadata result = CreateMetadata(GetTypedChromaticitiesGammaSource(metadata), ChromaticitiesGammaPrecedence);
            if (metadata.Chromaticities != null)
            {
                result.HasChromaticities = true;
                result.Chromaticities = metadata.Chromaticities;
            }

            if (metadata.Gamma != null)
            {
                result.HasGamma = true;
                result.Gamma = metadata.Gamma;
            }

            return result;
        }

        return new PngColorMetadata();
    }

    internal static string GetTypedChromaticitiesGammaSource(PngMetadata metadata)
    {
        if (metadata.Chromaticities != null && metadata.Gamma != null)
        {
            return ChromaticitiesGammaSource;
        }

        if (metadata.Chromaticities != null)
        {
            return PngConstants.ChunkChromaticities;
        }

        return PngConstants.ChunkGamma;
    }

    internal static PngColorMetadata Resolve(PngChunk[] chunks)
    {
        return Resolve(chunks, new PngSecurityLimits());
    }

    internal static PngColorMetadata Resolve(PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(limits);

        PngChunk? chunk = FindFirstChunk(chunks, PngConstants.ChunkCodingIndependentCodePoints);
        if (chunk != null)
        {
            return CreateCodingIndependentCodePointsMetadata(chunk);
        }

        chunk = FindFirstChunk(chunks, PngConstants.ChunkIccProfile);
        if (chunk != null)
        {
            return CreateIccProfileMetadata(chunk, limits);
        }

        chunk = FindFirstChunk(chunks, PngConstants.ChunkStandardRgb);
        if (chunk != null)
        {
            return CreateStandardRgbMetadata(chunk);
        }

        return CreateChromaticitiesGammaMetadata(chunks);
    }

    internal static bool IsColorMetadataChunk(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        return GetPrecedence(chunkType) != NoColorMetadataPrecedence;
    }

    internal static int GetPrecedence(string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunkType);

        if (chunkType == PngConstants.ChunkCodingIndependentCodePoints)
        {
            return CodingIndependentCodePointsPrecedence;
        }

        if (chunkType == PngConstants.ChunkIccProfile)
        {
            return IccProfilePrecedence;
        }

        if (chunkType == PngConstants.ChunkStandardRgb)
        {
            return StandardRgbPrecedence;
        }

        if (chunkType == PngConstants.ChunkChromaticities || chunkType == PngConstants.ChunkGamma)
        {
            return ChromaticitiesGammaPrecedence;
        }

        return NoColorMetadataPrecedence;
    }

    internal static PngColorMetadata CreateCodingIndependentCodePointsMetadata(PngChunk chunk)
    {
        PngColorMetadata metadata = CreateMetadata(PngConstants.ChunkCodingIndependentCodePoints, CodingIndependentCodePointsPrecedence);
        metadata.HasCodingIndependentCodePoints = true;
        metadata.CodingIndependentCodePoints = PngCodingIndependentCodePointsParser.Parse(chunk);
        return metadata;
    }

    internal static PngColorMetadata CreateIccProfileMetadata(PngChunk chunk, PngSecurityLimits limits)
    {
        PngColorMetadata metadata = CreateMetadata(PngConstants.ChunkIccProfile, IccProfilePrecedence);
        metadata.HasIccProfile = true;
        metadata.IccProfile = PngIccProfileParser.Parse(chunk, limits);
        return metadata;
    }

    internal static PngColorMetadata CreateStandardRgbMetadata(PngChunk chunk)
    {
        PngColorMetadata metadata = CreateMetadata(PngConstants.ChunkStandardRgb, StandardRgbPrecedence);
        metadata.HasStandardRgb = true;
        metadata.StandardRgb = PngStandardRgbParser.Parse(chunk);
        return metadata;
    }

    internal static PngColorMetadata CreateChromaticitiesGammaMetadata(PngChunk[] chunks)
    {
        PngChunk? chromaticitiesChunk = FindFirstChunk(chunks, PngConstants.ChunkChromaticities);
        PngChunk? gammaChunk = FindFirstChunk(chunks, PngConstants.ChunkGamma);
        if (chromaticitiesChunk == null && gammaChunk == null)
        {
            return new PngColorMetadata();
        }

        PngColorMetadata metadata = CreateMetadata(GetChromaticitiesGammaSource(chromaticitiesChunk, gammaChunk), ChromaticitiesGammaPrecedence);
        if (chromaticitiesChunk != null)
        {
            metadata.HasChromaticities = true;
            metadata.Chromaticities = PngChromaticitiesParser.Parse(chromaticitiesChunk);
        }

        if (gammaChunk != null)
        {
            metadata.HasGamma = true;
            metadata.Gamma = PngGammaParser.Parse(gammaChunk);
        }

        return metadata;
    }

    internal static string GetChromaticitiesGammaSource(PngChunk? chromaticitiesChunk, PngChunk? gammaChunk)
    {
        if (chromaticitiesChunk != null && gammaChunk != null)
        {
            return ChromaticitiesGammaSource;
        }

        if (chromaticitiesChunk != null)
        {
            return PngConstants.ChunkChromaticities;
        }

        if (gammaChunk != null)
        {
            return PngConstants.ChunkGamma;
        }

        throw new ArgumentException("At least one color metadata chunk is required.");
    }

    internal static PngColorMetadata CreateMetadata(string sourceChunkType, int precedence)
    {
        PngColorMetadata metadata = new PngColorMetadata();
        metadata.HasColorMetadata = true;
        metadata.SourceChunkType = sourceChunkType;
        metadata.Precedence = precedence;
        return metadata;
    }

    internal static PngChunk? FindFirstChunk(PngChunk[] chunks, string chunkType)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(chunkType);

        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            if (chunk.Type == chunkType)
            {
                return chunk;
            }

            index += 1;
        }

        return null;
    }
}
