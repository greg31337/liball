namespace sdp;

/// <summary>
/// Receives result objects from SDP parsing, writing, and validation operations.
/// </summary>
public interface ISdpResultVisitor
{
    /// <summary>Visits a parse result.</summary>
    /// <param name="result">The parse result to visit.</param>
    void VisitParseResult(SdpParseResult result);

    /// <summary>Visits a write result.</summary>
    /// <param name="result">The write result to visit.</param>
    void VisitWriteResult(SdpWriteResult result);

    /// <summary>Visits a validation result.</summary>
    /// <param name="result">The validation result to visit.</param>
    void VisitValidationResult(SdpValidationResult result);

    /// <summary>Visits a comparison result.</summary>
    /// <param name="result">The comparison result to visit.</param>
    void VisitComparisonResult(SdpComparisonResult result);

    /// <summary>Visits a caller-buffer write result.</summary>
    /// <param name="result">The caller-buffer write result to visit.</param>
    void VisitBufferWriteResult(SdpBufferWriteResult result);

    /// <summary>Visits an offer-and-answer negotiation result.</summary>
    /// <param name="result">The negotiation result to visit.</param>
    void VisitNegotiationResult(SdpNegotiationResult result);
}
