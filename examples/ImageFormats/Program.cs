using image;

var detector = new ImageFormatDetector();
string[] samples =
{
    "images/cafe-menu.png",
    "images/loading-progress.gif",
    "qr/workshop-qr.png"
};

Console.WriteLine($"{"File",-24} {"Bytes",8} {"Format",-8} MIME type");

// Inspect file signatures rather than trusting the filename extension.
foreach (string sample in samples)
{
    byte[] bytes = File.ReadAllBytes(SampleFiles.InputPath(sample));
    var info = detector.DetectBytes(bytes);
    Console.WriteLine($"{Path.GetFileName(sample),-24} {bytes.Length,8:N0} {info.Kind,-8} {info.MimeType}");
}
