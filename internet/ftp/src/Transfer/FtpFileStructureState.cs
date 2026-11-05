namespace ftp;

internal class FtpFileStructureState
{
    internal FtpTransferMode Mode;
    internal bool EndOfFileReached;
    internal long DataBytes;
    internal int OperationKind;

    internal FtpFileStructureState(FtpTransferMode mode)
    {
        if (mode != FtpTransferMode.Stream &&
            mode != FtpTransferMode.Block &&
            mode != FtpTransferMode.Compressed &&
            mode != FtpTransferMode.Deflate)
        {
            throw new FtpConfigurationException("The file-structure transfer mode is invalid.", null);
        }

        Mode = mode;
        EndOfFileReached = false;
        DataBytes = 0;
        OperationKind = 0;
    }

    internal FtpStructureFrame FrameData(byte[]? data, int offset, int count)
    {
        return CreateFrame(data, offset, count, false);
    }

    internal FtpStructureFrame FrameFinalData(byte[]? data, int offset, int count)
    {
        return CreateFrame(data, offset, count, true);
    }

    internal FtpStructureFrame CompleteFile()
    {
        return CreateFrame(new byte[0], 0, 0, true);
    }

    internal FtpStructureFrame CreateFrame(byte[]? data, int offset, int count, bool endOfFile)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A file-structure state cannot switch from consuming to producing frames.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("File-structure data cannot follow end-of-file.", null);
        }

        ValidateDataSlice(data, offset, count, endOfFile);
        if (DataBytes > long.MaxValue - count)
        {
            throw new FtpTransferException("The file-structure byte count exceeded Int64 capacity.", null);
        }

        byte[] copy = new byte[count];
        if (count != 0 && data != null)
        {
            System.Array.Copy(data, offset, copy, 0, count);
        }

        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = copy;
        frame.Count = count;
        frame.EndOfFile = endOfFile;
        ApplyEndOfFileSemantics(frame);
        DataBytes += count;
        if (endOfFile)
        {
            EndOfFileReached = true;
        }

        return frame;
    }

    internal void ConsumeFrame(FtpStructureFrame? frame)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A file-structure state cannot switch from producing to consuming frames.", null);
        }

        OperationKind = 2;
        if (frame == null)
        {
            throw new FtpTransferException("A file-structure frame is required.", null);
        }

        if (EndOfFileReached)
        {
            throw new FtpTransferException("File-structure data cannot follow end-of-file.", null);
        }

        ValidateConsumedData(frame);
        if (frame.EndOfRecord)
        {
            throw new FtpTransferException("STRU F cannot contain an end-of-record marker.", null);
        }

        ValidateEndOfFileSemantics(frame);
        if (DataBytes > long.MaxValue - frame.Count)
        {
            throw new FtpTransferException("The file-structure byte count exceeded Int64 capacity.", null);
        }

        DataBytes += frame.Count;
        if (frame.EndOfFile)
        {
            EndOfFileReached = true;
        }
    }

    internal bool IsComplete()
    {
        return EndOfFileReached;
    }

    internal void ApplyEndOfFileSemantics(FtpStructureFrame frame)
    {
        if (!frame.EndOfFile)
        {
            return;
        }

        if (Mode == FtpTransferMode.Block || Mode == FtpTransferMode.Compressed)
        {
            frame.RequiresEndOfFileDescriptor = true;
        }
        else
        {
            frame.RequiresConnectionClose = true;
        }
    }

    internal void ValidateEndOfFileSemantics(FtpStructureFrame frame)
    {
        if (!frame.EndOfFile)
        {
            if (frame.RequiresConnectionClose ||
                frame.RequiresStreamEndOfFileMarker ||
                frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Only an end-of-file frame may request EOF signaling.", null);
            }

            return;
        }

        bool requiresDescriptor = Mode == FtpTransferMode.Block || Mode == FtpTransferMode.Compressed;
        if (requiresDescriptor)
        {
            if (!frame.RequiresEndOfFileDescriptor ||
                frame.RequiresConnectionClose ||
                frame.RequiresStreamEndOfFileMarker)
            {
                throw new FtpTransferException("STRU F requires an explicit EOF descriptor in block and compressed modes.", null);
            }
        }
        else if (!frame.RequiresConnectionClose ||
            frame.RequiresStreamEndOfFileMarker ||
            frame.RequiresEndOfFileDescriptor)
        {
            throw new FtpTransferException("STRU F requires connection-close EOF signaling in stream-based modes.", null);
        }
    }

    internal void ValidateDataSlice(byte[]? data, int offset, int count, bool endOfFile)
    {
        if (data == null)
        {
            throw new FtpConfigurationException("File-structure frame data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new FtpConfigurationException("The file-structure data slice is outside the supplied buffer.", null);
        }

        if (count == 0 && !endOfFile)
        {
            throw new FtpConfigurationException("An empty file-structure frame must mark end-of-file.", null);
        }
    }

    internal void ValidateConsumedData(FtpStructureFrame frame)
    {
        byte[]? data = frame.Data;
        if (data == null)
        {
            throw new FtpTransferException("A received file-structure frame has no data buffer.", null);
        }

        if (frame.Count < 0 || frame.Count > data.Length)
        {
            throw new FtpTransferException("A received file-structure frame has an invalid data count.", null);
        }

        if (frame.Count == 0 && !frame.EndOfFile)
        {
            throw new FtpTransferException("A received empty file-structure frame does not mark end-of-file.", null);
        }
    }
}
