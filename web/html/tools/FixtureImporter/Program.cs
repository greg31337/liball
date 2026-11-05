using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace UniversalHtml.FixtureImporter;

public class FixtureManifest
{
    public int SchemaVersion { get; set; }

    public FixtureSubset[] Subsets { get; set; }
}

public class FixtureSubset
{
    public string Id { get; set; }

    public string Root { get; set; }

    public string Source { get; set; }

    public string Revision { get; set; }

    public string License { get; set; }

    public int ExpectedFileCount { get; set; }

    public string AggregateSha256 { get; set; }
}

public class FixtureDigest
{
    public FixtureDigest(string id, int fileCount, string sha256)
    {
        this.Id = id;
        this.FileCount = fileCount;
        this.Sha256 = sha256;
    }

    public string Id { get; }

    public int FileCount { get; }

    public string Sha256 { get; }
}

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            bool print = ReadPrintMode(args);
            string repositoryRoot = FindRepositoryRoot();
            string manifestPath = Path.Combine(repositoryRoot, "tests",
                "data", "fixture-manifest.json");
            FixtureManifest manifest = JsonSerializer.Deserialize<FixtureManifest>(
                File.ReadAllText(manifestPath), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            ValidateManifest(manifest);
            bool valid = true;
            HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (FixtureSubset subset in manifest.Subsets)
            {
                ValidateSubset(repositoryRoot, subset, ids);
                FixtureDigest digest = ComputeDigest(repositoryRoot, subset);
                Console.WriteLine(digest.Id + " files=" + digest.FileCount
                    + " sha256=" + digest.Sha256);
                if (!print && (digest.FileCount != subset.ExpectedFileCount
                    || digest.Sha256 != subset.AggregateSha256))
                {
                    valid = false;
                    Console.Error.WriteLine("Fixture checksum mismatch for "
                        + subset.Id + ".");
                }
            }

            return valid ? 0 : 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }

    static bool ReadPrintMode(string[] args)
    {
        if (args.Length == 0 || args.Length == 1 && args[0] == "--verify")
        {
            return false;
        }

        if (args.Length == 1 && args[0] == "--print")
        {
            return true;
        }

        throw new ArgumentException(
            "Usage: FixtureImporter [--verify|--print]");
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(
            Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "todo.md"))
                && Directory.Exists(Path.Combine(directory.FullName,
                    "tests", "data")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root.");
    }

    static void ValidateManifest(FixtureManifest manifest)
    {
        if (manifest == null || manifest.SchemaVersion != 1
            || manifest.Subsets == null || manifest.Subsets.Length == 0)
        {
            throw new InvalidDataException(
                "The fixture manifest is missing or unsupported.");
        }
    }

    static void ValidateSubset(string repositoryRoot, FixtureSubset subset,
        HashSet<string> ids)
    {
        if (subset == null || string.IsNullOrWhiteSpace(subset.Id)
            || string.IsNullOrWhiteSpace(subset.Root)
            || string.IsNullOrWhiteSpace(subset.Source)
            || string.IsNullOrWhiteSpace(subset.Revision)
            || string.IsNullOrWhiteSpace(subset.License)
            || subset.ExpectedFileCount < 1
            || subset.AggregateSha256 == null
            || subset.AggregateSha256.Length != 64)
        {
            throw new InvalidDataException(
                "A fixture subset manifest entry is incomplete.");
        }

        if (!ids.Add(subset.Id))
        {
            throw new InvalidDataException(
                "Duplicate fixture subset id: " + subset.Id);
        }

        ResolvePath(repositoryRoot, subset.Root, true);
        ResolvePath(repositoryRoot, subset.Source, false);
        ResolvePath(repositoryRoot, subset.License, false);
    }

    static FixtureDigest ComputeDigest(string repositoryRoot,
        FixtureSubset subset)
    {
        string root = ResolvePath(repositoryRoot, subset.Root, true);
        string[] files = Directory.GetFiles(root, "*",
            SearchOption.AllDirectories)
            .Where(path => !IsMetadataFile(path))
            .OrderBy(path => Path.GetRelativePath(root, path)
                .Replace('\\', '/'), StringComparer.Ordinal)
            .ToArray();
        using IncrementalHash hash = IncrementalHash.CreateHash(
            HashAlgorithmName.SHA256);
        foreach (string path in files)
        {
            string relative = Path.GetRelativePath(root, path)
                .Replace('\\', '/');
            hash.AppendData(Encoding.UTF8.GetBytes(relative + "\n"));
            hash.AppendData(File.ReadAllBytes(path));
            hash.AppendData(new byte[] { (byte)'\n' });
        }

        string digest = Convert.ToHexString(hash.GetHashAndReset())
            .ToLowerInvariant();
        return new FixtureDigest(subset.Id, files.Length, digest);
    }

    static bool IsMetadataFile(string path)
    {
        string name = Path.GetFileName(path);
        return name == "SOURCE.md" || name.StartsWith("LICENSE",
            StringComparison.Ordinal);
    }

    static string ResolvePath(string repositoryRoot, string relative,
        bool directory)
    {
        string combined = Path.GetFullPath(Path.Combine(repositoryRoot,
            relative));
        string prefix = repositoryRoot.EndsWith(
            Path.DirectorySeparatorChar)
            ? repositoryRoot
            : repositoryRoot + Path.DirectorySeparatorChar;
        if (!combined.StartsWith(prefix, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Fixture manifest path escapes the repository: " + relative);
        }

        bool exists = directory
            ? Directory.Exists(combined) : File.Exists(combined);
        if (!exists)
        {
            throw new FileNotFoundException(
                "Fixture manifest path does not exist.", relative);
        }

        return combined;
    }
}
