namespace qrcode;

internal class SymbolFamilyResult
{
    internal SymbolFamilyStatus Status;

    // All allowed dimension hypotheses, including rejected ones, in stable family order.
    // Identified means one supported hypothesis and no other viable hypothesis.
    // Ambiguous means multiple viable hypotheses with at least one supported.
    // InsufficientEvidence means viable hypotheses exist but none has enough observations.
    internal SymbolFamilyCandidate[] Candidates = new SymbolFamilyCandidate[0];
}
