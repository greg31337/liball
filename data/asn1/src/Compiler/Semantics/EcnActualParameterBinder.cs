namespace asn1;

internal class EcnActualParameterBinder
{
    internal DiagnosticBag Diagnostics;
    internal int EndPosition;
    internal bool Success = true;

    internal EcnActualParameterBinder(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal EcnActualParameter[] BindActualParameters(
        AsnToken[] tokens,
        int start,
        EcnSymbolTable table,
        EcnSymbol owner,
        EcnSymbol? target)
    {
        this.EndPosition = start;
        this.Success = true;
        if (start + 1 >= tokens.Length
            || tokens[start].Kind != AsnTokenKind.LeftBrace
            || tokens[start + 1].Kind != AsnTokenKind.LessThan)
        {
            return [];
        }

        System.Collections.Generic.List<EcnTokenGroupSyntax> groups =
            new System.Collections.Generic.List<EcnTokenGroupSyntax>();
        int groupStart = start + 2;
        int position = groupStart;
        int depth = 0;
        while (position < tokens.Length)
        {
            if (depth == 0
                && position + 1 < tokens.Length
                && tokens[position].Kind == AsnTokenKind.GreaterThan
                && tokens[position + 1].Kind == AsnTokenKind.RightBrace)
            {
                this.AddActualGroup(tokens, groups, groupStart, position);
                this.EndPosition = position + 2;
                return this.ValidateActualGroups(groups, table, owner, target);
            }

            AsnTokenKind kind = tokens[position].Kind;
            if (this.IsOpening(kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosing(kind))
            {
                if (depth > 0)
                {
                    depth = depth - 1;
                }
            }
            else if (depth == 0 && kind == AsnTokenKind.Comma)
            {
                this.AddActualGroup(tokens, groups, groupStart, position);
                groupStart = position + 1;
            }

            position = position + 1;
        }

        this.AddDiagnostic(
            "The ECN actual parameter list is missing its closing '>}' delimiter.",
            tokens[start].Span);
        this.EndPosition = tokens.Length;
        return [];
    }

    internal bool ValidateParameterPresence(
        EcnSymbol? target,
        AsnSourceSpan? span)
    {
        EcnSymbol? declaration = target == null
            ? null
            : target.ImportedTarget == null
                ? target
                : target.ImportedTarget;
        int formalCount = declaration == null || declaration.Scope == null
            ? 0
            : declaration.Scope.Symbols.Length;
        if (formalCount == 0)
        {
            return true;
        }

        this.AddDiagnostic(
            "The parameterized reference requires " + formalCount
                + " actual parameters.",
            span);
        return false;
    }

    internal EcnActualParameter[] ValidateActualGroups(
        System.Collections.Generic.List<EcnTokenGroupSyntax> groups,
        EcnSymbolTable table,
        EcnSymbol owner,
        EcnSymbol? target)
    {
        EcnSymbol? declaration = target == null
            ? null
            : target.ImportedTarget == null
                ? target
                : target.ImportedTarget;
        EcnSymbol[] formals = declaration == null || declaration.Scope == null
            ? []
            : declaration.Scope.Symbols;
        if (groups.Count != formals.Length)
        {
            this.AddDiagnostic(
                "The parameterized reference supplies " + groups.Count
                    + " actual parameters, but " + formals.Length
                    + " are required.",
                groups.Count == 0 ? owner.Span : groups[0].Span);
        }

        EcnActualParameter[] actuals = new EcnActualParameter[groups.Count];
        int index = 0;
        while (index < groups.Count)
        {
            EcnParameterKind expected = index < formals.Length
                ? formals[index].ParameterKind
                : EcnParameterKind.Unknown;
            actuals[index] = this.BindActualGroup(
                groups[index],
                expected,
                table,
                owner);
            index = index + 1;
        }

        return actuals;
    }

    internal EcnActualParameter BindActualGroup(
        EcnTokenGroupSyntax group,
        EcnParameterKind expected,
        EcnSymbolTable table,
        EcnSymbol owner)
    {
        EcnActualParameter actual = new EcnActualParameter
        {
            ExpectedKind = expected,
            Tokens = group.Tokens,
            Span = group.Span
        };
        if (group.Tokens.Length == 0)
        {
            this.AddDiagnostic("An ECN actual parameter cannot be empty.", group.Span);
            return actual;
        }

        if (expected == EcnParameterKind.Reference
            || expected == EcnParameterKind.AsnValue)
        {
            actual.ReferenceName = this.ReadReferenceName(group.Tokens);
            actual.Valid = true;
            return actual;
        }

        if ((expected == EcnParameterKind.EncodingObject
                || expected == EcnParameterKind.EncodingObjectSet)
            && group.Tokens[0].Kind == AsnTokenKind.LeftBrace)
        {
            actual.Valid = true;
            return actual;
        }

        string name = this.ReadReferenceName(group.Tokens);
        actual.ReferenceName = name;
        if (expected == EcnParameterKind.EncodingClass)
        {
            EcnBuiltInClassDefinition? builtIn =
                EcnBuiltInClassCatalog.FindClass(name);
            if (builtIn != null)
            {
                actual.BuiltIn = true;
                actual.Valid = true;
                return actual;
            }

            actual.Symbol = this.FindClassReference(table, owner, name);
        }
        else if (expected == EcnParameterKind.EncodingObject)
        {
            actual.Symbol = this.FindReference(
                table,
                owner,
                name,
                EcnSymbolKind.EncodingObject,
                EcnParameterKind.EncodingObject);
        }
        else if (expected == EcnParameterKind.EncodingObjectSet)
        {
            if (EcnEncodingObjectSetCatalog.IsBuiltIn(name))
            {
                actual.BuiltIn = true;
                actual.PredeclaredSet = EcnEncodingObjectSetCatalog.FindSet(
                    name);
                actual.Valid = true;
                return actual;
            }

            actual.Symbol = this.FindReference(
                table,
                owner,
                name,
                EcnSymbolKind.EncodingObjectSet,
                EcnParameterKind.EncodingObjectSet);
        }

        actual.Valid = actual.Symbol != null;
        if (!actual.Valid)
        {
            this.AddDiagnostic(
                "The actual parameter '" + name
                    + "' is not compatible with formal parameter kind '"
                    + expected.ToString() + "'.",
                group.Span);
        }

        return actual;
    }

    internal EcnSymbol? FindClassReference(
        EcnSymbolTable table,
        EcnSymbol owner,
        string name)
    {
        EcnSymbol? parameter = this.FindOwnerParameter(
            owner,
            name,
            EcnParameterKind.EncodingClass);
        if (parameter != null)
        {
            return parameter;
        }

        return this.FindTableReference(
            table,
            name,
            EcnSymbolKind.EncodingClass,
            EcnSymbolKind.EncodingStructure);
    }

    internal EcnSymbol? FindReference(
        EcnSymbolTable table,
        EcnSymbol owner,
        string name,
        EcnSymbolKind kind,
        EcnParameterKind parameterKind)
    {
        EcnSymbol? parameter = this.FindOwnerParameter(
            owner,
            name,
            parameterKind);
        return parameter == null
            ? this.FindTableReference(table, name, kind, kind)
            : parameter;
    }

    internal EcnSymbol? FindOwnerParameter(
        EcnSymbol owner,
        string name,
        EcnParameterKind kind)
    {
        EcnScope? scope = owner.Scope;
        if (scope == null)
        {
            return null;
        }

        EcnSymbol? found = scope.FindLocalSymbol(name);
        return found != null && found.ParameterKind == kind
            ? found
            : null;
    }

    internal EcnSymbol? FindTableReference(
        EcnSymbolTable table,
        string reference,
        EcnSymbolKind firstKind,
        EcnSymbolKind secondKind)
    {
        int external = reference.IndexOf(
            ".",
            System.StringComparison.Ordinal);
        string moduleName = external < 0
            ? ""
            : reference.Substring(0, external);
        string name = external < 0
            ? reference
            : reference.Substring(external + 1);
        EcnSymbol? found = null;
        int matches = 0;
        int index = 0;
        while (index < table.Symbols.Length)
        {
            EcnSymbol candidate = table.Symbols[index];
            if (candidate.Name == name
                && (candidate.Kind == firstKind
                    || candidate.Kind == secondKind)
                && (moduleName.Length == 0
                    || candidate.SourceModuleName == moduleName))
            {
                found = candidate;
                matches = matches + 1;
            }

            index = index + 1;
        }

        return matches == 1 ? found : null;
    }

    internal string ReadReferenceName(AsnToken[] tokens)
    {
        if (tokens.Length >= 3
            && tokens[1].Kind == AsnTokenKind.Dot)
        {
            return tokens[0].Text + "." + tokens[2].Text;
        }

        return tokens[0].Text;
    }

    internal void AddActualGroup(
        AsnToken[] tokens,
        System.Collections.Generic.List<EcnTokenGroupSyntax> groups,
        int start,
        int end)
    {
        if (end <= start)
        {
            return;
        }

        AsnToken[] copied = this.CopyTokens(tokens, start, end - start);
        groups.Add(new EcnTokenGroupSyntax
        {
            Tokens = copied,
            Span = this.CreateSpan(copied)
        });
    }

    internal AsnToken[] CopyTokens(AsnToken[] tokens, int start, int count)
    {
        AsnToken[] copied = new AsnToken[count];
        int index = 0;
        while (index < count)
        {
            copied[index] = tokens[start + index];
            index = index + 1;
        }

        return copied;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken[] tokens)
    {
        if (tokens.Length == 0)
        {
            return null;
        }

        AsnSourceSpan? first = tokens[0].Span;
        AsnSourceSpan? last = tokens[tokens.Length - 1].Span;
        if (first == null || last == null)
        {
            return first;
        }

        return new AsnSourceSpan
        {
            Start = first.Start,
            End = last.End
        };
    }

    internal bool IsOpening(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket;
    }

    internal bool IsClosing(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket;
    }

    internal void AddDiagnostic(string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            EcnDiagnosticCodes.InvalidActualParameter,
            message,
            span,
            null);
        this.Success = false;
    }
}
