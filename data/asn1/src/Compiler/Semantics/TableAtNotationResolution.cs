namespace asn1;

internal class TableAtNotationResolution
{
    internal ParsedAtNotationSyntax? SourceSyntax;
    internal ResolvedTypeSyntax? StartType;
    internal ResolvedComponentSyntax[] Components = [];
    internal ResolvedComponentSyntax[] LookupComponents = [];
    internal ResolvedTypeSyntax? ReferencedType;
    internal ResolvedObjectClassFieldSyntax? ReferencedField;
}
