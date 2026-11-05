namespace qrcode;

/// <summary>Assembles one compatible Model 2 structured-append sequence from decoded parts.</summary>
/// <remarks>Input order is arbitrary and equivalent duplicate observations are accepted. Invalid caller data and resource-limit violations throw in both methods. Missing parts, mixed identities, parity mismatches, and conflicting payloads are expected sequence failures.</remarks>
public interface IQrStructuredAppendAssembler
{
    /// <summary>Assembles a complete sequence or throws a structured-append exception with sequence diagnostics.</summary>
    /// <param name="parts">Decoded observations of one sequence, including any duplicates.</param>
    /// <param name="maximumResults">The positive maximum number of supplied observations, including duplicates.</param>
    /// <param name="maximumAllocationBytes">The positive allocation budget for grouping and payload assembly.</param>
    /// <returns>The complete payload with ordered bytes, retained per-part control boundaries, and aggregate diagnostics.</returns>
    QrDecodeResult AssembleSequence(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes);

    /// <summary>Attempts sequence assembly without throwing for expected sequence failures.</summary>
    /// <param name="parts">Decoded observations of one sequence, including any duplicates.</param>
    /// <param name="maximumResults">The positive maximum number of supplied observations, including duplicates.</param>
    /// <param name="maximumAllocationBytes">The positive allocation budget for grouping and payload assembly.</param>
    /// <returns>The complete result or failure diagnostics without a partial payload.</returns>
    QrStructuredAppendAttemptResult TryAssembleSequence(QrDecodeResult[] parts, int maximumResults, int maximumAllocationBytes);
}
