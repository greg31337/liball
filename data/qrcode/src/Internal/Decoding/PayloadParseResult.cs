namespace qrcode;

internal class PayloadParseResult
{
    internal QrSymbolKind SymbolKind = QrSymbolKind.Model2;

    internal QrSegment[] Segments = new QrSegment[0];

    internal bool HasTerminator;

    // Number of bits occupied by segments, excluding terminator and padding.
    internal int PayloadBitCount;

    internal int ActiveEciAssignment = -1;
}
