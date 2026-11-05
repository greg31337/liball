namespace asn1;

internal class LiteralScannerResult
{
    internal LiteralScannerStatus Status;
    internal AsnToken? Token;
    internal SourceCursorReadResult? Input;
    internal AsnTokenKind[] CandidateKinds = [];

    internal bool CanBeKind(AsnTokenKind kind)
    {
        int index = 0;
        while (index < this.CandidateKinds.Length)
        {
            if (this.CandidateKinds[index] == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
