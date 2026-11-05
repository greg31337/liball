using System.Text;
using cbor;

// A cold-room sensor message contains timestamps, measurements, and an array.
string input = File.ReadAllText(SampleFiles.InputPath("sensor-reading.json"));
var json = new CborJsonConverter();
var parsed = json.ConvertFromJson(input, new CborJsonOptions());
if (parsed.Status != CborReadStatus.Done || parsed.Value is null)
{
    throw new InvalidOperationException(parsed.ErrorMessage ?? "Could not convert JSON to CBOR.");
}

byte[] bytes = new CborEncoder().Encode(parsed.Value, new CborEncodeOptions());
Console.WriteLine("A sensor message encoded as CBOR");
Console.WriteLine($"JSON input: {input}");
Console.WriteLine($"UTF-8 JSON: {Encoding.UTF8.GetByteCount(input)} bytes; CBOR: {bytes.Length} bytes");
Console.WriteLine($"CBOR hex: {Convert.ToHexString(bytes)}");
var decoded = new CborDecoder().Decode(bytes, new CborDecodeOptions());
if (decoded.Status != CborReadStatus.Done || decoded.Value is null || decoded.Remaining != 0)
{
    throw new InvalidOperationException(decoded.ErrorMessage ?? "Could not decode the complete CBOR message.");
}

Console.WriteLine("\nDecoded JSON:");
Console.WriteLine(json.ConvertToJson(decoded.Value, new CborJsonOptions()));
byte[] reencoded = new CborEncoder().Encode(decoded.Value, new CborEncodeOptions());
if (!bytes.SequenceEqual(reencoded))
{
    throw new InvalidOperationException("CBOR bytes changed during the round trip.");
}

Console.WriteLine("CBOR round trip: bytes preserved.");
