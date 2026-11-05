using System.Collections.Generic;

namespace ftp;

internal class FtpBlockModeState
{
    internal const int EndOfRecordDescriptor = 128;
    internal const int EndOfFileDescriptor = 64;
    internal const int SuspectedErrorsDescriptor = 32;
    internal const int RestartMarkerDescriptor = 16;
    internal const int MaximumBlockByteCount = 65535;

    internal FtpFileStructure Structure;
    internal bool EndOfFileReached;
    internal long WireBytes;
    internal long DataBytes;
    internal long BlockCount;
    internal int OperationKind;
    internal byte[] PendingHeader;
    internal int PendingHeaderCount;
    internal byte[] PendingPayload;
    internal int PendingPayloadCount;
    internal int PendingDescriptor;
    internal int PendingByteCount;
    internal RestartMarkerCodec RestartMarkers;

    internal FtpBlockModeState(FtpFileStructure structure)
    {
        if (structure != FtpFileStructure.File &&
            structure != FtpFileStructure.Record &&
            structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("The block-mode file structure is invalid.", null);
        }

        Structure = structure;
        EndOfFileReached = false;
        WireBytes = 0;
        DataBytes = 0;
        BlockCount = 0;
        OperationKind = 0;
        PendingHeader = new byte[3];
        PendingHeaderCount = 0;
        PendingPayload = new byte[0];
        PendingPayloadCount = 0;
        PendingDescriptor = 0;
        PendingByteCount = 0;
        RestartMarkers = new RestartMarkerCodec();
    }

    internal FtpBlockModeBlock[] EncodeFrame(FtpStructureFrame? frame, bool suspectedErrors)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A block-mode state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Block-mode data cannot follow end-of-file.", null);
        }

        ValidateFrame(frame);
        if (frame == null || frame.Data == null)
        {
            throw new FtpTransferException("A complete block-mode frame is required.", null);
        }

        List<FtpBlockModeBlock> blocks = new List<FtpBlockModeBlock>();
        if (frame.Count == 0)
        {
            int emptyDescriptor = CreateDescriptor(
                frame.EndOfRecord, frame.EndOfFile, suspectedErrors, false);
            blocks.Add(CreateEncodedBlock(frame.Data, 0, 0, emptyDescriptor));
        }
        else
        {
            int sourceIndex = 0;
            while (sourceIndex < frame.Count)
            {
                int blockByteCount = frame.Count - sourceIndex;
                if (blockByteCount > MaximumBlockByteCount)
                {
                    blockByteCount = MaximumBlockByteCount;
                }

                bool finalBlock = sourceIndex + blockByteCount == frame.Count;
                int descriptor = CreateDescriptor(
                    finalBlock && frame.EndOfRecord,
                    finalBlock && frame.EndOfFile,
                    suspectedErrors,
                    false);
                blocks.Add(CreateEncodedBlock(
                    frame.Data, sourceIndex, blockByteCount, descriptor));
                sourceIndex += blockByteCount;
            }
        }

        if (frame.EndOfFile)
        {
            EndOfFileReached = true;
        }

        return blocks.ToArray();
    }

    internal FtpBlockModeBlock[] EncodePage(FtpPageFrame? page, bool suspectedErrors)
    {
        if (Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("Only page structure can encode page frames.", null);
        }

        if (page == null || page.SerializedLogicalBytes == null)
        {
            throw new FtpTransferException("A complete page frame is required for block-mode encoding.", null);
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
        if (page.LastPage)
        {
            frame.RequiresEndOfFileDescriptor = true;
        }

        return EncodeFrame(frame, suspectedErrors);
    }

    internal FtpBlockModeBlock EncodeRestartMarker(string? marker)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A block-mode state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("A restart marker cannot follow block-mode EOF.", null);
        }

        byte[] markerBytes = EncodeMarkerText(marker);
        return CreateEncodedBlock(
            markerBytes, 0, markerBytes.Length, RestartMarkerDescriptor);
    }

    internal FtpBlockModeBlock[] DecodeBytes(
        byte[]? source,
        int offset,
        int count,
        bool endOfInput)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A block-mode state cannot switch from encoding to decoding.", null);
        }

        OperationKind = 2;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Block-mode data cannot follow end-of-file.", null);
        }

        ValidateSourceSlice(source, offset, count);
        if (source == null)
        {
            throw new FtpTransferException("Block-mode source data is required.", null);
        }

        if (WireBytes > long.MaxValue - count)
        {
            throw new FtpTransferException("The block-mode wire byte count exceeded Int64 capacity.", null);
        }

        List<FtpBlockModeBlock> blocks = new List<FtpBlockModeBlock>();
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            if (PendingHeaderCount < 3)
            {
                int headerBytes = 3 - PendingHeaderCount;
                int available = end - index;
                if (headerBytes > available)
                {
                    headerBytes = available;
                }

                System.Array.Copy(source, index, PendingHeader,
                    PendingHeaderCount, headerBytes);
                PendingHeaderCount += headerBytes;
                index += headerBytes;
                if (PendingHeaderCount < 3)
                {
                    continue;
                }

                PendingDescriptor = PendingHeader[0];
                PendingByteCount = PendingHeader[1] * 256 + PendingHeader[2];
                ValidateDescriptor(PendingDescriptor, PendingByteCount);
                PendingPayload = new byte[PendingByteCount];
                PendingPayloadCount = 0;
                if (PendingByteCount == 0)
                {
                    FtpBlockModeBlock emptyBlock = CompletePendingBlock();
                    blocks.Add(emptyBlock);
                    if (emptyBlock.EndOfFile && index != end)
                    {
                        throw new FtpTransferException("Block-mode data cannot follow an EOF block.", null);
                    }

                    continue;
                }
            }

            int payloadBytes = PendingByteCount - PendingPayloadCount;
            int payloadAvailable = end - index;
            if (payloadBytes > payloadAvailable)
            {
                payloadBytes = payloadAvailable;
            }

            System.Array.Copy(source, index, PendingPayload,
                PendingPayloadCount, payloadBytes);
            PendingPayloadCount += payloadBytes;
            index += payloadBytes;
            if (PendingPayloadCount == PendingByteCount)
            {
                FtpBlockModeBlock block = CompletePendingBlock();
                blocks.Add(block);
                if (block.EndOfFile && index != end)
                {
                    throw new FtpTransferException("Block-mode data cannot follow an EOF block.", null);
                }
            }
        }

        if (endOfInput)
        {
            if (PendingHeaderCount != 0 || PendingPayloadCount != 0)
            {
                throw new FtpTransferException("The block-mode stream ended inside a block.", null);
            }

            if (!EndOfFileReached)
            {
                throw new FtpTransferException("The block-mode stream ended without an EOF descriptor.", null);
            }
        }

        WireBytes += count;
        return blocks.ToArray();
    }

    internal void RequireEndOfFile()
    {
        if (PendingHeaderCount != 0 || PendingPayloadCount != 0)
        {
            throw new FtpTransferException("The block-mode stream ended inside a block.", null);
        }

        if (!EndOfFileReached)
        {
            throw new FtpTransferException("The block-mode stream ended without an EOF descriptor.", null);
        }
    }

    internal FtpBlockModeBlock CreateEncodedBlock(
        byte[] source,
        int offset,
        int count,
        int descriptor)
    {
        if (WireBytes > long.MaxValue - count - 3)
        {
            throw new FtpTransferException("The block-mode wire byte count exceeded Int64 capacity.", null);
        }

        bool restartMarker = (descriptor & RestartMarkerDescriptor) != 0;
        if (!restartMarker && DataBytes > long.MaxValue - count)
        {
            throw new FtpTransferException("The block-mode data byte count exceeded Int64 capacity.", null);
        }

        if (BlockCount == long.MaxValue)
        {
            throw new FtpTransferException("The block-mode block count exceeded Int64 capacity.", null);
        }

        byte[] payload = new byte[count];
        if (count != 0)
        {
            System.Array.Copy(source, offset, payload, 0, count);
        }

        FtpBlockModeBlock block = CreateBlock(descriptor, payload);
        WireBytes += count + 3;
        if (!restartMarker)
        {
            DataBytes += count;
        }

        BlockCount++;
        return block;
    }

    internal FtpBlockModeBlock CompletePendingBlock()
    {
        bool restartMarker = (PendingDescriptor & RestartMarkerDescriptor) != 0;
        if (!restartMarker && DataBytes > long.MaxValue - PendingByteCount)
        {
            throw new FtpTransferException("The block-mode data byte count exceeded Int64 capacity.", null);
        }

        if (BlockCount == long.MaxValue)
        {
            throw new FtpTransferException("The block-mode block count exceeded Int64 capacity.", null);
        }

        FtpBlockModeBlock block = CreateBlock(PendingDescriptor, PendingPayload);
        if (!restartMarker)
        {
            DataBytes += PendingByteCount;
        }

        BlockCount++;
        if (block.EndOfFile)
        {
            EndOfFileReached = true;
        }

        PendingHeader = new byte[3];
        PendingHeaderCount = 0;
        PendingPayload = new byte[0];
        PendingPayloadCount = 0;
        PendingDescriptor = 0;
        PendingByteCount = 0;
        return block;
    }

    internal FtpBlockModeBlock CreateBlock(int descriptor, byte[] payload)
    {
        FtpBlockModeBlock block = new FtpBlockModeBlock();
        block.Descriptor = descriptor;
        block.Count = payload.Length;
        block.Data = CopyBytes(payload);
        block.EndOfRecord = (descriptor & EndOfRecordDescriptor) != 0;
        block.EndOfFile = (descriptor & EndOfFileDescriptor) != 0;
        block.SuspectedErrors = (descriptor & SuspectedErrorsDescriptor) != 0;
        block.IsRestartMarker = (descriptor & RestartMarkerDescriptor) != 0;
        block.SerializedBytes = new byte[payload.Length + 3];
        block.SerializedBytes[0] = (byte)descriptor;
        block.SerializedBytes[1] = (byte)(payload.Length / 256);
        block.SerializedBytes[2] = (byte)(payload.Length % 256);
        if (payload.Length != 0)
        {
            System.Array.Copy(payload, 0, block.SerializedBytes, 3, payload.Length);
        }

        if (block.IsRestartMarker)
        {
            block.RestartMarkerText = DecodeMarkerText(payload);
            RestartMarkers.AcceptOpaqueMarker(block.RestartMarkerText, MaximumBlockByteCount);
        }
        else
        {
            FtpStructureFrame frame = new FtpStructureFrame();
            frame.Data = CopyBytes(payload);
            frame.Count = payload.Length;
            frame.EndOfRecord = block.EndOfRecord;
            frame.EndOfFile = block.EndOfFile;
            if (block.EndOfFile)
            {
                frame.RequiresEndOfFileDescriptor = true;
            }

            block.Frame = frame;
        }

        return block;
    }

    internal int CreateDescriptor(
        bool endOfRecord,
        bool endOfFile,
        bool suspectedErrors,
        bool restartMarker)
    {
        int descriptor = 0;
        if (endOfRecord)
        {
            descriptor += EndOfRecordDescriptor;
        }

        if (endOfFile)
        {
            descriptor += EndOfFileDescriptor;
        }

        if (suspectedErrors)
        {
            descriptor += SuspectedErrorsDescriptor;
        }

        if (restartMarker)
        {
            descriptor += RestartMarkerDescriptor;
        }

        return descriptor;
    }

    internal void ValidateDescriptor(int descriptor, int byteCount)
    {
        if ((descriptor & 0x0F) != 0)
        {
            throw new FtpTransferException("The block header contains undefined descriptor bits.", null);
        }

        bool endOfRecord = (descriptor & EndOfRecordDescriptor) != 0;
        bool restartMarker = (descriptor & RestartMarkerDescriptor) != 0;
        if (endOfRecord && Structure != FtpFileStructure.Record)
        {
            throw new FtpTransferException("Only record structure can contain a block-mode EOR descriptor.", null);
        }

        if (restartMarker && byteCount == 0)
        {
            throw new FtpTransferException("A block-mode restart marker cannot be empty.", null);
        }

        if (descriptor == 0 && byteCount == 0)
        {
            throw new FtpTransferException("An empty block must carry a descriptor flag.", null);
        }
    }

    internal void ValidateFrame(FtpStructureFrame? frame)
    {
        if (frame == null || frame.Data == null || frame.Count < 0 || frame.Count > frame.Data.Length)
        {
            throw new FtpTransferException("A block-mode frame has invalid data bounds.", null);
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
                throw new FtpTransferException("Only an EOF frame may request block-mode EOF signaling.", null);
            }

            if (frame.Count == 0 && !frame.EndOfRecord)
            {
                throw new FtpTransferException("An empty block-mode frame must carry EOR or EOF.", null);
            }

            return;
        }

        if (frame.RequiresConnectionClose ||
            frame.RequiresStreamEndOfFileMarker ||
            !frame.RequiresEndOfFileDescriptor)
        {
            throw new FtpTransferException("Block mode requires an explicit EOF descriptor.", null);
        }
    }

    internal byte[] EncodeMarkerText(string? marker)
    {
        if (string.IsNullOrEmpty(marker) || marker.Length > MaximumBlockByteCount)
        {
            throw new FtpConfigurationException("A restart marker must contain from 1 through 65535 printable ASCII characters.", null);
        }

        byte[] encoded = new byte[marker.Length];
        int index = 0;
        while (index < marker.Length)
        {
            char value = marker[index];
            if (value < 33 || value > 126)
            {
                throw new FtpConfigurationException("A restart marker cannot contain spaces or non-printable ASCII characters.", null);
            }

            encoded[index] = (byte)value;
            index++;
        }

        return encoded;
    }

    internal string DecodeMarkerText(byte[] marker)
    {
        if (marker.Length == 0)
        {
            throw new FtpTransferException("A received restart marker cannot be empty.", null);
        }

        char[] decoded = new char[marker.Length];
        int index = 0;
        while (index < marker.Length)
        {
            byte value = marker[index];
            if (value < 33 || value > 126)
            {
                throw new FtpTransferException("A received restart marker is not printable ASCII without spaces.", null);
            }

            decoded[index] = (char)value;
            index++;
        }

        return new string(decoded);
    }

    internal void ValidateSourceSlice(byte[]? source, int offset, int count)
    {
        if (source == null)
        {
            throw new FtpTransferException("Block-mode source data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new FtpTransferException("The block-mode source slice is invalid.", null);
        }
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        if (source.Length != 0)
        {
            System.Array.Copy(source, 0, copy, 0, source.Length);
        }

        return copy;
    }
}
