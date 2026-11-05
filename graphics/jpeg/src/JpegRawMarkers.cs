using System.Collections.Generic;

namespace jpeg;

internal class JpegRawMarkerCollection
{
    internal List<JpegRawMarker> Markers = new List<JpegRawMarker>();
    internal int TotalPayloadBytes;

    internal bool AddIfPreserved(JpegMarkerSegment segment)
    {
        if (!ShouldPreserve(segment))
        {
            return false;
        }

        JpegRawMarker marker = CreateRawMarker(segment);
        Markers.Add(marker);
        TotalPayloadBytes = TotalPayloadBytes + marker.PayloadLength;
        return true;
    }

    internal JpegRawMarker CreateRawMarker(JpegMarkerSegment segment)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        JpegRawMarker marker = new JpegRawMarker();
        marker.Marker = segment.Marker;
        marker.Value = segment.Value;
        marker.Code = segment.Code;
        marker.Order = segment.Order;
        marker.Offset = segment.MarkerOffset;
        marker.Payload = CopyPayload(segment.Payload);
        marker.PayloadLength = marker.Payload.Length;
        marker.IsApplicationMarker = IsApplicationMarker(segment.Value);
        marker.IsComment = IsCommentMarker(segment.Value);
        marker.IsUnknownMarker = IsUnknownMarker(segment.Value);
        return marker;
    }

    internal bool ShouldPreserve(JpegMarkerSegment segment)
    {
        if (segment == null)
        {
            throw new System.ArgumentNullException("segment");
        }

        if (IsApplicationMarker(segment.Value))
        {
            return true;
        }

        if (IsCommentMarker(segment.Value))
        {
            return true;
        }

        if (IsUnknownMarker(segment.Value))
        {
            return true;
        }

        return false;
    }

    internal byte[] CopyPayload(byte[]? payload)
    {
        if (payload == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[payload.Length];
        System.Array.Copy(payload, 0, copy, 0, payload.Length);
        return copy;
    }

    internal bool IsApplicationMarker(int markerValue)
    {
        return markerValue >= JpegConstants.MarkerApp0 && markerValue <= JpegConstants.MarkerApp15;
    }

    internal bool IsCommentMarker(int markerValue)
    {
        return markerValue == JpegConstants.MarkerComment;
    }

    internal bool IsUnknownMarker(int markerValue)
    {
        if (markerValue < 0xFF00 || markerValue > 0xFFFF)
        {
            return false;
        }

        if (IsKnownMarker(markerValue))
        {
            return false;
        }

        return true;
    }

    internal bool IsKnownMarker(int markerValue)
    {
        if (markerValue == JpegConstants.MarkerTemporary)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame0 && markerValue <= JpegConstants.MarkerStartOfFrame15)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerRestart0 && markerValue <= JpegConstants.MarkerExpandReferenceComponents)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerApp0 && markerValue <= JpegConstants.MarkerApp15)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerJpegLs)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerComment)
        {
            return true;
        }

        return false;
    }
}
