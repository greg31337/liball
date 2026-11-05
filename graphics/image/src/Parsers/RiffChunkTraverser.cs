using System.Collections.Generic;

namespace image;

internal class RiffChunkTraverser
{
    internal const long HeaderLength = 12;
    internal const long MinimumFileSizeField = 4;
    internal const long MaximumFileSizeField = 4294967286;

    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);

        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "riff.reader.null", "RIFF parser received a null reader.");
            return document;
        }

        document.ByteLength = reader.Length;
        RiffHeader header = ReadHeader(document, reader);
        if (header.Valid)
        {
            List<RiffChunk> chunks = ReadChunks(document, reader, header.DeclaredEnd, options);
            AddChunkDirectories(document, chunks);
            WebPMetadataExtractor.Extract(document, chunks, options);
            ReportTrailingData(document, reader, header.DeclaredEnd);
        }

        Finish(document, reader);
        return document;
    }

    internal static RiffHeader ReadHeader(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        RiffHeader header = new RiffHeader();
        if (!reader.SeekAbsolute(0))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.header.seek", "RIFF header could not be reached.", "RIFF", 0, HeaderLength);
            return header;
        }

        if (!reader.CanRead(HeaderLength))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "riff.header.truncated", "RIFF header was truncated.", "RIFF", 0, HeaderLength);
            return header;
        }

        byte[] signatureBytes = reader.ReadBytes(4);
        uint fileSize = ReadUnsigned32(reader);
        byte[] formBytes = reader.ReadBytes(4);
        string signature = MetadataStringReader.DecodeAscii(signatureBytes);
        string formType = MetadataStringReader.DecodeAscii(formBytes);
        header.Signature = signature;
        header.FileSize = fileSize;
        header.FormType = formType;
        header.DeclaredEnd = 8 + (long)fileSize;

        if (signature != "RIFF")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.signature.invalid", "RIFF signature was invalid.", "RIFF", 0, 4);
            return header;
        }

        if (formType != "WEBP")
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.form.invalid", "RIFF form type was not WEBP.", "RIFF", 8, 4);
            return header;
        }

        if (fileSize < MinimumFileSizeField || fileSize > MaximumFileSizeField)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.file-size.invalid", "RIFF file size field was outside the valid WebP range.", "RIFF", 4, 4);
            return header;
        }

        if ((fileSize & 1) != 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.file-size.odd", "RIFF file size field was not even.", "RIFF", 4, 4);
        }

        if (header.DeclaredEnd > reader.Length)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "riff.file.truncated", "RIFF declared file size exceeded the available source bytes.", "RIFF", 0, header.DeclaredEnd);
        }

        AddHeaderDirectory(document, header, reader.Length);
        header.Valid = true;
        return header;
    }

    internal static List<RiffChunk> ReadChunks(ImageMetadataDocument document, BoundedStreamReader reader, long declaredEnd, ImageMetadataReaderOptions options)
    {
        List<RiffChunk> chunks = new List<RiffChunk>();
        if (!reader.SeekAbsolute(HeaderLength))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.chunk.seek", "RIFF chunk stream could not be reached.", "RIFF", HeaderLength, 0);
            return chunks;
        }

        while (reader.Position < declaredEnd)
        {
            if (chunks.Count >= GetMaxChunkCount(options))
            {
                MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "riff.chunk.count.limit", "RIFF chunk count exceeded the configured tag count limit.", "RIFF", reader.Position, chunks.Count);
                return chunks;
            }

            long declaredRemaining = declaredEnd - reader.Position;
            if (declaredRemaining < 8)
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "riff.chunk.header.truncated", "RIFF chunk header was truncated by the declared container boundary.", "RIFF", reader.Position, 8);
                return chunks;
            }

            if (!reader.CanRead(8))
            {
                MetadataDiagnostic.AddTruncated(document.Diagnostics, "riff.chunk.header.truncated", "RIFF chunk header was truncated.", "RIFF", reader.Position, 8);
                return chunks;
            }

            RiffChunk? chunk = ReadChunk(document, reader, chunks.Count, declaredEnd, options);
            if (chunk == null)
            {
                return chunks;
            }

            chunks.Add(chunk);
        }

        return chunks;
    }

    internal static RiffChunk? ReadChunk(ImageMetadataDocument document, BoundedStreamReader reader, int index, long declaredEnd, ImageMetadataReaderOptions options)
    {
        long offset = reader.Position;
        byte[] typeBytes = reader.ReadBytes(4);
        string fourCc = MetadataStringReader.DecodeAscii(typeBytes);
        uint length = ReadUnsigned32(reader);
        bool hasPadding = (length & 1) != 0;
        long storedLength = (long)length + (hasPadding ? 1 : 0);
        bool typeValid = IsValidFourCc(typeBytes);

        if (!typeValid)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.chunk.type.invalid", "RIFF chunk FourCC contained a non-printable ASCII byte.", fourCc, offset, 4);
        }

        if (storedLength > declaredEnd - reader.Position || !reader.CanRead(storedLength))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "riff.chunk.truncated", "RIFF chunk payload or padding was truncated.", fourCc, offset, 8 + storedLength);
            return null;
        }

        RiffChunk chunk = CreateChunk(index, offset, length, fourCc, typeValid);
        if (length > 2147483647 || length > GetMaxSegmentBytes(options))
        {
            chunk.DataLimited = true;
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "riff.chunk.length.limit", "RIFF chunk length exceeded the configured segment limit.", fourCc, offset, length);
            reader.SkipBytes(length);
        }
        else
        {
            chunk.Data = reader.ReadBytes((int)length);
        }

        if (hasPadding)
        {
            int padding = reader.ReadByte();
            chunk.PaddingValid = padding == 0;
            if (!chunk.PaddingValid)
            {
                MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "riff.chunk.padding.invalid", "RIFF odd-sized chunk padding byte was not zero.", fourCc, chunk.PaddingOffset, 1);
            }
        }

        return chunk;
    }

    internal static RiffChunk CreateChunk(int index, long offset, uint length, string fourCc, bool typeValid)
    {
        RiffChunk chunk = new RiffChunk();
        chunk.Index = index;
        chunk.FourCc = fourCc;
        chunk.Offset = offset;
        chunk.Length = length;
        chunk.DataOffset = offset + 8;
        chunk.PaddingOffset = chunk.DataOffset + length;
        chunk.HasPadding = (length & 1) != 0;
        chunk.TypeValid = typeValid;
        return chunk;
    }

    internal static void AddHeaderDirectory(ImageMetadataDocument document, RiffHeader header, long actualLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.WebP, "Header", "RIFF WebP Header");
        directory.Path = "RIFF/WEBP/Header";
        directory.Offset = 0;
        directory.Length = HeaderLength;
        AddTag(directory, "Riff.Header.Signature", "Signature", MetadataValue.FromString(header.Signature), 0, 4, header.Signature);
        AddTag(directory, "Riff.Header.FileSize", "File Size", MetadataValue.FromUnsignedInteger(header.FileSize), 4, 4, "");
        AddTag(directory, "Riff.Header.FormType", "Form Type", MetadataValue.FromString(header.FormType), 8, 4, header.FormType);
        AddTag(directory, "Riff.Header.DeclaredByteLength", "Declared Byte Length", MetadataValue.FromInteger(header.DeclaredEnd), 4, 4, "");
        AddTag(directory, "Riff.Header.ActualByteLength", "Actual Byte Length", MetadataValue.FromInteger(actualLength), 0, HeaderLength, "");
    }

    internal static void AddChunkDirectories(ImageMetadataDocument document, List<RiffChunk> chunks)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        foreach (RiffChunk chunk in chunks)
        {
            AddChunkDirectory(document, chunk);
        }
    }

    internal static MetadataDirectory AddChunkDirectory(ImageMetadataDocument document, RiffChunk chunk)
    {
        int repeatIndex = CountChunkDirectories(document, chunk.FourCc);
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.WebP, chunk.FourCc, chunk.FourCc + " Chunk");
        directory.Path = "RIFF/WEBP/" + chunk.FourCc + "/" + repeatIndex.ToString();
        directory.Offset = chunk.Offset;
        directory.Length = 8 + chunk.Length + (chunk.HasPadding ? 1 : 0);
        directory.RepeatIndex = repeatIndex;
        string prefix = "Riff." + chunk.FourCc;
        AddTag(directory, prefix + ".Index", "Index", MetadataValue.FromInteger(chunk.Index), chunk.Offset, 4, "");
        AddTag(directory, prefix + ".Length", "Length", MetadataValue.FromUnsignedInteger((ulong)chunk.Length), chunk.Offset + 4, 4, "");
        AddTag(directory, prefix + ".DataOffset", "Data Offset", MetadataValue.FromInteger(chunk.DataOffset), chunk.Offset, 8, "");
        AddTag(directory, prefix + ".HasPadding", "Has Padding", MetadataValue.FromBoolean(chunk.HasPadding), chunk.PaddingOffset, chunk.HasPadding ? 1 : 0, "");
        AddTag(directory, prefix + ".PaddingValid", "Padding Valid", MetadataValue.FromBoolean(chunk.PaddingValid), chunk.PaddingOffset, chunk.HasPadding ? 1 : 0, "");
        AddTag(directory, prefix + ".TypeValid", "Type Valid", MetadataValue.FromBoolean(chunk.TypeValid), chunk.Offset, 4, "");
        AddTag(directory, prefix + ".DataLimited", "Data Limited", MetadataValue.FromBoolean(chunk.DataLimited), chunk.DataOffset, chunk.Length, "");
        return directory;
    }

    internal static int CountChunkDirectories(ImageMetadataDocument document, string fourCc)
    {
        int count = 0;
        if (document == null)
        {
            return count;
        }

        foreach (MetadataDirectory directory in document.Directories)
        {
            if (directory != null && directory.Family == MetadataFamily.WebP && directory.Group == fourCc)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static void ReportTrailingData(ImageMetadataDocument document, BoundedStreamReader reader, long declaredEnd)
    {
        if (reader != null && declaredEnd < reader.Length)
        {
            MetadataDiagnostic.AddWarningAt(document.Diagnostics, "riff.trailing.data", "RIFF contained bytes after the declared container boundary.", "RIFF", declaredEnd, reader.Length - declaredEnd);
        }
    }

    internal static bool IsValidFourCc(byte[] bytes)
    {
        if (bytes == null || bytes.Length != 4)
        {
            return false;
        }

        foreach (byte value in bytes)
        {
            if (value < 32 || value > 126)
            {
                return false;
            }
        }

        return true;
    }

    internal static uint ReadUnsigned32(BoundedStreamReader reader)
    {
        byte[] bytes = reader.ReadBytes(4);
        if (bytes.Length != 4)
        {
            return 0;
        }

        return (uint)bytes[0]
            | ((uint)bytes[1] << 8)
            | ((uint)bytes[2] << 16)
            | ((uint)bytes[3] << 24);
    }

    internal static MetadataTag AddTag(MetadataDirectory directory, string key, string name, MetadataValue value, long offset, long length, string displayValue)
    {
        MetadataTag tag = directory.AddTagWithValue(key, name, value);
        tag.Offset = offset;
        tag.Length = length;
        tag.DisplayValue = displayValue;
        return tag;
    }

    internal static long GetMaxSegmentBytes(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxSegmentBytes : options.MaxSegmentBytes;
    }

    internal static int GetMaxChunkCount(ImageMetadataReaderOptions options)
    {
        return options == null ? new ImageMetadataReaderOptions().MaxTagCount : options.MaxTagCount;
    }

    internal static void Finish(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader != null && reader.Source != null && reader.Source.CanSeek)
        {
            reader.SeekAbsolute(0);
        }

        if (reader != null && reader.Diagnostics != null)
        {
            foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
            {
                document.Diagnostics.Add(diagnostic);
            }
        }

        ImageMetadataNormalizerCore.BuildSummary(document);
    }
}

internal class RiffHeader
{
    internal string Signature;
    internal uint FileSize;
    internal string FormType;
    internal long DeclaredEnd;
    internal bool Valid;

    internal RiffHeader()
    {
        Signature = "";
        FileSize = 0;
        FormType = "";
        DeclaredEnd = 0;
        Valid = false;
    }
}
