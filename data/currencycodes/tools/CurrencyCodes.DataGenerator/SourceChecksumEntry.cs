namespace CurrencyCodes.DataGenerator;

internal sealed class SourceChecksumEntry
{
    internal SourceChecksumEntry(string path, long byteLength, string sha256)
    {
        Path = path;
        ByteLength = byteLength;
        Sha256 = sha256;
    }

    internal string Path { get; }

    internal long ByteLength { get; }

    internal string Sha256 { get; }
}
