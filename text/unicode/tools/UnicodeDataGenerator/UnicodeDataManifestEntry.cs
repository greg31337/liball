namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeDataManifestEntry
{
    public string Hash;
    public string FileName;

    public UnicodeDataManifestEntry(string hash, string fileName)
    {
        Hash = hash;
        FileName = fileName;
    }
}
