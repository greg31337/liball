namespace xml;

internal enum XmlTokenKind
{
    Text = 0,
    Whitespace = 1,
    Name = 2,
    Equals = 3,
    QuotedValue = 4,
    CharacterReference = 5,
    EntityReference = 6,
    StartTagOpen = 7,
    EndTagOpen = 8,
    TagClose = 9,
    EmptyElementClose = 10,
    MarkupDeclarationOpen = 11,
    MarkupSymbol = 12,
    Comment = 13,
    CData = 14,
    ProcessingInstruction = 15,
    XmlDeclaration = 16,
    TextDeclaration = 17,
    EndOfInput = 18
}

internal enum XmlScannerState
{
    Content = 0,
    StartTag = 1,
    EndTag = 2,
    MarkupDeclaration = 3,
    EndOfInput = 4
}
