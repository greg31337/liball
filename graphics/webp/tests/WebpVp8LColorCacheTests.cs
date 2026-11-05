using webp;

namespace webp.Tests;

[TestFixture]
public class WebpVp8LColorCacheTests
{
    [Test]
    public void ParseColorCacheInfo_ReturnsAbsentWhenFlagIsZero()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(0, 1);

        WebpVp8LColorCacheParseResult result = WebpVp8LColorCache.ParseColorCacheInfo(new WebpVp8LBitReader(writer.ToArray()));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsPresent, Is.False);
        Assert.That(result.CacheBits, Is.EqualTo(0));
        Assert.That(result.CacheSize, Is.EqualTo(0));
    }

    [Test]
    public void ParseColorCacheInfo_ReadsPresentCacheBitsAndSize()
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits(4, 4);

        WebpVp8LColorCacheParseResult result = WebpVp8LColorCache.ParseColorCacheInfo(new WebpVp8LBitReader(writer.ToArray()));

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.IsPresent, Is.True);
        Assert.That(result.CacheBits, Is.EqualTo(4));
        Assert.That(result.CacheSize, Is.EqualTo(16));
    }

    [Test]
    public void ParseColorCacheInfo_RejectsInvalidBitCounts()
    {
        WebpVp8LColorCacheParseResult zero = WebpVp8LColorCache.ParseColorCacheInfo(NewPresentColorCacheReader(0));
        WebpVp8LColorCacheParseResult tooLarge = WebpVp8LColorCache.ParseColorCacheInfo(NewPresentColorCacheReader(12));

        Assert.That(zero.IsValid, Is.False);
        Assert.That(zero.Diagnostics[0].Message, Does.Contain("between 1 and 11"));
        Assert.That(tooLarge.IsValid, Is.False);
        Assert.That(tooLarge.Diagnostics[0].Message, Does.Contain("between 1 and 11"));
    }

    [Test]
    public void ParseColorCacheInfo_ReportsTruncatedCacheBitCount()
    {
        WebpVp8LBitReader reader = WebpVp8LBitReader.FromState(new byte[] { 0x80 }, new WebpVp8LHeader(), 7);

        WebpVp8LColorCacheParseResult result = WebpVp8LColorCache.ParseColorCacheInfo(reader);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Diagnostics[0].Message, Does.Contain("truncated"));
    }

    [Test]
    public void Insert_HashesArgbAndStoresColorCopy()
    {
        WebpVp8LColorCache cache = new WebpVp8LColorCache(3);
        WebpVp8LColor color = NewColor(10, 20, 30, 40);

        int index = cache.GetIndex(color);
        cache.Insert(color);
        color.Green = 99;
        WebpVp8LColor cached = cache.Lookup(index);

        Assert.That(index, Is.GreaterThanOrEqualTo(0));
        Assert.That(index, Is.LessThan(cache.Size));
        Assert.That(cache.ContainsIndex(index), Is.True);
        Assert.That(cached.ToArgb(), Is.EqualTo(0x280A141EU));
        Assert.That(cached, Is.Not.SameAs(color));
    }

    [Test]
    public void DecodeSymbol_MapsGreenColorCacheSymbolsToEntries()
    {
        WebpVp8LColorCache cache = new WebpVp8LColorCache(4);
        WebpVp8LColor color = NewColor(1, 2, 3, 4);
        cache.Insert(color);
        int index = cache.GetIndex(color);

        WebpVp8LColor decoded = cache.DecodeSymbol(280 + index);

        Assert.That(decoded.ToArgb(), Is.EqualTo(color.ToArgb()));
        Assert.That(decoded, Is.Not.SameAs(color));
    }

    [Test]
    public void Cache_RejectsInvalidBitsSymbolsAndUninitializedEntries()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpVp8LColorCache(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new WebpVp8LColorCache(12));

        WebpVp8LColorCache cache = new WebpVp8LColorCache(2);
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.DecodeSymbol(279));
        Assert.Throws<ArgumentOutOfRangeException>(() => cache.DecodeSymbol(284));
        Assert.Throws<ArgumentException>(() => cache.Lookup(0));
    }

    static WebpVp8LBitReader NewPresentColorCacheReader(int cacheBits)
    {
        WebpBitWriter writer = new WebpBitWriter(WebpBitOrder.LeastSignificantBitFirst);
        writer.WriteBits(1, 1);
        writer.WriteBits((uint)cacheBits, 4);
        return new WebpVp8LBitReader(writer.ToArray());
    }

    static WebpVp8LColor NewColor(byte red, byte green, byte blue, byte alpha)
    {
        WebpVp8LColor color = new WebpVp8LColor();
        color.Red = red;
        color.Green = green;
        color.Blue = blue;
        color.Alpha = alpha;
        return color;
    }
}
