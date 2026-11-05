namespace asn1;

internal class EcnPlanBounds
{
    internal long MinimumBits;
    internal long MaximumBits;
    internal long MinimumBytes;
    internal long MaximumBytes;
    internal string LowerValue = "";
    internal string UpperValue = "";
    internal bool HasMinimumBits;
    internal bool HasMaximumBits;
    internal bool HasMinimumBytes;
    internal bool HasMaximumBytes;
    internal bool HasLowerValue;
    internal bool HasUpperValue;
    internal bool Exact;
}
