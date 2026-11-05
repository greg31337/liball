using System.Collections.Generic;

namespace ftp;

internal class FtpStreamModeState
{
    internal FtpFileStructure Structure;
    internal long RestartOffset;
    internal long RestartBytesRemaining;
    internal long StreamOctets;
    internal long EmittedOctets;
    internal bool PendingEscape;
    internal bool EndOfFileReached;
    internal int OperationKind;

    internal FtpStreamModeState(
        FtpFileStructure structure,
        bool hasRestartOffset,
        long restartOffset)
    {
        if (structure != FtpFileStructure.File &&
            structure != FtpFileStructure.Record &&
            structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("The stream-mode file structure is invalid.", null);
        }

        if (restartOffset < 0 || (!hasRestartOffset && restartOffset != 0))
        {
            throw new FtpConfigurationException("A stream restart offset must be a specified non-negative octet count.", null);
        }

        if (restartOffset != 0 && structure != FtpFileStructure.File)
        {
            throw new FtpConfigurationException("RFC 3659 stream restart offsets require file structure.", null);
        }

        Structure = structure;
        RestartOffset = restartOffset;
        RestartBytesRemaining = restartOffset;
        StreamOctets = 0;
        EmittedOctets = 0;
        PendingEscape = false;
        EndOfFileReached = false;
        OperationKind = 0;
    }

    internal FtpStreamModeChunk EncodeFrame(FtpStructureFrame? frame)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A stream-mode state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Stream-mode data cannot follow end-of-file.", null);
        }

        ValidateFrame(frame);
        if (frame == null || frame.Data == null)
        {
            throw new FtpTransferException("A complete stream-mode frame is required.", null);
        }

        byte[] encoded = EncodeFrameBytes(frame);
        if (frame.EndOfFile && RestartBytesRemaining > encoded.Length)
        {
            throw new FtpTransferException("The RFC 3659 restart offset is beyond the transmitted stream.", null);
        }

        int skipped = 0;
        long restartBytesRemaining = RestartBytesRemaining;
        if (restartBytesRemaining != 0)
        {
            skipped = encoded.Length;
            if (restartBytesRemaining < skipped)
            {
                skipped = (int)restartBytesRemaining;
            }

            restartBytesRemaining -= skipped;
        }

        int emittedCount = encoded.Length - skipped;
        byte[] emitted = new byte[emittedCount];
        if (emittedCount != 0)
        {
            System.Array.Copy(encoded, skipped, emitted, 0, emittedCount);
        }

        if (StreamOctets > long.MaxValue - encoded.Length ||
            EmittedOctets > long.MaxValue - emittedCount)
        {
            throw new FtpTransferException("The stream-mode octet count exceeded Int64 capacity.", null);
        }

        AddStreamOctets(encoded.Length);
        AddEmittedOctets(emittedCount);
        RestartBytesRemaining = restartBytesRemaining;
        if (frame.EndOfFile)
        {
            EndOfFileReached = true;
        }

        bool closeConnection = Structure == FtpFileStructure.File && frame.EndOfFile;
        return new FtpStreamModeChunk(emitted, closeConnection);
    }

    internal FtpStreamModeChunk EncodePage(FtpPageFrame? page)
    {
        if (Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("Only page structure can encode page frames.", null);
        }

        if (page == null || page.SerializedLogicalBytes == null)
        {
            throw new FtpTransferException("A complete page frame is required for stream-mode encoding.", null);
        }

        if (page.LastPage != (page.PageType == 0) ||
            (page.LastPage && (page.HeaderLogicalByteCount != 4 || page.DataLogicalByteCount != 0)))
        {
            throw new FtpTransferException("The page frame has inconsistent last-page EOF metadata.", null);
        }

        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = page.SerializedLogicalBytes;
        frame.Count = page.SerializedLogicalBytes.Length;
        frame.EndOfFile = page.LastPage;
        return EncodeFrame(frame);
    }

    internal FtpStructureFrame[] DecodeBytes(
        byte[]? source,
        int offset,
        int count,
        bool connectionClosed)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A stream-mode state cannot switch from encoding to decoding.", null);
        }

        OperationKind = 2;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Stream-mode data cannot follow end-of-file.", null);
        }

        ValidateSourceSlice(source, offset, count);
        if (source == null)
        {
            throw new FtpTransferException("Stream-mode source data is required.", null);
        }

        if (StreamOctets == 0 && RestartOffset != 0)
        {
            StreamOctets = RestartOffset;
            RestartBytesRemaining = 0;
        }

        if (StreamOctets > long.MaxValue - count)
        {
            throw new FtpTransferException("The stream-mode octet position exceeded Int64 capacity.", null);
        }

        if (EmittedOctets > long.MaxValue - count)
        {
            throw new FtpTransferException("The stream-mode received octet count exceeded Int64 capacity.", null);
        }

        FtpStructureFrame[] frames;
        if (Structure == FtpFileStructure.Record)
        {
            frames = DecodeRecordBytes(source, offset, count, connectionClosed);
        }
        else
        {
            frames = DecodeRawBytes(source, offset, count, connectionClosed);
        }

        StreamOctets += count;
        EmittedOctets += count;
        return frames;
    }

    internal void CompletePageStructure(FtpPageFrame? lastPage)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("An encoding stream cannot accept a decoded page EOF marker.", null);
        }

        if (Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("Only page structure has a last-page EOF marker.", null);
        }

        if (EndOfFileReached)
        {
            throw new FtpTransferException("The page-structure EOF marker was already received.", null);
        }

        if (lastPage == null || !lastPage.LastPage || lastPage.PageType != 0 ||
            lastPage.HeaderLogicalByteCount != 4 || lastPage.DataLogicalByteCount != 0)
        {
            throw new FtpTransferException("Page-structured stream EOF requires a validated last-page header.", null);
        }

        OperationKind = 2;
        EndOfFileReached = true;
    }

    internal void RequireEndOfFile()
    {
        if (!EndOfFileReached)
        {
            throw new FtpTransferException("The stream-mode transfer ended before its required EOF signal.", null);
        }

        if (PendingEscape)
        {
            throw new FtpTransferException("The stream-mode transfer ended after an incomplete escape byte.", null);
        }

        if (RestartBytesRemaining != 0)
        {
            throw new FtpTransferException("The RFC 3659 restart offset is beyond the transmitted stream.", null);
        }
    }

    internal byte[] EncodeFrameBytes(FtpStructureFrame frame)
    {
        byte[] data = frame.Data ?? new byte[0];
        if (Structure != FtpFileStructure.Record)
        {
            byte[] raw = new byte[frame.Count];
            if (frame.Count != 0)
            {
                System.Array.Copy(data, 0, raw, 0, frame.Count);
            }

            return raw;
        }

        int escapedCount = 0;
        int index = 0;
        while (index < frame.Count)
        {
            if (data[index] == 0xFF)
            {
                if (escapedCount == int.MaxValue)
                {
                    throw new FtpTransferException("The escaped stream-mode frame exceeds supported buffer capacity.", null);
                }

                escapedCount++;
            }

            index++;
        }

        int markerCount = frame.EndOfRecord || frame.EndOfFile ? 2 : 0;
        if (frame.Count > int.MaxValue - escapedCount ||
            frame.Count + escapedCount > int.MaxValue - markerCount)
        {
            throw new FtpTransferException("The escaped stream-mode frame exceeds supported buffer capacity.", null);
        }

        byte[] encoded = new byte[frame.Count + escapedCount + markerCount];
        index = 0;
        int destinationIndex = 0;
        while (index < frame.Count)
        {
            byte value = data[index];
            encoded[destinationIndex] = value;
            destinationIndex++;
            if (value == 0xFF)
            {
                encoded[destinationIndex] = 0xFF;
                destinationIndex++;
            }

            index++;
        }

        if (markerCount != 0)
        {
            int marker = 0;
            if (frame.EndOfRecord)
            {
                marker += 1;
            }

            if (frame.EndOfFile)
            {
                marker += 2;
            }

            encoded[destinationIndex] = 0xFF;
            encoded[destinationIndex + 1] = (byte)marker;
        }

        return encoded;
    }

    internal FtpStructureFrame[] DecodeRawBytes(
        byte[] source,
        int offset,
        int count,
        bool connectionClosed)
    {
        if (Structure == FtpFileStructure.Page && connectionClosed)
        {
            throw new FtpTransferException("Page-structured stream EOF requires a last-page header.", null);
        }

        if (count == 0 && !connectionClosed)
        {
            return new FtpStructureFrame[0];
        }

        byte[] data = new byte[count];
        if (count != 0)
        {
            System.Array.Copy(source, offset, data, 0, count);
        }

        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data;
        frame.Count = count;
        if (Structure == FtpFileStructure.File && connectionClosed)
        {
            frame.EndOfFile = true;
            frame.RequiresConnectionClose = true;
            EndOfFileReached = true;
        }

        return new FtpStructureFrame[] { frame };
    }

    internal FtpStructureFrame[] DecodeRecordBytes(
        byte[] source,
        int offset,
        int count,
        bool connectionClosed)
    {
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        List<byte> data = new List<byte>();
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            byte value = source[index];
            if (PendingEscape)
            {
                PendingEscape = false;
                if (value == 0xFF)
                {
                    data.Add(0xFF);
                }
                else if (value == 1 || value == 2 || value == 3)
                {
                    bool endOfRecord = value == 1 || value == 3;
                    bool endOfFile = value == 2 || value == 3;
                    frames.Add(CreateDecodedFrame(data, endOfRecord, endOfFile));
                    data.Clear();
                    if (endOfFile)
                    {
                        EndOfFileReached = true;
                        if (index + 1 != end)
                        {
                            throw new FtpTransferException("Stream-mode data cannot follow an EOF control code.", null);
                        }
                    }
                }
                else
                {
                    throw new FtpTransferException("The stream-mode escape code is undefined.", null);
                }
            }
            else if (value == 0xFF)
            {
                PendingEscape = true;
            }
            else
            {
                data.Add(value);
            }

            index++;
        }

        if (data.Count != 0)
        {
            frames.Add(CreateDecodedFrame(data, false, false));
        }

        if (connectionClosed)
        {
            if (PendingEscape)
            {
                throw new FtpTransferException("The stream-mode connection closed after an incomplete escape byte.", null);
            }

            if (!EndOfFileReached)
            {
                throw new FtpTransferException("Record-structured stream EOF requires an explicit EOF control code.", null);
            }
        }

        return frames.ToArray();
    }

    internal FtpStructureFrame CreateDecodedFrame(
        List<byte> data,
        bool endOfRecord,
        bool endOfFile)
    {
        FtpStructureFrame frame = new FtpStructureFrame();
        frame.Data = data.ToArray();
        frame.Count = data.Count;
        frame.EndOfRecord = endOfRecord;
        frame.EndOfFile = endOfFile;
        if (endOfFile)
        {
            frame.RequiresStreamEndOfFileMarker = true;
        }

        return frame;
    }

    internal void ValidateFrame(FtpStructureFrame? frame)
    {
        if (frame == null || frame.Data == null || frame.Count < 0 || frame.Count > frame.Data.Length)
        {
            throw new FtpTransferException("A stream-mode frame has invalid data bounds.", null);
        }

        if (Structure != FtpFileStructure.Record && frame.EndOfRecord)
        {
            throw new FtpTransferException("Only record structure can contain an EOR marker.", null);
        }

        if (!frame.EndOfFile)
        {
            if (frame.RequiresConnectionClose ||
                frame.RequiresStreamEndOfFileMarker ||
                frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Only an EOF frame may request stream-mode EOF signaling.", null);
            }

            return;
        }

        if (Structure == FtpFileStructure.File)
        {
            if (!frame.RequiresConnectionClose ||
                frame.RequiresStreamEndOfFileMarker ||
                frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Stream-mode file structure requires connection-close EOF signaling.", null);
            }
        }
        else if (Structure == FtpFileStructure.Record)
        {
            if (frame.RequiresConnectionClose ||
                !frame.RequiresStreamEndOfFileMarker ||
                frame.RequiresEndOfFileDescriptor)
            {
                throw new FtpTransferException("Stream-mode record structure requires an explicit EOF control code.", null);
            }
        }
        else if (frame.RequiresConnectionClose ||
            frame.RequiresStreamEndOfFileMarker ||
            frame.RequiresEndOfFileDescriptor)
        {
            throw new FtpTransferException("Stream-mode page structure uses its last-page header as EOF.", null);
        }
    }

    internal void ValidateSourceSlice(byte[]? source, int offset, int count)
    {
        if (source == null)
        {
            throw new FtpTransferException("Stream-mode source data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new FtpTransferException("The stream-mode source slice is invalid.", null);
        }
    }

    internal void AddStreamOctets(int count)
    {
        if (StreamOctets > long.MaxValue - count)
        {
            throw new FtpTransferException("The stream-mode octet position exceeded Int64 capacity.", null);
        }

        StreamOctets += count;
    }

    internal void AddEmittedOctets(int count)
    {
        if (EmittedOctets > long.MaxValue - count)
        {
            throw new FtpTransferException("The stream-mode emitted octet count exceeded Int64 capacity.", null);
        }

        EmittedOctets += count;
    }
}
