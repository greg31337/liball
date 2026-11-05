namespace LanguageCodes.DataGenerator.Emission;

internal sealed class GeneratedSourceFile
{
    internal GeneratedSourceFile(string relativePath, string content)
    {
        RelativePath = relativePath;
        Content = content;
    }

    internal string RelativePath { get; }

    internal string Content { get; }
}
