namespace LanguageCodes.DataGenerator.Emission;

internal sealed class EmissionFileResult
{
    internal EmissionFileResult(string path, string sha256, int byteCount, bool changed)
    {
        Path = path;
        Sha256 = sha256;
        ByteCount = byteCount;
        Changed = changed;
    }

    internal string Path { get; }

    internal string Sha256 { get; }

    internal int ByteCount { get; }

    internal bool Changed { get; }
}
