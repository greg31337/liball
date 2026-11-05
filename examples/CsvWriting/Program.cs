using csv;

// These fields require CSV quoting because they contain commas, quotes, or newlines.
string[][] rows =
{
    new[] { "Name", "Note" },
    new[] { "Ada, Lovelace", "Said \"hello\"" },
    new[] { "Grace", "First line\nSecond line" }
};

using var output = new StringWriter();
var writer = new CsvWriter(output, new CsvWriterOptions
{
    LeaveOpen = true,
    LineTerminator = "\n"
});

foreach (var fields in rows)
{
    writer.WriteRow(new CsvRow { Fields = fields });
}

writer.Close();
Console.WriteLine("CSV with quoted and multiline fields:");
Console.WriteLine(output.ToString());

// Read logical records rather than splitting lines, since a field may span lines.
using var input = new StringReader(output.ToString());
var reader = new CsvReader(input, new CsvReaderOptions());
int row = 0;
while (reader.Read())
{
    var fields = reader.GetCurrentRow().Fields;
    if (!fields.SequenceEqual(rows[row]))
    {
        throw new InvalidOperationException("CSV fields changed.");
    }

    Console.WriteLine($"Row {++row}: {string.Join(" | ", fields.Select(value => value?.Replace("\n", "\\n")))}");
}

reader.Close();
if (row != rows.Length)
{
    throw new InvalidOperationException("CSV row count changed.");
}
