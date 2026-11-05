namespace asn1;

internal class EncodingConstraintCompiler
{
    internal ValueResolver ValueResolver;
    internal ConstraintSolver Solver;
    internal IntegerRangeAlgebra Algebra = new IntegerRangeAlgebra();
    internal CharacterRepertoireProvider Repertoires = new CharacterRepertoireProvider();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> Compiling =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<ResolvedTypeSyntax> Compiled =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();

    internal EncodingConstraintCompiler(ValueResolver valueResolver)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.Solver = new ConstraintSolver(this.ValueResolver);
    }

    internal void CompileTypes(SymbolTable[] tables)
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
                ResolvedTypeSyntax? type = tables[tableIndex].TypeSymbols[typeIndex]
                    .ResolvedType;
                if (type != null)
                {
                    this.CompileType(type);
                }

                typeIndex = typeIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }
    }

    internal void CompileType(ResolvedTypeSyntax type)
    {
        if (this.Compiled.Contains(type) || this.Compiling.Contains(type))
        {
            return;
        }

        this.Compiling.Add(type);
        this.CompileRelatedTypes(type);
        EncodingEffectiveConstraints constraints = new EncodingEffectiveConstraints
        {
            ContentsConstraints = type.ContentsConstraints,
            TypeConstraints = type.TypeConstraints
        };
        if (type.EffectiveKind == AsnTypeKind.Integer)
        {
            constraints.Per.IntegerConstraints = this.ComputePerIntegerConstraints(type);
            constraints.Oer.IntegerConstraints = this.ComputeOerIntegerConstraints(type);
        }

        if (this.IsPerSizeType(type.EffectiveKind))
        {
            constraints.Per.SizeConstraints = this.ComputePerSizeConstraints(type);
        }

        if (this.IsOerSizeType(type.EffectiveKind))
        {
            constraints.Oer.SizeConstraints = this.ComputeOerSizeConstraints(type);
        }

        if (this.IsKnownMultiplierString(type.EffectiveKind))
        {
            constraints.Per.PermittedAlphabet = this.ComputePerAlphabetConstraint(type);
        }

        constraints.Per.TimeSettings = this.CollectVisibleTimeSettings(type);
        constraints.Oer.TimeSettings = this.CollectVisibleTimeSettings(type);
        constraints.Per.ComponentConstraintSets = type.ComponentConstraintSets;
        constraints.Per.HasEncodingEffect =
            this.ViewHasEncodingEffect(constraints.Per);
        constraints.Per.HasValidationOnlyConstraints =
            type.ComponentConstraintSets.Length > 0
            || this.HasInvisiblePerApplication(type);
        constraints.Oer.HasEncodingEffect =
            this.ViewHasEncodingEffect(constraints.Oer);
        constraints.Ber.HasEncodingEffect =
            this.ViewHasEncodingEffect(constraints.Ber);
        constraints.Xer.HasEncodingEffect =
            this.ViewHasEncodingEffect(constraints.Xer);
        constraints.Other.HasEncodingEffect =
            this.ViewHasEncodingEffect(constraints.Other);
        type.EncodingConstraints = constraints;
        this.Compiling.Remove(type);
        this.Compiled.Add(type);
    }

    internal void CompileRelatedTypes(ResolvedTypeSyntax type)
    {
        if (type.AliasTarget != null)
        {
            this.CompileType(type.AliasTarget);
        }

        if (type.InnerType != null)
        {
            this.CompileType(type.InnerType);
        }

        if (type.SelectedComponent != null
            && type.SelectedComponent.TypeResolution != null)
        {
            this.CompileType(type.SelectedComponent.TypeResolution);
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedTypeSyntax? componentType = type.Components[componentIndex]
                .TypeResolution;
            if (componentType != null)
            {
                this.CompileType(componentType);
            }

            componentIndex = componentIndex + 1;
        }

        int constraintIndex = 0;
        while (constraintIndex < type.ConstraintTypes.Length)
        {
            this.CompileType(type.ConstraintTypes[constraintIndex]);
            constraintIndex = constraintIndex + 1;
        }
    }

    internal IntegerConstraintResult? ComputePerIntegerConstraints(
        ResolvedTypeSyntax type)
    {
        IntegerConstraintSet? domain = this.Solver.CreateTypeDomain(type);
        if (domain == null || type.EffectiveConstraint == null)
        {
            return null;
        }

        EncodingSetEvaluation evaluated = this.EvaluatePerApplications(
            type.EffectiveConstraint,
            type,
            domain,
            0);
        return evaluated.Visible
            ? this.CreateIntegerResult(evaluated, domain)
            : null;
    }

    internal IntegerConstraintResult? ComputeOerIntegerConstraints(
        ResolvedTypeSyntax type)
    {
        IntegerConstraintSet? domain = this.Solver.CreateTypeDomain(type);
        if (domain == null || type.EffectiveConstraint == null)
        {
            return null;
        }

        EncodingSetEvaluation evaluated = this.EvaluateOerApplications(
            type.EffectiveConstraint,
            type,
            domain,
            0);
        if (!evaluated.Visible || evaluated.Values == null)
        {
            return null;
        }

        IntegerConstraintSet hull = this.CreateHull(evaluated.Values);
        return new IntegerConstraintResult
        {
            Domain = domain,
            Root = hull,
            Extension = this.Algebra.CreateEmpty(),
            KnownValues = hull
        };
    }

    internal SizeConstraintResult? ComputePerSizeConstraints(ResolvedTypeSyntax type)
    {
        IntegerConstraintSet domain = this.CreateSizeDomain();
        if (type.EffectiveConstraint == null)
        {
            return null;
        }

        EncodingSetEvaluation evaluated = this.EvaluatePerApplications(
            type.EffectiveConstraint,
            type,
            domain,
            1);
        return evaluated.Visible
            ? this.CreateSizeResult(evaluated, domain)
            : null;
    }

    internal SizeConstraintResult? ComputeOerSizeConstraints(ResolvedTypeSyntax type)
    {
        IntegerConstraintSet domain = this.CreateSizeDomain();
        if (type.EffectiveConstraint == null)
        {
            return null;
        }

        EncodingSetEvaluation evaluated = this.EvaluateOerApplications(
            type.EffectiveConstraint,
            type,
            domain,
            1);
        if (!evaluated.Visible || evaluated.Values == null)
        {
            return null;
        }

        IntegerConstraintSet hull = this.CreateHull(evaluated.Values);
        EncodingSetEvaluation bounded = this.CreateVisibleSet(hull);
        return this.CreateSizeResult(bounded, domain);
    }

    internal PermittedAlphabetResult? ComputePerAlphabetConstraint(
        ResolvedTypeSyntax type)
    {
        if (type.EffectiveConstraint == null)
        {
            return null;
        }

        IntegerConstraintSet domain = this.Repertoires.CreateRepertoire(
            type.EffectiveKind);
        EncodingSetEvaluation evaluated = this.EvaluatePerApplications(
            type.EffectiveConstraint,
            type,
            domain,
            2);
        if (!evaluated.Visible || evaluated.Values == null)
        {
            return null;
        }

        PermittedAlphabetResult result = new PermittedAlphabetResult
        {
            Domain = domain,
            Root = evaluated.Root,
            Extension = evaluated.Extension,
            KnownScalars = evaluated.Values,
            OpenExtension = evaluated.OpenExtension
        };
        this.Solver.BuildAlphabetIndexes(result);
        return result;
    }

    internal EncodingSetEvaluation EvaluatePerGraph(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension)
    {
        EncodingSetEvaluation root = graph.RootPartition == null
            || graph.RootPartition.Expression == null
            ? this.CreateInvisibleSet()
            : this.EvaluatePerNode(
                graph.RootPartition.Expression,
                type,
                domain,
                dimension);
        if (!root.Visible || root.Values == null)
        {
            return root;
        }

        IntegerConstraintSet rootValues = root.Root == null
            ? root.Values
            : root.Root;
        IntegerConstraintSet extensionValues = root.Extension == null
            ? this.Algebra.CreateEmpty()
            : root.Extension;
        bool open = root.OpenExtension;
        bool crossDimension = root.CrossDimensionApproximation;
        if (graph.ExtensionPartition != null)
        {
            open = open || graph.ExtensionPartition.Open;
            if (graph.ExtensionPartition.Expression != null)
            {
                EncodingSetEvaluation extension = this.EvaluatePerNode(
                    graph.ExtensionPartition.Expression,
                    type,
                    domain,
                    dimension);
                if (extension.Visible && extension.Values != null)
                {
                    extensionValues = this.Algebra.Union(
                        [extensionValues, extension.Values]);
                    crossDimension = crossDimension
                        || extension.CrossDimensionApproximation;
                }
            }
        }

        return new EncodingSetEvaluation
        {
            Root = rootValues,
            Extension = extensionValues,
            Values = this.Algebra.Union([rootValues, extensionValues]),
            Visible = true,
            OpenExtension = open,
            CrossDimensionApproximation = crossDimension
        };
    }

    internal EncodingSetEvaluation EvaluatePerApplications(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension)
    {
        NormalizedConstraintGraph[] applications = graph.Applications.Length == 0
            ? [graph]
            : graph.Applications;
        EncodingSetEvaluation? accumulated = null;
        int index = 0;
        while (index < applications.Length)
        {
            EncodingSetEvaluation current = this.EvaluatePerGraph(
                applications[index],
                type,
                domain,
                dimension);
            if (!current.Visible || current.Values == null)
            {
                if (accumulated != null && accumulated.Values != null)
                {
                    accumulated.Root = accumulated.Values;
                    accumulated.Extension = this.Algebra.CreateEmpty();
                    accumulated.OpenExtension = false;
                }

                index = index + 1;
                continue;
            }

            if (accumulated == null || accumulated.Values == null)
            {
                accumulated = current;
            }
            else
            {
                IntegerConstraintSet known = this.Algebra.Intersect([
                    accumulated.Values,
                    current.Values]);
                IntegerConstraintSet root = this.Algebra.Intersect([
                    accumulated.Root == null
                        ? accumulated.Values
                        : accumulated.Root,
                    current.Root == null ? current.Values : current.Root]);
                accumulated = new EncodingSetEvaluation
                {
                    Root = root,
                    Extension = this.Algebra.Except(known, root),
                    Values = known,
                    Visible = true,
                    OpenExtension = accumulated.OpenExtension
                        && current.OpenExtension,
                    CrossDimensionApproximation =
                        accumulated.CrossDimensionApproximation
                        || current.CrossDimensionApproximation
                };
            }

            index = index + 1;
        }

        return accumulated == null
            ? this.CreateInvisibleSet()
            : accumulated;
    }

    internal EncodingSetEvaluation EvaluateOerApplications(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension)
    {
        NormalizedConstraintGraph[] applications = graph.Applications.Length == 0
            ? [graph]
            : graph.Applications;
        System.Collections.Generic.List<IntegerConstraintSet> visible =
            new System.Collections.Generic.List<IntegerConstraintSet>();
        int index = 0;
        while (index < applications.Length)
        {
            NormalizedConstraintGraph application = applications[index];
            if (application.ExtensionPartition == null
                && application.RootPartition != null
                && application.RootPartition.Expression != null)
            {
                EncodingSetEvaluation evaluated = this.EvaluateOerNode(
                    application.RootPartition.Expression,
                    type,
                    domain,
                    dimension);
                if (evaluated.Visible && evaluated.Values != null)
                {
                    visible.Add(evaluated.Values);
                }
            }

            index = index + 1;
        }

        return visible.Count == 0
            ? this.CreateInvisibleSet()
            : this.CreateVisibleSet(this.Algebra.Intersect(visible.ToArray()));
    }

    internal EncodingSetEvaluation EvaluatePerNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension)
    {
        return this.EvaluateEncodingNode(node, type, domain, dimension, false);
    }

    internal EncodingSetEvaluation EvaluateOerNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension)
    {
        return this.EvaluateEncodingNode(node, type, domain, dimension, true);
    }

    internal EncodingSetEvaluation EvaluateEncodingNode(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension,
        bool oer)
    {
        if (node.Kind == AsnConstraintKind.Intersection)
        {
            return this.EvaluateEncodingIntersection(
                node,
                type,
                domain,
                dimension,
                oer);
        }

        if (node.Kind == AsnConstraintKind.Union)
        {
            return this.EvaluateEncodingUnion(
                node,
                type,
                domain,
                dimension,
                oer);
        }

        if (node.Kind == AsnConstraintKind.Except)
        {
            return this.EvaluateEncodingExcept(
                node,
                type,
                domain,
                dimension,
                oer);
        }

        if (node.Kind == AsnConstraintKind.AllExcept)
        {
            return this.CreateVisibleSet(domain);
        }

        if (node.Kind == AsnConstraintKind.ContainedSubtype)
        {
            return this.EvaluateEncodingContainedSubtype(
                node,
                domain,
                dimension,
                oer);
        }

        if (dimension == 0)
        {
            return this.EvaluateEncodingIntegerLeaf(node, type, domain);
        }

        if (dimension == 1)
        {
            return this.EvaluateEncodingSizeLeaf(node, type, domain, oer);
        }

        if (dimension == 2)
        {
            return this.EvaluateEncodingAlphabetLeaf(node);
        }

        return this.CreateInvisibleSet();
    }

    internal EncodingSetEvaluation EvaluateEncodingIntersection(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension,
        bool oer)
    {
        System.Collections.Generic.List<IntegerConstraintSet> roots =
            new System.Collections.Generic.List<IntegerConstraintSet>();
        System.Collections.Generic.List<IntegerConstraintSet> values =
            new System.Collections.Generic.List<IntegerConstraintSet>();
        bool open = true;
        bool crossDimension = false;
        int index = 0;
        while (index < node.Children.Length)
        {
            EncodingSetEvaluation child = this.EvaluateEncodingNode(
                node.Children[index],
                type,
                domain,
                dimension,
                oer);
            if (child.Visible && child.Values != null)
            {
                values.Add(child.Values);
                roots.Add(child.Root == null ? child.Values : child.Root);
                open = open && child.OpenExtension;
                crossDimension = crossDimension
                    || child.CrossDimensionApproximation;
            }
            else if (this.NodeIsVisibleInOtherPerDimension(
                node.Children[index],
                type,
                dimension,
                oer))
            {
                crossDimension = true;
            }

            index = index + 1;
        }

        if (values.Count == 0)
        {
            return this.CreateInvisibleSet();
        }

        IntegerConstraintSet known = this.Algebra.Intersect(values.ToArray());
        IntegerConstraintSet root = this.Algebra.Intersect(roots.ToArray());
        return new EncodingSetEvaluation
        {
            Root = root,
            Extension = this.Algebra.Except(known, root),
            Values = known,
            Visible = true,
            OpenExtension = open,
            CrossDimensionApproximation = crossDimension
        };
    }

    internal EncodingSetEvaluation EvaluateEncodingUnion(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension,
        bool oer)
    {
        IntegerConstraintSet[] roots = new IntegerConstraintSet[node.Children.Length];
        IntegerConstraintSet[] values = new IntegerConstraintSet[node.Children.Length];
        bool open = false;
        bool crossDimension = false;
        int index = 0;
        while (index < node.Children.Length)
        {
            EncodingSetEvaluation child = this.EvaluateEncodingNode(
                node.Children[index],
                type,
                domain,
                dimension,
                oer);
            if (!child.Visible || child.Values == null)
            {
                return this.CreateInvisibleSet();
            }

            values[index] = child.Values;
            roots[index] = child.Root == null ? child.Values : child.Root;
            open = open || child.OpenExtension;
            crossDimension = crossDimension
                || child.CrossDimensionApproximation;
            index = index + 1;
        }

        IntegerConstraintSet known = this.Algebra.Union(values);
        IntegerConstraintSet root = this.Algebra.Union(roots);
        if (crossDimension)
        {
            known = this.CreateHull(known);
            root = this.CreateHull(root);
        }

        return new EncodingSetEvaluation
        {
            Root = root,
            Extension = this.Algebra.Except(known, root),
            Values = known,
            Visible = true,
            OpenExtension = open,
            CrossDimensionApproximation = crossDimension
        };
    }

    internal bool NodeIsVisibleInOtherPerDimension(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        int dimension,
        bool oer)
    {
        if (oer || !this.IsKnownMultiplierString(type.EffectiveKind))
        {
            return false;
        }

        if (dimension == 1)
        {
            IntegerConstraintSet alphabet = this.Repertoires.CreateRepertoire(
                type.EffectiveKind);
            return this.EvaluatePerNode(
                node,
                type,
                alphabet,
                2).Visible;
        }

        if (dimension == 2)
        {
            return this.EvaluatePerNode(
                node,
                type,
                this.CreateSizeDomain(),
                1).Visible;
        }

        return false;
    }

    internal EncodingSetEvaluation EvaluateEncodingExcept(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        int dimension,
        bool oer)
    {
        if (node.Children.Length == 0)
        {
            return this.CreateInvisibleSet();
        }

        EncodingSetEvaluation source = this.EvaluateEncodingNode(
            node.Children[0],
            type,
            domain,
            dimension,
            oer);
        if (!source.Visible || source.Values == null
            || node.SourceElement != null || node.SourceSyntax != null)
        {
            return source;
        }

        if (node.Children.Length < 2)
        {
            return source;
        }

        EncodingSetEvaluation excluded = this.EvaluateEncodingNode(
            node.Children[1],
            type,
            domain,
            dimension,
            oer);
        if (!excluded.Visible || excluded.Values == null)
        {
            return source;
        }

        IntegerConstraintSet root = this.Algebra.Except(
            source.Root == null ? source.Values : source.Root,
            excluded.Values);
        IntegerConstraintSet known = this.Algebra.Except(
            source.Values,
            excluded.Values);
        return new EncodingSetEvaluation
        {
            Root = root,
            Extension = this.Algebra.Except(known, root),
            Values = known,
            Visible = true,
            OpenExtension = source.OpenExtension,
            CrossDimensionApproximation =
                source.CrossDimensionApproximation
        };
    }

    internal EncodingSetEvaluation EvaluateEncodingContainedSubtype(
        NormalizedConstraintNode node,
        IntegerConstraintSet domain,
        int dimension,
        bool oer)
    {
        if (node.ResolvedContainedSubtype == null)
        {
            return this.CreateInvisibleSet();
        }

        ResolvedTypeSyntax contained = node.ResolvedContainedSubtype;
        this.CompileType(contained);
        EncodingConstraintView view = oer
            ? contained.EncodingConstraints.Oer
            : contained.EncodingConstraints.Per;
        if (dimension == 0 && view.IntegerConstraints != null)
        {
            return this.CreateContainedIntegerEvaluation(
                view.IntegerConstraints,
                domain);
        }

        if (dimension == 1 && view.SizeConstraints != null)
        {
            return this.CreateContainedSizeEvaluation(view.SizeConstraints, domain);
        }

        if (dimension == 2 && view.PermittedAlphabet != null)
        {
            return this.CreateContainedAlphabetEvaluation(
                view.PermittedAlphabet,
                domain);
        }

        return this.CreateInvisibleSet();
    }

    internal EncodingSetEvaluation EvaluateEncodingIntegerLeaf(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain)
    {
        if (node.Kind == AsnConstraintKind.SingleValue)
        {
            IntegerEvaluationResult value = this.Solver.ResolveNodeValue(
                node.SourceSyntax == null ? null : node.SourceSyntax.ValueSyntax,
                node.Value,
                node.GoverningType == null ? type : node.GoverningType);
            return value.Success
                ? this.CreateVisibleSet(this.Algebra.Intersect([
                    domain,
                    this.Algebra.CreateSingleton(value.Value)]))
                : this.CreateInvisibleSet();
        }

        if (node.Kind == AsnConstraintKind.ValueRange)
        {
            IntegerConstraintSet? range = this.Solver.EvaluateRange(node, type, domain);
            return range == null
                ? this.CreateInvisibleSet()
                : this.CreateVisibleSet(range);
        }

        return this.CreateInvisibleSet();
    }

    internal EncodingSetEvaluation EvaluateEncodingSizeLeaf(
        NormalizedConstraintNode node,
        ResolvedTypeSyntax type,
        IntegerConstraintSet domain,
        bool oer)
    {
        if (node.Kind != AsnConstraintKind.Size
            || node.NestedConstraints.Length != 1)
        {
            return this.CreateInvisibleSet();
        }

        NormalizedConstraintGraph nested = node.NestedConstraints[0];
        if (oer && nested.ExtensionPartition != null)
        {
            return this.CreateInvisibleSet();
        }

        return oer
            ? this.EvaluateOerApplications(nested, type, domain, 0)
            : this.EvaluatePerApplications(nested, type, domain, 0);
    }

    internal EncodingSetEvaluation EvaluateEncodingAlphabetLeaf(
        NormalizedConstraintNode node)
    {
        if (node.Kind != AsnConstraintKind.PermittedAlphabet
            || node.NestedConstraints.Length != 1
            || node.NestedConstraints[0].ExtensionPartition != null
            || node.AlphabetSet == null)
        {
            return this.CreateInvisibleSet();
        }

        return this.CreateVisibleSet(node.AlphabetSet);
    }

    internal EncodingSetEvaluation CreateContainedIntegerEvaluation(
        IntegerConstraintResult result,
        IntegerConstraintSet domain)
    {
        if (result.OpenExtension || result.KnownValues == null)
        {
            return this.CreateInvisibleSet();
        }

        return new EncodingSetEvaluation
        {
            Root = result.Root == null ? result.KnownValues : result.Root,
            Extension = result.Extension,
            Values = this.Algebra.Intersect([domain, result.KnownValues]),
            Visible = true
        };
    }

    internal EncodingSetEvaluation CreateContainedSizeEvaluation(
        SizeConstraintResult result,
        IntegerConstraintSet domain)
    {
        if (result.OpenExtension || result.KnownSizes == null)
        {
            return this.CreateInvisibleSet();
        }

        return new EncodingSetEvaluation
        {
            Root = result.Root == null ? result.KnownSizes : result.Root,
            Extension = result.Extension,
            Values = this.Algebra.Intersect([domain, result.KnownSizes]),
            Visible = true
        };
    }

    internal EncodingSetEvaluation CreateContainedAlphabetEvaluation(
        PermittedAlphabetResult result,
        IntegerConstraintSet domain)
    {
        if (result.OpenExtension || result.KnownScalars == null)
        {
            return this.CreateInvisibleSet();
        }

        return new EncodingSetEvaluation
        {
            Root = result.Root == null ? result.KnownScalars : result.Root,
            Extension = result.Extension,
            Values = this.Algebra.Intersect([domain, result.KnownScalars]),
            Visible = true
        };
    }

    internal IntegerConstraintResult CreateIntegerResult(
        EncodingSetEvaluation evaluated,
        IntegerConstraintSet domain)
    {
        return new IntegerConstraintResult
        {
            Domain = domain,
            Root = evaluated.Root,
            Extension = evaluated.Extension,
            KnownValues = evaluated.Values,
            OpenExtension = evaluated.OpenExtension
        };
    }

    internal SizeConstraintResult CreateSizeResult(
        EncodingSetEvaluation evaluated,
        IntegerConstraintSet domain)
    {
        IntegerConstraintSet? root = evaluated.Root == null
            ? evaluated.Values
            : evaluated.Root;
        SizeConstraintResult result = new SizeConstraintResult
        {
            Domain = domain,
            Root = root,
            Extension = evaluated.Extension,
            KnownSizes = evaluated.Values,
            OpenExtension = evaluated.OpenExtension,
            HasRootCardinality = root != null && root.HasFiniteCardinality,
            RootCardinality = root == null
                ? System.Numerics.BigInteger.Zero
                : root.FiniteCardinality
        };
        if (root != null && root.Intervals.Length > 0)
        {
            IntegerInterval first = root.Intervals[0];
            IntegerInterval last = root.Intervals[root.Intervals.Length - 1];
            result.HasRootLowerBound = first.HasLowerBound;
            result.RootLowerBound = first.LowerBound;
            result.HasRootUpperBound = last.HasUpperBound;
            result.RootUpperBound = last.UpperBound;
        }

        return result;
    }

    internal EncodingSetEvaluation CreateVisibleSet(IntegerConstraintSet values)
    {
        return new EncodingSetEvaluation
        {
            Root = values,
            Extension = this.Algebra.CreateEmpty(),
            Values = values,
            Visible = true
        };
    }

    internal EncodingSetEvaluation CreateInvisibleSet()
    {
        return new EncodingSetEvaluation();
    }

    internal IntegerConstraintSet CreateSizeDomain()
    {
        return this.Algebra.CreateInterval(
            true,
            System.Numerics.BigInteger.Zero,
            false,
            System.Numerics.BigInteger.Zero);
    }

    internal IntegerConstraintSet CreateHull(IntegerConstraintSet values)
    {
        if (values.Intervals.Length == 0)
        {
            return this.Algebra.CreateEmpty();
        }

        IntegerInterval first = values.Intervals[0];
        IntegerInterval last = values.Intervals[values.Intervals.Length - 1];
        return this.Algebra.CreateInterval(
            first.HasLowerBound,
            first.LowerBound,
            last.HasUpperBound,
            last.UpperBound);
    }

    internal TimePropertySettings[] CollectVisibleTimeSettings(
        ResolvedTypeSyntax type)
    {
        if (!this.IsTimeSettingsType(type.EffectiveKind)
            || type.EffectiveConstraint == null)
        {
            return [];
        }

        NormalizedConstraintGraph[] applications =
            type.EffectiveConstraint.Applications.Length == 0
                ? [type.EffectiveConstraint]
                : type.EffectiveConstraint.Applications;
        System.Collections.Generic.List<TimePropertySettings> settings =
            new System.Collections.Generic.List<TimePropertySettings>();
        int index = 0;
        while (index < applications.Length)
        {
            NormalizedConstraintGraph graph = applications[index];
            if (graph.ExtensionPartition == null
                && graph.RootPartition != null
                && graph.RootPartition.Expression != null)
            {
                this.CollectTimeSettingsFromNode(
                    graph.RootPartition.Expression,
                    settings);
            }

            index = index + 1;
        }

        return settings.ToArray();
    }

    internal void CollectTimeSettingsFromNode(
        NormalizedConstraintNode node,
        System.Collections.Generic.List<TimePropertySettings> settings)
    {
        if (node.Kind == AsnConstraintKind.PropertySettings
            && node.CompiledTimeSettings != null
            && !settings.Contains(node.CompiledTimeSettings))
        {
            settings.Add(node.CompiledTimeSettings);
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            this.CollectTimeSettingsFromNode(node.Children[index], settings);
            index = index + 1;
        }
    }

    internal bool IsPerSizeType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BitString
            || kind == AsnTypeKind.OctetString
            || kind == AsnTypeKind.SequenceOf
            || kind == AsnTypeKind.SetOf
            || this.IsKnownMultiplierString(kind);
    }

    internal bool IsOerSizeType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BitString
            || kind == AsnTypeKind.OctetString
            || this.IsKnownMultiplierString(kind);
    }

    internal bool IsKnownMultiplierString(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.ObjectDescriptor
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
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal bool ViewHasEncodingEffect(EncodingConstraintView view)
    {
        return view.IntegerConstraints != null
            || view.SizeConstraints != null
            || view.PermittedAlphabet != null
            || view.TimeSettings.Length > 0;
    }

    internal bool HasInvisiblePerApplication(ResolvedTypeSyntax type)
    {
        if (type.EffectiveConstraint == null)
        {
            return false;
        }

        NormalizedConstraintGraph[] applications =
            type.EffectiveConstraint.Applications.Length == 0
                ? [type.EffectiveConstraint]
                : type.EffectiveConstraint.Applications;
        int index = 0;
        while (index < applications.Length)
        {
            if (!this.PerApplicationIsVisible(applications[index], type))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool PerApplicationIsVisible(
        NormalizedConstraintGraph graph,
        ResolvedTypeSyntax type)
    {
        if (type.EffectiveKind == AsnTypeKind.Integer)
        {
            IntegerConstraintSet? domain = this.Solver.CreateTypeDomain(type);
            return domain != null && this.EvaluatePerGraph(
                graph,
                type,
                domain,
                0).Visible;
        }

        if (this.IsPerSizeType(type.EffectiveKind)
            && this.EvaluatePerGraph(
                graph,
                type,
                this.CreateSizeDomain(),
                1).Visible)
        {
            return true;
        }

        if (this.IsKnownMultiplierString(type.EffectiveKind))
        {
            IntegerConstraintSet alphabet = this.Repertoires.CreateRepertoire(
                type.EffectiveKind);
            if (this.EvaluatePerGraph(
                graph,
                type,
                alphabet,
                2).Visible)
            {
                return true;
            }
        }

        if (this.IsTimeSettingsType(type.EffectiveKind)
            && graph.ExtensionPartition == null
            && graph.RootPartition != null
            && graph.RootPartition.Expression != null)
        {
            System.Collections.Generic.List<TimePropertySettings> settings =
                new System.Collections.Generic.List<TimePropertySettings>();
            this.CollectTimeSettingsFromNode(
                graph.RootPartition.Expression,
                settings);
            return settings.Count > 0;
        }

        return false;
    }
}
