namespace svg;

/// <summary>Defines the public operations exposed by <see cref="SvgOpenTypeReader"/>.</summary>
public interface ISvgOpenTypeReader
{
    /// <summary>Creates an empty font database with explicit limits.</summary>
    /// <param name="limits">The database font count and byte limits.</param>
    /// <returns>A new empty font database.</returns>
    SvgFontDatabase CreateFontDatabaseWithLimits(SvgFontLimits limits);
    /// <summary>Reads caller-owned SFNT or web-font bytes using secure bounded defaults.</summary>
    /// <param name="bytes">The complete SFNT, WOFF, or WOFF2 font bytes.</param>
    /// <returns>The completed or failed font read.</returns>
    SvgOpenTypeReadResult ReadWithBytes(byte[] bytes);
    /// <summary>Reads SFNT or web-font bytes with explicit limits, validation, and ownership.</summary>
    /// <param name="bytes">The complete SFNT, WOFF, or WOFF2 font bytes.</param>
    /// <param name="options">The bounded read options.</param>
    /// <returns>The completed or failed font read.</returns>
    SvgOpenTypeReadResult ReadWithBytesAndOptions(byte[] bytes, SvgOpenTypeReadOptions? options);
}
