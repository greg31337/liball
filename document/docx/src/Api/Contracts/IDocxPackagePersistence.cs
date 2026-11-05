using System.IO;

namespace docx;

/// <summary>
/// Provides creation, loading, and saving entry points for DOCX packages.
/// </summary>
public interface IDocxPackagePersistence
{
    /// <summary>
    /// Creates a package containing a minimal WordprocessingML document.
    /// </summary>
    /// <returns>The created package.</returns>
    DocxPackage CreatePackage();

    /// <summary>
    /// Loads a DOCX package from a stream with the supplied options.
    /// </summary>
    /// <param name="stream">The package stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded package.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    DocxPackage OpenPackage(Stream stream, DocxLoadOptions options);

    /// <summary>
    /// Loads a DOCX package from a file with the supplied options.
    /// </summary>
    /// <param name="path">The path of the package file.</param>
    /// <param name="options">The package loading options.</param>
    /// <returns>The loaded package.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    DocxPackage OpenPackageFile(string path, DocxLoadOptions options);

    /// <summary>
    /// Saves a DOCX package to a stream with the supplied options.
    /// </summary>
    /// <param name="package">The package to save.</param>
    /// <param name="stream">The destination stream. The caller retains ownership of the stream.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SavePackage(DocxPackage package, Stream stream, DocxSaveOptions options);

    /// <summary>
    /// Saves a DOCX package to a file with the supplied options.
    /// </summary>
    /// <param name="package">The package to save.</param>
    /// <param name="path">The destination file path.</param>
    /// <param name="options">The package saving options.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    void SavePackageFile(DocxPackage package, string path, DocxSaveOptions options);
}
