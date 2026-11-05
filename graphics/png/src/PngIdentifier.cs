using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngIdentifier
{
    internal static PngIdentifyResult Identify(byte[] data)
    {
        return Identify(data, 0, new PngDecoderOptions());
    }

    internal static PngIdentifyResult Identify(byte[] data, int offset)
    {
        return Identify(data, offset, new PngDecoderOptions());
    }

    internal static PngIdentifyResult Identify(byte[] data, int offset, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        PngValidationResult signatureResult = PngSignature.ValidateSignature(data, offset, options.ErrorPolicy);
        if (!signatureResult.IsValid)
        {
            return CreateSignatureFailure(signatureResult);
        }

        PngIdentifyState state = CreateState();
        state.Result.IsPng = true;

        int currentOffset = offset + PngConstants.SignatureLength;
        while (currentOffset < data.Length)
        {
            EnforceChunkCountLimit(state.Result.ChunkCount, options.Limits);
            PngChunkHeader header = ReadHeader(data, currentOffset);
            ValidateChunkType(header.ChunkType);
            EnforceArrayChunkAvailable(data, currentOffset, header.Length);
            state.Result.ChunkCount += 1;

            if (header.Name == PngConstants.ChunkIDAT)
            {
                AddIdatBytes(state.Result, header.Length);
                AddImageDataPlaceholder(state, header, currentOffset);
            }
            else
            {
                PngChunk chunk = ReadMetadataChunk(data, currentOffset, header, options.Limits);
                AddChunk(state, chunk);
                if (chunk.Type == PngConstants.ChunkIHDR)
                {
                    ReadIhdr(state, chunk, options.Limits);
                }

                if (chunk.Type == PngConstants.ChunkAnimationControl)
                {
                    ReadActl(state, chunk, options.Limits);
                }

                if (chunk.Type == PngConstants.ChunkIEND)
                {
                    state.Result.FoundIend = true;
                    break;
                }
            }

            currentOffset += 12 + (int)header.Length;
        }

        FinishState(state, options);
        return state.Result;
    }

    internal static PngIdentifyResult Identify(Stream stream)
    {
        return Identify(stream, new PngDecoderOptions());
    }

    internal static PngIdentifyResult Identify(Stream stream, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        byte[] signature = ReadSignature(stream);
        PngValidationResult signatureResult = PngSignature.ValidateSignature(signature, 0, options.ErrorPolicy);
        if (!signatureResult.IsValid)
        {
            return CreateSignatureFailure(signatureResult);
        }

        PngIdentifyState state = CreateState();
        state.Result.IsPng = true;

        long currentOffset = PngConstants.SignatureLength;
        if (stream.CanSeek)
        {
            currentOffset = stream.Position;
        }

        while (!IsAtEnd(stream))
        {
            EnforceChunkCountLimit(state.Result.ChunkCount, options.Limits);
            long offset = currentOffset;
            PngChunkHeader header = ReadHeader(stream);
            ValidateChunkType(header.ChunkType);
            state.Result.ChunkCount += 1;

            if (header.Name == PngConstants.ChunkIDAT)
            {
                AddIdatBytes(state.Result, header.Length);
                AddImageDataPlaceholder(state, header, offset);
                SkipChunkData(stream, header.Length);
                header.Crc = PngBinaryReader.ReadUInt32(stream);
            }
            else
            {
                PngChunk chunk = ReadMetadataChunk(stream, offset, header, options.Limits);
                AddChunk(state, chunk);
                if (chunk.Type == PngConstants.ChunkIHDR)
                {
                    ReadIhdr(state, chunk, options.Limits);
                }

                if (chunk.Type == PngConstants.ChunkAnimationControl)
                {
                    ReadActl(state, chunk, options.Limits);
                }

                if (chunk.Type == PngConstants.ChunkIEND)
                {
                    state.Result.FoundIend = true;
                    break;
                }
            }

            currentOffset += 12 + header.Length;
        }

        FinishState(state, options);
        return state.Result;
    }

    internal static PngIdentifyState CreateState()
    {
        PngIdentifyState state = new PngIdentifyState();
        state.Result = new PngIdentifyResult();
        state.Chunks = new List<PngChunk>();
        state.AllChunks = new List<PngChunk>();
        state.Warnings = new List<PngWarning>();
        state.Errors = new List<PngError>();
        return state;
    }

    internal static PngIdentifyResult CreateSignatureFailure(PngValidationResult signatureResult)
    {
        PngIdentifyResult result = new PngIdentifyResult();
        result.IsPng = false;
        result.IsValid = false;
        result.Info = signatureResult.Info;
        result.Warnings = signatureResult.Warnings;
        result.Errors = signatureResult.Errors;
        return result;
    }

    internal static byte[] ReadSignature(Stream stream)
    {
        byte[] signature = new byte[PngConstants.SignatureLength];
        int totalRead = 0;
        while (totalRead < PngConstants.SignatureLength)
        {
            int read = stream.Read(signature, totalRead, PngConstants.SignatureLength - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (totalRead != PngConstants.SignatureLength)
        {
            byte[] shortSignature = new byte[totalRead];
            int index = 0;
            while (index < totalRead)
            {
                shortSignature[index] = signature[index];
                index += 1;
            }

            return shortSignature;
        }

        return signature;
    }

    internal static PngChunkHeader ReadHeader(byte[] data, int offset)
    {
        if (offset > data.Length - 12)
        {
            throw new EndOfStreamException();
        }

        PngChunkHeader header = new PngChunkHeader();
        header.Length = PngBinaryReader.ReadUInt32(data, offset);
        header.ChunkType = PngChunkTypeParser.Parse(data, offset + 4);
        header.Name = header.ChunkType.Name;
        return header;
    }

    internal static PngChunkHeader ReadHeader(Stream stream)
    {
        PngChunkHeader header = new PngChunkHeader();
        header.Length = PngBinaryReader.ReadUInt32(stream);
        byte typeByte0 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte1 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte2 = PngBinaryReader.ReadUInt8(stream);
        byte typeByte3 = PngBinaryReader.ReadUInt8(stream);
        header.ChunkType = PngChunkTypeParser.Parse(typeByte0, typeByte1, typeByte2, typeByte3);
        header.Name = header.ChunkType.Name;
        return header;
    }

    internal static void ValidateChunkType(PngChunkType chunkType)
    {
        PngValidationResult validationResult = PngChunkTypeValidator.Validate(chunkType);
        if (!validationResult.IsValid)
        {
            throw new InvalidDataException(validationResult.Errors[0].Message);
        }
    }

    internal static void EnforceArrayChunkAvailable(byte[] data, int offset, uint length)
    {
        long dataOffset = offset + 8;
        long available = data.Length - dataOffset - 4;
        if (available < 0)
        {
            throw new EndOfStreamException();
        }

        if (length > (ulong)available)
        {
            throw new EndOfStreamException();
        }
    }

    internal static PngChunk ReadMetadataChunk(byte[] data, int offset, PngChunkHeader header, PngSecurityLimits limits)
    {
        int allocation = PngSizeCalculator.CalculateChunkAllocation(header.Length, GetMetadataLimit(header.ChunkType, limits));
        byte[] chunkData = new byte[allocation];
        int index = 0;
        while (index < allocation)
        {
            chunkData[index] = data[offset + 8 + index];
            index += 1;
        }

        uint crc = PngBinaryReader.ReadUInt32(data, offset + 8 + allocation);
        return CreateChunk(header.ChunkType, chunkData, crc, offset, header.Length);
    }

    internal static PngChunk ReadMetadataChunk(Stream stream, long offset, PngChunkHeader header, PngSecurityLimits limits)
    {
        int allocation = PngSizeCalculator.CalculateChunkAllocation(header.Length, GetMetadataLimit(header.ChunkType, limits));
        byte[] chunkData = new byte[allocation];
        int totalRead = 0;
        while (totalRead < allocation)
        {
            int read = stream.Read(chunkData, totalRead, allocation - totalRead);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            totalRead += read;
        }

        uint crc = PngBinaryReader.ReadUInt32(stream);
        return CreateChunk(header.ChunkType, chunkData, crc, offset, header.Length);
    }

    internal static long GetMetadataLimit(PngChunkType chunkType, PngSecurityLimits limits)
    {
        if (chunkType.Name == PngConstants.ChunkFrameData)
        {
            if (limits.MaxIdatBytes <= 0)
            {
                return 0;
            }

            return PngSecurityLimitEnforcer.AddBytes(limits.MaxIdatBytes, PngFrameDataParser.SequenceNumberLength, 0);
        }

        if (PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return limits.MaxMetadataBytes;
        }

        return 0;
    }

    internal static PngChunk CreateChunk(PngChunkType chunkType, byte[] data, uint crc, long offset, uint length)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = chunkType.Name;
        chunk.Data = data;
        chunk.Crc = crc;
        chunk.Offset = offset;
        chunk.Length = length;
        chunk.IsKnown = PngKnownChunkTypes.IsKnown(chunkType.Name);
        chunk.IsCritical = PngChunkTypeFlags.IsCritical(chunkType);
        chunk.IsPublic = PngChunkTypeFlags.IsPublic(chunkType);
        chunk.IsReservedBitValid = PngChunkTypeFlags.IsReservedBitValid(chunkType);
        chunk.IsSafeToCopy = PngChunkTypeFlags.IsSafeToCopy(chunkType);
        return chunk;
    }

    internal static void AddChunk(PngIdentifyState state, PngChunk chunk)
    {
        state.Chunks.Add(chunk);
        state.AllChunks.Add(chunk);
    }

    internal static void AddImageDataPlaceholder(PngIdentifyState state, PngChunkHeader header, long offset)
    {
        PngChunk chunk = new PngChunk();
        chunk.Type = PngConstants.ChunkIDAT;
        chunk.Offset = offset;
        chunk.Length = header.Length;
        chunk.IsKnown = true;
        chunk.IsCritical = true;
        chunk.IsPublic = true;
        chunk.IsReservedBitValid = true;
        chunk.IsSafeToCopy = false;
        state.AllChunks.Add(chunk);
    }

    internal static bool IsTextChunk(string chunkType)
    {
        return chunkType == PngConstants.ChunkText
            || chunkType == PngConstants.ChunkCompressedText
            || chunkType == PngConstants.ChunkInternationalText;
    }

    internal static void AddIdatBytes(PngIdentifyResult result, uint length)
    {
        if (result.IdatBytes > long.MaxValue - length)
        {
            throw new OverflowException();
        }

        result.IdatBytes += length;
    }

    internal static void ReadIhdr(PngIdentifyState state, PngChunk chunk, PngSecurityLimits limits)
    {
        if (chunk.Length != 13)
        {
            AddError(state.Errors, "PNG_IHDR_LENGTH_INVALID", "IHDR must contain 13 bytes.", chunk.Type, chunk.Offset);
            return;
        }

        uint width = PngBinaryReader.ReadUInt32(chunk.Data, 0);
        uint height = PngBinaryReader.ReadUInt32(chunk.Data, 4);
        if (width > int.MaxValue || height > int.MaxValue)
        {
            AddError(state.Errors, "PNG_IHDR_DIMENSION_TOO_LARGE", "IHDR width and height must fit the image info model.", chunk.Type, chunk.Offset);
            return;
        }

        state.Result.Info.Width = (int)width;
        state.Result.Info.Height = (int)height;
        state.Result.Info.BitDepth = (PngBitDepth)chunk.Data[8];
        state.Result.Info.ColorType = (PngColorType)chunk.Data[9];
        state.Result.Info.CompressionMethod = (PngCompressionMethod)chunk.Data[10];
        state.Result.Info.FilterMethod = (PngFilterMethod)chunk.Data[11];
        state.Result.Info.InterlaceMethod = (PngInterlaceMethod)chunk.Data[12];
        state.Result.Info.PixelFormat = GetPixelFormat(state.Result.Info.ColorType, state.Result.Info.BitDepth);
        PngIhdr ihdr = PngIhdrParser.Parse(chunk.Data);
        PngValidationResult validationResult = PngIhdrValidator.Validate(ihdr, limits);
        AddErrors(state.Errors, validationResult.Errors);
        if (validationResult.IsValid)
        {
            PngSecurityLimitEnforcer.EnforceIhdr(ihdr, limits);
        }

        state.SeenIhdr = true;
    }

    internal static void ReadActl(PngIdentifyState state, PngChunk chunk, PngSecurityLimits limits)
    {
        if (chunk.Length != 8)
        {
            AddError(state.Errors, "PNG_ACTL_LENGTH_INVALID", "acTL must contain 8 bytes.", chunk.Type, chunk.Offset);
            return;
        }

        uint frameCount = PngBinaryReader.ReadUInt32(chunk.Data, 0);
        uint playCount = PngBinaryReader.ReadUInt32(chunk.Data, 4);
        if (frameCount == 0)
        {
            AddError(state.Errors, "PNG_ACTL_FRAME_COUNT_INVALID", "acTL frame count must not be 0.", chunk.Type, chunk.Offset);
            return;
        }

        if (frameCount > int.MaxValue || playCount > int.MaxValue)
        {
            AddError(state.Errors, "PNG_ACTL_VALUE_TOO_LARGE", "acTL values must fit the image info model.", chunk.Type, chunk.Offset);
            return;
        }

        PngApngAnimationLimitEnforcer.EnforceFrameCount(frameCount, limits);
        state.Result.Info.IsAnimated = true;
        state.Result.Info.FrameCount = (int)frameCount;
        state.Result.Info.PlayCount = (int)playCount;
    }

    internal static PngPixelFormat GetPixelFormat(PngColorType colorType, PngBitDepth bitDepth)
    {
        switch (colorType)
        {
            case PngColorType.Grayscale:
                return GetGrayscalePixelFormat(bitDepth);
            case PngColorType.Truecolor:
                return GetTruecolorPixelFormat(bitDepth);
            case PngColorType.IndexedColor:
                return GetIndexedPixelFormat(bitDepth);
            case PngColorType.GrayscaleAlpha:
                return GetGrayscaleAlphaPixelFormat(bitDepth);
            case PngColorType.TruecolorAlpha:
                return GetTruecolorAlphaPixelFormat(bitDepth);
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetGrayscalePixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return PngPixelFormat.Grayscale1;
            case PngBitDepth.Two:
                return PngPixelFormat.Grayscale2;
            case PngBitDepth.Four:
                return PngPixelFormat.Grayscale4;
            case PngBitDepth.Eight:
                return PngPixelFormat.Grayscale8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Grayscale16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetTruecolorPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.Rgb8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Rgb16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetIndexedPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.One:
                return PngPixelFormat.Indexed1;
            case PngBitDepth.Two:
                return PngPixelFormat.Indexed2;
            case PngBitDepth.Four:
                return PngPixelFormat.Indexed4;
            case PngBitDepth.Eight:
                return PngPixelFormat.Indexed8;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetGrayscaleAlphaPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.GrayscaleAlpha8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.GrayscaleAlpha16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static PngPixelFormat GetTruecolorAlphaPixelFormat(PngBitDepth bitDepth)
    {
        switch (bitDepth)
        {
            case PngBitDepth.Eight:
                return PngPixelFormat.Rgba8;
            case PngBitDepth.Sixteen:
                return PngPixelFormat.Rgba16;
            default:
                return PngPixelFormat.Unknown;
        }
    }

    internal static void FinishState(PngIdentifyState state, PngDecoderOptions options)
    {
        if (!state.SeenIhdr)
        {
            AddError(state.Errors, "PNG_IHDR_MISSING", "PNG identify did not find IHDR.", "", 0);
        }

        if (!state.Result.FoundIend)
        {
            PngWarning warning = new PngWarning();
            warning.Code = "PNG_IEND_MISSING";
            warning.Message = "PNG identify reached the end of input before IEND.";
            state.Warnings.Add(warning);
        }

        PngChunk[] allChunks = state.AllChunks.ToArray();
        PngChunk[] preservedChunks = state.Chunks.ToArray();
        PngSecurityLimitEnforcer.EnforceDatastream(allChunks, options.Limits);
        PngChunkPlacementProcessor.AnnotatePreservedChunks(allChunks);
        PngValidator.AddCrcFindings(state.Errors, state.Warnings, preservedChunks, options);
        state.Result.Metadata = PngMetadataReader.Read(allChunks, preservedChunks, state.Result.Info.ColorType, options, true);
        state.Result.Warnings = state.Warnings.ToArray();
        state.Result.Errors = state.Errors.ToArray();
        state.Result.IsValid = state.Result.IsPng && state.Errors.Count == 0;
    }

    internal static void AddError(List<PngError> errors, string code, string message, string chunkType, long offset)
    {
        PngError error = new PngError();
        error.Code = code;
        error.Message = message;
        error.ChunkType = chunkType;
        error.Offset = offset;
        error.IsFatal = true;
        errors.Add(error);
    }

    internal static void AddErrors(List<PngError> errors, PngError[] newErrors)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            errors.Add(newErrors[index]);
            index += 1;
        }
    }

    internal static void EnforceChunkCountLimit(int chunkCount, PngSecurityLimits limits)
    {
        if (limits.MaxChunkCount > 0 && chunkCount >= limits.MaxChunkCount)
        {
            throw new OverflowException();
        }
    }

    internal static bool IsAtEnd(Stream stream)
    {
        if (!stream.CanSeek)
        {
            return false;
        }

        return stream.Position >= stream.Length;
    }

    internal static void SkipChunkData(Stream stream, uint length)
    {
        if (stream.CanSeek)
        {
            stream.Seek(length, SeekOrigin.Current);
            return;
        }

        byte[] buffer = new byte[4096];
        uint remaining = length;
        while (remaining > 0)
        {
            int requested = buffer.Length;
            if (remaining < requested)
            {
                requested = (int)remaining;
            }

            int read = stream.Read(buffer, 0, requested);
            if (read == 0)
            {
                throw new EndOfStreamException();
            }

            remaining -= (uint)read;
        }
    }
}

internal class PngChunkHeader
{
    internal uint Length;
    internal string Name = "";
    internal PngChunkType ChunkType = new PngChunkType();
    internal uint Crc;
}

internal class PngIdentifyState
{
    internal PngIdentifyResult Result = new PngIdentifyResult();
    internal List<PngChunk> Chunks = new List<PngChunk>();
    internal List<PngChunk> AllChunks = new List<PngChunk>();
    internal List<PngWarning> Warnings = new List<PngWarning>();
    internal List<PngError> Errors = new List<PngError>();
    internal bool SeenIhdr;
}
