namespace asn1;

internal class CompilerPublicModelBuilder
{
    internal AsnModule[] BuildParsedModules(ModuleSyntax[] modules)
    {
        AsnModule[] result = new AsnModule[modules.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.BuildParsedModule(modules[index]);
            index = index + 1;
        }

        return result;
    }

    internal AsnModule BuildParsedModule(ModuleSyntax module)
    {
        AsnImport[] imports = new AsnImport[module.ImportedModules.Length];
        int importIndex = 0;
        while (importIndex < imports.Length)
        {
            ImportedModuleSyntax source = module.ImportedModules[importIndex];
            string[] symbols = new string[source.Symbols.Length];
            int symbolIndex = 0;
            while (symbolIndex < symbols.Length)
            {
                symbols[symbolIndex] = source.Symbols[symbolIndex].Name;
                symbolIndex = symbolIndex + 1;
            }

            imports[importIndex] = new AsnImport
            {
                ModuleName = source.ModuleName,
                GeneratedModuleName = source.ModuleName,
                Symbols = symbols,
                Span = this.CloneSpan(source.Span)
            };
            importIndex = importIndex + 1;
        }

        AsnAssignment[] assignments =
            new AsnAssignment[module.AssignmentHeads.Length];
        int assignmentIndex = 0;
        while (assignmentIndex < assignments.Length)
        {
            assignments[assignmentIndex] = this.BuildParsedAssignment(
                module.AssignmentHeads[assignmentIndex]);
            assignmentIndex = assignmentIndex + 1;
        }

        return new AsnModule
        {
            Name = module.Name,
            Imports = imports,
            Assignments = assignments,
            Span = this.CloneSpan(module.Span)
        };
    }

    internal AsnAssignment BuildParsedAssignment(AssignmentHeadSyntax head)
    {
        AssignmentHeadAlternativeSyntax? declared = head.DeclaredReference;
        if (declared == null && head.Alternatives.Length > 0)
        {
            declared = head.Alternatives[0];
        }

        AsnAssignmentKind kind = AsnAssignmentKind.Unknown;
        if (declared != null && declared.CandidateKinds.Length == 1)
        {
            kind = declared.CandidateKinds[0];
        }

        return new AsnAssignment
        {
            Name = declared == null ? "" : declared.Name,
            Kind = kind,
            Span = this.CloneSpan(head.Span)
        };
    }

    internal AsnModule[] BuildTableModules(SymbolTable[] tables)
    {
        AsnModule[] result = new AsnModule[tables.Length];
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            SymbolTable table = tables[tableIndex];
            AsnAssignment[] assignments =
                new AsnAssignment[table.Symbols.Length];
            int symbolIndex = 0;
            while (symbolIndex < assignments.Length)
            {
                SymbolTableEntry symbol = table.Symbols[symbolIndex];
                assignments[symbolIndex] = new AsnAssignment
                {
                    Name = symbol.Name,
                    Kind = symbol.Kind,
                    Parameters = this.CopyParameters(
                        symbol.FormalParameters),
                    Span = this.CloneSpan(symbol.Span)
                };
                symbolIndex = symbolIndex + 1;
            }

            result[tableIndex] = new AsnModule
            {
                Name = table.ModuleName,
                Assignments = assignments,
                Span = this.CloneSpan(table.Module?.Span)
            };
            tableIndex = tableIndex + 1;
        }

        return result;
    }

    internal AsnAssignment[] FlattenAssignments(AsnModule[] modules)
    {
        int count = 0;
        int moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            count = count + modules[moduleIndex].Assignments.Length;
            moduleIndex = moduleIndex + 1;
        }

        AsnAssignment[] result = new AsnAssignment[count];
        int resultIndex = 0;
        moduleIndex = 0;
        while (moduleIndex < modules.Length)
        {
            int assignmentIndex = 0;
            while (assignmentIndex
                < modules[moduleIndex].Assignments.Length)
            {
                result[resultIndex] =
                    modules[moduleIndex].Assignments[assignmentIndex];
                resultIndex = resultIndex + 1;
                assignmentIndex = assignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return result;
    }

    internal AsnEncodingPlan[] CollectEncodingPlans(
        AsnReflectionCatalog catalog)
    {
        System.Collections.Generic.List<AsnEncodingPlan> plans =
            new System.Collections.Generic.List<AsnEncodingPlan>();
        int moduleIndex = 0;
        while (moduleIndex < catalog.Modules.Length)
        {
            int encodingIndex = 0;
            while (encodingIndex
                < catalog.Modules[moduleIndex].Encodings.Length)
            {
                AsnEncodingPlan? plan =
                    catalog.Modules[moduleIndex].Encodings[encodingIndex].Plan;
                if (plan != null)
                {
                    plans.Add(plan);
                }

                encodingIndex = encodingIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return plans.ToArray();
    }

    internal AsnParameter[] CopyParameters(AsnParameter[] source)
    {
        AsnParameter[] result = new AsnParameter[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CloneSpan(AsnSourceSpan? span)
    {
        if (span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = new AsnSourcePosition
            {
                SourceName = span.Start.SourceName,
                Offset = span.Start.Offset,
                Line = span.Start.Line,
                Column = span.Start.Column
            },
            End = new AsnSourcePosition
            {
                SourceName = span.End.SourceName,
                Offset = span.End.Offset,
                Line = span.End.Line,
                Column = span.End.Column
            }
        };
    }
}
