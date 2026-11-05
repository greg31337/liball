using System.Text;

namespace gltf.Tests.Support;

internal sealed class TemporaryResourceWorkspace : IDisposable
{
    private bool disposed;

    internal string RootPath { get; }

    internal TemporaryResourceWorkspace()
    {
        RootPath = Path.Combine(
            Path.GetTempPath(),
            "gltf-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    internal string GetPath(string relativePath)
    {
        string result = Path.GetFullPath(Path.Combine(RootPath, relativePath));
        string prefix = RootPath.EndsWith(Path.DirectorySeparatorChar)
            ? RootPath
            : RootPath + Path.DirectorySeparatorChar;
        if (!result.StartsWith(prefix, StringComparison.Ordinal)
            && !string.Equals(result, RootPath, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "The relative path leaves the temporary workspace.",
                nameof(relativePath));
        }

        return result;
    }

    internal string WriteBytes(string relativePath, byte[] bytes)
    {
        string path = GetPath(relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllBytes(path, bytes);
        return path;
    }

    internal string WriteText(string relativePath, string text)
    {
        string path = GetPath(relativePath);
        string? directory = Path.GetDirectoryName(path);
        if (directory != null)
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, text, new UTF8Encoding(false));
        return path;
    }

    public void Dispose()
    {
        if (!disposed && Directory.Exists(RootPath))
        {
            Directory.Delete(RootPath, true);
        }

        disposed = true;
    }
}
