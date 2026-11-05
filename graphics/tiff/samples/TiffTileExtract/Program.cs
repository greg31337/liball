using System.Globalization;
using tiff;

return TiffTileExtractProgram.Run(args, Console.Out, Console.Error);

public static class TiffTileExtractProgram
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 4 || args[0] == "-h" || args[0] == "--help")
        {
            error.WriteLine("Usage: TiffTileExtract <input-tiff> <image-index> <tile-index> <output-bytes>");
            return args.Length == 1 ? 0 : 2;
        }

        string inputPath = args[0];
        string outputPath = args[3];
        if (!File.Exists(inputPath))
        {
            error.WriteLine("TIFF file does not exist: " + inputPath);
            return 1;
        }

        if (!TryParseIndex(args[1], "image index", error, out int imageIndex))
        {
            return 2;
        }

        if (!TryParseIndex(args[2], "tile index", error, out int tileIndex))
        {
            return 2;
        }

        string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            error.WriteLine("Output directory does not exist: " + outputDirectory);
            return 1;
        }

        TiffFile file = TiffApi.Factory.OpenReadPath(inputPath, new TiffReadOptions());
        if (!file.GetSuccess())
        {
            error.WriteLine("Failed to open TIFF file.");
            WriteDiagnostics(file.GetDiagnostics(), error);
            return 1;
        }

        if (imageIndex < 0 || imageIndex >= file.GetImageCount())
        {
            error.WriteLine("Image index is outside the available image range.");
            return 1;
        }

        TiffImage image = file.GetImage(imageIndex);
        TiffImageInfo info = image.GetInfo();
        if (!info.IsTiled)
        {
            error.WriteLine("Selected image is not tiled.");
            return 1;
        }

        TiffRawTileReadResult result = image.ReadRawTile(tileIndex);
        if (!result.Success)
        {
            error.WriteLine("Failed to read tile.");
            WriteDiagnostics(result.Diagnostics, error);
            return 1;
        }

        File.WriteAllBytes(outputPath, result.Bytes);
        output.WriteLine("Extracted raw tile");
        output.WriteLine("Input: " + inputPath);
        output.WriteLine("Output: " + outputPath);
        output.WriteLine("ImageIndex: " + imageIndex.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("TileIndex: " + tileIndex.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("TileWidth: " + info.TileWidth.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("TileLength: " + info.TileLength.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("Bytes: " + result.Bytes.Length.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("RangeOffset: " + result.Range.Offset.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("RangeByteCount: " + result.Range.ByteCount.ToString(CultureInfo.InvariantCulture));
        return 0;
    }

    static bool TryParseIndex(string text, string label, TextWriter error, out int value)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value) || value < 0)
        {
            error.WriteLine("Invalid " + label + ": " + text);
            return false;
        }

        return true;
    }

    static void WriteDiagnostics(TiffDiagnostics diagnostics, TextWriter error)
    {
        for (int index = 0; index < diagnostics.Count(); index++)
        {
            error.WriteLine(diagnostics.Format(diagnostics.Get(index)));
        }
    }
}
