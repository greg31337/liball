using System.IO;

namespace odf;

/// <summary>
/// Writes standard OpenDocument XML parts from universal XML trees.
/// </summary>
public class OdfXmlWriter : IOdfXmlWriter
{
    /// <summary>
    /// Initializes an XML part writer.
    /// </summary>
    public OdfXmlWriter()
    {
    }

    /// <summary>
    /// Writes an XML part to a file-system path.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="path">The destination path.</param>
    /// <param name="options">The save options.</param>
    public void WriteXmlPath(
        OdfPart part,
        string path,
        OdfSaveOptions options)
    {
        byte[] data = WriteXmlBytes(part, options);
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// Writes an XML part to a caller-supplied stream.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="output">The writable destination stream.</param>
    /// <param name="options">The save and stream-ownership options.</param>
    public void WriteXmlStream(
        OdfPart part,
        Stream output,
        OdfSaveOptions options)
    {
        if (output == null || !output.CanWrite)
        {
            throw new OdfXmlException("The XML output stream is not writable.");
        }
        if (options == null)
        {
            throw new OdfXmlException("XML save options are required.");
        }
        if (options.LeaveOutputOpen)
        {
            WriteOpenStream(part, output, options);
            return;
        }
        using (output)
        {
            WriteOpenStream(part, output, options);
        }
    }

    /// <summary>
    /// Writes an XML part to a new byte array.
    /// </summary>
    /// <param name="part">The XML part to write.</param>
    /// <param name="options">The save options.</param>
    /// <returns>The complete XML bytes.</returns>
    public byte[] WriteXmlBytes(OdfPart part, OdfSaveOptions options)
    {
        if (part == null || options == null)
        {
            throw new OdfXmlException("An XML part and save options are required.");
        }
        if (options.Cancellation.IsCancellationRequested)
        {
            throw new OdfXmlException("XML writing was cancelled.");
        }
        OdfNode[] nodes = OdfDocumentModel.PreparePartNodes(part, options);
        return OdfTreeWriter.WriteBytes(nodes, options);
    }

    internal void WriteOpenStream(
        OdfPart part,
        Stream output,
        OdfSaveOptions options)
    {
        byte[] data = WriteXmlBytes(part, options);
        output.Write(data, 0, data.Length);
        output.Flush();
    }
}
