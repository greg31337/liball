using System.IO;

namespace png;

/// <summary>Default service for metadata-only PNG identification.</summary>
public class PngIdentifierService : IPngIdentifier
{
    /// <summary>Creates an independent PNG identifier.</summary>
    public PngIdentifierService()
    {
    }

    /// <summary>Identifies PNG bytes using default options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <returns>The identification result.</returns>
    public PngIdentifyResult IdentifyBytes(byte[] data)
    {
        return Png.IdentifyBytes(data);
    }

    /// <summary>Identifies PNG bytes using explicit options.</summary>
    /// <param name="data">The PNG datastream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    public PngIdentifyResult IdentifyBytesWithOptions(byte[] data, PngDecoderOptions options)
    {
        return Png.IdentifyBytes(data, options);
    }

    /// <summary>Identifies PNG bytes beginning at an offset.</summary>
    /// <param name="data">The byte array containing the PNG datastream.</param>
    /// <param name="offset">The zero-based signature offset.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    public PngIdentifyResult IdentifyBytesAtOffset(byte[] data, int offset, PngDecoderOptions options)
    {
        return Png.IdentifyBytes(data, offset, options);
    }

    /// <summary>Identifies PNG data from a stream using default options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <returns>The identification result.</returns>
    public PngIdentifyResult IdentifyStream(Stream stream)
    {
        return Png.IdentifyStream(stream);
    }

    /// <summary>Identifies PNG data from a stream using explicit options.</summary>
    /// <param name="stream">The readable source stream.</param>
    /// <param name="options">The decoding options and safety limits.</param>
    /// <returns>The identification result.</returns>
    public PngIdentifyResult IdentifyStreamWithOptions(Stream stream, PngDecoderOptions options)
    {
        return Png.IdentifyStream(stream, options);
    }
}
