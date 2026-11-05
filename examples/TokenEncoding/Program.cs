using System.Text;
using token;

var encoding = new TokenEncodingService();

// Encode UTF-8 bytes so accented text survives every reversible representation.
byte[] input = Encoding.UTF8.GetBytes("café + tea / ?");
var url = encoding.EncodeBase64Url(input);
var hex = encoding.EncodeHex(input);
if (!url.Success || url.Text is null || !hex.Success)
{
    throw new InvalidOperationException("Encoding failed.");
}

Console.WriteLine($"Input:     {Encoding.UTF8.GetString(input)}");
Console.WriteLine($"Base64URL: {url.Text}");
Console.WriteLine($"Hex:       {hex.Text}");
var decoded = encoding.DecodeBase64Url(url.Text);
if (!decoded.Success || decoded.Bytes is null || !input.SequenceEqual(decoded.Bytes))
{
    throw new InvalidOperationException("Base64URL bytes changed.");
}

Console.WriteLine($"Decoded:   {Encoding.UTF8.GetString(decoded.Bytes)}");

// PEM wraps the same bytes in a labeled Base64 text block.
var pem = encoding.WritePemBlock("EXAMPLE DATA", input);
if (!pem.Success)
{
    throw new InvalidOperationException("PEM encoding failed.");
}

Console.WriteLine($"\n{pem.Text}");
Console.WriteLine($"Invalid Base64URL accepted: {encoding.DecodeBase64Url("***").Success}");
Console.WriteLine("These are reversible encodings; they do not encrypt or sign the data.");
