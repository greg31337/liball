namespace csv;

internal enum CsvParserState
{
    StartField = 0,
    InField = 1,
    InQuotedField = 2,
    AfterQuote = 3,
    AfterDelimiter = 4,
    EndRecord = 5
}
