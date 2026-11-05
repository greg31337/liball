namespace jpeg;

internal class JpegMarkerInspectionReader
{
    internal JpegInspectionResult InspectBytes(byte[] data, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        cancellationToken.ThrowIfCancellationRequested();
        JpegIdentifier identifier = JpegIdentifier.Create(data, options);
        identifier.CancellationTokenValue = cancellationToken;
        JpegInfo info = identifier.Read();
        return CreateResult(identifier, info);
    }

    internal JpegInspectionResult InspectStream(System.IO.Stream stream, JpegReadOptions options, System.Threading.CancellationToken cancellationToken)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        byte[] data = new JpegIo().ReadStream(stream, cancellationToken);
        return InspectBytes(data, options, cancellationToken);
    }

    internal JpegInspectionResult CreateResult(JpegIdentifier identifier, JpegInfo info)
    {
        if (!info.SawStartOfImage)
        {
            throw JpegInvalidDataException.Create("Marker inspection requires a marker-based JPEG stream.");
        }

        JpegInspectionResult result = new JpegInspectionResult();
        result.Info = info;
        foreach (JpegMarkerSegment segment in identifier.InspectedMarkers)
        {
            result.Markers.Add(segment);
        }
        result.Frame = info.Frame;
        result.ScanHeaders = info.ScanHeaders;
        result.Tables.QuantizationTables = info.QuantizationTables;
        result.Tables.HuffmanTables = info.HuffmanTables;
        result.Tables.ArithmeticConditioningTables = info.ArithmeticConditioningTables;
        result.HasRestartInterval = info.HasRestartInterval;
        result.RestartInterval = info.RestartInterval;
        result.RestartMarkerCount = info.RestartMarkerCount;
        result.HasDefinedNumberOfLines = info.HasDefinedNumberOfLines;
        result.DefinedNumberOfLines = info.DefinedNumberOfLines;
        result.HasHierarchicalProgression = info.HasHierarchicalProgression;
        result.HierarchicalProgressionMarkerCount = info.HierarchicalProgressionMarkerCount;
        result.ExpandReferenceComponentMarkerCount = info.ExpandReferenceComponentMarkerCount;
        result.IsJpegLs = info.IsJpegLs;
        result.JpegLsMarkerCount = info.JpegLsMarkerCount;
        result.HasSmartScale = info.HasSmartScale;
        result.SmartScaleMarkerCount = info.SmartScaleMarkerCount;
        result.Diagnostics = info.Diagnostics;
        return result;
    }
}
