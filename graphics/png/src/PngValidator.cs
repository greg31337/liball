using System.Collections.Generic;
using System.IO;

namespace png;

internal class PngValidator
{
    internal static PngValidationResult ValidateStrict(byte[] data)
    {
        return ValidateStrict(data, 0);
    }

    internal static PngValidationResult ValidateStrict(byte[] data, int offset)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.Strict;
        options.ValidateCrc = true;
        return Validate(data, offset, options);
    }

    internal static PngValidationResult ValidateTolerantMetadata(byte[] data)
    {
        return ValidateTolerantMetadata(data, 0);
    }

    internal static PngValidationResult ValidateTolerantMetadata(byte[] data, int offset)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.TolerantMetadata;
        options.ValidateCrc = true;
        return Validate(data, offset, options);
    }

    internal static PngValidationResult InspectBestEffort(byte[] data)
    {
        return InspectBestEffort(data, 0);
    }

    internal static PngValidationResult InspectBestEffort(byte[] data, int offset)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.BestEffortInspection;
        options.ValidateCrc = true;
        return Validate(data, offset, options);
    }

    internal static PngValidationResult ValidateTolerantRepair(byte[] data)
    {
        return ValidateTolerantRepair(data, 0);
    }

    internal static PngValidationResult ValidateTolerantRepair(byte[] data, int offset)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.TolerantRepair;
        options.ValidateCrc = true;
        return Validate(data, offset, options);
    }

    internal static PngValidationResult Validate(byte[] data)
    {
        return Validate(data, 0, new PngDecoderOptions());
    }

    internal static PngValidationResult Validate(byte[] data, int offset)
    {
        return Validate(data, offset, new PngDecoderOptions());
    }

    internal static PngValidationResult Validate(byte[] data, int offset, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        PngValidationResult signature = PngSignature.ValidateSignature(data, offset, options.ErrorPolicy);
        if (!signature.IsValid)
        {
            return signature;
        }

        PngValidationDatastreamReadResult readResult = PngValidationDatastreamReader.Read(data, offset + PngConstants.SignatureLength, options.Limits);
        PngChunkListResult chunkList = readResult.ChunkList;
        PngChunk[] validationChunks = CreateValidationChunks(chunkList.Chunks, options);
        PngSecurityLimitEnforcer.EnforceDatastream(validationChunks, options.Limits);
        List<PngError> errors = new List<PngError>();
        List<PngWarning> warnings = new List<PngWarning>();
        AddPolicyErrors(errors, warnings, readResult.Errors, options);
        AddPolicyErrors(errors, warnings, PngChunkOrderValidator.Validate(validationChunks).Errors, options);
        AddPolicyErrors(errors, warnings, PngChunkDuplicateValidator.Validate(validationChunks).Errors, options);
        AddPolicyErrors(errors, warnings, PngApngSequenceValidator.Validate(validationChunks).Errors, options);
        PngValidationResult semanticResult = PngDatastreamSemanticValidator.Validate(validationChunks, chunkList.TrailingBytes, options);
        AddPolicyErrors(errors, warnings, semanticResult.Errors, options);
        AddCrcFindings(errors, warnings, validationChunks, options);
        AddWarnings(warnings, chunkList.Warnings);
        PngValidationResult result = new PngValidationResult();
        result.Info = semanticResult.Info;
        result.Errors = errors.ToArray();
        result.Warnings = warnings.ToArray();
        result.IsValid = result.Errors.Length == 0;
        return result;
    }

    internal static PngValidationResult ValidateTolerantMetadata(Stream stream)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.TolerantMetadata;
        options.ValidateCrc = true;
        return Validate(stream, options);
    }

    internal static PngValidationResult InspectBestEffort(Stream stream)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.BestEffortInspection;
        options.ValidateCrc = true;
        return Validate(stream, options);
    }

    internal static PngValidationResult ValidateTolerantRepair(Stream stream)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.TolerantRepair;
        options.ValidateCrc = true;
        return Validate(stream, options);
    }

    internal static PngValidationResult ValidateStrict(Stream stream)
    {
        PngDecoderOptions options = new PngDecoderOptions();
        options.ErrorPolicy = PngErrorPolicy.Strict;
        options.ValidateCrc = true;
        return Validate(stream, options);
    }

    internal static PngValidationResult Validate(Stream stream)
    {
        return Validate(stream, new PngDecoderOptions());
    }

    internal static PngValidationResult Validate(Stream stream, PngDecoderOptions options)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Limits);

        byte[] data = ReadAllBytes(stream);
        return Validate(data, 0, options);
    }

    internal static PngValidationResult CreateBaseResult(PngIdentifyResult identify)
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = identify.IsValid;
        result.Info = identify.Info;
        result.Warnings = identify.Warnings;
        result.Errors = identify.Errors;
        return result;
    }

    internal static PngChunk[] CreateValidationChunks(PngChunk[] chunks, PngDecoderOptions options)
    {
        if (options.ErrorPolicy == PngErrorPolicy.TolerantRepair)
        {
            return PngRepairer.RepairAncillaryOrdering(chunks);
        }

        return chunks;
    }

    internal static void AddPolicyErrors(List<PngError> errors, List<PngWarning> warnings, PngError[] newErrors, PngDecoderOptions options)
    {
        int index = 0;
        while (index < newErrors.Length)
        {
            PngError error = newErrors[index];
            if (options.ErrorPolicy == PngErrorPolicy.BestEffortInspection)
            {
                warnings.Add(CreateWarning(error));
            }
            else if (options.ErrorPolicy == PngErrorPolicy.TolerantMetadata && IsToleratedMetadataError(error))
            {
                warnings.Add(CreateWarning(error));
            }
            else
            {
                errors.Add(error);
            }

            index += 1;
        }
    }

    internal static bool IsToleratedMetadataError(PngError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        ArgumentNullException.ThrowIfNull(error.Code);
        ArgumentNullException.ThrowIfNull(error.ChunkType);

        if (!IsToleratedMetadataChunk(error.ChunkType))
        {
            return false;
        }

        return error.Code != "PNG_CHUNK_TYPE_RESERVED_BIT";
    }

    internal static bool IsToleratedMetadataChunk(string chunkType)
    {
        if (chunkType.Length != 4)
        {
            return false;
        }

        if (!PngChunkTypeValidator.IsValid(chunkType))
        {
            return false;
        }

        if (!PngChunkTypeFlags.IsAncillary(chunkType))
        {
            return false;
        }

        return chunkType != PngConstants.ChunkAnimationControl
            && chunkType != PngConstants.ChunkFrameControl
            && chunkType != PngConstants.ChunkFrameData;
    }

    internal static PngWarning CreateWarning(PngError error)
    {
        PngWarning warning = new PngWarning();
        warning.Code = error.Code;
        warning.Message = error.Message;
        warning.ChunkType = error.ChunkType;
        warning.Offset = error.Offset;
        return warning;
    }

    internal static void AddCrcFindings(List<PngError> errors, List<PngWarning> warnings, PngChunk[] chunks, PngDecoderOptions options)
    {
        if (options.ValidateCrc && IsCrcValidationPolicy(options.ErrorPolicy))
        {
            AddCrcFindingsForChunks(errors, warnings, chunks, options);
        }
    }

    internal static bool IsCrcValidationPolicy(PngErrorPolicy errorPolicy)
    {
        return errorPolicy == PngErrorPolicy.Strict
            || errorPolicy == PngErrorPolicy.TolerantMetadata
            || errorPolicy == PngErrorPolicy.TolerantRepair
            || errorPolicy == PngErrorPolicy.BestEffortInspection;
    }

    internal static void AddCrcFindingsForChunks(List<PngError> errors, List<PngWarning> warnings, PngChunk[] chunks, PngDecoderOptions options)
    {
        int index = 0;
        while (index < chunks.Length)
        {
            PngChunk chunk = chunks[index];
            ArgumentNullException.ThrowIfNull(chunk);
            ArgumentNullException.ThrowIfNull(chunk.Type);
            ArgumentNullException.ThrowIfNull(chunk.Data);
            uint expectedCrc = PngCrc32.CalculateChunkCrc(chunk.Type, chunk.Data);
            if (chunk.Crc != expectedCrc)
            {
                AddCrcFinding(errors, warnings, chunk, options);
            }

            index += 1;
        }
    }

    internal static void AddCrcFinding(List<PngError> errors, List<PngWarning> warnings, PngChunk chunk, PngDecoderOptions options)
    {
        PngError error = new PngError();
        error.Code = "PNG_CHUNK_CRC_MISMATCH";
        error.Message = "PNG chunk CRC does not match the chunk type and data.";
        error.ChunkType = chunk.Type;
        error.Offset = chunk.Offset + 8 + chunk.Length;
        error.IsFatal = true;
        if (ContainsCrcFinding(errors, warnings, error))
        {
            return;
        }

        if (options.ErrorPolicy == PngErrorPolicy.BestEffortInspection)
        {
            warnings.Add(CreateWarning(error));
        }
        else if (options.ErrorPolicy == PngErrorPolicy.TolerantMetadata && IsToleratedMetadataError(error))
        {
            warnings.Add(CreateWarning(error));
        }
        else
        {
            errors.Add(error);
        }
    }

    internal static bool ContainsCrcFinding(List<PngError> errors, List<PngWarning> warnings, PngError finding)
    {
        int errorIndex = 0;
        while (errorIndex < errors.Count)
        {
            PngError error = errors[errorIndex];
            if (error.Code == finding.Code && error.ChunkType == finding.ChunkType && error.Offset == finding.Offset)
            {
                return true;
            }

            errorIndex += 1;
        }

        int warningIndex = 0;
        while (warningIndex < warnings.Count)
        {
            PngWarning warning = warnings[warningIndex];
            if (warning.Code == finding.Code && warning.ChunkType == finding.ChunkType && warning.Offset == finding.Offset)
            {
                return true;
            }

            warningIndex += 1;
        }

        return false;
    }

    internal static byte[] ReadAllBytes(Stream stream)
    {
        MemoryStream output = new MemoryStream();
        byte[] buffer = new byte[4096];
        while (true)
        {
            int read = stream.Read(buffer, 0, buffer.Length);
            if (read == 0)
            {
                break;
            }

            output.Write(buffer, 0, read);
        }

        byte[] data = output.ToArray();
        output.Dispose();
        return data;
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

    internal static void AddWarnings(List<PngWarning> warnings, PngWarning[] newWarnings)
    {
        int index = 0;
        while (index < newWarnings.Length)
        {
            warnings.Add(newWarnings[index]);
            index += 1;
        }
    }
}
