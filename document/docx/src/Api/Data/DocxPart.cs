using System.IO.Compression;
using System.Text;

namespace docx;

/// <summary>
/// Represents one XML or binary part in a DOCX package.
/// </summary>
public class DocxPart
{
    /// <summary>
    /// The normalized absolute package part name, such as <c>/word/document.xml</c>.
    /// </summary>
    public string PartName;
    /// <summary>
    /// The MIME content type associated with the part.
    /// </summary>
    public string ContentType;
    internal byte[] Bytes;
    /// <summary>
    /// Whether this part stores OPC relationships rather than document content.
    /// </summary>
    public bool IsRelationshipPart;
    /// <summary>
    /// Whether the part bytes are deferred until text or binary content is first requested.
    /// </summary>
    public bool IsLazy;
    internal byte[] PackageBytes;
    internal string ZipEntryName;
    internal int LoadCount;

    /// <summary>
    /// Creates a part instance.
    /// </summary>
    /// <param name="partName">The absolute package part name.</param>
    /// <param name="contentType">The MIME content type for the part or binary data.</param>
    /// <param name="bytes">The binary data to store. The data is copied when required by the operation.</param>
    /// <param name="isRelationshipPart">The is relationship part used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxPart(string partName, string contentType, byte[] bytes, bool isRelationshipPart)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        this.PartName = DocxPartName.Normalize(partName);
        this.ContentType = contentType;
        this.Bytes = CopyBytes(bytes);
        this.IsRelationshipPart = isRelationshipPart;
        this.IsLazy = false;
        this.PackageBytes = new byte[0];
        this.ZipEntryName = "";
        this.LoadCount = 0;
    }

    internal static DocxPart CreateLazy(string partName, string contentType, byte[] packageBytes, string zipEntryName, bool isRelationshipPart)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        if (packageBytes == null)
        {
            throw new ArgumentNullException("packageBytes");
        }

        if (zipEntryName == null)
        {
            throw new ArgumentNullException("zipEntryName");
        }

        DocxPart part = new DocxPart(partName, contentType, new byte[0], isRelationshipPart);
        part.IsLazy = true;
        part.PackageBytes = packageBytes;
        part.ZipEntryName = zipEntryName;
        return part;
    }

    internal string GetText()
    {
        LoadBytesIfNeeded();
        return Encoding.UTF8.GetString(this.Bytes);
    }

    internal void ReplaceText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ReplaceBytes(Encoding.UTF8.GetBytes(text));
    }

    internal void ReplaceBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        this.Bytes = CopyBytes(bytes);
        this.IsLazy = false;
        this.PackageBytes = new byte[0];
        this.ZipEntryName = "";
    }

    internal void SetContentType(string contentType)
    {
        if (contentType == null)
        {
            throw new ArgumentNullException("contentType");
        }

        this.ContentType = contentType;
    }

    internal byte[] GetBytes()
    {
        LoadBytesIfNeeded();
        return CopyBytes(this.Bytes);
    }

    internal DocxPart CreateDiagnosticCopy()
    {
        if (this.IsLazy)
        {
            return CreateLazy(
                this.PartName,
                this.ContentType,
                this.PackageBytes,
                this.ZipEntryName,
                this.IsRelationshipPart);
        }

        return new DocxPart(this.PartName, this.ContentType, this.Bytes, this.IsRelationshipPart);
    }

    internal void LoadBytesIfNeeded()
    {
        if (!this.IsLazy)
        {
            return;
        }

        using (MemoryStream stream = new MemoryStream(this.PackageBytes))
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName == this.ZipEntryName)
                    {
                        this.Bytes = DocxPackage.ReadZipEntryBytes(entry);
                        this.IsLazy = false;
                        this.PackageBytes = new byte[0];
                        this.ZipEntryName = "";
                        this.LoadCount = this.LoadCount + 1;
                        return;
                    }
                }
            }
        }

        throw new InvalidDataException("Lazy package entry was not found.");
    }

    internal static byte[] CopyBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException("bytes");
        }

        byte[] copy = new byte[bytes.Length];

        for (int index = 0; index < bytes.Length; index = index + 1)
        {
            copy[index] = bytes[index];
        }

        return copy;
    }
}
