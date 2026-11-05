namespace fake;

internal static class PathData
{
    internal const string Source = "Project-authored generic path segments and conventional platform syntax";
    internal const string License = "project-license";

    internal static readonly string[] DirectoryNames = new string[]
    {
        "archive", "cache", "data", "documents", "exports", "fixtures",
        "projects", "reports", "temp", "workspace"
    };

    internal static readonly string[] WindowsDrives = new string[]
    {
        "C", "D", "E"
    };

    internal static readonly string[] UnixReservedNames = new string[]
    {
        ".", ".."
    };

    internal static readonly string[] WindowsReservedNames = new string[]
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM9", "LPT1", "LPT9"
    };

    internal static readonly string[] UnicodeNames = new string[]
    {
        "café", "naïve", "résumé", "資料", "東京"
    };

    internal static readonly char[] UnixInvalidSegmentCharacters = new char[]
    {
        '\0', '/'
    };

    internal static readonly char[] WindowsInvalidSegmentCharacters = new char[]
    {
        '\0', '\u0001', '\u0002', '\u0003', '\u0004', '\u0005', '\u0006',
        '\u0007', '\u0008', '\u0009', '\u000a', '\u000b', '\u000c', '\u000d',
        '\u000e', '\u000f', '\u0010', '\u0011', '\u0012', '\u0013', '\u0014',
        '\u0015', '\u0016', '\u0017', '\u0018', '\u0019', '\u001a', '\u001b',
        '\u001c', '\u001d', '\u001e', '\u001f', '"', '<', '>', '|', ':', '*',
        '?', '/', '\\'
    };
}
