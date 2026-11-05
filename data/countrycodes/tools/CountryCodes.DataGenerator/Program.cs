using System;
using System.Collections.Generic;
using System.IO;
using CountryCodes.DataGenerator.Emission;
using CountryCodes.DataGenerator.Overrides;
using CountryCodes.DataGenerator.Reconciliation;
using CountryCodes.DataGenerator.Staging;

namespace CountryCodes.DataGenerator;

internal static class Program
{
    private const string VerifyCommand = "verify-acquisition";
    private const string ParseCommand = "parse-sources";
    private const string StageCommand = "stage-sources";
    private const string ReconcileCommand = "reconcile-sources";
    private const string OverrideCommand = "process-overrides";
    private const string EmitCountryCommand = "emit-country-rows";
    private const string VerifyGeneratedCommand = "verify-generated";

    private static int Main(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            WriteUsage(Console.Error);
            return 2;
        }

        if (arguments.Length == 1 &&
            (string.Equals(arguments[0], "--help", StringComparison.Ordinal) ||
             string.Equals(arguments[0], "-h", StringComparison.Ordinal)))
        {
            WriteUsage(Console.Out);
            return 0;
        }

        bool isVerifyCommand = string.Equals(arguments[0], VerifyCommand, StringComparison.Ordinal);
        bool isParseCommand = string.Equals(arguments[0], ParseCommand, StringComparison.Ordinal);
        bool isStageCommand = string.Equals(arguments[0], StageCommand, StringComparison.Ordinal);
        bool isReconcileCommand = string.Equals(
            arguments[0],
            ReconcileCommand,
            StringComparison.Ordinal);
        bool isOverrideCommand = string.Equals(
            arguments[0],
            OverrideCommand,
            StringComparison.Ordinal);
        bool isEmitCountryCommand = string.Equals(
            arguments[0],
            EmitCountryCommand,
            StringComparison.Ordinal);
        bool isVerifyGeneratedCommand = string.Equals(
            arguments[0],
            VerifyGeneratedCommand,
            StringComparison.Ordinal);
        if (!isVerifyCommand &&
            !isParseCommand &&
            !isStageCommand &&
            !isReconcileCommand &&
            !isOverrideCommand &&
            !isEmitCountryCommand &&
            !isVerifyGeneratedCommand)
        {
            Console.Error.WriteLine($"Unknown command '{arguments[0]}'.");
            WriteUsage(Console.Error);
            return 2;
        }

        try
        {
            CommandOptions options = ParseOptions(arguments);
            VerificationSummary summary = AcquisitionVerifier.Verify(
                options.RepositoryRoot,
                options.ManifestPath);

            WriteVerificationSummary(summary);
            if (isParseCommand ||
                isStageCommand ||
                isReconcileCommand ||
                isOverrideCommand ||
                isEmitCountryCommand ||
                isVerifyGeneratedCommand)
            {
                RawSourceData data = SourceDatasetParser.Parse(options.RepositoryRoot);
                WriteParseSummary(data);

                if (isStageCommand ||
                    isReconcileCommand ||
                    isOverrideCommand ||
                    isEmitCountryCommand ||
                    isVerifyGeneratedCommand)
                {
                    SourceManifestInfo manifest = SourceManifestInfo.Load(options.ManifestPath);
                    GeneratorStagingData staging = GeneratorStager.Stage(data, manifest);
                    WriteStagingSummary(staging);

                    if (isReconcileCommand ||
                        isOverrideCommand ||
                        isEmitCountryCommand ||
                        isVerifyGeneratedCommand)
                    {
                        OverrideCatalog overrides = OverrideCatalogLoader.Load(
                            options.OverridesPath,
                            manifest);
                        ReconciliationResult result = SourceReconciler.Reconcile(
                            staging,
                            overrides);
                        WriteReconciliationSummary(result);

                        if (isOverrideCommand ||
                            isEmitCountryCommand ||
                            isVerifyGeneratedCommand)
                        {
                            OverrideApplicationResult applied = OverrideProcessor.Apply(
                                overrides,
                                staging,
                                result);
                            if (isOverrideCommand)
                            {
                                WriteOverrideSummary(applied);
                            }

                            if (isEmitCountryCommand)
                            {
                                CountryRowEmissionSummary emission = CountryRowEmitter.Emit(
                                    applied,
                                    manifest,
                                    options.OutputPath);
                                WriteEmissionSummary(emission);
                            }

                            if (isVerifyGeneratedCommand)
                            {
                                GeneratedSnapshotVerificationSummary verification =
                                    GeneratedSnapshotVerifier.Verify(
                                        applied,
                                        manifest,
                                        options.OutputPath);
                                WriteGeneratedVerificationSummary(verification);
                            }
                        }
                    }
                }
            }

            return 0;
        }
        catch (VerificationException exception)
        {
            Console.Error.WriteLine("Acquisition verification failed:");
            foreach (string issue in exception.Issues)
            {
                Console.Error.WriteLine($"- {issue}");
            }

            return 1;
        }
        catch (ArgumentException exception)
        {
            Console.Error.WriteLine(exception.Message);
            WriteUsage(Console.Error);
            return 2;
        }
        catch (SourceParseException exception)
        {
            Console.Error.WriteLine($"Source parsing failed: {exception.Message}");
            return 1;
        }
        catch (StagingException exception)
        {
            Console.Error.WriteLine($"Source staging failed: {exception.Message}");
            return 1;
        }
        catch (ReconciliationException exception)
        {
            Console.Error.WriteLine("Source reconciliation failed:");
            foreach (string issue in exception.Issues)
            {
                Console.Error.WriteLine($"- {issue}");
            }

            return 1;
        }
        catch (OverrideException exception)
        {
            Console.Error.WriteLine("Override processing failed:");
            foreach (string issue in exception.Issues)
            {
                Console.Error.WriteLine($"- {issue}");
            }

            return 1;
        }
        catch (EmissionException exception)
        {
            Console.Error.WriteLine($"Generated-data emission failed: {exception.Message}");
            return 1;
        }
        catch (GeneratedSnapshotVerificationException exception)
        {
            Console.Error.WriteLine($"Generated snapshot verification failed: {exception.Message}");
            return 1;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"Acquisition verification failed: {exception.Message}");
            return 1;
        }
    }

    private static CommandOptions ParseOptions(string[] arguments)
    {
        string? repositoryRoot = null;
        string? manifestPath = null;
        string? overridesPath = null;
        string? outputPath = null;

        int index = 1;
        while (index < arguments.Length)
        {
            string option = arguments[index];
            if (string.Equals(option, "--repository-root", StringComparison.Ordinal))
            {
                repositoryRoot = ReadOptionValue(arguments, index, option);
                index += 2;
                continue;
            }

            if (string.Equals(option, "--manifest", StringComparison.Ordinal))
            {
                manifestPath = ReadOptionValue(arguments, index, option);
                index += 2;
                continue;
            }

            if (string.Equals(option, "--overrides", StringComparison.Ordinal))
            {
                overridesPath = ReadOptionValue(arguments, index, option);
                index += 2;
                continue;
            }

            if (string.Equals(option, "--output", StringComparison.Ordinal))
            {
                outputPath = ReadOptionValue(arguments, index, option);
                index += 2;
                continue;
            }

            throw new ArgumentException($"Unknown option '{option}'.");
        }

        string resolvedRoot = repositoryRoot == null
            ? FindRepositoryRoot(Directory.GetCurrentDirectory())
            : Path.GetFullPath(repositoryRoot);

        string resolvedManifest = manifestPath == null
            ? Path.Combine(resolvedRoot, "data", "source-manifest.json")
            : ResolveManifestPath(resolvedRoot, manifestPath);
        string resolvedOverrides = overridesPath == null
            ? Path.Combine(resolvedRoot, "data", "overrides.json")
            : ResolveManifestPath(resolvedRoot, overridesPath);
        string resolvedOutput = outputPath == null
            ? Path.Combine(resolvedRoot, "src", "Generated")
            : ResolveManifestPath(resolvedRoot, outputPath);

        return new CommandOptions(
            resolvedRoot,
            resolvedManifest,
            resolvedOverrides,
            resolvedOutput);
    }

    private static string ReadOptionValue(string[] arguments, int optionIndex, string option)
    {
        int valueIndex = optionIndex + 1;
        if (valueIndex >= arguments.Length || arguments[valueIndex].Length == 0)
        {
            throw new ArgumentException($"Option '{option}' requires a value.");
        }

        return arguments[valueIndex];
    }

    private static string FindRepositoryRoot(string startingDirectory)
    {
        DirectoryInfo? directory = new DirectoryInfo(Path.GetFullPath(startingDirectory));
        while (directory != null)
        {
            string manifest = Path.Combine(directory.FullName, "data", "source-manifest.json");
            string sourceProject = Path.Combine(directory.FullName, "src", "countrycodes.csproj");
            if (File.Exists(manifest) && File.Exists(sourceProject))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new ArgumentException(
            "Could not find the repository root. Use --repository-root to specify it explicitly.");
    }

    private static string ResolveManifestPath(string repositoryRoot, string manifestPath)
    {
        if (Path.IsPathRooted(manifestPath))
        {
            return Path.GetFullPath(manifestPath);
        }

        return Path.GetFullPath(Path.Combine(repositoryRoot, manifestPath));
    }

    private static void WriteUsage(TextWriter writer)
    {
        writer.WriteLine("CountryCodes.DataGenerator");
        writer.WriteLine();
        writer.WriteLine("Usage:");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "verify-acquisition [--repository-root <path>] [--manifest <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "parse-sources [--repository-root <path>] [--manifest <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "stage-sources [--repository-root <path>] [--manifest <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "reconcile-sources [--repository-root <path>] [--manifest <path>] " +
            "[--overrides <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "process-overrides [--repository-root <path>] [--manifest <path>] " +
            "[--overrides <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "emit-country-rows [--repository-root <path>] [--manifest <path>] " +
            "[--overrides <path>] [--output <path>]");
        writer.WriteLine(
            "  dotnet run --project tools/CountryCodes.DataGenerator -- " +
            "verify-generated [--repository-root <path>] [--manifest <path>] " +
            "[--overrides <path>] [--output <checked-in-generated-path>]");
        writer.WriteLine();
        writer.WriteLine(
            "The command reads only pinned local files, validates the fail-closed manifest, " +
            "and verifies SHA-256 digests. No command downloads source datasets. " +
            "Parsing, staging, and reconciliation occur only after verification succeeds.");
    }

    private static void WriteVerificationSummary(VerificationSummary summary)
    {
        Console.Out.WriteLine(
            $"Verified {summary.FileCount} pinned files from {summary.SourceCount} sources " +
            $"({summary.TotalBytes} bytes) using SHA-256.");
    }

    private static void WriteParseSummary(RawSourceData data)
    {
        int territoryNameCount = 0;
        int subdivisionNameCount = 0;
        foreach (CldrNameRecord name in data.Cldr.Names)
        {
            if (name.Target == CldrNameTarget.Territory)
            {
                territoryNameCount += 1;
            }
            else
            {
                subdivisionNameCount += 1;
            }
        }

        int territoryAliasCount = 0;
        int subdivisionAliasCount = 0;
        foreach (CldrAliasRecord alias in data.Cldr.Aliases)
        {
            if (alias.Target == CldrAliasTarget.Territory)
            {
                territoryAliasCount += 1;
            }
            else
            {
                subdivisionAliasCount += 1;
            }
        }

        Console.Out.WriteLine(
            $"Parsed Debian: countries={data.Debian.Countries.Count}, " +
            $"subdivisions={data.Debian.Subdivisions.Count}, " +
            $"historical={data.Debian.HistoricalCountries.Count}.");
        Console.Out.WriteLine(
            $"Parsed UN M49: localized-countries={data.UnM49.Countries.Count}, " +
            $"history={data.UnM49.History.Count}, " +
            $"localized-hierarchy={data.UnM49.Hierarchy.Count}.");
        Console.Out.WriteLine(
            $"Parsed CLDR ({data.Cldr.Locale}): territory-names={territoryNameCount}, " +
            $"subdivision-names={subdivisionNameCount}, territory-aliases={territoryAliasCount}, " +
            $"subdivision-aliases={subdivisionAliasCount}, " +
            $"containment={data.Cldr.Containment.Count}.");
        Console.Out.WriteLine(
            $"Parsed IANA registry {data.Iana.FileDate}: records={data.Iana.Records.Count}.");
    }

    private static void WriteStagingSummary(GeneratorStagingData data)
    {
        Console.Out.WriteLine(
            $"Staged normalized rows: countries={data.Countries.Count}, " +
            $"subdivisions={data.Subdivisions.Count}, " +
            $"historical={data.HistoricalCountries.Count}, names={data.Names.Count}, " +
            $"aliases={data.Aliases.Count}, containment={data.Containment.Count}, " +
            $"m49-history={data.M49History.Count}, " +
            $"m49-hierarchy={data.M49Hierarchy.Count}, iana={data.IanaRecords.Count}.");
        Console.Out.WriteLine(
            "Validated field-level provenance and lossless display and numeric values.");
    }

    private static void WriteReconciliationSummary(ReconciliationResult result)
    {
        Console.Out.WriteLine(
            $"Reconciled sources: country-cross-checks={result.CountryCrossCheckCount}, " +
            $"subdivision-parent-cross-checks={result.SubdivisionParentCrossCheckCount}, " +
            $"region-nodes={result.RegionNodeCount}, transitions={result.TransitionCount}, " +
            $"locales={result.LocaleCount}, explained-deltas={result.SourceDeltas.Count}.");
        foreach (ExplainedSourceDelta delta in result.SourceDeltas)
        {
            Console.Out.WriteLine(
                $"Explained delta {delta.SourceId}:{delta.UpstreamKey} " +
                $"({delta.FieldGroup}): {delta.Explanation}");
        }
    }

    private static void WriteOverrideSummary(OverrideApplicationResult result)
    {
        int sourceDeltaCount = 0;
        int supplementalCount = 0;
        foreach (AppliedOverride applied in result.AppliedOverrides)
        {
            if (string.Equals(
                    applied.Definition.Kind,
                    "source-delta",
                    StringComparison.Ordinal))
            {
                sourceDeltaCount += 1;
            }
            else
            {
                supplementalCount += 1;
            }
        }

        Console.Out.WriteLine(
            $"Processed reviewed overrides: total={result.AppliedOverrides.Count}, " +
            $"source-deltas={sourceDeltaCount}, supplements={supplementalCount}; " +
            "validated deterministic precedence and source-revision expiry conditions.");
    }

    private static void WriteEmissionSummary(CountryRowEmissionSummary summary)
    {
        Console.Out.WriteLine(
            $"Emitted deterministic generated rows: countries={summary.CountryCount}, " +
            $"reserved-or-former={summary.ReservedCodeCount}, " +
            $"user-assigned-ranges={summary.UserAssignedRangeCount}, " +
            $"country-names={summary.CountryNameCount}, " +
            $"subdivisions={summary.SubdivisionCount}, " +
            $"subdivision-names={summary.SubdivisionNameCount}, " +
            $"subdivision-aliases={summary.SubdivisionAliasCount}, " +
            $"subdivision-replacements={summary.SubdivisionReplacementCount}, " +
            $"historical-countries={summary.HistoricalCountryCount}, " +
            $"country-changes={summary.CountryChangeCount}, " +
            $"predecessors={summary.PredecessorLinkCount}, " +
            $"successors={summary.SuccessorLinkCount}, " +
            $"geographic-regions={summary.GeographicRegionCount}, " +
            $"region-names={summary.GeographicRegionNameCount}, " +
            $"country-region-mappings={summary.CountryRegionMappingCount}, " +
            $"exact-indexes={summary.ExactIndexCount}, " +
            $"search-indexes={summary.SearchIndexCount}, " +
            $"source-revisions={summary.SourceRevisionCount}, " +
            $"content-hashes={summary.ContentHashCount}.");
        Console.Out.WriteLine(
            $"Aggregate generated-content SHA-256: {summary.AggregateContentChecksum}.");
        Console.Out.WriteLine($"Wrote {summary.CountryTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.ReservedCodeTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.CountryNameTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.SubdivisionTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.SubdivisionNameTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.HistoricalCountryTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.CountryChangeTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.GeographicRegionTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.GeneratedIndexTablePath}.");
        Console.Out.WriteLine($"Wrote {summary.GeneratedMetadataPath}.");
    }

    private static void WriteGeneratedVerificationSummary(
        GeneratedSnapshotVerificationSummary summary)
    {
        Console.Out.WriteLine(
            $"Verified {summary.FileCount} checked-in generated files across " +
            $"{summary.CultureCount} cultures and {summary.GenerationCount} repeated runs; " +
            "all bytes match.");
    }

    private sealed class CommandOptions
    {
        internal CommandOptions(
            string repositoryRoot,
            string manifestPath,
            string overridesPath,
            string outputPath)
        {
            RepositoryRoot = repositoryRoot;
            ManifestPath = manifestPath;
            OverridesPath = overridesPath;
            OutputPath = outputPath;
        }

        internal string RepositoryRoot { get; }

        internal string ManifestPath { get; }

        internal string OverridesPath { get; }

        internal string OutputPath { get; }
    }
}
