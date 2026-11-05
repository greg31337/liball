using System.Formats.Tar;
using System.IO.Compression;

namespace yaml.Tests;

public class YamlTestSuiteCase
{
    public string Id;
    public string Name;
    public string InputYaml;
    public string ExpectedEvents;
    public string ExpectedJson;
    public string OutputYaml;
    public string EmitterYaml;
    public bool ShouldParse;

    public YamlTestSuiteCase()
    {
        Id = "";
        Name = "";
        InputYaml = "";
        ExpectedEvents = "";
        ExpectedJson = "";
        OutputYaml = "";
        EmitterYaml = "";
        ShouldParse = true;
    }

    public override string ToString()
    {
        return Id;
    }
}

public static class YamlTestSuiteArchive
{
    public const string Release = "data-2022-01-17";
    public const string ArchiveName = "yaml-test-suite-data-2022-01-17.tar.gz";
    public const string Source = "https://github.com/yaml/yaml-test-suite";
    public const int ExpectedCaseCount = 402;
    public const int ExpectedValidCount = 308;
    public const int ExpectedInvalidCount = 94;

    public static IReadOnlyList<YamlTestSuiteCase> Cases()
    {
        return ArchiveCache.Cases;
    }

    public static string ArchivePath()
    {
        DirectoryInfo? directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(directory.FullName, "specs", ArchiveName);
            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException("Pinned YAML Test Suite archive was not found.", ArchiveName);
    }

    public static List<YamlTestSuiteCase> ReadCases(string archivePath)
    {
        Dictionary<string, Dictionary<string, string>> filesByCase = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        using FileStream file = File.OpenRead(archivePath);
        using GZipStream gzip = new GZipStream(file, CompressionMode.Decompress);
        using TarReader tar = new TarReader(gzip);
        TarEntry? entry = tar.GetNextEntry();
        while (entry != null)
        {
            if (entry.EntryType == TarEntryType.RegularFile)
            {
                AddEntry(filesByCase, entry.Name, entry.DataStream == null ? Stream.Null : entry.DataStream);
            }

            entry = tar.GetNextEntry();
        }

        List<YamlTestSuiteCase> cases = new List<YamlTestSuiteCase>();
        foreach (string id in filesByCase.Keys.OrderBy(value => value, StringComparer.Ordinal))
        {
            Dictionary<string, string> files = filesByCase[id];
            if (!files.ContainsKey("in.yaml"))
            {
                continue;
            }

            YamlTestSuiteCase testCase = new YamlTestSuiteCase();
            testCase.Id = id;
            testCase.Name = Text(files, "===").TrimEnd('\r', '\n');
            testCase.InputYaml = Text(files, "in.yaml");
            testCase.ExpectedEvents = Text(files, "test.event");
            testCase.ExpectedJson = Text(files, "in.json");
            testCase.OutputYaml = Text(files, "out.yaml");
            testCase.EmitterYaml = Text(files, "emit.yaml");
            testCase.ShouldParse = !files.ContainsKey("error");
            cases.Add(testCase);
        }

        return cases;
    }

    public static void AddEntry(Dictionary<string, Dictionary<string, string>> filesByCase, string entryName, Stream data)
    {
        string normalized = entryName.Replace('\\', '/');
        int firstSlash = normalized.IndexOf('/');
        if (firstSlash < 0 || firstSlash + 1 >= normalized.Length)
        {
            return;
        }

        string relative = normalized.Substring(firstSlash + 1);
        string fileName = Path.GetFileName(relative);
        if (!IsFixtureFile(fileName))
        {
            return;
        }

        string? directory = Path.GetDirectoryName(relative);
        if (directory == null || directory == "" || directory == "tags")
        {
            return;
        }

        string id = directory.Replace('\\', '/');
        if (!filesByCase.TryGetValue(id, out Dictionary<string, string>? files))
        {
            files = new Dictionary<string, string>(StringComparer.Ordinal);
            filesByCase[id] = files;
        }

        using StreamReader reader = new StreamReader(data, System.Text.Encoding.UTF8, true, 1024, true);
        files[fileName] = reader.ReadToEnd();
    }

    public static bool IsFixtureFile(string name)
    {
        return name == "==="
            || name == "in.yaml"
            || name == "test.event"
            || name == "in.json"
            || name == "out.yaml"
            || name == "emit.yaml"
            || name == "error";
    }

    public static string Text(Dictionary<string, string> files, string name)
    {
        if (files.TryGetValue(name, out string? value))
        {
            return value;
        }

        return "";
    }
}

public static class ArchiveCache
{
    public static readonly IReadOnlyList<YamlTestSuiteCase> Cases = YamlTestSuiteArchive.ReadCases(YamlTestSuiteArchive.ArchivePath());
}
