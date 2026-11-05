namespace asn1;

internal class EcnPlanExecutionRequest
{
    internal EcnExecutablePlan? Plan;
    internal EcnClassCategory Category;
    internal EcnEncodingProperty[] Properties = [];
    internal AsnRuntimeValue? Value;
    internal EcnPlanExecutionBinding[] Bindings = [];
    internal byte[] Data = [];
    internal int BitCount;
    internal bool FinalBlock;
}
