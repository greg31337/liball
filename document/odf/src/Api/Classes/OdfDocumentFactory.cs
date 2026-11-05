using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

/// <summary>
/// Creates editable OpenDocument 1.4 skeleton documents.
/// </summary>
public class OdfDocumentFactory : IOdfDocumentFactory
{
    /// <summary>
    /// Initializes a document factory.
    /// </summary>
    public OdfDocumentFactory()
    {
    }

    /// <summary>
    /// Creates a minimal package document for a document or template kind.
    /// </summary>
    /// <param name="kind">The document or template kind to create.</param>
    /// <returns>The new editable package document.</returns>
    /// <exception cref="OdfDocumentException">
    /// Thrown when the kind is not a recognized document kind.
    /// </exception>
    public OdfDocument CreatePackageDocument(OdfDocumentKind kind)
    {
        string mediaType = MediaTypeForKind(kind);
        byte[] packageData = CreatePackageBytes(kind, mediaType);
        OdfDocumentReader reader = new OdfDocumentReader();
        return reader.ReadDocumentBytes(
            packageData,
            "skeleton.odf",
            new OdfLoadOptions());
    }

    /// <summary>
    /// Creates a minimal flat document for a document or template kind.
    /// </summary>
    /// <param name="kind">The document or template kind to create.</param>
    /// <returns>The new editable flat document.</returns>
    /// <exception cref="OdfDocumentException">
    /// Thrown when the kind is not a recognized document kind.
    /// </exception>
    public OdfDocument CreateFlatDocument(OdfDocumentKind kind)
    {
        string mediaType = MediaTypeForKind(kind);
        string xml = FlatXml(kind, mediaType);
        OdfDocumentReader reader = new OdfDocumentReader();
        return reader.ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "skeleton.xml",
            new OdfLoadOptions());
    }

    internal byte[] CreatePackageBytes(
        OdfDocumentKind kind,
        string mediaType)
    {
        string contentXml = PackageContentXml(kind);
        string manifestXml = ManifestXml(mediaType);
        using (MemoryStream output = new MemoryStream())
        {
            using (ZipArchive archive = new ZipArchive(
                output,
                ZipArchiveMode.Create,
                true))
            {
                WritePackageEntry(
                    archive,
                    OdfPartNames.Mimetype,
                    Encoding.UTF8.GetBytes(mediaType),
                    CompressionLevel.NoCompression);
                WritePackageEntry(
                    archive,
                    OdfPartNames.Content,
                    Encoding.UTF8.GetBytes(contentXml),
                    CompressionLevel.Optimal);
                WritePackageEntry(
                    archive,
                    OdfPartNames.Manifest,
                    Encoding.UTF8.GetBytes(manifestXml),
                    CompressionLevel.Optimal);
            }
            return output.ToArray();
        }
    }

    internal void WritePackageEntry(
        ZipArchive archive,
        string path,
        byte[] data,
        CompressionLevel compression)
    {
        ZipArchiveEntry entry = archive.CreateEntry(path, compression);
        entry.LastWriteTime = new DateTimeOffset(
            1980,
            1,
            1,
            0,
            0,
            0,
            TimeSpan.Zero);
        using (Stream output = entry.Open())
        {
            output.Write(data, 0, data.Length);
        }
    }

    internal string PackageContentXml(OdfDocumentKind kind)
    {
        if (IsFormulaKind(kind))
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<math:math xmlns:math=\""
                + OdfNamespaces.Math
                + "\"/>";
        }
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<office:document-content xmlns:office=\""
            + OdfNamespaces.Office
            + "\""
            + AdditionalNamespaceDeclarations(kind)
            + " office:version=\"1.4\"><office:body>"
            + BodyXml(kind)
            + "</office:body></office:document-content>";
    }

    internal string FlatXml(
        OdfDocumentKind kind,
        string mediaType)
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<office:document xmlns:office=\""
            + OdfNamespaces.Office
            + "\""
            + AdditionalNamespaceDeclarations(kind)
            + " office:version=\"1.4\" office:mimetype=\""
            + mediaType
            + "\"><office:body>"
            + BodyXml(kind)
            + "</office:body></office:document>";
    }

    internal string BodyXml(OdfDocumentKind kind)
    {
        if (IsTextKind(kind) || IsFormulaKind(kind))
        {
            return "<office:text/>";
        }
        if (kind == OdfDocumentKind.Spreadsheet
            || kind == OdfDocumentKind.SpreadsheetTemplate)
        {
            return "<office:spreadsheet/>";
        }
        if (kind == OdfDocumentKind.Presentation
            || kind == OdfDocumentKind.PresentationTemplate)
        {
            return "<office:presentation/>";
        }
        if (kind == OdfDocumentKind.Drawing
            || kind == OdfDocumentKind.DrawingTemplate)
        {
            return "<office:drawing/>";
        }
        if (kind == OdfDocumentKind.Chart
            || kind == OdfDocumentKind.ChartTemplate)
        {
            return "<office:chart><chart:chart chart:class=\"chart:bar\">"
                + "<chart:plot-area/></chart:chart></office:chart>";
        }
        if (kind == OdfDocumentKind.Image
            || kind == OdfDocumentKind.ImageTemplate)
        {
            return "<office:image><draw:frame/></office:image>";
        }
        if (kind == OdfDocumentKind.Database)
        {
            return "<office:database/>";
        }
        throw new OdfDocumentException(
            "A recognized document or template kind is required.");
    }

    internal string AdditionalNamespaceDeclarations(OdfDocumentKind kind)
    {
        if (kind == OdfDocumentKind.Chart
            || kind == OdfDocumentKind.ChartTemplate)
        {
            return " xmlns:chart=\"" + OdfNamespaces.Chart + "\"";
        }
        if (kind == OdfDocumentKind.Image
            || kind == OdfDocumentKind.ImageTemplate)
        {
            return " xmlns:draw=\"" + OdfNamespaces.Drawing + "\"";
        }
        return "";
    }

    internal string ManifestXml(string mediaType)
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<manifest:manifest xmlns:manifest=\""
            + OdfNamespaces.Manifest
            + "\" manifest:version=\"1.4\">"
            + "<manifest:file-entry manifest:full-path=\"/\" "
            + "manifest:media-type=\""
            + mediaType
            + "\" manifest:version=\"1.4\"/>"
            + "<manifest:file-entry manifest:full-path=\"content.xml\" "
            + "manifest:media-type=\"text/xml\"/>"
            + "</manifest:manifest>";
    }

    internal string MediaTypeForKind(OdfDocumentKind kind)
    {
        if (kind == OdfDocumentKind.Text)
        {
            return OdfMediaTypes.Text;
        }
        if (kind == OdfDocumentKind.TextTemplate)
        {
            return OdfMediaTypes.TextTemplate;
        }
        if (kind == OdfDocumentKind.TextMaster)
        {
            return OdfMediaTypes.TextMaster;
        }
        if (kind == OdfDocumentKind.TextMasterTemplate)
        {
            return OdfMediaTypes.TextMasterTemplate;
        }
        if (kind == OdfDocumentKind.Spreadsheet)
        {
            return OdfMediaTypes.Spreadsheet;
        }
        if (kind == OdfDocumentKind.SpreadsheetTemplate)
        {
            return OdfMediaTypes.SpreadsheetTemplate;
        }
        if (kind == OdfDocumentKind.Presentation)
        {
            return OdfMediaTypes.Presentation;
        }
        if (kind == OdfDocumentKind.PresentationTemplate)
        {
            return OdfMediaTypes.PresentationTemplate;
        }
        if (kind == OdfDocumentKind.Drawing)
        {
            return OdfMediaTypes.Graphics;
        }
        if (kind == OdfDocumentKind.DrawingTemplate)
        {
            return OdfMediaTypes.GraphicsTemplate;
        }
        if (kind == OdfDocumentKind.Chart)
        {
            return OdfMediaTypes.Chart;
        }
        if (kind == OdfDocumentKind.ChartTemplate)
        {
            return OdfMediaTypes.ChartTemplate;
        }
        if (kind == OdfDocumentKind.Formula)
        {
            return OdfMediaTypes.Formula;
        }
        if (kind == OdfDocumentKind.FormulaTemplate)
        {
            return OdfMediaTypes.FormulaTemplate;
        }
        if (kind == OdfDocumentKind.Image)
        {
            return OdfMediaTypes.Image;
        }
        if (kind == OdfDocumentKind.ImageTemplate)
        {
            return OdfMediaTypes.ImageTemplate;
        }
        if (kind == OdfDocumentKind.Database)
        {
            return OdfMediaTypes.Base;
        }
        throw new OdfDocumentException(
            "A recognized document or template kind is required.");
    }

    internal bool IsTextKind(OdfDocumentKind kind)
    {
        return kind == OdfDocumentKind.Text
            || kind == OdfDocumentKind.TextTemplate
            || kind == OdfDocumentKind.TextMaster
            || kind == OdfDocumentKind.TextMasterTemplate;
    }

    internal bool IsFormulaKind(OdfDocumentKind kind)
    {
        return kind == OdfDocumentKind.Formula
            || kind == OdfDocumentKind.FormulaTemplate;
    }
}
