namespace cbor;

internal class CborTokenFrame
{
    internal CborTokenKind Kind;
    internal ulong RemainingCount;
    internal bool MapExpectsValue;
    internal bool IsIndefinite;
    internal CborRepresentation? StartRepresentation;
    internal ulong CompletedCount;
    internal ulong StringByteCount;
}
