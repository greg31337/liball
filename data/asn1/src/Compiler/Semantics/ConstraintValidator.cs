namespace asn1;

internal class ConstraintValidator
{
    internal ValueResolver ValueResolver;
    internal DiagnosticBag Diagnostics;
    internal IntegerRangeAlgebra Algebra = new IntegerRangeAlgebra();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> ValidatedTypes =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> CycleTypes =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<int> CycleStates =
        new System.Collections.Generic.List<int>();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> CycleStackTypes =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<NormalizedConstraintNode?> CycleStackEdges =
        new System.Collections.Generic.List<NormalizedConstraintNode?>();
    internal bool Success = true;

    internal ConstraintValidator(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Diagnostics = valueResolver.Diagnostics;
    }

    internal bool ValidateTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.ValidateType(type);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.DetectTypeCycles(type, null);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal void ValidateType(ResolvedTypeSyntax type)
    {
        if (this.ValidatedTypes.Contains(type))
        {
            return;
        }

        this.ValidatedTypes.Add(type);
        type.ConstraintValidationSuccess = true;
        type.EffectiveConstraintEmpty = false;
        int graphIndex = 0;
        while (graphIndex < type.NormalizedConstraints.Length)
        {
            this.ValidateGraph(
                type.NormalizedConstraints[graphIndex],
                this.CreateContext(type, type.EffectiveKind, 0),
                type);
            graphIndex = graphIndex + 1;
        }

        this.ValidateKnownEmptyConstraint(type);
        this.ValidateFiniteValueApplications(type);
        this.ValidateComponentContradictions(type);
        this.ValidateTimeContradictions(type);
        if (type.AliasTarget != null)
        {
            this.ValidateType(type.AliasTarget);
        }

        if (type.InnerType != null)
        {
            this.ValidateType(type.InnerType);
        }

        if (type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.ValidateType(type.SelectedComponent.TypeResolution);
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[componentIndex].TypeResolution;
            if (componentType != null)
            {
                this.ValidateType(componentType);
            }

            componentIndex = componentIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.ValidateType(type.ConstraintTypes[constraintIndex]);
            constraintIndex = constraintIndex + 1;
        }
    }

    internal ConstraintValidationContext CreateContext(
        ResolvedTypeSyntax type,
        AsnTypeKind expectedKind,
        int domainMode)
    {
        return new ConstraintValidationContext
        {
            GoverningType = type,
            ExpectedKind = expectedKind,
            DomainMode = domainMode
        };
    }

    internal void ValidateGraph(
        NormalizedConstraintGraph graph,
        ConstraintValidationContext context,
        ResolvedTypeSyntax ownerType)
    {
        ConstraintValidationContext effectiveContext = graph.GoverningType == null
            || context.DomainMode != 0
            ? context
            : this.CreateContext(
                graph.GoverningType,
                graph.GoverningType.EffectiveKind,
                0);
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.ValidateNode(graph.RootPartition.Expression, effectiveContext, ownerType);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.ValidateNode(
                graph.ExtensionPartition.Expression,
                effectiveContext,
                ownerType);
        }
    }

    internal void ValidateNode(
        NormalizedConstraintNode node,
        ConstraintValidationContext context,
        ResolvedTypeSyntax ownerType)
    {
        if (!this.IsConstraintAllowed(node.Kind, context))
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.InvalidConstraintApplication,
                "A " + node.Kind.ToString() + " constraint cannot be applied to "
                    + context.ExpectedKind.ToString() + ".",
                ownerType,
                this.GetNodeSpan(node),
                null);
        }

        if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            this.ValidateContainedSubtype(node, context, ownerType);
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.ValidateNode(node.Children[childIndex], context, ownerType);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            ConstraintValidationContext nestedContext = context;
            if (node.Kind == AsnConstraintKind.Size && context.GoverningType != null)
            {
                nestedContext = this.CreateContext(
                    context.GoverningType,
                    AsnTypeKind.Integer,
                    1);
            }
            else if (node.Kind == AsnConstraintKind.PermittedAlphabet
                && context.GoverningType != null)
            {
                nestedContext = this.CreateContext(
                    context.GoverningType,
                    context.ExpectedKind,
                    2);
            }

            this.ValidateGraph(node.NestedConstraints[nestedIndex], nestedContext, ownerType);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal bool IsConstraintAllowed(
        AsnConstraintKind constraintKind,
        ConstraintValidationContext context)
    {
        if (constraintKind == AsnConstraintKind.Unknown
            || constraintKind == AsnConstraintKind.Union
            || constraintKind == AsnConstraintKind.Intersection
            || constraintKind == AsnConstraintKind.Except
            || constraintKind == AsnConstraintKind.AllExcept
            || constraintKind == AsnConstraintKind.Extension
            || constraintKind == AsnConstraintKind.Exception
            || constraintKind == AsnConstraintKind.UserDefined)
        {
            return true;
        }

        if (context.DomainMode == 1)
        {
            return constraintKind == AsnConstraintKind.SingleValue
                || constraintKind == AsnConstraintKind.ContainedSubtype
                || constraintKind == AsnConstraintKind.ValueRange;
        }

        if (context.DomainMode == 2)
        {
            return constraintKind == AsnConstraintKind.SingleValue
                || constraintKind == AsnConstraintKind.ContainedSubtype
                || constraintKind == AsnConstraintKind.ValueRange;
        }

        if (constraintKind == AsnConstraintKind.SingleValue)
        {
            return context.ExpectedKind != AsnTypeKind.OpenType
                && context.ExpectedKind != AsnTypeKind.Unknown
                && context.ExpectedKind != AsnTypeKind.Defined;
        }

        if (constraintKind == AsnConstraintKind.ContainedSubtype)
        {
            return this.IsContainedSubtypeType(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.ValueRange)
        {
            return context.ExpectedKind == AsnTypeKind.Integer
                || context.ExpectedKind == AsnTypeKind.Enumerated
                || context.ExpectedKind == AsnTypeKind.Real
                || this.IsRestrictedCharacterString(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.Size)
        {
            return this.IsSizeType(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.PermittedAlphabet)
        {
            return this.IsAlphabetType(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.Pattern)
        {
            return this.IsRestrictedCharacterString(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.PropertySettings)
        {
            return this.IsTimeSettingsType(context.ExpectedKind);
        }

        if (constraintKind == AsnConstraintKind.WithComponent
            || constraintKind == AsnConstraintKind.WithComponents
            || constraintKind == AsnConstraintKind.Type
            || constraintKind == AsnConstraintKind.Contents
            || constraintKind == AsnConstraintKind.SimpleTable
            || constraintKind == AsnConstraintKind.ComponentRelation)
        {
            return true;
        }

        return true;
    }

    internal bool IsContainedSubtypeType(AsnTypeKind kind)
    {
        return kind != AsnTypeKind.External
            && kind != AsnTypeKind.EmbeddedPdv
            && kind != AsnTypeKind.CharacterString
            && kind != AsnTypeKind.OpenType
            && kind != AsnTypeKind.Unknown
            && kind != AsnTypeKind.Defined;
    }

    internal bool IsSizeType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BitString
            || kind == AsnTypeKind.OctetString
            || kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || this.IsRestrictedCharacterString(kind)
            || kind == AsnTypeKind.SequenceOf
            || kind == AsnTypeKind.SetOf;
    }

    internal bool IsAlphabetType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
            || this.IsRestrictedCharacterString(kind);
    }

    internal bool IsRestrictedCharacterString(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString;
    }

    internal bool IsTimeSettingsType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal void ValidateContainedSubtype(
        NormalizedConstraintNode node,
        ConstraintValidationContext context,
        ResolvedTypeSyntax ownerType)
    {
        ResolvedTypeSyntax? contained = this.ResolveContainedType(node, context);
        if (contained == null || context.GoverningType == null)
        {
            return;
        }

        bool mapping = true;
        if (context.DomainMode == 1)
        {
            mapping = contained.EffectiveKind == AsnTypeKind.Integer;
        }
        else if (context.DomainMode == 2)
        {
            mapping = this.IsAlphabetType(contained.EffectiveKind)
                || contained.EffectiveKind == AsnTypeKind.CharacterString;
        }
        else
        {
            mapping = this.HaveValueMapping(
                context.GoverningType,
                contained,
                new System.Collections.Generic.List<ResolvedTypeSyntax>(),
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
        }

        if (!mapping)
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.NonValuePreservingSubtype,
                "The contained subtype has no value-preserving mapping to its parent type.",
                ownerType,
                this.GetNodeSpan(node),
                this.CreateTypeRelatedLocation(
                    contained,
                    "The incompatible contained subtype is declared here."));
            return;
        }

        if (context.DomainMode == 0
            && !this.HaveKnownConstraintOverlap(context.GoverningType, contained))
        {
            this.AddDiagnostic(
                ConstraintDiagnosticCodes.NonValuePreservingSubtype,
                "The contained subtype root has no value that maps to the parent type root.",
                ownerType,
                this.GetNodeSpan(node),
                this.CreateTypeRelatedLocation(
                    contained,
                    "The disjoint contained subtype is declared here."));
        }
    }

    internal ResolvedTypeSyntax? ResolveContainedType(
        NormalizedConstraintNode node,
        ConstraintValidationContext context)
    {
        if (node.ResolvedContainedSubtype != null)
        {
            return node.ResolvedContainedSubtype;
        }

        ParsedTypeSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.TypeSyntax;
        ResolvedTypeSyntax? governingType = context.GoverningType;
        SymbolTableEntry? declaration = governingType == null
            ? null
            : governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (syntax == null || declaration == null || module == null)
        {
            return null;
        }

        ResolvedTypeSyntax? contained = null;
        if (syntax.Type.Kind == AsnTypeKind.Defined)
        {
            TypeReferenceBinding binding = this.ValueResolver.TypeResolver.FindTypeBinding(
                module,
                syntax.ReferenceModule,
                syntax.ReferenceName);
            if (binding.Target != null)
            {
                contained = binding.Target.ResolvedType;
            }
        }
        else
        {
            contained = this.ValueResolver.TypeResolver.ResolveNestedType(
                syntax,
                module,
                declaration,
                true);
            ConstraintNormalizer normalizer = new ConstraintNormalizer();
            normalizer.NormalizeType(
                contained,
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
            normalizer.CombineTypeConstraints(
                contained,
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
            this.ValueResolver.ResolveNamedValuesInType(
                contained,
                new System.Collections.Generic.List<ResolvedTypeSyntax>());
        }

        node.ResolvedContainedSubtype = contained;
        return contained;
    }

    internal bool HaveValueMapping(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained,
        System.Collections.Generic.List<ResolvedTypeSyntax> parentPath,
        System.Collections.Generic.List<ResolvedTypeSyntax> containedPath)
    {
        if (parent.EffectiveKind != contained.EffectiveKind)
        {
            return false;
        }

        int existingIndex = 0;
        while (existingIndex < parentPath.Count)
        {
            if (parentPath[existingIndex] == parent
                && containedPath[existingIndex] == contained)
            {
                return true;
            }

            existingIndex = existingIndex + 1;
        }

        parentPath.Add(parent);
        containedPath.Add(contained);
        bool result = true;
        if (parent.EffectiveKind == AsnTypeKind.Enumerated)
        {
            result = this.HaveEnumerationMapping(parent, contained);
        }
        else if (parent.EffectiveKind == AsnTypeKind.Sequence)
        {
            result = this.HaveOrderedComponentMapping(
                parent,
                contained,
                parentPath,
                containedPath);
        }
        else if (parent.EffectiveKind == AsnTypeKind.Choice)
        {
            result = this.HaveChoiceComponentMapping(
                parent,
                contained,
                parentPath,
                containedPath);
        }
        else if (parent.EffectiveKind == AsnTypeKind.Set)
        {
            result = this.HaveSetComponentMapping(
                parent,
                contained,
                parentPath,
                containedPath);
        }
        else if (parent.EffectiveKind == AsnTypeKind.SequenceOf
            || parent.EffectiveKind == AsnTypeKind.SetOf)
        {
            result = parent.InnerType != null
                && contained.InnerType != null
                && this.HaveValueMapping(
                    parent.InnerType,
                    contained.InnerType,
                    parentPath,
                    containedPath);
        }

        parentPath.RemoveAt(parentPath.Count - 1);
        containedPath.RemoveAt(containedPath.Count - 1);
        return result;
    }

    internal bool HaveEnumerationMapping(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained)
    {
        ParsedTypeSyntax? parentSyntax = this.FindCanonicalSyntax(parent);
        ParsedTypeSyntax? containedSyntax = this.FindCanonicalSyntax(contained);
        if (parentSyntax == null || containedSyntax == null)
        {
            return false;
        }

        int parentIndex = 0;
        while (parentIndex < parentSyntax.RootNamedValues.Length)
        {
            int containedIndex = 0;
            while (containedIndex < containedSyntax.RootNamedValues.Length)
            {
                if (this.NamedValuesMap(
                    parentSyntax.RootNamedValues[parentIndex],
                    containedSyntax.RootNamedValues[containedIndex]))
                {
                    return true;
                }

                containedIndex = containedIndex + 1;
            }

            parentIndex = parentIndex + 1;
        }

        return false;
    }

    internal ParsedTypeSyntax? FindCanonicalSyntax(ResolvedTypeSyntax type)
    {
        ResolvedTypeSyntax canonical = type.ConstraintDomain == null
            || type.ConstraintDomain.CanonicalType == null
            ? type
            : type.ConstraintDomain.CanonicalType;
        return canonical.SourceSyntax;
    }

    internal bool NamedValuesMap(AsnNamedValue left, AsnNamedValue right)
    {
        AsnValue? leftValue = left.Value;
        AsnValue? rightValue = right.Value;
        return left.Name == right.Name
            && leftValue != null
            && rightValue != null
            && leftValue.HasIntegerValue
            && rightValue.HasIntegerValue
            && leftValue.IntegerValue == rightValue.IntegerValue;
    }

    internal bool HaveChoiceComponentMapping(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained,
        System.Collections.Generic.List<ResolvedTypeSyntax> parentPath,
        System.Collections.Generic.List<ResolvedTypeSyntax> containedPath)
    {
        int parentIndex = 0;
        while (parentIndex < parent.Components.Length)
        {
            AsnComponent? parentSource = parent.Components[parentIndex].Component;
            int containedIndex = parentSource == null
                ? -1
                : this.FindComponentIndex(contained, parentSource.Name);
            if (containedIndex >= 0
                && this.HaveComponentMapping(
                    parent.Components[parentIndex],
                    contained.Components[containedIndex],
                    parentPath,
                    containedPath))
            {
                return true;
            }

            parentIndex = parentIndex + 1;
        }

        return false;
    }

    internal bool HaveOrderedComponentMapping(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained,
        System.Collections.Generic.List<ResolvedTypeSyntax> parentPath,
        System.Collections.Generic.List<ResolvedTypeSyntax> containedPath)
    {
        if (parent.Components.Length != contained.Components.Length)
        {
            return false;
        }

        int index = 0;
        while (index < parent.Components.Length)
        {
            if (!this.HaveComponentMapping(
                parent.Components[index],
                contained.Components[index],
                parentPath,
                containedPath))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool HaveSetComponentMapping(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained,
        System.Collections.Generic.List<ResolvedTypeSyntax> parentPath,
        System.Collections.Generic.List<ResolvedTypeSyntax> containedPath)
    {
        if (parent.Components.Length != contained.Components.Length)
        {
            return false;
        }

        int parentIndex = 0;
        while (parentIndex < parent.Components.Length)
        {
            AsnComponent? source = parent.Components[parentIndex].Component;
            int containedIndex = source == null
                ? -1
                : this.FindComponentIndex(contained, source.Name);
            if (containedIndex < 0
                || !this.HaveComponentMapping(
                    parent.Components[parentIndex],
                    contained.Components[containedIndex],
                    parentPath,
                    containedPath))
            {
                return false;
            }

            parentIndex = parentIndex + 1;
        }

        return true;
    }

    internal int FindComponentIndex(ResolvedTypeSyntax type, string name)
    {
        int index = 0;
        while (index < type.Components.Length)
        {
            AsnComponent? component = type.Components[index].Component;
            if (component != null && component.Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool HaveComponentMapping(
        ResolvedComponentSyntax parent,
        ResolvedComponentSyntax contained,
        System.Collections.Generic.List<ResolvedTypeSyntax> parentPath,
        System.Collections.Generic.List<ResolvedTypeSyntax> containedPath)
    {
        AsnComponent? parentSource = parent.Component;
        AsnComponent? containedSource = contained.Component;
        if (parentSource == null
            || containedSource == null
            || parentSource.Name != containedSource.Name
            || parentSource.Optional != containedSource.Optional
            || parent.TypeResolution == null
            || contained.TypeResolution == null)
        {
            return false;
        }

        return this.HaveValueMapping(
            parent.TypeResolution,
            contained.TypeResolution,
            parentPath,
            containedPath);
    }

    internal bool HaveKnownConstraintOverlap(
        ResolvedTypeSyntax parent,
        ResolvedTypeSyntax contained)
    {
        if ((parent.EffectiveKind == AsnTypeKind.Integer
                || parent.EffectiveKind == AsnTypeKind.Enumerated)
            && contained.IntegerConstraints != null)
        {
            IntegerConstraintSet? parentSet = this.FindParentIntegerSet(parent);
            IntegerConstraintSet? containedSet = contained.IntegerConstraints.Root;
            if (parentSet != null && containedSet != null)
            {
                return !this.Algebra.Intersect([parentSet, containedSet]).IsEmpty;
            }
        }

        if (this.IsSizeType(parent.EffectiveKind)
            && contained.SizeConstraints != null)
        {
            IntegerConstraintSet? parentSizes = this.FindParentSizeSet(parent);
            IntegerConstraintSet? containedSizes = contained.SizeConstraints.Root;
            if (parentSizes != null && containedSizes != null)
            {
                return !this.Algebra.Intersect([parentSizes, containedSizes]).IsEmpty;
            }
        }

        ResolvedTypeSyntax? parentRootType = parent.AliasTarget;
        if (parentRootType != null
            && parentRootType.EffectiveConstraint != null
            && contained.EffectiveConstraint != null)
        {
            ConstraintFiniteValueSet parentValues = this.EvaluateFiniteGraph(
                parentRootType.EffectiveConstraint,
                parentRootType);
            ConstraintFiniteValueSet containedValues = this.EvaluateFiniteGraph(
                contained.EffectiveConstraint,
                contained);
            if (parentValues.Complete && containedValues.Complete)
            {
                return this.IntersectFiniteSets(parentValues, containedValues)
                    .Values.Length > 0;
            }
        }

        return true;
    }

    internal IntegerConstraintSet? FindParentIntegerSet(ResolvedTypeSyntax parent)
    {
        if (parent.AliasTarget != null
            && parent.AliasTarget.IntegerConstraints != null)
        {
            return parent.AliasTarget.IntegerConstraints.Root;
        }

        return parent.IntegerConstraints == null
            ? null
            : parent.IntegerConstraints.Domain;
    }

    internal IntegerConstraintSet? FindParentSizeSet(ResolvedTypeSyntax parent)
    {
        if (parent.AliasTarget != null && parent.AliasTarget.SizeConstraints != null)
        {
            return parent.AliasTarget.SizeConstraints.Root;
        }

        return parent.SizeConstraints == null ? null : parent.SizeConstraints.Domain;
    }

    internal void ValidateKnownEmptyConstraint(ResolvedTypeSyntax type)
    {
        if (type.IntegerConstraints != null
            && type.IntegerConstraints.KnownValues != null
            && type.IntegerConstraints.KnownValues.IsEmpty
            && !type.IntegerConstraints.OpenExtension)
        {
            this.MarkEmptyConstraint(
                type,
                "The effective value constraint contains no possible value.");
            return;
        }

        if (type.PermittedAlphabet != null
            && type.PermittedAlphabet.KnownScalars != null
            && type.PermittedAlphabet.KnownScalars.IsEmpty
            && !type.PermittedAlphabet.OpenExtension)
        {
            IntegerConstraintSet? sizes = type.SizeConstraints == null
                ? null
                : type.SizeConstraints.KnownSizes;
            bool permitsEmptyString = sizes == null
                || sizes.Contains(System.Numerics.BigInteger.Zero);
            if (!permitsEmptyString)
            {
                this.MarkEmptyConstraint(
                    type,
                    "The effective alphabet and size constraints permit no string value.");
            }
        }

        if (type.SizeConstraints != null
            && type.SizeConstraints.KnownSizes != null
            && type.SizeConstraints.KnownSizes.IsEmpty
            && !type.SizeConstraints.OpenExtension)
        {
            type.EffectiveConstraintEmpty = true;
            type.ConstraintValidationSuccess = false;
        }
    }

    internal void ValidateFiniteValueApplications(ResolvedTypeSyntax type)
    {
        if (type.EffectiveKind == AsnTypeKind.Integer
            || type.EffectiveKind == AsnTypeKind.Enumerated
            || type.EffectiveConstraint == null)
        {
            return;
        }

        NormalizedConstraintGraph[] applications =
            type.EffectiveConstraint.Applications.Length == 0
                ? [type.EffectiveConstraint]
                : type.EffectiveConstraint.Applications;
        ConstraintFiniteValueSet? intersection = null;
        int index = 0;
        while (index < applications.Length)
        {
            ConstraintFiniteValueSet candidate = this.EvaluateFiniteGraph(
                applications[index],
                type);
            if (candidate.Complete)
            {
                intersection = intersection == null
                    ? candidate
                    : this.IntersectFiniteSets(intersection, candidate);
                if (intersection.Values.Length == 0)
                {
                    this.MarkEmptyConstraintAtGraph(
                        type,
                        applications[index],
                        "Sequential constraints have no value in common.",
                        this.CreateGraphRelatedLocations(applications, index));
                    return;
                }
            }

            index = index + 1;
        }
    }

    internal ConstraintFiniteValueSet EvaluateFiniteGraph(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax type)
    {
        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Open)
        {
            return new ConstraintFiniteValueSet { SourceGraph = graph };
        }

        ConstraintFiniteValueSet root = graph.RootPartition == null
            || graph.RootPartition.Expression == null
            ? new ConstraintFiniteValueSet { SourceGraph = graph }
            : this.EvaluateFiniteNode(graph.RootPartition.Expression, type);
        root.SourceGraph = graph;
        if (!root.Complete)
        {
            return root;
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            ConstraintFiniteValueSet extension = this.EvaluateFiniteNode(
                graph.ExtensionPartition.Expression,
                type);
            if (!extension.Complete)
            {
                return new ConstraintFiniteValueSet { SourceGraph = graph };
            }

            root = this.UnionFiniteSets(root, extension);
            root.SourceGraph = graph;
        }

        return root;
    }

    internal ConstraintFiniteValueSet EvaluateFiniteNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            ResolvedValueSyntax? value = this.ResolveFiniteNodeValue(node, type);
            return value == null
                ? new ConstraintFiniteValueSet()
                : new ConstraintFiniteValueSet
                {
                    Values = [value],
                    Complete = true
                };
        }

        if (node.Kind == AsnConstraintKind.Union
            || node.Kind == AsnConstraintKind.Intersection)
        {
            ConstraintFiniteValueSet result = new ConstraintFiniteValueSet
            {
                Complete = true
            };
            int index = 0;
            while (index < node.Children.Length)
            {
                ConstraintFiniteValueSet operand = this.EvaluateFiniteNode(
                    node.Children[index],
                    type);
                if (!operand.Complete)
                {
                    return new ConstraintFiniteValueSet();
                }

                if (index == 0)
                {
                    result = operand;
                }
                else
                {
                    result = node.Kind == AsnConstraintKind.Union
                        ? this.UnionFiniteSets(result, operand)
                        : this.IntersectFiniteSets(result, operand);
                }

                index = index + 1;
            }

            return result;
        }

        if (node.Kind == AsnConstraintKind.Except && node.Children.Length == 2)
        {
            ConstraintFiniteValueSet source = this.EvaluateFiniteNode(
                node.Children[0],
                type);
            ConstraintFiniteValueSet excluded = this.EvaluateFiniteNode(
                node.Children[1],
                type);
            return source.Complete && excluded.Complete
                ? this.ExceptFiniteSets(source, excluded)
                : new ConstraintFiniteValueSet();
        }

        return new ConstraintFiniteValueSet();
    }

    internal ResolvedValueSyntax? ResolveFiniteNodeValue(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        ParsedValueSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.ValueSyntax;
        ResolvedTypeSyntax governingType = node.GoverningType == null
            ? type
            : node.GoverningType;
        SymbolTableEntry? declaration = governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (syntax == null || declaration == null || module == null)
        {
            return null;
        }

        ResolvedValueSyntax resolved = this.ValueResolver.ResolveNestedValueSyntax(
            syntax,
            governingType,
            module,
            declaration);
        return resolved.Success ? resolved : null;
    }

    internal ConstraintFiniteValueSet UnionFiniteSets(
        ConstraintFiniteValueSet left,
        ConstraintFiniteValueSet right)
    {
        System.Collections.Generic.List<ResolvedValueSyntax> values =
            new System.Collections.Generic.List<ResolvedValueSyntax>();
        this.AddUniqueValues(values, left.Values);
        this.AddUniqueValues(values, right.Values);
        return new ConstraintFiniteValueSet
        {
            Values = values.ToArray(),
            Complete = left.Complete && right.Complete
        };
    }

    internal ConstraintFiniteValueSet IntersectFiniteSets(
        ConstraintFiniteValueSet left,
        ConstraintFiniteValueSet right)
    {
        System.Collections.Generic.List<ResolvedValueSyntax> values =
            new System.Collections.Generic.List<ResolvedValueSyntax>();
        int leftIndex = 0;
        while (leftIndex < left.Values.Length)
        {
            if (this.ContainsResolvedValue(right.Values, left.Values[leftIndex]))
            {
                values.Add(left.Values[leftIndex]);
            }

            leftIndex = leftIndex + 1;
        }

        return new ConstraintFiniteValueSet
        {
            Values = values.ToArray(),
            SourceGraph = right.SourceGraph,
            Complete = left.Complete && right.Complete
        };
    }

    internal ConstraintFiniteValueSet ExceptFiniteSets(
        ConstraintFiniteValueSet source,
        ConstraintFiniteValueSet excluded)
    {
        System.Collections.Generic.List<ResolvedValueSyntax> values =
            new System.Collections.Generic.List<ResolvedValueSyntax>();
        int index = 0;
        while (index < source.Values.Length)
        {
            if (!this.ContainsResolvedValue(excluded.Values, source.Values[index]))
            {
                values.Add(source.Values[index]);
            }

            index = index + 1;
        }

        return new ConstraintFiniteValueSet
        {
            Values = values.ToArray(),
            Complete = source.Complete && excluded.Complete
        };
    }

    internal void AddUniqueValues(
        System.Collections.Generic.List<ResolvedValueSyntax> target,
        ResolvedValueSyntax[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (!this.ContainsResolvedValue(target.ToArray(), values[index]))
            {
                target.Add(values[index]);
            }

            index = index + 1;
        }
    }

    internal bool ContainsResolvedValue(
        ResolvedValueSyntax[] values,
        ResolvedValueSyntax expected)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (this.ValueResolver.AreResolvedValuesEqual(values[index], expected))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void ValidateComponentContradictions(ResolvedTypeSyntax type)
    {
        if (type.EffectiveConstraint == null
            || type.EffectiveConstraint.RootPartition == null
            || type.EffectiveConstraint.RootPartition.Expression == null)
        {
            return;
        }

        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets =
            new System.Collections.Generic.List<NormalizedComponentConstraintSet>();
        this.CollectRequiredComponentSets(
            type.EffectiveConstraint.RootPartition.Expression,
            sets);
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<ComponentPresenceConstraintKind> presences =
            new System.Collections.Generic.List<ComponentPresenceConstraintKind>();
        System.Collections.Generic.List<ParsedNamedConstraintSyntax?> sources =
            new System.Collections.Generic.List<ParsedNamedConstraintSyntax?>();
        string requiredChoice = "";
        ParsedNamedConstraintSyntax? requiredChoiceSource = null;
        int setIndex = 0;
        while (setIndex < sets.Count)
        {
            NormalizedComponentConstraintSet set = sets[setIndex];
            int componentIndex = 0;
            while (componentIndex < set.Components.Length)
            {
                NormalizedComponentConstraint component = set.Components[componentIndex];
                if (component.Presence == ComponentPresenceConstraintKind.Present
                    || component.Presence == ComponentPresenceConstraintKind.Absent)
                {
                    int existingIndex = names.IndexOf(component.Name);
                    if (existingIndex >= 0
                        && presences[existingIndex] != component.Presence)
                    {
                        this.MarkComponentContradiction(
                            type,
                            component,
                            sources[existingIndex],
                            "Component '" + component.Name
                                + "' is required to be both PRESENT and ABSENT.");
                        return;
                    }

                    if (existingIndex < 0)
                    {
                        names.Add(component.Name);
                        presences.Add(component.Presence);
                        sources.Add(component.SourceSyntax);
                    }
                }

                if (type.EffectiveKind == AsnTypeKind.Choice
                    && component.Presence == ComponentPresenceConstraintKind.Present)
                {
                    if (requiredChoice.Length > 0 && requiredChoice != component.Name)
                    {
                        this.MarkComponentContradiction(
                            type,
                            component,
                            requiredChoiceSource,
                            "CHOICE alternatives '" + requiredChoice + "' and '"
                                + component.Name + "' are both required to be PRESENT.");
                        return;
                    }

                    requiredChoice = component.Name;
                    requiredChoiceSource = component.SourceSyntax;
                }

                componentIndex = componentIndex + 1;
            }

            setIndex = setIndex + 1;
        }
    }

    internal void CollectRequiredComponentSets(
        NormalizedConstraintNode node,
        System.Collections.Generic.List<NormalizedComponentConstraintSet> sets)
    {
        if (node.CompiledComponentConstraints != null)
        {
            sets.Add(node.CompiledComponentConstraints);
            return;
        }

        if (node.Kind != AsnConstraintKind.Intersection)
        {
            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            this.CollectRequiredComponentSets(node.Children[index], sets);
            index = index + 1;
        }
    }

    internal void MarkComponentContradiction(
        ResolvedTypeSyntax type,
        NormalizedComponentConstraint component,
        ParsedNamedConstraintSyntax? earlier,
        string message)
    {
        AsnDiagnosticRelatedLocation[]? related = earlier == null
            || earlier.Span == null
            ? null
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The conflicting presence requirement is here.",
                Span = earlier.Span
            }];
        this.MarkEmptyConstraintAtSpan(
            type,
            component.SourceSyntax == null ? null : component.SourceSyntax.Span,
            message,
            related);
    }

    internal void ValidateTimeContradictions(ResolvedTypeSyntax type)
    {
        if (!this.IsTimeSettingsType(type.EffectiveKind)
            || type.EffectiveConstraint == null
            || type.EffectiveConstraint.RootPartition == null
            || type.EffectiveConstraint.RootPartition.Expression == null)
        {
            return;
        }

        System.Collections.Generic.List<TimePropertySettings> settings =
            new System.Collections.Generic.List<TimePropertySettings>();
        this.CollectRequiredTimeSettings(
            type.EffectiveConstraint.RootPartition.Expression,
            settings);
        int rightIndex = 1;
        while (rightIndex < settings.Count)
        {
            int leftIndex = 0;
            while (leftIndex < rightIndex)
            {
                if (this.TimeSettingsConflict(settings[leftIndex], settings[rightIndex]))
                {
                    NormalizedConstraintNode? earlierNode = settings[leftIndex].SourceNode;
                    NormalizedConstraintNode? currentNode = settings[rightIndex].SourceNode;
                    AsnSourceSpan? earlierSpan = earlierNode == null
                        ? null
                        : this.GetNodeSpan(earlierNode);
                    AsnDiagnosticRelatedLocation[]? related = earlierSpan == null
                        ? null
                        : [new AsnDiagnosticRelatedLocation
                        {
                            Message = "The conflicting time property selection is here.",
                            Span = earlierSpan
                        }];
                    this.MarkEmptyConstraintAtSpan(
                        type,
                        currentNode == null
                            ? null
                            : this.GetNodeSpan(currentNode),
                        "Sequential SETTINGS constraints select incompatible time properties.",
                        related);
                    return;
                }

                leftIndex = leftIndex + 1;
            }

            rightIndex = rightIndex + 1;
        }
    }

    internal void CollectRequiredTimeSettings(
        NormalizedConstraintNode node,
        System.Collections.Generic.List<TimePropertySettings> settings)
    {
        if (node.CompiledTimeSettings != null)
        {
            settings.Add(node.CompiledTimeSettings);
            return;
        }

        if (node.Kind != AsnConstraintKind.Intersection)
        {
            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            this.CollectRequiredTimeSettings(node.Children[index], settings);
            index = index + 1;
        }
    }

    internal bool TimeSettingsConflict(
        TimePropertySettings left,
        TimePropertySettings right)
    {
        return this.SettingsDiffer((int)left.Basic, (int)right.Basic)
            || this.SettingsDiffer((int)left.Date, (int)right.Date)
            || this.SettingsDiffer((int)left.Year, (int)right.Year)
            || this.SettingsDiffer((int)left.Time, (int)right.Time)
            || this.SettingsDiffer((int)left.Zone, (int)right.Zone)
            || this.SettingsDiffer((int)left.Midnight, (int)right.Midnight)
            || this.SettingsDiffer((int)left.Interval, (int)right.Interval)
            || this.SettingsDiffer((int)left.Endpoint, (int)right.Endpoint)
            || this.SettingsDiffer((int)left.Recurrence, (int)right.Recurrence)
            || (left.Year == TimeYearSettingKind.Large
                && right.Year == TimeYearSettingKind.Large
                && left.LargeYearDigits != right.LargeYearDigits)
            || (left.Time != TimePrecisionSettingKind.Unspecified
                && right.Time != TimePrecisionSettingKind.Unspecified
                && left.FractionalDigits != right.FractionalDigits)
            || (left.Recurrence == TimeRecurrenceSettingKind.DigitCount
                && right.Recurrence == TimeRecurrenceSettingKind.DigitCount
                && left.RecurrenceDigits != right.RecurrenceDigits);
    }

    internal bool SettingsDiffer(int left, int right)
    {
        return left != 0 && right != 0 && left != right;
    }

    internal void DetectTypeCycles(
        ResolvedTypeSyntax type,
        NormalizedConstraintNode? enteringEdge)
    {
        int stateIndex = this.FindCycleTypeIndex(type);
        if (stateIndex >= 0 && this.CycleStates[stateIndex] == 2)
        {
            return;
        }

        if (stateIndex >= 0 && this.CycleStates[stateIndex] == 1)
        {
            if (enteringEdge != null)
            {
                this.AddConstraintCycleDiagnostic(type, enteringEdge);
            }

            return;
        }

        if (stateIndex < 0)
        {
            this.CycleTypes.Add(type);
            this.CycleStates.Add(1);
            stateIndex = this.CycleTypes.Count - 1;
        }
        else
        {
            this.CycleStates[stateIndex] = 1;
        }

        this.CycleStackTypes.Add(type);
        this.CycleStackEdges.Add(enteringEdge);
        int graphIndex = 0;
        while (graphIndex < type.NormalizedConstraints.Length)
        {
            this.DetectGraphCycles(type.NormalizedConstraints[graphIndex], type);
            graphIndex = graphIndex + 1;
        }

        this.CycleStackTypes.RemoveAt(this.CycleStackTypes.Count - 1);
        this.CycleStackEdges.RemoveAt(this.CycleStackEdges.Count - 1);
        this.CycleStates[stateIndex] = 2;
    }

    internal void DetectGraphCycles(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax ownerType)
    {
        if (graph.RootPartition != null && graph.RootPartition.Expression != null)
        {
            this.DetectNodeCycles(graph.RootPartition.Expression, ownerType);
        }

        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            this.DetectNodeCycles(graph.ExtensionPartition.Expression, ownerType);
        }
    }

    internal void DetectNodeCycles(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax ownerType)
    {
        if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            ResolvedTypeSyntax? governing = node.GoverningType == null
                ? ownerType
                : node.GoverningType;
            ResolvedTypeSyntax? target = this.ResolveContainedType(
                node,
                this.CreateContext(governing, governing.EffectiveKind, 0));
            if (target != null)
            {
                this.DetectTypeCycles(target, node);
            }
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            this.DetectNodeCycles(node.Children[childIndex], ownerType);
            childIndex = childIndex + 1;
        }

        int nestedIndex = 0;
        while (nestedIndex < node.NestedConstraints.Length)
        {
            this.DetectGraphCycles(node.NestedConstraints[nestedIndex], ownerType);
            nestedIndex = nestedIndex + 1;
        }
    }

    internal int FindCycleTypeIndex(ResolvedTypeSyntax type)
    {
        int index = 0;
        while (index < this.CycleTypes.Count)
        {
            if (this.CycleTypes[index] == type)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal void AddConstraintCycleDiagnostic(
        ResolvedTypeSyntax repeatedType,
        NormalizedConstraintNode closingEdge)
    {
        int cycleStart = this.CycleStackTypes.IndexOf(repeatedType);
        System.Collections.Generic.List<AsnDiagnosticRelatedLocation> related =
            new System.Collections.Generic.List<AsnDiagnosticRelatedLocation>();
        int index = cycleStart < 0 ? 0 : cycleStart + 1;
        while (index < this.CycleStackEdges.Count)
        {
            NormalizedConstraintNode? edge = this.CycleStackEdges[index];
            AsnSourceSpan? span = edge == null ? null : this.GetNodeSpan(edge);
            if (span != null)
            {
                related.Add(new AsnDiagnosticRelatedLocation
                {
                    Message = "Another contained-subtype reference in the cycle is here.",
                    Span = span
                });
            }

            index = index + 1;
        }

        index = cycleStart < 0 ? 0 : cycleStart;
        while (index < this.CycleStackTypes.Count)
        {
            this.CycleStackTypes[index].ConstraintValidationSuccess = false;
            index = index + 1;
        }

        this.AddDiagnostic(
            ConstraintDiagnosticCodes.ConstraintCycle,
            "A contained-subtype constraint participates in a constraint cycle.",
            repeatedType,
            this.GetNodeSpan(closingEdge),
            related.ToArray());
    }

    internal void MarkEmptyConstraint(
        ResolvedTypeSyntax type,
        string message)
    {
        NormalizedConstraintGraph? graph = this.FindLastApplication(type);
        this.MarkEmptyConstraintAtGraph(
            type,
            graph,
            message,
            this.CreateEffectiveConstraintLocations(type, graph));
    }

    internal void MarkEmptyConstraintAtGraph(
        ResolvedTypeSyntax type,
        NormalizedConstraintGraph? graph,
        string message,
        AsnDiagnosticRelatedLocation[]? related)
    {
        AsnSourceSpan? span = graph == null || graph.SourceSyntax == null
            ? type.SourceSyntax == null ? null : type.SourceSyntax.Span
            : graph.SourceSyntax.Span;
        this.MarkEmptyConstraintAtSpan(type, span, message, related);
    }

    internal void MarkEmptyConstraintAtSpan(
        ResolvedTypeSyntax type,
        AsnSourceSpan? span,
        string message,
        AsnDiagnosticRelatedLocation[]? related)
    {
        if (type.EffectiveConstraintEmpty)
        {
            return;
        }

        type.EffectiveConstraintEmpty = true;
        this.AddDiagnostic(
            ConstraintDiagnosticCodes.EmptyConstraint,
            message,
            type,
            span,
            related);
    }

    internal NormalizedConstraintGraph? FindLastApplication(ResolvedTypeSyntax type)
    {
        if (type.EffectiveConstraint == null)
        {
            return null;
        }

        NormalizedConstraintGraph[] applications = type.EffectiveConstraint.Applications;
        return applications.Length == 0
            ? type.EffectiveConstraint
            : applications[applications.Length - 1];
    }

    internal AsnDiagnosticRelatedLocation[] CreateEffectiveConstraintLocations(
        ResolvedTypeSyntax type,
        NormalizedConstraintGraph? excluded)
    {
        if (type.EffectiveConstraint == null)
        {
            return [];
        }

        NormalizedConstraintGraph[] applications =
            type.EffectiveConstraint.Applications.Length == 0
                ? [type.EffectiveConstraint]
                : type.EffectiveConstraint.Applications;
        System.Collections.Generic.List<AsnDiagnosticRelatedLocation> locations =
            new System.Collections.Generic.List<AsnDiagnosticRelatedLocation>();
        int index = 0;
        while (index < applications.Length)
        {
            ParsedConstraintSyntax? source = applications[index].SourceSyntax;
            if (applications[index] != excluded
                && source != null
                && source.Span != null)
            {
                locations.Add(new AsnDiagnosticRelatedLocation
                {
                    Message = "Another constraint contributing to the empty intersection is here.",
                    Span = source.Span
                });
            }

            index = index + 1;
        }

        return locations.ToArray();
    }

    internal AsnDiagnosticRelatedLocation[] CreateGraphRelatedLocations(
        NormalizedConstraintGraph[] graphs,
        int end)
    {
        System.Collections.Generic.List<AsnDiagnosticRelatedLocation> locations =
            new System.Collections.Generic.List<AsnDiagnosticRelatedLocation>();
        int index = 0;
        while (index < end)
        {
            ParsedConstraintSyntax? source = graphs[index].SourceSyntax;
            if (source != null && source.Span != null)
            {
                locations.Add(new AsnDiagnosticRelatedLocation
                {
                    Message = "An earlier finite-value constraint is here.",
                    Span = source.Span
                });
            }

            index = index + 1;
        }

        return locations.ToArray();
    }

    internal AsnDiagnosticRelatedLocation[]? CreateTypeRelatedLocation(
        ResolvedTypeSyntax type,
        string message)
    {
        AsnSourceSpan? span = type.SourceDeclaration == null
            ? type.SourceSyntax == null ? null : type.SourceSyntax.Span
            : type.SourceDeclaration.Span;
        return span == null
            ? null
            : [new AsnDiagnosticRelatedLocation
            {
                Message = message,
                Span = span
            }];
    }

    internal AsnSourceSpan? GetNodeSpan(NormalizedConstraintNode node)
    {
        if (node.SourceSyntax != null && node.SourceSyntax.Span != null)
        {
            return node.SourceSyntax.Span;
        }

        if (node.SourceNamedConstraint != null
            && node.SourceNamedConstraint.Span != null)
        {
            return node.SourceNamedConstraint.Span;
        }

        return node.SourceGeneralConstraint == null
            ? null
            : node.SourceGeneralConstraint.Span;
    }

    internal void AddDiagnostic(
        int code,
        string message,
        ResolvedTypeSyntax type,
        AsnSourceSpan? span,
        AsnDiagnosticRelatedLocation[]? related)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Constraint,
            code,
            message,
            span,
            related);
        type.ConstraintValidationSuccess = false;
        this.Success = false;
    }
}
