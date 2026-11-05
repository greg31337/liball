namespace asn1;

internal class ParsedObjectClassSyntax
{
    internal AsnObjectClass ObjectClass = new AsnObjectClass();
    internal ParsedObjectClassFieldSyntax[] FieldSyntax = [];
    internal ParsedSyntaxTemplateItem[] SyntaxTemplate = [];
    internal CompiledSyntaxGrammar? SyntaxGrammar;
    internal string ReferenceModule = "";
    internal string ReferenceName = "";
    internal ParsedParameterizedReferenceSyntax? ParameterizedReference;
    internal AsnToken[] SourceTokens = [];
    internal AsnSourceSpan? Span;
}
