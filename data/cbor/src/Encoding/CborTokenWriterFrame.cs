namespace cbor;

internal class CborTokenWriterFrame
{
    internal CborTokenKind Kind;
    internal ulong RemainingCount;
    internal bool MapExpectsValue;
    internal bool IsIndefinite;
}
