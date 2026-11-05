namespace asn1;

internal class EcnFieldPath
{
    internal EcnFieldPathRoot Root;
    internal string Text = "";
    internal string NormalizedText = "";
    internal string[] Segments = [];
    internal EcnFieldPathSegment[] ResolvedSegments = [];
    internal EcnSymbol? ParameterSymbol;
    internal int TopLevelFieldOrder = -1;
    internal int ParentDepth;
    internal bool Deferred;
    internal bool Valid;
    internal AsnSourceSpan? Span;
}
