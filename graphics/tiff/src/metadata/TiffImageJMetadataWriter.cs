using System;

namespace tiff;

internal class TiffImageJMetadataWriter
{
    internal static TiffImageJMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset)
    {
        return WriteClassic(destination, values, metadata, byteOrder, directoryOffset, nextIfdOffset, new TiffWriteOptions());
    }

    internal static TiffImageJMetadataWriteResult WriteClassic(Span<byte> destination, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffWriteOptions options)
    {
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        if (!ValidateClassicRequest(destination.Length, values, metadata, byteOrder, directoryOffset, nextIfdOffset, diagnostics))
        {
            return TiffImageJMetadataWriteResult.Create(directoryOffset, 0, diagnostics);
        }

        string description = CreateDescription(metadata);
        TiffImageDescriptionWriteResult writeResult = TiffImageDescriptionWriter.WriteClassic(destination, values, description, byteOrder, directoryOffset, nextIfdOffset, options);
        diagnostics.Merge(writeResult.Diagnostics);
        return TiffImageJMetadataWriteResult.Create(writeResult.DirectoryOffset, writeResult.BytesWritten, diagnostics);
    }

    internal static int CalculateClassicWriteByteCount(TiffTagValue[] values, TiffImageJMetadata metadata)
    {
        string description = CreateDescription(metadata);
        if (!TiffImageJMetadataReader.Parse(description, new byte[0]).HasImageJ)
        {
            return 0;
        }

        return TiffImageDescriptionWriter.CalculateClassicWriteByteCount(values, description);
    }

    internal static bool ValidateClassicRequest(int destinationLength, TiffTagValue[] values, TiffImageJMetadata metadata, TiffByteOrder byteOrder, ulong directoryOffset, ulong nextIfdOffset, TiffDiagnostics diagnostics)
    {
        if (metadata == null || !metadata.HasImageJ)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ writer requires ImageJ metadata.", TiffDiagnostic.NoImageIndex, TiffTagId.ImageDescription);
            return false;
        }

        string description = CreateDescription(metadata);
        if (!TiffImageJMetadataReader.Parse(description, new byte[0]).HasImageJ)
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagValue, "ImageJ writer requires an ImageJ= version entry.", TiffDiagnostic.NoImageIndex, TiffTagId.ImageDescription);
            return false;
        }

        return TiffImageDescriptionWriter.ValidateClassicRequest(destinationLength, values, description, byteOrder, directoryOffset, nextIfdOffset, diagnostics);
    }

    internal static TiffTagValue CreateValue(TiffImageJMetadata metadata)
    {
        return TiffTagValue.CreateAscii(TiffTagId.ImageDescription, CreateDescription(metadata));
    }

    internal static string CreateDescription(TiffImageJMetadata metadata)
    {
        if (metadata == null)
        {
            return "";
        }

        if (metadata.RawDescription.Length > 0 && TiffImageJMetadataReader.Parse(metadata.RawDescription, metadata.RawBytes).HasImageJ)
        {
            return metadata.RawDescription;
        }

        if (metadata.Entries != null && metadata.Entries.Length > 0)
        {
            return SerializeEntries(metadata.Entries);
        }

        return BuildDescriptionFromFields(metadata);
    }

    internal static string BuildDescriptionFromFields(TiffImageJMetadata metadata)
    {
        string version = metadata.Version;
        if (version == null || version.Length == 0)
        {
            version = "1.54";
        }

        string text = "";
        text = AppendLine(text, "ImageJ", version);
        text = AppendIntLine(text, "images", metadata.ImageCount);
        text = AppendIntLine(text, "channels", metadata.Channels);
        text = AppendIntLine(text, "slices", metadata.Slices);
        text = AppendIntLine(text, "frames", metadata.Frames);
        if (metadata.IsHyperstack)
        {
            text = AppendLine(text, "hyperstack", "true");
        }

        text = AppendStringLine(text, "mode", metadata.Mode);
        text = AppendStringLine(text, "unit", metadata.Unit);
        text = AppendStringLine(text, "spacing", metadata.Spacing);
        return text;
    }

    internal static string SerializeEntries(TiffImageJMetadataEntry[] entries)
    {
        string text = "";
        int index = 0;
        while (entries != null && index < entries.Length)
        {
            if (entries[index] != null && entries[index].Key.Length > 0)
            {
                text = AppendLine(text, entries[index].Key, entries[index].Value);
            }

            index = index + 1;
        }

        return text;
    }

    internal static string AppendIntLine(string text, string key, int value)
    {
        if (value <= 0)
        {
            return text;
        }

        return AppendLine(text, key, value.ToString());
    }

    internal static string AppendStringLine(string text, string key, string value)
    {
        string actualValue = TiffImageJMetadataEntry.NormalizeString(value);
        if (actualValue.Length == 0)
        {
            return text;
        }

        return AppendLine(text, key, actualValue);
    }

    internal static string AppendLine(string text, string key, string value)
    {
        return TiffImageJMetadataEntry.NormalizeString(text) + TiffImageJMetadataEntry.NormalizeString(key) + "=" + TiffImageJMetadataEntry.NormalizeString(value) + "\n";
    }
}
