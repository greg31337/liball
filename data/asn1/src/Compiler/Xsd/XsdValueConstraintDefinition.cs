namespace asn1;

internal class XsdValueConstraintDefinition
{
    internal string StableId = "";
    internal string ComponentId = "";
    internal string ComponentKind = "";
    internal string ComponentName = "";
    internal string Kind = "";
    internal string LexicalValue = "";
    internal string CanonicalValue = "";
    internal string TypeName = "";
    internal string TypeNamespace = "";
    internal string BuiltInTypeName = "";
    internal AsnValue? Value;
    internal AsnConstraint? FixedConstraint;
    internal AsnEncodingInstruction? DefaultForEmptyInstruction;
    internal AsnEncodingInstruction[] TypeInstructions = [];
    internal bool IgnoredForQName;
    internal AsnSourceSpan? Span;
}
