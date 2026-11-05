namespace fix;

/// <summary>Generates deterministic C# models from selected canonical dictionary definitions.</summary>
public class CSharpModelGenerator : IModelGenerator
{
    /// <summary>Creates a C# model generator.</summary>
    public CSharpModelGenerator()
    {
    }

    /// <summary>Generates the selected models without writing caller files.</summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="options">The generation selection and naming options.</param>
    /// <returns>The generated files, diagnostics, and manifest.</returns>
    public GenerationResult GenerateModels(FixDictionary dictionary, ModelGenerationOptions options)
    {
        GenerationResult result = new GenerationResult();
        if (dictionary == null)
        {
            AddGeneratorDiagnostic(result, ValidationSeverity.Error, "missing-generation-dictionary", "The source dictionary is missing.", "/");
            return result;
        }
        if (options == null)
        {
            AddGeneratorDiagnostic(result, ValidationSeverity.Error, "missing-generation-options", "The model generation options are missing.", "/options");
            return result;
        }
        if (options.Target != GenerationTarget.CSharp)
        {
            AddGeneratorDiagnostic(result, ValidationSeverity.Error, "unsupported-generation-target", "The C# model generator only supports the CSharp target.", "/options/target");
            return result;
        }
        if (options.MaxFiles <= 0)
        {
            AddGeneratorDiagnostic(result, ValidationSeverity.Error, "invalid-generation-file-limit", "The maximum generated file count must be positive.", "/options/max-files");
            return result;
        }

        GeneratedApiPlan selection = GeneratedApiPlanner.PlanGeneration(dictionary, options);
        CopyGeneratorDiagnostics(result.Diagnostics, selection.Diagnostics);
        if (!selection.CanEmit) return result;
        CSharpNamePlan names = CSharpNameResolver.ResolveNames(selection.Dictionary, options);
        CSharpEmissionResult emitted = CSharpEmitter.EmitSources(selection.Dictionary, options, names);
        CopyGeneratorDiagnostics(result.Diagnostics, emitted.Diagnostics);
        if (emitted.Files.Count > options.MaxFiles)
        {
            AddGeneratorDiagnostic(result, ValidationSeverity.Error, "generation-file-limit-exceeded", "The selected model requires more files than the configured generation limit.", "/options/max-files");
            return result;
        }

        result.Files = emitted.Files;
        GenerationManifest.ApplyManifest(result.Files, options, result.Diagnostics);
        result.Manifest = GenerationManifest.CreateManifest(result.Files);
        result.Success = !HasGeneratorErrors(result.Diagnostics);
        return result;
    }

    internal static bool HasGeneratorErrors(System.Collections.Generic.List<GenerationDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }

    internal static void CopyGeneratorDiagnostics(System.Collections.Generic.List<GenerationDiagnostic> destination, System.Collections.Generic.List<GenerationDiagnostic> source)
    {
        for (int index = 0; index < source.Count; index++) destination.Add(source[index]);
    }

    internal static void AddGeneratorDiagnostic(GenerationResult result, ValidationSeverity severity, string code, string message, string source)
    {
        GenerationDiagnostic diagnostic = new GenerationDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.Source = source;
        result.Diagnostics.Add(diagnostic);
    }
}
