namespace asn1;

internal class XerInstructionResolver
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];

    internal XerInstructionResolver(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal XerInstructionResolutionResult ResolveInstructions(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int before = this.Diagnostics.GetCount();
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            this.ResetModuleTypes(tables[tableIndex]);
            this.ConfigureGlobalDefaults(tables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            this.ExpandTargetedInstructions(tables[tableIndex]);
            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            SymbolTable table = tables[tableIndex];
            int typeIndex = 0;
            while (typeIndex < table.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.ResolveTypeInstructions(type, table);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return new XerInstructionResolutionResult
        {
            Success = this.Diagnostics.GetCount() == before
        };
    }

    internal void ResetModuleTypes(SymbolTable table)
    {
        table.XerImportedInstructionTargets = [];
        table.XerImportedInstructions = [];
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int index = 0;
        while (index < table.TypeSymbols.Length)
        {
            ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
            if (type != null)
            {
                this.ResetTypeTree(type, visited);
            }

            index = index + 1;
        }
    }

    internal void ResetTypeTree(
        ResolvedTypeSyntax type,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        type.TargetedEncodingInstructions = [];
        type.FinalEncodingInstructions = [];
        type.XerInstructionResolutionState = 0;
        type.XerModifiedEncodings = false;
        if (type.InnerType != null)
        {
            this.ResetTypeTree(type.InnerType, visited);
        }

        if (type.AliasTarget != null)
        {
            this.ResetTypeTree(type.AliasTarget, visited);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? component = type.Components[index].TypeResolution;
            if (component != null)
            {
                this.ResetTypeTree(component, visited);
            }

            index = index + 1;
        }
    }

    internal void ConfigureGlobalDefaults(SymbolTable table)
    {
        ModuleSyntax? module = table.Module;
        if (module == null)
        {
            return;
        }

        module.XerModifiedEncodings = table.PredeclaredXsdSupport;
        module.XerControlNamespaceUri = "urn:oid:2.1.5.2.0.1";
        module.XerControlNamespacePrefix = "asn1";
        bool modifiedSeen = table.PredeclaredXsdSupport;
        bool namespaceSeen = false;
        bool instructionSeen = false;
        bool nonGlobalSeen = false;
        int sectionIndex = 0;
        while (sectionIndex < module.EncodingControlSections.Length)
        {
            EncodingControlSectionSyntax section = module.EncodingControlSections[sectionIndex];
            if (section.EncodingReference == "XER")
            {
                int instructionIndex = 0;
                while (instructionIndex < section.Instructions.Length)
                {
                    ParsedEncodingInstructionSyntax syntax =
                        section.Instructions[instructionIndex];
                    AsnEncodingInstruction instruction = syntax.Instruction;
                    if (instruction.Kind == AsnInstructionKind.GlobalDefaults)
                    {
                        if (nonGlobalSeen)
                        {
                            this.AddDiagnostic(
                                306,
                                "GLOBAL-DEFAULTS instructions must precede targeted instructions.",
                                instruction.Span);
                        }

                        if (instruction.Negated)
                        {
                            this.AddDiagnostic(
                                300,
                                "NOT GLOBAL-DEFAULTS is not permitted.",
                                instruction.Span);
                        }
                        else if (syntax.ModifiedEncodings)
                        {
                            if (instructionSeen)
                            {
                                this.AddDiagnostic(
                                    307,
                                    "GLOBAL-DEFAULTS MODIFIED-ENCODINGS must be the first XER instruction.",
                                    instruction.Span);
                            }

                            if (modifiedSeen)
                            {
                                this.AddDiagnostic(
                                    301,
                                    "GLOBAL-DEFAULTS MODIFIED-ENCODINGS can be assigned only once.",
                                    instruction.Span);
                            }

                            modifiedSeen = true;
                            module.XerModifiedEncodings = true;
                        }
                        else if (syntax.ControlNamespace)
                        {
                            if (namespaceSeen)
                            {
                                this.AddDiagnostic(
                                    302,
                                    "A GLOBAL-DEFAULTS control namespace can be assigned only once.",
                                    instruction.Span);
                            }

                            namespaceSeen = true;
                            module.XerControlNamespaceUri = syntax.NamespaceUri;
                            module.XerControlNamespacePrefix = syntax.NamespacePrefix;
                        }
                    }
                    else
                    {
                        nonGlobalSeen = true;
                    }

                    instructionSeen = true;

                    instructionIndex = instructionIndex + 1;
                }
            }

            sectionIndex = sectionIndex + 1;
        }
    }

    internal void ExpandTargetedInstructions(SymbolTable table)
    {
        ModuleSyntax? module = table.Module;
        if (module == null)
        {
            return;
        }

        int sectionIndex = 0;
        while (sectionIndex < module.EncodingControlSections.Length)
        {
            EncodingControlSectionSyntax section = module.EncodingControlSections[sectionIndex];
            if (section.EncodingReference == "XER")
            {
                int instructionIndex = 0;
                while (instructionIndex < section.Instructions.Length)
                {
                    AsnEncodingInstruction instruction =
                        section.Instructions[instructionIndex].Instruction;
                    if (instruction.Kind != AsnInstructionKind.GlobalDefaults)
                    {
                        int targetIndex = 0;
                        while (targetIndex < instruction.Targets.Length)
                        {
                            this.ExpandOneTarget(
                                table,
                                instruction,
                                instruction.Targets[targetIndex]);
                            targetIndex = targetIndex + 1;
                        }
                    }

                    instructionIndex = instructionIndex + 1;
                }
            }

            sectionIndex = sectionIndex + 1;
        }
    }

    internal void ExpandOneTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        string target)
    {
        if (target == "ALL")
        {
            int index = 0;
            while (index < table.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
                if (type != null)
                {
                    this.AppendTargetedInstruction(type, instruction);
                }

                index = index + 1;
            }

            return;
        }

        if (target.StartsWith("ALL IMPORTS FROM "))
        {
            this.ExpandImportedTarget(table, instruction, target.Substring(17));
            return;
        }

        int inIndex = target.IndexOf(" IN ");
        if (inIndex >= 0)
        {
            string identifiers = target.Substring(0, inIndex);
            string context = target.Substring(inIndex + 4);
            this.ExpandContextTarget(table, instruction, identifiers, context);
            return;
        }

        AsnTypeKind builtInKind = this.GetBuiltInKind(target);
        if (builtInKind != AsnTypeKind.Unknown)
        {
            this.ExpandBuiltInTarget(table, instruction, builtInKind);
            return;
        }

        this.ExpandPathTarget(table, instruction, target);
    }

    internal void ExpandImportedTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        string moduleName)
    {
        int importIndex = 0;
        while (importIndex < table.ImportResolutions.Length)
        {
            ImportResolution import = table.ImportResolutions[importIndex];
            if (import.TargetModule != null
                && import.TargetModule.ModuleName == moduleName)
            {
                int symbolIndex = 0;
                while (symbolIndex < import.Symbols.Length)
                {
                    SymbolTableEntry? symbol = import.Symbols[symbolIndex].Target;
                    if (symbol != null
                        && symbol.Kind == AsnAssignmentKind.Type
                        && symbol.ResolvedType != null)
                    {
                        this.AppendImportedInstruction(table, symbol, instruction);
                    }

                    symbolIndex = symbolIndex + 1;
                }
            }

            importIndex = importIndex + 1;
        }
    }

    internal void AppendImportedInstruction(
        SymbolTable table,
        SymbolTableEntry target,
        AsnEncodingInstruction instruction)
    {
        int length = table.XerImportedInstructions.Length;
        SymbolTableEntry[] targets = new SymbolTableEntry[length + 1];
        AsnEncodingInstruction[] instructions = new AsnEncodingInstruction[length + 1];
        int index = 0;
        while (index < length)
        {
            targets[index] = table.XerImportedInstructionTargets[index];
            instructions[index] = table.XerImportedInstructions[index];
            index = index + 1;
        }

        targets[length] = target;
        instructions[length] = instruction;
        table.XerImportedInstructionTargets = targets;
        table.XerImportedInstructions = instructions;
    }

    internal void ExpandContextTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        string identifiers,
        string context)
    {
        ResolvedTypeSyntax? type = this.FindPathTarget(table, context);
        if (type == null)
        {
            return;
        }

        if (identifiers == "ALL")
        {
            this.AppendDescendantTargets(type, instruction, true);
            return;
        }

        if (identifiers == "COMPONENTS")
        {
            this.AppendDescendantTargets(type, instruction, false);
            return;
        }

        string[] names = identifiers.Split(',');
        int nameIndex = 0;
        while (nameIndex < names.Length)
        {
            this.AppendNamedComponentTarget(
                type,
                names[nameIndex].Trim(),
                instruction);
            nameIndex = nameIndex + 1;
        }
    }

    internal void ExpandPathTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        string target)
    {
        if (target.EndsWith(".ALL"))
        {
            ResolvedTypeSyntax? parent = this.FindPathTarget(
                table,
                target.Substring(0, target.Length - 4));
            if (parent != null)
            {
                this.AppendDescendantTargets(parent, instruction, true);
            }

            return;
        }

        if (target.EndsWith(".*"))
        {
            ResolvedTypeSyntax? collection = this.FindPathTarget(
                table,
                target.Substring(0, target.Length - 2));
            if (collection != null && collection.InnerType != null)
            {
                this.AppendTargetedInstruction(collection.InnerType, instruction);
            }

            return;
        }

        ResolvedTypeSyntax? type = this.FindPathTarget(table, target);
        if (type != null)
        {
            this.AppendTargetedInstruction(type, instruction);
        }
    }

    internal ResolvedTypeSyntax? FindPathTarget(SymbolTable table, string target)
    {
        int qualifierIndex = target.IndexOf(":");
        string path = qualifierIndex < 0 ? target : target.Substring(0, qualifierIndex);
        string[] parts = path.Split('.');
        int partIndex = 0;
        if (parts.Length > 1 && parts[0] == table.ModuleName)
        {
            partIndex = 1;
        }

        if (partIndex >= parts.Length)
        {
            return null;
        }

        SymbolTableEntry? symbol = table.FindSymbol(parts[partIndex], AsnAssignmentKind.Type);
        if (symbol == null || symbol.ResolvedType == null)
        {
            return null;
        }

        ResolvedTypeSyntax current = symbol.ResolvedType;
        partIndex = partIndex + 1;
        while (partIndex < parts.Length)
        {
            ResolvedTypeSyntax? component = this.FindNamedComponentType(
                current,
                parts[partIndex]);
            if (component == null)
            {
                return null;
            }

            current = component;
            partIndex = partIndex + 1;
        }

        return current;
    }

    internal ResolvedTypeSyntax? FindNamedComponentType(
        ResolvedTypeSyntax type,
        string name)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[index];
            if (component.Component != null
                && component.Component.Name == name)
            {
                return component.TypeResolution;
            }

            index = index + 1;
        }

        return null;
    }

    internal void AppendNamedComponentTarget(
        ResolvedTypeSyntax type,
        string name,
        AsnEncodingInstruction instruction)
    {
        ResolvedTypeSyntax? component = this.FindNamedComponentType(type, name);
        if (component != null)
        {
            this.AppendTargetedInstruction(component, instruction);
        }
    }

    internal void AppendDescendantTargets(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        bool recursive)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        visited.Add(type);
        this.AppendDescendantTargetsTracked(type, instruction, recursive, visited);
    }

    internal void AppendDescendantTargetsTracked(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        bool recursive,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? component = type.Components[index].TypeResolution;
            if (component != null && !visited.Contains(component))
            {
                visited.Add(component);
                this.AppendTargetedInstruction(component, instruction);
                if (recursive)
                {
                    this.AppendDescendantTargetsTracked(
                        component,
                        instruction,
                        true,
                        visited);
                }
            }

            index = index + 1;
        }

    }

    internal void ExpandBuiltInTarget(
        SymbolTable table,
        AsnEncodingInstruction instruction,
        AsnTypeKind kind)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int index = 0;
        while (index < table.TypeSymbols.Length)
        {
            ResolvedTypeSyntax? type = table.TypeSymbols[index].ResolvedType;
            if (type != null)
            {
                this.AppendBuiltInMatches(type, instruction, kind, visited);
            }

            index = index + 1;
        }
    }

    internal void AppendBuiltInMatches(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction,
        AsnTypeKind kind,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(type))
        {
            return;
        }

        visited.Add(type);
        if (type.EffectiveKind == kind)
        {
            this.AppendTargetedInstruction(type, instruction);
        }

        if (type.InnerType != null)
        {
            this.AppendBuiltInMatches(type.InnerType, instruction, kind, visited);
        }

        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? component = type.Components[index].TypeResolution;
            if (component != null)
            {
                this.AppendBuiltInMatches(component, instruction, kind, visited);
            }

            index = index + 1;
        }
    }

    internal void AppendTargetedInstruction(
        ResolvedTypeSyntax type,
        AsnEncodingInstruction instruction)
    {
        AsnEncodingInstruction[] current = type.TargetedEncodingInstructions;
        AsnEncodingInstruction[] result = new AsnEncodingInstruction[current.Length + 1];
        int index = 0;
        while (index < current.Length)
        {
            result[index] = current[index];
            index = index + 1;
        }

        result[current.Length] = instruction;
        type.TargetedEncodingInstructions = result;
    }

    internal AsnTypeKind GetBuiltInKind(string name)
    {
        switch (name)
        {
            case "BOOLEAN": return AsnTypeKind.Boolean;
            case "INTEGER": return AsnTypeKind.Integer;
            case "ENUMERATED": return AsnTypeKind.Enumerated;
            case "REAL": return AsnTypeKind.Real;
            case "BIT STRING": return AsnTypeKind.BitString;
            case "OCTET STRING": return AsnTypeKind.OctetString;
            case "NULL": return AsnTypeKind.Null;
            case "OBJECT IDENTIFIER": return AsnTypeKind.ObjectIdentifier;
            case "RELATIVE-OID": return AsnTypeKind.RelativeObjectIdentifier;
            case "OID-IRI": return AsnTypeKind.ObjectIdentifierIri;
            case "RELATIVE-OID-IRI": return AsnTypeKind.RelativeObjectIdentifierIri;
            case "CHOICE": return AsnTypeKind.Choice;
            case "SEQUENCE": return AsnTypeKind.Sequence;
            case "SET": return AsnTypeKind.Set;
            case "SEQUENCE OF": return AsnTypeKind.SequenceOf;
            case "SET OF": return AsnTypeKind.SetOf;
            case "INSTANCE OF": return AsnTypeKind.InstanceOf;
            case "EXTERNAL": return AsnTypeKind.External;
            case "EMBEDDED PDV": return AsnTypeKind.EmbeddedPdv;
            case "CHARACTER STRING": return AsnTypeKind.CharacterString;
            case "ObjectDescriptor": return AsnTypeKind.ObjectDescriptor;
            case "UTF8String": return AsnTypeKind.Utf8String;
            case "UTCTime": return AsnTypeKind.UtcTime;
            case "GeneralizedTime": return AsnTypeKind.GeneralizedTime;
            case "TIME": return AsnTypeKind.Time;
            case "DATE": return AsnTypeKind.Date;
            case "TIME-OF-DAY": return AsnTypeKind.TimeOfDay;
            case "DATE-TIME": return AsnTypeKind.DateTime;
            case "DURATION": return AsnTypeKind.Duration;
        }

        return AsnTypeKind.Unknown;
    }

    internal void ResolveTypeInstructions(ResolvedTypeSyntax type, SymbolTable module)
    {
        if (type.XerInstructionResolutionState == 2)
        {
            return;
        }

        if (type.XerInstructionResolutionState == 1)
        {
            return;
        }

        type.XerInstructionResolutionState = 1;
        System.Collections.Generic.List<AsnEncodingInstruction> final =
            new System.Collections.Generic.List<AsnEncodingInstruction>();
        ParsedTypeSyntax? source = type.SourceSyntax;
        if (source != null
            && source.Type.Kind == AsnTypeKind.Defined
            && type.AliasTarget != null)
        {
            SymbolTable inheritedModule = this.FindDeclaringTable(type.AliasTarget) ?? module;
            this.ResolveTypeInstructions(type.AliasTarget, inheritedModule);
            this.AppendInheritedInstructions(type.AliasTarget, final, false);
            this.ApplyImportedInstructions(type.AliasTarget, module, final);
        }
        else if (source != null
            && source.Type.Kind == AsnTypeKind.Selection
            && type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            ResolvedTypeSyntax selected = type.SelectedComponent.TypeResolution;
            this.ResolveTypeInstructions(selected, module);
            this.AppendInheritedInstructions(selected, final, true);
        }
        else if (source != null
            && (source.Type.Kind == AsnTypeKind.Prefixed
                || source.Type.Kind == AsnTypeKind.Tagged)
            && type.InnerType != null)
        {
            this.ResolveTypeInstructions(type.InnerType, module);
            this.AppendInheritedInstructions(type.InnerType, final, true);
        }

        int index = 0;
        while (index < type.TargetedEncodingInstructions.Length)
        {
            this.ApplyInstruction(final, type.TargetedEncodingInstructions[index]);
            index = index + 1;
        }

        if (source != null
            && (source.EncodingReference == "XER" || source.EncodingReference.Length == 0))
        {
            index = 0;
            while (index < source.EncodingInstructions.Length)
            {
                if (source.EncodingInstructions[index].Kind != AsnInstructionKind.Unknown)
                {
                    this.ApplyInstruction(final, source.EncodingInstructions[index]);
                }

                index = index + 1;
            }
        }

        type.FinalEncodingInstructions = final.ToArray();
        type.XerModifiedEncodings = module.Module != null
            && module.Module.XerModifiedEncodings;
        this.ValidateFinalInstructions(type);
        type.XerInstructionResolutionState = 2;

        if (type.InnerType != null)
        {
            this.ResolveTypeInstructions(type.InnerType, module);
        }

        index = 0;
        while (index < type.Components.Length)
        {
            ResolvedTypeSyntax? component = type.Components[index].TypeResolution;
            if (component != null)
            {
                this.ResolveTypeInstructions(component, module);
            }

            index = index + 1;
        }

        this.ValidateWildcardComponents(type);
    }

    internal void ApplyImportedInstructions(
        ResolvedTypeSyntax inherited,
        SymbolTable module,
        System.Collections.Generic.List<AsnEncodingInstruction> final)
    {
        SymbolTableEntry? declaration = inherited.SourceDeclaration;
        if (declaration == null)
        {
            return;
        }

        int index = 0;
        while (index < module.XerImportedInstructions.Length)
        {
            if (module.XerImportedInstructionTargets[index] == declaration)
            {
                this.ApplyInstruction(final, module.XerImportedInstructions[index]);
            }

            index = index + 1;
        }
    }

    internal void AppendInheritedInstructions(
        ResolvedTypeSyntax source,
        System.Collections.Generic.List<AsnEncodingInstruction> destination,
        bool includeNames)
    {
        int index = 0;
        while (index < source.FinalEncodingInstructions.Length)
        {
            AsnEncodingInstruction instruction = source.FinalEncodingInstructions[index];
            if (includeNames
                || (instruction.Kind != AsnInstructionKind.Name
                    && instruction.Kind != AsnInstructionKind.Namespace))
            {
                destination.Add(instruction);
            }

            index = index + 1;
        }
    }

    internal void ApplyInstruction(
        System.Collections.Generic.List<AsnEncodingInstruction> final,
        AsnEncodingInstruction instruction)
    {
        AsnInstructionKind category = instruction.Kind == AsnInstructionKind.Element
            ? AsnInstructionKind.Untagged
            : instruction.Kind;
        int index = final.Count - 1;
        while (index >= 0)
        {
            if (final[index].Kind == category)
            {
                final.RemoveAt(index);
            }

            index = index - 1;
        }

        if (!instruction.Negated
            && instruction.Kind != AsnInstructionKind.Element
            && instruction.Kind != AsnInstructionKind.GlobalDefaults)
        {
            final.Add(this.CloneInstruction(instruction));
        }
    }

    internal AsnEncodingInstruction CloneInstruction(AsnEncodingInstruction source)
    {
        string[] targets = new string[source.Targets.Length];
        string[] arguments = new string[source.Arguments.Length];
        int index = 0;
        while (index < targets.Length)
        {
            targets[index] = source.Targets[index];
            index = index + 1;
        }

        index = 0;
        while (index < arguments.Length)
        {
            arguments[index] = source.Arguments[index];
            index = index + 1;
        }

        return new AsnEncodingInstruction
        {
            Name = source.Name,
            Kind = source.Kind,
            Negated = false,
            Arguments = arguments,
            Targets = targets,
            Span = source.Span
        };
    }

    internal void ValidateFinalInstructions(ResolvedTypeSyntax type)
    {
        int index = 0;
        while (index < type.FinalEncodingInstructions.Length)
        {
            AsnEncodingInstruction instruction = type.FinalEncodingInstructions[index];
            if (!this.IsInstructionLegalForType(instruction.Kind, type))
            {
                this.AddDiagnostic(
                    303,
                    "The " + instruction.Name + " instruction is not legal for "
                        + type.EffectiveKind.ToString() + ".",
                    instruction.Span);
            }


            if ((instruction.Kind == AsnInstructionKind.AnyAttributes
                    || instruction.Kind == AsnInstructionKind.AnyElement)
                && !this.IsWildcardRestrictionValid(instruction.Arguments))
            {
                this.AddDiagnostic(
                    308,
                    "The " + instruction.Name
                        + " namespace restriction is malformed or contains duplicates.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.Name
                && !this.IsNameInstructionValid(instruction.Arguments))
            {
                this.AddDiagnostic(
                    310,
                    "The NAME instruction requires AS followed by a quoted name or case action.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.Namespace
                && !this.IsNamespaceInstructionValid(instruction.Arguments))
            {
                this.AddDiagnostic(
                    311,
                    "The NAMESPACE instruction requires a quoted URI and an optional quoted prefix.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.Text
                && !this.IsTextInstructionValid(instruction.Arguments))
            {
                this.AddDiagnostic(
                    312,
                    "The TEXT instruction contains an invalid replacement action.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.Whitespace
                && !this.IsWhitespaceInstructionValid(
                    instruction.Arguments))
            {
                this.AddDiagnostic(
                    313,
                    "The WHITESPACE instruction requires REPLACE or COLLAPSE.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.DefaultForEmpty
                && !this.IsDefaultForEmptyInstructionValid(
                    instruction.Arguments))
            {
                this.AddDiagnostic(
                    314,
                    "DEFAULT-FOR-EMPTY requires AS followed by one ASN.1 value.",
                    instruction.Span);
            }

            if ((instruction.Kind == AsnInstructionKind.EmbedValues
                    || instruction.Kind == AsnInstructionKind.UseNil
                    || instruction.Kind == AsnInstructionKind.UseOrder)
                && instruction.Arguments.Length != 0)
            {
                this.AddDiagnostic(
                    315,
                    "The " + instruction.Name
                        + " instruction does not accept arguments.",
                    instruction.Span);
            }

            if (instruction.Kind == AsnInstructionKind.PiOrComment
                && !this.IsPiOrCommentInstructionValid(
                    instruction.Arguments))
            {
                this.AddDiagnostic(
                    316,
                    "PI-OR-COMMENT requires AS, a quoted XML fragment, and a valid position.",
                    instruction.Span);
            }

            if (!type.XerModifiedEncodings
                && !this.IsUnmodifiedInstructionAllowed(instruction.Kind))
            {
                this.AddDiagnostic(
                    304,
                    "The " + instruction.Name
                        + " instruction requires GLOBAL-DEFAULTS MODIFIED-ENCODINGS.",
                    instruction.Span);
            }

            int otherIndex = index + 1;
            while (otherIndex < type.FinalEncodingInstructions.Length)
            {
                AsnInstructionKind other = type.FinalEncodingInstructions[otherIndex].Kind;
                bool allowed = type.XerModifiedEncodings
                    ? this.IsModifiedCombinationAllowed(instruction.Kind, other)
                    : this.IsUnmodifiedCombinationAllowed(instruction.Kind, other);
                if (!allowed)
                {
                    this.AddDiagnostic(
                        305,
                        "The " + instruction.Name + " and "
                            + type.FinalEncodingInstructions[otherIndex].Name
                            + " instructions cannot be combined.",
                        type.FinalEncodingInstructions[otherIndex].Span);
                }

                otherIndex = otherIndex + 1;
            }

            index = index + 1;
        }
    }

    internal bool IsInstructionLegalForType(
        AsnInstructionKind instruction,
        ResolvedTypeSyntax resolution)
    {
        AsnTypeKind type = resolution.EffectiveKind;
        switch (instruction)
        {
            case AsnInstructionKind.AnyAttributes:
                return (type == AsnTypeKind.SequenceOf
                        || type == AsnTypeKind.SetOf)
                    && this.HasUtf8CollectionElement(resolution);
            case AsnInstructionKind.AnyElement:
                return type == AsnTypeKind.OctetString || type == AsnTypeKind.Utf8String;
            case AsnInstructionKind.Base64:
                return type == AsnTypeKind.OctetString
                    || type == AsnTypeKind.OpenType
                    || this.IsCharacterStringKind(type);
            case AsnInstructionKind.Decimal:
                return type == AsnTypeKind.Real;
            case AsnInstructionKind.DefaultForEmpty:
                return this.IsDefaultForEmptyResolution(resolution);
            case AsnInstructionKind.EmbedValues:
                return type == AsnTypeKind.Sequence
                    && this.IsEmbedValuesResolution(resolution);
            case AsnInstructionKind.List:
                return (type == AsnTypeKind.SequenceOf || type == AsnTypeKind.SetOf)
                    && this.HasCharacterCollectionElement(resolution);
            case AsnInstructionKind.Text:
                return type == AsnTypeKind.Boolean
                    || type == AsnTypeKind.Integer
                    || type == AsnTypeKind.Enumerated
                    || type == AsnTypeKind.BitString;
            case AsnInstructionKind.UseNil:
                return type == AsnTypeKind.Sequence
                    && this.IsUseNilResolution(resolution);
            case AsnInstructionKind.UseOrder:
                return type == AsnTypeKind.Sequence
                    && this.IsUseOrderResolution(resolution);
            case AsnInstructionKind.UseQName:
                return type == AsnTypeKind.Sequence
                    && this.IsQNameResolutionShape(resolution);
            case AsnInstructionKind.UseNumber:
                return type == AsnTypeKind.Enumerated;
            case AsnInstructionKind.UseType:
                return type == AsnTypeKind.Choice;
            case AsnInstructionKind.UseUnion:
                return type == AsnTypeKind.Choice
                    && this.HasCharacterChoiceAlternatives(resolution);
            case AsnInstructionKind.Whitespace:
                return this.IsCharacterStringKind(type)
                    || this.ContainsFinalInstruction(
                        resolution,
                        AsnInstructionKind.Text);
        }

        return true;
    }

    internal bool IsCharacterEncodableKind(AsnTypeKind type)
    {
        return type == AsnTypeKind.Boolean
            || type == AsnTypeKind.Integer
            || type == AsnTypeKind.Enumerated
            || type == AsnTypeKind.Real
            || type == AsnTypeKind.BitString
            || type == AsnTypeKind.ObjectIdentifier
            || type == AsnTypeKind.RelativeObjectIdentifier
            || type == AsnTypeKind.ObjectIdentifierIri
            || type == AsnTypeKind.RelativeObjectIdentifierIri
            || type == AsnTypeKind.UtcTime
            || type == AsnTypeKind.GeneralizedTime
            || type == AsnTypeKind.Time
            || type == AsnTypeKind.Date
            || type == AsnTypeKind.TimeOfDay
            || type == AsnTypeKind.DateTime
            || type == AsnTypeKind.Duration
            || type == AsnTypeKind.ObjectDescriptor
            || this.IsCharacterStringKind(type);
    }

    internal bool HasCharacterCollectionElement(
        ResolvedTypeSyntax resolution)
    {
        ResolvedTypeSyntax? current = resolution;
        int depth = 0;
        while (current != null && depth < 32)
        {
            if ((current.EffectiveKind == AsnTypeKind.SequenceOf
                    || current.EffectiveKind == AsnTypeKind.SetOf)
                && current.InnerType != null)
            {
                return this.IsCharacterEncodableResolution(
                    current.InnerType);
            }

            if (current.EffectiveType != null
                && current.EffectiveType.ElementType != null)
            {
                return this.IsCharacterEncodableKind(
                    current.EffectiveType.ElementType.Kind);
            }

            current = current.InnerType == null
                ? current.AliasTarget
                : current.InnerType;
            depth = depth + 1;
        }

        return false;
    }

    internal bool IsNameInstructionValid(string[] arguments)
    {
        if (arguments.Length != 2 || arguments[0] != "AS")
        {
            return false;
        }

        string action = arguments[1];
        return this.IsQuotedInstructionString(action)
            || action == "CAPITALIZED"
            || action == "UNCAPITALIZED"
            || action == "UPPERCASED"
            || action == "LOWERCASED";
    }

    internal bool IsNamespaceInstructionValid(string[] arguments)
    {
        return arguments.Length == 2
                && arguments[0] == "AS"
                && this.IsQuotedInstructionString(arguments[1])
            || arguments.Length == 4
                && arguments[0] == "AS"
                && this.IsQuotedInstructionString(arguments[1])
                && arguments[2] == "PREFIX"
                && this.IsQuotedInstructionString(arguments[3]);
    }

    internal bool IsTextInstructionValid(string[] arguments)
    {
        return arguments.Length == 0
            || arguments.Length == 2
                && arguments[0] == "AS"
                && (this.IsQuotedInstructionString(arguments[1])
                    || arguments[1] == "CAPITALIZED"
                    || arguments[1] == "UNCAPITALIZED"
                    || arguments[1] == "UPPERCASED"
                    || arguments[1] == "LOWERCASED");
    }

    internal bool IsQuotedInstructionString(string value)
    {
        return value.Length >= 2
            && value[0] == '"'
            && value[value.Length - 1] == '"';
    }

    internal bool IsWhitespaceInstructionValid(string[] arguments)
    {
        return arguments.Length == 1
            && (arguments[0] == "REPLACE"
                || arguments[0] == "COLLAPSE");
    }

    internal bool IsDefaultForEmptyInstructionValid(string[] arguments)
    {
        return arguments.Length >= 2 && arguments[0] == "AS";
    }

    internal bool IsPiOrCommentInstructionValid(string[] arguments)
    {
        if (arguments.Length != 3
            || arguments[0] != "AS"
            || !this.IsQuotedInstructionString(arguments[1]))
        {
            return false;
        }

        string position = arguments[2];
        return position == "BEFORE-TAG"
            || position == "BEFORE-VALUE"
            || position == "AFTER-VALUE"
            || position == "AFTER-TAG";
    }

    internal bool IsDefaultForEmptyResolution(
        ResolvedTypeSyntax resolution)
    {
        if (this.IsCharacterEncodableResolution(resolution))
        {
            return true;
        }

        if (resolution.EffectiveKind != AsnTypeKind.Sequence)
        {
            return false;
        }

        if (this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.EmbedValues)
            || this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.UseNil))
        {
            return true;
        }

        int matches = 0;
        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedComponentSyntax component = resolution.Components[index];
            if (component != null
                && component.TypeResolution != null
                && this.IsCharacterEncodableResolution(
                    component.TypeResolution)
                && this.ContainsFinalInstruction(
                    component.TypeResolution,
                    AsnInstructionKind.Untagged))
            {
                matches = matches + 1;
            }

            index = index + 1;
        }

        return matches == 1;
    }

    internal bool IsEmbedValuesResolution(ResolvedTypeSyntax resolution)
    {
        if (resolution.Components.Length == 0)
        {
            return false;
        }

        ResolvedComponentSyntax first = resolution.Components[0];
        if (first == null
            || first.Component == null
            || first.Component.Optional
            || first.Component.HasDefaultValue
            || first.TypeResolution == null
            || first.TypeResolution.EffectiveKind != AsnTypeKind.SequenceOf
            || first.TypeResolution.InnerType == null)
        {
            return false;
        }

        return first.TypeResolution.InnerType.EffectiveKind
            == AsnTypeKind.Utf8String;
    }

    internal bool IsUseNilResolution(ResolvedTypeSyntax resolution)
    {
        int optionalCount = 0;
        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedComponentSyntax component = resolution.Components[index];
            if (component != null
                && component.Component != null
                && component.Component.Optional)
            {
                optionalCount = optionalCount + 1;
            }

            index = index + 1;
        }

        return optionalCount == 1;
    }

    internal bool IsUseOrderResolution(ResolvedTypeSyntax resolution)
    {
        int carrierIndex = this.ContainsFinalInstruction(
            resolution,
            AsnInstructionKind.EmbedValues) ? 1 : 0;
        if (carrierIndex >= resolution.Components.Length)
        {
            return false;
        }

        ResolvedComponentSyntax carrier = resolution.Components[carrierIndex];
        return carrier != null
            && carrier.Component != null
            && !carrier.Component.Optional
            && !carrier.Component.HasDefaultValue
            && carrier.TypeResolution != null
            && carrier.TypeResolution.EffectiveKind == AsnTypeKind.SequenceOf
            && carrier.TypeResolution.InnerType != null
            && carrier.TypeResolution.InnerType.EffectiveKind
                == AsnTypeKind.Enumerated;
    }

    internal bool IsQNameResolutionShape(ResolvedTypeSyntax resolution)
    {
        if (resolution.Components.Length != 2)
        {
            return false;
        }

        ResolvedComponentSyntax first = resolution.Components[0];
        ResolvedComponentSyntax second = resolution.Components[1];
        return first != null
            && second != null
            && first.Component != null
            && first.Component.Optional
            && !first.Component.HasDefaultValue
            && first.TypeResolution != null
            && second.TypeResolution != null
            && first.TypeResolution.EffectiveKind == AsnTypeKind.Utf8String
            && second.TypeResolution.EffectiveKind == AsnTypeKind.Utf8String;
    }

    internal bool HasCharacterChoiceAlternatives(
        ResolvedTypeSyntax resolution)
    {
        if (resolution.Components.Length == 0)
        {
            return false;
        }

        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedComponentSyntax component = resolution.Components[index];
            if (component == null
                || component.TypeResolution == null
                || !component.TypeResolution.ParameterReference
                    && !this.IsCharacterEncodableResolution(
                        component.TypeResolution))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool IsCharacterEncodableResolution(
        ResolvedTypeSyntax resolution)
    {
        if (this.IsCharacterEncodableKind(resolution.EffectiveKind))
        {
            return true;
        }

        if ((resolution.EffectiveKind == AsnTypeKind.OctetString
                || resolution.EffectiveKind == AsnTypeKind.OpenType)
            && this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.Base64))
        {
            return true;
        }

        if ((resolution.EffectiveKind == AsnTypeKind.SequenceOf
                || resolution.EffectiveKind == AsnTypeKind.SetOf)
            && this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.List))
        {
            return true;
        }

        if (resolution.EffectiveKind == AsnTypeKind.Sequence
            && this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.UseQName))
        {
            return true;
        }

        return resolution.EffectiveKind == AsnTypeKind.Choice
            && this.ContainsFinalInstruction(
                resolution,
                AsnInstructionKind.UseUnion);
    }

    internal bool ContainsFinalInstruction(
        ResolvedTypeSyntax resolution,
        AsnInstructionKind kind)
    {
        int index = 0;
        while (index < resolution.FinalEncodingInstructions.Length)
        {
            if (resolution.FinalEncodingInstructions[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasUtf8CollectionElement(ResolvedTypeSyntax resolution)
    {
        ResolvedTypeSyntax? current = resolution;
        int depth = 0;
        while (current != null && depth < 32)
        {
            if ((current.EffectiveKind == AsnTypeKind.SequenceOf
                    || current.EffectiveKind == AsnTypeKind.SetOf)
                && current.InnerType != null
                && current.InnerType.EffectiveKind == AsnTypeKind.Utf8String)
            {
                return true;
            }

            if (current.EffectiveType != null
                && current.EffectiveType.ElementType != null
                && current.EffectiveType.ElementType.Kind
                    == AsnTypeKind.Utf8String)
            {
                return true;
            }

            current = current.InnerType == null
                ? current.AliasTarget
                : current.InnerType;
            depth = depth + 1;
        }

        return false;
    }

    internal bool IsWildcardRestrictionValid(string[] arguments)
    {
        if (arguments.Length == 0)
        {
            return true;
        }

        if (arguments.Length < 2
            || (arguments[0] != "FROM" && arguments[0] != "EXCEPT"))
        {
            return false;
        }

        bool absent = false;
        System.Collections.Generic.List<string> uris =
            new System.Collections.Generic.List<string>();
        int index = 1;
        while (index < arguments.Length)
        {
            string argument = arguments[index];
            if (argument == "ABSENT")
            {
                if (absent)
                {
                    return false;
                }

                absent = true;
            }
            else
            {
                if (argument.Length < 3
                    || argument[0] != '"'
                    || argument[argument.Length - 1] != '"'
                    || uris.Contains(argument))
                {
                    return false;
                }

                uris.Add(argument);
            }

            index = index + 1;
        }

        return true;
    }

    internal void ValidateWildcardComponents(ResolvedTypeSyntax type)
    {
        if (type.EffectiveKind != AsnTypeKind.Sequence
            && type.EffectiveKind != AsnTypeKind.Set)
        {
            return;
        }

        int count = 0;
        int index = 0;
        while (index < type.Components.Length)
        {
            ResolvedComponentSyntax component = type.Components[index];
            ResolvedTypeSyntax? componentType = component.TypeResolution;
            if (componentType != null
                && this.HasFinalInstruction(
                    componentType,
                    AsnInstructionKind.AnyAttributes))
            {
                count = count + 1;
                if (component.Component == null
                    || component.Component.Optional
                    || component.Component.HasDefaultValue)
                {
                    this.AddDiagnostic(
                        309,
                        "An ANY-ATTRIBUTES component must be mandatory.",
                        componentType.SourceSyntax == null
                            ? null
                            : componentType.SourceSyntax.Span);
                }
            }

            index = index + 1;
        }

        if (count > 1)
        {
            this.AddDiagnostic(
                310,
                "A sequence or set can contain only one ANY-ATTRIBUTES component.",
                type.SourceSyntax == null ? null : type.SourceSyntax.Span);
        }
    }

    internal bool HasFinalInstruction(
        ResolvedTypeSyntax type,
        AsnInstructionKind kind)
    {
        int index = 0;
        while (index < type.FinalEncodingInstructions.Length)
        {
            if (type.FinalEncodingInstructions[index].Kind == kind)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsCharacterStringKind(AsnTypeKind type)
    {
        return type == AsnTypeKind.Utf8String
            || type == AsnTypeKind.NumericString
            || type == AsnTypeKind.PrintableString
            || type == AsnTypeKind.TeletexString
            || type == AsnTypeKind.VideotexString
            || type == AsnTypeKind.Ia5String
            || type == AsnTypeKind.GraphicString
            || type == AsnTypeKind.VisibleString
            || type == AsnTypeKind.GeneralString
            || type == AsnTypeKind.UniversalString
            || type == AsnTypeKind.BmpString;
    }

    internal bool IsUnmodifiedInstructionAllowed(AsnInstructionKind instruction)
    {
        return this.IsKindListed(
            instruction,
            ",3,4,10,11,13,14,17,22,");
    }

    internal bool IsUnmodifiedCombinationAllowed(
        AsnInstructionKind left,
        AsnInstructionKind right)
    {
        switch (left)
        {
            case AsnInstructionKind.Attribute:
                return this.IsKindListed(right, ",4,10,11,14,17,22,");
            case AsnInstructionKind.Base64:
                return this.IsKindListed(right, ",3,11,13,");
            case AsnInstructionKind.List:
                return this.IsKindListed(right, ",3,11,13,");
            case AsnInstructionKind.Name:
                return this.IsKindListed(right, ",3,4,10,13,14,17,22,");
            case AsnInstructionKind.PiOrComment:
                return this.IsKindListed(right, ",4,10,11,14,17,22,");
            case AsnInstructionKind.Text:
                return this.IsKindListed(right, ",3,11,13,");
            case AsnInstructionKind.UseNumber:
                return this.IsKindListed(right, ",3,11,13,");
            case AsnInstructionKind.Whitespace:
                return this.IsKindListed(right, ",3,11,13,");
        }

        return false;
    }

    internal bool IsModifiedCombinationAllowed(
        AsnInstructionKind left,
        AsnInstructionKind right)
    {
        if (left == AsnInstructionKind.Name
            || left == AsnInstructionKind.Namespace
            || right == AsnInstructionKind.Name
            || right == AsnInstructionKind.Namespace)
        {
            return true;
        }

        switch (left)
        {
            case AsnInstructionKind.AnyAttributes:
            case AsnInstructionKind.AnyElement:
                return false;
            case AsnInstructionKind.Attribute:
                return this.IsKindListed(right, ",4,5,10,14,17,19,21,22,");
            case AsnInstructionKind.Base64:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.Decimal:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.DefaultForEmpty:
                return this.IsKindListed(right, ",4,5,8,10,13,14,16,17,18,19,21,22,");
            case AsnInstructionKind.EmbedValues:
                return this.IsKindListed(right, ",6,13,16,18,");
            case AsnInstructionKind.List:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.PiOrComment:
                return this.IsKindListed(right, ",4,5,6,8,10,14,16,17,18,19,20,21,22,");
            case AsnInstructionKind.Text:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.Untagged:
                return this.IsKindListed(right, ",4,5,10,14,17,19,21,22,");
            case AsnInstructionKind.UseNil:
                return this.IsKindListed(right, ",6,8,13,18,");
            case AsnInstructionKind.UseNumber:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.UseOrder:
                return this.IsKindListed(right, ",6,8,13,16,");
            case AsnInstructionKind.UseQName:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.UseType:
                return this.IsKindListed(right, ",13,");
            case AsnInstructionKind.UseUnion:
                return this.IsKindListed(right, ",3,6,13,15,");
            case AsnInstructionKind.Whitespace:
                return this.IsKindListed(right, ",3,6,13,15,");
        }

        return false;
    }

    internal bool IsKindListed(AsnInstructionKind kind, string list)
    {
        return list.Contains("," + ((int)kind).ToString() + ",");
    }

    internal SymbolTable? FindDeclaringTable(ResolvedTypeSyntax type)
    {
        SymbolTableEntry? declaration = type.SourceDeclaration;
        if (declaration == null)
        {
            return null;
        }

        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            SymbolTable table = this.Tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.Symbols.Length)
            {
                if (table.Symbols[symbolIndex] == declaration)
                {
                    return table;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal void AddDiagnostic(int number, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            number,
            message,
            span,
            null);
    }
}
