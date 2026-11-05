namespace asn1;

internal class CSharpGenerator : IAsnCodeGenerator
{
    public AsnGeneratedFile[] GenerateFiles(
        AsnValidationResult validation,
        AsnGenerationOptions options)
    {
        if (validation == null)
        {
            throw new System.ArgumentNullException("validation");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!validation.Success)
        {
            throw new AsnCodeGenerationException(
                "C# generation requires a successful validation result.",
                "ASNGEN001",
                validation.Diagnostics);
        }

        LoweredCompilationIr compilation =
            new ValidationReflectionIrAdapter().LowerValidation(validation);
        CSharpGenerationResult result = this.GenerateCompilation(
            compilation,
            options);
        return this.CreateFilesFromResult(result);
    }

    internal AsnGeneratedFile[] CreateFilesFromResult(
        CSharpGenerationResult result)
    {
        AsnGeneratedFile[] files =
            new AsnGeneratedFile[result.Sources.Length];
        int index = 0;
        while (index < files.Length)
        {
            files[index] = new AsnGeneratedFile
            {
                RelativePath = result.Sources[index].Partition.RelativePath,
                Content = result.Sources[index].Content,
                SourceSpans = result.Sources[index].SourceSpans,
                Diagnostics = result.Sources[index].Diagnostics
            };
            index = index + 1;
        }

        return files;
    }

    internal CSharpGenerationResult GenerateCompilation(
        LoweredCompilationIr compilation,
        AsnGenerationOptions options)
    {
        if (compilation == null)
        {
            throw new System.ArgumentNullException("compilation");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        CSharpGenerationCapabilities capabilities =
            this.GetCapabilities(options.Target);
        CSharpGenerationPlan plan = new CSharpFilePlanner()
            .PlanCompilation(compilation, options.NamespaceName);
        System.Collections.Generic.List<CSharpGeneratedSource> sources =
            new System.Collections.Generic.List<CSharpGeneratedSource>();
        System.Collections.Generic.List<AsnDiagnostic> diagnostics =
            new System.Collections.Generic.List<AsnDiagnostic>();
        int index = 0;
        while (index < plan.Partitions.Length)
        {
            CSharpFilePartition partition = plan.Partitions[index];
            if (this.ShouldEmitPartition(partition.Kind, capabilities))
            {
                CSharpGeneratedSource source = this.EmitPartitionSource(
                    partition,
                    plan,
                    options.IncludeSourceMappings);
                sources.Add(source);
                int diagnosticIndex = 0;
                while (diagnosticIndex < source.Diagnostics.Length)
                {
                    diagnostics.Add(source.Diagnostics[diagnosticIndex]);
                    diagnosticIndex = diagnosticIndex + 1;
                }
            }

            index = index + 1;
        }

        return new CSharpGenerationResult
        {
            Plan = plan,
            Capabilities = capabilities,
            Sources = sources.ToArray(),
            Diagnostics = diagnostics.ToArray()
        };
    }

    internal CSharpGenerationCapabilities GetCapabilities(
        AsnCodeGenerationTarget target)
    {
        if (target == AsnCodeGenerationTarget.ModelOnly)
        {
            return new CSharpGenerationCapabilities
            {
                Models = true
            };
        }

        if (target == AsnCodeGenerationTarget.DescriptorOnly)
        {
            return new CSharpGenerationCapabilities
            {
                Descriptors = true
            };
        }

        if (target == AsnCodeGenerationTarget.EncodeOnly)
        {
            return new CSharpGenerationCapabilities
            {
                Models = true,
                Descriptors = true,
                Validators = true,
                Codecs = true,
                Encode = true
            };
        }

        if (target == AsnCodeGenerationTarget.DecodeOnly)
        {
            return new CSharpGenerationCapabilities
            {
                Models = true,
                Descriptors = true,
                Validators = true,
                Codecs = true,
                Decode = true
            };
        }

        if (target == AsnCodeGenerationTarget.Combined)
        {
            return new CSharpGenerationCapabilities
            {
                Models = true,
                Descriptors = true,
                Validators = true,
                Codecs = true,
                Encode = true,
                Decode = true
            };
        }

        throw new System.ArgumentException(
            "The C# generation target is not supported.",
            "target");
    }

    internal bool ShouldEmitPartition(
        CSharpFilePartitionKind kind,
        CSharpGenerationCapabilities capabilities)
    {
        if (kind == CSharpFilePartitionKind.Model
            || kind == CSharpFilePartitionKind.Constants)
        {
            return capabilities.Models;
        }

        if (kind == CSharpFilePartitionKind.Descriptors)
        {
            return capabilities.Descriptors;
        }

        if (kind == CSharpFilePartitionKind.Validator)
        {
            return capabilities.Validators;
        }

        if (kind == CSharpFilePartitionKind.Codec)
        {
            return capabilities.Codecs;
        }

        return false;
    }

    internal CSharpGeneratedSource EmitPartitionSource(
        CSharpFilePartition partition,
        CSharpGenerationPlan plan,
        bool includeSourceMappings)
    {
        CSharpModulePlan module = partition.Module;
        if (partition.Kind == CSharpFilePartitionKind.Model)
        {
            if (partition.Type == null)
            {
                throw new System.InvalidOperationException(
                    "A model partition requires a planned type.");
            }

            ModelEmitter emitter = new ModelEmitter
            {
                GenerationPlan = plan
            };
            CSharpModelEmission emission = emitter.EmitModel(
                partition.Type.Assignment,
                module.NamespaceName.GeneratedName,
                module.TypeScope);
            System.Collections.Generic.List<CSharpNameMapping> mappings =
                new System.Collections.Generic.List<CSharpNameMapping>();
            mappings.Add(emission.TypeName);
            if (emission.ChoiceName != null)
            {
                mappings.Add(emission.ChoiceName);
            }

            this.AddMappings(mappings, emission.FieldNames);
            this.AddMappings(mappings, emission.PresenceNames);
            return this.CreateGeneratedSource(
                partition,
                emission.Content,
                mappings.ToArray(),
                includeSourceMappings);
        }

        if (partition.Kind == CSharpFilePartitionKind.Constants)
        {
            ConstantEmitter emitter = new ConstantEmitter
            {
                GenerationPlan = plan
            };
            CSharpConstantEmission emission = emitter.EmitConstants(
                module.Module,
                module.NamespaceName.GeneratedName,
                module.TypeScope);
            System.Collections.Generic.List<CSharpNameMapping> mappings =
                new System.Collections.Generic.List<CSharpNameMapping>();
            if (emission.ModuleValuesName != null)
            {
                mappings.Add(emission.ModuleValuesName);
            }

            this.AddMappings(mappings, emission.NamedValueContainerNames);
            this.AddMappings(mappings, emission.ValueNames);
            return this.CreateGeneratedSource(
                partition,
                emission.Content,
                mappings.ToArray(),
                includeSourceMappings);
        }

        if (partition.Kind == CSharpFilePartitionKind.Descriptors)
        {
            DescriptorEmitter emitter = new DescriptorEmitter
            {
                GenerationPlan = plan
            };
            CSharpDescriptorEmission emission = emitter.EmitDescriptors(
                module.Module,
                module.NamespaceName.GeneratedName,
                module.TypeScope);
            System.Collections.Generic.List<CSharpNameMapping> mappings =
                new System.Collections.Generic.List<CSharpNameMapping>();
            mappings.Add(emission.SchemaName);
            this.AddMappings(mappings, emission.TypeNames);
            this.AddMappings(mappings, emission.FieldNames);
            this.AddMappings(mappings, emission.AssignmentNames);
            return this.CreateGeneratedSource(
                partition,
                emission.Content,
                mappings.ToArray(),
                includeSourceMappings);
        }

        if (partition.Kind == CSharpFilePartitionKind.Validator)
        {
            ValidatorEmitter emitter = new ValidatorEmitter
            {
                GenerationPlan = plan
            };
            CSharpValidatorEmission emission = emitter.EmitValidator(
                module.Module,
                module.NamespaceName.GeneratedName,
                module.TypeScope);
            return this.CreateGeneratedSource(
                partition,
                emission.Content,
                [emission.ValidatorName],
                includeSourceMappings);
        }

        if (partition.Kind == CSharpFilePartitionKind.Codec)
        {
            CodecFacadeEmitter emitter = new CodecFacadeEmitter
            {
                GenerationPlan = plan
            };
            CSharpCodecFacadeEmission emission = emitter.EmitFacade(
                module.Module,
                module.NamespaceName.GeneratedName,
                module.TypeScope);
            return this.CreateGeneratedSource(
                partition,
                emission.Content,
                [emission.FacadeName],
                includeSourceMappings);
        }

        throw new System.InvalidOperationException(
            "The planned C# file kind cannot be emitted.");
    }

    internal void AddMappings(
        System.Collections.Generic.List<CSharpNameMapping> target,
        CSharpNameMapping[] source)
    {
        int index = 0;
        while (index < source.Length)
        {
            target.Add(source[index]);
            index = index + 1;
        }
    }

    internal CSharpGeneratedSource CreateGeneratedSource(
        CSharpFilePartition partition,
        string content,
        CSharpNameMapping[] emittedMappings,
        bool includeSourceMappings)
    {
        System.Collections.Generic.List<CSharpNameMapping> mappings =
            new System.Collections.Generic.List<CSharpNameMapping>();
        mappings.Add(partition.Module.NamespaceName);
        mappings.Add(partition.FileName);
        this.AddMappings(mappings, emittedMappings);
        AsnSourceSpan[] sourceSpans = includeSourceMappings
            ? new GeneratedSourceMap().CollectPartitionSpans(partition)
            : [];
        AsnDiagnostic[] diagnostics =
            new CSharpCollisionDiagnosticBuilder().BuildDiagnostics(
                mappings.ToArray(),
                sourceSpans);
        return new CSharpGeneratedSource
        {
            Partition = partition,
            Content = content,
            SourceSpans = sourceSpans,
            NameMappings = mappings.ToArray(),
            Diagnostics = diagnostics
        };
    }
}
