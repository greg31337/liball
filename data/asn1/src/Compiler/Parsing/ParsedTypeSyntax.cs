namespace asn1;

internal class ParsedTypeSyntax
{
    internal AsnType Type = new AsnType();
    internal AsnNamedValue[] RootNamedValues = [];
    internal AsnNamedValue[] ExtensionNamedValues = [];
    internal AsnSourceSpan?[] RootNamedValueSpans = [];
    internal AsnSourceSpan?[] ExtensionNamedValueSpans = [];
    internal AsnToken[] ExtensionExceptionTokens = [];
    internal ParsedComponentSyntax[] ComponentSyntax = [];
    internal AsnEncodingInstruction[] EncodingInstructions = [];
    internal ParsedEncodingInstructionSyntax[] EncodingInstructionSyntax = [];
    internal AsnToken[] EncodingInstructionTokens = [];
    internal ParsedConstraintSyntax[] ConstraintSyntax = [];
    internal ParsedRecoverySyntax[] Recoveries = [];
    internal ParsedTypeSyntax? InnerTypeSyntax;
    internal ParsedInformationFromObjectsSyntax? InformationFromObjects;
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal string SourceTypeName = "";
    internal string ElementName = "";
    internal string EncodingReference = "";
    internal string SelectionIdentifier = "";
    internal string TagNumberReference = "";
    internal string DefinedByReference = "";
    internal bool HasExtensionMarker;
    internal bool HasExtensionEndMarker;
    internal bool HasNumericTagNumber;
    internal bool LegacyAnySyntax;
    internal bool HasDefinedByReference;
    internal ResolvedTypeSyntax? Resolution;
    internal AsnSourceSpan? Span;
}
