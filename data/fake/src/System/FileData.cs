namespace fake;

internal static class FileData
{
    internal const string Source = "Reviewed conventional extension mappings with bundled IANA media-type validation";
    internal const string License = "project-license";

    internal static readonly string[] Extensions = new string[]
    {
        "bin", "csv", "gif", "gz", "html", "jpg", "json", "mp3",
        "mp4", "ogg", "pdf", "png", "svg", "txt", "xml", "zip"
    };

    internal static readonly string[] MimeTypes = new string[]
    {
        "application/octet-stream", "text/csv", "image/gif", "application/gzip",
        "text/html", "image/jpeg", "application/json", "audio/mpeg",
        "video/mp4", "audio/ogg", "application/pdf", "image/png",
        "image/svg+xml", "text/plain", "application/xml", "application/zip"
    };

    internal static readonly string[] Types = new string[]
    {
        "binary", "data", "image", "archive", "document", "image", "data",
        "audio", "video", "audio", "document", "image", "image", "text",
        "data", "archive"
    };

    internal static readonly string[] BaseNames = new string[]
    {
        "asset", "attachment", "example", "export", "fixture", "sample",
        "upload", "workspace"
    };
}
