using System.Text;
using sip;

// Protocol messages use CRLF on the wire, regardless of the file's line endings.
string wire = File.ReadAllText(SampleFiles.InputPath("protocols/service-options.sip")).ReplaceLineEndings("\r\n");
var codec = new SipMessageCodec();
var parsed = codec.ParseDatagramMessage(Encoding.ASCII.GetBytes(wire), new SipParseOptions());
var message = parsed.Message ?? throw new InvalidOperationException(parsed.ErrorMessage ?? "No SIP message.");
Console.WriteLine(
    $"Method: {message.Method}; header count: {message.Headers.Length}; body: {message.Body.Length} bytes");
var saved = codec.SerializeMessage(message, new SipWriteOptions());
if (!saved.Success)
{
    throw new InvalidOperationException(saved.ErrorMessage);
}

Console.WriteLine("Serialized SIP request:");
Console.WriteLine(Encoding.ASCII.GetString(saved.Bytes));
var reopened = codec.ParseDatagramMessage(saved.Bytes, new SipParseOptions());
if (reopened.Message?.Method != message.Method)
{
    throw new InvalidOperationException("SIP method changed.");
}
