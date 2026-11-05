using System.Globalization;
using tiff;

return TiffTranscodeProgram.Run(args, Console.Out, Console.Error);

public static class TiffTranscodeProgram
{
    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2 || args.Length > 3 || args[0] == "-h" || args[0] == "--help")
        {
            error.WriteLine("Usage: TiffTranscode <input-tiff> <output-tiff> [--classic|--bigtiff]");
            return args.Length == 1 ? 0 : 2;
        }

        string inputPath = args[0];
        string outputPath = args[1];
        if (!File.Exists(inputPath))
        {
            error.WriteLine("TIFF file does not exist: " + inputPath);
            return 1;
        }

        string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
        {
            error.WriteLine("Output directory does not exist: " + outputDirectory);
            return 1;
        }

        TiffFile input = TiffApi.Factory.OpenReadPath(inputPath, new TiffReadOptions());
        if (!input.GetSuccess())
        {
            error.WriteLine("Failed to open input TIFF.");
            WriteDiagnostics(input.GetDiagnostics(), error);
            return 1;
        }

        TiffWriteOptions writeOptions = new TiffWriteOptions();
        writeOptions.ByteOrder = input.GetHeader().ByteOrder;
        writeOptions.FileKind = input.GetHeader().FileKind;
        if (args.Length == 3)
        {
            if (args[2] == "--classic")
            {
                writeOptions.FileKind = TiffFileKind.Classic;
            }
            else if (args[2] == "--bigtiff")
            {
                writeOptions.FileKind = TiffFileKind.BigTiff;
            }
            else
            {
                error.WriteLine("Unknown output option: " + args[2]);
                return 2;
            }
        }

        TiffTranscodeResult result = Transcode(input, writeOptions);
        if (!result.Success)
        {
            error.WriteLine("Failed to transcode TIFF.");
            WriteDiagnostics(result.Diagnostics, error);
            return 1;
        }

        File.WriteAllBytes(outputPath, result.FileBytes);
        output.WriteLine("Transcoded TIFF");
        output.WriteLine("Input: " + inputPath);
        output.WriteLine("Output: " + outputPath);
        output.WriteLine("InputKind: " + input.GetHeader().FileKind);
        output.WriteLine("OutputKind: " + TiffApi.Factory.OpenMetadataBytes(result.FileBytes, new TiffReadOptions()).GetHeader().FileKind);
        output.WriteLine("Images: " + input.GetImageCount().ToString(CultureInfo.InvariantCulture));
        output.WriteLine("Bytes: " + result.FileBytes.Length.ToString(CultureInfo.InvariantCulture));
        return 0;
    }

    public static TiffTranscodeResult Transcode(TiffFile input, TiffWriteOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (input == null)
        {
            diagnostics.AddError(TiffErrorCode.UnknownError, "An input TIFF file is required.");
            return new TiffTranscodeResult(new byte[0], diagnostics);
        }

        TiffWriter writer = TiffApi.Factory.CreateWriter(options);
        for (int imageIndex = 0; imageIndex < input.GetImageCount(); imageIndex++)
        {
            TiffImage sourceImage = input.GetImage(imageIndex);
            TiffRawImageDataCopyResult rawResult = TiffApi.Copy.CopyRawImageData(sourceImage);
            diagnostics.Merge(rawResult.Diagnostics);
            if (diagnostics.HasErrors())
            {
                return new TiffTranscodeResult(new byte[0], diagnostics);
            }

            int destinationImageIndex = writer.AddImage(rawResult.Definition);
            diagnostics.Merge(TiffApi.Copy.CopyMetadataToWriter(sourceImage, writer, destinationImageIndex));
            if (diagnostics.HasErrors())
            {
                return new TiffTranscodeResult(new byte[0], diagnostics);
            }

        }

        diagnostics.Merge(writer.Flush());
        diagnostics.Merge(writer.GetDiagnostics());
        if (diagnostics.HasErrors())
        {
            return new TiffTranscodeResult(new byte[0], diagnostics);
        }

        return new TiffTranscodeResult(writer.GetFileBytes(), diagnostics);
    }

    static void WriteDiagnostics(TiffDiagnostics diagnostics, TextWriter error)
    {
        for (int index = 0; index < diagnostics.Count(); index++)
        {
            error.WriteLine(diagnostics.Format(diagnostics.Get(index)));
        }
    }
}

public class TiffTranscodeResult
{
    public bool Success;
    public byte[] FileBytes;
    public TiffDiagnostics Diagnostics;

    public TiffTranscodeResult(byte[] fileBytes, TiffDiagnostics diagnostics)
    {
        FileBytes = fileBytes.AsSpan().ToArray();
        Diagnostics = diagnostics;
        Success = !Diagnostics.HasErrors();
    }
}
