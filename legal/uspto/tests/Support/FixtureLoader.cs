using System.Text;

namespace uspto.Tests;

internal static class FixtureLoader
{
    internal static string LoadText(string relativePath)
    {
        return File.ReadAllText(ResolvePath(relativePath), Encoding.UTF8);
    }

    internal static byte[] LoadBytes(string relativePath)
    {
        return File.ReadAllBytes(ResolvePath(relativePath));
    }

    internal static string ResolvePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("A fixture-relative path is required.", nameof(relativePath));
        }

        string root = FindFixtureRoot();
        string path = Path.GetFullPath(Path.Combine(root, relativePath));
        string prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!path.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("The fixture path must remain inside the fixture root.", nameof(relativePath));
        }

        return path;
    }

    private static string FindFixtureRoot()
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "tests", "Fixtures");
            if (Directory.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The test fixture root could not be located.");
    }
}
