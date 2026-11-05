using System;

namespace cbor;

internal class CborEncodingPlanWriter
{
    internal CborEncodeOptions Options;
    internal string? ErrorCode;
    internal string? ErrorMessage;
    internal string? ErrorPath;
    internal int Written;

    internal CborEncodingPlanWriter(CborEncodeOptions options)
    {
        Options = options;
    }

    internal bool WritePlan(CborSizeCalculator calculator, byte[] destination)
    {
        CborTokenWriterCore writer = new CborTokenWriterCore(Options);
        int offset = 0;
        int index = 0;
        while (index < calculator.Tokens.Count)
        {
            CborWriteResult result = writer.WriteTokenToSpan(
                calculator.Tokens[index],
                destination.AsSpan(offset)
            );
            if (result.Status != CborWriteStatus.Done)
            {
                ErrorCode = result.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE";
                ErrorMessage = result.ErrorMessage
                    ?? "The calculated CBOR value could not be written.";
                ErrorPath = result.ErrorPath;
                Written = offset;
                return false;
            }
            offset = offset + result.Written;
            index = index + 1;
        }
        CborWriteResult completion = writer.CompleteWriter(Span<byte>.Empty);
        if (completion.Status != CborWriteStatus.Done)
        {
            ErrorCode = completion.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE";
            ErrorMessage = completion.ErrorMessage
                ?? "The calculated CBOR value was incomplete.";
            ErrorPath = completion.ErrorPath;
            Written = offset;
            return false;
        }
        Written = offset;
        return true;
    }
}
