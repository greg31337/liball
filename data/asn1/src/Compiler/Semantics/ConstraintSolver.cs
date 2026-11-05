namespace asn1;

internal class ConstraintSolver
{
    internal ValueResolver ValueResolver;
    internal IntegerRangeAlgebra Algebra = new IntegerRangeAlgebra();
    internal CharacterRepertoireProvider Repertoires = new CharacterRepertoireProvider();
    internal SymbolTable[] Tables = [];
    internal System.Collections.Generic.List<ResolvedTypeSyntax> Solving =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> Solved =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal bool Success = true;

    internal ConstraintSolver(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
    }

    internal bool SolveTypes(SymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Tables = tables;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int typeIndex = 0;
            while (typeIndex < tables[tableIndex].TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.SolveType(type);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal IntegerConstraintResult? SolveType(ResolvedTypeSyntax type)
    {
        if (this.Solved.Contains(type))
        {
            return type.IntegerConstraints;
        }

        if (this.Solving.Contains(type))
        {
            return null;
        }

        this.Solving.Add(type);
        if (type.EffectiveKind == AsnTypeKind.Integer
            || type.EffectiveKind == AsnTypeKind.Enumerated)
        {
            IntegerConstraintSet? domain = this.CreateTypeDomain(type);
            if (domain != null)
            {
                type.IntegerConstraints = this.EvaluateTypeConstraint(type, domain);
            }
        }

        if (this.IsSizeConstrainedType(type.EffectiveKind))
        {
            type.SizeConstraints = this.EvaluateSizeConstraint(type);
        }

        if (this.IsAlphabetType(type.EffectiveKind))
        {
            type.PermittedAlphabet = this.EvaluateAlphabetConstraint(type);
        }

        this.Solving.Remove(type);
        this.Solved.Add(type);
        if (type.AliasTarget != null)
        {
            this.SolveType(type.AliasTarget);
        }

        if (type.InnerType != null)
        {
            this.SolveType(type.InnerType);
        }

        if (type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.SolveType(type.SelectedComponent.TypeResolution);
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[componentIndex].TypeResolution;
            if (componentType != null)
            {
                this.SolveType(componentType);
            }

            componentIndex = componentIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.SolveType(type.ConstraintTypes[constraintIndex]);
            constraintIndex = constraintIndex + 1;
        }

        return type.IntegerConstraints;
    }

    internal bool IsSizeConstrainedType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BitString
            || kind == AsnTypeKind.OctetString
            || kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString
            || kind == AsnTypeKind.SequenceOf
            || kind == AsnTypeKind.SetOf;
    }

    internal bool IsAlphabetType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
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

    internal PermittedAlphabetResult? EvaluateAlphabetConstraint(ResolvedTypeSyntax type)
    {
        NormalizedConstraintGraph? graph = type.EffectiveConstraint;
        if (graph == null || graph.RootPartition == null
            || graph.RootPartition.Expression == null)
        {
            return null;
        }

        IntegerConstraintSet domain = this.Repertoires.CreateRepertoire(type.EffectiveKind);
        IntegerConstraintSet? rootKnown = this.EvaluateAlphabetNode(
            graph.RootPartition.Expression,
            type,
            domain);
        if (rootKnown == null)
        {
            return null;
        }

        IntegerConstraintSet root = graph.RootPartition.Expression.AlphabetRootSet == null
            ? rootKnown
            : graph.RootPartition.Expression.AlphabetRootSet;
        IntegerConstraintSet extension =
            graph.RootPartition.Expression.AlphabetExtensionSet == null
                ? this.Algebra.CreateEmpty()
                : graph.RootPartition.Expression.AlphabetExtensionSet;
        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            IntegerConstraintSet? evaluatedExtension = this.EvaluateAlphabetNode(
                graph.ExtensionPartition.Expression,
                type,
                domain);
            if (evaluatedExtension != null)
            {
                extension = this.Algebra.Union([extension, evaluatedExtension]);
            }
        }

        bool open = graph.ExtensionPartition != null
            && graph.ExtensionPartition.Open;
        open = open || graph.RootPartition.Expression.OpenAlphabetSet;
        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            open = open || graph.ExtensionPartition.Expression.OpenAlphabetSet;
        }

        PermittedAlphabetResult result = new PermittedAlphabetResult
        {
            Domain = domain,
            Root = root,
            Extension = extension,
            KnownScalars = this.Algebra.Union([root, extension]),
            OpenExtension = open
        };
        this.BuildAlphabetIndexes(result);
        return result;
    }

    internal IntegerConstraintSet? EvaluateAlphabetNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        IntegerConstraintSet? result = null;
        if (node.Kind == AsnConstraintKind.PermittedAlphabet
            && node.NestedConstraints.Length == 1)
        {
            NormalizedConstraintGraph nested = node.NestedConstraints[0];
            IntegerConstraintSet? root = nested.RootPartition == null
                || nested.RootPartition.Expression == null
                ? domain
                : this.EvaluateAlphabetScalarNode(
                    nested.RootPartition.Expression,
                    type,
                    domain);
            IntegerConstraintSet extension = this.Algebra.CreateEmpty();
            if (nested.ExtensionPartition != null
                && nested.ExtensionPartition.Expression != null)
            {
                IntegerConstraintSet? evaluatedExtension = this.EvaluateAlphabetScalarNode(
                    nested.ExtensionPartition.Expression,
                    type,
                    domain);
                if (evaluatedExtension != null)
                {
                    extension = evaluatedExtension;
                }
            }

            if (root != null)
            {
                result = this.Algebra.Union([root, extension]);
                node.AlphabetRootSet = root;
                node.AlphabetExtensionSet = extension;
            }

            node.OpenAlphabetSet = nested.ExtensionPartition != null
                && nested.ExtensionPartition.Open;
        }
        else if (node.Kind == AsnConstraintKind.Union
            || node.Kind == AsnConstraintKind.Intersection)
        {
            System.Collections.Generic.List<IntegerConstraintSet> operands =
                new System.Collections.Generic.List<IntegerConstraintSet>();
            System.Collections.Generic.List<IntegerConstraintSet> rootOperands =
                new System.Collections.Generic.List<IntegerConstraintSet>();
            bool open = node.Kind == AsnConstraintKind.Intersection;
            bool unknownUnionOperand = false;
            int index = 0;
            while (index < node.Children.Length)
            {
                IntegerConstraintSet? operand = this.EvaluateAlphabetNode(
                    node.Children[index],
                    type,
                    domain);
                if (operand == null)
                {
                    if (node.Kind == AsnConstraintKind.Union)
                    {
                        unknownUnionOperand = true;
                    }
                }
                else
                {
                    operands.Add(operand);
                    IntegerConstraintSet? childRoot = node.Children[index].AlphabetRootSet;
                    rootOperands.Add(childRoot == null ? operand : childRoot);
                    if (node.Kind == AsnConstraintKind.Intersection)
                    {
                        open = open && node.Children[index].OpenAlphabetSet;
                    }
                    else
                    {
                        open = open || node.Children[index].OpenAlphabetSet;
                    }
                }

                index = index + 1;
            }

            if (!unknownUnionOperand && operands.Count > 0)
            {
                result = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(operands.ToArray())
                    : this.Algebra.Intersect(operands.ToArray());
                IntegerConstraintSet computedRoot = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(rootOperands.ToArray())
                    : this.Algebra.Intersect(rootOperands.ToArray());
                node.AlphabetRootSet = computedRoot;
                node.AlphabetExtensionSet = this.Algebra.Except(result, computedRoot);
                node.OpenAlphabetSet = open;
            }
        }
        else if (node.Kind == AsnConstraintKind.Except && node.Children.Length == 2)
        {
            IntegerConstraintSet? source = this.EvaluateAlphabetNode(
                node.Children[0],
                type,
                domain);
            IntegerConstraintSet? excluded = this.EvaluateAlphabetNode(
                node.Children[1],
                type,
                domain);
            if (source != null && excluded != null)
            {
                result = this.Algebra.Except(source, excluded);
                IntegerConstraintSet? childRoot = node.Children[0].AlphabetRootSet;
                IntegerConstraintSet computedRoot = this.Algebra.Except(
                    childRoot == null ? source : childRoot,
                    excluded);
                node.AlphabetRootSet = computedRoot;
                node.AlphabetExtensionSet = this.Algebra.Except(result, computedRoot);
                node.OpenAlphabetSet = node.Children[0].OpenAlphabetSet;
            }
        }
        else if (node.Kind == AsnConstraintKind.AllExcept
            && node.Children.Length == 1)
        {
            IntegerConstraintSet? excluded = this.EvaluateAlphabetNode(
                node.Children[0],
                type,
                domain);
            if (excluded != null)
            {
                result = this.Algebra.Except(domain, excluded);
            }
        }

        if (result != null && node.AlphabetRootSet == null)
        {
            node.AlphabetRootSet = result;
            node.AlphabetExtensionSet = this.Algebra.CreateEmpty();
        }

        node.AlphabetSet = result;
        return result;
    }

    internal IntegerConstraintSet? EvaluateAlphabetScalarNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        IntegerConstraintSet? result = null;
        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            CharacterScalarResult scalars = this.ResolveCharacterScalars(
                node.SourceSyntax == null ? null : node.SourceSyntax.ValueSyntax,
                node.Value,
                node.GoverningType == null ? type : node.GoverningType);
            if (scalars.Success)
            {
                System.Numerics.BigInteger[] values =
                    new System.Numerics.BigInteger[scalars.Scalars.Length];
                int index = 0;
                while (index < values.Length)
                {
                    values[index] = new System.Numerics.BigInteger(scalars.Scalars[index]);
                    index = index + 1;
                }

                result = this.Algebra.Intersect([
                    domain,
                    this.Algebra.CreateFiniteDomain(values)]);
            }
        }
        else if (node.Kind == AsnConstraintKind.ValueRange)
        {
            CharacterScalarResult lower = this.ResolveCharacterScalars(
                node.SourceSyntax == null ? null : node.SourceSyntax.LowerValueSyntax,
                node.LowerValue,
                node.GoverningType == null ? type : node.GoverningType);
            CharacterScalarResult upper = this.ResolveCharacterScalars(
                node.SourceSyntax == null ? null : node.SourceSyntax.UpperValueSyntax,
                node.UpperValue,
                node.GoverningType == null ? type : node.GoverningType);
            if (lower.Success && upper.Success
                && lower.Scalars.Length == 1 && upper.Scalars.Length == 1)
            {
                System.Numerics.BigInteger lowerScalar =
                    new System.Numerics.BigInteger(lower.Scalars[0]);
                System.Numerics.BigInteger upperScalar =
                    new System.Numerics.BigInteger(upper.Scalars[0]);
                if (!node.LowerEndpointIncluded)
                {
                    lowerScalar = lowerScalar + System.Numerics.BigInteger.One;
                }

                if (!node.UpperEndpointIncluded)
                {
                    upperScalar = upperScalar - System.Numerics.BigInteger.One;
                }

                result = this.Algebra.Intersect([
                    domain,
                    this.Algebra.CreateInterval(
                        true,
                        lowerScalar,
                        true,
                        upperScalar)]);
            }
        }
        else if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            result = this.EvaluateAlphabetSubtype(node, type, domain);
        }
        else if (node.Kind == AsnConstraintKind.Union
            || node.Kind == AsnConstraintKind.Intersection)
        {
            IntegerConstraintSet[] operands =
                new IntegerConstraintSet[node.Children.Length];
            bool success = true;
            int index = 0;
            while (index < node.Children.Length)
            {
                IntegerConstraintSet? operand = this.EvaluateAlphabetScalarNode(
                    node.Children[index],
                    type,
                    domain);
                if (operand == null)
                {
                    success = false;
                }
                else
                {
                    operands[index] = operand;
                }

                index = index + 1;
            }

            if (success)
            {
                result = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(operands)
                    : this.Algebra.Intersect(operands);
            }
        }
        else if (node.Kind == AsnConstraintKind.Except && node.Children.Length == 2)
        {
            IntegerConstraintSet? source = this.EvaluateAlphabetScalarNode(
                node.Children[0],
                type,
                domain);
            IntegerConstraintSet? excluded = this.EvaluateAlphabetScalarNode(
                node.Children[1],
                type,
                domain);
            if (source != null && excluded != null)
            {
                result = this.Algebra.Except(source, excluded);
            }
        }
        else if (node.Kind == AsnConstraintKind.AllExcept
            && node.Children.Length == 1)
        {
            IntegerConstraintSet? excluded = this.EvaluateAlphabetScalarNode(
                node.Children[0],
                type,
                domain);
            if (excluded != null)
            {
                result = this.Algebra.Except(domain, excluded);
            }
        }

        node.AlphabetSet = result;
        return result;
    }

    internal CharacterScalarResult ResolveCharacterScalars(
        ParsedValueSyntax? syntax,
        AsnValue? source,
        ResolvedTypeSyntax governingType)
    {
        if (source != null && source.Kind != AsnValueKind.Defined)
        {
            return this.ReadCharacterScalars(source.TextValue);
        }

        if (syntax == null)
        {
            return new CharacterScalarResult();
        }

        SymbolTableEntry? declaration = governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null || module == null)
        {
            return new CharacterScalarResult();
        }

        ResolvedValueSyntax resolved = this.ValueResolver.ResolveNestedValueSyntax(
            syntax,
            governingType,
            module,
            declaration);
        AsnValue? value = resolved.EffectiveValue;
        return resolved.Success && value != null
            ? this.ReadCharacterScalars(value.TextValue)
            : new CharacterScalarResult();
    }

    internal CharacterScalarResult ReadCharacterScalars(string text)
    {
        SemanticValueValidator validator = new SemanticValueValidator(
            this.ValueResolver.Diagnostics);
        System.Collections.Generic.List<int> scalars =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = validator.ReadUnicodeScalar(text, index);
            if (!scalar.Success)
            {
                return new CharacterScalarResult();
            }

            scalars.Add(scalar.Scalar);
            index = scalar.NextIndex;
        }

        return new CharacterScalarResult
        {
            Scalars = scalars.ToArray(),
            Success = true
        };
    }

    internal IntegerConstraintSet? EvaluateAlphabetSubtype(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        ParsedTypeSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.TypeSyntax;
        if (syntax == null)
        {
            return null;
        }

        if (syntax.Type.Kind != AsnTypeKind.Defined)
        {
            return this.Algebra.Intersect([
                domain,
                this.Repertoires.CreateRepertoire(syntax.Type.Kind)]);
        }

        ResolvedTypeSyntax governingType = node.GoverningType == null
            ? type
            : node.GoverningType;
        SymbolTableEntry? declaration = governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null || module == null)
        {
            return null;
        }

        TypeReferenceBinding binding = this.ValueResolver.TypeResolver.FindTypeBinding(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName);
        ResolvedTypeSyntax? contained = binding.Target == null
            ? null
            : binding.Target.ResolvedType;
        if (contained == null || !this.IsAlphabetType(contained.EffectiveKind))
        {
            return null;
        }

        this.SolveType(contained);
        IntegerConstraintSet containedSet = contained.PermittedAlphabet == null
            || contained.PermittedAlphabet.KnownScalars == null
            ? this.Repertoires.CreateRepertoire(contained.EffectiveKind)
            : contained.PermittedAlphabet.KnownScalars;
        return this.Algebra.Intersect([domain, containedSet]);
    }

    internal void BuildAlphabetIndexes(PermittedAlphabetResult result)
    {
        if (result.Root == null)
        {
            return;
        }

        AlphabetIndexRange[] ranges =
            new AlphabetIndexRange[result.Root.Intervals.Length];
        int firstIndex = 0;
        int index = 0;
        while (index < ranges.Length)
        {
            IntegerInterval interval = result.Root.Intervals[index];
            if (!interval.HasLowerBound || !interval.HasUpperBound)
            {
                return;
            }

            int lower = (int)interval.LowerBound;
            int upper = (int)interval.UpperBound;
            ranges[index] = new AlphabetIndexRange
            {
                LowerScalar = lower,
                UpperScalar = upper,
                FirstIndex = firstIndex
            };
            firstIndex = firstIndex + upper - lower + 1;
            index = index + 1;
        }

        result.RootIndexRanges = ranges;
        result.RootAlphabetSize = firstIndex;
    }

    internal SizeConstraintResult? EvaluateSizeConstraint(ResolvedTypeSyntax type)
    {
        NormalizedConstraintGraph? graph = type.EffectiveConstraint;
        if (graph == null || graph.RootPartition == null
            || graph.RootPartition.Expression == null)
        {
            return null;
        }

        IntegerConstraintSet domain = this.Algebra.CreateInterval(
            true,
            System.Numerics.BigInteger.Zero,
            false,
            System.Numerics.BigInteger.Zero);
        IntegerConstraintSet? rootKnown = this.EvaluateSizeNode(
            graph.RootPartition.Expression,
            type,
            domain);
        if (rootKnown == null)
        {
            return null;
        }

        IntegerConstraintSet root = graph.RootPartition.Expression.SizeRootSet == null
            ? rootKnown
            : graph.RootPartition.Expression.SizeRootSet;
        IntegerConstraintSet extension = graph.RootPartition.Expression.SizeExtensionSet == null
            ? this.Algebra.CreateEmpty()
            : graph.RootPartition.Expression.SizeExtensionSet;
        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            IntegerConstraintSet? evaluatedExtension = this.EvaluateSizeNode(
                graph.ExtensionPartition.Expression,
                type,
                domain);
            if (evaluatedExtension != null)
            {
                extension = this.Algebra.Union([extension, evaluatedExtension]);
            }
        }

        IntegerConstraintSet known = this.Algebra.Union([root, extension]);
        bool open = graph.ExtensionPartition != null
            && graph.ExtensionPartition.Open;
        open = open || graph.RootPartition.Expression.OpenSizeSet;
        if (graph.ExtensionPartition != null
            && graph.ExtensionPartition.Expression != null)
        {
            open = open || graph.ExtensionPartition.Expression.OpenSizeSet;
        }

        SizeConstraintResult result = new SizeConstraintResult
        {
            Domain = domain,
            Root = root,
            Extension = extension,
            KnownSizes = known,
            OpenExtension = open,
            HasRootCardinality = root.HasFiniteCardinality,
            RootCardinality = root.FiniteCardinality
        };
        if (root.Intervals.Length > 0)
        {
            IntegerInterval first = root.Intervals[0];
            IntegerInterval last = root.Intervals[root.Intervals.Length - 1];
            result.HasRootLowerBound = first.HasLowerBound;
            result.RootLowerBound = first.LowerBound;
            result.HasRootUpperBound = last.HasUpperBound;
            result.RootUpperBound = last.UpperBound;
        }

        if (root.IsEmpty)
        {
            this.AddSizeDiagnostic(
                ConstraintDiagnosticCodes.ImpossibleSize,
                "The effective size constraint contains no possible size.",
                type.SourceSyntax == null ? null : type.SourceSyntax.Span);
        }

        return result;
    }

    internal IntegerConstraintSet? EvaluateSizeNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        IntegerConstraintSet? result = null;
        if (node.Kind == AsnConstraintKind.Size
            && node.NestedConstraints.Length == 1)
        {
            NormalizedConstraintGraph nested = node.NestedConstraints[0];
            if (nested.RootPartition != null
                && nested.RootPartition.Expression != null
                && this.HasNegativeSizeValue(nested.RootPartition.Expression, type))
            {
                this.AddSizeDiagnostic(
                    ConstraintDiagnosticCodes.NegativeSize,
                    "A size constraint cannot contain a negative size.",
                    node.SourceSyntax == null ? null : node.SourceSyntax.Span);
            }

            IntegerConstraintSet? root = nested.RootPartition == null
                || nested.RootPartition.Expression == null
                ? domain
                : this.EvaluateNode(nested.RootPartition.Expression, type, domain);
            IntegerConstraintSet extension = this.Algebra.CreateEmpty();
            if (nested.ExtensionPartition != null
                && nested.ExtensionPartition.Expression != null)
            {
                if (this.HasNegativeSizeValue(
                    nested.ExtensionPartition.Expression,
                    type))
                {
                    this.AddSizeDiagnostic(
                        ConstraintDiagnosticCodes.NegativeSize,
                        "A size constraint cannot contain a negative size.",
                        node.SourceSyntax == null ? null : node.SourceSyntax.Span);
                }

                IntegerConstraintSet? evaluatedExtension = this.EvaluateNode(
                    nested.ExtensionPartition.Expression,
                    type,
                    domain);
                if (evaluatedExtension != null)
                {
                    extension = evaluatedExtension;
                }
            }

            if (root != null)
            {
                result = this.Algebra.Union([root, extension]);
                node.SizeRootSet = root;
                node.SizeExtensionSet = extension;
            }

            node.OpenSizeSet = nested.ExtensionPartition != null
                && nested.ExtensionPartition.Open;
        }
        else if (node.Kind == AsnConstraintKind.Union
            || node.Kind == AsnConstraintKind.Intersection)
        {
            System.Collections.Generic.List<IntegerConstraintSet> operands =
                new System.Collections.Generic.List<IntegerConstraintSet>();
            System.Collections.Generic.List<IntegerConstraintSet> rootOperands =
                new System.Collections.Generic.List<IntegerConstraintSet>();
            bool open = node.Kind == AsnConstraintKind.Intersection;
            bool unknownUnionOperand = false;
            int index = 0;
            while (index < node.Children.Length)
            {
                IntegerConstraintSet? operand = this.EvaluateSizeNode(
                    node.Children[index],
                    type,
                    domain);
                if (operand == null)
                {
                    if (node.Kind == AsnConstraintKind.Union)
                    {
                        unknownUnionOperand = true;
                    }
                }
                else
                {
                    operands.Add(operand);
                    IntegerConstraintSet? childRoot = node.Children[index].SizeRootSet;
                    rootOperands.Add(childRoot == null
                        ? operand
                        : childRoot);
                    if (node.Kind == AsnConstraintKind.Intersection)
                    {
                        open = open && node.Children[index].OpenSizeSet;
                    }
                    else
                    {
                        open = open || node.Children[index].OpenSizeSet;
                    }
                }

                index = index + 1;
            }

            if (!unknownUnionOperand && operands.Count > 0)
            {
                result = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(operands.ToArray())
                    : this.Algebra.Intersect(operands.ToArray());
                IntegerConstraintSet computedRoot = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(rootOperands.ToArray())
                    : this.Algebra.Intersect(rootOperands.ToArray());
                node.SizeRootSet = computedRoot;
                node.SizeExtensionSet = this.Algebra.Except(
                    result,
                    computedRoot);
                node.OpenSizeSet = open;
            }
        }
        else if (node.Kind == AsnConstraintKind.Except && node.Children.Length == 2)
        {
            IntegerConstraintSet? source = this.EvaluateSizeNode(
                node.Children[0],
                type,
                domain);
            IntegerConstraintSet? excluded = this.EvaluateSizeNode(
                node.Children[1],
                type,
                domain);
            if (source != null && excluded != null)
            {
                result = this.Algebra.Except(source, excluded);
                IntegerConstraintSet? childRoot = node.Children[0].SizeRootSet;
                IntegerConstraintSet sourceRoot = childRoot == null
                    ? source
                    : childRoot;
                IntegerConstraintSet computedRoot = this.Algebra.Except(
                    sourceRoot,
                    excluded);
                node.SizeRootSet = computedRoot;
                node.SizeExtensionSet = this.Algebra.Except(
                    result,
                    computedRoot);
                node.OpenSizeSet = node.Children[0].OpenSizeSet;
            }
        }
        else if (node.Kind == AsnConstraintKind.AllExcept
            && node.Children.Length == 1)
        {
            IntegerConstraintSet? excluded = this.EvaluateSizeNode(
                node.Children[0],
                type,
                domain);
            if (excluded != null)
            {
                result = this.Algebra.Except(domain, excluded);
                node.SizeRootSet = result;
                node.SizeExtensionSet = this.Algebra.CreateEmpty();
            }
        }

        if (result != null && node.SizeRootSet == null)
        {
            node.SizeRootSet = result;
            node.SizeExtensionSet = this.Algebra.CreateEmpty();
        }

        node.SizeSet = result;
        return result;
    }

    internal bool HasNegativeSizeValue(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type)
    {
        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            IntegerEvaluationResult value = this.ResolveNodeValue(
                node.SourceSyntax == null ? null : node.SourceSyntax.ValueSyntax,
                node.Value,
                node.GoverningType == null ? type : node.GoverningType);
            return value.Success && value.Value < System.Numerics.BigInteger.Zero;
        }

        if (node.Kind == AsnConstraintKind.ValueRange)
        {
            ResolvedTypeSyntax governingType = node.GoverningType == null
                ? type
                : node.GoverningType;
            if (!node.LowerEndpointUnbounded)
            {
                IntegerEvaluationResult lower = this.ResolveNodeValue(
                    node.SourceSyntax == null ? null : node.SourceSyntax.LowerValueSyntax,
                    node.LowerValue,
                    governingType);
                if (lower.Success && lower.Value < System.Numerics.BigInteger.Zero)
                {
                    return true;
                }
            }

            if (!node.UpperEndpointUnbounded)
            {
                IntegerEvaluationResult upper = this.ResolveNodeValue(
                    node.SourceSyntax == null ? null : node.SourceSyntax.UpperValueSyntax,
                    node.UpperValue,
                    governingType);
                if (upper.Success && upper.Value < System.Numerics.BigInteger.Zero)
                {
                    return true;
                }
            }
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            if (this.HasNegativeSizeValue(node.Children[index], type))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void AddSizeDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        this.ValueResolver.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Constraint,
            code,
            message,
            span,
            null);
        this.Success = false;
    }

    internal IntegerConstraintSet? CreateTypeDomain(ResolvedTypeSyntax type)
    {
        if (type.EffectiveKind == AsnTypeKind.Integer)
        {
            return this.Algebra.CreateUniverse();
        }

        ResolvedTypeSyntax canonical = type.ConstraintDomain == null
            || type.ConstraintDomain.CanonicalType == null
            ? type
            : type.ConstraintDomain.CanonicalType;
        ParsedTypeSyntax? syntax = canonical.SourceSyntax;
        if (syntax == null)
        {
            return null;
        }

        System.Collections.Generic.List<System.Numerics.BigInteger> values =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        this.AddEnumerationValues(syntax.RootNamedValues, values);
        this.AddEnumerationValues(syntax.ExtensionNamedValues, values);
        return this.Algebra.CreateFiniteDomain(values.ToArray());
    }

    internal void AddEnumerationValues(
        AsnNamedValue[] namedValues,
        System.Collections.Generic.List<System.Numerics.BigInteger> values)
    {
        int index = 0;
        while (index < namedValues.Length)
        {
            AsnValue? value = namedValues[index].Value;
            if (value != null && value.HasIntegerValue)
            {
                values.Add(value.IntegerValue);
            }

            index = index + 1;
        }
    }

    internal IntegerConstraintResult EvaluateTypeConstraint(
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        NormalizedConstraintGraph? graph = type.EffectiveConstraint;
        if (graph == null)
        {
            return new IntegerConstraintResult
            {
                Domain = domain,
                Root = domain,
                Extension = this.Algebra.CreateEmpty(),
                KnownValues = domain
            };
        }

        IntegerConstraintSet? root = graph.RootPartition == null
            || graph.RootPartition.Expression == null
            ? domain
            : this.EvaluateNode(graph.RootPartition.Expression, type, domain);
        if (graph.RootPartition != null)
        {
            graph.RootPartition.IntegerSet = root;
        }

        IntegerConstraintSet? extension = graph.ExtensionPartition == null
            || graph.ExtensionPartition.Expression == null
            ? this.Algebra.CreateEmpty()
            : this.EvaluateNode(graph.ExtensionPartition.Expression, type, domain);
        if (graph.ExtensionPartition != null)
        {
            graph.ExtensionPartition.IntegerSet = extension;
        }

        IntegerConstraintSet? known = null;
        if (root != null && extension != null)
        {
            known = this.Algebra.Union([root, extension]);
        }

        graph.KnownIntegerSet = known;
        return new IntegerConstraintResult
        {
            Domain = domain,
            Root = root,
            Extension = extension,
            KnownValues = known,
            OpenExtension = graph.ExtensionPartition != null
                && graph.ExtensionPartition.Open
        };
    }

    internal IntegerConstraintSet? EvaluateNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        IntegerConstraintSet? result = null;
        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            IntegerEvaluationResult value = this.ResolveNodeValue(
                node.SourceSyntax == null ? null : node.SourceSyntax.ValueSyntax,
                node.Value,
                node.GoverningType == null ? type : node.GoverningType);
            if (value.Success)
            {
                result = this.Algebra.Intersect(
                    [domain, this.Algebra.CreateSingleton(value.Value)]);
            }
        }
        else if (node.Kind == AsnConstraintKind.ValueRange)
        {
            result = this.EvaluateRange(node, type, domain);
        }
        else if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            result = this.EvaluateContainedSubtype(node, type, domain);
        }
        else if (node.Kind == AsnConstraintKind.Union
            || node.Kind == AsnConstraintKind.Intersection)
        {
            IntegerConstraintSet[] operands =
                new IntegerConstraintSet[node.Children.Length];
            bool success = true;
            int index = 0;
            while (index < node.Children.Length)
            {
                IntegerConstraintSet? operand = this.EvaluateNode(
                    node.Children[index],
                    type,
                    domain);
                if (operand == null)
                {
                    success = false;
                }
                else
                {
                    operands[index] = operand;
                }

                index = index + 1;
            }

            if (success)
            {
                result = node.Kind == AsnConstraintKind.Union
                    ? this.Algebra.Union(operands)
                    : this.Algebra.Intersect(operands);
            }
        }
        else if (node.Kind == AsnConstraintKind.Except && node.Children.Length == 2)
        {
            IntegerConstraintSet? source = this.EvaluateNode(node.Children[0], type, domain);
            IntegerConstraintSet? excluded = this.EvaluateNode(node.Children[1], type, domain);
            if (source != null && excluded != null)
            {
                result = this.Algebra.Except(source, excluded);
            }
        }
        else if (node.Kind == AsnConstraintKind.AllExcept && node.Children.Length == 1)
        {
            IntegerConstraintSet? excluded = this.EvaluateNode(node.Children[0], type, domain);
            if (excluded != null)
            {
                result = this.Algebra.Except(domain, excluded);
            }
        }

        node.IntegerSet = result;
        return result;
    }

    internal IntegerConstraintSet? EvaluateRange(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        bool hasLower = !node.LowerEndpointUnbounded;
        bool hasUpper = !node.UpperEndpointUnbounded;
        System.Numerics.BigInteger lower = System.Numerics.BigInteger.Zero;
        System.Numerics.BigInteger upper = System.Numerics.BigInteger.Zero;
        ResolvedTypeSyntax governingType = node.GoverningType == null
            ? type
            : node.GoverningType;
        if (hasLower)
        {
            IntegerEvaluationResult value = this.ResolveNodeValue(
                node.SourceSyntax == null ? null : node.SourceSyntax.LowerValueSyntax,
                node.LowerValue,
                governingType);
            if (!value.Success)
            {
                return null;
            }

            lower = node.LowerEndpointIncluded
                ? value.Value
                : value.Value + System.Numerics.BigInteger.One;
        }

        if (hasUpper)
        {
            IntegerEvaluationResult value = this.ResolveNodeValue(
                node.SourceSyntax == null ? null : node.SourceSyntax.UpperValueSyntax,
                node.UpperValue,
                governingType);
            if (!value.Success)
            {
                return null;
            }

            upper = node.UpperEndpointIncluded
                ? value.Value
                : value.Value - System.Numerics.BigInteger.One;
        }

        return this.Algebra.Intersect(
            [domain, this.Algebra.CreateInterval(hasLower, lower, hasUpper, upper)]);
    }

    internal IntegerConstraintSet? EvaluateContainedSubtype(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        ParsedTypeSyntax? syntax = node.SourceSyntax == null
            ? null
            : node.SourceSyntax.TypeSyntax;
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

        ResolvedTypeSyntax? contained = node.ResolvedContainedSubtype;
        if (contained == null && syntax.Type.Kind == AsnTypeKind.Defined)
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
        else if (contained == null)
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
        }

        if (contained == null)
        {
            return null;
        }

        node.ResolvedContainedSubtype = contained;

        IntegerConstraintResult? result = this.SolveType(contained);
        if (result == null || result.KnownValues == null)
        {
            return null;
        }

        return this.Algebra.Intersect([domain, result.KnownValues]);
    }

    internal IntegerEvaluationResult ResolveNodeValue(
        ParsedValueSyntax? syntax,
        AsnValue? source,
        ResolvedTypeSyntax governingType)
    {
        if (source != null && source.HasIntegerValue)
        {
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = source.IntegerValue
            };
        }

        if (syntax == null)
        {
            return new IntegerEvaluationResult();
        }

        SymbolTableEntry? declaration = governingType.SourceDeclaration;
        SymbolTable? module = declaration == null
            ? null
            : this.ValueResolver.FindDeclaringModule(declaration);
        if (declaration == null || module == null)
        {
            return new IntegerEvaluationResult();
        }

        ResolvedValueSyntax resolved = this.ValueResolver.ResolveNestedValueSyntax(
            syntax,
            governingType,
            module,
            declaration);
        AsnValue? value = resolved.EffectiveValue;
        return resolved.Success && value != null && value.HasIntegerValue
            ? new IntegerEvaluationResult
            {
                Success = true,
                Value = value.IntegerValue
            }
            : new IntegerEvaluationResult();
    }
}
