using System;

namespace email;
/// <summary>
/// Provides operations for working with MIME type.
/// </summary>
public class EmailMimeType : IEmailMimeTypeContract
{
    internal static string? GetMediaTypeForExtensionImplementation(string extension)
    {
        string value = NormalizeExtension(extension);
        switch (value)
        {
            case "txt":
            case "text":
            case "log":
                return "text/plain";
            case "html":
            case "htm":
                return "text/html";
            case "css":
                return "text/css";
            case "csv":
                return "text/csv";
            case "xml":
                return "application/xml";
            case "json":
                return "application/json";
            case "pdf":
                return "application/pdf";
            case "zip":
                return "application/zip";
            case "gz":
            case "gzip":
                return "application/gzip";
            case "7z":
                return "application/x-7z-compressed";
            case "bz2":
                return "application/x-bzip2";
            case "xz":
                return "application/x-xz";
            case "tar":
                return "application/x-tar";
            case "rar":
                return "application/vnd.rar";
            case "rtf":
                return "application/rtf";
            case "doc":
                return "application/msword";
            case "docx":
                return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            case "xls":
                return "application/vnd.ms-excel";
            case "xlsx":
                return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            case "ppt":
                return "application/vnd.ms-powerpoint";
            case "pptx":
                return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            case "odt":
                return "application/vnd.oasis.opendocument.text";
            case "ods":
                return "application/vnd.oasis.opendocument.spreadsheet";
            case "odp":
                return "application/vnd.oasis.opendocument.presentation";
            case "epub":
                return "application/epub+zip";
            case "jar":
                return "application/java-archive";
            case "apk":
                return "application/vnd.android.package-archive";
            case "wasm":
                return "application/wasm";
            case "bin":
            case "dat":
                return "application/octet-stream";
            case "jpg":
            case "jpeg":
            case "jpe":
                return "image/jpeg";
            case "png":
                return "image/png";
            case "gif":
                return "image/gif";
            case "webp":
                return "image/webp";
            case "bmp":
                return "image/bmp";
            case "tif":
            case "tiff":
                return "image/tiff";
            case "ico":
                return "image/vnd.microsoft.icon";
            case "svg":
                return "image/svg+xml";
            case "heic":
                return "image/heic";
            case "avif":
                return "image/avif";
            case "mp3":
                return "audio/mpeg";
            case "wav":
                return "audio/wav";
            case "ogg":
            case "oga":
                return "audio/ogg";
            case "flac":
                return "audio/flac";
            case "m4a":
                return "audio/mp4";
            case "mp4":
            case "m4v":
                return "video/mp4";
            case "webm":
                return "video/webm";
            case "mov":
                return "video/quicktime";
            case "ics":
                return "text/calendar";
            case "eml":
                return "message/rfc822";
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the media type for extension.
    /// </summary>
    /// <param name = "extension">The extension used by the operation.</param>
    /// <returns>The current media type for extension.</returns>
    public string? GetMediaTypeForExtension(string extension)
    {
        return EmailMimeType.GetMediaTypeForExtensionImplementation(extension);
    }

    internal static string? GetMediaTypeForFilenameImplementation(string filename)
    {
        if (filename == null)
        {
            throw new ArgumentNullException("filename");
        }

        int separator = LastSeparator(filename);
        int dot = filename.LastIndexOf('.');
        if (dot <= separator || dot + 1 >= filename.Length)
        {
            return null;
        }

        return GetMediaTypeForExtensionImplementation(filename.Substring(dot + 1));
    }

    /// <summary>
    /// Gets the media type for filename.
    /// </summary>
    /// <param name = "filename">The filename used by the operation.</param>
    /// <returns>The current media type for filename.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string? GetMediaTypeForFilename(string filename)
    {
        return EmailMimeType.GetMediaTypeForFilenameImplementation(filename);
    }

    internal static string? GetPreferredExtensionImplementationWithMediaTypeUsingEmailMediaType(EmailMediaType mediaType)
    {
        if (mediaType == null)
        {
            throw new ArgumentNullException("mediaType");
        }

        return GetPreferredExtensionImplementationWithMediaTypeUsingString(mediaType.GetNormalizedTopLevelType() + "/" + mediaType.GetNormalizedSubtype());
    }

    /// <summary>
    /// Gets the preferred extension.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current preferred extension.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public string? GetPreferredExtensionWithMediaTypeUsingEmailMediaType(EmailMediaType mediaType)
    {
        return EmailMimeType.GetPreferredExtensionImplementationWithMediaTypeUsingEmailMediaType(mediaType);
    }

    internal static string? GetPreferredExtensionImplementationWithMediaTypeUsingString(string mediaType)
    {
        string value = NormalizeMediaType(mediaType);
        switch (value)
        {
            case "text/plain":
                return "txt";
            case "text/html":
                return "html";
            case "text/css":
                return "css";
            case "text/csv":
                return "csv";
            case "text/xml":
            case "application/xml":
                return "xml";
            case "application/json":
                return "json";
            case "application/pdf":
            case "application/x-pdf":
                return "pdf";
            case "application/zip":
                return "zip";
            case "application/gzip":
            case "application/x-gzip":
                return "gz";
            case "application/x-7z-compressed":
                return "7z";
            case "application/x-bzip2":
                return "bz2";
            case "application/x-xz":
                return "xz";
            case "application/x-tar":
                return "tar";
            case "application/vnd.rar":
            case "application/x-rar-compressed":
                return "rar";
            case "application/rtf":
            case "text/rtf":
                return "rtf";
            case "application/msword":
                return "doc";
            case "application/vnd.openxmlformats-officedocument.wordprocessingml.document":
                return "docx";
            case "application/vnd.ms-excel":
                return "xls";
            case "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet":
                return "xlsx";
            case "application/vnd.ms-powerpoint":
                return "ppt";
            case "application/vnd.openxmlformats-officedocument.presentationml.presentation":
                return "pptx";
            case "application/vnd.oasis.opendocument.text":
                return "odt";
            case "application/vnd.oasis.opendocument.spreadsheet":
                return "ods";
            case "application/vnd.oasis.opendocument.presentation":
                return "odp";
            case "application/epub+zip":
                return "epub";
            case "application/java-archive":
                return "jar";
            case "application/vnd.android.package-archive":
                return "apk";
            case "application/wasm":
                return "wasm";
            case "application/octet-stream":
                return "bin";
            case "image/jpeg":
            case "image/jpg":
                return "jpg";
            case "image/png":
                return "png";
            case "image/gif":
                return "gif";
            case "image/webp":
                return "webp";
            case "image/bmp":
                return "bmp";
            case "image/tiff":
                return "tif";
            case "image/vnd.microsoft.icon":
            case "image/x-icon":
                return "ico";
            case "image/svg+xml":
                return "svg";
            case "image/heic":
                return "heic";
            case "image/avif":
                return "avif";
            case "audio/mpeg":
                return "mp3";
            case "audio/wav":
            case "audio/x-wav":
                return "wav";
            case "audio/ogg":
            case "application/ogg":
                return "ogg";
            case "audio/flac":
                return "flac";
            case "audio/mp4":
                return "m4a";
            case "video/mp4":
                return "mp4";
            case "video/webm":
                return "webm";
            case "video/quicktime":
                return "mov";
            case "text/calendar":
                return "ics";
            case "message/rfc822":
                return "eml";
            default:
                return null;
        }
    }

    /// <summary>
    /// Gets the preferred extension.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns>The current preferred extension.</returns>
    public string? GetPreferredExtensionWithMediaTypeUsingString(string mediaType)
    {
        return EmailMimeType.GetPreferredExtensionImplementationWithMediaTypeUsingString(mediaType);
    }

    internal static string? DetectFromMagicImplementation(ReadOnlySpan<byte> prefix)
    {
        if (Matches(prefix, 0, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d }))
            return "application/pdf";
        if (Matches(prefix, 0, new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return "image/png";
        if (Matches(prefix, 0, new byte[] { 0xff, 0xd8, 0xff }))
            return "image/jpeg";
        if (MatchesAscii(prefix, 0, "GIF87a") || MatchesAscii(prefix, 0, "GIF89a"))
            return "image/gif";
        if (Matches(prefix, 0, new byte[] { 0x50, 0x4b, 0x03, 0x04 }) || Matches(prefix, 0, new byte[] { 0x50, 0x4b, 0x05, 0x06 }) || Matches(prefix, 0, new byte[] { 0x50, 0x4b, 0x07, 0x08 }))
            return "application/zip";
        if (Matches(prefix, 0, new byte[] { 0x1f, 0x8b }))
            return "application/gzip";
        if (Matches(prefix, 0, new byte[] { 0x37, 0x7a, 0xbc, 0xaf, 0x27, 0x1c }))
            return "application/x-7z-compressed";
        if (MatchesAscii(prefix, 0, "BZh"))
            return "application/x-bzip2";
        if (Matches(prefix, 0, new byte[] { 0xfd, 0x37, 0x7a, 0x58, 0x5a, 0x00 }))
            return "application/x-xz";
        if (Matches(prefix, 0, new byte[] { 0x00, 0x61, 0x73, 0x6d }))
            return "application/wasm";
        if (Matches(prefix, 0, new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1a, 0x07 }))
            return "application/vnd.rar";
        if (Matches(prefix, 0, new byte[] { 0x49, 0x49, 0x2a, 0x00 }) || Matches(prefix, 0, new byte[] { 0x4d, 0x4d, 0x00, 0x2a }))
            return "image/tiff";
        if (MatchesAscii(prefix, 0, "RIFF") && MatchesAscii(prefix, 8, "WEBP"))
            return "image/webp";
        if (MatchesAscii(prefix, 0, "RIFF") && MatchesAscii(prefix, 8, "WAVE"))
            return "audio/wav";
        if (MatchesAscii(prefix, 0, "OggS"))
            return "application/ogg";
        if (MatchesAscii(prefix, 0, "fLaC"))
            return "audio/flac";
        if (MatchesAscii(prefix, 0, "ID3"))
            return "audio/mpeg";
        if (MatchesAscii(prefix, 0, "8BPS"))
            return "image/vnd.adobe.photoshop";
        if (MatchesAscii(prefix, 0, "SQLite format 3") && Matches(prefix, 15, new byte[] { 0x00 }))
            return "application/vnd.sqlite3";
        if (MatchesAscii(prefix, 0, "{\\rtf"))
            return "application/rtf";
        if (Matches(prefix, 0, new byte[] { 0x00, 0x00, 0x01, 0x00 }))
            return "image/vnd.microsoft.icon";
        if (MatchesAscii(prefix, 0, "BM") && prefix.Length >= 14)
            return "image/bmp";
        if (MatchesAscii(prefix, 4, "ftyp"))
            return DetectIsoBaseMedia(prefix);
        return null;
    }

    /// <summary>
    /// Detects the from magic.
    /// </summary>
    /// <param name = "prefix">The prefix used by the operation.</param>
    /// <returns>The string produced by the operation.</returns>
    public string? DetectFromMagic(ReadOnlySpan<byte> prefix)
    {
        return EmailMimeType.DetectFromMagicImplementation(prefix);
    }

    internal static bool AreCompatibleImplementation(string firstMediaType, string secondMediaType)
    {
        string first = NormalizeMediaType(firstMediaType);
        string second = NormalizeMediaType(secondMediaType);
        if (string.Equals(first, second, StringComparison.Ordinal))
            return true;
        if (IsAliasPair(first, second))
            return true;
        if (IsZipContainer(first) && string.Equals(second, "application/zip", StringComparison.Ordinal))
            return true;
        if (IsZipContainer(second) && string.Equals(first, "application/zip", StringComparison.Ordinal))
            return true;
        return false;
    }

    /// <summary>
    /// Determines whether the compared values are compatible.
    /// </summary>
    /// <param name = "firstMediaType">The first media type used by the operation.</param>
    /// <param name = "secondMediaType">The second media type used by the operation.</param>
    /// <returns><c>true</c> when the operation succeeds or the condition is met; otherwise, <c>false</c>.</returns>
    public bool AreCompatible(string firstMediaType, string secondMediaType)
    {
        return EmailMimeType.AreCompatibleImplementation(firstMediaType, secondMediaType);
    }

    internal static bool IsGenericImplementation(string mediaType)
    {
        string value = NormalizeMediaType(mediaType);
        return value == "application/octet-stream" || value == "binary/octet-stream";
    }

    /// <summary>
    /// Determines whether the current value or state is generic.
    /// </summary>
    /// <param name = "mediaType">The media type used by the operation.</param>
    /// <returns><c>true</c> when the value or state is generic; otherwise, <c>false</c>.</returns>
    public bool IsGeneric(string mediaType)
    {
        return EmailMimeType.IsGenericImplementation(mediaType);
    }

    static string? DetectIsoBaseMedia(ReadOnlySpan<byte> prefix)
    {
        if (MatchesAscii(prefix, 8, "M4A ") || MatchesAscii(prefix, 8, "M4B "))
            return "audio/mp4";
        if (MatchesAscii(prefix, 8, "qt  "))
            return "video/quicktime";
        if (MatchesAscii(prefix, 8, "heic") || MatchesAscii(prefix, 8, "heix") || MatchesAscii(prefix, 8, "hevc") || MatchesAscii(prefix, 8, "hevx") || MatchesAscii(prefix, 8, "mif1"))
            return "image/heic";
        if (MatchesAscii(prefix, 8, "avif") || MatchesAscii(prefix, 8, "avis"))
            return "image/avif";
        if (MatchesAscii(prefix, 8, "isom") || MatchesAscii(prefix, 8, "iso2") || MatchesAscii(prefix, 8, "mp41") || MatchesAscii(prefix, 8, "mp42") || MatchesAscii(prefix, 8, "avc1") || MatchesAscii(prefix, 8, "M4V "))
            return "video/mp4";
        return null;
    }

    static bool IsAliasPair(string first, string second)
    {
        if ((first == "image/jpeg" && second == "image/jpg") || (first == "image/jpg" && second == "image/jpeg"))
            return true;
        if ((first == "application/pdf" && second == "application/x-pdf") || (first == "application/x-pdf" && second == "application/pdf"))
            return true;
        if ((first == "application/gzip" && second == "application/x-gzip") || (first == "application/x-gzip" && second == "application/gzip"))
            return true;
        if ((first == "audio/wav" && second == "audio/x-wav") || (first == "audio/x-wav" && second == "audio/wav"))
            return true;
        if ((first == "audio/ogg" && second == "application/ogg") || (first == "application/ogg" && second == "audio/ogg"))
            return true;
        if ((first == "application/xml" && second == "text/xml") || (first == "text/xml" && second == "application/xml"))
            return true;
        if ((first == "image/vnd.microsoft.icon" && second == "image/x-icon") || (first == "image/x-icon" && second == "image/vnd.microsoft.icon"))
            return true;
        if ((first == "application/rtf" && second == "text/rtf") || (first == "text/rtf" && second == "application/rtf"))
            return true;
        if ((first == "application/vnd.rar" && second == "application/x-rar-compressed") || (first == "application/x-rar-compressed" && second == "application/vnd.rar"))
            return true;
        return false;
    }

    static bool IsZipContainer(string mediaType)
    {
        return mediaType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document" || mediaType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" || mediaType == "application/vnd.openxmlformats-officedocument.presentationml.presentation" || mediaType == "application/vnd.oasis.opendocument.text" || mediaType == "application/vnd.oasis.opendocument.spreadsheet" || mediaType == "application/vnd.oasis.opendocument.presentation" || mediaType == "application/epub+zip" || mediaType == "application/java-archive" || mediaType == "application/vnd.android.package-archive";
    }

    static bool Matches(ReadOnlySpan<byte> value, int offset, byte[] expected)
    {
        if (offset < 0 || offset + expected.Length > value.Length)
            return false;
        int index = 0;
        while (index < expected.Length)
        {
            if (value[offset + index] != expected[index])
                return false;
            index++;
        }

        return true;
    }

    static bool MatchesAscii(ReadOnlySpan<byte> value, int offset, string expected)
    {
        if (offset < 0 || offset + expected.Length > value.Length)
            return false;
        int index = 0;
        while (index < expected.Length)
        {
            if (value[offset + index] != (byte)expected[index])
                return false;
            index++;
        }

        return true;
    }

    static string NormalizeExtension(string extension)
    {
        if (extension == null)
            throw new ArgumentNullException("extension");
        string value = extension.Trim();
        int dot = value.LastIndexOf('.');
        if (dot >= 0)
            value = value.Substring(dot + 1);
        if (value.Length == 0)
            throw new ArgumentException("A file extension is required.", "extension");
        return value.ToLowerInvariant();
    }

    static string NormalizeMediaType(string mediaType)
    {
        if (mediaType == null)
            throw new ArgumentNullException("mediaType");
        string value = mediaType.Trim().ToLowerInvariant();
        if (value.Length == 0 || value.IndexOf('/') < 1 || value[value.Length - 1] == '/')
            throw new ArgumentException("A complete media type is required.", "mediaType");
        return value;
    }

    static int LastSeparator(string value)
    {
        int slash = value.LastIndexOf('/');
        int backslash = value.LastIndexOf('\\');
        return slash > backslash ? slash : backslash;
    }
}
