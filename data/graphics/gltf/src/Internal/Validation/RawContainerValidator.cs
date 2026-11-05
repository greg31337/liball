using System;

namespace gltf;

internal class RawContainerValidationResult
{
    internal GltfValidationReport Report = new GltfValidationReport();
    internal GltfContainerData? Container;
}

internal class RawContainerValidator
{
    internal RawContainerValidationResult ValidateBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        RawContainerValidationResult result = new RawContainerValidationResult();
        if (bytes.Length == 0)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.InputEmpty,
                GltfValidationSeverity.Error,
                "The input does not contain a glTF document.",
                0);
            return result;
        }

        if (bytes.Length > GltfConstants.DefaultMaximumInputByteLength)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.InputLimitExceeded,
                GltfValidationSeverity.Error,
                $"Input length {bytes.Length} exceeds the supported maximum {GltfConstants.DefaultMaximumInputByteLength}.",
                GltfConstants.DefaultMaximumInputByteLength);
            return result;
        }

        if (HasGlbMagic(bytes))
        {
            return ValidateGlbBytes(bytes);
        }

        int jsonOffset = FindJsonStart(bytes);
        if (jsonOffset >= 0 && jsonOffset < bytes.Length && bytes[jsonOffset] == 0x7B)
        {
            GltfContainerData container = new GltfContainerData();
            container.Format = GltfContainerFormat.Json;
            container.JsonBytes = OwnedBytes.CopyArray(bytes);
            container.OriginalByteLength = bytes.Length;
            result.Container = container;
            return result;
        }

        string code = bytes[0] == 0x67
            ? GltfIssueCodes.GlbInvalidMagic
            : GltfIssueCodes.ContainerFormatUnknown;
        AddIssue(
            result.Report,
            code,
            GltfValidationSeverity.Error,
            "The input is neither a JSON glTF document nor a binary GLB container.",
            0);
        return result;
    }

    internal RawContainerValidationResult ValidateContainer(GltfContainerData container)
    {
        if (container == null)
        {
            throw new ArgumentNullException("container");
        }

        RawContainerValidationResult result = new RawContainerValidationResult();
        result.Container = container;
        if (container.Format != GltfContainerFormat.Json
            && container.Format != GltfContainerFormat.Glb)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.ContainerFormatUnknown,
                GltfValidationSeverity.Error,
                "Parsed container data must identify JSON glTF or GLB format.",
                0);
        }

        if (container.JsonBytes == null || container.JsonBytes.Length == 0)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.InputEmpty,
                GltfValidationSeverity.Error,
                "The container JSON chunk is empty.",
                0);
        }
        else if (container.JsonBytes.Length > GltfConstants.DefaultMaximumJsonByteLength)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.InputLimitExceeded,
                GltfValidationSeverity.Error,
                $"JSON content length {container.JsonBytes.Length} exceeds the supported maximum {GltfConstants.DefaultMaximumJsonByteLength}.",
                GltfConstants.DefaultMaximumJsonByteLength);
        }

        if (container.Format == GltfContainerFormat.Json && container.HasBinChunk)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.ContainerFormatMismatch,
                GltfValidationSeverity.Error,
                "A JSON glTF container cannot contain a GLB BIN chunk.",
                0);
        }

        if (container.HasBinChunk && container.BinBytes == null)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbUnexpectedEndOfChunkData,
                GltfValidationSeverity.Error,
                "The parsed GLB BIN chunk data is missing.",
                0);
        }

        if (container.UnknownChunks == null || container.TrailingBytes == null)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.ContainerFormatMismatch,
                GltfValidationSeverity.Error,
                "Parsed container collections and trailing bytes must not be null.",
                0);
        }

        return result;
    }

    internal RawContainerValidationResult ValidateGlbBytes(byte[] bytes)
    {
        RawContainerValidationResult result = new RawContainerValidationResult();
        if (bytes.Length < GltfConstants.GlbHeaderByteLength)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbUnexpectedEndOfHeader,
                GltfValidationSeverity.Error,
                "The GLB header is truncated.",
                bytes.Length);
            return result;
        }

        uint version = ReadUInt32(bytes, 4);
        if (version != (uint)GltfConstants.GlbVersion)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbInvalidVersion,
                GltfValidationSeverity.Error,
                $"Invalid GLB version value {version}.",
                4);
        }

        uint unsignedLength = ReadUInt32(bytes, 8);
        if (unsignedLength > (uint)int.MaxValue)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbChunkTooBig,
                GltfValidationSeverity.Error,
                $"Declared GLB length {unsignedLength} exceeds the supported signed length.",
                8);
            return result;
        }

        int declaredLength = (int)unsignedLength;
        if (declaredLength < GltfConstants.MinimumGlbByteLength)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbLengthTooSmall,
                GltfValidationSeverity.Error,
                $"Declared GLB length {declaredLength} is too small.",
                8);
            return result;
        }

        if (declaredLength != bytes.Length)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbLengthMismatch,
                GltfValidationSeverity.Error,
                $"Declared GLB length {declaredLength} does not match input length {bytes.Length}.",
                8);
            if (declaredLength < bytes.Length)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbExtraData,
                    GltfValidationSeverity.Warning,
                    "The GLB input contains data after its declared end.",
                    declaredLength);
            }

            return result;
        }

        GltfContainerData container = new GltfContainerData();
        container.Format = GltfContainerFormat.Glb;
        container.OriginalByteLength = bytes.Length;
        bool hasJson = false;
        bool hasBin = false;
        int chunkIndex = 0;
        int offset = GltfConstants.GlbHeaderByteLength;
        while (offset < declaredLength)
        {
            int remaining = declaredLength - offset;
            if (remaining < GltfConstants.GlbChunkHeaderByteLength)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbUnexpectedEndOfChunkHeader,
                    GltfValidationSeverity.Error,
                    "The GLB chunk header is truncated.",
                    offset);
                return result;
            }

            uint unsignedChunkLength = ReadUInt32(bytes, offset);
            uint chunkType = ReadUInt32(bytes, offset + 4);
            if (unsignedChunkLength > (uint)int.MaxValue)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbChunkTooBig,
                    GltfValidationSeverity.Error,
                    $"GLB chunk length {unsignedChunkLength} exceeds the supported signed length.",
                    offset);
                return result;
            }

            int chunkLength = (int)unsignedChunkLength;
            if (chunkLength % GltfConstants.AlignmentByteLength != 0)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbChunkLengthUnaligned,
                    GltfValidationSeverity.Error,
                    $"GLB chunk length {chunkLength} is not aligned to four bytes.",
                    offset);
            }

            int dataOffset = offset + GltfConstants.GlbChunkHeaderByteLength;
            if (chunkLength > declaredLength - dataOffset)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbUnexpectedEndOfChunkData,
                    GltfValidationSeverity.Error,
                    "The GLB chunk data extends beyond the declared container length.",
                    dataOffset);
                return result;
            }

            if (chunkIndex == 0 && chunkType != GltfConstants.GlbJsonChunkType)
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbUnexpectedFirstChunk,
                    GltfValidationSeverity.Error,
                    $"The first GLB chunk has type {chunkType} instead of JSON.",
                    offset + 4);
            }

            if (chunkType == GltfConstants.GlbJsonChunkType)
            {
                if (hasJson)
                {
                    AddIssue(
                        result.Report,
                        GltfIssueCodes.GlbDuplicateChunk,
                        GltfValidationSeverity.Error,
                        "The GLB contains more than one JSON chunk.",
                        offset + 4);
                }
                else if (chunkLength == 0)
                {
                    AddIssue(
                        result.Report,
                        GltfIssueCodes.GlbEmptyChunk,
                        GltfValidationSeverity.Error,
                        "The GLB JSON chunk is empty.",
                        dataOffset);
                }
                else
                {
                    container.JsonBytes = CopyRange(bytes, dataOffset, chunkLength);
                    ValidateJsonPadding(container.JsonBytes, dataOffset, result.Report);
                    hasJson = true;
                }
            }
            else if (chunkType == GltfConstants.GlbBinChunkType)
            {
                if (hasBin)
                {
                    AddIssue(
                        result.Report,
                        GltfIssueCodes.GlbDuplicateChunk,
                        GltfValidationSeverity.Error,
                        "The GLB contains more than one BIN chunk.",
                        offset + 4);
                }
                else
                {
                    if (chunkIndex != 1)
                    {
                        AddIssue(
                            result.Report,
                            GltfIssueCodes.GlbUnexpectedBinChunk,
                            GltfValidationSeverity.Error,
                            "The GLB BIN chunk must be the second chunk.",
                            offset + 4);
                    }

                    if (chunkLength == 0)
                    {
                        AddIssue(
                            result.Report,
                            GltfIssueCodes.GlbEmptyBinChunk,
                            GltfValidationSeverity.Information,
                            "An empty GLB BIN chunk should be omitted.",
                            dataOffset);
                    }

                    container.HasBinChunk = true;
                    container.BinBytes = CopyRange(bytes, dataOffset, chunkLength);
                    hasBin = true;
                }
            }
            else
            {
                AddIssue(
                    result.Report,
                    GltfIssueCodes.GlbUnknownChunkType,
                    GltfValidationSeverity.Warning,
                    $"Unknown GLB chunk type {chunkType}.",
                    offset + 4);
                GltfGlbChunk unknown = new GltfGlbChunk();
                unknown.Type = chunkType;
                unknown.Data = CopyRange(bytes, dataOffset, chunkLength);
                container.UnknownChunks.Add(unknown);
            }

            offset = dataOffset + chunkLength;
            chunkIndex++;
        }

        if (!hasJson)
        {
            AddIssue(
                result.Report,
                GltfIssueCodes.GlbEmptyChunk,
                GltfValidationSeverity.Error,
                "The GLB does not contain a JSON chunk.",
                GltfConstants.GlbHeaderByteLength);
        }

        if (result.Report.IsValid)
        {
            result.Container = container;
        }

        return result;
    }

    internal void ValidateJsonPadding(
        byte[] jsonBytes,
        int dataOffset,
        GltfValidationReport report)
    {
        int closingOffset = jsonBytes.Length - 1;
        while (closingOffset >= 0 && jsonBytes[closingOffset] != 0x7D)
        {
            closingOffset--;
        }

        int offset = closingOffset + 1;
        while (closingOffset >= 0 && offset < jsonBytes.Length)
        {
            if (jsonBytes[offset] != GltfConstants.GlbJsonPaddingByte)
            {
                AddIssue(
                    report,
                    GltfIssueCodes.InvalidJson,
                    GltfValidationSeverity.Error,
                    "The GLB JSON chunk contains invalid padding.",
                    dataOffset + offset);
                return;
            }

            offset++;
        }
    }

    internal bool HasGlbMagic(byte[] bytes)
    {
        return bytes.Length >= 4
            && ReadUInt32(bytes, 0) == GltfConstants.GlbMagic;
    }

    internal int FindJsonStart(byte[] bytes)
    {
        int offset = 0;
        if (bytes.Length >= 3
            && bytes[0] == 0xEF
            && bytes[1] == 0xBB
            && bytes[2] == 0xBF)
        {
            offset = 3;
        }

        while (offset < bytes.Length
            && (bytes[offset] == 0x20
                || bytes[offset] == 0x09
                || bytes[offset] == 0x0A
                || bytes[offset] == 0x0D))
        {
            offset++;
        }

        return offset;
    }

    internal uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset]
            | ((uint)bytes[offset + 1] << 8)
            | ((uint)bytes[offset + 2] << 16)
            | ((uint)bytes[offset + 3] << 24);
    }

    internal byte[] CopyRange(byte[] bytes, int offset, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(bytes, offset, result, 0, length);
        return result;
    }

    internal void AddIssue(
        GltfValidationReport report,
        string code,
        GltfValidationSeverity severity,
        string message,
        int byteOffset)
    {
        GltfValidationIssue issue = new GltfValidationIssue();
        issue.Code = code;
        issue.Severity = severity;
        issue.Message = message;
        issue.HasByteOffset = true;
        issue.ByteOffset = byteOffset;
        report.Issues.Add(issue);
        if (severity == GltfValidationSeverity.Error)
        {
            report.IsValid = false;
        }
    }
}
