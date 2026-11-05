using System;
using System.IO;
using System.Text;

namespace xml;

/// <summary>
/// Describes one discriminated XML input source.
/// </summary>
public class XmlInput
{
    /// <summary>
    /// The active input representation.
    /// </summary>
    public XmlInputKind Kind;
    /// <summary>
    /// The string input.
    /// </summary>
    public string? Text;
    /// <summary>
    /// The character-memory input.
    /// </summary>
    public ReadOnlyMemory<char> Characters;
    /// <summary>
    /// The byte-memory input.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes;
    /// <summary>
    /// The byte stream input.
    /// </summary>
    public Stream? ByteStream;
    /// <summary>
    /// The text reader input.
    /// </summary>
    public TextReader? TextReader;
    /// <summary>
    /// The base URI associated with the input.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The transport media type.
    /// </summary>
    public string? MediaType;
    /// <summary>
    /// The transport or caller-selected encoding name.
    /// </summary>
    public string? EncodingName;
    /// <summary>
    /// The caller-selected built-in encoding.
    /// </summary>
    public Encoding? Encoding;
    /// <summary>
    /// Whether the processor owns and closes the stream or reader.
    /// </summary>
    public bool OwnsResource;
}

