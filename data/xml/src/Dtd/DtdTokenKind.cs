namespace xml;

internal enum DtdTokenKind
{
    Whitespace = 0,
    MarkupDeclarationOpen = 1,
    DeclarationClose = 2,
    ConditionalSectionOpen = 3,
    ConditionalSectionClose = 4,
    Name = 5,
    Nmtoken = 6,
    ParameterEntityReference = 7,
    QuotedLiteral = 8,
    PublicIdentifierLiteral = 9,
    SystemLiteral = 10,
    Comment = 11,
    ProcessingInstruction = 12,
    Symbol = 13,
    EndOfInput = 14
}

internal enum DtdLiteralExpectation
{
    None = 0,
    PublicIdentifier = 1,
    SystemIdentifier = 2
}
