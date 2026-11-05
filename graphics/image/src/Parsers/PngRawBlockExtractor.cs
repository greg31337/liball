using System.Collections.Generic;

namespace image;

internal class PngRawBlockExtractor
{
    internal static void Extract(ImageMetadataDocument document, List<PngChunk> chunks, ImageMetadataReaderOptions options)
    {
        if (document == null || chunks == null || !CaptureRawBlocks(options))
        {
            return;
        }

        foreach (PngChunk chunk in chunks)
        {
            if (IsUnknownAncillaryChunk(chunk))
            {
                AddRawBlock(document, chunk, options);
            }
        }
    }

    internal static bool IsUnknownAncillaryChunk(PngChunk chunk)
    {
        if (chunk == null || !chunk.TypeValid || chunk.Critical)
        {
            return false;
        }

        return !IsKnownChunkType(chunk.Type);
    }

    internal static bool IsKnownChunkType(string type)
    {
        switch (type)
        {
            case "IHDR":
            case "PLTE":
            case "IDAT":
            case "IEND":
            case "tRNS":
            case "cHRM":
            case "gAMA":
            case "iCCP":
            case "sBIT":
            case "sRGB":
            case "cICP":
            case "mDCV":
            case "mDCv":
            case "cLLI":
            case "cLLi":
            case "tEXt":
            case "zTXt":
            case "iTXt":
            case "bKGD":
            case "hIST":
            case "pHYs":
            case "sPLT":
            case "eXIf":
            case "tIME":
            case "acTL":
            case "fcTL":
            case "fdAT":
                return true;
            default:
                return false;
        }
    }

    internal static void AddRawBlock(ImageMetadataDocument document, PngChunk chunk, ImageMetadataReaderOptions options)
    {
        MetadataRawBlock rawBlock = new MetadataRawBlock();
        rawBlock.Family = MetadataFamily.Png;
        rawBlock.BlockType = chunk.Type;
        rawBlock.Name = "Unknown PNG Ancillary Chunk";
        rawBlock.Offset = chunk.DataOffset;
        rawBlock.Length = chunk.Length;

        if (CaptureRawBlockData(options))
        {
            if (chunk.Length > GetMaxRawBlockBytes(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.raw.block.limit", "PNG ancillary chunk exceeded the configured raw block limit.", chunk.Type, chunk.DataOffset, chunk.Length);
            }
            else if (!chunk.DataLimited && chunk.Data != null && chunk.Data.Length == chunk.Length)
            {
                rawBlock.Data = PngColorExtractor.CopyRange(chunk.Data, 0, chunk.Data.Length);
            }
        }

        document.AddRawBlock(rawBlock);
    }

    internal static bool CaptureRawBlocks(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlocks;
        }

        return options.CaptureRawBlocks;
    }

    internal static bool CaptureRawBlockData(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().CaptureRawBlockData;
        }

        return options.CaptureRawBlockData;
    }

    internal static long GetMaxRawBlockBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxRawBlockBytes;
        }

        return options.MaxRawBlockBytes;
    }
}
