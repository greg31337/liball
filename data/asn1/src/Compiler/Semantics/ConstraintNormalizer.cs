namespace asn1;

internal class ConstraintNormalizer
{
    internal System.Collections.Generic.List<ResolvedTypeSyntax> DomainTypes =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal System.Collections.Generic.List<ConstraintDomainIdentity> Domains =
        new System.Collections.Generic.List<ConstraintDomainIdentity>();

    internal void NormalizeTypes(SymbolTableEntry[] types)
    {
        if (types == null)
        {
            throw new System.ArgumentNullException("types");
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        int index = 0;
        while (index < types.Length)
        {
            ResolvedTypeSyntax? resolution = types[index].ResolvedType;
            if (resolution != null)
            {
                this.NormalizeType(resolution, visited);
            }

            index = index + 1;
        }

        System.Collections.Generic.List<ResolvedTypeSyntax> combined =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        index = 0;
        while (index < types.Length)
        {
            ResolvedTypeSyntax? resolution = types[index].ResolvedType;
            if (resolution != null)
            {
                this.CombineTypeConstraints(resolution, combined);
            }

            index = index + 1;
        }
    }

    internal void NormalizeType(
        ResolvedTypeSyntax resolution,
        System.Collections.Generic.List<ResolvedTypeSyntax> visited)
    {
        if (visited.Contains(resolution))
        {
            return;
        }

        visited.Add(resolution);
        ConstraintDomainIdentity domain = this.GetDomain(resolution);
        resolution.ConstraintDomain = domain;
        ParsedConstraintSyntax[] constraints = resolution.SourceSyntax == null
            ? []
            : resolution.SourceSyntax.ConstraintSyntax;
        NormalizedConstraintGraph[] graphs =
            new NormalizedConstraintGraph[constraints.Length];
        int constraintIndex = 0;
        while (constraintIndex < constraints.Length)
        {
            graphs[constraintIndex] = this.NormalizeConstraint(
                constraints[constraintIndex],
                domain,
                resolution);
            constraintIndex = constraintIndex + 1;
        }

        resolution.NormalizedConstraints = graphs;
        if (resolution.AliasTarget != null)
        {
            this.NormalizeType(resolution.AliasTarget, visited);
        }

        if (resolution.InnerType != null)
        {
            this.NormalizeType(resolution.InnerType, visited);
        }

        if (resolution.SelectedComponent != null
            && resolution.SelectedComponent.TypeResolution != null)
        {
            this.NormalizeType(resolution.SelectedComponent.TypeResolution, visited);
        }

        int componentIndex = 0;
        while (componentIndex < resolution.Components.Length)
        {
            ResolvedTypeSyntax? componentType =
                resolution.Components[componentIndex].TypeResolution;
            if (componentType != null)
            {
                this.NormalizeType(componentType, visited);
            }

            componentIndex = componentIndex + 1;
        }
    }

    internal void CombineTypeConstraints(
        ResolvedTypeSyntax resolution,
        System.Collections.Generic.List<ResolvedTypeSyntax> combined)
    {
        if (combined.Contains(resolution))
        {
            return;
        }

        combined.Add(resolution);
        ResolvedTypeSyntax? inheritedType = this.FindInheritedConstraintType(resolution);
        if (inheritedType != null)
        {
            this.CombineTypeConstraints(inheritedType, combined);
        }

        System.Collections.Generic.List<NormalizedConstraintGraph> applications =
            new System.Collections.Generic.List<NormalizedConstraintGraph>();
        if (inheritedType != null && inheritedType.EffectiveConstraint != null)
        {
            if (inheritedType.EffectiveConstraint.Applications.Length == 0)
            {
                applications.Add(inheritedType.EffectiveConstraint);
            }
            else
            {
                applications.AddRange(inheritedType.EffectiveConstraint.Applications);
            }
        }

        applications.AddRange(resolution.NormalizedConstraints);
        resolution.EffectiveConstraint = this.CombineApplications(
            applications.ToArray(),
            resolution.ConstraintDomain);

        if (resolution.AliasTarget != null)
        {
            this.CombineTypeConstraints(resolution.AliasTarget, combined);
        }

        if (resolution.InnerType != null)
        {
            this.CombineTypeConstraints(resolution.InnerType, combined);
        }

        if (resolution.SelectedComponent != null
            && resolution.SelectedComponent.TypeResolution != null)
        {
            this.CombineTypeConstraints(resolution.SelectedComponent.TypeResolution, combined);
        }

        int componentIndex = 0;
        while (componentIndex < resolution.Components.Length)
        {
            ResolvedTypeSyntax? componentType =
                resolution.Components[componentIndex].TypeResolution;
            if (componentType != null)
            {
                this.CombineTypeConstraints(componentType, combined);
            }

            componentIndex = componentIndex + 1;
        }
    }

    internal ResolvedTypeSyntax? FindInheritedConstraintType(ResolvedTypeSyntax resolution)
    {
        if (resolution.AliasTarget != null)
        {
            return resolution.AliasTarget;
        }

        if (resolution.SelectedComponent != null
            && resolution.SelectedComponent.TypeResolution != null)
        {
            return resolution.SelectedComponent.TypeResolution;
        }

        if ((resolution.SourceType?.Kind == AsnTypeKind.Tagged
                || resolution.SourceType?.Kind == AsnTypeKind.Prefixed)
            && resolution.InnerType != null)
        {
            return resolution.InnerType;
        }

        return null;
    }

    internal ConstraintDomainIdentity GetDomain(ResolvedTypeSyntax resolution)
    {
        ResolvedTypeSyntax canonical = this.FindCanonicalType(resolution);
        int index = 0;
        while (index < this.DomainTypes.Count)
        {
            if (this.DomainTypes[index] == canonical)
            {
                return this.Domains[index];
            }

            index = index + 1;
        }

        ConstraintDomainIdentity domain = new ConstraintDomainIdentity
        {
            Identity = this.Domains.Count,
            CanonicalType = canonical,
            EffectiveKind = canonical.EffectiveKind
        };
        this.DomainTypes.Add(canonical);
        this.Domains.Add(domain);
        return domain;
    }

    internal ResolvedTypeSyntax FindCanonicalType(ResolvedTypeSyntax resolution)
    {
        System.Collections.Generic.List<ResolvedTypeSyntax> visited =
            new System.Collections.Generic.List<ResolvedTypeSyntax>();
        ResolvedTypeSyntax current = resolution;
        while (!visited.Contains(current))
        {
            visited.Add(current);
            ResolvedTypeSyntax? next = null;
            if (current.AliasTarget != null)
            {
                next = current.AliasTarget;
            }
            else if (current.SelectedComponent != null
                && current.SelectedComponent.TypeResolution != null)
            {
                next = current.SelectedComponent.TypeResolution;
            }
            else if ((current.SourceType?.Kind == AsnTypeKind.Tagged
                    || current.SourceType?.Kind == AsnTypeKind.Prefixed)
                && current.InnerType != null)
            {
                next = current.InnerType;
            }

            if (next == null || visited.Contains(next))
            {
                return current;
            }

            current = next;
        }

        return current;
    }

    internal NormalizedConstraintGraph NormalizeConstraint(
        ParsedConstraintSyntax syntax,
        ConstraintDomainIdentity domain,
        ResolvedTypeSyntax governingType)
    {
        NormalizedConstraintPartition? root = syntax.RootElement == null
            ? null
            : new NormalizedConstraintPartition
            {
                Domain = domain,
                Expression = this.NormalizeElement(
                    syntax.RootElement,
                    syntax,
                    domain,
                    governingType),
                Extension = false,
                Open = false
            };
        NormalizedConstraintPartition? extension = null;
        if (syntax.Constraint.Extensible)
        {
            extension = new NormalizedConstraintPartition
            {
                Domain = domain,
                Expression = syntax.AdditionalElement == null
                    ? null
                    : this.NormalizeElement(
                        syntax.AdditionalElement,
                        syntax,
                        domain,
                        governingType),
                Extension = true,
                Open = true
            };
        }

        return new NormalizedConstraintGraph
        {
            Domain = domain,
            GoverningType = governingType,
            RootPartition = root,
            ExtensionPartition = extension,
            Exception = syntax.ExceptionElement == null
                ? null
                : this.NormalizeElement(
                    syntax.ExceptionElement,
                    syntax,
                    domain,
                    governingType),
            SourceSyntax = syntax
        };
    }

    internal NormalizedConstraintNode NormalizeElement(
        AsnConstraintElement element,
        ParsedConstraintSyntax syntax,
        ConstraintDomainIdentity domain,
        ResolvedTypeSyntax governingType)
    {
        ParsedSubtypeElementSyntax? sourceSyntax = this.FindSubtypeElement(syntax, element);
        ParsedNamedConstraintSyntax? sourceNamedConstraint =
            this.FindNamedConstraint(syntax, element);
        ParsedGeneralConstraintSyntax? sourceGeneralConstraint =
            this.FindGeneralConstraint(syntax, element);
        ParsedConstraintSyntax? nestedSyntax = this.FindNestedConstraint(syntax, element);
        NormalizedConstraintGraph[] nested = nestedSyntax == null
            ? []
            : [this.NormalizeConstraint(nestedSyntax, domain, governingType)];
        NormalizedConstraintNode[] children =
            new NormalizedConstraintNode[nested.Length == 0 ? element.Children.Length : 0];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.NormalizeElement(
                element.Children[index],
                syntax,
                domain,
                governingType);
            index = index + 1;
        }

        if ((element.Kind == AsnConstraintKind.Union
                || element.Kind == AsnConstraintKind.Intersection)
            && nested.Length == 0)
        {
            return this.CreateOperation(
                element.Kind,
                children,
                domain,
                element);
        }

        return new NormalizedConstraintNode
        {
            Kind = element.Kind,
            Domain = domain,
            GoverningType = governingType,
            SourceElement = element,
            SourceSyntax = sourceSyntax,
            SourceNamedConstraint = sourceNamedConstraint,
            SourceGeneralConstraint = sourceGeneralConstraint,
            Value = element.Value,
            LowerValue = element.LowerValue,
            UpperValue = element.UpperValue,
            Type = element.Type,
            Name = element.Name,
            Text = element.Text,
            LowerBound = element.LowerBound,
            UpperBound = element.UpperBound,
            Children = children,
            NestedConstraints = nested,
            HasLowerBound = element.HasLowerBound,
            HasUpperBound = element.HasUpperBound,
            LowerEndpointUnbounded = element.LowerEndpointUnbounded,
            UpperEndpointUnbounded = element.UpperEndpointUnbounded,
            LowerEndpointIncluded = element.LowerEndpointIncluded,
            UpperEndpointIncluded = element.UpperEndpointIncluded
        };
    }

    internal ParsedSubtypeElementSyntax? FindSubtypeElement(
        ParsedConstraintSyntax syntax,
        AsnConstraintElement element)
    {
        int index = 0;
        while (index < syntax.SubtypeElements.Length)
        {
            if (syntax.SubtypeElements[index].Element == element)
            {
                return syntax.SubtypeElements[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedNamedConstraintSyntax? FindNamedConstraint(
        ParsedConstraintSyntax syntax,
        AsnConstraintElement element)
    {
        int subtypeIndex = 0;
        while (subtypeIndex < syntax.SubtypeElements.Length)
        {
            ParsedSubtypeElementSyntax subtype = syntax.SubtypeElements[subtypeIndex];
            int namedIndex = 0;
            while (namedIndex < subtype.NamedConstraints.Length)
            {
                if (subtype.NamedConstraints[namedIndex].Element == element)
                {
                    return subtype.NamedConstraints[namedIndex];
                }

                namedIndex = namedIndex + 1;
            }

            subtypeIndex = subtypeIndex + 1;
        }

        return null;
    }

    internal ParsedGeneralConstraintSyntax? FindGeneralConstraint(
        ParsedConstraintSyntax syntax,
        AsnConstraintElement element)
    {
        int index = 0;
        while (index < syntax.GeneralConstraints.Length)
        {
            if (syntax.GeneralConstraints[index].Element == element)
            {
                return syntax.GeneralConstraints[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal NormalizedConstraintNode CreateOperation(
        AsnConstraintKind kind,
        NormalizedConstraintNode[] operands,
        ConstraintDomainIdentity domain,
        AsnConstraintElement? source)
    {
        System.Collections.Generic.List<NormalizedConstraintNode> children =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        int index = 0;
        while (index < operands.Length)
        {
            NormalizedConstraintNode operand = operands[index];
            if (operand.Kind == kind && operand.NestedConstraints.Length == 0)
            {
                children.AddRange(operand.Children);
            }
            else
            {
                children.Add(operand);
            }

            index = index + 1;
        }

        if (children.Count == 1)
        {
            return children[0];
        }

        return new NormalizedConstraintNode
        {
            Kind = kind,
            Domain = domain,
            SourceElement = source,
            Children = children.ToArray()
        };
    }

    internal NormalizedConstraintGraph? CombineApplications(
        NormalizedConstraintGraph[] applications,
        ConstraintDomainIdentity? domain)
    {
        if (applications.Length == 0 || domain == null)
        {
            return null;
        }

        if (applications.Length == 1)
        {
            return applications[0];
        }

        System.Collections.Generic.List<NormalizedConstraintNode> roots =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        System.Collections.Generic.List<NormalizedConstraintNode> complete =
            new System.Collections.Generic.List<NormalizedConstraintNode>();
        bool hasExtension = false;
        bool hasKnownAddition = false;
        bool open = true;
        int index = 0;
        while (index < applications.Length)
        {
            NormalizedConstraintGraph graph = applications[index];
            NormalizedConstraintNode? root = graph.RootPartition == null
                ? null
                : graph.RootPartition.Expression;
            if (root != null)
            {
                roots.Add(root);
                NormalizedConstraintNode full = root;
                if (graph.ExtensionPartition != null)
                {
                    hasExtension = true;
                    if (graph.ExtensionPartition.Expression != null)
                    {
                        hasKnownAddition = true;
                        full = this.CreateOperation(
                            AsnConstraintKind.Union,
                            [root, graph.ExtensionPartition.Expression],
                            domain,
                            null);
                    }
                }
                else
                {
                    open = false;
                }

                complete.Add(full);
            }

            index = index + 1;
        }

        NormalizedConstraintNode rootExpression = this.CreateOperation(
            AsnConstraintKind.Intersection,
            roots.ToArray(),
            domain,
            null);
        NormalizedConstraintNode completeExpression = this.CreateOperation(
            AsnConstraintKind.Intersection,
            complete.ToArray(),
            domain,
            null);
        NormalizedConstraintPartition? extension = null;
        if (hasExtension)
        {
            NormalizedConstraintNode? extensionExpression = hasKnownAddition
                ? new NormalizedConstraintNode
                {
                    Kind = AsnConstraintKind.Except,
                    Domain = domain,
                    Children = [completeExpression, rootExpression]
                }
                : null;
            extension = new NormalizedConstraintPartition
            {
                Domain = domain,
                Expression = extensionExpression,
                Extension = true,
                Open = open
            };
        }

        return new NormalizedConstraintGraph
        {
            Domain = domain,
            RootPartition = new NormalizedConstraintPartition
            {
                Domain = domain,
                Expression = rootExpression,
                Extension = false,
                Open = false
            },
            ExtensionPartition = extension,
            Applications = applications
        };
    }

    internal ParsedConstraintSyntax? FindNestedConstraint(
        ParsedConstraintSyntax syntax,
        AsnConstraintElement element)
    {
        int subtypeIndex = 0;
        while (subtypeIndex < syntax.SubtypeElements.Length)
        {
            ParsedSubtypeElementSyntax subtype = syntax.SubtypeElements[subtypeIndex];
            if (subtype.Element == element)
            {
                return subtype.NestedConstraintSyntax;
            }

            int namedIndex = 0;
            while (namedIndex < subtype.NamedConstraints.Length)
            {
                ParsedNamedConstraintSyntax named = subtype.NamedConstraints[namedIndex];
                if (named.Element == element)
                {
                    return named.ValueConstraintSyntax;
                }

                namedIndex = namedIndex + 1;
            }

            subtypeIndex = subtypeIndex + 1;
        }

        return null;
    }
}
