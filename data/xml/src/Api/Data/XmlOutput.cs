using System.IO;
using System.Text;

namespace xml;

/// <summary>
/// Describes one discriminated XML output destination.
/// </summary>
public class XmlOutput
{
    /// <summary>
    /// The active output representation.
    /// </summary>
    public XmlOutputKind Kind;
    /// <summary>
    /// The byte stream destination.
    /// </summary>
    public Stream? ByteStream;
    /// <summary>
    /// The text writer destination.
    /// </summary>
    public TextWriter? TextWriter;
    /// <summary>
    /// The byte sink destination.
    /// </summary>
    public IXmlByteSink? ByteSink;
    /// <summary>
    /// The destination base URI.
    /// </summary>
    public string? BaseUri;
    /// <summary>
    /// The destination media type.
    /// </summary>
    public string? MediaType;
    /// <summary>
    /// The requested encoding name.
    /// </summary>
    public string? EncodingName;
    /// <summary>
    /// The requested built-in encoding.
    /// </summary>
    public Encoding? Encoding;
    /// <summary>
    /// Whether the processor owns and closes the stream or writer.
    /// </summary>
    public bool OwnsResource;
}

