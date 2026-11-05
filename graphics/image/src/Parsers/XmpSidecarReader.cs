namespace image;

internal class XmpSidecarReader
{
    internal static ImageMetadataDocument Parse(BoundedStreamReader? reader, ImageFormatInfo format, ImageMetadataReaderOptions options)
    {
        ImageMetadataDocument document = new ImageMetadataDocument();
        document.Format = BasicMetadataParser.CopyFormat(format);
        if (reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(document.Diagnostics, "xmp.sidecar.reader.null", "XMP sidecar reader was null.");
            return document;
        }

        document.ByteLength = reader.Length;
        CopyDiagnostics(document, reader);
        ImageMetadataReaderOptions actualOptions = options == null ? new ImageMetadataReaderOptions() : options;
        if (!actualOptions.ParseXmp)
        {
            return document;
        }

        MetadataProfile profile = CreateProfile(reader.Length);
        document.AddProfile(profile);
        if (actualOptions.MaxXmlBytes < 0)
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "xmp.sidecar.limit.negative", "Configured XMP XML byte limit was negative.", "XMP", 0, actualOptions.MaxXmlBytes);
            return document;
        }

        if (actualOptions.MaxXmlBytes > 0 && reader.Length > actualOptions.MaxXmlBytes)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "xmp.sidecar.limit", "XMP sidecar exceeded the configured XML byte limit.", "XMP", 0, reader.Length);
            return document;
        }

        if (reader.AccessibleLength() < reader.Length || reader.AccessibleLength() > 2147483647)
        {
            return document;
        }

        byte[] data = reader.ReadBytes((int)reader.AccessibleLength());
        profile.Data = data;
        profile.Text = MetadataXmlReader.DecodeXmlBytes(data, actualOptions.MaxXmlBytes, document.Diagnostics, "XMP");
        CaptureRawBlock(document, data, reader.Length, actualOptions);
        if (!IsXmpText(profile.Text))
        {
            MetadataDiagnostic.AddRecoverableErrorAt(document.Diagnostics, "xmp.sidecar.signature.invalid", "XMP sidecar did not contain an RDF/XMP packet.", "XMP", 0, reader.Length);
            return document;
        }

        XmpPacketParser.ParseProfile(document, profile, actualOptions, "XMP/Sidecar");
        ImageMetadataNormalizerCore.BuildSummary(document);
        return document;
    }

    internal static bool IsXmpText(string text)
    {
        return text != null && text.IndexOf(XmpPacketParser.RdfNamespace, System.StringComparison.Ordinal) >= 0;
    }

    internal static MetadataProfile CreateProfile(long length)
    {
        MetadataProfile profile = new MetadataProfile();
        profile.Kind = MetadataProfileKind.Xmp;
        profile.Family = MetadataFamily.Xmp;
        profile.Key = "Xmp.Sidecar";
        profile.Name = "XMP Sidecar";
        profile.MimeType = "application/rdf+xml";
        profile.Offset = 0;
        profile.Length = length;
        return profile;
    }

    internal static void CopyDiagnostics(ImageMetadataDocument document, BoundedStreamReader reader)
    {
        if (reader.Diagnostics == null)
        {
            return;
        }

        foreach (MetadataDiagnostic diagnostic in reader.Diagnostics)
        {
            document.Diagnostics.Add(diagnostic);
        }
    }

    internal static void CaptureRawBlock(ImageMetadataDocument document, byte[] data, long length, ImageMetadataReaderOptions options)
    {
        if (!options.CaptureRawBlocks)
        {
            return;
        }

        MetadataRawBlock block = document.AddRawBlockWithRange(MetadataFamily.Xmp, "XMP", "XMP Sidecar", 0, length);
        if (!options.CaptureRawBlockData)
        {
            return;
        }

        if (options.MaxRawBlockBytes < 0 || length > options.MaxRawBlockBytes)
        {
            MetadataDiagnostic.AddLimitExceeded(document.Diagnostics, "xmp.sidecar.raw.limit", "XMP sidecar raw block exceeded the configured raw-block byte limit.", "XMP", 0, length);
            return;
        }

        block.Data = Copy(data);
    }

    internal static byte[] Copy(byte[] data)
    {
        if (data == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            copy[index] = data[index];
            index = index + 1;
        }

        return copy;
    }
}
