namespace asn1;

internal class RuntimeConstraintEvaluationResult
{
    internal NormalizedConstraintNode? ViolatedNode;
    internal string ValuePath = "$";
    internal string Message = "";
    internal bool Success;
    internal bool RequiresApplicationValidation;
}
