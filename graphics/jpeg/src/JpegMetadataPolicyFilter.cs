using System.Collections.Generic;

namespace jpeg;

internal class JpegMetadataPolicyFilter
{
    internal JpegMetadata CreateMetadataForPolicy(JpegMetadata metadata, JpegMetadataPolicy policy)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        if (policy == JpegMetadataPolicy.PreserveAll)
        {
            return metadata;
        }

        JpegMetadata result = new JpegMetadata();
        if (policy == JpegMetadataPolicy.StripAll)
        {
            return result;
        }

        result.RawMarkersAreExplicit = metadata.RawMarkersAreExplicit;
        CopyStructuredMetadata(metadata, result, policy);
        CopyRawMarkers(metadata, result, policy);
        CopyMetadataSegments(metadata, result, policy);
        return result;
    }

    internal bool ShouldParseMetadata(JpegMetadataPolicy policy)
    {
        return policy != JpegMetadataPolicy.StripAll;
    }

    internal bool ShouldKeepRawMarker(JpegRawMarker marker, JpegMetadataPolicy policy)
    {
        if (marker == null)
        {
            throw new System.ArgumentNullException("marker");
        }

        return ShouldKeepPayload(marker.Value, marker.Payload, policy);
    }

    internal bool ShouldKeepSegment(JpegMetadataSegment segment, JpegMetadataPolicy policy)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        return ShouldKeepPayload((int)segment.Marker, segment.Data, policy);
    }

    internal bool ShouldKeepPayload(int markerValue, byte[] payload, JpegMetadataPolicy policy)
    {
        if (policy == JpegMetadataPolicy.PreserveAll)
        {
            return true;
        }

        if (policy == JpegMetadataPolicy.StripAll)
        {
            return false;
        }

        if (policy == JpegMetadataPolicy.ReplaceSelected)
        {
            return false;
        }

        if (policy == JpegMetadataPolicy.StripUnsafe)
        {
            return IsSafePayload(markerValue, payload);
        }

        if (policy == JpegMetadataPolicy.PreserveKnown)
        {
            return IsKnownPayload(markerValue, payload);
        }

        return false;
    }

    internal bool IsKnownPayload(int markerValue, byte[] payload)
    {
        JpegMetadataWriter writer = new JpegMetadataWriter();
        int category = writer.GetPayloadCategory(markerValue, payload);
        if (category != JpegMetadataWriter.CategoryUnknown)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerApp2)
        {
            JpegMpfMetadataParser mpfParser = new JpegMpfMetadataParser();
            if (mpfParser.IsMpf(payload))
            {
                return true;
            }
        }

        if (markerValue == JpegConstants.MarkerApp13)
        {
            JpegPhotoshopMetadataParser photoshopParser = new JpegPhotoshopMetadataParser();
            if (photoshopParser.IsPhotoshop(payload))
            {
                return true;
            }
        }

        return false;
    }

    internal bool IsSafePayload(int markerValue, byte[] payload)
    {
        JpegMetadataWriter writer = new JpegMetadataWriter();
        int category = writer.GetPayloadCategory(markerValue, payload);
        if (category == JpegMetadataWriter.CategoryJfif)
        {
            return true;
        }

        if (category == JpegMetadataWriter.CategoryJfxx)
        {
            return true;
        }

        if (category == JpegMetadataWriter.CategoryIcc)
        {
            return true;
        }

        if (category == JpegMetadataWriter.CategoryAdobe)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldKeepSafeStructuredMetadata(JpegMetadataPolicy policy)
    {
        if (policy == JpegMetadataPolicy.PreserveKnown)
        {
            return true;
        }

        if (policy == JpegMetadataPolicy.StripUnsafe)
        {
            return true;
        }

        if (policy == JpegMetadataPolicy.ReplaceSelected)
        {
            return true;
        }

        return false;
    }

    internal bool ShouldKeepUnsafeStructuredMetadata(JpegMetadataPolicy policy)
    {
        if (policy == JpegMetadataPolicy.PreserveKnown)
        {
            return true;
        }

        if (policy == JpegMetadataPolicy.ReplaceSelected)
        {
            return true;
        }

        return false;
    }

    internal void CopyStructuredMetadata(JpegMetadata source, JpegMetadata target, JpegMetadataPolicy policy)
    {
        if (ShouldKeepSafeStructuredMetadata(policy))
        {
            CopySafeStructuredMetadata(source, target);
        }

        if (ShouldKeepUnsafeStructuredMetadata(policy))
        {
            CopyUnsafeStructuredMetadata(source, target);
        }
    }

    internal void CopySafeStructuredMetadata(JpegMetadata source, JpegMetadata target)
    {
        target.HasJfif = source.HasJfif;
        target.Jfif = source.Jfif;
        target.HasJfxx = source.HasJfxx;
        target.Jfxx = CopyJfxxList(source.Jfxx);
        target.HasIccProfileChunks = source.HasIccProfileChunks;
        target.HasIccProfile = source.HasIccProfile;
        target.IccProfileChunks = CopyIccChunkList(source.IccProfileChunks);
        target.IccProfile = CopyBytes(source.IccProfile);
        target.Icc = source.Icc;
        target.HasCicp = source.HasCicp;
        target.Cicp = source.Cicp;
        target.JfifMajorVersion = source.JfifMajorVersion;
        target.JfifMinorVersion = source.JfifMinorVersion;
        target.DensityUnit = source.DensityUnit;
        target.XDensity = source.XDensity;
        target.YDensity = source.YDensity;
        target.HasDensityDpi = source.HasDensityDpi;
        target.XDpi = source.XDpi;
        target.YDpi = source.YDpi;
        target.HasAdobe = source.HasAdobe;
        target.Adobe = source.Adobe;
        target.AdobeVersion = source.AdobeVersion;
        target.AdobeTransform = source.AdobeTransform;
    }

    internal void CopyUnsafeStructuredMetadata(JpegMetadata source, JpegMetadata target)
    {
        target.HasExif = source.HasExif;
        target.ExifMetadata = source.ExifMetadata;
        target.Exif = CopyBytes(source.Exif);
        target.HasXmp = source.HasXmp;
        target.XmpMetadata = source.XmpMetadata;
        target.Xmp = CopyBytes(source.Xmp);
        target.HasExtendedXmpChunks = source.HasExtendedXmpChunks;
        target.HasExtendedXmp = source.HasExtendedXmp;
        target.ExtendedXmpChunks = CopyExtendedXmpChunkList(source.ExtendedXmpChunks);
        target.ExtendedXmpMetadata = source.ExtendedXmpMetadata;
        target.ExtendedXmp = CopyBytes(source.ExtendedXmp);
        target.HasMpf = source.HasMpf;
        target.MpfMetadata = source.MpfMetadata;
        target.Mpf = CopyBytes(source.Mpf);
        target.HasPhotoshop = source.HasPhotoshop;
        target.Photoshop = source.Photoshop;
        target.HasIptc = source.HasIptc;
        target.Iptc = CopyBytes(source.Iptc);
        target.Comments = CopyStringList(source.Comments);
    }

    internal void CopyRawMarkers(JpegMetadata source, JpegMetadata target, JpegMetadataPolicy policy)
    {
        foreach (JpegRawMarker marker in source.RawMarkers)
        {
            if ((policy == JpegMetadataPolicy.ReplaceSelected && source.RawMarkersAreExplicit)
                || ShouldKeepRawMarker(marker, policy))
            {
                target.RawMarkers.Add(CopyRawMarker(marker));
            }
        }
    }

    internal void CopyMetadataSegments(JpegMetadata source, JpegMetadata target, JpegMetadataPolicy policy)
    {
        foreach (JpegMetadataSegment segment in source.Segments)
        {
            if (ShouldKeepSegment(segment, policy))
            {
                target.Segments.Add(CopyMetadataSegment(segment));
            }
        }
    }

    internal long CalculateRawPayloadBytes(JpegMetadata metadata)
    {
        if (metadata == null)
        {
            throw new System.ArgumentNullException("metadata");
        }

        long total = 0;
        foreach (JpegRawMarker marker in metadata.RawMarkers)
        {
            total = total + marker.PayloadLength;
        }

        return total;
    }

    internal List<JpegJfxxMetadata> CopyJfxxList(List<JpegJfxxMetadata> source)
    {
        List<JpegJfxxMetadata> result = new List<JpegJfxxMetadata>();
        foreach (JpegJfxxMetadata item in source)
        {
            result.Add(item);
        }

        return result;
    }

    internal List<JpegIccProfileChunk> CopyIccChunkList(List<JpegIccProfileChunk> source)
    {
        List<JpegIccProfileChunk> result = new List<JpegIccProfileChunk>();
        foreach (JpegIccProfileChunk item in source)
        {
            result.Add(item);
        }

        return result;
    }

    internal List<JpegExtendedXmpChunk> CopyExtendedXmpChunkList(List<JpegExtendedXmpChunk> source)
    {
        List<JpegExtendedXmpChunk> result = new List<JpegExtendedXmpChunk>();
        foreach (JpegExtendedXmpChunk item in source)
        {
            result.Add(item);
        }

        return result;
    }

    internal List<string> CopyStringList(List<string> source)
    {
        List<string> result = new List<string>();
        foreach (string item in source)
        {
            result.Add(item);
        }

        return result;
    }

    internal JpegRawMarker CopyRawMarker(JpegRawMarker source)
    {
        JpegRawMarker result = new JpegRawMarker();
        result.Marker = source.Marker;
        result.Value = source.Value;
        result.Code = source.Code;
        result.Order = source.Order;
        result.Offset = source.Offset;
        result.PayloadLength = source.PayloadLength;
        result.Payload = CopyBytes(source.Payload);
        result.IsApplicationMarker = source.IsApplicationMarker;
        result.IsComment = source.IsComment;
        result.IsUnknownMarker = source.IsUnknownMarker;
        return result;
    }

    internal JpegMetadataSegment CopyMetadataSegment(JpegMetadataSegment source)
    {
        JpegMetadataSegment result = new JpegMetadataSegment();
        result.Marker = source.Marker;
        result.Offset = source.Offset;
        result.Order = source.Order;
        result.Data = CopyBytes(source.Data);
        return result;
    }

    internal byte[] CopyBytes(byte[]? source)
    {
        if (source == null)
        {
            return new byte[0];
        }

        byte[] result = new byte[source.Length];
        if (source.Length > 0)
        {
            System.Array.Copy(source, 0, result, 0, source.Length);
        }

        return result;
    }
}
