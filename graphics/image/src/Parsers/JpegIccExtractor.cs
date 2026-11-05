using System.Collections.Generic;

namespace image;

internal class JpegIccExtractor
{
    internal const int App2Marker = 226;
    internal const string IccIdentifier = "ICC_PROFILE\0";

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions? options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        if (options != null && !options.ParseIcc)
        {
            return;
        }

        List<JpegIccChunk> chunks = new List<JpegIccChunk>();

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == App2Marker)
            {
                ExtractSegment(document, reader, segment, chunks);
            }
        }

        ExtractProfile(document, chunks, options);
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment, List<JpegIccChunk> chunks)
    {
        byte[] payload = ReadPayload(document, reader, segment);

        if (JpegExifExtractor.HasIdentifier(payload, IccIdentifier))
        {
            ExtractChunk(document, segment, payload, chunks);
            return;
        }

        if (JpegExifExtractor.HasPartialIdentifier(payload, IccIdentifier))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.icc.identifier.truncated", "JPEG APP2 ICC identifier was truncated.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }
    }

    internal static byte[] ReadPayload(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment)
    {
        if (document == null || reader == null || segment == null || segment.PayloadLength <= 0)
        {
            return new byte[0];
        }

        if (segment.PayloadLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.app2.payload.large", "JPEG APP2 payload was too large to buffer.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        if (!reader.SeekAbsolute(segment.PayloadOffset))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.app2.seek", "JPEG APP2 payload could not be reached.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return new byte[0];
        }

        byte[] payload = reader.ReadBytes((int)segment.PayloadLength);
        if (payload.Length < segment.PayloadLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.app2.truncated", "JPEG APP2 payload ended early.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }

        return payload;
    }

    internal static void ExtractChunk(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, List<JpegIccChunk> chunks)
    {
        int headerLength = IccIdentifier.Length + 2;
        MetadataDirectory directory = AddChunkDirectory(document, segment);

        if (payload == null || payload.Length < headerLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.icc.truncated", "JPEG APP2 ICC payload was too short.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int sequenceNumber = payload[IccIdentifier.Length];
        int sequenceCount = payload[IccIdentifier.Length + 1];
        byte[] data = JpegExifExtractor.CopyRange(payload, headerLength, payload.Length - headerLength);

        directory.AddTagWithValue("Icc.APP2.Identifier", "Identifier", MetadataValue.FromString("ICC_PROFILE"));
        directory.AddTagWithValue("Icc.APP2.SequenceNumber", "Sequence Number", MetadataValue.FromInteger(sequenceNumber));
        directory.AddTagWithValue("Icc.APP2.SequenceCount", "Sequence Count", MetadataValue.FromInteger(sequenceCount));
        directory.AddTagWithValue("Icc.APP2.ChunkByteLength", "Chunk Byte Length", MetadataValue.FromInteger(data.Length));

        if (sequenceNumber <= 0 || sequenceCount <= 0 || sequenceNumber > sequenceCount)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.icc.sequence.invalid", "JPEG ICC chunk sequence fields were invalid.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        JpegIccChunk chunk = new JpegIccChunk();
        chunk.SequenceNumber = sequenceNumber;
        chunk.SequenceCount = sequenceCount;
        chunk.Data = data;
        chunk.PayloadOffset = segment.PayloadOffset + headerLength;
        chunk.SegmentOffset = segment.MarkerOffset;
        chunks.Add(chunk);
    }

    internal static MetadataDirectory AddChunkDirectory(ImageMetadataDocument document, JpegMarkerSegment segment)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Icc, "APP2", "ICC Profile Chunk");
        directory.Path = "JPEG/APP2/ICC";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        return directory;
    }

    internal static void ExtractProfile(ImageMetadataDocument document, List<JpegIccChunk> chunks, ImageMetadataReaderOptions? options)
    {
        if (document == null || chunks == null || chunks.Count == 0)
        {
            return;
        }

        int sequenceCount = chunks[0].SequenceCount;

        if (!HasConsistentSequenceCount(chunks, sequenceCount))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.icc.sequence.count.mismatch", "JPEG ICC chunks did not agree on sequence count.", "APP2", 0, sequenceCount);
            return;
        }

        if (!HasCompleteSequence(chunks, sequenceCount))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.icc.sequence.incomplete", "JPEG ICC chunks did not contain every declared sequence number.", "APP2", 0, sequenceCount);
            return;
        }

        long totalLength = TotalLength(chunks, sequenceCount);
        if (totalLength > GetMaxProfileBytes(options))
        {
            AddProfile(document, chunks, sequenceCount, totalLength, new byte[0], options, true);
            return;
        }

        if (totalLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.icc.profile.large", "JPEG ICC profile was too large to buffer.", "APP2", 0, totalLength);
            return;
        }

        byte[] data = new byte[(int)totalLength];
        int writeOffset = 0;
        int sequenceNumber = 1;
        while (sequenceNumber <= sequenceCount)
        {
            JpegIccChunk? chunk = FindChunk(chunks, sequenceNumber);
            if (chunk == null)
            {
                return;
            }

            CopyChunkData(data, writeOffset, chunk.Data);
            writeOffset = writeOffset + chunk.Data.Length;
            sequenceNumber = sequenceNumber + 1;
        }

        AddProfile(document, chunks, sequenceCount, totalLength, data, options, false);
    }

    internal static void AddProfile(ImageMetadataDocument document, List<JpegIccChunk> chunks, int sequenceCount, long totalLength, byte[] data, ImageMetadataReaderOptions? options, bool limited)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Icc;
        profile.Family = MetadataFamily.Icc;
        profile.Key = "Icc.APP2";
        profile.Name = "ICC Profile";
        profile.MimeType = "application/vnd.iccprofile";
        profile.Offset = FindFirstPayloadOffset(chunks);
        profile.Length = totalLength;
        profile.SequenceNumber = 1;
        profile.SequenceCount = sequenceCount;
        profile.Data = data;
        document.AddProfile(profile);
        IccProfileParser.ParseProfile(document, profile, options, "JPEG/APP2/ICC", "APP2");

        if (limited)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.icc.profile.limit", "JPEG ICC profile exceeded the configured profile limit.", "APP2", profile.Offset, totalLength);
        }
    }

    internal static bool HasConsistentSequenceCount(List<JpegIccChunk> chunks, int sequenceCount)
    {
        foreach (JpegIccChunk chunk in chunks)
        {
            if (chunk != null && chunk.SequenceCount != sequenceCount)
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasCompleteSequence(List<JpegIccChunk> chunks, int sequenceCount)
    {
        int sequenceNumber = 1;
        while (sequenceNumber <= sequenceCount)
        {
            if (FindChunk(chunks, sequenceNumber) == null)
            {
                return false;
            }

            sequenceNumber = sequenceNumber + 1;
        }

        return true;
    }

    internal static JpegIccChunk? FindChunk(List<JpegIccChunk> chunks, int sequenceNumber)
    {
        foreach (JpegIccChunk chunk in chunks)
        {
            if (chunk != null && chunk.SequenceNumber == sequenceNumber)
            {
                return chunk;
            }
        }

        return null;
    }

    internal static long TotalLength(List<JpegIccChunk> chunks, int sequenceCount)
    {
        long total = 0;
        int sequenceNumber = 1;
        while (sequenceNumber <= sequenceCount)
        {
            JpegIccChunk? chunk = FindChunk(chunks, sequenceNumber);
            if (chunk != null && chunk.Data != null)
            {
                total = total + chunk.Data.Length;
            }

            sequenceNumber = sequenceNumber + 1;
        }

        return total;
    }

    internal static long FindFirstPayloadOffset(List<JpegIccChunk> chunks)
    {
        JpegIccChunk? first = FindChunk(chunks, 1);
        if (first != null)
        {
            return first.PayloadOffset;
        }

        foreach (JpegIccChunk chunk in chunks)
        {
            if (chunk != null)
            {
                return chunk.PayloadOffset;
            }
        }

        return -1;
    }

    internal static void CopyChunkData(byte[] target, int offset, byte[] data)
    {
        if (target == null || data == null)
        {
            return;
        }

        int index = 0;
        while (index < data.Length)
        {
            target[offset + index] = data[index];
            index = index + 1;
        }
    }

    internal static long GetMaxProfileBytes(ImageMetadataReaderOptions? options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxProfileBytes;
        }

        return options.MaxProfileBytes;
    }
}
