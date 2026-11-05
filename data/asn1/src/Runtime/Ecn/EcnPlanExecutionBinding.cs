namespace asn1;

internal class EcnPlanExecutionBinding
{
    internal string Path = "";
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties = [];
    internal EcnExecutablePlan? Plan;
    internal EcnPlanExecutionBinding[] NestedBindings = [];
    internal AsnRuntimeValue? Value;
    internal EcnBitField? EncodedValue;
    internal int BitCount = -1;
    internal int RepetitionCount = -1;
    internal bool Present = true;
    internal bool Selected;
}
