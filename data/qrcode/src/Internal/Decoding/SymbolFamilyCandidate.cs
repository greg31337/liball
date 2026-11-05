namespace qrcode;

internal class SymbolFamilyCandidate
{
    internal QrSymbolKind SymbolKind;

    // Numbered version for square families; zero-based size index for rMQR.
    internal int Version;

    internal int Width;

    internal int Height;

    internal int FixedModuleCount;

    internal int ObservedModuleCount;

    internal int MismatchedModuleCount;

    internal int DistinctiveModuleCount;

    internal int ObservedDistinctiveModuleCount;

    internal int MismatchedDistinctiveModuleCount;

    internal bool HasSufficientEvidence;

    internal bool Rejected;
}
