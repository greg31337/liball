namespace asn1;

internal class ObjectSetEvaluationContext
{
    internal string StablePrefix = "";
    internal string[] FormalNames = [];
    internal ParsedActualParameterSyntax[] ActualParameters = [];
    internal System.Collections.Generic.List<string> ParameterizedReferences =
        new System.Collections.Generic.List<string>();
}
