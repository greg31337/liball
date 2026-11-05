namespace asn1;

internal enum XmlValueScannerStatus
{
    NeedMoreData = 0,
    Token = 1,
    Trivia = 2,
    NonXmlValue = 3,
    EndOfInput = 4
}
