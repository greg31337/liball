using System.IO;

namespace odf;

/// <summary>
/// Reads standard OpenDocument XML parts with secure materialization.
/// </summary>
public class OdfXmlReader : IOdfXmlReader
{
    /// <summary>
    /// Initializes an XML part reader.
    /// </summary>
    public OdfXmlReader()
    {
    }

    /// <summary>
    /// Reads an XML part from a file-system path.
    /// </summary>
    /// <param name="path">The path to read.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlPath(
        string path,
        string partPath,
        OdfLoadOptions options)
    {
        CheckReadInputs(partPath, options);
        CheckCancellation(options);
        byte[] data = File.ReadAllBytes(path);
        CheckCancellation(options);
        return ReadXmlBytes(data, partPath, options);
    }

    /// <summary>
    /// Reads an XML part from a caller-supplied stream.
    /// </summary>
    /// <param name="input">The readable XML stream.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load and stream-ownership options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlStream(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        CheckReadInputs(partPath, options);
        if (input == null || !input.CanRead)
        {
            throw new OdfXmlException("The XML input stream is not readable.");
        }
        if (options.LeaveInputOpen)
        {
            return ReadOpenStream(input, partPath, options);
        }
        using (input)
        {
            return ReadOpenStream(input, partPath, options);
        }
    }

    /// <summary>
    /// Reads an XML part from complete input bytes.
    /// </summary>
    /// <param name="data">The complete XML bytes.</param>
    /// <param name="partPath">The package-relative part path, or an empty string for flat XML.</param>
    /// <param name="options">The load options.</param>
    /// <returns>The materialized standard XML part.</returns>
    public OdfPart ReadXmlBytes(
        byte[] data,
        string partPath,
        OdfLoadOptions options)
    {
        CheckReadInputs(partPath, options);
        if (data == null)
        {
            throw new OdfXmlException("XML input bytes are required.");
        }
        CheckCancellation(options);
        OdfNode[] nodes = OdfTreeBuilder.ReadBytes(data, partPath, options);
        OdfPart part = OdfDocumentModel.CreateStandalonePart(
            nodes,
            partPath,
            OdfMediaTypes.Xml);
        if (part.IsFlatDocument)
        {
            part.MediaType = RootMediaType(part.Root);
        }
        return part;
    }

    internal OdfPart ReadOpenStream(
        Stream input,
        string partPath,
        OdfLoadOptions options)
    {
        CheckCancellation(options);
        using (MemoryStream buffer = new MemoryStream())
        {
            input.CopyTo(buffer);
            CheckCancellation(options);
            return ReadXmlBytes(buffer.ToArray(), partPath, options);
        }
    }

    internal void CheckReadInputs(string partPath, OdfLoadOptions options)
    {
        if (partPath == null || options == null)
        {
            throw new OdfXmlException("A part path and load options are required.");
        }
    }

    internal void CheckCancellation(OdfLoadOptions options)
    {
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfXmlException("XML reading was cancelled.");
        }
    }

    internal string RootMediaType(OdfNode? root)
    {
        if (root == null)
        {
            return "";
        }
        foreach (OdfAttribute attribute in root.Attributes)
        {
            if (attribute.Name.NamespaceName == OdfNamespaces.Office
                && attribute.Name.LocalName == "mimetype")
            {
                return attribute.Value;
            }
        }
        return "";
    }
}
