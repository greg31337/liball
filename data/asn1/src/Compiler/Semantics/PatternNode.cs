namespace asn1;

internal class PatternNode
{
    internal PatternNodeKind Kind;
    internal PatternNode[] Children = [];
    internal int Scalar;
    internal int LowerScalar;
    internal int UpperScalar;
    internal int Minimum;
    internal int Maximum;
    internal bool Unbounded;
    internal bool Negated;
}
