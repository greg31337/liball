namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="FontLoader"/>.</summary>
public interface IFontLoader
{
    /// <summary>Loads a font package from a file.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    FontPackage LoadFromFile(string path, FontLoadOptions options);

    /// <summary>Loads a font package from a readable stream.</summary>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    FontPackage LoadFromStream(System.IO.Stream stream, FontLoadOptions options);

    /// <summary>Loads a font package after copying an array.</summary>
    /// <param name="data">The complete font container bytes.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    FontPackage LoadFromBytes(byte[] data, FontLoadOptions options);

    /// <summary>Loads a font package from read-only memory.</summary>
    /// <param name="data">The complete font container memory.</param>
    /// <param name="options">The loading policies, limits, and ownership choice.</param>
    /// <returns>The loaded font package.</returns>
    FontPackage LoadFromMemory(ReadOnlyMemory<byte> data, FontLoadOptions options);

    /// <summary>Loads a font package after copying a span.</summary>
    /// <param name="data">The complete font container span.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    FontPackage LoadFromSpan(ReadOnlySpan<byte> data, FontLoadOptions options);
}
