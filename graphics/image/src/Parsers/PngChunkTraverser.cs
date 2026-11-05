using System.Collections.Generic;

namespace image;

internal class PngChunkTraverser
{
    internal static readonly byte[] Signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "png.reader.null", "PNG parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;

        if (!ValidateSignature(document, reader))
        {
            ResetReader(reader);
            CopyReaderDiagnostics(document, reader);
            ImageMetadataNormalizerCore.BuildSummary(document);
            return document;
        }

        AddHeaderDirectory(document);
        List<PngChunk> chunks = ReadChunks(document, reader, options);
        AddChunkDirectories(document, chunks);
        PngIhdrExtractor.Extract(document, chunks, options);
        PngColorExtractor.Extract(document, chunks, options);
        PngMetadataExtractor.Extract(document, chunks, options);
        PngRawBlockExtractor.Extract(document, chunks, options);
        ResetReader(reader);
        CopyReaderDiagnostics(document, reader);
        ImageMetadataNormalizerCore.BuildSummary(document);
        return document;
    }

    internal static bool ValidateSignature(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.signature.seek", "PNG signature could not be reached.", "PNG", 0, 8);
            return false;
        }

        if (!reader.CanRead(8))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.signature.truncated", "PNG signature was truncated.", "PNG", reader.Position, 8);
            return false;
        }

        byte[] signature = reader.ReadBytes(8);
        if (signature.Length != 8)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.signature.truncated", "PNG signature was truncated.", "PNG", 0, 8);
            return false;
        }

        int index = 0;
        while (index < Signature.Length)
        {
            if (signature[index] != Signature[index])
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.signature.invalid", "PNG signature was invalid.", "PNG", 0, 8);
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static List<PngChunk> ReadChunks(ImageMetadataDocument document, BoundedStreamReader reader, ImageMetadataReaderOptions options)
    {
        List<PngChunk> chunks = new List<PngChunk>();

        if (!reader.SeekAbsolute(8))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.chunk.seek", "PNG chunk stream could not be reached.", "PNG", 8, 0);
            return chunks;
        }

        while (true)
        {
            if (chunks.Count >= GetMaxChunkCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.chunk.count.limit", "PNG chunk count exceeded the configured tag count limit.", "PNG", reader.Position, chunks.Count);
                return chunks;
            }

            if (reader.Remaining() < 8)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.chunk.header.truncated", "PNG chunk header was truncated.", "PNG", reader.Position, 8);
                return chunks;
            }

            PngChunk? chunk = ReadChunk(document, reader, chunks.Count, options);
            if (chunk == null)
            {
                return chunks;
            }

            chunks.Add(chunk);
            if (chunk.Type == "IEND")
            {
                return chunks;
            }
        }
    }

    internal static PngChunk? ReadChunk(ImageMetadataDocument document, BoundedStreamReader reader, int index, ImageMetadataReaderOptions options)
    {
        long chunkOffset = reader.Position;
        uint length = ReadUnsigned32(reader);
        byte[] typeBytes = reader.ReadBytes(4);
        string type = MetadataStringReader.DecodeAsciiRange(typeBytes, 0, typeBytes.Length, null, "PNG");
        bool typeValid = Crc32.ValidateChunkType(type, document.Diagnostics);

        if (length > 2147483647 || length > GetMaxSegmentBytes(options))
        {
            return ReadLimitedChunk(document, reader, index, chunkOffset, length, type, typeValid);
        }

        if (reader.Remaining() < (long)length + 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.chunk.truncated", "PNG chunk payload or CRC was truncated.", type, chunkOffset, 12 + (long)length);
            return null;
        }

        PngChunk chunk = CreateChunk(index, chunkOffset, length, type, typeValid);
        chunk.Data = reader.ReadBytes((int)length);
        chunk.ExpectedCrc = ReadUnsigned32(reader);
        chunk.ActualCrc = 0;
        chunk.CrcValid = false;

        if (typeValid)
        {
            chunk.ActualCrc = Crc32.ComputePngChunk(type, chunk.Data, document.Diagnostics);
            if (chunk.ActualCrc == chunk.ExpectedCrc)
            {
                chunk.CrcValid = true;
            }
            else
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.crc.mismatch", "PNG chunk CRC did not match the computed CRC.", type, chunkOffset, length);
            }
        }

        ValidateReservedBit(document, chunk);
        return chunk;
    }

    internal static PngChunk? ReadLimitedChunk(ImageMetadataDocument document, BoundedStreamReader reader, int index, long chunkOffset, uint length, string type, bool typeValid)
    {
        MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "png.chunk.length.limit", "PNG chunk length exceeded the configured segment limit.", type, chunkOffset, length);
        if (reader.Remaining() < (long)length + 4)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "png.chunk.truncated", "PNG chunk payload or CRC was truncated.", type, chunkOffset, 12 + (long)length);
            return null;
        }

        PngChunk chunk = CreateChunk(index, chunkOffset, length, type, typeValid);
        chunk.DataLimited = true;
        reader.SkipBytes(length);
        chunk.ExpectedCrc = ReadUnsigned32(reader);
        ValidateReservedBit(document, chunk);
        return chunk;
    }

    internal static PngChunk CreateChunk(int index, long chunkOffset, uint length, string type, bool typeValid)
    {
        PngChunk chunk = new PngChunk();
        chunk.Index = index;
        chunk.Type = type;
        chunk.Offset = chunkOffset;
        chunk.Length = length;
        chunk.DataOffset = chunkOffset + 8;
        chunk.CrcOffset = chunk.DataOffset + length;
        chunk.TypeValid = typeValid;
        chunk.Critical = IsCritical(type);
        chunk.Public = IsPublic(type);
        chunk.ReservedBitValid = HasValidReservedBit(type);
        chunk.SafeToCopy = IsSafeToCopy(type);
        return chunk;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Png, "Header", "PNG Header");
        directory.Path = "PNG/Header";
        directory.Offset = 0;
        directory.Length = 8;
        directory.AddTagWithValue("Png.Header.SignatureValid", "Signature Valid", MetadataValue.FromBoolean(true));
        directory.AddTagWithValue("Png.Header.SignatureLength", "Signature Length", MetadataValue.FromInteger(8));
    }

    internal static void AddChunkDirectories(ImageMetadataDocument document, List<PngChunk> chunks)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        foreach (PngChunk chunk in chunks)
        {
            AddChunkDirectory(document, chunk);
        }
    }

    internal static MetadataDirectory AddChunkDirectory(ImageMetadataDocument document, PngChunk chunk)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Png, chunk.Type, chunk.Type + " Chunk");
        directory.Path = "PNG/" + chunk.Type + "/" + chunk.Index.ToString();
        directory.Offset = chunk.Offset;
        directory.Length = 12 + chunk.Length;
        directory.RepeatIndex = chunk.Index;
        string prefix = "Png." + chunk.Type;
        directory.AddTagWithValue(prefix + ".Index", "Index", MetadataValue.FromInteger(chunk.Index));
        directory.AddTagWithValue(prefix + ".Length", "Length", MetadataValue.FromUnsignedInteger((ulong)chunk.Length));
        directory.AddTagWithValue(prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(chunk.DataOffset));
        directory.AddTagWithValue(prefix + ".CrcOffset", "CRC Offset", MetadataValue.FromInteger(chunk.CrcOffset));
        directory.AddTagWithValue(prefix + ".ExpectedCrc", "Expected CRC", MetadataValue.FromUnsignedInteger(chunk.ExpectedCrc));
        directory.AddTagWithValue(prefix + ".ActualCrc", "Actual CRC", MetadataValue.FromUnsignedInteger(chunk.ActualCrc));
        directory.AddTagWithValue(prefix + ".CrcValid", "CRC Valid", MetadataValue.FromBoolean(chunk.CrcValid));
        directory.AddTagWithValue(prefix + ".TypeValid", "Type Valid", MetadataValue.FromBoolean(chunk.TypeValid));
        directory.AddTagWithValue(prefix + ".Critical", "Critical", MetadataValue.FromBoolean(chunk.Critical));
        directory.AddTagWithValue(prefix + ".Public", "Public", MetadataValue.FromBoolean(chunk.Public));
        directory.AddTagWithValue(prefix + ".ReservedBitValid", "Reserved Bit Valid", MetadataValue.FromBoolean(chunk.ReservedBitValid));
        directory.AddTagWithValue(prefix + ".SafeToCopy", "Safe To Copy", MetadataValue.FromBoolean(chunk.SafeToCopy));
        directory.AddTagWithValue(prefix + ".DataLimited", "Data Limited", MetadataValue.FromBoolean(chunk.DataLimited));
        return directory;
    }

    internal static void ValidateReservedBit(ImageMetadataDocument document, PngChunk chunk)
    {
        if (chunk.TypeValid && !chunk.ReservedBitValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "png.chunk.reserved.invalid", "PNG chunk reserved type bit was set.", chunk.Type, chunk.Offset + 4, 4);
        }
    }

    internal static uint ReadUnsigned32(BoundedStreamReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length != 4)
        {
            return 0;
        }

        return ((uint)bytes[0] << 24)
            | ((uint)bytes[1] << 16)
            | ((uint)bytes[2] << 8)
            | (uint)bytes[3];
    }

    internal static bool IsCritical(string type)
    {
        return HasTypeByte(type, 0) && IsUpperAscii(type[0]);
    }

    internal static bool IsPublic(string type)
    {
        return HasTypeByte(type, 1) && IsUpperAscii(type[1]);
    }

    internal static bool HasValidReservedBit(string type)
    {
        return HasTypeByte(type, 2) && IsUpperAscii(type[2]);
    }

    internal static bool IsSafeToCopy(string type)
    {
        return HasTypeByte(type, 3) && IsLowerAscii(type[3]);
    }

    internal static bool HasTypeByte(string type, int index)
    {
        return type != null && index >= 0 && index < type.Length;
    }

    internal static bool IsUpperAscii(char value)
    {
        return value >= 'A' && value <= 'Z';
    }

    internal static bool IsLowerAscii(char value)
    {
        return value >= 'a' && value <= 'z';
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxSegmentBytes;
        }

        return options.MaxSegmentBytes;
    }

    internal static int GetMaxChunkCount(ImageMetadataReaderOptions options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxTagCount;
        }

        return options.MaxTagCount;
    }

    internal static void CopyReaderDiagnostics(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (document == null || reader == null || reader.Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
        {
            document.Diagnostics.Add(diagnostic);
        }
    }

    internal static void ResetReader(BoundedStreamReader reader)
    {
        if (reader == null || reader.Source == null || !reader.Source.CanSeek)
        {
            return;
        }

        reader.SeekAbsolute(0);
    }
}
