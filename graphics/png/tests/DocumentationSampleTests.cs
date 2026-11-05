using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace png.Tests;

[TestFixture]
public class DocumentationSampleTests
{
    [Test]
    public void Readme_FeatureMatrixListsFirstMilestoneAreas()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));

        Assert.That(readme, Does.Contain("## Feature Matrix"));
        Assert.That(readme, Does.Contain("| Area | First milestone status | Main APIs |"));
        Assert.That(readme, Does.Contain("| Metadata-only identify |"));
        Assert.That(readme, Does.Contain("`IPngIdentifier`, `PngIdentifierService`"));
        Assert.That(readme, Does.Contain("| Validation and inspection |"));
        Assert.That(readme, Does.Contain("`IPngValidator`, `PngValidationService`"));
        Assert.That(readme, Does.Contain("| PNG decode |"));
        Assert.That(readme, Does.Contain("`IPngDecoder`, `PngCodec`"));
        Assert.That(readme, Does.Contain("| PNG encode |"));
        Assert.That(readme, Does.Contain("`IPngEncoder`, `PngCodec`, `PngEncoderOptions`"));
        Assert.That(readme, Does.Contain("| APNG |"));
        Assert.That(readme, Does.Contain("`PngCodec`, `PngAnimation`, `PngFrame`"));
        Assert.That(readme, Does.Contain("| Optimization helpers |"));
        Assert.That(readme, Does.Contain("`IPngOptimizer`, `PngOptimizationService`"));
        Assert.That(readme, Does.Contain("| Runtime dependency profile |"));
        Assert.That(readme, Does.Contain("`src/png.csproj`"));
    }

    [Test]
    public void Readme_CommandLineSampleCompilesAgainstLibraryApi()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));
        string sample = ExtractCodeBlock(readme, "Use this `Program.cs`:", "csharp");

        AssertSampleCompiles(sample, "ReadmeCommandLineSample", OutputKind.ConsoleApplication);
        Assert.That(sample, Does.Contain("PngIdentifierService"));
        Assert.That(sample, Does.Contain("PngValidationService"));
        Assert.That(sample, Does.Contain("codec.DecodeBytes"));
        Assert.That(sample, Does.Contain("codec.EncodeToBytes"));
        Assert.That(sample, Does.Contain("PngEncoderOptions"));
    }

    [Test]
    public void Readme_ApiUsageExamplesCompileAndCoverMainWorkflows()
    {
        string readme = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "README.md"));
        string sample = ExtractCodeBlock(readme, "Use these focused workflow examples:", "csharp");

        AssertSampleCompiles(sample, "ReadmeApiUsageExamples", OutputKind.DynamicallyLinkedLibrary);
        Assert.That(sample, Does.Contain("public static PngIdentifyResult Identify"));
        Assert.That(sample, Does.Contain("PngIdentifierService"));
        Assert.That(sample, Does.Contain("public static PngImage Decode"));
        Assert.That(sample, Does.Contain("PngCodec"));
        Assert.That(sample, Does.Contain("public static byte[] Encode()"));
        Assert.That(sample, Does.Contain("EncodeToBytes"));
        Assert.That(sample, Does.Contain("public static byte[] EditMetadata"));
        Assert.That(sample, Does.Contain("editor.SetGammaValue"));
        Assert.That(sample, Does.Contain("editor.ReplaceTextModels"));
        Assert.That(sample, Does.Contain("public static byte[] EncodeApng"));
        Assert.That(sample, Does.Contain("options.EncodeAnimation = true"));
        Assert.That(sample, Does.Contain("public static byte[] Transform"));
        Assert.That(sample, Does.Contain("transformer.Crop"));
        Assert.That(sample, Does.Contain("transformer.StripAlphaIfOpaque"));
        Assert.That(sample, Does.Contain("public static PngValidationResult Validate"));
        Assert.That(sample, Does.Contain("validator.ValidateBytes"));
        Assert.That(sample, Does.Contain("validator.ValidateTolerantRepairBytes"));
        Assert.That(sample, Does.Contain("public static PngOptimizerReport Optimize"));
        Assert.That(sample, Does.Contain("optimizer.RunCompressionTrialsWithOptions"));
        Assert.That(sample, Does.Contain("optimizer.RunFilterTrialsWithOptions"));
        Assert.That(sample, Does.Contain("optimizer.CreateReportFromBytes"));
    }

    static void AssertSampleCompiles(string sample, string assemblyName, OutputKind outputKind)
    {
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sample, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Latest));
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName,
            new SyntaxTree[] { syntaxTree },
            CreateMetadataReferences(),
            new CSharpCompilationOptions(outputKind));

        Diagnostic[] errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        Assert.That(errors, Is.Empty, string.Join("\n", errors.Select(error => error.ToString())));
    }

    static MetadataReference[] CreateMetadataReferences()
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .ToArray();
        MetadataReference[] references = new MetadataReference[assemblies.Length];
        int index = 0;
        while (index < assemblies.Length)
        {
            references[index] = MetadataReference.CreateFromFile(assemblies[index].Location);
            index += 1;
        }

        return references;
    }

    static string ExtractCodeBlock(string markdown, string marker, string language)
    {
        int markerIndex = markdown.IndexOf(marker, StringComparison.Ordinal);
        Assert.That(markerIndex, Is.GreaterThanOrEqualTo(0), marker);

        string fence = "```" + language;
        int fenceStart = markdown.IndexOf(fence, markerIndex, StringComparison.Ordinal);
        Assert.That(fenceStart, Is.GreaterThanOrEqualTo(0), fence);
        int codeStart = fenceStart + fence.Length;
        if (codeStart < markdown.Length && markdown[codeStart] == '\r')
        {
            codeStart += 1;
        }

        if (codeStart < markdown.Length && markdown[codeStart] == '\n')
        {
            codeStart += 1;
        }

        int fenceEnd = markdown.IndexOf("```", codeStart, StringComparison.Ordinal);
        Assert.That(fenceEnd, Is.GreaterThanOrEqualTo(0), "closing fence");
        return markdown.Substring(codeStart, fenceEnd - codeStart);
    }

    static string FindRepositoryRoot()
    {
        string? directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "png.slnx")) && Directory.Exists(Path.Combine(directory, "src")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }
}
