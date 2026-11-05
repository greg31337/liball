using System.IO;

namespace tiff;

internal class TiffLazyTagValue
{
    internal TiffIfdEntry Entry;
    internal byte[]? FileBytes;
    internal Stream? Stream;
    internal TiffLimits Limits;
    internal bool UsesStream;
    internal bool Loaded;
    internal TiffTagValueLoadResult CachedResult;

    internal TiffLazyTagValue(TiffIfdEntry entry, byte[] fileBytes, TiffLimits limits)
    {
        Entry = CopyEntry(entry);
        FileBytes = fileBytes;
        Stream = null;
        Limits = NormalizeLimits(limits);
        UsesStream = false;
        Loaded = false;
        CachedResult = new TiffTagValueLoadResult();
    }

    internal static TiffLazyTagValue CreateFromStream(TiffIfdEntry entry, Stream stream, TiffLimits limits)
    {
        TiffLazyTagValue value = new TiffLazyTagValue(entry, new byte[0], limits);
        value.FileBytes = null;
        value.Stream = stream;
        value.UsesStream = true;
        return value;
    }

    internal bool IsMaterialized()
    {
        return Loaded;
    }

    internal bool IsExternal()
    {
        return Entry.HasExternalValue();
    }

    internal TiffTagValueLoadResult GetBytes()
    {
        if (Loaded)
        {
            return CachedResult.Copy();
        }

        if (UsesStream)
        {
            if (Stream == null)
            {
                TiffDiagnostics diagnostics = new TiffDiagnostics();
                diagnostics.AddError(TiffErrorCode.UnsupportedStream, "A stream is required to load an external tag value.");
                CachedResult = TiffTagValueLoadResult.Create(new byte[0], false, diagnostics);
            }
            else
            {
                CachedResult = TiffTagValueLoader.LoadBytes(Entry, Stream, Limits);
            }
        }
        else
        {
            if (FileBytes == null)
            {
                CachedResult = TiffTagValueLoader.LoadBytes(Entry, new byte[0], Limits);
            }
            else
            {
                CachedResult = TiffTagValueLoader.LoadBytes(Entry, FileBytes, Limits);
            }
        }

        Loaded = true;
        return CachedResult.Copy();
    }

    internal static bool ShouldUseLazyLoading(TiffIfdEntry entry, long threshold)
    {
        if (entry == null)
        {
            return false;
        }

        if (!entry.HasExternalValue())
        {
            return false;
        }

        if (threshold < 0)
        {
            return false;
        }

        return entry.ValueByteCount > threshold;
    }

    internal static TiffIfdEntry CopyEntry(TiffIfdEntry entry)
    {
        if (entry == null)
        {
            return new TiffIfdEntry();
        }

        return entry.Copy();
    }

    internal static TiffLimits NormalizeLimits(TiffLimits limits)
    {
        if (limits == null)
        {
            return new TiffLimits();
        }

        return limits;
    }
}
