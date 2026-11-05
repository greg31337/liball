using System.Collections.Generic;

namespace ftp;

internal class FtpTransferFramingTransform
{
    internal const int MaximumPendingPageBytes = 67108864;

    internal FtpTransferOptions Options;
    internal bool Encoding;
    internal FtpFileStructureState? FileStructure;
    internal FtpRecordStructureState? RecordStructure;
    internal FtpPageStructureState? PageStructure;
    internal FtpStreamModeState? StreamMode;
    internal FtpBlockModeState? BlockMode;
    internal FtpCompressedModeState? CompressedMode;
    internal List<byte> PendingPageBytes;
    internal List<byte> PendingRecordBytes;
    internal bool Completed;

    internal FtpTransferFramingTransform(
        FtpTransferOptions options,
        bool encoding)
    {
        FtpTransferValidator.ValidateTransferOptions(options);
        Options = options;
        Encoding = encoding;
        FileStructure = null;
        RecordStructure = null;
        PageStructure = null;
        StreamMode = null;
        BlockMode = null;
        CompressedMode = null;
        PendingPageBytes = new List<byte>();
        PendingRecordBytes = new List<byte>();
        Completed = false;

        if (options.Structure == FtpFileStructure.File)
        {
            FileStructure = FileStructureCodec.CreateFramingState(options);
        }
        else if (options.Structure == FtpFileStructure.Record)
        {
            RecordStructure = RecordStructureCodec.CreateFramingState(options);
        }
        else
        {
            PageStructure = PageStructureCodec.CreateFramingState(options);
        }

        if (options.Mode == FtpTransferMode.Stream ||
            options.Mode == FtpTransferMode.Deflate)
        {
            StreamMode = StreamModeCodec.CreateFramingState(
                options, false, 0);
        }
        else if (options.Mode == FtpTransferMode.Block)
        {
            BlockMode = BlockModeCodec.CreateFramingState(options);
        }
        else if (options.Mode == FtpTransferMode.Compressed)
        {
            CompressedMode = CompressedModeCodec.CreateFramingState(options);
        }
    }

    internal byte[] ConvertChunk(
        byte[] data,
        int count,
        bool finalChunk)
    {
        if (Completed)
        {
            throw new FtpTransferException(
                "Framed data cannot follow transfer completion.", null);
        }
        if (count < 0 || count > data.Length)
        {
            throw new FtpTransferException(
                "The transfer framing input bounds are invalid.", null);
        }

        byte[] result;
        if (Encoding)
        {
            result = EncodeChunk(data, count, finalChunk);
        }
        else
        {
            result = DecodeChunk(data, count, finalChunk);
        }
        if (finalChunk)
        {
            Completed = true;
        }
        return result;
    }

    internal byte[] EncodeChunk(
        byte[] data,
        int count,
        bool finalChunk)
    {
        FtpStructureFrame[] frames = CreateStructureFrames(
            data, count, finalChunk);
        List<byte> wire = new List<byte>();
        int index = 0;
        while (index < frames.Length)
        {
            EncodeModeFrame(frames[index], wire);
            index++;
        }
        return wire.ToArray();
    }

    internal FtpStructureFrame[] CreateStructureFrames(
        byte[] data,
        int count,
        bool finalChunk)
    {
        if (Options.Structure == FtpFileStructure.Page)
        {
            return CreatePageFrames(data, count, finalChunk);
        }
        if (Options.Structure == FtpFileStructure.Record)
        {
            FtpRecordStructureState? records = RecordStructure;
            if (records == null)
            {
                throw new FtpConfigurationException(
                    "A record-structure state is required.", null);
            }
            List<FtpStructureFrame> frames =
                new List<FtpStructureFrame>();
            if (count != 0)
            {
                frames.AddRange(records.ConvertData(data, 0, count));
            }
            if (finalChunk)
            {
                frames.AddRange(records.CompleteRecords());
            }
            if (Options.Mode == FtpTransferMode.Compressed)
            {
                return CoalesceCompressedRecordFrames(frames);
            }
            return frames.ToArray();
        }

        FtpFileStructureState? file = FileStructure;
        if (file == null)
        {
            throw new FtpConfigurationException(
                "A file-structure state is required.", null);
        }
        if (finalChunk)
        {
            return new FtpStructureFrame[]
            {
                file.FrameFinalData(data, 0, count)
            };
        }
        if (count == 0)
        {
            return new FtpStructureFrame[0];
        }
        return new FtpStructureFrame[]
        {
            file.FrameData(data, 0, count)
        };
    }

    internal FtpStructureFrame[] CoalesceCompressedRecordFrames(
        List<FtpStructureFrame> frames)
    {
        List<FtpStructureFrame> completed =
            new List<FtpStructureFrame>();
        int index = 0;
        while (index < frames.Count)
        {
            FtpStructureFrame frame = frames[index];
            if (frame.Data == null)
            {
                throw new FtpTransferException(
                    "A compressed record frame requires data.", null);
            }
            if (PendingRecordBytes.Count >
                MaximumPendingPageBytes - frame.Count)
            {
                throw new FtpTransferException(
                    "A record exceeds the bounded pending-record limit.",
                    null);
            }
            AppendSlice(PendingRecordBytes, frame.Data, frame.Count);
            if (frame.EndOfRecord || frame.EndOfFile)
            {
                FtpStructureFrame combined = new FtpStructureFrame();
                combined.Data = PendingRecordBytes.ToArray();
                combined.Count = PendingRecordBytes.Count;
                combined.EndOfRecord = frame.EndOfRecord;
                combined.EndOfFile = frame.EndOfFile;
                combined.RequiresEndOfFileDescriptor =
                    frame.RequiresEndOfFileDescriptor;
                completed.Add(combined);
                PendingRecordBytes.Clear();
            }
            index++;
        }
        return completed.ToArray();
    }

    internal FtpStructureFrame[] CreatePageFrames(
        byte[] data,
        int count,
        bool finalChunk)
    {
        AppendPendingPageBytes(data, count);
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        int pageLength = ResolvePendingPageLength();
        while (pageLength != 0 && PendingPageBytes.Count >= pageLength)
        {
            byte[] serialized = RemovePendingPage(pageLength);
            FtpPageStructureState? pages = PageStructure;
            if (pages == null)
            {
                throw new FtpConfigurationException(
                    "A page-structure state is required.", null);
            }
            FtpPageFrame page = pages.DecodePage(
                serialized, 0, serialized.Length);
            FtpStructureFrame frame = new FtpStructureFrame();
            frame.Data = page.SerializedLogicalBytes;
            frame.Count = page.SerializedLogicalBytes.Length;
            frame.EndOfFile = page.LastPage;
            if (page.LastPage &&
                (Options.Mode == FtpTransferMode.Block ||
                 Options.Mode == FtpTransferMode.Compressed))
            {
                frame.RequiresEndOfFileDescriptor = true;
            }
            frames.Add(frame);
            pageLength = ResolvePendingPageLength();
        }
        if (finalChunk)
        {
            if (PendingPageBytes.Count != 0)
            {
                throw new FtpTransferException(
                    "The page-structured input ended within a page.", null);
            }
            FtpPageStructureState? pages = PageStructure;
            if (pages == null)
            {
                throw new FtpConfigurationException(
                    "A page-structure state is required.", null);
            }
            pages.RequireEndOfFile();
        }
        return frames.ToArray();
    }

    internal void EncodeModeFrame(
        FtpStructureFrame frame,
        List<byte> wire)
    {
        if (Options.Mode == FtpTransferMode.Stream ||
            Options.Mode == FtpTransferMode.Deflate)
        {
            FtpStreamModeState? stream = StreamMode;
            if (stream == null)
            {
                throw new FtpConfigurationException(
                    "A stream-mode state is required.", null);
            }
            FtpStreamModeChunk chunk = stream.EncodeFrame(frame);
            wire.AddRange(chunk.Data);
            return;
        }
        if (Options.Mode == FtpTransferMode.Block)
        {
            FtpBlockModeState? block = BlockMode;
            if (block == null)
            {
                throw new FtpConfigurationException(
                    "A block-mode state is required.", null);
            }
            FtpBlockModeBlock[] blocks = block.EncodeFrame(frame, false);
            int index = 0;
            while (index < blocks.Length)
            {
                wire.AddRange(blocks[index].SerializedBytes);
                index++;
            }
            return;
        }

        FtpCompressedModeState? compressed = CompressedMode;
        if (compressed == null)
        {
            throw new FtpConfigurationException(
                "A compressed-mode state is required.", null);
        }
        FtpCompressedModeSegment[] segments =
            compressed.EncodeFrame(frame, false);
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            wire.AddRange(segments[segmentIndex].EncodedBytes);
            segmentIndex++;
        }
    }

    internal byte[] DecodeChunk(
        byte[] data,
        int count,
        bool finalChunk)
    {
        FtpStructureFrame[] frames = DecodeModeFrames(
            data, count, finalChunk);
        List<byte> structured = new List<byte>();
        int index = 0;
        while (index < frames.Length)
        {
            DecodeStructureFrame(frames[index], structured);
            index++;
        }
        if (Options.Structure == FtpFileStructure.Page && finalChunk)
        {
            CompleteDecodedPages(structured);
        }
        return structured.ToArray();
    }

    internal FtpStructureFrame[] DecodeModeFrames(
        byte[] data,
        int count,
        bool finalChunk)
    {
        List<FtpStructureFrame> frames = new List<FtpStructureFrame>();
        if (Options.Mode == FtpTransferMode.Stream ||
            Options.Mode == FtpTransferMode.Deflate)
        {
            FtpStreamModeState? stream = StreamMode;
            if (stream == null)
            {
                throw new FtpConfigurationException(
                    "A stream-mode state is required.", null);
            }
            if (finalChunk && count == 0 && stream.EndOfFileReached)
            {
                stream.RequireEndOfFile();
                return frames.ToArray();
            }
            bool connectionClosed = finalChunk &&
                Options.Structure != FtpFileStructure.Page;
            frames.AddRange(stream.DecodeBytes(
                data, 0, count, connectionClosed));
            return frames.ToArray();
        }
        if (Options.Mode == FtpTransferMode.Block)
        {
            FtpBlockModeState? block = BlockMode;
            if (block == null)
            {
                throw new FtpConfigurationException(
                    "A block-mode state is required.", null);
            }
            if (finalChunk && count == 0 && block.EndOfFileReached)
            {
                block.RequireEndOfFile();
                return frames.ToArray();
            }
            FtpBlockModeBlock[] blocks = block.DecodeBytes(
                data, 0, count, finalChunk);
            int index = 0;
            while (index < blocks.Length)
            {
                FtpStructureFrame? frame = blocks[index].Frame;
                if (frame != null)
                {
                    frames.Add(frame);
                }
                index++;
            }
            return frames.ToArray();
        }

        FtpCompressedModeState? compressed = CompressedMode;
        if (compressed == null)
        {
            throw new FtpConfigurationException(
                "A compressed-mode state is required.", null);
        }
        if (finalChunk && count == 0 && compressed.EndOfFileReached)
        {
            compressed.RequireEndOfFile();
            return frames.ToArray();
        }
        FtpCompressedModeSegment[] segments = compressed.DecodeBytes(
            data, 0, count, finalChunk);
        int segmentIndex = 0;
        while (segmentIndex < segments.Length)
        {
            FtpStructureFrame? frame = segments[segmentIndex].Frame;
            if (frame != null)
            {
                frames.Add(frame);
            }
            segmentIndex++;
        }
        return frames.ToArray();
    }

    internal void DecodeStructureFrame(
        FtpStructureFrame frame,
        List<byte> structured)
    {
        if (Options.Structure == FtpFileStructure.File)
        {
            FtpFileStructureState? file = FileStructure;
            if (file == null || frame.Data == null)
            {
                throw new FtpConfigurationException(
                    "A complete file-structure frame is required.", null);
            }
            file.ConsumeFrame(frame);
            AppendSlice(structured, frame.Data, frame.Count);
            return;
        }
        if (Options.Structure == FtpFileStructure.Record)
        {
            FtpRecordStructureState? records = RecordStructure;
            if (records == null)
            {
                throw new FtpConfigurationException(
                    "A record-structure state is required.", null);
            }
            structured.AddRange(records.ConvertFrame(frame));
            return;
        }

        if (frame.Data == null)
        {
            throw new FtpTransferException(
                "A page-structure frame requires data.", null);
        }
        AppendPendingPageBytes(frame.Data, frame.Count);
        AppendCompletePages(structured);
    }

    internal void CompleteDecodedPages(List<byte> structured)
    {
        AppendCompletePages(structured);
        if (PendingPageBytes.Count != 0)
        {
            throw new FtpTransferException(
                "The page-structured transfer ended within a page.", null);
        }
        FtpPageStructureState? pages = PageStructure;
        if (pages == null)
        {
            throw new FtpConfigurationException(
                "A page-structure state is required.", null);
        }
        pages.RequireEndOfFile();
        FtpStreamModeState? stream = StreamMode;
        if (stream != null)
        {
            FtpPageFrame lastPage = new FtpPageFrame();
            lastPage.HeaderLogicalByteCount = 4;
            lastPage.PageType = 0;
            lastPage.LastPage = true;
            stream.CompletePageStructure(lastPage);
        }
    }

    internal void AppendCompletePages(List<byte> structured)
    {
        int pageLength = ResolvePendingPageLength();
        while (pageLength != 0 && PendingPageBytes.Count >= pageLength)
        {
            byte[] serialized = RemovePendingPage(pageLength);
            FtpPageStructureState? pages = PageStructure;
            if (pages == null)
            {
                throw new FtpConfigurationException(
                    "A page-structure state is required.", null);
            }
            FtpPageFrame page = pages.DecodePage(
                serialized, 0, serialized.Length);
            structured.AddRange(page.SerializedLogicalBytes);
            pageLength = ResolvePendingPageLength();
        }
    }

    internal void AppendPendingPageBytes(byte[] data, int count)
    {
        if (PendingPageBytes.Count > MaximumPendingPageBytes - count)
        {
            throw new FtpTransferException(
                "A page exceeds the bounded pending-page limit.", null);
        }
        int index = 0;
        while (index < count)
        {
            PendingPageBytes.Add(data[index]);
            index++;
        }
    }

    internal int ResolvePendingPageLength()
    {
        FtpPageStructureState? pages = PageStructure;
        if (pages == null)
        {
            throw new FtpConfigurationException(
                "A page-structure state is required.", null);
        }
        int headerBytes = pages.StorageByteCount * 4;
        if (PendingPageBytes.Count < headerBytes)
        {
            return 0;
        }
        long headerCount = ReadPendingPageInteger(0);
        long dataCount = ReadPendingPageInteger(2);
        if (headerCount > int.MaxValue || dataCount > int.MaxValue ||
            headerCount > int.MaxValue - dataCount)
        {
            throw new FtpTransferException(
                "A page length exceeds supported buffer capacity.", null);
        }
        int logicalBytes = (int)headerCount + (int)dataCount;
        if (logicalBytes > MaximumPendingPageBytes /
            pages.StorageByteCount)
        {
            throw new FtpTransferException(
                "A page exceeds the bounded pending-page limit.", null);
        }
        return logicalBytes * pages.StorageByteCount;
    }

    internal long ReadPendingPageInteger(int logicalByteIndex)
    {
        FtpPageStructureState? pages = PageStructure;
        if (pages == null)
        {
            throw new FtpConfigurationException(
                "A page-structure state is required.", null);
        }
        int sourceIndex = logicalByteIndex * pages.StorageByteCount;
        int ignoredBytes = pages.StorageByteCount - 8;
        int index = 0;
        while (index < ignoredBytes)
        {
            if (PendingPageBytes[sourceIndex + index] != 0)
            {
                throw new FtpTransferException(
                    "A page length exceeds Int64 capacity.", null);
            }
            index++;
        }
        long value = 0;
        while (index < pages.StorageByteCount)
        {
            byte current = PendingPageBytes[sourceIndex + index];
            if (value > (long.MaxValue - current) / 256)
            {
                throw new FtpTransferException(
                    "A page length exceeds Int64 capacity.", null);
            }
            value = value * 256 + current;
            index++;
        }
        return value;
    }

    internal byte[] RemovePendingPage(int count)
    {
        byte[] page = new byte[count];
        int index = 0;
        while (index < count)
        {
            page[index] = PendingPageBytes[index];
            index++;
        }
        PendingPageBytes.RemoveRange(0, count);
        return page;
    }

    internal void AppendSlice(
        List<byte> destination,
        byte[] source,
        int count)
    {
        int index = 0;
        while (index < count)
        {
            destination.Add(source[index]);
            index++;
        }
    }

}
