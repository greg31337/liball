namespace asn1;

internal class EcnEncodingObjectSetDefinition
{
    internal string Name = "";
    internal string CompletionName = "";
    internal EcnSymbol? Symbol;
    internal EcnSymbol? CompletionSymbol;
    internal EcnPredeclaredEncodingObjectSet? PredeclaredCompletion;
    internal EcnEncodingObjectSetTerm[] Terms = [];
    internal EcnSymbol[] Objects = [];
    internal EcnPredeclaredEncodingObjectSet[] PredeclaredSets = [];
    internal EcnPredeclaredEncodingObject[] PredeclaredObjects = [];
    internal EcnActualParameter[] CompletionActualParameters = [];
    internal AsnToken[] SourceTokens = [];
    internal int ResolutionState;
    internal bool CompletionBuiltIn;
    internal bool Parameterized;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
