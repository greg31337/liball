using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngRepairer
{
    internal static PngRepairResult Repair(byte[] data, int offset, PngRepairOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        ValidateSignature(data, offset);
        PngDecoderOptions validationOptions = CreateValidationOptions(options.Limits);
        PngValidationResult before = PngValidator.Validate(data, offset, validationOptions);
        PngValidationDatastreamReadResult readResult = PngValidationDatastreamReader.Read(data, offset + PngConstants.SignatureLength, options.Limits);
        byte[] original = CopyBytes(data, offset, data.Length - offset);
        if (HasUnrepairableFraming(readResult) || !readResult.ChunkList.FoundIend)
        {
            return CreateUnchangedResult(original, readResult.ChunkList.Chunks, before);
        }

        List<PngRepairAction> actions = new List<PngRepairAction>();
        PngChunk[] repaired = CloneChunks(readResult.ChunkList.Chunks);
        if (options.RemoveMalformedAncillaryChunks)
        {
            repaired = RemoveMalformedAncillaryChunks(repaired, before.Errors, actions);
        }

        if (options.RemoveDuplicateAncillaryChunks)
        {
            repaired = RemoveDuplicateAncillaryChunks(repaired, actions);
        }

        if (options.RepairAncillaryOrdering)
        {
            repaired = RepairAncillaryOrderingWithReport(repaired, actions);
        }

        if (options.RepairAncillaryCrc)
        {
            RepairAncillaryCrcWithReport(repaired, actions);
        }

        byte[] trailingBytes = readResult.ChunkList.TrailingBytes;
        if (options.RemoveTrailingData && trailingBytes.Length > 0)
        {
            actions.Add(CreateAction("PNG_REPAIR_TRAILING_DATA_REMOVED", "Removed bytes found after IEND.", "", GetTrailingOffset(readResult.ChunkList.Chunks)));
            trailingBytes = new byte[0];
        }

        byte[] output = original;
        if (actions.Count > 0)
        {
            output = WriteRepairResult(repaired, trailingBytes, options.Limits);
        }

        PngValidationResult after = PngValidator.Validate(output, 0, validationOptions);
        return CreateResult(output, repaired, before, after, actions);
    }

    internal static PngRepairResult Repair(Stream stream, PngRepairOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);

        return Repair(ReadAllBytes(stream), 0, options);
    }

    internal static PngRepairResult Repair(PngChunk[] chunks, PngRepairOptions options)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        byte[] data = WriteRepairResult(CloneChunks(chunks), new byte[0], options.Limits);
        return Repair(data, 0, options);
    }

    internal static PngDecoderOptions CreateValidationOptions(PngSecurityLimits limits)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.Strict;
        options.ValidateCrc = true;
        options.IgnoreTrailingBytes = false;
        options.Limits = limits;
        return options;
    }

    internal static bool HasUnrepairableFraming(PngValidationDatastreamReadResult readResult)
    {
        int index = 0;
        while (index < readResult.Errors.Length)
        {
            string code = readResult.Errors[index].Code;
            if (code == "PNG_CHUNK_FRAMING_TRUNCATED" || code == "PNG_CHUNK_PAYLOAD_TRUNCATED")
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static PngChunk[] RemoveMalformedAncillaryChunks(PngChunk[] chunks, PngError[] errors, List<PngRepairAction> actions)
    {
        List<PngChunk> repaired = new List<PngChunk>();
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (IsRepairableAncillaryChunk(chunk.Type) && IsMalformedAncillaryChunk(chunk, errors))
            {
                actions.Add(CreateAction("PNG_REPAIR_MALFORMED_ANCILLARY_REMOVED", "Removed a malformed ancillary metadata chunk.", chunk.Type, chunk.Offset));
            }
            else
            {
                repaired.Add(chunk);
            }

            index += 1;
        }

        return repaired.ToArray();
    }

    internal static bool IsMalformedAncillaryChunk(PngChunk chunk, PngError[] errors)
    {
        if (!PngChunkTypeValidator.IsValid(chunk.Type))
        {
            return true;
        }

        int index = 0;
        while (index < errors.Length)
        {
            PngError error = errors[index];
            if (error.ChunkType == chunk.Type && error.Offset == chunk.Offset && IsMalformedAncillaryError(error.Code))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool IsMalformedAncillaryError(string code)
    {
        return code != "PNG_ANCILLARY_AFTER_PLTE"
            && code != "PNG_ANCILLARY_AFTER_IDAT"
            && code != "PNG_CHUNK_DUPLICATE"
            && code != "PNG_CHUNK_CRC_MISMATCH"
            && code != "PNG_COLOR_METADATA_CONFLICT"
            && code != "PNG_TRAILING_DATA";
    }

    internal static PngChunk[] RemoveDuplicateAncillaryChunks(PngChunk[] chunks, List<PngRepairAction> actions)
    {
        List<PngChunk> repaired = new List<PngChunk>();
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (IsRepairableAncillaryChunk(chunk.Type)
                && PngChunkDuplicateValidator.MustAppearAtMostOnce(chunk.Type)
                && ContainsChunkType(repaired, chunk.Type))
            {
                actions.Add(CreateAction("PNG_REPAIR_DUPLICATE_ANCILLARY_REMOVED", "Removed a duplicate ancillary chunk whose type permits at most one occurrence.", chunk.Type, chunk.Offset));
            }
            else
            {
                repaired.Add(chunk);
            }

            index += 1;
        }

        return repaired.ToArray();
    }

    internal static bool ContainsChunkType(List<PngChunk> chunks, string chunkType)
    {
        int index = 0;
        while (index < chunks.Count)
        {
            if (chunks[index].Type == chunkType)
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static PngChunk[] RepairAncillaryOrderingWithReport(PngChunk[] chunks, List<PngRepairAction> actions)
    {
        PngChunk[] repaired = RepairAncillaryOrdering(chunks);
        int index = 0;
        while (index < repaired.Length)
        {
            PngChunk chunk = repaired[index];
            int originalIndex = FindChunkReference(chunks, chunk);
            if (originalIndex != index && IsRepairableAncillaryChunk(chunk.Type))
            {
                actions.Add(CreateAction("PNG_REPAIR_ANCILLARY_MOVED", "Moved an ancillary chunk to a valid datastream position.", chunk.Type, chunk.Offset));
            }

            index += 1;
        }

        return repaired;
    }

    internal static int FindChunkReference(PngChunk[] chunks, PngChunk chunk)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            if (chunks[index] == chunk)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static void RepairAncillaryCrcWithReport(PngChunk[] chunks, List<PngRepairAction> actions)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            if (IsRepairableAncillaryChunk(chunk.Type) && PngChunkTypeValidator.IsValid(chunk.Type))
            {
                uint expected = PngCrc32.CalculateChunkCrc(chunk.Type, chunk.Data);
                if (chunk.Crc != expected)
                {
                    chunk.Crc = expected;
                    actions.Add(CreateAction("PNG_REPAIR_ANCILLARY_CRC_RECALCULATED", "Recalculated an ancillary metadata chunk CRC.", chunk.Type, chunk.Offset));
                }
            }

            index += 1;
        }
    }

    internal static bool IsRepairableAncillaryChunk(string chunkType)
    {
        if (chunkType.Length != 4 || !PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return false;
        }

        return chunkType != PngConstants.ChunkAnimationControl
            && chunkType != PngConstants.ChunkFrameControl
            && chunkType != PngConstants.ChunkFrameData;
    }

    internal static PngChunk[] CloneChunks(PngChunk[] chunks)
    {
        PngChunk[] result = new PngChunk[chunks.Length];
        int index = 0;
        while (index < chunks.Length)
        {
            ValidateChunk(chunks[index]);
            result[index] = CloneChunk(chunks[index]);
            index += 1;
        }

        return result;
    }

    internal static PngChunk CloneChunk(PngChunk source)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = source.Type;
        chunk.Data = CopyBytes(source.Data, 0, source.Data.Length);
        chunk.Crc = source.Crc;
        chunk.Offset = source.Offset;
        chunk.Length = source.Length;
        chunk.IsKnown = source.IsKnown;
        chunk.IsCritical = source.IsCritical;
        chunk.IsPublic = source.IsPublic;
        chunk.IsReservedBitValid = source.IsReservedBitValid;
        chunk.IsSafeToCopy = source.IsSafeToCopy;
        chunk.Placement = source.Placement;
        chunk.PreservationState = source.PreservationState;
        return chunk;
    }

    internal static byte[] WriteRepairResult(PngChunk[] chunks, byte[] trailingBytes, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(trailingBytes);
        ArgumentNullException.ThrowIfNull(limits);

        if (chunks.Length == 0 || chunks[chunks.Length - 1].Type != PngConstants.ChunkIEND)
        {
            throw new InvalidDataException("PNG datastream must end with IEND.");
        }

        PngSecurityLimitEnforcer.EnforceDatastream(chunks, limits);

        MemoryStream stream = new MemoryStream();
        WriteSignature(stream);
        int index = 0;
        while (index < chunks.Length)
        {
            WriteRepairChunk(stream, chunks[index], limits);
            index += 1;
        }

        stream.Write(trailingBytes, 0, trailingBytes.Length);
        byte[] result = stream.ToArray();
        stream.Dispose();
        return result;
    }

    internal static void WriteRepairChunk(Stream stream, PngChunk chunk, PngSecurityLimits limits)
    {
        ValidateChunk(chunk);
        if (chunk.Type.Length != 4)
        {
            throw new InvalidDataException("PNG chunk type must contain exactly four characters.");
        }

        PngChunkType chunkType = PngChunkTypeParser.Parse((byte)chunk.Type[0], (byte)chunk.Type[1], (byte)chunk.Type[2], (byte)chunk.Type[3]);
        long limit = GetChunkDataLimit(chunkType, limits);
        PngSizeCalculator.CalculateChunkAllocation((uint)chunk.Data.Length, limit);
        PngBinaryWriter.WriteUInt32(stream, (uint)chunk.Data.Length);
        WriteTypeBytes(stream, chunk.Type);
        stream.Write(chunk.Data, 0, chunk.Data.Length);
        PngBinaryWriter.WriteUInt32(stream, chunk.Crc);
    }

    internal static byte[] CopyBytes(byte[] data, int offset, int length)
    {
        byte[] result = new byte[length];
        int index = 0;
        while (index < length)
        {
            result[index] = data[offset + index];
            index += 1;
        }

        return result;
    }

    internal static long GetTrailingOffset(PngChunk[] chunks)
    {
        if (chunks.Length == 0)
        {
            return 0;
        }

        PngChunk last = chunks[chunks.Length - 1];
        return last.Offset + 12 + last.Length;
    }

    internal static PngRepairAction CreateAction(string code, string message, string chunkType, long offset)
    {
        PngRepairAction action = new PngRepairAction();
        action.Code = code;
        action.Message = message;
        action.ChunkType = chunkType;
        action.Offset = offset;
        return action;
    }

    internal static PngRepairResult CreateUnchangedResult(byte[] data, PngChunk[] chunks, PngValidationResult validation)
    {
        return CreateResult(data, CloneChunks(chunks), validation, validation, new List<PngRepairAction>());
    }

    internal static PngRepairResult CreateResult(byte[] data, PngChunk[] chunks, PngValidationResult before, PngValidationResult after, List<PngRepairAction> actions)
    {
        PngRepairReport report = new PngRepairReport();
        report.Changed = actions.Count > 0;
        report.BeforeValidation = before;
        report.AfterValidation = after;
        report.Actions = actions.ToArray();
        report.HasUnrepairedCriticalErrors = HasCriticalErrors(after.Errors);
        PngRepairResult result = new PngRepairResult();
        result.Data = data;
        result.Chunks = chunks;
        result.Report = report;
        return result;
    }

    internal static bool HasCriticalErrors(PngError[] errors)
    {
        int index = 0;
        while (index < errors.Length)
        {
            PngError error = errors[index];
            if (!IsRepairableAncillaryError(error))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static bool IsRepairableAncillaryError(PngError error)
    {
        if (error.Code == "PNG_TRAILING_DATA")
        {
            return true;
        }

        return IsRepairableAncillaryChunk(error.ChunkType);
    }
    internal static byte[] RepairAncillaryOrdering(byte[] data)
    {
        return RepairAncillaryOrdering(data, 0);
    }

    internal static byte[] RepairAncillaryOrdering(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        PngSecurityLimits limits = new PngSecurityLimits();
        ValidateSignature(data, offset);
        PngChunkListResult chunkList = PngChunkListReader.Read(data, offset + PngConstants.SignatureLength, limits);
        PngChunk[] repairedChunks = RepairAncillaryOrdering(chunkList.Chunks);
        return WriteChunks(repairedChunks, limits);
    }

    internal static byte[] RepairAncillaryOrdering(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] data = ReadAllBytes(stream);
        return RepairAncillaryOrdering(data, 0);
    }

    internal static PngChunk[] RepairAncillaryOrdering(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        PngChunk[] beforePaletteAndIdat = new PngChunk[chunks.Length];
        PngChunk[] beforeIdat = new PngChunk[chunks.Length];
        PngChunk[] remaining = new PngChunk[chunks.Length];
        int beforePaletteAndIdatCount = 0;
        int beforeIdatCount = 0;
        int remainingCount = 0;
        bool seenPlte = false;
        bool seenIdat = false;
        int index = 0;

        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunk(chunk);

            if (MustAppearBeforePlteAndIdat(chunk.Type) && (seenPlte || seenIdat))
            {
                beforePaletteAndIdat[beforePaletteAndIdatCount] = chunk;
                beforePaletteAndIdatCount += 1;
            }
            else if (MustAppearBeforeIdat(chunk.Type) && seenIdat)
            {
                beforeIdat[beforeIdatCount] = chunk;
                beforeIdatCount += 1;
            }
            else
            {
                remaining[remainingCount] = chunk;
                remainingCount += 1;
            }

            if (chunk.Type == PngConstants.ChunkPLTE)
            {
                seenPlte = true;
            }
            else if (chunk.Type == PngConstants.ChunkIDAT)
            {
                seenIdat = true;
            }

            index += 1;
        }

        return BuildRepairedChunks(remaining, remainingCount, beforePaletteAndIdat, beforePaletteAndIdatCount, beforeIdat, beforeIdatCount);
    }

    internal static byte[] RepairAncillaryCrc(byte[] data)
    {
        return RepairAncillaryCrc(data, 0);
    }

    internal static byte[] RepairAncillaryCrc(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        PngSecurityLimits limits = new PngSecurityLimits();
        ValidateSignature(data, offset);
        PngChunkListResult chunkList = PngChunkListReader.Read(data, offset + PngConstants.SignatureLength, limits);
        PngChunk[] repairedChunks = RepairAncillaryCrc(chunkList.Chunks);
        return WriteChunks(repairedChunks, limits);
    }

    internal static byte[] RepairAncillaryCrc(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        byte[] data = ReadAllBytes(stream);
        return RepairAncillaryCrc(data, 0);
    }

    internal static PngChunk[] RepairAncillaryCrc(PngChunk[] chunks)
    {
        ArgumentNullException.ThrowIfNull(chunks);

        PngChunk[] repaired = new PngChunk[chunks.Length];
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ValidateChunk(chunk);
            if (IsRepairableAncillaryChunk(chunk.Type))
            {
                uint expectedCrc = PngCrc32.CalculateChunkCrc(chunk.Type, chunk.Data);
                if (chunk.Crc != expectedCrc)
                {
                    chunk.Crc = expectedCrc;
                }
            }

            repaired[index] = chunk;
            index += 1;
        }

        return repaired;
    }

    internal static PngChunk[] BuildRepairedChunks(PngChunk[] remaining, int remainingCount, PngChunk[] beforePaletteAndIdat, int beforePaletteAndIdatCount, PngChunk[] beforeIdat, int beforeIdatCount)
    {
        PngChunk[] repaired = new PngChunk[remainingCount + beforePaletteAndIdatCount + beforeIdatCount];
        int beforePaletteAndIdatIndex = FindBeforePaletteAndIdatIndex(remaining, remainingCount);
        int beforeIdatIndex = FindFirstChunkIndex(remaining, remainingCount, PngConstants.ChunkIDAT);
        if (beforeIdatIndex < 0)
        {
            beforeIdatIndex = FindFirstChunkIndex(remaining, remainingCount, PngConstants.ChunkIEND);
        }

        if (beforeIdatIndex < 0)
        {
            beforeIdatIndex = remainingCount;
        }

        int outputIndex = 0;
        int index = 0;
        while (index < remainingCount)
        {
            if (index == beforePaletteAndIdatIndex)
            {
                outputIndex = AppendChunks(repaired, outputIndex, beforePaletteAndIdat, beforePaletteAndIdatCount);
            }

            if (index == beforeIdatIndex)
            {
                outputIndex = AppendChunks(repaired, outputIndex, beforeIdat, beforeIdatCount);
            }

            repaired[outputIndex] = remaining[index];
            outputIndex += 1;
            index += 1;
        }

        if (beforePaletteAndIdatIndex == remainingCount)
        {
            outputIndex = AppendChunks(repaired, outputIndex, beforePaletteAndIdat, beforePaletteAndIdatCount);
        }

        if (beforeIdatIndex == remainingCount)
        {
            outputIndex = AppendChunks(repaired, outputIndex, beforeIdat, beforeIdatCount);
        }

        return repaired;
    }

    internal static int AppendChunks(PngChunk[] target, int targetIndex, PngChunk[] chunks, int count)
    {
        int index = 0;
        while (index < count)
        {
            target[targetIndex] = chunks[index];
            targetIndex += 1;
            index += 1;
        }

        return targetIndex;
    }

    internal static int FindBeforePaletteAndIdatIndex(PngChunk[] chunks, int count)
    {
        int index = FindFirstChunkIndex(chunks, count, PngConstants.ChunkPLTE);
        int idatIndex = FindFirstChunkIndex(chunks, count, PngConstants.ChunkIDAT);
        if (index < 0 || (idatIndex >= 0 && idatIndex < index))
        {
            index = idatIndex;
        }

        if (index < 0)
        {
            index = FindFirstChunkIndex(chunks, count, PngConstants.ChunkIEND);
        }

        if (index < 0)
        {
            index = count;
        }

        return index;
    }

    internal static int FindFirstChunkIndex(PngChunk[] chunks, int count, string chunkType)
    {
        int index = 0;
        while (index < count)
        {
            if (chunks[index].Type == chunkType)
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    internal static bool MustAppearBeforePlteAndIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkChromaticities
            || chunkType == PngConstants.ChunkGamma
            || chunkType == PngConstants.ChunkIccProfile
            || chunkType == PngConstants.ChunkSignificantBits
            || chunkType == PngConstants.ChunkStandardRgb
            || chunkType == PngConstants.ChunkCodingIndependentCodePoints
            || chunkType == PngConstants.ChunkMasteringDisplayColorVolume
            || chunkType == PngConstants.ChunkContentLightLevelInformation;
    }

    internal static bool MustAppearBeforeIdat(string chunkType)
    {
        return chunkType == PngConstants.ChunkTransparency
            || chunkType == PngConstants.ChunkBackgroundColor
            || chunkType == PngConstants.ChunkPaletteHistogram
            || chunkType == PngConstants.ChunkPhysicalPixels
            || chunkType == PngConstants.ChunkSuggestedPalette
            || chunkType == PngConstants.ChunkExif
            || chunkType == PngConstants.ChunkAnimationControl;
    }

    internal static void ValidateSignature(byte[] data, int offset)
    {
        PngValidationResult signatureResult = PngSignature.ValidateSignature(data, offset, PngErrorPolicy.Strict);
        if (!signatureResult.IsValid)
        {
            throw new InvalidDataException("PNG signature is invalid.");
        }
    }

    internal static byte[] WriteChunks(PngChunk[] chunks, PngSecurityLimits limits)
    {
        MemoryStream stream = new MemoryStream();
        WriteChunks(stream, chunks, limits);
        return stream.ToArray();
    }

    internal static void WriteChunks(Stream stream, PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ValidateChunks(chunks, limits);

        WriteSignature(stream);
        int index = 0;
        while (index < chunks.Length)
        {
            WriteChunk(stream, chunks[index], limits);
            index += 1;
        }
    }

    internal static void ValidateChunks(PngChunk[] chunks, PngSecurityLimits limits)
    {
        ArgumentNullException.ThrowIfNull(chunks);
        ArgumentNullException.ThrowIfNull(limits);

        if (chunks.Length == 0)
        {
            throw new InvalidDataException("PNG datastream must contain an IEND chunk.");
        }

        PngSecurityLimitEnforcer.EnforceDatastream(chunks, limits);

        int index = 0;
        while (index < chunks.Length)
        {
            ValidateChunk(chunks[index]);
            if (index < chunks.Length - 1 && chunks[index].Type == PngConstants.ChunkIEND)
            {
                throw new InvalidDataException("PNG datastream cannot contain chunks after IEND.");
            }

            index += 1;
        }

        if (chunks[chunks.Length - 1].Type != PngConstants.ChunkIEND)
        {
            throw new InvalidDataException("PNG datastream must end with IEND.");
        }
    }

    internal static void WriteChunk(Stream stream, PngChunk chunk, PngSecurityLimits limits)
    {
        PngChunkType chunkType = ValidateWritableChunk(chunk, limits);
        long offset = 0;
        if (stream.CanSeek)
        {
            offset = stream.Position;
        }

        PngBinaryWriter.WriteUInt32(stream, (uint)chunk.Data.Length);
        WriteTypeBytes(stream, chunk.Type);
        stream.Write(chunk.Data, 0, chunk.Data.Length);
        PngBinaryWriter.WriteUInt32(stream, chunk.Crc);
        UpdateChunkFields(chunk, chunkType, offset);
    }

    internal static PngChunkType ValidateWritableChunk(PngChunk chunk, PngSecurityLimits limits)
    {
        ValidateChunk(chunk);
        ArgumentNullException.ThrowIfNull(limits);

        if (chunk.Data.Length > int.MaxValue - 12)
        {
            throw new OverflowException();
        }

        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunk.Type);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(validationResult.Errors[0].Message);
        }

        PngChunkType chunkType = PngChunkTypeParser.Parse(
            (byte)chunk.Type[0],
            (byte)chunk.Type[1],
            (byte)chunk.Type[2],
            (byte)chunk.Type[3]);
        long limit = GetChunkDataLimit(chunkType, limits);
        PngSizeCalculator.CalculateChunkAllocation((uint)chunk.Data.Length, limit);
        return chunkType;
    }

    internal static long GetChunkDataLimit(PngChunkType chunkType, PngSecurityLimits limits)
    {
        if (chunkType.Name == PngConstants.ChunkIDAT)
        {
            return limits.MaxIdatBytes;
        }

        if (PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return limits.MaxMetadataBytes;
        }

        return 0;
    }

    internal static void WriteSignature(Stream stream)
    {
        stream.WriteByte(PngConstants.SignatureByte0);
        stream.WriteByte(PngConstants.SignatureByte1);
        stream.WriteByte(PngConstants.SignatureByte2);
        stream.WriteByte(PngConstants.SignatureByte3);
        stream.WriteByte(PngConstants.SignatureByte4);
        stream.WriteByte(PngConstants.SignatureByte5);
        stream.WriteByte(PngConstants.SignatureByte6);
        stream.WriteByte(PngConstants.SignatureByte7);
    }

    internal static void WriteTypeBytes(Stream stream, string chunkType)
    {
        stream.WriteByte((byte)chunkType[0]);
        stream.WriteByte((byte)chunkType[1]);
        stream.WriteByte((byte)chunkType[2]);
        stream.WriteByte((byte)chunkType[3]);
    }

    internal static void UpdateChunkFields(PngChunk chunk, PngChunkType chunkType, long offset)
    {
        chunk.Length = (uint)chunk.Data.Length;
        chunk.Offset = offset;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunkType.Name);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
    }

    internal static void ValidateChunk(PngChunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);
        ArgumentNullException.ThrowIfNull(chunk.Type);
        ArgumentNullException.ThrowIfNull(chunk.Data);
    }

    internal static byte[] ReadAllBytes(Stream stream)
    {
        MemoryStream output = new MemoryStream();
        stream.CopyTo(output);
        return output.ToArray();
    }
}
