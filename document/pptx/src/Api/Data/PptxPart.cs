using System.Text;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents one named package part together with its content type, bytes, and relationships.
/// </summary>
public class PptxPart
{
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the content type associated with this object.
    /// </summary>
    public string ContentType;
    /// <summary>
    /// Contains the binary payload bytes. Callers should treat the array as mutable data.
    /// </summary>
    public byte[] Bytes;
    internal XmlDocument XmlDocument;
    /// <summary>
    /// Indicates whether the has xml document setting is enabled.
    /// </summary>
    public bool HasXmlDocument;
    /// <summary>
    /// Stores the relationships value associated with this object.
    /// </summary>
    public PptxRelationshipCollection Relationships;
    internal bool IsDirty;
    internal PptxPart(string partName, string contentType, byte[] bytes)
    {
        PartName = PptxUriCore.NormalizePartName(partName);
        ContentType = ValidateAndNormalizeContentType(contentType);
        Bytes = CopyBytes(bytes);
        XmlDocument = PptxXml.CreateDocument();
        HasXmlDocument = false;
        Relationships = new PptxRelationshipCollection(PartName);
        IsDirty = false;
    }
    internal static PptxPart Create(string partName, string contentType, byte[] bytes)
    {
        return new PptxPart(partName, contentType, bytes);
    }
    internal int ByteCount()
    {
        return Bytes.Length;
    }
    internal bool HasRelationships()
    {
        return Relationships.Count() > 0;
    }
    internal bool IsXmlContentType()
    {
        if (ContentType == PptxContentTypes.Xml)
        {
            return true;
        }

        if (ContentType.EndsWith("+xml", System.StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }
    internal byte[] GetBytes()
    {
        return CopyBytes(Bytes);
    }
    internal void SetBytes(byte[] bytes)
    {
        Bytes = CopyBytes(bytes);
        IsDirty = true;
    }
    internal void SetContentType(string contentType)
    {
        ContentType = ValidateAndNormalizeContentType(contentType);
        IsDirty = true;
    }
    internal void SetRelationships(PptxRelationshipCollection relationships)
    {
        if (relationships == null)
        {
            throw new System.ArgumentNullException("relationships");
        }

        Relationships = relationships;
        IsDirty = true;
    }

    internal XmlDocument LoadXmlDocument()
    {
        string xml = Encoding.UTF8.GetString(Bytes);
        XmlDocument document = PptxContentTypesParser.LoadXml(xml);
        XmlDocument = document;
        HasXmlDocument = true;
        return XmlDocument;
    }

    internal void SetXmlDocument(XmlDocument document)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        XmlDocument = document;
        HasXmlDocument = true;
        IsDirty = true;
    }

    internal void ClearXmlDocument()
    {
        XmlDocument = PptxXml.CreateDocument();
        HasXmlDocument = false;
        IsDirty = true;
    }

    internal void MarkDirty()
    {
        IsDirty = true;
    }

    internal void MarkClean()
    {
        IsDirty = false;
    }

    internal static byte[] CopyBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return new byte[0];
        }

        byte[] result = new byte[bytes.Length];
        System.Array.Copy(bytes, result, bytes.Length);
        return result;
    }

    internal static string ValidateAndNormalizeContentType(string contentType)
    {
        PptxContentTypeManifest manifest = new PptxContentTypeManifest();
        if (!PptxContentTypesParser.ValidateContentType(contentType, manifest))
        {
            throw new System.ArgumentException(manifest.Diagnostics.FormatDiagnostics());
        }

        return contentType;
    }
}
