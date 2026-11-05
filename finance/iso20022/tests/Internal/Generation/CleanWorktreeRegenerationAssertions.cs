using System.Security.Cryptography;
using iso20022;

namespace iso20022.Tests;

internal static class CleanWorktreeRegenerationAssertions
{
    internal static void AssertDeterministic(
        string generatorName,
        Func<bool, GenerationPlan> createPlan,
        Func<GenerationPlan, GenerationResult> generate)
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "iso20022-clean-regeneration-" + generatorName + "-" +
            Guid.NewGuid().ToString("N"));
        try
        {
            GenerationResult first = GenerateClean(root, false, createPlan, generate);
            Dictionary<string, byte[]> firstFiles = ReadOutputFiles(root);
            Directory.Delete(root, true);

            GenerationResult repeated = GenerateClean(root, false, createPlan, generate);
            Dictionary<string, byte[]> repeatedFiles = ReadOutputFiles(root);
            Directory.Delete(root, true);

            GenerationResult shuffled = GenerateClean(root, true, createPlan, generate);
            Dictionary<string, byte[]> shuffledFiles = ReadOutputFiles(root);

            AssertResultMatchesDisk(first, firstFiles);
            AssertResultMatchesDisk(repeated, repeatedFiles);
            AssertResultMatchesDisk(shuffled, shuffledFiles);
            AssertEquivalent(first, repeated);
            AssertEquivalent(first, shuffled);
            AssertFileSetsEqual(firstFiles, repeatedFiles);
            AssertFileSetsEqual(firstFiles, shuffledFiles);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }

    private static GenerationResult GenerateClean(
        string root,
        bool shuffled,
        Func<bool, GenerationPlan> createPlan,
        Func<GenerationPlan, GenerationResult> generate)
    {
        Assert.That(Directory.Exists(root), Is.False);
        GenerationPlan plan = createPlan(shuffled);
        plan.Options.OutputRootPath = root;
        GenerationResult result = generate(plan);
        Assert.That(result.Success, Is.True, result.ErrorMessage);
        return result;
    }

    private static Dictionary<string, byte[]> ReadOutputFiles(string root)
    {
        return Directory.GetFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(
                path => Path.GetRelativePath(root, path)
                    .Replace(Path.DirectorySeparatorChar, '/'),
                File.ReadAllBytes,
                StringComparer.Ordinal);
    }

    private static void AssertResultMatchesDisk(
        GenerationResult result,
        Dictionary<string, byte[]> diskFiles)
    {
        string[] generatedPaths = result.Files
            .Select(file => file.RelativePath)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] diskPaths = diskFiles.Keys
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.That(diskPaths, Is.EqualTo(generatedPaths));
        foreach (GeneratedFile file in result.Files)
        {
            Assert.Multiple(() =>
            {
                Assert.That(diskFiles[file.RelativePath], Is.EqualTo(file.Content),
                    file.RelativePath);
                Assert.That(file.HashAlgorithm, Is.EqualTo("SHA-256"),
                    file.RelativePath);
                Assert.That(file.Hash.Value, Is.EqualTo(SHA256.HashData(file.Content)),
                    file.RelativePath);
            });
        }
    }

    private static void AssertEquivalent(
        GenerationResult expected,
        GenerationResult actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Files.Select(file => file.RelativePath),
                Is.EqualTo(expected.Files.Select(file => file.RelativePath)));
            Assert.That(actual.Files.Select(file => file.Content),
                Is.EqualTo(expected.Files.Select(file => file.Content)));
            Assert.That(actual.Files.Select(file => file.Hash.Value),
                Is.EqualTo(expected.Files.Select(file => file.Hash.Value)));
            Assert.That(actual.Manifest.SettingsHash.Value,
                Is.EqualTo(expected.Manifest.SettingsHash.Value));
            Assert.That(actual.Manifest.OutputHash.Value,
                Is.EqualTo(expected.Manifest.OutputHash.Value));
            Assert.That(actual.Manifest.ManifestHash.Value,
                Is.EqualTo(expected.Manifest.ManifestHash.Value));
            Assert.That(actual.Manifest.Files.Select(file => file.RelativePath),
                Is.EqualTo(expected.Manifest.Files.Select(file => file.RelativePath)));
            Assert.That(actual.Manifest.Files.Select(file => file.Hash.Value),
                Is.EqualTo(expected.Manifest.Files.Select(file => file.Hash.Value)));
        });
    }

    private static void AssertFileSetsEqual(
        Dictionary<string, byte[]> expected,
        Dictionary<string, byte[]> actual)
    {
        string[] expectedPaths = expected.Keys
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        string[] actualPaths = actual.Keys
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.That(actualPaths, Is.EqualTo(expectedPaths));
        foreach (string path in expectedPaths)
        {
            Assert.That(actual[path], Is.EqualTo(expected[path]), path);
        }
    }
}
