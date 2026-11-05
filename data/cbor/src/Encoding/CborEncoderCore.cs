using System;

namespace cbor;

internal class CborEncoderCore
{
    internal CborEncodeOptions Options;

    internal CborEncoderCore(CborEncodeOptions options)
    {
        Options = options;
    }

    internal byte[] EncodeValue(CborValue value)
    {
        CborSizeCalculator calculator = new CborSizeCalculator(Options);
        if (!calculator.Calculate(value))
        {
            throw CreateException(calculator);
        }
        byte[] destination = new byte[(int)calculator.RequiredBytes];
        WriteCalculated(calculator, destination);
        return destination;
    }

    internal CborWriteResult EncodeValueInto(CborValue value, byte[]? destination)
    {
        CborSizeCalculator calculator = new CborSizeCalculator(Options);
        if (!calculator.Calculate(value))
        {
            return CreateErrorResult(calculator);
        }
        if (destination == null)
        {
            CborWriteResult missing = new CborWriteResult();
            missing.Status = CborWriteStatus.Error;
            missing.Required = (int)calculator.RequiredBytes;
            missing.ErrorCode = "CBOR_DESTINATION_MISSING";
            missing.ErrorMessage = "A destination byte array is required.";
            missing.ErrorPath = "$";
            return missing;
        }
        if (destination.Length < calculator.RequiredBytes)
        {
            CborWriteResult tooSmall = new CborWriteResult();
            tooSmall.Status = CborWriteStatus.DestinationTooSmall;
            tooSmall.Required = (int)calculator.RequiredBytes;
            tooSmall.ErrorCode = "CBOR_DESTINATION_TOO_SMALL";
            tooSmall.ErrorMessage = "The destination does not have enough space for the encoded value.";
            tooSmall.ErrorPath = "$";
            return tooSmall;
        }
        WriteCalculated(calculator, destination);
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Done;
        result.Written = (int)calculator.RequiredBytes;
        result.Required = (int)calculator.RequiredBytes;
        return result;
    }

    internal int MeasureValue(CborValue value)
    {
        CborSizeCalculator calculator = new CborSizeCalculator(Options);
        if (!calculator.Calculate(value))
        {
            throw CreateException(calculator);
        }
        return (int)calculator.RequiredBytes;
    }

    internal void WriteCalculated(CborSizeCalculator calculator, byte[] destination)
    {
        CborEncodingPlanWriter writer = new CborEncodingPlanWriter(Options);
        if (!writer.WritePlan(calculator, destination))
        {
            throw new CborEncodeException(
                writer.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE",
                writer.ErrorMessage ?? "The calculated CBOR value could not be written.",
                writer.Written,
                writer.ErrorPath
            );
        }
    }

    internal CborEncodeException CreateException(CborSizeCalculator calculator)
    {
        return new CborEncodeException(
            calculator.ErrorCode ?? "CBOR_ENCODE_INVALID_VALUE",
            calculator.ErrorMessage ?? "The CBOR value cannot be encoded.",
            -1,
            calculator.ErrorPath
        );
    }

    internal CborWriteResult CreateErrorResult(CborSizeCalculator calculator)
    {
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Error;
        result.ErrorCode = calculator.ErrorCode ?? "CBOR_ENCODE_INVALID_VALUE";
        result.ErrorMessage = calculator.ErrorMessage ?? "The CBOR value cannot be encoded.";
        result.ErrorPath = calculator.ErrorPath;
        return result;
    }
}
