namespace jpeg;

internal class JpegSmartScaleMode
{
    internal bool IsSmartScaleMarker(int markerValue)
    {
        return markerValue == JpegConstants.MarkerJpegExtension;
    }

    internal string GetUnsupportedMessage()
    {
        return "JPEG SmartScale/JPEG extension marker is recognized but not supported for decoding.";
    }
}
