using System.Collections.Generic;

namespace ftp;

internal class FtpCompressedModeState
{
    internal const int EndOfRecordDescriptor = 128;
    internal const int EndOfFileDescriptor = 64;
    internal const int SuspectedErrorsDescriptor = 32;
    internal const int RestartMarkerDescriptor = 16;
    internal const int MaximumLiteralCount = 127;
    internal const int MaximumRunCount = 63;

    internal FtpFileStructure Structure;
    internal FtpTransferType Type;
    internal byte FillerByte;
    internal bool EndOfFileReached;
    internal long WireBytes;
    internal long DataBytes;
    internal long SegmentCount;
    internal int OperationKind;
    internal int PendingKind;
    internal int PendingCount;
    internal int PendingDescriptor;
    internal int PendingHeader;
    internal byte[] PendingLiteral;
    internal int PendingLiteralCount;
    internal RestartMarkerCodec RestartMarkers;

    internal FtpCompressedModeState(FtpFileStructure structure, FtpTransferType type)
    {
        if (structure != FtpFileStructure.File &&
            structure != FtpFileStructure.Record &&
            structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("The compressed-mode file structure is invalid.", null);
        }

        if (type != FtpTransferType.Ascii &&
            type != FtpTransferType.Ebcdic &&
            type != FtpTransferType.Image &&
            type != FtpTransferType.LocalByte)
        {
            throw new FtpConfigurationException("The compressed-mode representation type is invalid.", null);
        }

        Structure = structure;
        Type = type;
        FillerByte = 0;
        if (type == FtpTransferType.Ascii)
        {
            FillerByte = 32;
        }
        else if (type == FtpTransferType.Ebcdic)
        {
            FillerByte = 64;
        }

        EndOfFileReached = false;
        WireBytes = 0;
        DataBytes = 0;
        SegmentCount = 0;
        OperationKind = 0;
        PendingKind = 0;
        PendingCount = 0;
        PendingDescriptor = 0;
        PendingHeader = 0;
        PendingLiteral = new byte[0];
        PendingLiteralCount = 0;
        RestartMarkers = new RestartMarkerCodec();
    }

    internal FtpCompressedModeSegment[] EncodeFrame(
        FtpStructureFrame? frame,
        bool suspectedErrors)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A compressed-mode state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Compressed-mode data cannot follow end-of-file.", null);
        }

        ValidateFrame(frame);
        if (frame == null || frame.Data == null)
        {
            throw new FtpTransferException("A complete compressed-mode frame is required.", null);
        }

        List<FtpCompressedModeSegment> segments = EncodeDataSegments(frame.Data, frame.Count);
        if (segments.Count == 0)
        {
            int descriptor = CreateDescriptor(
                frame.EndOfRecord, frame.EndOfFile, suspectedErrors, false);
            byte[] control = new byte[] { 0, (byte)descriptor };
            segments.Add(CreateSegment(new byte[0], control, descriptor));
        }
        else
        {
            int index = 0;
            while (index < segments.Count)
            {
                bool finalSegment = index + 1 == segments.Count;
                int descriptor = CreateDescriptor(
                    finalSegment && frame.EndOfRecord,
                    finalSegment && frame.EndOfFile,
                    suspectedErrors,
                    false);
                if (descriptor != 0)
                {
                    ApplyDescriptor(segments[index], descriptor);
                }

                index++;
            }
        }

        RegisterEncodedSegments(segments, frame.Count, false);
        if (frame.EndOfFile)
        {
            EndOfFileReached = true;
        }

        return segments.ToArray();
    }

    internal FtpCompressedModeSegment[] EncodePage(
        FtpPageFrame? page,
        bool suspectedErrors)
    {
        if (Structure != FtpFileStructure.Page)
        {
            throw new FtpConfigurationException("Only page structure can encode page frames.", null);
        }

        if (page == null || page.SerializedLogicalBytes == null)
        {
            throw new FtpTransferException("A complete page frame is required for compressed-mode encoding.", null);
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

    internal FtpCompressedModeSegment EncodeRestartMarker(string? marker)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A compressed-mode state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("A restart marker cannot follow compressed-mode EOF.", null);
        }

        byte[] markerBytes = EncodeMarkerText(marker);
        byte[] encoded = new byte[markerBytes.Length + 3];
        encoded[0] = 0;
        encoded[1] = RestartMarkerDescriptor;
        encoded[2] = (byte)markerBytes.Length;
        System.Array.Copy(markerBytes, 0, encoded, 3, markerBytes.Length);
        FtpCompressedModeSegment segment = CreateSegment(
            markerBytes, encoded, RestartMarkerDescriptor);
        List<FtpCompressedModeSegment> segments = new List<FtpCompressedModeSegment>();
        segments.Add(segment);
        RegisterEncodedSegments(segments, 0, true);
        return segment;
    }

    internal FtpCompressedModeSegment[] DecodeBytes(
        byte[]? source,
        int offset,
        int count,
        bool endOfInput)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A compressed-mode state cannot switch from encoding to decoding.", null);
        }

        OperationKind = 2;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("Compressed-mode data cannot follow end-of-file.", null);
        }

        ValidateSourceSlice(source, offset, count);
        if (source == null)
        {
            throw new FtpTransferException("Compressed-mode source data is required.", null);
        }

        if (WireBytes > long.MaxValue - count)
        {
            throw new FtpTransferException("The compressed-mode wire byte count exceeded Int64 capacity.", null);
        }

        List<FtpCompressedModeSegment> segments = new List<FtpCompressedModeSegment>();
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            if (PendingKind == 1)
            {
                int descriptor = source[index];
                index++;
                ValidateDescriptor(descriptor);
                if (PendingDescriptor != 0)
                {
                    throw new FtpTransferException("A compressed descriptor must be followed by one data string.", null);
                }

                PendingDescriptor = descriptor;
                PendingKind = 0;
                continue;
            }

            if (PendingKind == 2)
            {
                int literalBytes = PendingCount - PendingLiteralCount;
                int available = end - index;
                if (literalBytes > available)
                {
                    literalBytes = available;
                }

                System.Array.Copy(source, index, PendingLiteral,
                    PendingLiteralCount, literalBytes);
                PendingLiteralCount += literalBytes;
                index += literalBytes;
                if (PendingLiteralCount == PendingCount)
                {
                    byte[] token = new byte[PendingCount + 1];
                    token[0] = (byte)PendingHeader;
                    System.Array.Copy(PendingLiteral, 0, token, 1, PendingCount);
                    AddDecodedSegment(segments, PendingLiteral, token);
                    ClearPendingToken();
                    if (EndOfFileReached && index != end)
                    {
                        throw new FtpTransferException("Compressed-mode data cannot follow an EOF descriptor.", null);
                    }
                }

                continue;
            }

            if (PendingKind == 3)
            {
                byte repeatedValue = source[index];
                index++;
                byte[] repeated = CreateRepeatedBytes(repeatedValue, PendingCount);
                byte[] token = new byte[] { (byte)PendingHeader, repeatedValue };
                AddDecodedSegment(segments, repeated, token);
                ClearPendingToken();
                if (EndOfFileReached && index != end)
                {
                    throw new FtpTransferException("Compressed-mode data cannot follow an EOF descriptor.", null);
                }

                continue;
            }

            int header = source[index];
            index++;
            if (header == 0)
            {
                if (PendingDescriptor != 0)
                {
                    throw new FtpTransferException("A compressed descriptor must be followed by one data string.", null);
                }

                PendingKind = 1;
            }
            else if (header < 128)
            {
                PendingKind = 2;
                PendingCount = header;
                PendingHeader = header;
                PendingLiteral = new byte[PendingCount];
                PendingLiteralCount = 0;
            }
            else if (header < 192)
            {
                int runCount = header & 0x3F;
                if (runCount == 0)
                {
                    throw new FtpTransferException("A replicated compressed string must have a positive count.", null);
                }

                PendingKind = 3;
                PendingCount = runCount;
                PendingHeader = header;
            }
            else
            {
                int fillCount = header & 0x3F;
                if (fillCount == 0)
                {
                    throw new FtpTransferException("A filler compressed string must have a positive count.", null);
                }

                byte[] filler = CreateRepeatedBytes(FillerByte, fillCount);
                byte[] token = new byte[] { (byte)header };
                AddDecodedSegment(segments, filler, token);
                if (EndOfFileReached && index != end)
                {
                    throw new FtpTransferException("Compressed-mode data cannot follow an EOF descriptor.", null);
                }
            }
        }

        if (endOfInput)
        {
            if (PendingKind != 0)
            {
                throw new FtpTransferException("The compressed-mode stream ended inside an encoded string or escape.", null);
            }

            if (PendingDescriptor != 0)
            {
                if ((PendingDescriptor & RestartMarkerDescriptor) != 0)
                {
                    throw new FtpTransferException("A compressed restart descriptor requires marker data.", null);
                }

                if ((PendingDescriptor & EndOfFileDescriptor) == 0)
                {
                    throw new FtpTransferException("A trailing compressed descriptor does not mark EOF.", null);
                }

                FtpCompressedModeSegment empty = CreateSegment(
                    new byte[0], new byte[] { 0, (byte)PendingDescriptor }, PendingDescriptor);
                RegisterDecodedSegment(empty);
                segments.Add(empty);
                PendingDescriptor = 0;
            }

            if (!EndOfFileReached)
            {
                throw new FtpTransferException("The compressed-mode stream ended without an EOF descriptor.", null);
            }
        }

        WireBytes += count;
        return segments.ToArray();
    }

    internal void RequireEndOfFile()
    {
        if (PendingKind != 0 || PendingDescriptor != 0)
        {
            throw new FtpTransferException("The compressed-mode stream ended with incomplete encoded state.", null);
        }

        if (!EndOfFileReached)
        {
            throw new FtpTransferException("The compressed-mode stream ended without an EOF descriptor.", null);
        }
    }

    internal List<FtpCompressedModeSegment> EncodeDataSegments(byte[] source, int count)
    {
        List<FtpCompressedModeSegment> segments = new List<FtpCompressedModeSegment>();
        List<byte> literal = new List<byte>();
        int index = 0;
        while (index < count)
        {
            byte value = source[index];
            int runLength = 1;
            while (index + runLength < count &&
                source[index + runLength] == value &&
                runLength < int.MaxValue)
            {
                runLength++;
            }

            if (value == FillerByte || runLength >= 2)
            {
                FlushLiteral(segments, literal);
                int remaining = runLength;
                while (remaining != 0)
                {
                    int encodedCount = remaining;
                    if (encodedCount > MaximumRunCount)
                    {
                        encodedCount = MaximumRunCount;
                    }

                    byte[] decoded = CreateRepeatedBytes(value, encodedCount);
                    byte[] encoded;
                    if (value == FillerByte)
                    {
                        encoded = new byte[] { (byte)(0xC0 | encodedCount) };
                    }
                    else
                    {
                        encoded = new byte[] { (byte)(0x80 | encodedCount), value };
                    }

                    segments.Add(CreateSegment(decoded, encoded, 0));
                    remaining -= encodedCount;
                }

                index += runLength;
            }
            else
            {
                literal.Add(value);
                index++;
                if (literal.Count == MaximumLiteralCount)
                {
                    FlushLiteral(segments, literal);
                }
            }
        }

        FlushLiteral(segments, literal);
        return segments;
    }

    internal void FlushLiteral(
        List<FtpCompressedModeSegment> segments,
        List<byte> literal)
    {
        if (literal.Count == 0)
        {
            return;
        }

        byte[] decoded = literal.ToArray();
        byte[] encoded = new byte[decoded.Length + 1];
        encoded[0] = (byte)decoded.Length;
        System.Array.Copy(decoded, 0, encoded, 1, decoded.Length);
        segments.Add(CreateSegment(decoded, encoded, 0));
        literal.Clear();
    }

    internal void ApplyDescriptor(FtpCompressedModeSegment segment, int descriptor)
    {
        byte[] encoded = new byte[segment.EncodedBytes.Length + 2];
        encoded[0] = 0;
        encoded[1] = (byte)descriptor;
        System.Array.Copy(segment.EncodedBytes, 0, encoded, 2, segment.EncodedBytes.Length);
        segment.EncodedBytes = encoded;
        ApplyDescriptorFields(segment, descriptor);
    }

    internal void ApplyDescriptorFields(FtpCompressedModeSegment segment, int descriptor)
    {
        segment.EndOfRecord = (descriptor & EndOfRecordDescriptor) != 0;
        segment.EndOfFile = (descriptor & EndOfFileDescriptor) != 0;
        segment.SuspectedErrors = (descriptor & SuspectedErrorsDescriptor) != 0;
        segment.IsRestartMarker = (descriptor & RestartMarkerDescriptor) != 0;
        if (segment.IsRestartMarker)
        {
            segment.RestartMarkerText = DecodeMarkerText(segment.Data);
            RestartMarkers.AcceptOpaqueMarker(
                segment.RestartMarkerText, MaximumLiteralCount);
            segment.Frame = null;
        }
        else
        {
            FtpStructureFrame frame = new FtpStructureFrame();
            frame.Data = CopyBytes(segment.Data);
            frame.Count = segment.Count;
            frame.EndOfRecord = segment.EndOfRecord;
            frame.EndOfFile = segment.EndOfFile;
            if (segment.EndOfFile)
            {
                frame.RequiresEndOfFileDescriptor = true;
            }

            segment.Frame = frame;
        }
    }

    internal void AddDecodedSegment(
        List<FtpCompressedModeSegment> segments,
        byte[] decoded,
        byte[] token)
    {
        byte[] encoded = token;
        int descriptor = PendingDescriptor;
        if (descriptor != 0)
        {
            encoded = new byte[token.Length + 2];
            encoded[0] = 0;
            encoded[1] = (byte)descriptor;
            System.Array.Copy(token, 0, encoded, 2, token.Length);
        }

        FtpCompressedModeSegment segment = CreateSegment(decoded, encoded, descriptor);
        PendingDescriptor = 0;
        RegisterDecodedSegment(segment);
        segments.Add(segment);
    }

    internal FtpCompressedModeSegment CreateSegment(
        byte[] decoded,
        byte[] encoded,
        int descriptor)
    {
        FtpCompressedModeSegment segment = new FtpCompressedModeSegment();
        segment.Data = CopyBytes(decoded);
        segment.Count = decoded.Length;
        segment.EncodedBytes = CopyBytes(encoded);
        ApplyDescriptorFields(segment, descriptor);
        return segment;
    }

    internal void RegisterEncodedSegments(
        List<FtpCompressedModeSegment> segments,
        int dataCount,
        bool restartMarker)
    {
        long encodedCount = 0;
        int index = 0;
        while (index < segments.Count)
        {
            if (encodedCount > long.MaxValue - segments[index].EncodedBytes.Length)
            {
                throw new FtpTransferException("The compressed-mode wire byte count exceeded Int64 capacity.", null);
            }

            encodedCount += segments[index].EncodedBytes.Length;
            index++;
        }

        if (WireBytes > long.MaxValue - encodedCount)
        {
            throw new FtpTransferException("The compressed-mode wire byte count exceeded Int64 capacity.", null);
        }

        if (!restartMarker && DataBytes > long.MaxValue - dataCount)
        {
            throw new FtpTransferException("The compressed-mode data byte count exceeded Int64 capacity.", null);
        }

        if (SegmentCount > long.MaxValue - segments.Count)
        {
            throw new FtpTransferException("The compressed-mode segment count exceeded Int64 capacity.", null);
        }

        WireBytes += encodedCount;
        if (!restartMarker)
        {
            DataBytes += dataCount;
        }

        SegmentCount += segments.Count;
    }

    internal void RegisterDecodedSegment(FtpCompressedModeSegment segment)
    {
        if (!segment.IsRestartMarker && DataBytes > long.MaxValue - segment.Count)
        {
            throw new FtpTransferException("The compressed-mode data byte count exceeded Int64 capacity.", null);
        }

        if (SegmentCount == long.MaxValue)
        {
            throw new FtpTransferException("The compressed-mode segment count exceeded Int64 capacity.", null);
        }

        if (!segment.IsRestartMarker)
        {
            DataBytes += segment.Count;
        }

        SegmentCount++;
        if (segment.EndOfFile)
        {
            EndOfFileReached = true;
        }
    }

    internal void ClearPendingToken()
    {
        PendingKind = 0;
        PendingCount = 0;
        PendingHeader = 0;
        PendingLiteral = new byte[0];
        PendingLiteralCount = 0;
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

    internal void ValidateDescriptor(int descriptor)
    {
        if (descriptor == 0 || (descriptor & 0x0F) != 0)
        {
            throw new FtpTransferException("The compressed escape contains undefined descriptor bits.", null);
        }

        if ((descriptor & EndOfRecordDescriptor) != 0 &&
            Structure != FtpFileStructure.Record)
        {
            throw new FtpTransferException("Only record structure can contain a compressed-mode EOR descriptor.", null);
        }
    }

    internal void ValidateFrame(FtpStructureFrame? frame)
    {
        if (frame == null || frame.Data == null || frame.Count < 0 || frame.Count > frame.Data.Length)
        {
            throw new FtpTransferException("A compressed-mode frame has invalid data bounds.", null);
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
                throw new FtpTransferException("Only an EOF frame may request compressed-mode EOF signaling.", null);
            }

            if (frame.Count == 0)
            {
                throw new FtpTransferException("Compressed mode cannot encode an empty non-final data string.", null);
            }

            return;
        }

        if (frame.RequiresConnectionClose ||
            frame.RequiresStreamEndOfFileMarker ||
            !frame.RequiresEndOfFileDescriptor)
        {
            throw new FtpTransferException("Compressed mode requires an explicit EOF descriptor.", null);
        }
    }

    internal byte[] CreateRepeatedBytes(byte value, int count)
    {
        byte[] repeated = new byte[count];
        int index = 0;
        while (index < count)
        {
            repeated[index] = value;
            index++;
        }

        return repeated;
    }

    internal byte[] EncodeMarkerText(string? marker)
    {
        if (string.IsNullOrEmpty(marker) || marker.Length > MaximumLiteralCount)
        {
            throw new FtpConfigurationException("A compressed restart marker must contain from 1 through 127 printable ASCII characters.", null);
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
            throw new FtpTransferException("Compressed-mode source data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new FtpTransferException("The compressed-mode source slice is invalid.", null);
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
