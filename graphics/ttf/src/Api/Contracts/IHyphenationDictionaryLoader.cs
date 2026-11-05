namespace ttf;

/// <summary>Defines bounded hyphenation dictionary loading operations.</summary>
public interface IHyphenationDictionaryLoader
{
    /// <summary>Loads a dictionary from encoded bytes using default limits.</summary>
    /// <param name="data">The encoded dictionary bytes.</param>
    /// <returns>The parsed dictionary.</returns>
    HyphenationDictionary LoadFromBytes(byte[] data);

    /// <summary>Loads a dictionary from encoded bytes using explicit limits.</summary>
    /// <param name="data">The encoded dictionary bytes.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed dictionary.</returns>
    HyphenationDictionary LoadFromBytesWithLimits(
        byte[] data,
        HyphenationLimits limits);

    /// <summary>Loads a dictionary from a stream using default limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The parsed dictionary.</returns>
    HyphenationDictionary LoadFromStream(System.IO.Stream stream);

    /// <summary>Loads a dictionary from a stream using explicit limits.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>The parsed dictionary.</returns>
    HyphenationDictionary LoadFromStreamWithLimits(
        System.IO.Stream stream,
        HyphenationLimits limits);
}
