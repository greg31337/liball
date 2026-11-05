namespace asn1;

internal class AutomaticTagger
{
    internal DiagnosticBag Diagnostics;
    internal SymbolTable[] Tables = [];

    internal AutomaticTagger(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal AutomaticTaggingResult ApplyAutomaticTags(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        System.Collections.Generic.List<SymbolTableEntry> types =
            new System.Collections.Generic.List<SymbolTableEntry>();
        bool success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                SymbolTableEntry entry = tables[tableIndex].TypeSymbols[typeIndex];
                types.Add(entry);
                if (entry.ResolvedType == null
                    || !this.ProcessResolvedType(entry.ResolvedType, tables[tableIndex]))
                {
                    success = false;
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return new AutomaticTaggingResult
        {
            Types = types.ToArray(),
            Success = success
        };
    }

    internal bool ProcessResolvedType(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        if (resolution.AutomaticTaggingState == 2)
        {
            return resolution.AutomaticTaggingSuccess;
        }

        if (resolution.AutomaticTaggingState == 1)
        {
            return true;
        }

        resolution.AutomaticTaggingState = 1;
        bool success = resolution.TagResolutionSuccess;
        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        if (syntax == null)
        {
            if (!this.ProcessComponentTypes(resolution, module))
            {
                success = false;
            }
        }
        else if (syntax.Type.Kind == AsnTypeKind.Defined)
        {
            if (resolution.AliasTarget != null)
            {
                SymbolTable targetModule = this.FindAutomaticModule(resolution.AliasTarget, module);
                success = this.ProcessResolvedType(resolution.AliasTarget, targetModule) && success;
            }
        }
        else if (syntax.Type.Kind == AsnTypeKind.Selection)
        {
            ResolvedTypeSyntax? selectedType = resolution.SelectedComponent == null
                ? null
                : resolution.SelectedComponent.TypeResolution;
            if (selectedType != null)
            {
                SymbolTable selectedModule = this.FindAutomaticModule(selectedType, module);
                success = this.ProcessResolvedType(selectedType, selectedModule) && success;
            }
        }
        else if (syntax.Type.Kind == AsnTypeKind.Tagged
            || syntax.Type.Kind == AsnTypeKind.Prefixed
            || syntax.Type.Kind == AsnTypeKind.SequenceOf
            || syntax.Type.Kind == AsnTypeKind.SetOf)
        {
            if (resolution.InnerType != null)
            {
                SymbolTable innerModule = this.FindAutomaticModule(resolution.InnerType, module);
                success = this.ProcessResolvedType(resolution.InnerType, innerModule) && success;
            }
        }
        else if (syntax.Type.Kind == AsnTypeKind.Sequence
            || syntax.Type.Kind == AsnTypeKind.Set
            || syntax.Type.Kind == AsnTypeKind.Choice)
        {
            if (!this.ProcessComponentTypes(resolution, module))
            {
                success = false;
            }

            this.ApplyComponentTagging(resolution, module);
            if ((syntax.Type.Kind == AsnTypeKind.Set
                    || syntax.Type.Kind == AsnTypeKind.Choice)
                && !this.ValidateComponentTagCollisions(resolution))
            {
                success = false;
            }
        }

        resolution.AutomaticTaggingSuccess = success;
        resolution.AutomaticTaggingState = 2;
        return success;
    }

    internal bool ProcessComponentTypes(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        bool success = true;
        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedTypeSyntax? component = resolution.Components[index].TypeResolution;
            if (component != null)
            {
                SymbolTable componentModule = this.FindAutomaticModule(component, module);
                if (!this.ProcessResolvedType(component, componentModule))
                {
                    success = false;
                }
            }

            index = index + 1;
        }

        return success;
    }

    internal void ApplyComponentTagging(ResolvedTypeSyntax resolution, SymbolTable module)
    {
        ParsedTypeSyntax? syntax = resolution.SourceSyntax;
        if (syntax == null)
        {
            return;
        }

        bool automaticModule = module.Module != null
            && module.Module?.TagDefault == AsnTaggingMode.Automatic;
        bool textuallyTagged = this.HasTextuallyTaggedComponent(syntax);
        bool apply = automaticModule
            && syntax.ComponentSyntax.Length > 0
            && !textuallyTagged;
        resolution.AutomaticTaggingApplied = apply;
        resolution.AutomaticTaggingSuppressed = automaticModule && textuallyTagged;
        if (!apply)
        {
            int componentIndex = 0;
            while (componentIndex < resolution.Components.Length)
            {
                ResolvedComponentSyntax component = resolution.Components[componentIndex];
                component.AutomaticTag = null;
                component.TagChain = component.TypeResolution == null
                    ? []
                    : this.CopyAutomaticChain(component.TypeResolution.TagChain);
                componentIndex = componentIndex + 1;
            }

            return;
        }

        int next = this.ApplyAutomaticPartition(resolution, module, false, 0);
        this.ApplyAutomaticPartition(resolution, module, true, next);
    }

    internal int ApplyAutomaticPartition(
        ResolvedTypeSyntax resolution,
        SymbolTable module,
        bool extension,
        int firstIndex)
    {
        int automaticIndex = firstIndex;
        int componentIndex = 0;
        while (componentIndex < resolution.Components.Length)
        {
            ResolvedComponentSyntax component = resolution.Components[componentIndex];
            if (component.Component != null
                && component.Component?.Extension == extension)
            {
                ResolvedTagSyntax[] intrinsic = component.TypeResolution == null
                    ? []
                    : component.TypeResolution.TagChain;
                AsnTaggingMode mode = intrinsic.Length == 0
                    ? AsnTaggingMode.Explicit
                    : AsnTaggingMode.Implicit;
                ResolvedTagSyntax tag = this.CreateAutomaticTag(
                    resolution,
                    component,
                    module,
                    automaticIndex,
                    mode);
                component.AutomaticTag = tag;
                component.TagChain = mode == AsnTaggingMode.Explicit
                    ? this.PrependAutomaticTag(tag, intrinsic)
                    : this.ReplaceAutomaticTag(tag, intrinsic);
                automaticIndex = automaticIndex + 1;
            }

            componentIndex = componentIndex + 1;
        }

        return automaticIndex;
    }

    internal bool HasTextuallyTaggedComponent(ParsedTypeSyntax syntax)
    {
        int index = 0;
        while (index < syntax.ComponentSyntax.Length)
        {
            ParsedComponentSyntax component = syntax.ComponentSyntax[index];
            if (!component.ComponentsOf
                && component.TypeSyntax != null
                && this.IsTextuallyTaggedType(component.TypeSyntax))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsTextuallyTaggedType(ParsedTypeSyntax syntax)
    {
        ParsedTypeSyntax? current = syntax;
        while (current != null)
        {
            if (current.Type.Kind == AsnTypeKind.Tagged)
            {
                return true;
            }

            if (current.Type.Kind != AsnTypeKind.Prefixed)
            {
                return false;
            }

            current = current.InnerTypeSyntax;
        }

        return false;
    }

    internal ResolvedTagSyntax CreateAutomaticTag(
        ResolvedTypeSyntax containingType,
        ResolvedComponentSyntax component,
        SymbolTable module,
        int automaticIndex,
        AsnTaggingMode mode)
    {
        ParsedComponentSyntax? origin = this.GetComponentUseSite(component);
        return new ResolvedTagSyntax
        {
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Mode = mode,
                Number = new System.Numerics.BigInteger(automaticIndex)
            },
            OriginSyntax = origin == null ? null : origin.TypeSyntax,
            OriginComponentSyntax = origin,
            OriginDeclaration = containingType.SourceDeclaration,
            OriginModule = module.Module,
            DeclaredMode = AsnTaggingMode.Unspecified,
            DefaultModeApplied = true,
            HasNumericNumber = true,
            AutomaticallyGenerated = true,
            AutomaticIndex = automaticIndex
        };
    }

    internal ParsedComponentSyntax? GetComponentUseSite(ResolvedComponentSyntax component)
    {
        if (component.Provenance.Length == 0)
        {
            return null;
        }

        return component.Provenance[component.Provenance.Length - 1].ComponentSyntax;
    }

    internal bool ValidateComponentTagCollisions(ResolvedTypeSyntax resolution)
    {
        bool success = true;
        int firstIndex = 0;
        while (firstIndex < resolution.Components.Length)
        {
            LeadingTagSet first = this.GetLeadingTagSet(resolution.Components[firstIndex]);
            int secondIndex = firstIndex + 1;
            while (secondIndex < resolution.Components.Length)
            {
                LeadingTagSet second = this.GetLeadingTagSet(resolution.Components[secondIndex]);
                if (first.Open || second.Open)
                {
                    this.AddOpenCollisionDiagnostic(
                        resolution.Components[firstIndex],
                        resolution.Components[secondIndex]);
                    success = false;
                }
                else
                {
                    ResolvedTagSyntax? duplicate = this.FindDuplicateLeadingTag(first, second);
                    if (duplicate != null)
                    {
                        this.AddDuplicateTagDiagnostic(
                            resolution.Components[firstIndex],
                            resolution.Components[secondIndex],
                            duplicate);
                        success = false;
                    }
                }

                secondIndex = secondIndex + 1;
            }

            firstIndex = firstIndex + 1;
        }

        return success;
    }

    internal LeadingTagSet GetLeadingTagSet(ResolvedComponentSyntax component)
    {
        LeadingTagSet result = new LeadingTagSet();
        if (component.TagChain.Length > 0)
        {
            result.Tags = [component.TagChain[0]];
            return result;
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> path =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        if (component.TypeResolution == null)
        {
            result.Open = true;
        }
        else
        {
            this.CollectLeadingTags(component.TypeResolution, result, path);
        }

        return result;
    }

    internal void CollectLeadingTags(
        ResolvedTypeSyntax resolution,
        LeadingTagSet result,
        System.Collections.Generic.List<ResolvedTypeSyntax> path)
    {
        if (resolution.TagChain.Length > 0)
        {
            this.AppendLeadingTag(result, resolution.TagChain[0]);
            return;
        }

        if (this.ContainsLeadingPath(path, resolution))
        {
            result.Open = true;
            return;
        }

        if (resolution.EffectiveKind != AsnTypeKind.Choice)
        {
            result.Open = true;
            return;
        }

        if (resolution.EffectiveType != null && resolution.EffectiveType?.Extensible == true)
        {
            result.Open = true;
        }

        path.Add(resolution);
        int index = 0;
        while (index < resolution.Components.Length)
        {
            ResolvedComponentSyntax component = resolution.Components[index];
            if (component.TagChain.Length > 0)
            {
                this.AppendLeadingTag(result, component.TagChain[0]);
            }
            else if (component.TypeResolution == null)
            {
                result.Open = true;
            }
            else
            {
                this.CollectLeadingTags(component.TypeResolution, result, path);
            }

            index = index + 1;
        }

        path.RemoveAt(path.Count - 1);
    }

    internal void AppendLeadingTag(LeadingTagSet result, ResolvedTagSyntax tag)
    {
        int index = 0;
        while (index < result.Tags.Length)
        {
            if (this.TagsEqual(result.Tags[index], tag))
            {
                return;
            }

            index = index + 1;
        }

        ResolvedTagSyntax[] replacement = new ResolvedTagSyntax[result.Tags.Length + 1];
        System.Array.Copy(result.Tags, 0, replacement, 0, result.Tags.Length);
        replacement[replacement.Length - 1] = tag;
        result.Tags = replacement;
    }

    internal bool ContainsLeadingPath(
        System.Collections.Generic.List<ResolvedTypeSyntax> path,
        ResolvedTypeSyntax resolution)
    {
        int index = 0;
        while (index < path.Count)
        {
            if (path[index] == resolution)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal ResolvedTagSyntax? FindDuplicateLeadingTag(
        LeadingTagSet first,
        LeadingTagSet second)
    {
        int firstIndex = 0;
        while (firstIndex < first.Tags.Length)
        {
            int secondIndex = 0;
            while (secondIndex < second.Tags.Length)
            {
                if (this.TagsEqual(first.Tags[firstIndex], second.Tags[secondIndex]))
                {
                    return first.Tags[firstIndex];
                }

                secondIndex = secondIndex + 1;
            }

            firstIndex = firstIndex + 1;
        }

        return null;
    }

    internal bool TagsEqual(ResolvedTagSyntax first, ResolvedTagSyntax second)
    {
        if (first.Tag.Class != second.Tag.Class)
        {
            return false;
        }

        if (first.HasNumericNumber && second.HasNumericNumber)
        {
            return first.Tag.Number == second.Tag.Number;
        }

        return !first.HasNumericNumber
            && !second.HasNumericNumber
            && first.NumberReference.Length > 0
            && first.NumberReference == second.NumberReference;
    }

    internal ResolvedTagSyntax[] PrependAutomaticTag(
        ResolvedTagSyntax outer,
        ResolvedTagSyntax[] inner)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[inner.Length + 1];
        result[0] = outer;
        int index = 0;
        while (index < inner.Length)
        {
            result[index + 1] = inner[index];
            index = index + 1;
        }

        return result;
    }

    internal ResolvedTagSyntax[] ReplaceAutomaticTag(
        ResolvedTagSyntax outer,
        ResolvedTagSyntax[] inner)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[inner.Length];
        result[0] = outer;
        int index = 1;
        while (index < inner.Length)
        {
            result[index] = inner[index];
            index = index + 1;
        }

        return result;
    }

    internal ResolvedTagSyntax[] CopyAutomaticChain(ResolvedTagSyntax[] source)
    {
        ResolvedTagSyntax[] result = new ResolvedTagSyntax[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal SymbolTable FindAutomaticModule(
        ResolvedTypeSyntax resolution,
        SymbolTable fallback)
    {
        SymbolTableEntry? declaration = resolution.SourceDeclaration;
        if (declaration == null)
        {
            return fallback;
        }

        int tableIndex = 0;
        while (tableIndex < this.Tables.Length)
        {
            int symbolIndex = 0;
            while (symbolIndex < this.Tables[tableIndex].Symbols.Length)
            {
                if (this.Tables[tableIndex].Symbols[symbolIndex] == declaration)
                {
                    return this.Tables[tableIndex];
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return fallback;
    }

    internal AsnSourceSpan? GetAutomaticComponentSpan(ResolvedComponentSyntax component)
    {
        ParsedComponentSyntax? syntax = this.GetComponentUseSite(component);
        return syntax == null ? null : syntax.Span;
    }

    internal string GetAutomaticComponentName(ResolvedComponentSyntax component)
    {
        return component.Component == null ? "<open>" : component.Component.Name;
    }

    internal string FormatResolvedTag(ResolvedTagSyntax tag)
    {
        string number = tag.HasNumericNumber
            ? tag.Tag.Number.ToString(System.Globalization.CultureInfo.InvariantCulture)
            : tag.NumberReference;
        return tag.Tag.Class.ToString() + " " + number;
    }

    internal void AddDuplicateTagDiagnostic(
        ResolvedComponentSyntax first,
        ResolvedComponentSyntax second,
        ResolvedTagSyntax duplicate)
    {
        AsnSourceSpan? firstSpan = this.GetAutomaticComponentSpan(first);
        AsnDiagnosticRelatedLocation[] related = firstSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The first component with this effective tag is here.",
                Span = firstSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.DuplicateEffectiveTag,
            "Components '" + this.GetAutomaticComponentName(first) + "' and '"
                + this.GetAutomaticComponentName(second) + "' share effective tag "
                + this.FormatResolvedTag(duplicate) + ".",
            this.GetAutomaticComponentSpan(second),
            related);
    }

    internal void AddOpenCollisionDiagnostic(
        ResolvedComponentSyntax first,
        ResolvedComponentSyntax second)
    {
        AsnSourceSpan? firstSpan = this.GetAutomaticComponentSpan(first);
        AsnDiagnosticRelatedLocation[] related = firstSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The first potentially overlapping component is here.",
                Span = firstSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            SchemaDiagnosticCodes.OpenTagCollision,
            "Open tag sets for components '" + this.GetAutomaticComponentName(first)
                + "' and '" + this.GetAutomaticComponentName(second) + "' can overlap.",
            this.GetAutomaticComponentSpan(second),
            related);
    }
}
