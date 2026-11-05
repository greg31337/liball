using cbor;

// Diagnostic notation can express integer map keys and byte strings directly.
const string input = "{1: h'0102FF', 2: [true, null, -7]}";
var diagnostics = new CborDiagnosticConverter();
var parsed = diagnostics.ParseDiagnostic(input, new CborDiagnosticOptions());
if (parsed.Status != CborReadStatus.Done || parsed.Value is null)
{
    throw new InvalidOperationException(parsed.ErrorMessage);
}

var encoder = new CborEncoder();

// Deterministic encoding gives the same wire bytes for the same CBOR value.
var options = new CborEncodeOptions
{
    EncodingProfile = CborEncodingProfile.CoreDeterministic
};

byte[] bytes = encoder.Encode(parsed.Value, options);
var decoded = new CborDecoder().Decode(bytes, new CborDecodeOptions());
if (decoded.Status != CborReadStatus.Done || decoded.Value is null)
{
    throw new InvalidOperationException(decoded.ErrorMessage);
}

Console.WriteLine($"Diagnostic input: {input}");
Console.WriteLine($"Encoded ({bytes.Length} bytes): {Convert.ToHexString(bytes)}");
Console.WriteLine($"Decoded: {diagnostics.FormatDiagnostic(decoded.Value, new CborDiagnosticOptions())}");
if (!bytes.SequenceEqual(encoder.Encode(decoded.Value, options)))
{
    throw new InvalidOperationException("Deterministic encoding changed.");
}
