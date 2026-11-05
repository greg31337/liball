using System.Globalization;
using tiff;

return TiffMetadataDumpProgram.Run(args, Console.Out, Console.Error);

public static class TiffMetadataDumpProgram
{
    const long MaxDecodedValueBytes = 128;

    public static int Run(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length != 1 || args[0] == "-h" || args[0] == "--help")
        {
            error.WriteLine("Usage: TiffMetadataDump <path-to-tiff>");
            return args.Length == 1 ? 0 : 2;
        }

        string path = args[0];
        if (!File.Exists(path))
        {
            error.WriteLine("TIFF file does not exist: " + path);
            return 1;
        }

        TiffFile file = TiffApi.Factory.OpenMetadataPath(path, new TiffReadOptions());
        if (!file.GetSuccess())
        {
            error.WriteLine("Failed to open TIFF metadata.");
            WriteDiagnostics(file.GetDiagnostics(), error);
            return 1;
        }

        WriteDump(path, file, output);
        if (!file.GetSuccess())
        {
            error.WriteLine("Failed to inspect TIFF metadata.");
            WriteDiagnostics(file.GetDiagnostics(), error);
            return 1;
        }

        return 0;
    }

    static void WriteDump(string path, TiffFile file, TextWriter output)
    {
        TiffHeader header = file.GetHeader();
        byte[] fileBytes = file.GetFileBytes();
        TiffTagRegistry registry = TiffApi.Factory.CreateDefaultTagRegistry();

        output.WriteLine("TIFF metadata dump");
        output.WriteLine("Path: " + path);
        output.WriteLine("FileKind: " + header.FileKind);
        output.WriteLine("ByteOrder: " + header.ByteOrder);
        output.WriteLine("FirstIfdOffset: " + header.FirstIfdOffset.ToString(CultureInfo.InvariantCulture));
        output.WriteLine("FileBytes: " + file.GetFileByteCount().ToString(CultureInfo.InvariantCulture));
        output.WriteLine("Images: " + file.GetImageCount().ToString(CultureInfo.InvariantCulture));

        for (int imageIndex = 0; imageIndex < file.GetImageCount(); imageIndex++)
        {
            TiffImage image = file.GetImage(imageIndex);
            TiffImageInfo info = image.GetInfo();
            TiffDirectory directory = image.GetDirectory();

            output.WriteLine();
            output.WriteLine("Image " + imageIndex.ToString(CultureInfo.InvariantCulture));
            output.WriteLine("  Width: " + info.Width.ToString(CultureInfo.InvariantCulture));
            output.WriteLine("  Length: " + info.Length.ToString(CultureInfo.InvariantCulture));
            output.WriteLine("  SamplesPerPixel: " + info.SamplesPerPixel.ToString(CultureInfo.InvariantCulture));
            output.WriteLine("  BitsPerSample: " + FormatIntArray(info.BitsPerSample));
            output.WriteLine("  Compression: " + info.Compression);
            output.WriteLine("  Photometric: " + info.Photometric);
            output.WriteLine("  IsTiled: " + info.IsTiled);
            output.WriteLine("  SubImages: " + image.GetSubImageCount().ToString(CultureInfo.InvariantCulture));
            output.WriteLine("  Tags:");

            foreach (TiffIfdEntry entry in image.GetTags())
            {
                output.WriteLine("    " + FormatEntry(entry, directory, fileBytes, header.ByteOrder, registry));
            }
        }
    }

    static string FormatEntry(TiffIfdEntry entry, TiffDirectory directory, byte[] fileBytes, TiffByteOrder byteOrder, TiffTagRegistry registry)
    {
        string tagName = GetTagName(registry, entry.TagId);
        string storage = entry.ValueByteCount > entry.ValueSlotByteCount ? "external" : "inline";
        return entry.TagId.ToString(CultureInfo.InvariantCulture)
            + " " + tagName
            + " " + entry.DataType
            + " count=" + entry.Count.ToString(CultureInfo.InvariantCulture)
            + " bytes=" + entry.ValueByteCount.ToString(CultureInfo.InvariantCulture)
            + " " + storage
            + " " + FormatValue(entry, directory, fileBytes, byteOrder);
    }

    static string GetTagName(TiffTagRegistry registry, int tagId)
    {
        TiffTagDefinition definition = registry.Find(tagId);
        if (definition == null || string.IsNullOrWhiteSpace(definition.Name))
        {
            return "Tag" + tagId.ToString(CultureInfo.InvariantCulture);
        }

        return definition.Name;
    }

    static string FormatValue(TiffIfdEntry entry, TiffDirectory directory, byte[] fileBytes, TiffByteOrder byteOrder)
    {
        if (entry.ValueByteCount > MaxDecodedValueBytes)
        {
            return "value=<omitted>";
        }

        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue value = TiffApi.Codec.ReadTagValue(entry, fileBytes, byteOrder, diagnostics);
        if (diagnostics.HasErrors())
        {
            return "value=<unreadable>";
        }

        if (value.TextValues.Length > 0)
        {
            return "value=\"" + EscapeText(value.Text) + "\"";
        }

        if (value.UnsignedValues.Length > 0)
        {
            return "value=" + FormatUnsignedArray(value.UnsignedValues);
        }

        if (value.SignedValues.Length > 0)
        {
            return "value=" + string.Join(",", value.SignedValues.Select(item => item.ToString(CultureInfo.InvariantCulture)));
        }

        if (value.FloatingValues.Length > 0)
        {
            return "value=" + string.Join(",", value.FloatingValues.Select(item => item.ToString(CultureInfo.InvariantCulture)));
        }

        if (value.RationalValues.Length > 0)
        {
            return "value=" + string.Join(",", value.RationalValues.Select(item => item.Numerator.ToString(CultureInfo.InvariantCulture) + "/" + item.Denominator.ToString(CultureInfo.InvariantCulture)));
        }

        if (value.RawBytes.Length > 0)
        {
            return "value=<raw " + value.RawBytes.Length.ToString(CultureInfo.InvariantCulture) + " bytes>";
        }

        return "value=<empty>";
    }

    static string FormatIntArray(int[] values)
    {
        return string.Join(",", values.Select(item => item.ToString(CultureInfo.InvariantCulture)));
    }

    static string FormatUnsignedArray(ulong[] values)
    {
        return string.Join(",", values.Select(item => item.ToString(CultureInfo.InvariantCulture)));
    }

    static string EscapeText(string text)
    {
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    static void WriteDiagnostics(TiffDiagnostics diagnostics, TextWriter error)
    {
        for (int index = 0; index < diagnostics.Count(); index++)
        {
            error.WriteLine(diagnostics.Format(diagnostics.Get(index)));
        }
    }
}
