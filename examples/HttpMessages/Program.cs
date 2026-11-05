using System.Text;
using http;

// Protocol messages use CRLF on the wire, regardless of the file's line endings.
string wire = File.ReadAllText(SampleFiles.InputPath("protocols/order-request.http")).ReplaceLineEndings("\r\n");
var parser = new Http1Parser();
parser.ResetHttp1Parser(new Http1ParserOptions { MessageKind = HttpMessageKind.Request });
var parsed = parser.ParseHttp1Bytes(Encoding.ASCII.GetBytes(wire));
if (parsed.ErrorCode is not null)
{
    throw new InvalidOperationException(parsed.ErrorMessage);
}

foreach (var item in parsed.Events)
{
    Console.WriteLine($"Parser event: {item.EventKind}");
}

var request = parsed.Events.Select(e => e.Request).LastOrDefault(r => r is not null)
    ?? throw new InvalidOperationException("No request was parsed.");
Console.WriteLine($"Request: {request.Method} {request.Target}");
Console.WriteLine($"Body: {Encoding.UTF8.GetString(request.Content)}");
var serialized = new Http1Serializer().SerializeHttp1Request(request, new Http1SerializationOptions());
if (serialized.Status != HttpOperationStatus.Done)
{
    throw new InvalidOperationException(serialized.ErrorMessage);
}

Console.WriteLine($"\nSerialized HTTP ({serialized.Bytes.Length} bytes):");
Console.WriteLine(Encoding.ASCII.GetString(serialized.Bytes));
if (Encoding.UTF8.GetString(request.Content) != "hello")
{
    throw new InvalidOperationException("The body was not preserved.");
}
