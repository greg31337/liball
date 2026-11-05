using System.Collections.Generic;

namespace image;

/// <summary>Contains the metadata and diagnostics produced for one scanned file.</summary>
public class ImageMetadataScanFileResult
{
    /// <summary>The absolute file path.</summary>
    public string Path;
    /// <summary>Indicates that the file existed and metadata reading completed.</summary>
    public bool IsSuccess;
    /// <summary>The extracted metadata document, or an empty document when reading could not begin.</summary>
    public ImageMetadataDocument Document;
    /// <summary>The file-specific read and parser diagnostics.</summary>
    public List<MetadataDiagnostic> Diagnostics;

    /// <summary>Creates an unsuccessful result with an empty path, document, and diagnostics list.</summary>
    public ImageMetadataScanFileResult()
    {
        Path = "";
        IsSuccess = false;
        Document = new ImageMetadataDocument();
        Diagnostics = new List<MetadataDiagnostic>();
    }
}
