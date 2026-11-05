using whois;

// Protocol messages use CRLF on the wire, regardless of the file's line endings.
string input = File.ReadAllText(SampleFiles.InputPath("protocols/domain-record.txt")).ReplaceLineEndings("\r\n");
var parser = new WhoisResponseParser();
var parsed = parser.ParseResponse(new WhoisTextResponse
{
    Text = input,
    IsDecoded = true,
    EncodingName = "utf-8"
}, new WhoisLimits());
if (!parsed.IsParsed)
{
    throw new InvalidOperationException("WHOIS parsing failed.", parsed.Error);
}

Console.WriteLine("Synthetic WHOIS record (no network lookup)");
foreach (string name in new[] { "Domain Name", "Registrar", "Creation Date", "Name Server" })
{
    foreach (var field in parser.FindFields(parsed, name))
    {
        Console.WriteLine($"{field.OriginalName,-15} {field.Value}");
    }
}

Console.WriteLine($"Parsed fields: {parsed.Fields.Length}");
