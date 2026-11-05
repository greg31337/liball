using System;
namespace token;

/// <summary>Provides foundational token serialization and format-detection operations.</summary>
public interface ITokenCodecOperations
{
    /// <summary>Sorts CBOR map entries into deterministic encoding order.</summary>
    /// <param name="entries">The entries to sort.</param>
    /// <returns>The sorted entries, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>entries</c> is null.</exception>
    CborMapSortResult SortCborMap(CborMapEntry[] entries);

    /// <summary>Reads the first strict PEM block from text.</summary>
    /// <param name="text">The PEM text.</param>
    /// <returns>The decoded block, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    PemReadResult ReadPemBlock(string text);

    /// <summary>Reads a size-limited COSE_Key value.</summary>
    /// <param name="cbor">The encoded COSE_Key bytes.</param>
    /// <param name="policy">The input size and CBOR depth limits.</param>
    /// <returns>The decoded key, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    CoseKeyResult ReadCoseKey(byte[] cbor, TokenPolicy policy);

    /// <summary>Scans the boundaries of a compact JOSE serialization.</summary>
    /// <param name="text">The compact token text.</param>
    /// <param name="policy">The input size limits.</param>
    /// <returns>The segment boundaries, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    JoseCompactScanResult ScanJoseCompact(string text, TokenPolicy policy);

    /// <summary>Parses and decodes a compact JOSE serialization.</summary>
    /// <param name="text">The compact token text.</param>
    /// <param name="policy">The input size limits.</param>
    /// <returns>The parsed serialization, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    JoseCompactParseResult ParseJoseCompact(string text, TokenPolicy policy);

    /// <summary>Detects a text token representation without authenticating it.</summary>
    /// <param name="text">The token text.</param>
    /// <param name="policy">The input size and nesting limits.</param>
    /// <returns>The detected token kind, or stable failure details.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    TokenKindResult DetectTextToken(string text, TokenPolicy policy);
}
