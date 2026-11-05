using System.Text.Json;

namespace UniversalHtml.FuzzRunner;

internal sealed class FuzzFailureOptions
{
    public int CaseTimeoutMilliseconds { get; set; }
    public int MaximumInputLength { get; set; }
    public int MaximumDepth { get; set; }
}

internal sealed class FuzzFailureArtifact
{
    public int SchemaVersion { get; set; }
    public string Target { get; set; }
    public string RootSeed { get; set; }
    public int CaseIndex { get; set; }
    public string CaseSeed { get; set; }
    public string FailureKind { get; set; }
    public string ExceptionType { get; set; }
    public string Message { get; set; }
    public string OriginalInput { get; set; }
    public string MinimizedInput { get; set; }
    public int ShrinkAttempts { get; set; }
    public string ReproductionCommand { get; set; }
    public FuzzFailureOptions Options { get; set; }

    public static FuzzFailureArtifact Create(FuzzOptions options,
        GeneratedCase generated, FuzzEvaluation original,
        ShrinkResult minimized, string relativeArtifactPath)
    {
        return new FuzzFailureArtifact
        {
            SchemaVersion = 1,
            Target = FuzzTargetNames.Format(generated.Target),
            RootSeed = FormatSeed(options.Seed),
            CaseIndex = generated.CaseIndex,
            CaseSeed = FormatSeed(generated.CaseSeed),
            FailureKind = minimized.Evaluation.FailureKind,
            ExceptionType = minimized.Evaluation.ExceptionType,
            Message = minimized.Evaluation.Message,
            OriginalInput = generated.Input,
            MinimizedInput = minimized.Input,
            ShrinkAttempts = minimized.Attempts,
            ReproductionCommand = "dotnet run --project tools/FuzzRunner -- --replay "
                + relativeArtifactPath.Replace('\\', '/'),
            Options = new FuzzFailureOptions
            {
                CaseTimeoutMilliseconds = options.CaseTimeoutMilliseconds,
                MaximumInputLength = options.MaximumInputLength,
                MaximumDepth = options.MaximumDepth
            }
        };
    }

    public static FuzzFailureArtifact Read(string path)
    {
        FuzzFailureArtifact artifact =
            JsonSerializer.Deserialize<FuzzFailureArtifact>(
                File.ReadAllText(path), JsonOptions());
        if (artifact == null || artifact.SchemaVersion != 1
            || artifact.Options == null
            || string.IsNullOrWhiteSpace(artifact.Target)
            || string.IsNullOrWhiteSpace(artifact.FailureKind)
            || artifact.MinimizedInput == null)
        {
            throw new InvalidDataException(
                "The fuzz failure artifact is missing or unsupported.");
        }

        return artifact;
    }

    public void Write(string path)
    {
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(this, JsonOptions()));
    }

    public static string FormatSeed(ulong seed)
    {
        return "0x" + seed.ToString("x16",
            System.Globalization.CultureInfo.InvariantCulture);
    }

    static JsonSerializerOptions JsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        };
    }
}
