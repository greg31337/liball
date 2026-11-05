using System.Collections.Generic;

namespace ftp;

internal class FtpRecordStructureState
{
    internal FtpTransferMode Mode;
    internal FtpTransferType Type;
    internal bool PendingAsciiCarriageReturn;
    internal bool CurrentRecordHasData;
    internal bool EndOfFileReached;
    internal long DataBytes;
    internal long RecordCount;
    internal int OperationKind;

    internal FtpRecordStructureState(FtpTransferMode mode, FtpTransferType type)
    {
        if (mode != FtpTransferMode.Stream &&
            mode != FtpTransferMode.Block &&
            mode != FtpTransferMode.Compressed &&
            mode != FtpTransferMode.Deflate)
        {
            throw new FtpConfigurationException("Record-structure conversion requires a recognized transfer mode.", null);
        }

        if (type != FtpTransferType.Ascii && type != FtpTransferType.Ebcdic)
        {
            throw new FtpConfigurationException("Record-structure conversion requires ASCII or EBCDIC text.", null);
        }

        Mode = mode;
        Type = type;
        PendingAsciiCarriageReturn = false;
        CurrentRecordHasData = false;
        EndOfFileReached = false;
        DataBytes = 0;
        RecordCount = 0;
        OperationKind = 0;
    }

    internal FtpStructureFrame[] ConvertData(byte[]? data, int offset, int count)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A record-structure state cannot switch from consuming to producing frames.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Record data cannot follow end-of-file.", null);
        }

        ValidateDataSlice(data, offset, count);
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        List<byte> segment = new List<byte>();
        int index = offset;
        int end = offset + count;

        if (Type == FtpTransferType.Ascii && PendingAsciiCarriageReturn)
        {
            if (index < end && data != null && data[index] == 10)
            {
                AddRecordFrame(frames, segment);
                index++;
            }
            else
            {
                segment.Add(13);
            }

            PendingAsciiCarriageReturn = false;
        }

        while (index < end && data != null)
        {
            byte value = data[index];
            if (Type == FtpTransferType.Ascii && value == 13)
            {
                if (index + 1 < end)
                {
                    if (data[index + 1] == 10)
                    {
                        AddRecordFrame(frames, segment);
                        index += 2;
                        continue;
                    }

                    segment.Add(value);
                }
                else
                {
                    PendingAsciiCarriageReturn = true;
                }
            }
            else if (Type == FtpTransferType.Ebcdic && value == 0x15)
            {
                AddRecordFrame(frames, segment);
            }
            else
            {
                segment.Add(value);
            }

            index++;
        }

        if (segment.Count != 0)
        {
            byte[] payload = segment.ToArray();
            AddDataBytes(payload.Length);
            CurrentRecordHasData = true;
            frames.Add(CreatePayloadFrame(payload, false, false));
        }

        return frames.ToArray();
    }

    internal FtpStructureFrame[] CompleteRecords()
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A record-structure state cannot switch from consuming to producing frames.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Record-structure end-of-file was already produced.", null);
        }

        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        if (PendingAsciiCarriageReturn)
        {
            byte[] carriageReturn = new byte[] { 13 };
            AddDataBytes(1);
            CurrentRecordHasData = true;
            frames.Add(CreatePayloadFrame(carriageReturn, true, true));
            AddRecordCount();
            PendingAsciiCarriageReturn = false;
            CurrentRecordHasData = false;
        }
        else if (CurrentRecordHasData)
        {
            frames.Add(CreatePayloadFrame(new byte[0], true, true));
            AddRecordCount();
            CurrentRecordHasData = false;
        }
        else
        {
            frames.Add(CreatePayloadFrame(new byte[0], false, true));
        }

        EndOfFileReached = true;
        return frames.ToArray();
    }

    internal byte[] ConvertFrame(FtpStructureFrame? frame)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A record-structure state cannot switch from producing to consuming frames.", null);
        }

        OperationKind = 2;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Record data cannot follow end-of-file.", null);
        }

        ValidateConsumedFrame(frame);
        if (frame == null)
        {
            throw new FtpTransferException("A record-structure frame is required.", null);
        }

        byte[]? data = frame.Data;
        if (data == null)
        {
            throw new FtpTransferException("A record-structure frame data buffer is required.", null);
        }

        if (frame.EndOfFile && (CurrentRecordHasData || frame.Count != 0) && !frame.EndOfRecord)
        {
            throw new FtpTransferException("The final STRU R record must have an explicit EOR marker.", null);
        }

        int delimiterBytes = 0;
        if (frame.EndOfRecord)
        {
            delimiterBytes = Type == FtpTransferType.Ascii ? 2 : 1;
        }

        if (frame.Count > int.MaxValue - delimiterBytes)
        {
            throw new FtpTransferException("The converted record is too large for one output buffer.", null);
        }

        byte[] converted = new byte[frame.Count + delimiterBytes];
        if (frame.Count != 0)
        {
            System.Array.Copy(data, 0, converted, 0, frame.Count);
            AddDataBytes(frame.Count);
            CurrentRecordHasData = true;
        }

        if (frame.EndOfRecord)
        {
            if (Type == FtpTransferType.Ascii)
            {
                converted[frame.Count] = 13;
                converted[frame.Count + 1] = 10;
            }
            else
            {
                converted[frame.Count] = 0x15;
            }

            AddRecordCount();
            CurrentRecordHasData = false;
        }

        if (frame.EndOfFile)
        {
            EndOfFileReached = true;
        }

        return converted;
    }

    internal bool IsComplete()
    {
        return EndOfFileReached;
    }

    internal void AddRecordFrame(List<FtpStructureFrame> frames, List<byte> segment)
    {
        byte[] payload = segment.ToArray();
        AddDataBytes(payload.Length);
        frames.Add(CreatePayloadFrame(payload, true, false));
        AddRecordCount();
        CurrentRecordHasData = false;
        segment.Clear();
    }

    internal FtpStructureFrame CreatePayloadFrame(byte[] payload, bool endOfRecord, bool endOfFile)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = payload;
        frame.Count = payload.Length;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        if (endOfFile)
        {
            if (Mode == FtpTransferMode.Stream ||
                Mode == FtpTransferMode.Deflate)
            {
                frame.RequiresStreamEndOfFileMarker = true;
            }
            else
            {
                frame.RequiresEndOfFileDescriptor = true;
            }
        }

        return frame;
    }

    internal void ValidateConsumedFrame(FtpStructureFrame? frame)
    {
        if (frame == null)
        {
            throw new FtpTransferException("A record-structure frame is required.", null);
        }

        byte[]? data = frame.Data;
        if (data == null || frame.Count < 0 || frame.Count > data.Length)
        {
            throw new FtpTransferException("A record-structure frame has invalid data bounds.", null);
        }

        if (!frame.EndOfFile)
        {
            if (frame.RequiresConnectionClose ||
                frame.RequiresStreamEndOfFileMarker ||
                frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Only an EOF frame may request record EOF signaling.", null);
            }

            return;
        }

        if (frame.RequiresConnectionClose)
        {
            throw new FtpTransferException("STRU R uses explicit EOF signaling instead of connection-close EOF.", null);
        }

        if (Mode == FtpTransferMode.Stream ||
            Mode == FtpTransferMode.Deflate)
        {
            if (!frame.RequiresStreamEndOfFileMarker || frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Stream-mode STRU R requires an explicit stream EOF marker.", null);
            }
        }
        else if (!frame.RequiresEndOfFileDescriptor || frame.RequiresStreamEndOfFileMarker)
        {
            throw new FtpTransferException("Block-mode STRU R requires an explicit EOF descriptor.", null);
        }
    }

    internal void ValidateDataSlice(byte[]? data, int offset, int count)
    {
        if (data == null)
        {
            throw new FtpConfigurationException("Record conversion data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > data.Length - count)
        {
            throw new FtpConfigurationException("The record conversion slice is outside the supplied buffer.", null);
        }
    }

    internal void AddDataBytes(int count)
    {
        if (DataBytes > long.MaxValue - count)
        {
            throw new FtpTransferException("The record-structure byte count exceeded Int64 capacity.", null);
        }

        DataBytes += count;
    }

    internal void AddRecordCount()
    {
        if (RecordCount == long.MaxValue)
        {
            throw new FtpTransferException("The record count exceeded Int64 capacity.", null);
        }

        RecordCount++;
    }
}
