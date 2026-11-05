using System;

namespace tiff;

internal class TiffRawUnknownTagPreserver
{
    internal static TiffRawUnknownTagPreservationResult PreserveDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes)
    {
        return PreserveDirectory(directory, fileBytes, new TiffReadOptions());
    }

    internal static TiffRawUnknownTagPreservationResult PreserveDirectory(TiffDirectory directory, ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        if (directory == null)
        {
            TiffDiagnostics diagnostics = new TiffDiagnostics();
            diagnostics.AddError(TiffErrorCode.UnknownError, "A directory is required to preserve unknown tags.");
            return TiffRawUnknownTagPreservationResult.Create(new TiffTagValue[0], diagnostics);
        }

        return PreserveEntries(directory.Entries, fileBytes, options);
    }

    internal static TiffRawUnknownTagPreservationResult PreserveEntries(TiffIfdEntry[] entries, ReadOnlySpan<byte> fileBytes)
    {
        return PreserveEntries(entries, fileBytes, new TiffReadOptions());
    }

    internal static TiffRawUnknownTagPreservationResult PreserveEntries(TiffIfdEntry[] entries, ReadOnlySpan<byte> fileBytes, TiffReadOptions options)
    {
        TiffReadOptions actualOptions = TiffDirectoryReader.NormalizeOptions(options);
        TiffLimits limits = TiffDirectoryReader.GetLimits(actualOptions);
        TiffDiagnostics diagnostics = new TiffDiagnostics();
        TiffTagValue[] values = new TiffTagValue[0];

        if (!ShouldPreserveUnknownTags(actualOptions))
        {
            return TiffRawUnknownTagPreservationResult.Create(values, diagnostics);
        }

        if (entries == null)
        {
            return TiffRawUnknownTagPreservationResult.Create(values, diagnostics);
        }

        int index = 0;
        while (index < entries.Length)
        {
            if (entries[index] != null && IsUnknownTag(entries[index], actualOptions))
            {
                values = PreserveOneEntry(values, entries[index], fileBytes, limits, diagnostics);
            }

            index = index + 1;
        }

        return TiffRawUnknownTagPreservationResult.Create(values, diagnostics);
    }

    internal static TiffTagValue[] PreserveOneEntry(TiffTagValue[] values, TiffIfdEntry entry, ReadOnlySpan<byte> fileBytes, TiffLimits limits, TiffDiagnostics diagnostics)
    {
        if (!TiffDataTypeInfo.IsKnown(entry.DataType))
        {
            diagnostics.AddErrorForTag(TiffErrorCode.InvalidTagType, "Unknown tag uses an invalid TIFF data type.", TiffDiagnostic.NoImageIndex, entry.TagId);
            return values;
        }

        TiffTagValueLoadResult loadResult = TiffTagValueLoader.LoadBytes(entry, fileBytes, limits);
        diagnostics.Merge(loadResult.Diagnostics);
        if (loadResult.Success)
        {
            TiffTagValue value = TiffTagValue.CreateRaw(entry.TagId, entry.DataType, entry.Count, loadResult.Bytes);
            values = AppendValue(values, value);
        }

        return values;
    }

    internal static bool ShouldPreserveUnknownTags(TiffReadOptions options)
    {
        if (options == null)
        {
            return true;
        }

        return options.PreserveUnknownTags;
    }

    internal static bool IsUnknownTag(TiffIfdEntry entry, TiffReadOptions options)
    {
        if (entry == null)
        {
            return false;
        }

        TiffReadOptions actualOptions = TiffDirectoryReader.NormalizeOptions(options);
        TiffTagDefinition? definition = actualOptions.FindTagDefinition(entry.TagId);
        return definition == null;
    }

    internal static TiffTagValue[] AppendValue(TiffTagValue[] values, TiffTagValue value)
    {
        if (values == null)
        {
            TiffTagValue[] single = new TiffTagValue[1];
            single[0] = value.Copy();
            return single;
        }

        TiffTagValue[] copy = new TiffTagValue[values.Length + 1];
        int index = 0;
        while (index < values.Length)
        {
            copy[index] = values[index].Copy();
            index = index + 1;
        }

        copy[copy.Length - 1] = value.Copy();
        return copy;
    }
}
