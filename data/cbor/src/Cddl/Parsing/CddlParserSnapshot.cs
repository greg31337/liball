namespace cbor;

internal class CddlParserSnapshot
{
    internal CddlToken Current = new CddlToken();
    internal int LexerOffset;
    internal int LexerLine;
    internal int LexerColumn;
    internal int LexerTokenCount;
    internal int LexerDiagnosticCount;
    internal int LexerDiagnosticIndex;
    internal int DocumentDiagnosticCount;
    internal int TriviaCount;
    internal int TriviaSpanCount;
    internal int Depth;
    internal int ErrorCount;
    internal int NodeCount;
    internal bool HasError;
    internal bool RecoveryLimitReached;
    internal bool NodeLimitReached;
}
