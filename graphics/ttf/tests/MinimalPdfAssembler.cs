using System.Globalization;
using System.Text;
using ttf;

namespace ttf.Tests;

internal static class MinimalPdfAssembler
{
    internal static byte[] Assemble(
        PdfFontResource resource,
        byte[] characterCodes)
    {
        return Assemble(resource, characterCodes, null);
    }

    internal static byte[] Assemble(
        PdfFontResource resource,
        byte[] characterCodes,
        string? actualText)
    {
        if (resource == null)
        {
            throw new ArgumentNullException(nameof(resource));
        }

        if (characterCodes == null)
        {
            throw new ArgumentNullException(nameof(characterCodes));
        }

        int codeByteCount = resource.GetToUnicodeMappings()
            .GetCodeByteCount();

        if (characterCodes.Length % codeByteCount != 0)
        {
            throw new ArgumentException(
                "Character codes do not match the resource code width.",
                nameof(characterCodes));
        }

        MinimalPdfObjectStore objects = new MinimalPdfObjectStore();
        int catalogObject = objects.Reserve();
        int pagesObject = objects.Reserve();
        int pageObject = objects.Reserve();
        int contentObject = objects.AddStream(
            string.Empty,
            CreateContent(characterCodes, actualText));
        int toUnicodeObject = objects.AddStream(
            string.Empty,
            resource.GetToUnicodeMappings().GetCMap().GetBytes());
        int fontObject = resource.GetFontKind() ==
            PdfFontKind.SimpleTrueType ?
            AddSimpleFont(objects, resource, toUnicodeObject) :
            AddCompositeFont(objects, resource, toUnicodeObject);

        objects.Set(
            pageObject,
            Ascii(
                "<< /Type /Page /Parent " + Reference(pagesObject) +
                " /MediaBox [0 0 612 792] /Resources << /Font << /F1 " +
                Reference(fontObject) + " >> >> /Contents " +
                Reference(contentObject) + " >>"));
        objects.Set(
            pagesObject,
            Ascii(
                "<< /Type /Pages /Count 1 /Kids [" +
                Reference(pageObject) + "] >>"));
        objects.Set(
            catalogObject,
            Ascii(
                "<< /Type /Catalog /Pages " +
                Reference(pagesObject) + " >>"));
        return objects.Build(catalogObject);
    }

    static int AddSimpleFont(
        MinimalPdfObjectStore objects,
        PdfFontResource resource,
        int toUnicodeObject)
    {
        PdfSimpleFontEncodingArtifact? encoding =
            resource.GetSimpleEncoding();
        string baseFont = new PdfName(
            resource.GetBaseFontName()).GetToken();
        byte[] fontBytes = resource.CopyEmbeddedFont();
        int fontFileObject = objects.AddStream(
            " /Length1 " + Invariant(fontBytes.Length),
            fontBytes);
        int descriptorObject = objects.Add(Ascii(
            "<< /Type /FontDescriptor /FontName " + baseFont +
            " /Flags 4 /FontBBox [0 -1000 2000 2000]" +
            " /ItalicAngle 0 /Ascent 1000 /Descent -250" +
            " /CapHeight 700 /StemV 80 /MissingWidth 1000" +
            " /FontFile2 " + Reference(fontFileObject) + " >>"));
        int lastCode = Math.Max(0, encoding!.GetDifferenceCount() - 1);
        StringBuilder widths = new StringBuilder("[");
        int code = 0;

        while (code <= lastCode)
        {
            if (code != 0)
            {
                widths.Append(' ');
            }

            widths.Append("1000");
            code += 1;
        }

        widths.Append(']');
        StringBuilder differences = new StringBuilder("[");
        int index = 0;

        while (index < encoding.GetDifferenceCount())
        {
            PdfSimpleFontEncodingDifference difference =
                encoding.GetDifference(index);
            differences.Append(' ');
            differences.Append(Invariant(difference.GetCharacterCode()));
            differences.Append(' ');
            differences.Append(new PdfName(
                difference.GetGlyphName()).GetToken());
            index += 1;
        }

        differences.Append(" ]");
        return objects.Add(Ascii(
            "<< /Type /Font /Subtype /TrueType /BaseFont " + baseFont +
            " /FirstChar 0 /LastChar " + Invariant(lastCode) +
            " /Widths " + widths +
            " /FontDescriptor " + Reference(descriptorObject) +
            " /Encoding << /Type /Encoding /BaseEncoding /" +
            encoding.GetBaseEncodingName() + " /Differences " +
            differences + " >> /ToUnicode " +
            Reference(toUnicodeObject) + " >>"));
    }

    static int AddCompositeFont(
        MinimalPdfObjectStore objects,
        PdfFontResource resource,
        int toUnicodeObject)
    {
        MinimalPdfCompositeFontData data = GetCompositeData(resource);
        string fontFileEntries = string.Empty;

        if (data.metrics.HasLength1())
        {
            fontFileEntries += " /Length1 " +
                Invariant(data.metrics.GetLength1());
        }

        if (data.metrics.HasFontFileSubtype())
        {
            fontFileEntries += " /Subtype " + new PdfName(
                data.metrics.GetFontFileSubtype()!).GetToken();
        }

        int fontFileObject = objects.AddStream(
            fontFileEntries,
            data.fontBytes);
        int cidMapObject = 0;
        byte[] cidMapBytes = data.cidMapping.CopyCidToGidMap();

        if (cidMapBytes.Length != 0)
        {
            cidMapObject = objects.AddStream(string.Empty, cidMapBytes);
        }

        int cidSetObject = 0;

        if (data.cidMapping.HasCidSet())
        {
            cidSetObject = objects.AddStream(
                string.Empty,
                data.cidMapping.CopyCidSet());
        }

        string baseFont = new PdfName(data.baseFontName).GetToken();
        StringBuilder descriptor = new StringBuilder(
            "<< /Type /FontDescriptor /FontName ");
        descriptor.Append(baseFont);
        descriptor.Append(" /Flags ");
        descriptor.Append(Invariant(data.flags.GetFlags()));
        descriptor.Append(" /FontBBox [");
        descriptor.Append(Invariant(data.metrics.GetBoundingBoxXMinimum()));
        descriptor.Append(' ');
        descriptor.Append(Invariant(data.metrics.GetBoundingBoxYMinimum()));
        descriptor.Append(' ');
        descriptor.Append(Invariant(data.metrics.GetBoundingBoxXMaximum()));
        descriptor.Append(' ');
        descriptor.Append(Invariant(data.metrics.GetBoundingBoxYMaximum()));
        descriptor.Append("] /ItalicAngle ");
        descriptor.Append(Invariant(data.metrics.GetItalicAngle()));
        descriptor.Append(" /Ascent ");
        descriptor.Append(Invariant(data.metrics.GetAscent()));
        descriptor.Append(" /Descent ");
        descriptor.Append(Invariant(data.metrics.GetDescent()));
        descriptor.Append(" /CapHeight ");
        descriptor.Append(Invariant(data.metrics.GetCapHeight()));

        if (data.metrics.HasXHeight())
        {
            descriptor.Append(" /XHeight ");
            descriptor.Append(Invariant(data.metrics.GetXHeight()));
        }

        descriptor.Append(" /StemV ");
        descriptor.Append(Invariant(data.metrics.GetStemVertical()));
        descriptor.Append(" /MissingWidth ");
        descriptor.Append(Invariant(data.metrics.GetMissingWidth()));
        descriptor.Append(" /");
        descriptor.Append(data.metrics.GetFontFileKey());
        descriptor.Append(' ');
        descriptor.Append(Reference(fontFileObject));

        if (cidSetObject != 0)
        {
            descriptor.Append(" /CIDSet ");
            descriptor.Append(Reference(cidSetObject));
        }

        descriptor.Append(" >>");
        int descriptorObject = objects.Add(Ascii(descriptor.ToString()));
        StringBuilder descendant = new StringBuilder(
            "<< /Type /Font /Subtype /");
        descendant.Append(data.descendantKind ==
            PdfFontKind.Type0CidFontType2 ?
            "CIDFontType2" : "CIDFontType0");
        descendant.Append(" /BaseFont ");
        descendant.Append(baseFont);
        descendant.Append(
            " /CIDSystemInfo << /Registry (Adobe) /Ordering (Identity) /Supplement 0 >>");
        descendant.Append(" /FontDescriptor ");
        descendant.Append(Reference(descriptorObject));
        descendant.Append(" /DW ");
        descendant.Append(Invariant(data.widths.GetDefaultWidth()));
        descendant.Append(" /W ");
        descendant.Append(SerializeWidths(data.widths));

        if (data.verticalWidths != null)
        {
            descendant.Append(" /DW2 [");
            descendant.Append(Invariant(
                data.verticalWidths.GetDefaultOriginY()));
            descendant.Append(' ');
            descendant.Append(Invariant(
                data.verticalWidths.GetDefaultDisplacementY()));
            descendant.Append("] /W2 ");
            descendant.Append(SerializeVerticalWidths(
                data.verticalWidths));
        }

        if (data.cidMapping.HasCidToGidMap())
        {
            descendant.Append(" /CIDToGIDMap ");

            if (data.cidMapping.UsesIdentityCidToGidMap())
            {
                descendant.Append("/Identity");
            }
            else
            {
                descendant.Append(Reference(cidMapObject));
            }
        }

        descendant.Append(" >>");
        int descendantObject = objects.Add(Ascii(descendant.ToString()));
        string encodingName = data.verticalWidths == null ?
            "Identity-H" : "Identity-V";
        return objects.Add(Ascii(
            "<< /Type /Font /Subtype /Type0 /BaseFont " + baseFont +
            " /Encoding /" + encodingName + " /DescendantFonts [" +
            Reference(descendantObject) + "] /ToUnicode " +
            Reference(toUnicodeObject) + " >>"));
    }

    static MinimalPdfCompositeFontData GetCompositeData(
        PdfFontResource resource)
    {
        MinimalPdfCompositeFontData data =
            new MinimalPdfCompositeFontData();

        if (resource.GetFontKind() == PdfFontKind.Type0CidFontType2)
        {
            PdfCidFontType2Artifact? artifact =
                resource.GetCidFontType2Artifact();
            data.baseFontName = artifact!.GetBaseFontName();
            data.fontBytes = artifact.CopyFontFile2();
            data.descendantKind = PdfFontKind.Type0CidFontType2;
            data.cidMapping = artifact.GetCidMapping();
            data.widths = artifact.GetWidths();
            data.verticalWidths = artifact.GetVerticalWidths();
            data.flags = artifact.GetDescriptorFlags();
            data.metrics = artifact.GetDescriptorMetrics();
            return data;
        }

        if (resource.GetFontKind() == PdfFontKind.Type0CidFontType0)
        {
            PdfCidFontType0Artifact? artifact =
                resource.GetCidFontType0Artifact();
            data.baseFontName = artifact!.GetBaseFontName();
            data.fontBytes = artifact.CopyFontFile3();
            data.descendantKind = PdfFontKind.Type0CidFontType0;
            data.cidMapping = artifact.GetCidMapping();
            data.widths = artifact.GetWidths();
            data.verticalWidths = artifact.GetVerticalWidths();
            data.flags = artifact.GetDescriptorFlags();
            data.metrics = artifact.GetDescriptorMetrics();
            return data;
        }

        PdfOpenTypeFontArtifact? openType = resource.GetOpenTypeArtifact();
        data.baseFontName = openType!.GetBaseFontName();
        data.fontBytes = openType.CopyFontFile3();
        data.descendantKind = openType.GetDescendantFontKind();
        data.cidMapping = openType.GetCidMapping();
        data.widths = openType.GetWidths();
        data.verticalWidths = openType.GetVerticalWidths();
        data.flags = openType.GetDescriptorFlags();
        data.metrics = openType.GetDescriptorMetrics();
        return data;
    }

    static string SerializeWidths(PdfCidWidthsArtifact widths)
    {
        StringBuilder value = new StringBuilder("[");
        int index = 0;

        while (index < widths.GetEntryCount())
        {
            PdfCidWidthEntry entry = widths.GetEntry(index);
            value.Append(' ');
            value.Append(Invariant(entry.GetFirstCid()));
            value.Append(' ');

            if (entry.GetKind() == PdfCidWidthEntryKind.Range)
            {
                value.Append(Invariant(entry.GetLastCid()));
                value.Append(' ');
                value.Append(Invariant(entry.GetRangeWidth()));
            }
            else
            {
                value.Append('[');
                int[] arrayWidths = entry.CopyArrayWidths();
                int widthIndex = 0;

                while (widthIndex < arrayWidths.Length)
                {
                    if (widthIndex != 0)
                    {
                        value.Append(' ');
                    }

                    value.Append(Invariant(arrayWidths[widthIndex]));
                    widthIndex += 1;
                }

                value.Append(']');
            }

            index += 1;
        }

        value.Append(" ]");
        return value.ToString();
    }

    static string SerializeVerticalWidths(
        PdfCidVerticalWidthsArtifact widths)
    {
        StringBuilder value = new StringBuilder("[");
        int index = 0;

        while (index < widths.GetEntryCount())
        {
            PdfCidVerticalWidthEntry entry = widths.GetEntry(index);
            value.Append(' ');
            value.Append(Invariant(entry.GetFirstCid()));
            value.Append(' ');

            if (entry.GetKind() == PdfCidWidthEntryKind.Range)
            {
                value.Append(Invariant(entry.GetLastCid()));
                value.Append(' ');
                AppendVerticalMetric(value, entry.GetRangeMetric());
            }
            else
            {
                value.Append('[');
                int metricIndex = 0;

                while (metricIndex < entry.GetArrayMetricCount())
                {
                    if (metricIndex != 0)
                    {
                        value.Append(' ');
                    }

                    AppendVerticalMetric(
                        value,
                        entry.GetArrayMetric(metricIndex));
                    metricIndex += 1;
                }

                value.Append(']');
            }

            index += 1;
        }

        value.Append(" ]");
        return value.ToString();
    }

    static void AppendVerticalMetric(
        StringBuilder value,
        PdfCidVerticalMetric? metric)
    {
        value.Append(Invariant(metric!.GetDisplacementY()));
        value.Append(' ');
        value.Append(Invariant(metric.GetOriginX()));
        value.Append(' ');
        value.Append(Invariant(metric.GetOriginY()));
    }

    static byte[] CreateContent(
        byte[] characterCodes,
        string? actualText)
    {
        StringBuilder content = new StringBuilder();

        if (actualText != null)
        {
            content.Append("/Span << /ActualText <");
            AppendUtf16Be(content, actualText);
            content.Append("> >> BDC\n");
        }

        content.Append("BT\n/F1 24 Tf\n72 720 Td\n<");
        int index = 0;

        while (index < characterCodes.Length)
        {
            content.Append(characterCodes[index].ToString(
                "X2",
                CultureInfo.InvariantCulture));
            index += 1;
        }

        content.Append("> Tj\nET\n");

        if (actualText != null)
        {
            content.Append("EMC\n");
        }

        return Ascii(content.ToString());
    }

    static void AppendUtf16Be(StringBuilder content, string value)
    {
        content.Append("FEFF");
        byte[] bytes = Encoding.BigEndianUnicode.GetBytes(value);
        int index = 0;

        while (index < bytes.Length)
        {
            content.Append(bytes[index].ToString(
                "X2",
                CultureInfo.InvariantCulture));
            index += 1;
        }
    }

    static byte[] Ascii(string value)
    {
        return Encoding.ASCII.GetBytes(value);
    }

    static string Reference(int objectNumber)
    {
        return Invariant(objectNumber) + " 0 R";
    }

    static string Invariant(int value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    static string Invariant(double value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}

internal class MinimalPdfCompositeFontData
{
    internal string baseFontName = string.Empty;
    internal byte[] fontBytes = Array.Empty<byte>();
    internal PdfFontKind descendantKind;
    internal PdfCidMappingArtifact cidMapping = null!;
    internal PdfCidWidthsArtifact widths = null!;
    internal PdfCidVerticalWidthsArtifact? verticalWidths = null!;
    internal PdfFontDescriptorFlagsReport flags = null!;
    internal PdfFontDescriptorMetrics metrics = null!;
}

internal class MinimalPdfObjectStore
{
    List<byte[]> objects = new List<byte[]> { Array.Empty<byte>() };

    internal int Reserve()
    {
        objects.Add(Array.Empty<byte>());
        return objects.Count - 1;
    }

    internal int Add(byte[] value)
    {
        int objectNumber = Reserve();
        Set(objectNumber, value);
        return objectNumber;
    }

    internal int AddStream(string entries, byte[] data)
    {
        byte[] prefix = Encoding.ASCII.GetBytes(
            "<< /Length " + data.Length.ToString(
                CultureInfo.InvariantCulture) + entries +
            " >>\nstream\n");
        byte[] suffix = Encoding.ASCII.GetBytes("\nendstream");
        byte[] value = new byte[prefix.Length + data.Length + suffix.Length];
        Buffer.BlockCopy(prefix, 0, value, 0, prefix.Length);
        Buffer.BlockCopy(data, 0, value, prefix.Length, data.Length);
        Buffer.BlockCopy(
            suffix,
            0,
            value,
            prefix.Length + data.Length,
            suffix.Length);
        return Add(value);
    }

    internal void Set(int objectNumber, byte[] value)
    {
        if (objectNumber <= 0 || objectNumber >= objects.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(objectNumber));
        }

        objects[objectNumber] = value;
    }

    internal byte[] Build(int rootObject)
    {
        using MemoryStream output = new MemoryStream();
        Write(output, Encoding.ASCII.GetBytes("%PDF-1.7\n%"));
        Write(output, new byte[] { 0xE2, 0xE3, 0xCF, 0xD3, 0x0A });
        long[] offsets = new long[objects.Count];
        int objectNumber = 1;

        while (objectNumber < objects.Count)
        {
            offsets[objectNumber] = output.Position;
            WriteAscii(
                output,
                objectNumber.ToString(CultureInfo.InvariantCulture) +
                " 0 obj\n");
            Write(output, objects[objectNumber]);
            WriteAscii(output, "\nendobj\n");
            objectNumber += 1;
        }

        long xrefOffset = output.Position;
        WriteAscii(
            output,
            "xref\n0 " + objects.Count.ToString(
                CultureInfo.InvariantCulture) + "\n");
        WriteAscii(output, "0000000000 65535 f \n");
        objectNumber = 1;

        while (objectNumber < objects.Count)
        {
            WriteAscii(
                output,
                offsets[objectNumber].ToString(
                    "D10",
                    CultureInfo.InvariantCulture) + " 00000 n \n");
            objectNumber += 1;
        }

        WriteAscii(
            output,
            "trailer\n<< /Size " + objects.Count.ToString(
                CultureInfo.InvariantCulture) + " /Root " +
                rootObject.ToString(CultureInfo.InvariantCulture) +
                " 0 R >>\nstartxref\n" + xrefOffset.ToString(
                    CultureInfo.InvariantCulture) + "\n%%EOF\n");
        return output.ToArray();
    }

    static void WriteAscii(MemoryStream output, string value)
    {
        Write(output, Encoding.ASCII.GetBytes(value));
    }

    static void Write(MemoryStream output, byte[] value)
    {
        output.Write(value, 0, value.Length);
    }
}
