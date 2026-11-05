namespace cbor;

/// <summary>
/// Writes back-to-back CBOR values to a stream.
/// </summary>
public class CborStreamWriter : ICborStreamWriter, System.IDisposable
{
    internal System.IO.Stream Output;
    internal CborEncodeOptions Options;
    internal bool Faulted;
    internal bool Disposed;
    internal CborSequenceState Sequence;

    /// <summary>Creates a CBOR stream writer.</summary>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The encoding policies and limits.</param>
    public CborStreamWriter(System.IO.Stream output, CborEncodeOptions options)
    {
        Output = output;
        Options = options;
        Sequence = new CborSequenceState(
            options == null ? -1 : options.MaxSequenceItems,
            options == null ? -1 : options.MaxSequenceBytes
        );
    }

    /// <summary>Writes the next complete CBOR value to the stream.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The write outcome and size information.</returns>
    public CborWriteResult WriteNext(CborValue value)
    {
        CborWriteResult? stateFailure = ValidateWriteState();
        if (stateFailure != null)
        {
            return stateFailure;
        }
        if (!Sequence.CanStartItem())
        {
            return CreateFailure(
                Sequence.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED",
                Sequence.ErrorMessage ?? "The CBOR Sequence exceeds its configured limits.",
                $"$[{Sequence.ItemCount}]",
                0
            );
        }

        CborSizeCalculator calculator = new CborSizeCalculator(Options);
        if (!calculator.Calculate(value))
        {
            return CreateFailure(
                calculator.ErrorCode ?? "CBOR_ENCODE_INVALID_VALUE",
                calculator.ErrorMessage ?? "The CBOR value cannot be encoded.",
                CborEncoder.CreateSequencePath(Sequence.ItemCount, calculator.ErrorPath),
                0
            );
        }

        int required = (int)calculator.RequiredBytes;
        byte[] encoded = new byte[required];
        CborEncodingPlanWriter planWriter = new CborEncodingPlanWriter(Options);
        if (!planWriter.WritePlan(calculator, encoded))
        {
            return CreateFailure(
                planWriter.ErrorCode ?? "CBOR_ENCODE_INTERNAL_FAILURE",
                planWriter.ErrorMessage ?? "The calculated CBOR value could not be written.",
                CborEncoder.CreateSequencePath(Sequence.ItemCount, planWriter.ErrorPath),
                required
            );
        }
        if (!Sequence.AddItem(encoded.Length))
        {
            return CreateFailure(
                Sequence.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_EXCEEDED",
                Sequence.ErrorMessage ?? "The CBOR Sequence exceeds its configured limits.",
                $"$[{Sequence.ItemCount}]",
                required
            );
        }

        Faulted = true;
        Output.Write(encoded, 0, encoded.Length);
        Faulted = false;

        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Done;
        result.Written = encoded.Length;
        result.Required = encoded.Length;
        return result;
    }

    /// <summary>Flushes buffered encoded output to the stream.</summary>
    public void FlushOutput()
    {
        ValidateFlushState();
        Faulted = true;
        Output.Flush();
        Faulted = false;
    }

    /// <summary>Releases the output stream unless the encoding options request that it remain open.</summary>
    public void Dispose()
    {
        if (Disposed)
        {
            return;
        }
        Disposed = true;
        if (Options != null && Options.LeaveOpen)
        {
            if (!Faulted && Output != null)
            {
                Faulted = true;
                Output.Flush();
                Faulted = false;
            }
            return;
        }
        if (Output != null)
        {
            Faulted = true;
            Output.Dispose();
        }
    }

    internal CborWriteResult? ValidateWriteState()
    {
        if (Disposed)
        {
            return CreateFailure(
                "CBOR_STREAM_WRITER_DISPOSED",
                "The stream writer has been disposed.",
                "$",
                0
            );
        }
        if (Faulted)
        {
            return CreateFailure(
                "CBOR_STREAM_WRITER_FAULTED",
                "The stream writer cannot continue after an output failure.",
                "$",
                0
            );
        }
        if (Output == null || Options == null)
        {
            Faulted = true;
            return CreateFailure(
                "CBOR_STREAM_WRITER_CONFIGURATION_INVALID",
                "The stream writer requires a writable stream and encoding options.",
                "$",
                0
            );
        }
        if (!Sequence.ValidateEmpty())
        {
            Faulted = true;
            return CreateFailure(
                Sequence.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_INVALID",
                Sequence.ErrorMessage ?? "The CBOR Sequence limits are invalid.",
                "$",
                0
            );
        }

        Faulted = true;
        if (!Output.CanWrite)
        {
            return CreateFailure(
                "CBOR_STREAM_NOT_WRITABLE",
                "The destination stream does not support writing.",
                "$",
                0
            );
        }
        Faulted = false;
        return null;
    }

    internal void ValidateFlushState()
    {
        if (Disposed)
        {
            throw CreateStateException(
                "CBOR_STREAM_WRITER_DISPOSED",
                "The stream writer has been disposed."
            );
        }
        if (Faulted)
        {
            throw CreateStateException(
                "CBOR_STREAM_WRITER_FAULTED",
                "The stream writer cannot continue after an output failure."
            );
        }
        if (Output == null || Options == null)
        {
            Faulted = true;
            throw CreateStateException(
                "CBOR_STREAM_WRITER_CONFIGURATION_INVALID",
                "The stream writer requires a writable stream and encoding options."
            );
        }
        if (!Sequence.ValidateEmpty())
        {
            Faulted = true;
            throw CreateStateException(
                Sequence.ErrorCode ?? "CBOR_SEQUENCE_LIMIT_INVALID",
                Sequence.ErrorMessage ?? "The CBOR Sequence limits are invalid."
            );
        }

        Faulted = true;
        if (!Output.CanWrite)
        {
            throw CreateStateException(
                "CBOR_STREAM_NOT_WRITABLE",
                "The destination stream does not support writing."
            );
        }
        Faulted = false;
    }

    internal static CborWriteResult CreateFailure(
        string errorCode,
        string errorMessage,
        string? errorPath,
        int required)
    {
        CborWriteResult result = new CborWriteResult();
        result.Status = CborWriteStatus.Error;
        result.Required = required;
        result.ErrorCode = errorCode;
        result.ErrorMessage = errorMessage;
        result.ErrorPath = errorPath;
        return result;
    }

    internal static CborEncodeException CreateStateException(
        string errorCode,
        string errorMessage)
    {
        return new CborEncodeException(errorCode, errorMessage, -1, "$");
    }
}
