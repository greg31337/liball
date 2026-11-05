using System.Collections.Generic;

namespace jpeg;

internal class JpegMetadataUpdater
{
    internal JpegMetadataContainerEditor ContainerEditor = new JpegMetadataContainerEditor();
    internal JpegMetadataPolicyFilter PolicyFilter = new JpegMetadataPolicyFilter();
    internal JpegMetadataWriter MetadataWriter = new JpegMetadataWriter();

    internal byte[] SetComments(byte[] data, List<string> comments)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = SetCommentsInMetadata(ReadMetadataForUpdate(data), comments);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] SetIccProfile(byte[] data, byte[] profile)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = SetIccProfileInMetadata(ReadMetadataForUpdate(data), profile);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] SetExif(byte[] data, byte[] exifPayload)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = SetExifInMetadata(ReadMetadataForUpdate(data), exifPayload);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] ReplaceExifFields(byte[] data, JpegExifMetadata metadata)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        byte[] exifPayload = new JpegExifMetadataWriter().WritePayload(metadata);
        JpegMetadata replacement = SetExifInMetadata(ReadMetadataForUpdate(data), exifPayload);
        return ContainerEditor.EditMarkers(data, replacement.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] SetXmp(byte[] data, byte[] xmpPayload)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = SetXmpInMetadata(ReadMetadataForUpdate(data), xmpPayload);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] ReplaceXmp(byte[] data, JpegXmpMetadata metadata)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        byte[] payload = CreateStandardXmpPayload(metadata.PacketData);
        return SetXmp(data, payload);
    }

    internal byte[] RemoveXmp(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = CopyMetadataWithoutXmpForm(ReadMetadataForUpdate(data), true);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] ReplaceExtendedXmp(byte[] data, JpegExtendedXmpMetadata metadata)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegMetadata replacement = SetExtendedXmpInMetadata(ReadMetadataForUpdate(data), metadata);
        return ContainerEditor.EditMarkers(data, replacement.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal byte[] RemoveExtendedXmp(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        JpegMetadata metadata = CopyMetadataWithoutXmpForm(ReadMetadataForUpdate(data), false);
        return ContainerEditor.EditMarkers(data, metadata.RawMarkers, JpegMetadataPolicy.ReplaceSelected);
    }

    internal JpegMetadata ReadMetadataForUpdate(byte[] data)
    {
        JpegMetadata metadata = new JpegMetadata();
        metadata.RawMarkers = ContainerEditor.ReadMetadataMarkers(data);
        metadata.RawMarkersAreExplicit = true;
        return metadata;
    }

    internal JpegMetadata SetCommentsInMetadata(JpegMetadata metadata, List<string> comments)
    {
        if (comments == null)
        {
            throw new System.ArgumentNullException("comments");
        }

        JpegMetadata result = CopyMetadataWithoutCategory(metadata, JpegMetadataWriter.CategoryComment);
        result.Comments = PolicyFilter.CopyStringList(comments);
        JpegCommentWriter writer = new JpegCommentWriter();
        foreach (string comment in comments)
        {
            result.RawMarkers.Add(CreateRawMarker(JpegMarker.Comment, JpegConstants.MarkerComment, writer.WritePayload(comment)));
        }

        return result;
    }

    internal JpegMetadata SetIccProfileInMetadata(JpegMetadata metadata, byte[] profile)
    {
        JpegIccProfileChunkWriter writer = new JpegIccProfileChunkWriter();
        List<JpegIccProfileChunk> chunks = writer.CreateChunks(profile);
        JpegMetadata result = CopyMetadataWithoutCategory(metadata, JpegMetadataWriter.CategoryIcc);
        result.HasIccProfile = true;
        result.HasIccProfileChunks = true;
        result.IccProfile = PolicyFilter.CopyBytes(profile);
        result.IccProfileChunks = chunks;

        int index = 0;
        while (index < chunks.Count)
        {
            result.RawMarkers.Add(CreateRawMarker(JpegMarker.App2, JpegConstants.MarkerApp2, writer.WritePayload(chunks[index])));
            index = index + 1;
        }

        return result;
    }

    internal JpegMetadata SetExifInMetadata(JpegMetadata metadata, byte[] exifPayload)
    {
        ValidateExifPayload(exifPayload);
        JpegMetadata result = CopyMetadataWithoutCategory(metadata, JpegMetadataWriter.CategoryExif);
        result.HasExif = true;
        result.Exif = PolicyFilter.CopyBytes(exifPayload);
        result.ExifMetadata.RawPayload = PolicyFilter.CopyBytes(exifPayload);
        result.RawMarkers.Add(CreateRawMarker(JpegMarker.App1, JpegConstants.MarkerApp1, exifPayload));
        return result;
    }

    internal JpegMetadata SetXmpInMetadata(JpegMetadata metadata, byte[] xmpPayload)
    {
        ValidateXmpPayload(xmpPayload);
        JpegMetadata result = CopyMetadataWithoutXmpForm(metadata, true);
        JpegXmpMetadata parsed = new JpegXmpMetadataParser().Parse(xmpPayload);
        result.HasXmp = true;
        result.Xmp = PolicyFilter.CopyBytes(xmpPayload);
        result.XmpMetadata = parsed;
        result.RawMarkers.Add(CreateRawMarker(JpegMarker.App1, JpegConstants.MarkerApp1, xmpPayload));
        return result;
    }

    internal JpegMetadata SetExtendedXmpInMetadata(JpegMetadata metadata, JpegExtendedXmpMetadata replacement)
    {
        JpegExtendedXmpChunkWriter writer = new JpegExtendedXmpChunkWriter();
        List<JpegExtendedXmpChunk> chunks = writer.CreateChunks(replacement.PacketData, replacement.Guid);
        JpegExtendedXmpMetadata assembled = new JpegExtendedXmpAssembler().Inspect(chunks);
        JpegMetadata result = CopyMetadataWithoutXmpForm(metadata, false);
        result.HasExtendedXmpChunks = true;
        result.HasExtendedXmp = true;
        result.ExtendedXmpChunks = chunks;
        result.ExtendedXmpMetadata = assembled;
        result.ExtendedXmp = PolicyFilter.CopyBytes(replacement.PacketData);
        foreach (JpegExtendedXmpChunk chunk in chunks)
        {
            result.RawMarkers.Add(CreateRawMarker(JpegMarker.App1, JpegConstants.MarkerApp1, writer.WritePayload(chunk)));
        }

        return result;
    }

    internal JpegMetadata CopyMetadataWithoutXmpForm(JpegMetadata metadata, bool standard)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegMetadata result = new JpegMetadata();
        JpegXmpMetadataParser standardParser = new JpegXmpMetadataParser();
        JpegExtendedXmpChunkParser extendedParser = new JpegExtendedXmpChunkParser();
        foreach (JpegRawMarker marker in metadata.RawMarkers)
        {
            bool remove = false;
            if (marker.Value == JpegConstants.MarkerApp1)
            {
                if (standard && standardParser.IsXmp(marker.Payload))
                {
                    remove = true;
                }

                if (!standard && extendedParser.IsExtendedXmp(marker.Payload))
                {
                    remove = true;
                }
            }

            if (!remove)
            {
                result.RawMarkers.Add(PolicyFilter.CopyRawMarker(marker));
            }
        }

        result.Segments = CopyMetadataSegments(metadata.Segments);
        return result;
    }

    internal JpegMetadata CopyMetadataWithoutCategory(JpegMetadata metadata, int category)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        JpegMetadata result = new JpegMetadata();
        foreach (JpegRawMarker marker in metadata.RawMarkers)
        {
            if (MetadataWriter.GetRawMarkerCategory(marker) != category)
            {
                result.RawMarkers.Add(PolicyFilter.CopyRawMarker(marker));
            }
        }

        result.Segments = CopyMetadataSegments(metadata.Segments);
        return result;
    }

    internal List<JpegMetadataSegment> CopyMetadataSegments(List<JpegMetadataSegment>? source)
    {
        List<JpegMetadataSegment> result = new List<JpegMetadataSegment>();
        if (source == null)
        {
            return result;
        }

        foreach (JpegMetadataSegment segment in source)
        {
            result.Add(PolicyFilter.CopyMetadataSegment(segment));
        }

        return result;
    }

    internal JpegRawMarker CreateRawMarker(JpegMarker marker, int value, byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        JpegRawMarker raw = new JpegRawMarker();
        raw.Marker = marker;
        raw.Value = value;
        raw.Code = value & 255;
        raw.Payload = PolicyFilter.CopyBytes(payload);
        raw.PayloadLength = raw.Payload.Length;
        raw.IsApplicationMarker = raw.Value >= JpegConstants.MarkerApp0 && raw.Value <= JpegConstants.MarkerApp15;
        raw.IsComment = raw.Value == JpegConstants.MarkerComment;
        raw.IsUnknownMarker = raw.Value < JpegConstants.MarkerApp0 || raw.Value > JpegConstants.MarkerComment;
        return raw;
    }

    internal void ValidateExifPayload(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        JpegExifMetadataParser parser = new JpegExifMetadataParser();
        if (!parser.IsExif(payload))
        {
            throw JpegWriteValidationException.Create("Exif update requires an APP1 Exif payload.");
        }
    }

    internal void ValidateXmpPayload(byte[] payload)
    {
        if (payload == null)
        {
            throw new System.ArgumentNullException("payload");
        }

        JpegXmpMetadataParser parser = new JpegXmpMetadataParser();
        if (!parser.IsXmp(payload))
        {
            throw JpegWriteValidationException.Create("XMP update requires an APP1 XMP payload.");
        }
    }

    internal byte[] CreateStandardXmpPayload(byte[] packet)
    {
        if (packet == null)
        {
            throw new System.ArgumentNullException("packet");
        }

        byte[] identifier = new JpegXmpMetadataParser().GetIdentifier();
        if (packet.Length > JpegConstants.MaximumMarkerPayloadLength - identifier.Length)
        {
            throw JpegWriteValidationException.Create("XMP packet exceeds the maximum APP1 payload length.");
        }

        byte[] payload = new byte[identifier.Length + packet.Length];
        System.Array.Copy(identifier, 0, payload, 0, identifier.Length);
        if (packet.Length > 0)
        {
            System.Array.Copy(packet, 0, payload, identifier.Length, packet.Length);
        }

        return payload;
    }
}
