namespace asn1;

internal class CompilerSourceParseResult
{
    internal AsnLexerResult LexerResult = new AsnLexerResult();
    internal AsnParserResult ParserResult = new AsnParserResult();
    internal ModuleSyntax[] Modules = [];
}
