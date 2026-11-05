using System.Collections.Generic;
using System.Text;

namespace image;

internal class JpegXmpExtractor
{
    internal const int App1Marker = 225;
    internal const string StandardXmpIdentifier = "http://ns.adobe.com/xap/1.0/\0";
    internal const string ExtendedXmpIdentifier = "http://ns.adobe.com/xmp/extension/\0";
    internal const int ExtendedGuidLength = 32;

    internal static void Extract(ImageMetadataDocument document, BoundedStreamReader reader, List<JpegMarkerSegment> segments, ImageMetadataReaderOptions? options)
    {
        if (document == null || reader == null || segments == null)
        {
            return;
        }

        if (options != null && !options.ParseXmp)
        {
            return;
        }

        List<JpegXmpExtensionChunk> chunks = new List<JpegXmpExtensionChunk>();

        foreach (JpegMarkerSegment segment in segments)
        {
            if (segment != null && segment.Marker == App1Marker)
            {
                ExtractSegment(document, reader, segment, options, chunks);
            }
        }

        ExtractExtendedProfiles(document, chunks, options);
    }

    internal static void ExtractSegment(ImageMetadataDocument document, BoundedStreamReader reader, JpegMarkerSegment segment, ImageMetadataReaderOptions? options, List<JpegXmpExtensionChunk> chunks)
    {
        byte[] payload = JpegExifExtractor.ReadPayload(document, reader, segment);

        if (JpegExifExtractor.HasIdentifier(payload, StandardXmpIdentifier))
        {
            ExtractStandardXmp(document, segment, payload, options);
            return;
        }

        if (JpegExifExtractor.HasIdentifier(payload, ExtendedXmpIdentifier))
        {
            ExtractExtendedChunk(document, segment, payload, chunks);
            return;
        }

        if (JpegExifExtractor.HasPartialIdentifier(payload, StandardXmpIdentifier))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.xmp.identifier.truncated", "JPEG APP1 XMP identifier was truncated.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
        }
    }

    internal static void ExtractStandardXmp(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, ImageMetadataReaderOptions? options)
    {
        int offset = StandardXmpIdentifier.Length;
        int length = payload.Length - offset;
        byte[] packet = JpegExifExtractor.CopyRange(payload, offset, length);
        long packetOffset = segment.PayloadOffset + offset;

        MetadataProfile profile = CreateXmpProfile("Xmp.APP1", "XMP", packetOffset, length, packet, options, document);
        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "JPEG/APP1/XMP");
        AddStandardDirectory(document, segment, packetOffset, length);
    }

    internal static void ExtractExtendedChunk(ImageMetadataDocument document, JpegMarkerSegment segment, byte[] payload, List<JpegXmpExtensionChunk> chunks)
    {
        int headerLength = ExtendedXmpIdentifier.Length + ExtendedGuidLength + 8;
        MetadataDirectory directory = AddExtendedChunkDirectory(document, segment);

        if (payload == null || payload.Length < headerLength)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.xmp.extended.truncated", "JPEG extended XMP APP1 payload was too short.", segment.Name, segment.PayloadOffset, segment.PayloadLength);
            return;
        }

        int guidOffset = ExtendedXmpIdentifier.Length;
        int fullLengthOffset = guidOffset + ExtendedGuidLength;
        int chunkOffsetOffset = fullLengthOffset + 4;
        int chunkDataOffset = chunkOffsetOffset + 4;
        string guid = AsciiString(payload, guidOffset, ExtendedGuidLength);
        long fullLength = ReadUInt32(payload, fullLengthOffset);
        long chunkOffset = ReadUInt32(payload, chunkOffsetOffset);
        byte[] chunkData = JpegExifExtractor.CopyRange(payload, chunkDataOffset, payload.Length - chunkDataOffset);

        directory.AddTagWithValue("Xmp.APP1.Extended.Guid", "GUID", MetadataValue.FromString(guid));
        directory.AddTagWithValue("Xmp.APP1.Extended.FullByteLength", "Full Byte Length", MetadataValue.FromInteger(fullLength));
        directory.AddTagWithValue("Xmp.APP1.Extended.ChunkOffset", "Chunk Offset", MetadataValue.FromInteger(chunkOffset));
        directory.AddTagWithValue("Xmp.APP1.Extended.ChunkByteLength", "Chunk Byte Length", MetadataValue.FromInteger(chunkData.Length));

        JpegXmpExtensionChunk chunk = new JpegXmpExtensionChunk();
        chunk.GuidText = guid;
        chunk.FullLength = fullLength;
        chunk.ChunkOffset = chunkOffset;
        chunk.Data = chunkData;
        chunk.PayloadOffset = segment.PayloadOffset + chunkDataOffset;
        chunk.SegmentOffset = segment.MarkerOffset;
        chunks.Add(chunk);
    }

    internal static void ExtractExtendedProfiles(ImageMetadataDocument document, List<JpegXmpExtensionChunk> chunks, ImageMetadataReaderOptions? options)
    {
        if (document == null || chunks == null)
        {
            return;
        }

        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk != null && IsFirstChunkWithGuid(chunks, chunk))
            {
                ExtractExtendedProfile(document, chunks, chunk.GuidText, options);
            }
        }
    }

    internal static void ExtractExtendedProfile(ImageMetadataDocument document, List<JpegXmpExtensionChunk> chunks, string guid, ImageMetadataReaderOptions? options)
    {
        long fullLength = FindFullLength(chunks, guid);
        if (fullLength <= 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.xmp.extended.length.invalid", "JPEG extended XMP full length was invalid.", "APP1", 0, fullLength);
            return;
        }

        if (fullLength > GetMaxXmlBytes(options))
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.xmp.extended.limit", "JPEG extended XMP packet exceeded the configured XML limit.", "APP1", 0, fullLength);
            return;
        }

        if (fullLength > 2147483647)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.xmp.extended.large", "JPEG extended XMP packet was too large to buffer.", "APP1", 0, fullLength);
            return;
        }

        byte[] packet = new byte[(int)fullLength];
        bool[] filled = new bool[(int)fullLength];

        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk != null && SameText(chunk.GuidText, guid))
            {
                CopyChunk(document, packet, filled, chunk);
            }
        }

        if (!IsComplete(filled))
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.xmp.extended.incomplete", "JPEG extended XMP chunks did not cover the complete packet.", "APP1", 0, fullLength);
            return;
        }

        MetadataProfile profile = CreateXmpProfile("Xmp.APP1.Extended." + guid, "Extended XMP", FindFirstPayloadOffset(chunks, guid), fullLength, packet, options, document);
        profile.SequenceCount = CountChunks(chunks, guid);
        document.AddProfile(profile);
        XmpPacketParser.ParseProfile(document, profile, options, "JPEG/APP1/ExtendedXMP");
    }

    internal static void CopyChunk(ImageMetadataDocument document, byte[] packet, bool[] filled, JpegXmpExtensionChunk chunk)
    {
        if (document == null || packet == null || filled == null || chunk == null || chunk.Data == null)
        {
            return;
        }

        if (chunk.ChunkOffset < 0 || chunk.ChunkOffset > packet.Length)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "jpeg.xmp.extended.offset.invalid", "JPEG extended XMP chunk offset was outside the packet.", "APP1", chunk.PayloadOffset, chunk.Data.Length);
            return;
        }

        if (chunk.Data.Length > packet.Length - chunk.ChunkOffset)
        {
            MetadataDiagnostic.AddTruncated(document.Diagnostics, "jpeg.xmp.extended.chunk.range", "JPEG extended XMP chunk exceeded the declared packet length.", "APP1", chunk.PayloadOffset, chunk.Data.Length);
            return;
        }

        int index = 0;
        while (index < chunk.Data.Length)
        {
            int target = (int)chunk.ChunkOffset + index;
            packet[target] = chunk.Data[index];
            filled[target] = true;
            index = index + 1;
        }
    }

    internal static MetadataProfile CreateXmpProfile(string key, string name, long offset, long length, byte[] packet, ImageMetadataReaderOptions? options, ImageMetadataDocument document)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = key;
        profile.Name = name;
        profile.MimeType = "application/rdf+xml";
        profile.Offset = offset;
        profile.Length = length;

        if (length <= GetMaxXmlBytes(options))
        {
            profile.Data = packet;
            profile.Text = Encoding.UTF8.GetString(packet);
        }
        else
        {
            profile.Data = new byte[0];
            profile.Text = "";
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "jpeg.xmp.profile.limit", "JPEG XMP packet exceeded the configured XML limit.", "APP1", offset, length);
        }

        return profile;
    }

    internal static MetadataDirectory AddStandardDirectory(ImageMetadataDocument document, JpegMarkerSegment segment, long packetOffset, int packetLength)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "APP1", "XMP");
        directory.Path = "JPEG/APP1/XMP";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        directory.AddTagWithValue("Xmp.APP1.Identifier", "Identifier", MetadataValue.FromString("XMP"));
        directory.AddTagWithValue("Xmp.APP1.PacketOffset", "Packet Offset", MetadataValue.FromInteger(packetOffset));
        directory.AddTagWithValue("Xmp.APP1.PacketByteLength", "Packet Byte Length", MetadataValue.FromInteger(packetLength));
        directory.AddTagWithValue("Xmp.APP1.Extended", "Extended", MetadataValue.FromBoolean(false));
        return directory;
    }

    internal static MetadataDirectory AddExtendedChunkDirectory(ImageMetadataDocument document, JpegMarkerSegment segment)
    {
        MetadataDirectory directory = document.AddDirectoryWithName(MetadataFamily.Xmp, "APP1", "Extended XMP Chunk");
        directory.Path = "JPEG/APP1/XMP/Extended";
        directory.Offset = segment.MarkerOffset;
        directory.Length = segment.SegmentLength;
        return directory;
    }

    internal static bool IsFirstChunkWithGuid(List<JpegXmpExtensionChunk> chunks, JpegXmpExtensionChunk candidate)
    {
        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk == candidate)
            {
                return true;
            }

            if (chunk != null && candidate != null && SameText(chunk.GuidText, candidate.GuidText))
            {
                return false;
            }
        }

        return false;
    }

    internal static long FindFullLength(List<JpegXmpExtensionChunk> chunks, string guid)
    {
        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk != null && SameText(chunk.GuidText, guid))
            {
                return chunk.FullLength;
            }
        }

        return 0;
    }

    internal static long FindFirstPayloadOffset(List<JpegXmpExtensionChunk> chunks, string guid)
    {
        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk != null && SameText(chunk.GuidText, guid))
            {
                return chunk.PayloadOffset;
            }
        }

        return -1;
    }

    internal static int CountChunks(List<JpegXmpExtensionChunk> chunks, string guid)
    {
        int count = 0;

        foreach (JpegXmpExtensionChunk chunk in chunks)
        {
            if (chunk != null && SameText(chunk.GuidText, guid))
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal static bool IsComplete(bool[] filled)
    {
        if (filled == null)
        {
            return false;
        }

        int index = 0;
        while (index < filled.Length)
        {
            if (!filled[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static string AsciiString(byte[] bytes, int offset, int length)
    {
        if (bytes == null || offset < 0 || length <= 0 || offset > bytes.Length)
        {
            return "";
        }

        int actualLength = length;
        if (actualLength > bytes.Length - offset)
        {
            actualLength = bytes.Length - offset;
        }

        return Encoding.ASCII.GetString(JpegExifExtractor.CopyRange(bytes, offset, actualLength));
    }

    internal static long ReadUInt32(byte[] bytes, int offset)
    {
        if (bytes == null || offset < 0 || offset + 3 >= bytes.Length)
        {
            return 0;
        }

        long value = 0;
        value = value + ((long)bytes[offset] * 16777216);
        value = value + ((long)bytes[offset + 1] * 65536);
        value = value + ((long)bytes[offset + 2] * 256);
        value = value + bytes[offset + 3];
        return value;
    }

    internal static bool SameText(string left, string right)
    {
        return string.Equals(left, right, System.StringComparison.Ordinal);
    }

    internal static long GetMaxXmlBytes(ImageMetadataReaderOptions? options)
    {
        if (options == null)
        {
            return new ImageMetadataReaderOptions().MaxXmlBytes;
        }

        return options.MaxXmlBytes;
    }
}
