namespace LanguageCodes.DataGenerator.Emission;

internal sealed class RenderedSourceFile
{
    internal RenderedSourceFile(string relativePath, byte[] bytes, string sha256)
    {
        RelativePath = relativePath;
        Bytes = bytes;
        Sha256 = sha256;
    }

    internal string RelativePath { get; }

    internal byte[] Bytes { get; }

    internal string Sha256 { get; }
}
