namespace jpeg;

internal class JpegLsMode
{
    internal bool IsJpegLsMarker(int markerValue)
    {
        return markerValue == JpegConstants.MarkerJpegLs;
    }

    internal string GetUnsupportedMessage()
    {
        return "JPEG-LS is recognized but not supported for decoding.";
    }
}
