namespace ftp;

internal class FtpTransferPipelineTransform :
    IFtpTransferPumpTransform, IFtpTransferMarkerProvider
{
    internal FtpTransferOptions Options;
    internal FtpRepresentationTransform Representation;
    internal FtpTransferFramingTransform Framing;
    internal System.Action<string>? RestartMarkerCallback;
    internal RestartMarkerCodec RestartMarkers;
    internal string? PendingRestartMarker;
    internal bool Encoding;
    internal bool Completed;
    internal long LogicalBytes;
    internal long WireBytes;
    internal long StructureUnits;

    internal FtpTransferPipelineTransform(
        FtpTransferOptions options,
        bool encoding,
        System.Action<string>? restartMarkerCallback,
        long initialLogicalBytes)
    {
        if (initialLogicalBytes < 0)
        {
            throw new FtpConfigurationException(
                "An initial transfer checkpoint cannot be negative.", null);
        }
        Options = options;
        Representation = new FtpRepresentationTransform(options, encoding);
        Framing = new FtpTransferFramingTransform(options, encoding);
        RestartMarkerCallback = restartMarkerCallback;
        RestartMarkers = new RestartMarkerCodec();
        PendingRestartMarker = null;
        Encoding = encoding;
        Completed = false;
        LogicalBytes = initialLogicalBytes;
        WireBytes = 0;
        StructureUnits = 0;
    }

    /// <summary>
    /// Converts one bounded local or wire-data chunk through the configured FTP pipeline.
    /// </summary>
    /// <param name="data">The source buffer.</param>
    /// <param name="count">The number of source bytes.</param>
    /// <param name="finalChunk">Indicates that the transfer input is complete.</param>
    /// <returns>The converted chunks with logical and wire-byte accounting.</returns>
    public FtpTransferPumpChunk[] TransformChunk(
        byte[] data,
        int count,
        bool finalChunk)
    {
        if (Completed)
        {
            throw new FtpTransferException(
                "Transfer data cannot follow pipeline completion.", null);
        }
        byte[] output;
        if (Encoding)
        {
            byte[] represented = Representation.ConvertChunk(
                data, count, finalChunk);
            output = Framing.ConvertChunk(
                represented, represented.Length, finalChunk);
        }
        else
        {
            byte[] framed = Framing.ConvertChunk(
                data, count, finalChunk);
            output = Representation.ConvertChunk(
                framed, framed.Length, finalChunk);
        }
        if (finalChunk)
        {
            Completed = true;
        }

        AddProgress(count, output.Length);
        if (!finalChunk && count != 0)
        {
            ReportRestartMarker();
        }

        FtpTransferPumpChunk chunk = new FtpTransferPumpChunk();
        chunk.Data = output;
        chunk.Count = output.Length;
        chunk.LogicalBytes = Encoding ? count : output.Length;
        chunk.WireBytes = Encoding ? output.Length : count;
        return new FtpTransferPumpChunk[] { chunk };
    }

    internal void AddProgress(int sourceCount, int destinationCount)
    {
        long logical = Encoding ? sourceCount : destinationCount;
        long wire = Encoding ? destinationCount : sourceCount;
        if (LogicalBytes > long.MaxValue - logical ||
            WireBytes > long.MaxValue - wire ||
            StructureUnits == long.MaxValue)
        {
            throw new FtpTransferException(
                "The transfer checkpoint exceeded Int64 capacity.", null);
        }
        LogicalBytes += logical;
        WireBytes += wire;
        StructureUnits++;
    }

    internal void ReportRestartMarker()
    {
        if (RestartMarkerCallback == null ||
            (Options.Mode != FtpTransferMode.Block &&
             Options.Mode != FtpTransferMode.Compressed) ||
            !IsRestartCheckpointSafe())
        {
            return;
        }
        FtpRestartCheckpoint checkpoint = new FtpRestartCheckpoint();
        checkpoint.LogicalByteOffset = LogicalBytes;
        checkpoint.WireByteOffset = WireBytes;
        checkpoint.StructureUnitCount = StructureUnits;
        checkpoint.PendingCarriageReturn =
            Representation.PendingCarriageReturn;
        FtpLocalByteEncodingState? localEncoding =
            Representation.LocalEncoding;
        if (localEncoding != null)
        {
            checkpoint.PendingBitCount = localEncoding.Writer.BitCount;
        }
        FtpLocalByteDecodingState? localDecoding =
            Representation.LocalDecoding;
        if (localDecoding != null)
        {
            checkpoint.PendingBitCount = localDecoding.LogicalBitCount;
        }
        PendingRestartMarker = RestartMarkers.GenerateMarker(checkpoint);
    }

    internal bool IsRestartCheckpointSafe()
    {
        if (Representation.PendingCarriageReturn ||
            Framing.PendingPageBytes.Count != 0 ||
            Framing.PendingRecordBytes.Count != 0)
        {
            return false;
        }
        FtpLocalByteEncodingState? localEncoding =
            Representation.LocalEncoding;
        if (localEncoding != null &&
            (localEncoding.StoredByteCount != 0 ||
             localEncoding.Writer.BitCount != 0))
        {
            return false;
        }
        FtpLocalByteDecodingState? localDecoding =
            Representation.LocalDecoding;
        if (localDecoding != null && localDecoding.LogicalBitCount != 0)
        {
            return false;
        }
        FtpRecordStructureState? records = Framing.RecordStructure;
        if (records != null &&
            (records.PendingAsciiCarriageReturn ||
             records.CurrentRecordHasData))
        {
            return false;
        }
        FtpBlockModeState? block = Framing.BlockMode;
        if (!Encoding && block != null &&
            (block.PendingHeaderCount != 0 ||
             block.PendingPayloadCount != 0))
        {
            return false;
        }
        FtpCompressedModeState? compressed = Framing.CompressedMode;
        if (!Encoding && compressed != null &&
            (compressed.PendingKind != 0 ||
             compressed.PendingDescriptor != 0))
        {
            return false;
        }
        return true;
    }

    /// <summary>
    /// Reports the checkpoint for a chunk only after its transformed bytes were written.
    /// </summary>
    public void ReportTransferredChunk()
    {
        string? marker = PendingRestartMarker;
        PendingRestartMarker = null;
        if (marker != null && RestartMarkerCallback != null)
        {
            RestartMarkerCallback.Invoke(marker);
        }
    }
}
