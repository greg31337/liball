using System;

namespace webp;

internal class WebpVp8LColorCache
{
    internal int CacheBits;
    internal int Size;
    internal WebpVp8LColor[] Entries;
    internal bool[] HasEntry;

    internal WebpVp8LColorCache(int cacheBits)
    {
        ValidateCacheBits(cacheBits);

        CacheBits = cacheBits;
        Size = 1 << cacheBits;
        Entries = new WebpVp8LColor[Size];
        HasEntry = new bool[Size];
    }

    internal static WebpVp8LColorCacheParseResult ParseColorCacheInfo(WebpVp8LBitReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        WebpVp8LColorCacheParseResult result = new WebpVp8LColorCacheParseResult();
        if (!reader.HasBits(1))
        {
            AddDiagnostic(result, "VP8L bitstream is truncated before the color cache flag");
            Finish(result);
            return result;
        }

        if (reader.ReadBits(1) == 0)
        {
            Finish(result);
            return result;
        }

        result.IsPresent = true;
        if (!reader.HasBits(4))
        {
            AddDiagnostic(result, "VP8L bitstream is truncated before the color cache bit count");
            Finish(result);
            return result;
        }

        result.CacheBits = (int)reader.ReadBits(4);
        if (result.CacheBits < WebpConstants.Vp8LMinColorCacheBits || result.CacheBits > WebpConstants.Vp8LMaxColorCacheBits)
        {
            AddDiagnostic(result, "VP8L color cache bits must be between 1 and 11");
            Finish(result);
            return result;
        }

        result.CacheSize = 1 << result.CacheBits;
        Finish(result);
        return result;
    }

    internal int GetIndex(WebpVp8LColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        ulong wideProduct = (ulong)color.ToArgb() * 0x1E35A7BDUL;
        uint product = (uint)(wideProduct & uint.MaxValue);
        return (int)(product >> (32 - CacheBits));
    }

    internal void Insert(WebpVp8LColor color)
    {
        ArgumentNullException.ThrowIfNull(color);

        int index = GetIndex(color);
        Entries[index] = color.Copy();
        HasEntry[index] = true;
    }

    internal WebpVp8LColor Lookup(int index)
    {
        ValidateIndex(index);
        if (!HasEntry[index])
        {
            throw new ArgumentException("VP8L color cache entry has not been initialized", "index");
        }

        return Entries[index].Copy();
    }

    internal WebpVp8LColor DecodeSymbol(int greenSymbol)
    {
        if (greenSymbol < 280)
        {
            throw new ArgumentOutOfRangeException("greenSymbol");
        }

        int index = greenSymbol - 280;
        return Lookup(index);
    }

    internal bool ContainsIndex(int index)
    {
        ValidateIndex(index);
        return HasEntry[index];
    }

    internal static void ValidateCacheBits(int cacheBits)
    {
        if (cacheBits < WebpConstants.Vp8LMinColorCacheBits || cacheBits > WebpConstants.Vp8LMaxColorCacheBits)
        {
            throw new ArgumentOutOfRangeException("cacheBits");
        }
    }

    internal void ValidateIndex(int index)
    {
        if (index < 0 || index >= Size)
        {
            throw new ArgumentOutOfRangeException("index");
        }
    }

    internal static void AddDiagnostic(WebpVp8LColorCacheParseResult result, string message)
    {
        result.Diagnostics.Add(new WebpDiagnostic(WebpDiagnosticSeverity.Error, -1, WebpConstants.FourCcVp8LString, message));
    }

    internal static void Finish(WebpVp8LColorCacheParseResult result)
    {
        result.IsValid = !result.HasErrors();
    }
}
