using System.Collections.Generic;

namespace png;

internal class PngChunkPlacementProcessor
{
    internal static PngChunk[] ApplyPolicy(PngChunk[] chunks, PngChunkPlacementPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        switch (policy)
        {
            case PngChunkPlacementPolicy.Preserve:
                return chunks;
            case PngChunkPlacementPolicy.Canonicalize:
                return Canonicalize(chunks);
            default:
                throw new ArgumentOutOfRangeException("policy");
        }
    }

    internal static PngChunk[] Canonicalize(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        List<PngChunk> headers = new List<PngChunk>();
        List<PngChunk> beforePalette = new List<PngChunk>();
        List<PngChunk> palettes = new List<PngChunk>();
        List<PngChunk> beforeImageData = new List<PngChunk>();
        List<PngChunk> imageData = new List<PngChunk>();
        List<PngChunk> afterImageData = new List<PngChunk>();
        List<PngChunk> ends = new List<PngChunk>();
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);

            PngChunkPlacement placement = GetCanonicalPlacement(chunk);
            chunk.Placement = placement;
            if (chunk.Type == PngConstants.ChunkIHDR)
            {
                headers.Add(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkPLTE)
            {
                palettes.Add(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkIDAT)
            {
                imageData.Add(chunk);
            }
            else if (chunk.Type == PngConstants.ChunkIEND)
            {
                ends.Add(chunk);
            }
            else if (placement == PngChunkPlacement.BeforePalette)
            {
                beforePalette.Add(chunk);
            }
            else if (placement == PngChunkPlacement.BeforeImageData)
            {
                beforeImageData.Add(chunk);
            }
            else if (placement == PngChunkPlacement.AfterImageData)
            {
                afterImageData.Add(chunk);
            }
            else
            {
                throw new ArgumentException("Only IDAT chunks can occupy the image-data placement.");
            }

            index += 1;
        }

        PngChunk[] result = new PngChunk[chunks.Length];
        int outputIndex = 0;
        outputIndex = Append(result, outputIndex, headers);
        outputIndex = Append(result, outputIndex, beforePalette);
        outputIndex = Append(result, outputIndex, palettes);
        outputIndex = Append(result, outputIndex, beforeImageData);
        outputIndex = Append(result, outputIndex, imageData);
        outputIndex = Append(result, outputIndex, afterImageData);
        Append(result, outputIndex, ends);
        return result;
    }

    internal static PngChunkPlacement GetCanonicalPlacement(PngChunk chunk)
    {
        if (chunk.Type == PngConstants.ChunkIHDR || PngChunkOrderValidator.MustAppearBeforePlteAndIdat(chunk.Type))
        {
            return PngChunkPlacement.BeforePalette;
        }

        if (chunk.Type == PngConstants.ChunkPLTE || PngChunkOrderValidator.MustAppearBeforeIdat(chunk.Type))
        {
            return PngChunkPlacement.BeforeImageData;
        }

        if (chunk.Type == PngConstants.ChunkIDAT)
        {
            return PngChunkPlacement.ImageData;
        }

        if (chunk.Type == PngConstants.ChunkIEND)
        {
            return PngChunkPlacement.AfterImageData;
        }

        switch (chunk.Placement)
        {
            case PngChunkPlacement.Unspecified:
                return PngChunkPlacement.BeforeImageData;
            case PngChunkPlacement.BeforePalette:
            case PngChunkPlacement.BeforeImageData:
            case PngChunkPlacement.ImageData:
            case PngChunkPlacement.AfterImageData:
                return chunk.Placement;
            default:
                throw new ArgumentOutOfRangeException("Placement");
        }
    }

    internal static void AnnotatePreservedChunks(PngChunk[] chunks)
    {
        int paletteIndex = FindFirstChunk(chunks, PngConstants.ChunkPLTE);
        int imageDataIndex = FindFirstChunk(chunks, PngConstants.ChunkIDAT);
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            chunk.Placement = GetReadPlacement(chunk, index, paletteIndex, imageDataIndex);
            chunk.PreservationState = PngChunkPreservationState.Preserved;
            index += 1;
        }
    }

    internal static PngChunkPlacement GetReadPlacement(PngChunk chunk, int index, int paletteIndex, int imageDataIndex)
    {
        if (chunk.Type == PngConstants.ChunkIDAT)
        {
            return PngChunkPlacement.ImageData;
        }

        if (chunk.Type == PngConstants.ChunkIEND || (imageDataIndex >= 0 && index > imageDataIndex))
        {
            return PngChunkPlacement.AfterImageData;
        }

        if (chunk.Type == PngConstants.ChunkPLTE)
        {
            return PngChunkPlacement.BeforeImageData;
        }

        if (paletteIndex >= 0 && index < paletteIndex)
        {
            return PngChunkPlacement.BeforePalette;
        }

        if (paletteIndex >= 0)
        {
            return PngChunkPlacement.BeforeImageData;
        }

        if (PngChunkOrderValidator.MustAppearBeforePlteAndIdat(chunk.Type))
        {
            return PngChunkPlacement.BeforePalette;
        }

        return PngChunkPlacement.BeforeImageData;
    }

    internal static int FindFirstChunk(PngChunk[] chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index].Type == chunkType)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static int Append(PngChunk[] target, int targetIndex, List<PngChunk> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            target[targetIndex] = source[index];
            targetIndex += 1;
            index += 1;
        }

        return targetIndex;
    }
}
