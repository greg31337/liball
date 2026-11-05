using System.Text;
using png;
using qrcode;

Console.OutputEncoding = Encoding.UTF8;
string payload = File.ReadAllText(SampleFiles.InputPath("qr/workshop-url.txt")).Trim();
var png = new PngCodec();

// Detect the QR grid in a real PNG; no module coordinates are supplied.
byte[] input = File.ReadAllBytes(SampleFiles.InputPath("qr/workshop-qr.png"));
var source = png.DecodeBytes(input, new PngDecoderOptions { PreferredPixelFormat = PngPixelFormat.Rgba8 });
var image = new QrImage
{
    Pixels = source.Pixels.Data,
    Width = source.Info.Width,
    Height = source.Info.Height,
    Stride = source.Pixels.Stride,
    PixelFormat = QrPixelFormat.Rgba32
};

var detectionOptions = new QrDetectionOptions();
var detected = new QrDetector().DetectImage(image, detectionOptions);
var detection = detected.Detections.FirstOrDefault()
    ?? throw new InvalidOperationException("No QR code was found in the sample PNG.");
var decoded = new QrDecoder().DecodeDetection(image, detection, detectionOptions, new QrDecodeOptions());
if (decoded.Text != payload)
{
    throw new InvalidOperationException("The PNG contains an unexpected QR payload.");
}

Console.WriteLine($"Read workshop-qr.png: {image.Width} x {image.Height} pixels");
Console.WriteLine($"Decoded from image: {decoded.Text}");

// A four-module quiet zone and integer pixel scaling keep the exported code scannable.
var symbol = new QrEncoder().EncodeText(payload, new QrEncodeOptions
{
    ForceSymbolKind = true,
    SymbolKind = QrSymbolKind.Model2,
    ForceErrorCorrectionLevel = true,
    ErrorCorrectionLevel = QrErrorCorrectionLevel.Medium,
    BoostErrorCorrectionLevel = false,
    EciAssignment = 26,
    QuietZoneModules = 4
});

var matrix = symbol.Matrix ?? throw new InvalidOperationException("The encoder returned no matrix.");
Console.WriteLine($"Version {symbol.Version}, error correction {symbol.ErrorCorrectionLevel}");
for (int row = 0; row < matrix.Height; row++)
{
    var line = new StringBuilder();
    for (int column = 0; column < matrix.Width; column++)
    {
        line.Append(matrix.Values[row * matrix.Width + column] == QrModuleValue.Dark ? "  " : "██");
    }

    Console.WriteLine(line);
}

var raster = new QrRenderer().RenderSymbol(symbol, new QrRenderOptions
{
    Scale = 8,
    QuietZoneModules = 4
});

var outputImage = new PngImageFactory().CreateImageWithData(
    raster.Width,
    raster.Height,
    PngPixelFormat.Grayscale8,
    raster.Pixels);
string outputPath = SampleFiles.OutputPath("QrCodes", "workshop-qr.png");
File.WriteAllBytes(outputPath, png.EncodeToBytes(outputImage, new PngEncoderOptions()));
Console.WriteLine($"Saved a scannable PNG: {outputPath}");
