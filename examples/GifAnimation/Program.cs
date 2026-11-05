using gif;

// The bundled GIF contains three visible progress-bar frames.
byte[] input = File.ReadAllBytes(SampleFiles.InputPath("images/loading-progress.gif"));
var codec = new GifCodec();
var animation = codec.DecodeAnimationBytes(input, null);
Console.WriteLine($"Progress animation: {animation.Width} x {animation.Height}");
Console.WriteLine($"Frames: {animation.Frames.Length}; one cycle: {animation.TotalDurationMilliseconds} ms");
for (int i = 0; i < animation.Frames.Length; i++)
{
    var frame = animation.Frames[i];
    Console.WriteLine(
        $"  Frame {i + 1}: {frame.DelayMilliseconds} ms, {frame.Bitmap.RgbaPixels.Length:N0} RGBA bytes");
}

// Preserve the document's timing, palette, loop count, and frame disposal metadata.
var document = codec.DecodeBytes(input, null);
byte[] encoded = codec.Encode(document, null);
var reopened = codec.DecodeAnimationBytes(encoded, null);
if (reopened.Frames.Length != animation.Frames.Length || reopened.LoopCount != animation.LoopCount)
{
    throw new InvalidOperationException("GIF frame count or loop count changed.");
}

for (int i = 0; i < animation.Frames.Length; i++)
{
    if (reopened.Frames[i].DelayMilliseconds != animation.Frames[i].DelayMilliseconds ||
        !reopened.Frames[i].Bitmap.RgbaPixels.SequenceEqual(animation.Frames[i].Bitmap.RgbaPixels))
    {
        throw new InvalidOperationException("GIF timing or pixels changed.");
    }
}

string outputPath = SampleFiles.OutputPath("GifAnimation", "loading-progress.gif");
File.WriteAllBytes(outputPath, encoded);
Console.WriteLine($"Animation frames and timing preserved. Saved: {outputPath}");
