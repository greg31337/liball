using System.Collections.Generic;
using System.Security.Cryptography;

namespace iso20022;

/// <summary>Generates deterministic restricted-syntax C# data models from selected repository concepts.</summary>
public class CSharpModelGenerator : ICSharpModelGenerator
{
    /// <summary>Initializes a deterministic logical-model-to-C# generator.</summary>
    public CSharpModelGenerator()
    {
    }

    /// <summary>Generates the selected C# files and their ownership manifest.</summary>
    /// <param name="plan">The exact sources, selection, settings, and previous ownership state.</param><returns>The exception-less deterministic generation result.</returns>
    public GenerationResult GenerateCSharpModels(GenerationPlan? plan)
    {
        XsdGenerator planner = new XsdGenerator();
        if (plan == null)
        {
            return planner.CreateGenerationFailure(
                null,
                "generation.plan.required",
                "A generation plan is required.");
        }
        GenerationResult? invalid = planner.ValidateGenerationPlan(plan);
        if (invalid != null)
        {
            return invalid;
        }

        GenerationPlan selectedPlan = plan;
        GenerationResult result = new GenerationResult();
        result.Plan = selectedPlan;
        planner.InitializeManifest(result, selectedPlan);
        result.Manifest.GeneratorIdentifier = "iso20022.csharp-models";
        List<XsdGenerationUnit> units = planner.BuildGenerationUnits(selectedPlan);
        if (units.Count == 0)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.csharp.selection_empty",
                "The generation plan did not select any repository message or XSD artifact.");
        }
        planner.SortGenerationUnits(units);
        CSharpGenerationContext context = CSharpModelEmitter.CreateCSharpContext(
            selectedPlan,
            units,
            result);
        if (context.Symbols.Symbols.Count == 0)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.csharp.model_empty",
                "The selected logical model does not contain a C# data type.");
        }
        if (context.Symbols.Symbols.Count > selectedPlan.Options.Limits.MaxCollectionCount ||
            context.Symbols.Symbols.Count > selectedPlan.Options.Limits.MaxElementCount)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.csharp.model_limit",
                "The selected logical model exceeds the configured generation limits.");
        }

        List<CSharpGeneratedSource> sources = CSharpModelEmitter.EmitCSharpSources(context);
        sources.AddRange(CSharpMetadataEmitter.EmitMetadataSources(context));
        CSharpModelEmitter.SortGeneratedSources(sources);
        if (sources.Count > selectedPlan.Options.Limits.MaxCollectionCount ||
            sources.Count > selectedPlan.Options.Limits.MaxElementCount)
        {
            return planner.CreateGenerationFailure(
                selectedPlan,
                "generation.csharp.file_limit",
                "The selected output exceeds the configured generated-file limit.");
        }
        long totalBytes = 0;
        int index = 0;
        while (index < sources.Count)
        {
            CSharpGeneratedSource source = sources[index];
            totalBytes += source.Content.Length;
            if (source.Content.Length > selectedPlan.Options.Limits.MaxTotalBytes ||
                totalBytes > selectedPlan.Options.Limits.MaxTotalBytes)
            {
                return planner.CreateGenerationFailure(
                    selectedPlan,
                    "generation.csharp.byte_limit",
                    "The generated output exceeds the configured total-byte limit.");
            }
            GeneratedFile file = CreateCSharpGeneratedFile(source);
            result.Files.Add(file);
            result.Manifest.Files.Add(file);
            index++;
        }
        GenerationManifestEmitter.AddCSharpMappings(context, result.Files, result.Manifest);
        GenerationManifestEmitter.CompleteManifest(result);
        if (!IncrementalGenerationWriter.ApplyOutput(result))
        {
            return result;
        }
        result.Success = true;
        return result;
    }

    internal GeneratedFile CreateCSharpGeneratedFile(CSharpGeneratedSource source)
    {
        GeneratedFile file = new GeneratedFile();
        file.Kind = source.Kind;
        file.RelativePath = source.RelativePath;
        file.MediaType = Iso20022MediaTypes.CSharp;
        file.NamespaceName = source.NamespaceName;
        file.Content = source.Content;
        file.Hash.Value = SHA256.HashData(source.Content);
        file.Hash.Encoding = BinaryEncodingKind.Hexadecimal;
        file.HashAlgorithm = "SHA-256";
        if (source.SourceIdentifier.Length > 0)
        {
            file.SourceIdentifiers.Add(source.SourceIdentifier);
        }
        return file;
    }
}
