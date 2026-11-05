using System.IO;

namespace docx;

/// <summary>
/// Creates, loads, and saves DOCX packages and documents with explicit options.
/// </summary>
public class DocxPackagePersistence : IDocxPackagePersistence, IDocxDocumentPersistence
{
    /// <summary>
    /// Creates a package persistence service.
    /// </summary>
    public DocxPackagePersistence()
    {
    }

    /// <summary>
    /// Creates a package containing a minimal WordprocessingML document.
    /// </summary>
    /// <returns>The created package.</returns>
    public DocxPackage CreatePackage()
    {
        return DocxPackage.Create();
    }

    /// <summary>
    /// Creates a document backed by a minimal WordprocessingML package.
    /// </summary>
    /// <returns>The created document.</returns>
    public DocxDocument CreateDocument()
    {
        return DocxDocument.Create();
    }

    /// <summary>
    /// Loads a DOCX package from a stream with the supplied options.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded package.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxPackage OpenPackage(Stream stream, DocxLoadOptions options)
    {
        return DocxPackage.Open(stream, options);
    }

    /// <summary>
    /// Loads a DOCX package from a file with the supplied options.
    /// </summary>
    /// <param name="path">The path of the package file.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded package.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxPackage OpenPackageFile(string path, DocxLoadOptions options)
    {
        return DocxPackage.OpenFile(path, options);
    }

    /// <summary>
    /// Loads a DOCX document from a stream with the supplied options.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxDocument OpenDocument(Stream stream, DocxLoadOptions options)
    {
        DocxPackage package = DocxPackage.Open(stream, options);
        return new DocxDocument(package);
    }

    /// <summary>
    /// Loads a DOCX document from a file with the supplied options.
    /// </summary>
    /// <param name="path">The path of the package file.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded document.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxDocument OpenDocumentFile(string path, DocxLoadOptions options)
    {
        DocxPackage package = DocxPackage.OpenFile(path, options);
        return new DocxDocument(package);
    }

    /// <summary>
    /// Saves a DOCX package to a stream with the supplied options.
    /// </summary>
    /// <param name="package">The package to save.</param>
    /// <param name="stream">The destination stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SavePackage(DocxPackage package, Stream stream, DocxSaveOptions options)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        package.SaveWithOptions(stream, options);
    }

    /// <summary>
    /// Saves a DOCX package to a file with the supplied options.
    /// </summary>
    /// <param name="package">The package to save.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SavePackageFile(DocxPackage package, string path, DocxSaveOptions options)
    {
        if (package == null)
        {
            throw new ArgumentNullException("package");
        }

        DocxSaveOptions fileOptions = CreateFileSaveOptions(path, options);
        package.SaveFileWithOptions(path, fileOptions);
    }

    /// <summary>
    /// Saves a DOCX document to a stream with the supplied options.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="stream">The destination stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SaveDocument(DocxDocument document, Stream stream, DocxSaveOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (stream == null)
        {
            throw new ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.ValidateValues();
        DocxTemplate.UpdateMainDocumentBody(document);
        document.Package.SaveWithOptions(stream, options);
    }

    /// <summary>
    /// Saves a DOCX document to a file with the supplied options.
    /// </summary>
    /// <param name="document">The document to save.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SaveDocumentFile(DocxDocument document, string path, DocxSaveOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        DocxSaveOptions fileOptions = CreateFileSaveOptions(path, options);
        DocxTemplate.UpdateMainDocumentBody(document);
        document.Package.SaveFileWithOptions(path, fileOptions);
    }

    internal static DocxSaveOptions CreateFileSaveOptions(string path, DocxSaveOptions options)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        options.ValidateValues();

        DocxSaveOptions fileOptions = new DocxSaveOptions();
        fileOptions.PreserveXmlWhitespace = options.PreserveXmlWhitespace;
        fileOptions.PreserveUnknownParts = options.PreserveUnknownParts;
        fileOptions.PreserveUnknownRelationships = options.PreserveUnknownRelationships;
        fileOptions.PreserveUnknownMarkup = options.PreserveUnknownMarkup;
        fileOptions.Deterministic = options.Deterministic;
        fileOptions.Mode = options.Mode;
        fileOptions.CompressionMode = options.CompressionMode;
        fileOptions.OutputType = options.OutputType;

        if (fileOptions.OutputType != DocxOutputType.Preserve)
        {
            return fileOptions;
        }

        string extension = Path.GetExtension(path).ToLowerInvariant();

        if (extension == ".dotx" || extension == ".dotm")
        {
            fileOptions.OutputType = DocxOutputType.Template;
        }
        else if (extension == ".docx" || extension == ".docm")
        {
            fileOptions.OutputType = DocxOutputType.Document;
        }

        return fileOptions;
    }
}
