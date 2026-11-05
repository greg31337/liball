using System.Text;

namespace LanguageCodes.Tests.Generator;

internal sealed class GeneratorFixture : IDisposable
{
    private readonly string directoryPath;

    internal GeneratorFixture()
    {
        directoryPath = Path.Combine(Path.GetTempPath(), "languagecodes-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
    }

    public void Dispose()
    {
        Directory.Delete(directoryPath, true);
    }

    internal string WriteText(string fileName, string content)
    {
        string path = Path.Combine(directoryPath, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(false, true));
        return path;
    }

    internal string WriteBomText(string fileName, string content)
    {
        string path = Path.Combine(directoryPath, fileName);
        File.WriteAllText(path, content, new UTF8Encoding(true, true));
        return path;
    }

    internal string WriteBytes(string fileName, byte[] content)
    {
        string path = Path.Combine(directoryPath, fileName);
        File.WriteAllBytes(path, content);
        return path;
    }
}
