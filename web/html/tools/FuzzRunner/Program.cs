using System.Globalization;

namespace UniversalHtml.FuzzRunner;

internal static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            FuzzOptions options = FuzzOptions.Parse(args);
            if (options.ShowHelp)
            {
                WriteHelp();
                return 0;
            }

            string repositoryRoot = FindRepositoryRoot();
            if (options.ReplayPath != null)
            {
                return Replay(repositoryRoot, options.ReplayPath);
            }

            return Run(repositoryRoot, options);
        }
        catch (FuzzUsageException exception)
        {
            Console.Error.WriteLine("FuzzRunner: " + exception.Message);
            Console.Error.WriteLine("Run with --help for usage.");
            return 2;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("FuzzRunner: " + exception.Message);
            return 2;
        }
    }

    static int Run(string repositoryRoot, FuzzOptions options)
    {
        FuzzTarget[] targets = options.GetTargets();
        FuzzBudget budget = new FuzzBudget(options.WorkBudget,
            TimeSpan.FromMilliseconds(options.TimeBudgetMilliseconds));
        FuzzExecutor executor = new FuzzExecutor(
            options.CaseTimeoutMilliseconds, options.MaximumInputLength);
        int executed = 0;
        for (int index = 0; index < options.Cases && budget.IsAvailable();
            index++)
        {
            FuzzTarget target = targets[index % targets.Length];
            ulong caseSeed = DeterministicRandom.DeriveSeed(options.Seed,
                target, index);
            GeneratedCase generated = FuzzGenerators.Generate(target, index,
                caseSeed, budget, options.MaximumInputLength,
                options.MaximumDepth);
            if (!budget.TrySpend(1))
            {
                break;
            }

            FuzzEvaluation evaluation = executor.Evaluate(target,
                generated.Input);
            executed++;
            if (!evaluation.Failed)
            {
                continue;
            }

            ShrinkResult minimized = options.Shrink
                ? FuzzShrinker.Shrink(generated.Input, evaluation,
                    value => executor.Evaluate(target, value), budget,
                    options.ShrinkSteps)
                : new ShrinkResult(generated.Input, evaluation, 0);
            string artifactPath = CreateArtifactPath(repositoryRoot, options,
                generated);
            string relativePath = Path.GetRelativePath(repositoryRoot,
                artifactPath);
            FuzzFailureArtifact artifact = FuzzFailureArtifact.Create(options,
                generated, evaluation, minimized, relativePath);
            artifact.Write(artifactPath);
            Console.Error.WriteLine("Fuzz failure: target="
                + FuzzTargetNames.Format(target) + ", case=" + index
                + ", seed=" + FuzzFailureArtifact.FormatSeed(caseSeed)
                + ", kind=" + minimized.Evaluation.FailureKind);
            Console.Error.WriteLine("Reproducer: " + relativePath);
            return 1;
        }

        Console.WriteLine("Fuzz run completed: seed="
            + FuzzFailureArtifact.FormatSeed(options.Seed) + ", cases="
            + executed.ToString(CultureInfo.InvariantCulture) + "/"
            + options.Cases.ToString(CultureInfo.InvariantCulture)
            + ", work=" + budget.GetUsedWork().ToString(
                CultureInfo.InvariantCulture) + "/"
            + options.WorkBudget.ToString(CultureInfo.InvariantCulture)
            + ", elapsed-ms=" + ((long)budget.GetElapsed().TotalMilliseconds)
                .ToString(CultureInfo.InvariantCulture) + ".");
        if (executed < options.Cases)
        {
            Console.WriteLine("The configured time or work budget stopped "
                + "generation before the requested case count.");
        }

        return 0;
    }

    static int Replay(string repositoryRoot, string replayPath)
    {
        string path = Path.GetFullPath(Path.IsPathRooted(replayPath)
            ? replayPath : Path.Combine(repositoryRoot, replayPath));
        FuzzFailureArtifact artifact = FuzzFailureArtifact.Read(path);
        FuzzTarget target = FuzzTargetNames.Parse(artifact.Target);
        FuzzExecutor executor = new FuzzExecutor(
            artifact.Options.CaseTimeoutMilliseconds,
            artifact.Options.MaximumInputLength);
        FuzzEvaluation evaluation = executor.Evaluate(target,
            artifact.MinimizedInput);
        bool reproduced = evaluation.Failed
            && evaluation.FailureKind == artifact.FailureKind;
        Console.WriteLine(reproduced
            ? "Failure reproduced: " + artifact.FailureKind
            : "Failure did not reproduce; observed: "
                + (evaluation.Failed ? evaluation.FailureKind : "success"));
        return reproduced ? 1 : 2;
    }

    static string CreateArtifactPath(string repositoryRoot,
        FuzzOptions options, GeneratedCase generated)
    {
        string directory = Path.GetFullPath(Path.IsPathRooted(
            options.OutputDirectory) ? options.OutputDirectory
            : Path.Combine(repositoryRoot, options.OutputDirectory));
        string name = FuzzTargetNames.Format(generated.Target) + "-"
            + generated.CaseSeed.ToString("x16",
                CultureInfo.InvariantCulture) + "-"
            + generated.CaseIndex.ToString("D6",
                CultureInfo.InvariantCulture) + ".json";
        return Path.Combine(directory, name);
    }

    static string FindRepositoryRoot()
    {
        DirectoryInfo directory = new DirectoryInfo(
            Directory.GetCurrentDirectory());
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "todo.md"))
                && Directory.Exists(Path.Combine(directory.FullName, "src")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not find the repository root.");
    }

    static void WriteHelp()
    {
        Console.WriteLine("UniversalHtml deterministic grammar fuzz runner");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  FuzzRunner [options]");
        Console.WriteLine("  FuzzRunner --replay <failure.json>");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --target <all|html|selector|css|url>");
        Console.WriteLine("  --seed <decimal|0xhex>       Root seed");
        Console.WriteLine("  --cases <count>              Total generated cases");
        Console.WriteLine("  --work-budget <count>        Grammar/evaluation work units");
        Console.WriteLine("  --time-budget-ms <ms>        Overall wall-time budget");
        Console.WriteLine("  --case-timeout-ms <ms>       Per-case deadline");
        Console.WriteLine("  --max-input-length <chars>   Generated input bound");
        Console.WriteLine("  --max-depth <depth>          Grammar recursion bound");
        Console.WriteLine("  --shrink-steps <count>       Deletion attempts per failure");
        Console.WriteLine("  --no-shrink                  Disable deletion shrinking");
        Console.WriteLine("  --output <directory>         Failure artifact directory");
        Console.WriteLine("  --help                       Show this help");
    }
}
