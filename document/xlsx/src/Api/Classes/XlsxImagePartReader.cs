using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml;

namespace xlsx;
/// <summary>
/// Reads image part data from supported byte arrays, streams, files, or XML text without taking ownership of caller-provided streams.
/// </summary>
/// <remarks>
/// Readers validate the input and attach diagnostics to the returned model or result. Caller-provided streams remain the caller's responsibility unless a returned streaming cursor documents a close operation.
/// </remarks>
public class XlsxImagePartReader : IXlsxImagePartReader
{
    /// <summary>
    /// Initializes a new image part reader with its default state.
    /// </summary>
    public XlsxImagePartReader()
    {
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The image part produced by the operation.</returns>
    public XlsxImagePart ReadBytes(string partPath, byte[] bytes)
    {
        return ReadBytesWithContentType(partPath, "", bytes);
    }

    /// <summary>
    /// Parses UTF-8 OOXML bytes and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "bytes">The complete OOXML package bytes to read.</param>
    /// <returns>The image part produced by the operation.</returns>
    public XlsxImagePart ReadBytesWithContentType(string partPath, string contentType, byte[] bytes)
    {
        XlsxImagePart part = new XlsxImagePart();
        ApplyImageMetadata(part, partPath, contentType, bytes);
        return part;
    }

    /// <summary>
    /// Parses OOXML from the current stream position and returns a typed model with validation diagnostics.
    /// </summary>
    /// <param name = "partPath">The part path value used by the operation.</param>
    /// <param name = "contentType">The MIME content type recorded for the package part.</param>
    /// <param name = "stream">The readable input stream. The caller retains ownership unless a returned streaming cursor is explicitly closed.</param>
    /// <returns>The image part produced by the operation.</returns>
    public XlsxImagePart ReadStream(string partPath, string contentType, Stream stream)
    {
        if (stream == null)
        {
            XlsxImagePart part = new XlsxImagePart();
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.streamNull", "Image stream must not be null."));
            return part;
        }

        using (MemoryStream buffer = new MemoryStream())
        {
            stream.CopyTo(buffer);
            return ReadBytesWithContentType(partPath, contentType, buffer.ToArray());
        }
    }

    internal static void ApplyImageMetadata(XlsxImagePart part, string partPath, string contentType, byte[] bytes)
    {
        XlsxPackagePathResult normalized = XlsxPackagePath.NormalizePartPath(partPath);
        foreach (XlsxDiagnostic diagnostic in normalized.Validation.Diagnostics)
        {
            diagnostic.SetLocation(partPath, "", "");
            part.AddDiagnostic(diagnostic);
        }

        if (normalized.Validation.IsValid)
        {
            part.PartPath = normalized.Path;
            part.Extension = GetExtensionFromPartPath(normalized.Path);
        }

        if (bytes == null)
        {
            part.Data = new byte[0];
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.dataNull", "Image data must not be null."));
        }
        else
        {
            part.Data = new byte[bytes.Length];
            Array.Copy(bytes, part.Data, bytes.Length);
            if (bytes.Length == 0)
            {
                part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.dataEmpty", "Image data must not be empty."));
            }
        }

        string detectedFormat = DetectImageFormat(part.Data);
        string contentFormat = GetImageFormatFromContentType(contentType);
        if (contentType != null && contentType.Length > 0 && contentFormat.Length == 0)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.contentTypeUnsupported", "Image content type is not supported."));
        }

        if (detectedFormat.Length == 0 && part.Data.Length > 0)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.formatUnsupported", "Image data format is not supported."));
        }

        if (detectedFormat.Length > 0 && contentFormat.Length > 0 && detectedFormat != contentFormat)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.contentTypeMismatch", "Image content type does not match the detected image data format."));
        }

        if (contentFormat.Length > 0)
        {
            part.Format = contentFormat;
        }
        else
        {
            part.Format = detectedFormat;
        }

        if (contentType != null && contentType.Length > 0)
        {
            part.ContentType = contentType;
        }
        else
        {
            part.ContentType = GetContentTypeForFormat(part.Format);
        }

        if (part.Extension == "")
        {
            part.Extension = GetExtensionForFormat(part.Format);
        }

        string extensionFormat = GetImageFormatFromExtension(part.Extension);
        if (extensionFormat.Length > 0 && part.Format.Length > 0 && extensionFormat != part.Format)
        {
            part.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "image.extensionMismatch", "Image part extension does not match the detected image data format."));
        }
    }

    internal static string DetectImageFormat(byte[] bytes)
    {
        if (bytes == null || bytes.Length == 0)
        {
            return "";
        }

        if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 && bytes[4] == 0x0D && bytes[5] == 0x0A && bytes[6] == 0x1A && bytes[7] == 0x0A)
        {
            return "png";
        }

        if (bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
        {
            return "jpeg";
        }

        if (bytes.Length >= 6 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38 && (bytes[4] == 0x37 || bytes[4] == 0x39) && bytes[5] == 0x61)
        {
            return "gif";
        }

        if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
        {
            return "bmp";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00)
        {
            return "tiff";
        }

        if (bytes.Length >= 4 && bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A)
        {
            return "tiff";
        }

        if (bytes.Length >= 12 && bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 && bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
        {
            return "webp";
        }

        if (bytes.Length >= 44 && bytes[0] == 0x01 && bytes[1] == 0x00 && bytes[2] == 0x00 && bytes[3] == 0x00 && bytes[40] == 0x20 && bytes[41] == 0x45 && bytes[42] == 0x4D && bytes[43] == 0x46)
        {
            return "emf";
        }

        if (bytes.Length >= 4 && ((bytes[0] == 0xD7 && bytes[1] == 0xCD && bytes[2] == 0xC6 && bytes[3] == 0x9A) || ((bytes[0] == 0x01 || bytes[0] == 0x02) && bytes[1] == 0x00 && bytes[2] == 0x09 && bytes[3] == 0x00)))
        {
            return "wmf";
        }

        int textLength = bytes.Length;
        if (textLength > 1024)
        {
            textLength = 1024;
        }
        string prefix = Encoding.UTF8.GetString(bytes, 0, textLength);
        if (prefix.IndexOf("<svg", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return "svg";
        }

        return "";
    }

    internal static string GetImageFormatFromContentType(string contentType)
    {
        if (string.Equals(contentType, "image/png", System.StringComparison.OrdinalIgnoreCase))
        {
            return "png";
        }

        if (string.Equals(contentType, "image/jpeg", System.StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "image/jpg", System.StringComparison.OrdinalIgnoreCase))
        {
            return "jpeg";
        }

        if (string.Equals(contentType, "image/gif", System.StringComparison.OrdinalIgnoreCase))
        {
            return "gif";
        }

        if (string.Equals(contentType, "image/bmp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "bmp";
        }

        if (string.Equals(contentType, "image/tiff", System.StringComparison.OrdinalIgnoreCase))
        {
            return "tiff";
        }

        if (string.Equals(contentType, "image/webp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "webp";
        }

        if (string.Equals(contentType, "image/svg+xml", System.StringComparison.OrdinalIgnoreCase))
        {
            return "svg";
        }

        if (string.Equals(contentType, "image/x-emf", System.StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "image/emf", System.StringComparison.OrdinalIgnoreCase))
        {
            return "emf";
        }

        if (string.Equals(contentType, "image/x-wmf", System.StringComparison.OrdinalIgnoreCase) || string.Equals(contentType, "image/wmf", System.StringComparison.OrdinalIgnoreCase))
        {
            return "wmf";
        }

        return "";
    }

    internal static string GetContentTypeForFormat(string format)
    {
        if (format == "png")
        {
            return "image/png";
        }

        if (format == "jpeg")
        {
            return "image/jpeg";
        }

        if (format == "gif")
        {
            return "image/gif";
        }

        if (format == "bmp")
        {
            return "image/bmp";
        }

        if (format == "tiff")
        {
            return "image/tiff";
        }

        if (format == "webp")
        {
            return "image/webp";
        }

        if (format == "svg")
        {
            return "image/svg+xml";
        }

        if (format == "emf")
        {
            return "image/x-emf";
        }

        if (format == "wmf")
        {
            return "image/x-wmf";
        }

        return "";
    }

    internal static string GetExtensionForFormat(string format)
    {
        if (format == "jpeg")
        {
            return "jpg";
        }

        if (format == "png" || format == "gif" || format == "bmp" || format == "tiff" || format == "webp" || format == "svg" || format == "emf" || format == "wmf")
        {
            return format;
        }

        return "";
    }

    internal static string GetImageFormatFromExtension(string extension)
    {
        if (extension == null)
        {
            return "";
        }

        string normalized = extension;
        if (normalized.StartsWith("."))
        {
            normalized = normalized.Substring(1);
        }

        if (string.Equals(normalized, "png", System.StringComparison.OrdinalIgnoreCase))
        {
            return "png";
        }

        if (string.Equals(normalized, "jpg", System.StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "jpeg", System.StringComparison.OrdinalIgnoreCase))
        {
            return "jpeg";
        }

        if (string.Equals(normalized, "gif", System.StringComparison.OrdinalIgnoreCase))
        {
            return "gif";
        }

        if (string.Equals(normalized, "bmp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "bmp";
        }

        if (string.Equals(normalized, "tif", System.StringComparison.OrdinalIgnoreCase) || string.Equals(normalized, "tiff", System.StringComparison.OrdinalIgnoreCase))
        {
            return "tiff";
        }

        if (string.Equals(normalized, "webp", System.StringComparison.OrdinalIgnoreCase))
        {
            return "webp";
        }

        if (string.Equals(normalized, "svg", System.StringComparison.OrdinalIgnoreCase))
        {
            return "svg";
        }

        if (string.Equals(normalized, "emf", System.StringComparison.OrdinalIgnoreCase))
        {
            return "emf";
        }

        if (string.Equals(normalized, "wmf", System.StringComparison.OrdinalIgnoreCase))
        {
            return "wmf";
        }

        return "";
    }

    internal static string GetExtensionFromPartPath(string path)
    {
        if (path == null)
        {
            return "";
        }

        int dotIndex = path.LastIndexOf('.');
        if (dotIndex < 0 || dotIndex == path.Length - 1)
        {
            return "";
        }

        return path.Substring(dotIndex + 1);
    }
}
