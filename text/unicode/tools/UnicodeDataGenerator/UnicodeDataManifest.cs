namespace unicode.Tools.UnicodeDataGenerator;

public static class UnicodeDataManifest
{
    public static void DownloadAndVerify(string dataDirectory, string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        UnicodeDataManifestEntry[] entries = Read(manifestPath);
        using HttpClient client = new HttpClient();

        foreach (UnicodeDataManifestEntry entry in entries)
        {
            string destination = Path.Combine(dataDirectory, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
            string directory = Path.GetDirectoryName(destination) ?? dataDirectory;
            Directory.CreateDirectory(directory);
            byte[] content = client.GetByteArrayAsync(UnicodeDataFiles.GetDownloadUrl(entry.FileName)).GetAwaiter().GetResult();
            string actualHash = ComputeHash(content);
            if (!string.Equals(actualHash, entry.Hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Checksum mismatch for " + entry.FileName + ".");
            }

            string temporaryPath = destination + ".download";
            File.WriteAllBytes(temporaryPath, content);
            File.Move(temporaryPath, destination, true);
        }

        Verify(dataDirectory, manifestPath);
    }

    public static void Verify(string dataDirectory, string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dataDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        UnicodeDataManifestEntry[] entries = Read(manifestPath);
        foreach (UnicodeDataManifestEntry entry in entries)
        {
            string path = Path.Combine(dataDirectory, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Required Unicode data file is missing.", path);
            }

            string actualHash = ComputeHash(File.ReadAllBytes(path));
            if (!string.Equals(actualHash, entry.Hash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Checksum mismatch for " + entry.FileName + ".");
            }
        }
    }

    public static UnicodeDataManifestEntry[] Read(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        List<UnicodeDataManifestEntry> entries = new List<UnicodeDataManifestEntry>();
        foreach (string rawLine in File.ReadLines(manifestPath))
        {
            string line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            int separator = line.IndexOf(' ');
            if (separator != 64)
            {
                throw new FormatException("Checksum manifest records must contain a SHA-256 hash and relative file name.");
            }

            string hash = line.Substring(0, separator);
            string fileName = line.Substring(separator).Trim();
            if (fileName.Length == 0 || Path.IsPathRooted(fileName) || fileName.Contains("..", StringComparison.Ordinal))
            {
                throw new FormatException("Checksum manifest file names must be safe relative paths.");
            }

            entries.Add(new UnicodeDataManifestEntry(hash, fileName));
        }

        return entries.ToArray();
    }

    static string ComputeHash(byte[] content)
    {
        byte[] hash = System.Security.Cryptography.SHA256.HashData(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
