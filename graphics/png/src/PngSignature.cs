using System.IO;

namespace png;

internal class PngSignature
{
    internal static bool HasSignatureSpan(ReadOnlySpan<byte> data)
    {
        if (data.Length < PngConstants.SignatureLength)
        {
            return false;
        }

        return data[0] == PngConstants.SignatureByte0
            && data[1] == PngConstants.SignatureByte1
            && data[2] == PngConstants.SignatureByte2
            && data[3] == PngConstants.SignatureByte3
            && data[4] == PngConstants.SignatureByte4
            && data[5] == PngConstants.SignatureByte5
            && data[6] == PngConstants.SignatureByte6
            && data[7] == PngConstants.SignatureByte7;
    }

    internal static PngValidationResult ValidateSignatureSpan(ReadOnlySpan<byte> data, PngErrorPolicy errorPolicy)
    {
        bool isFatal = IsStrictValidation(errorPolicy);
        if (data.Length < PngConstants.SignatureLength)
        {
            return CreateInvalidResult("PNG_SIGNATURE_TOO_SHORT", "PNG signature is shorter than 8 bytes.", 0, isFatal);
        }

        if (!HasSignatureSpan(data))
        {
            return CreateInvalidResult("PNG_SIGNATURE_MISMATCH", "PNG signature bytes do not match the PNG signature.", 0, isFatal);
        }

        return CreateValidResult();
    }

    internal static bool HasSignature(byte[] data)
    {
        return HasSignature(data, 0);
    }

    internal static bool HasSignature(byte[] data, int offset)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        if (offset > data.Length - PngConstants.SignatureLength)
        {
            return false;
        }

        return data[offset] == PngConstants.SignatureByte0
            && data[offset + 1] == PngConstants.SignatureByte1
            && data[offset + 2] == PngConstants.SignatureByte2
            && data[offset + 3] == PngConstants.SignatureByte3
            && data[offset + 4] == PngConstants.SignatureByte4
            && data[offset + 5] == PngConstants.SignatureByte5
            && data[offset + 6] == PngConstants.SignatureByte6
            && data[offset + 7] == PngConstants.SignatureByte7;
    }

    internal static bool HasSignature(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        long originalPosition = 0;
        bool restorePosition = stream.CanSeek;
        if (restorePosition)
        {
            originalPosition = stream.Position;
        }

        byte[] data = new byte[PngConstants.SignatureLength];
        int totalRead = 0;
        while (totalRead < PngConstants.SignatureLength)
        {
            int read = stream.Read(data, totalRead, PngConstants.SignatureLength - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (restorePosition)
        {
            stream.Position = originalPosition;
        }

        if (totalRead != PngConstants.SignatureLength)
        {
            return false;
        }

        return HasSignature(data, 0);
    }

    internal static PngValidationResult ValidateSignature(byte[] data, PngErrorPolicy errorPolicy)
    {
        return ValidateSignature(data, 0, errorPolicy);
    }

    internal static PngValidationResult ValidateSignature(byte[] data, int offset, PngErrorPolicy errorPolicy)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException("offset");
        }

        bool isFatal = IsStrictValidation(errorPolicy);
        if (offset > data.Length - PngConstants.SignatureLength)
        {
            return CreateInvalidResult("PNG_SIGNATURE_TOO_SHORT", "PNG signature is shorter than 8 bytes.", offset, isFatal);
        }

        if (!HasSignature(data, offset))
        {
            return CreateInvalidResult("PNG_SIGNATURE_MISMATCH", "PNG signature bytes do not match the PNG signature.", offset, isFatal);
        }

        return CreateValidResult();
    }

    internal static PngValidationResult ValidateSignature(Stream stream, PngErrorPolicy errorPolicy)
    {
        ArgumentNullException.ThrowIfNull(stream);

        bool isFatal = IsStrictValidation(errorPolicy);
        long originalPosition = 0;
        bool restorePosition = stream.CanSeek;
        if (restorePosition)
        {
            originalPosition = stream.Position;
        }

        byte[] data = new byte[PngConstants.SignatureLength];
        int totalRead = 0;
        while (totalRead < PngConstants.SignatureLength)
        {
            int read = stream.Read(data, totalRead, PngConstants.SignatureLength - totalRead);
            if (read == 0)
            {
                break;
            }

            totalRead += read;
        }

        if (restorePosition)
        {
            stream.Position = originalPosition;
        }

        if (totalRead != PngConstants.SignatureLength)
        {
            return CreateInvalidResult("PNG_SIGNATURE_TOO_SHORT", "PNG signature is shorter than 8 bytes.", originalPosition, isFatal);
        }

        if (!HasSignature(data, 0))
        {
            return CreateInvalidResult("PNG_SIGNATURE_MISMATCH", "PNG signature bytes do not match the PNG signature.", originalPosition, isFatal);
        }

        return CreateValidResult();
    }

    internal static bool IsStrictValidation(PngErrorPolicy errorPolicy)
    {
        switch (errorPolicy)
        {
            case PngErrorPolicy.Strict:
                return true;
            case PngErrorPolicy.TolerantMetadata:
                return false;
            case PngErrorPolicy.TolerantRepair:
                return false;
            case PngErrorPolicy.BestEffortInspection:
                return false;
            default:
                throw new ArgumentOutOfRangeException("errorPolicy");
        }
    }

    internal static PngValidationResult CreateValidResult()
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = true;
        return result;
    }

    internal static PngValidationResult CreateInvalidResult(string code, string message, long offset, bool isFatal)
    {
        PngValidationResult result = new PngValidationResult();
        result.IsValid = false;

        if (isFatal)
        {
            PngError error = new PngError();
            error.Code = code;
            error.Message = message;
            error.Offset = offset;
            error.IsFatal = true;
            result.Errors = new PngError[] { error };
        }
        else
        {
            PngWarning warning = new PngWarning();
            warning.Code = code;
            warning.Message = message;
            warning.Offset = offset;
            result.Warnings = new PngWarning[] { warning };
        }

        return result;
    }
}
