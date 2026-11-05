namespace jpeg;

internal class JpegHierarchicalMode
{
    internal bool IsHierarchicalMarker(int markerValue)
    {
        if (markerValue == JpegConstants.MarkerDefineHierarchicalProgression)
        {
            return true;
        }

        if (markerValue == JpegConstants.MarkerExpandReferenceComponents)
        {
            return true;
        }

        return false;
    }

    internal string GetUnsupportedMessage(int markerValue)
    {
        if (markerValue == JpegConstants.MarkerDefineHierarchicalProgression)
        {
            return "Hierarchical JPEG progression is recognized but not supported for decoding.";
        }

        if (markerValue == JpegConstants.MarkerExpandReferenceComponents)
        {
            return "Hierarchical JPEG reference component expansion is recognized but not supported for decoding.";
        }

        return "Hierarchical JPEG mode is recognized but not supported for decoding.";
    }
}
