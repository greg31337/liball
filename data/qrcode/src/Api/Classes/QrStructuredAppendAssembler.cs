namespace qrcode;

/// <summary>Provides reusable stateless structured-append assembly with exception and attempt-result paths.</summary>
public class QrStructuredAppendAssembler : IQrStructuredAppendAssembler
{
    /// <summary>Initializes a stateless structured-append assembler.</summary>
    public QrStructuredAppendAssembler()
    {
    }

    /// <summary>Assembles one complete sequence and preserves its bytes, text boundaries, and diagnostics.</summary>
    /// <param name="parts">The decoded sequence observations in any order.</param>
    /// <param name="maximumResults">The positive limit on input observations, including duplicates.</param>
    /// <param name="maximumAllocationBytes">The positive grouping and assembly allocation limit.</param>
    /// <returns>The complete assembled result with independent payload buffers.</returns>
    /// <exception cref="QrStructuredAppendException">The parts are missing, incompatible, or fail sequence parity verification.</exception>
    public QrDecodeResult AssembleSequence(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        QrStructuredAppendAttemptResult attempt = TryAssembleSequence(parts, maximumResults, maximumAllocationBytes);
        if (attempt.Result == null)
        {
            throw new QrStructuredAppendException("The supplied parts could not be assembled into one complete sequence.",
                attempt.FailureReason, attempt.ExpectedPartCount, attempt.ActualPartCount, attempt.Parity);
        }
        return attempt.Result;
    }

    /// <summary>Returns expected sequence failures without throwing or exposing a partial payload.</summary>
    /// <param name="parts">The decoded sequence observations in any order.</param>
    /// <param name="maximumResults">The positive limit on input observations, including duplicates.</param>
    /// <param name="maximumAllocationBytes">The positive grouping and assembly allocation limit.</param>
    /// <returns>The complete assembled result or sequence diagnostics. Invalid input and resource limits still throw.</returns>
    public QrStructuredAppendAttemptResult TryAssembleSequence(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes)
    {
        StructuredAppendAssemblyResult assembled = StructuredAppendAssembly.Assemble(parts, maximumResults, maximumAllocationBytes);
        return new QrStructuredAppendAttemptResult
        {
            Success = assembled.Success, FailureReason = assembled.FailureReason, Result = assembled.Result,
            ExpectedPartCount = assembled.ExpectedPartCount, ActualPartCount = assembled.ActualPartCount,
            Parity = assembled.Parity, MissingIndices = assembled.MissingIndices,
            ConflictingIndices = assembled.ConflictingIndices, DuplicateCount = assembled.DuplicateCount
        };
    }
}
