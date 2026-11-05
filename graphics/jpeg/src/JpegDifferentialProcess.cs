namespace jpeg;

internal class JpegDifferentialProcess
{
    internal bool IsDifferentialMarker(int markerValue)
    {
        if (markerValue >= JpegConstants.MarkerStartOfFrame5 && markerValue <= JpegConstants.MarkerStartOfFrame7)
        {
            return true;
        }

        if (markerValue >= JpegConstants.MarkerStartOfFrame13 && markerValue <= JpegConstants.MarkerStartOfFrame15)
        {
            return true;
        }

        return false;
    }

    internal bool IsDifferentialProcess(JpegProcess process)
    {
        return IsDifferentialMarker((int)process);
    }

    internal bool RequiresCorpusBeforeDecoderSupport(JpegProcess process)
    {
        return IsDifferentialProcess(process);
    }

    internal string GetUnsupportedMessage(JpegProcess process)
    {
        if (process == JpegProcess.DifferentialSequentialDct)
        {
            return "Differential sequential DCT JPEG is recognized but not supported for decoding.";
        }

        if (process == JpegProcess.DifferentialProgressiveDct)
        {
            return "Differential progressive DCT JPEG is recognized but not supported for decoding.";
        }

        if (process == JpegProcess.DifferentialLosslessSequential)
        {
            return "Differential lossless Huffman JPEG is recognized but not supported for decoding.";
        }

        if (process == JpegProcess.DifferentialSequentialDctArithmetic)
        {
            return "Differential sequential DCT arithmetic JPEG is recognized but not supported for decoding.";
        }

        if (process == JpegProcess.DifferentialProgressiveDctArithmetic)
        {
            return "Differential progressive DCT arithmetic JPEG is recognized but not supported for decoding.";
        }

        if (process == JpegProcess.DifferentialLosslessSequentialArithmetic)
        {
            return "Differential lossless arithmetic JPEG is recognized but not supported for decoding.";
        }

        return "Differential JPEG process is recognized but not supported for decoding.";
    }
}
