namespace jpeg;

internal class JpegScannedMarker
{
    internal JpegMarker Marker;
    internal int Value;
    internal int Code;
    internal long Offset;
    internal bool FoundAfterEntropyData;
    internal long EntropyBytesSkipped;
}

internal class JpegMarkerScanner
{
    internal JpegByteReader Reader = new JpegByteReader(new byte[0]);

    internal JpegMarkerScanner(byte[] data)
    {
        Reader = new JpegByteReader(data);
    }

    internal static JpegMarkerScanner FromStream(System.IO.Stream stream)
    {
        JpegMarkerScanner scanner = new JpegMarkerScanner(new byte[0]);
        scanner.Reader = JpegByteReader.FromStream(stream);
        return scanner;
    }

    internal JpegScannedMarker ReadStartOfImage()
    {
        long offset = Reader.AbsoluteOffset;
        int value = Reader.ReadUInt16();
        if (value != JpegConstants.MarkerStartOfImage)
        {
            throw JpegInvalidDataException.Create("JPEG stream must start with the SOI marker.");
        }

        return CreateMarker(value, offset, false, 0);
    }

    internal JpegScannedMarker ReadNextMarker()
    {
        while (true)
        {
            long offset = Reader.AbsoluteOffset;
            int value = Reader.ReadByte();
            if (value != JpegConstants.MarkerPrefix)
            {
                continue;
            }

            int code = ReadMarkerCodeAfterPrefix();
            if (code == JpegConstants.MarkerStuffedByte)
            {
                throw JpegInvalidDataException.Create("Stuffed entropy byte found while scanning marker boundary.");
            }

            return CreateMarker((JpegConstants.MarkerPrefix << JpegConstants.MarkerPrefixShift) | code, offset, false, 0);
        }
    }

    internal JpegScannedMarker ReadEntropyMarker()
    {
        long entropyStart = Reader.AbsoluteOffset;
        while (true)
        {
            long offset = Reader.AbsoluteOffset;
            int value = Reader.ReadByte();
            if (value != JpegConstants.MarkerPrefix)
            {
                continue;
            }

            int code = ReadMarkerCodeAfterPrefix();
            if (code == JpegConstants.MarkerStuffedByte)
            {
                continue;
            }

            return CreateMarker((JpegConstants.MarkerPrefix << JpegConstants.MarkerPrefixShift) | code, offset, true, offset - entropyStart);
        }
    }

    internal bool IsRestartMarker(JpegScannedMarker marker)
    {
        return marker.Value >= JpegConstants.MarkerRestart0 && marker.Value <= JpegConstants.MarkerRestart7;
    }

    internal int ReadMarkerCodeAfterPrefix()
    {
        int code = Reader.ReadByte();
        while (code == JpegConstants.MarkerPrefix)
        {
            code = Reader.ReadByte();
        }

        return code;
    }

    internal JpegScannedMarker CreateMarker(int value, long offset, bool foundAfterEntropyData, long entropyBytesSkipped)
    {
        JpegScannedMarker marker = new JpegScannedMarker();
        marker.Value = value;
        marker.Code = value & 255;
        marker.Marker = ResolveMarkerKind(value);
        marker.Offset = offset;
        marker.FoundAfterEntropyData = foundAfterEntropyData;
        marker.EntropyBytesSkipped = entropyBytesSkipped;
        return marker;
    }

    internal JpegMarker ResolveMarkerKind(int value)
    {
        if (value == JpegConstants.MarkerTemporary
            || value == JpegConstants.MarkerStartOfFrame0
            || value == JpegConstants.MarkerStartOfFrame1
            || value == JpegConstants.MarkerStartOfFrame2
            || value == JpegConstants.MarkerStartOfFrame3
            || value == JpegConstants.MarkerDefineHuffmanTable
            || value == JpegConstants.MarkerStartOfFrame5
            || value == JpegConstants.MarkerStartOfFrame6
            || value == JpegConstants.MarkerStartOfFrame7
            || value == JpegConstants.MarkerJpegExtension
            || value == JpegConstants.MarkerStartOfFrame9
            || value == JpegConstants.MarkerStartOfFrame10
            || value == JpegConstants.MarkerStartOfFrame11
            || value == JpegConstants.MarkerDefineArithmeticConditioning
            || value == JpegConstants.MarkerStartOfFrame13
            || value == JpegConstants.MarkerStartOfFrame14
            || value == JpegConstants.MarkerStartOfFrame15
            || value == JpegConstants.MarkerStartOfImage
            || value == JpegConstants.MarkerEndOfImage
            || value == JpegConstants.MarkerStartOfScan
            || value == JpegConstants.MarkerDefineQuantizationTable
            || value == JpegConstants.MarkerDefineNumberOfLines
            || value == JpegConstants.MarkerDefineRestartInterval
            || value == JpegConstants.MarkerDefineHierarchicalProgression
            || value == JpegConstants.MarkerExpandReferenceComponents
            || value == JpegConstants.MarkerJpegLs
            || value == JpegConstants.MarkerComment
            || (value >= JpegConstants.MarkerRestart0 && value <= JpegConstants.MarkerRestart7)
            || (value >= JpegConstants.MarkerApp0 && value <= JpegConstants.MarkerApp15))
        {
            return (JpegMarker)value;
        }

        return JpegMarker.Unknown;
    }
}
