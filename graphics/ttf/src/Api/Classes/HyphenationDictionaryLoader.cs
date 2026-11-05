namespace ttf;

/// <summary>Loads bounded TeX-style hyphenation dictionaries.</summary>
public class HyphenationDictionaryLoader : IHyphenationDictionaryLoader
{
    /// <summary>Creates a hyphenation dictionary loader.</summary>
    public HyphenationDictionaryLoader()
    {
    }

    /// <summary>Loads a dictionary from encoded bytes using default limits.</summary>
    /// <param name="data">The encoded dictionary bytes.</param>
    /// <returns>The parsed dictionary.</returns>
    public HyphenationDictionary LoadFromBytes(byte[] data)
    {
        return HyphenationDictionary.Load(data);
    }

    /// <summary>Loads a dictionary from encoded bytes using explicit limits.</summary>
    /// <param name="data">The encoded dictionary bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed dictionary.</returns>
    public HyphenationDictionary LoadFromBytesWithLimits(
        byte[] data,
        HyphenationLimits limits)
    {
        return HyphenationDictionary.Load(data, limits);
    }

    /// <summary>Loads a dictionary from a stream using default limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The parsed dictionary.</returns>
    public HyphenationDictionary LoadFromStream(System.IO.Stream stream)
    {
        return HyphenationDictionary.Load(stream);
    }

    /// <summary>Loads a dictionary from a stream using explicit limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed dictionary.</returns>
    public HyphenationDictionary LoadFromStreamWithLimits(
        System.IO.Stream stream,
        HyphenationLimits limits)
    {
        return HyphenationDictionary.Load(stream, limits);
    }
}
