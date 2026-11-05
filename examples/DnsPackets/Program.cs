using dns;

var names = new DnsNameService();

// Build a DNS A question without sending it to a resolver.
var query = new DnsMessage();
query.Header.Identifier = 0x1234;
query.Header.FlagsValue = 0x0100; // Request recursion.
query.Questions.Add(new DnsQuestion
{
    Name = names.ParseName("example.com.", null),
    RecordType = DnsRecordType.A,
    RecordTypeValue = 1,
    Class = DnsClass.Internet,
    ClassValue = 1
});

var codec = new DnsMessageCodec();
var encoded = codec.EncodeMessage(query, new DnsEncodeOptions());
if (!encoded.Success)
{
    throw new InvalidOperationException("DNS encoding failed.");
}

var decoded = codec.DecodeMessage(encoded.Bytes, new DnsDecodeOptions());
if (!decoded.Success || decoded.Message is null)
{
    throw new InvalidOperationException("DNS decoding failed.");
}

var question = decoded.Message.Questions.Single();
Console.WriteLine($"DNS packet: {encoded.Bytes.Length} bytes; ID: 0x{decoded.Message.Header.Identifier:X4}");
Console.WriteLine($"Question: {names.FormatName(question.Name, new DnsTextOptions())} {question.RecordType}");

// Reverse IPv4 queries reverse the address octets under in-addr.arpa.
var reverse = names.BuildIpv4ReverseName(new byte[] { 192, 0, 2, 10 });
Console.WriteLine($"Reverse name: {names.FormatName(reverse, new DnsTextOptions())}");
if (!names.NamesEqual(query.Questions[0].Name, question.Name))
{
    throw new InvalidOperationException("DNS name changed.");
}
