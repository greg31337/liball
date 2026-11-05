using png;

// Read a viewable menu image and request a predictable four-channel pixel layout.
string inputPath = SampleFiles.InputPath("images/cafe-menu.png");
byte[] input = File.ReadAllBytes(inputPath);
var codec = new PngCodec();
var options = new PngDecoderOptions
{
    PreferredPixelFormat = PngPixelFormat.Rgba8
};

var menu = codec.DecodeBytes(input, options);
Console.WriteLine($"Menu image: {Path.GetFileName(inputPath)}");
Console.WriteLine($"Dimensions: {menu.Info.Width} x {menu.Info.Height}; input size: {input.Length:N0} bytes");

// Stride is the byte distance between rows; it need not equal width * 4.
foreach (var point in new[] { (X: 0, Y: 0), (X: 48, Y: 48), (X: 360, Y: 240) })
{
    int offset = point.Y * menu.Pixels.Stride + point.X * 4;
    byte[] rgba = menu.Pixels.Data.AsSpan(offset, 4).ToArray();
    Console.WriteLine($"Pixel ({point.X}, {point.Y}): RGBA ({string.Join(", ", rgba)})");
}

// Compare decoded pixels: PNG compression can change bytes without changing the picture.
byte[] encoded = codec.EncodeToBytes(menu, new PngEncoderOptions());
var reopened = codec.DecodeBytes(encoded, options);
if (!menu.Pixels.Data.SequenceEqual(reopened.Pixels.Data))
{
    throw new InvalidOperationException("PNG pixels changed during the round trip.");
}

string outputPath = SampleFiles.OutputPath("PngImages", "cafe-menu.png");
File.WriteAllBytes(outputPath, encoded);
Console.WriteLine($"All {menu.Info.Width * menu.Info.Height:N0} pixels preserved.");
Console.WriteLine($"Saved: {outputPath} ({encoded.Length:N0} bytes)");
