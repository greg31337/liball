namespace asn1;

internal enum XerXmlTokenKind
{
    StartElement = 0,
    EndElement = 1,
    EmptyElement = 2,
    Text = 3,
    CData = 4,
    Comment = 5,
    ProcessingInstruction = 6,
    XmlDeclaration = 7
}
