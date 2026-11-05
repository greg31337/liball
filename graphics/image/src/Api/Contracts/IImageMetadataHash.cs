using System;
using System.IO;

namespace image;

/// <summary>Defines deterministic hashing operations for source bytes and extracted metadata.</summary>
public interface IImageMetadataHash
{
    /// <summary>Computes a SHA-256 hash for source bytes and metadata.</summary>
    /// <param name="sourceBytes">The source bytes, or null.</param>
    /// <param name="document">The metadata document, or null.</param>
    /// <returns>The lowercase hexadecimal digest.</returns>
    string ComputeSha256(byte[]? sourceBytes, ImageMetadataDocument? document);

    /// <summary>Computes a hash using a supported algorithm name.</summary>
    /// <param name="sourceBytes">The source bytes, or null.</param>
    /// <param name="document">The metadata document, or null.</param>
    /// <param name="algorithmName">The requested algorithm name, or null for SHA-256.</param>
    /// <returns>The lowercase hexadecimal digest.</returns>
    string ComputeHash(byte[]? sourceBytes, ImageMetadataDocument? document, string? algorithmName);

    /// <summary>Computes a selected metadata-only hash from a byte array.</summary>
    /// <param name="sourceBytes">The complete source bytes used to create <c>document</c>, or null.</param>
    /// <param name="document">The metadata document containing source ranges, or null.</param>
    /// <param name="options">The algorithm and metadata selection, or null for defaults.</param>
    /// <returns>The explicit hash outcome and digest.</returns>
    ImageMetadataHashResult ComputeBytes(byte[]? sourceBytes, ImageMetadataDocument? document, ImageMetadataHashOptions? options);

    /// <summary>Computes a selected metadata-only hash directly from a caller-owned byte span.</summary>
    /// <param name="sourceBytes">The complete source bytes used to create <c>document</c>.</param>
    /// <param name="document">The metadata document containing source ranges, or null.</param>
    /// <param name="options">The algorithm and metadata selection, or null for defaults.</param>
    /// <returns>The explicit hash outcome and digest.</returns>
    ImageMetadataHashResult ComputeSpan(ReadOnlySpan<byte> sourceBytes, ImageMetadataDocument? document, ImageMetadataHashOptions? options);

    /// <summary>Computes a selected metadata-only hash from the current position of a stream.</summary>
    /// <remarks>A seekable stream is restored to its original position. A non-seekable stream is consumed through the last selected range.</remarks>
    /// <param name="sourceStream">The source stream used to create <c>document</c>, or null.</param>
    /// <param name="document">The metadata document containing source ranges, or null.</param>
    /// <param name="options">The algorithm and metadata selection, or null for defaults.</param>
    /// <returns>The explicit hash outcome and digest.</returns>
    ImageMetadataHashResult ComputeStream(Stream? sourceStream, ImageMetadataDocument? document, ImageMetadataHashOptions? options);
}
