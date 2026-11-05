namespace asn1;

internal class ParameterizedParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;
    internal System.Collections.Generic.List<ParsedRecoverySyntax> ParameterRecoveries =
        new System.Collections.Generic.List<ParsedRecoverySyntax>();

    internal ParameterizedParser(
        AsnToken[] tokens,
        DiagnosticBag diagnostics,
        bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal ParameterListParseResult ParseFormalParameterListSyntax()
    {
        int start = this.Position;
        this.ParameterRecoveries.Clear();
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(200, "Expected '{' to begin a formal parameter list.", this.PeekToken());
            return this.CreateParameterListResult(false, [], start);
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<ParsedParameterSyntax> parameters =
            new System.Collections.Generic.List<ParsedParameterSyntax>();
        bool success = true;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(201, "A formal parameter list requires at least one parameter.", this.PeekToken());
            return this.CreateParameterListResult(false, [], start);
        }

        while (this.PeekToken() != null)
        {
            int parameterStart = this.Position;
            int parameterEnd = this.FindParameterEnd();
            if (parameterEnd == parameterStart)
            {
                this.AddDiagnostic(202, "Expected a formal parameter.", this.PeekToken());
                success = false;
            }
            else
            {
                AsnToken[] parameterTokens = this.CopyTokenRange(parameterStart, parameterEnd);
                ParsedParameterSyntax? parameter = this.ParseFormalParameterTokens(
                    parameterTokens,
                    parameters);
                this.Position = parameterEnd;
                if (parameter == null)
                {
                    success = false;
                    this.AddParameterRecovery("parameter list", parameterTokens);
                }
                else
                {
                    parameters.Add(parameter);
                }

            }
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                AsnToken comma = this.ReadRequiredToken();
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(203, "A formal parameter list cannot have a trailing comma.", comma);
                    success = false;
                    break;
                }

                continue;
            }

            break;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(204, "The formal parameter list is missing its closing brace.", this.PeekToken());
            return this.CreateParameterListResult(false, parameters.ToArray(), start);
        }

        this.Position = this.Position + 1;
        return this.CreateParameterListResult(success, parameters.ToArray(), start);
    }

    internal ParsedParameterSyntax? ParseFormalParameterTokens(
        AsnToken[] tokens,
        System.Collections.Generic.List<ParsedParameterSyntax> preceding)
    {
        int colon = this.FindTopLevelColon(tokens);
        int dummyIndex = colon < 0 ? 0 : colon + 1;
        if (dummyIndex != tokens.Length - 1 || !this.IsReference(tokens[dummyIndex]))
        {
            this.AddDiagnostic(205, "A formal parameter requires one dummy reference.", tokens[tokens.Length - 1]);
            return null;
        }

        AsnToken dummy = tokens[dummyIndex];
        ParsedParameterSyntax syntax = new ParsedParameterSyntax
        {
            Parameter = new AsnParameter { Name = dummy.Text },
            SourceTokens = tokens,
            Span = this.CreateSpan(tokens[0], tokens[tokens.Length - 1])
        };
        if (colon < 0)
        {
            if (this.IsLowerReference(dummy))
            {
                syntax.CandidateKinds = [AsnParameterKind.Type, AsnParameterKind.ObjectClass];
                syntax.Parameter.Kind = AsnParameterKind.Unknown;
            }
            else if (dummy.Kind == AsnTokenKind.ObjectClassReference)
            {
                syntax.CandidateKinds = [AsnParameterKind.ObjectClass, AsnParameterKind.Type];
                syntax.Parameter.Kind = AsnParameterKind.ObjectClass;
            }
            else
            {
                syntax.CandidateKinds = [AsnParameterKind.Type, AsnParameterKind.ObjectClass];
                syntax.Parameter.Kind = AsnParameterKind.Type;
            }

            return syntax;
        }

        if (colon == 0)
        {
            this.AddDiagnostic(206, "Expected a governor before ':'.", tokens[colon]);
            return null;
        }

        AsnToken[] governor = this.CopyArrayRange(tokens, 0, colon);
        syntax.GovernorTokens = governor;
        AsnParameterKind governorKind = this.ClassifyGovernor(governor, preceding, syntax);
        bool lower = this.IsLowerReference(dummy);
        if (governorKind == AsnParameterKind.Type)
        {
            syntax.Parameter.Kind = lower ? AsnParameterKind.Value : AsnParameterKind.ValueSet;
            syntax.CandidateKinds = [syntax.Parameter.Kind];
        }
        else if (governorKind == AsnParameterKind.ObjectClass)
        {
            syntax.Parameter.Kind = lower ? AsnParameterKind.Object : AsnParameterKind.ObjectSet;
            syntax.CandidateKinds = [syntax.Parameter.Kind];
        }
        else
        {
            syntax.Parameter.Kind = AsnParameterKind.Unknown;
            syntax.CandidateKinds = lower
                ? [AsnParameterKind.Value, AsnParameterKind.Object]
                : [AsnParameterKind.ValueSet, AsnParameterKind.ObjectSet];
        }

        return syntax;
    }

    internal AsnParameterKind ClassifyGovernor(
        AsnToken[] governor,
        System.Collections.Generic.List<ParsedParameterSyntax> preceding,
        ParsedParameterSyntax syntax)
    {
        if (governor.Length == 1)
        {
            int index = 0;
            while (index < preceding.Count)
            {
                if (preceding[index].Parameter.Name == governor[0].Text
                    && (preceding[index].Parameter.Kind == AsnParameterKind.Type
                        || preceding[index].Parameter.Kind == AsnParameterKind.ObjectClass))
                {
                    syntax.DummyGovernorName = governor[0].Text;
                    return preceding[index].Parameter.Kind;
                }

                index = index + 1;
            }
        }

        TypeParseResult type = new TypeParser(
            governor,
            this.Diagnostics,
            false).ParseTypeSyntax();
        ObjectClassParseResult objectClass = new ObjectClassParser(
            governor,
            this.Diagnostics,
            false).ParseObjectClassSyntax();
        bool typeSuccess = type.Success && type.ConsumedTokenCount == governor.Length;
        bool classSuccess = objectClass.Success
            && objectClass.ConsumedTokenCount == governor.Length;
        if (this.IsCertainTypeGovernor(governor[0]) && typeSuccess)
        {
            syntax.GovernorTypeSyntax = type.Syntax;
            syntax.Parameter.GovernorType = type.Syntax == null ? null : type.Syntax.Type;
            return AsnParameterKind.Type;
        }

        if (this.IsCertainClassGovernor(governor[0]) && classSuccess)
        {
            syntax.GovernorObjectClassSyntax = objectClass.Syntax;
            syntax.Parameter.GovernorObjectClass = objectClass.Syntax == null
                ? null
                : objectClass.Syntax.ObjectClass;
            return AsnParameterKind.ObjectClass;
        }

        if (typeSuccess && !classSuccess)
        {
            syntax.GovernorTypeSyntax = type.Syntax;
            syntax.Parameter.GovernorType = type.Syntax == null ? null : type.Syntax.Type;
            return AsnParameterKind.Type;
        }

        if (classSuccess && !typeSuccess)
        {
            syntax.GovernorObjectClassSyntax = objectClass.Syntax;
            syntax.Parameter.GovernorObjectClass = objectClass.Syntax == null
                ? null
                : objectClass.Syntax.ObjectClass;
            return AsnParameterKind.ObjectClass;
        }

        if (typeSuccess)
        {
            syntax.GovernorTypeSyntax = type.Syntax;
            syntax.Parameter.GovernorType = type.Syntax == null ? null : type.Syntax.Type;
        }

        return AsnParameterKind.Unknown;
    }

    internal ActualParameterListParseResult ParseActualParameterListSyntax(
        ParsedParameterSyntax[]? formalParameters)
    {
        int start = this.Position;
        this.ParameterRecoveries.Clear();
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(207, "Expected '{' to begin an actual parameter list.", this.PeekToken());
            return this.CreateActualListResult(false, [], start);
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<ParsedActualParameterSyntax> actuals =
            new System.Collections.Generic.List<ParsedActualParameterSyntax>();
        bool success = true;
        int actualOrdinal = 0;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(208, "An actual parameter list requires at least one parameter.", this.PeekToken());
            return this.CreateActualListResult(false, [], start);
        }

        while (this.PeekToken() != null)
        {
            int actualStart = this.Position;
            int actualEnd = this.FindParameterEnd();
            AsnToken[] actualTokens = this.CopyTokenRange(actualStart, actualEnd);
            AsnParameterKind expected = formalParameters != null
                && actualOrdinal < formalParameters.Length
                ? formalParameters[actualOrdinal].Parameter.Kind
                : AsnParameterKind.Unknown;
            ParsedParameterSyntax? formal = formalParameters != null
                && actualOrdinal < formalParameters.Length
                ? formalParameters[actualOrdinal]
                : null;
            ParsedActualParameterSyntax? actual = this.ParseActualParameterTokens(
                actualTokens,
                expected,
                formal);
            this.Position = actualEnd;
            actualOrdinal = actualOrdinal + 1;
            if (actual == null)
            {
                success = false;
                this.AddParameterRecovery("parameter list", actualTokens);
            }
            else
            {
                actuals.Add(actual);
            }
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                AsnToken comma = this.ReadRequiredToken();
                if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
                {
                    this.AddDiagnostic(209, "An actual parameter list cannot have a trailing comma.", comma);
                    success = false;
                    break;
                }

                continue;
            }

            break;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(210, "The actual parameter list is missing its closing brace.", this.PeekToken());
            return this.CreateActualListResult(false, actuals.ToArray(), start);
        }

        this.Position = this.Position + 1;
        if (formalParameters != null && actualOrdinal != formalParameters.Length)
        {
            this.AddDiagnostic(211, "The actual parameter count does not match the formal parameter count.", this.PeekTokenAt(-1));
            return this.CreateActualListResult(false, actuals.ToArray(), start);
        }

        return this.CreateActualListResult(success, actuals.ToArray(), start);
    }

    internal ParsedActualParameterSyntax? ParseActualParameterTokens(
        AsnToken[] tokens,
        AsnParameterKind expected,
        ParsedParameterSyntax? formal)
    {
        if (tokens.Length == 0)
        {
            this.AddDiagnostic(212, "Expected an actual parameter.", this.PeekToken());
            return null;
        }

        if (expected == AsnParameterKind.Unknown)
        {
            return this.ParseUnclassifiedActual(tokens);
        }

        ParsedActualParameterSyntax syntax = new ParsedActualParameterSyntax
        {
            Parameter = new AsnActualParameter { Kind = expected },
            CandidateKinds = [expected],
            SourceTokens = tokens,
            Span = this.CreateSpan(tokens[0], tokens[tokens.Length - 1])
        };
        bool success = false;
        if (expected == AsnParameterKind.Type)
        {
            TypeParseResult parsed = new TypeParser(tokens, this.Diagnostics, false).ParseTypeSyntax();
            success = parsed.Success && parsed.ConsumedTokenCount == tokens.Length;
            syntax.TypeSyntax = parsed.Syntax;
            syntax.Parameter.Type = parsed.Syntax == null ? null : parsed.Syntax.Type;
        }
        else if (expected == AsnParameterKind.Value)
        {
            AsnType? governor = formal == null ? null : formal.Parameter.GovernorType;
            ValueParseResult parsed = new ValueParser(tokens, this.Diagnostics, false).ParseValueSyntax(governor);
            success = parsed.Success && parsed.ConsumedTokenCount == tokens.Length;
            syntax.ValueSyntax = parsed.Syntax;
            syntax.Parameter.Value = parsed.Syntax == null ? null : parsed.Syntax.Value;
        }
        else if (expected == AsnParameterKind.ValueSet)
        {
            success = this.ParseActualValueSet(tokens, syntax, formal);
        }
        else if (expected == AsnParameterKind.ObjectClass)
        {
            ObjectClassParseResult parsed = new ObjectClassParser(
                tokens,
                this.Diagnostics,
                false).ParseObjectClassSyntax();
            success = parsed.Success && parsed.ConsumedTokenCount == tokens.Length;
            syntax.ObjectClassSyntax = parsed.Syntax;
            syntax.Parameter.ObjectClass = parsed.Syntax == null ? null : parsed.Syntax.ObjectClass;
        }
        else if (expected == AsnParameterKind.Object)
        {
            ParsedObjectClassSyntax? governor = formal == null
                ? null
                : formal.GovernorObjectClassSyntax;
            InformationObjectParseResult parsed = new InformationObjectParser(
                tokens,
                this.Diagnostics,
                false).ParseObjectSyntax(governor);
            success = parsed.Success && parsed.ConsumedTokenCount == tokens.Length;
            syntax.ObjectSyntax = parsed.Syntax;
            syntax.Parameter.Object = parsed.Syntax == null ? null : parsed.Syntax.Object;
        }
        else if (expected == AsnParameterKind.ObjectSet)
        {
            ParsedObjectClassSyntax? governor = formal == null
                ? null
                : formal.GovernorObjectClassSyntax;
            InformationObjectSetParseResult parsed = new InformationObjectParser(
                tokens,
                this.Diagnostics,
                false).ParseObjectSetSyntax(governor);
            success = parsed.Success && parsed.ConsumedTokenCount == tokens.Length;
            syntax.ObjectSetSyntax = parsed.Syntax;
            syntax.Parameter.ObjectSet = parsed.Syntax == null ? null : parsed.Syntax.ObjectSet;
        }

        if (!success)
        {
            this.AddDiagnostic(213, "The actual parameter does not match its formal parameter category.", tokens[0]);
            return null;
        }

        return syntax;
    }

    internal bool ParseActualValueSet(
        AsnToken[] tokens,
        ParsedActualParameterSyntax syntax,
        ParsedParameterSyntax? formal)
    {
        if (tokens[0].Kind != AsnTokenKind.LeftBrace
            || tokens[tokens.Length - 1].Kind != AsnTokenKind.RightBrace)
        {
            return false;
        }

        AsnType valueSetType = formal != null && formal.Parameter.GovernorType != null
            ? formal.Parameter.GovernorType
            : new AsnType { Kind = AsnTypeKind.Defined };
        AsnToken[] constraintTokens = this.CreateConstraintTokens(tokens);
        ConstraintParseResult parsed = new ConstraintParser(
            constraintTokens,
            this.Diagnostics,
            false).ParseConstraintSyntax(valueSetType);
        if (!parsed.Success || parsed.ConsumedTokenCount != constraintTokens.Length)
        {
            return false;
        }

        syntax.ValueSetSyntax = parsed.Syntax;
        syntax.Parameter.ValueSetType = new AsnType
        {
            Kind = valueSetType.Kind,
            Name = valueSetType.Name,
            Constraints = parsed.Syntax == null ? [] : [parsed.Syntax.Constraint]
        };
        return true;
    }

    internal ParsedActualParameterSyntax? ParseUnclassifiedActual(AsnToken[] tokens)
    {
        ParsedActualParameterSyntax syntax = new ParsedActualParameterSyntax
        {
            SourceTokens = tokens,
            Span = this.CreateSpan(tokens[0], tokens[tokens.Length - 1])
        };
        TypeParseResult type = new TypeParser(tokens, this.Diagnostics, false).ParseTypeSyntax();
        if (type.Success && type.ConsumedTokenCount == tokens.Length)
        {
            syntax.Parameter.Kind = AsnParameterKind.Type;
            syntax.Parameter.Type = type.Syntax == null ? null : type.Syntax.Type;
            syntax.TypeSyntax = type.Syntax;
            syntax.CandidateKinds = [AsnParameterKind.Type, AsnParameterKind.ObjectClass];
            return syntax;
        }

        ValueParseResult value = new ValueParser(tokens, this.Diagnostics, false).ParseValueSyntax(null);
        if (value.Success && value.ConsumedTokenCount == tokens.Length)
        {
            syntax.Parameter.Kind = AsnParameterKind.Value;
            syntax.Parameter.Value = value.Syntax == null ? null : value.Syntax.Value;
            syntax.ValueSyntax = value.Syntax;
            syntax.CandidateKinds = [AsnParameterKind.Value, AsnParameterKind.Object];
            return syntax;
        }

        if (tokens[0].Kind == AsnTokenKind.LeftBrace
            && tokens[tokens.Length - 1].Kind == AsnTokenKind.RightBrace)
        {
            syntax.CandidateKinds = [
                AsnParameterKind.ValueSet,
                AsnParameterKind.Object,
                AsnParameterKind.ObjectSet
            ];
            return syntax;
        }

        this.AddDiagnostic(212, "Expected an actual parameter.", tokens[0]);
        return null;
    }

    internal ParameterizedReferenceParseResult ParseParameterizedReferenceSyntax(
        AsnParameterKind expectedKind,
        ParsedParameterSyntax[]? formalParameters)
    {
        int start = this.Position;
        AsnToken? first = this.PeekToken();
        if (!this.IsReferenceForKind(first, expectedKind))
        {
            this.AddDiagnostic(214, "Expected a parameterized reference.", first);
            return this.CreateReferenceResult(false, null, start);
        }

        this.Position = this.Position + 1;
        string module = "";
        AsnToken reference = first == null ? new AsnToken() : first;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Dot)
            && this.IsReferenceForKind(this.PeekTokenAt(1), expectedKind))
        {
            module = reference.Text;
            reference = this.PeekTokenAt(1) ?? reference;
            this.Position = this.Position + 2;
        }

        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedParser actualParser = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics);
        ActualParameterListParseResult actuals = actualParser.ParseActualParameterListSyntax(formalParameters);
        this.Position = this.Position + actuals.ConsumedTokenCount;
        if (!actuals.Success)
        {
            return this.CreateReferenceResult(false, null, start);
        }

        AsnActualParameter[] publicActuals = new AsnActualParameter[actuals.Parameters.Length];
        int index = 0;
        while (index < actuals.Parameters.Length)
        {
            publicActuals[index] = actuals.Parameters[index].Parameter;
            index = index + 1;
        }

        ParsedParameterizedReferenceSyntax syntax = new ParsedParameterizedReferenceSyntax
        {
            Kind = expectedKind,
            ReferenceModule = module,
            ReferenceName = reference.Text,
            ActualParameterSyntax = actuals.Parameters,
            ActualParameters = publicActuals,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.CreateReferenceResult(true, syntax, start);
    }

    internal ParameterizedAssignmentParseResult ParseParameterizedAssignmentSyntax(
        AsnAssignmentKind assignmentKind)
    {
        int start = this.Position;
        AsnToken? name = this.PeekToken();
        if (!this.IsAssignmentNameForKind(name, assignmentKind))
        {
            this.AddDiagnostic(215, "The assignment reference does not match the parameterized assignment category.", name);
            return this.CreateAssignmentResult(false, null, start);
        }

        this.Position = this.Position + 1;
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ParameterizedParser listParser = new ParameterizedParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics);
        ParameterListParseResult parameters = listParser.ParseFormalParameterListSyntax();
        this.Position = this.Position + parameters.ConsumedTokenCount;
        if (!parameters.Success)
        {
            return this.CreateAssignmentResult(false, null, start);
        }

        ParsedParameterizedAssignmentSyntax syntax = new ParsedParameterizedAssignmentSyntax
        {
            ParameterSyntax = parameters.Parameters
        };
        AsnParameter[] publicParameters = new AsnParameter[parameters.Parameters.Length];
        int parameterIndex = 0;
        while (parameterIndex < parameters.Parameters.Length)
        {
            publicParameters[parameterIndex] = parameters.Parameters[parameterIndex].Parameter;
            parameterIndex = parameterIndex + 1;
        }

        if (!this.ParseAssignmentGovernor(assignmentKind, syntax))
        {
            return this.CreateAssignmentResult(false, syntax, start);
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Assignment))
        {
            this.AddDiagnostic(216, "Expected '::=' in the parameterized assignment.", this.PeekToken());
            return this.CreateAssignmentResult(false, syntax, start);
        }

        this.Position = this.Position + 1;
        AsnAssignment assignment = new AsnAssignment
        {
            Name = name == null ? "" : name.Text,
            Kind = assignmentKind,
            Parameters = publicParameters
        };
        syntax.Assignment = assignment;
        if (!this.ParseAssignmentRightSide(assignmentKind, syntax))
        {
            return this.CreateAssignmentResult(false, syntax, start);
        }

        assignment.Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1));
        syntax.SourceTokens = this.CopyTokenRange(start, this.Position);
        syntax.Span = assignment.Span;
        return this.CreateAssignmentResult(true, syntax, start);
    }

    internal bool ParseAssignmentGovernor(
        AsnAssignmentKind assignmentKind,
        ParsedParameterizedAssignmentSyntax syntax)
    {
        if (assignmentKind == AsnAssignmentKind.Type
            || assignmentKind == AsnAssignmentKind.ObjectClass)
        {
            return true;
        }

        int end = this.FindAssignmentOperator();
        if (end == this.Position || end >= this.Tokens.Length)
        {
            this.AddDiagnostic(217, "Expected a governor before '::='.", this.PeekToken());
            return false;
        }

        AsnToken[] governor = this.CopyTokenRange(this.Position, end);
        if (assignmentKind == AsnAssignmentKind.Value
            || assignmentKind == AsnAssignmentKind.ValueSet)
        {
            TypeParseResult parsed = new TypeParser(
                governor,
                this.Diagnostics,
                false).ParseTypeSyntax();
            if (!parsed.Success || parsed.Syntax == null
                || parsed.ConsumedTokenCount != governor.Length)
            {
                this.AddDiagnostic(218, "Expected a type governor in the parameterized assignment.", this.PeekToken());
                return false;
            }

            syntax.GovernorTypeSyntax = parsed.Syntax;
            syntax.Assignment.Type = parsed.Syntax.Type;
        }
        else
        {
            ObjectClassParseResult parsed = new ObjectClassParser(
                governor,
                this.Diagnostics,
                false).ParseObjectClassSyntax();
            if (!parsed.Success || parsed.Syntax == null
                || parsed.ConsumedTokenCount != governor.Length)
            {
                this.AddDiagnostic(219, "Expected an object-class governor in the parameterized assignment.", this.PeekToken());
                return false;
            }

            syntax.GovernorObjectClassSyntax = parsed.Syntax;
            syntax.Assignment.ObjectClass = parsed.Syntax.ObjectClass;
        }

        this.Position = end;
        return true;
    }

    internal bool ParseAssignmentRightSide(
        AsnAssignmentKind assignmentKind,
        ParsedParameterizedAssignmentSyntax syntax)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        if (assignmentKind == AsnAssignmentKind.Type)
        {
            TypeParseResult parsed = new TypeParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseTypeSyntax();
            this.Position = this.Position + parsed.ConsumedTokenCount;
            syntax.TypeSyntax = parsed.Syntax;
            syntax.Assignment.Type = parsed.Syntax == null ? null : parsed.Syntax.Type;
            return parsed.Success && parsed.Syntax != null;
        }

        if (assignmentKind == AsnAssignmentKind.Value)
        {
            AsnType? governor = syntax.GovernorTypeSyntax == null
                ? null
                : syntax.GovernorTypeSyntax.Type;
            ValueParseResult parsed = new ValueParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseValueSyntax(governor);
            this.Position = this.Position + parsed.ConsumedTokenCount;
            syntax.ValueSyntax = parsed.Syntax;
            syntax.Assignment.Value = parsed.Syntax == null ? null : parsed.Syntax.Value;
            return parsed.Success && parsed.Syntax != null;
        }

        if (assignmentKind == AsnAssignmentKind.ValueSet)
        {
            return this.ParseAssignmentValueSet(syntax);
        }

        if (assignmentKind == AsnAssignmentKind.ObjectClass)
        {
            ObjectClassParseResult parsed = new ObjectClassParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseObjectClassSyntax();
            this.Position = this.Position + parsed.ConsumedTokenCount;
            syntax.ObjectClassSyntax = parsed.Syntax;
            syntax.Assignment.ObjectClass = parsed.Syntax == null ? null : parsed.Syntax.ObjectClass;
            return parsed.Success && parsed.Syntax != null;
        }

        if (assignmentKind == AsnAssignmentKind.Object)
        {
            InformationObjectParseResult parsed = new InformationObjectParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseObjectSyntax(syntax.GovernorObjectClassSyntax);
            this.Position = this.Position + parsed.ConsumedTokenCount;
            syntax.ObjectSyntax = parsed.Syntax;
            syntax.Assignment.Object = parsed.Syntax == null ? null : parsed.Syntax.Object;
            return parsed.Success && parsed.Syntax != null;
        }

        InformationObjectSetParseResult objectSet = new InformationObjectParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics).ParseObjectSetSyntax(syntax.GovernorObjectClassSyntax);
        this.Position = this.Position + objectSet.ConsumedTokenCount;
        syntax.ObjectSetSyntax = objectSet.Syntax;
        syntax.Assignment.ObjectSet = objectSet.Syntax == null ? null : objectSet.Syntax.ObjectSet;
        return objectSet.Success && objectSet.Syntax != null;
    }

    internal bool ParseAssignmentValueSet(ParsedParameterizedAssignmentSyntax syntax)
    {
        int start = this.Position;
        if (!this.ReadBalancedBraceGroup())
        {
            this.AddDiagnostic(220, "Expected a value set on the right side of the assignment.", this.PeekToken());
            return false;
        }

        syntax.ValueSetTokens = this.CopyTokenRange(start, this.Position);
        AsnToken[] constraintTokens = this.CreateConstraintTokens(syntax.ValueSetTokens);
        AsnType governor = syntax.GovernorTypeSyntax == null
            ? new AsnType { Kind = AsnTypeKind.Defined }
            : syntax.GovernorTypeSyntax.Type;
        ConstraintParseResult parsed = new ConstraintParser(
            constraintTokens,
            this.Diagnostics,
            this.ReportDiagnostics).ParseConstraintSyntax(governor);
        if (!parsed.Success || parsed.ConsumedTokenCount != constraintTokens.Length)
        {
            return false;
        }

        syntax.ValueSetSyntax = parsed.Syntax;
        if (parsed.Syntax != null)
        {
            syntax.Assignment.Type = new AsnType
            {
                Kind = governor.Kind,
                Name = governor.Name,
                Constraints = [parsed.Syntax.Constraint]
            };
        }

        return true;
    }

    internal int FindParameterEnd()
    {
        int braces = 0;
        int parentheses = 0;
        int brackets = 0;
        int index = this.Position;
        while (index < this.Tokens.Length)
        {
            AsnTokenKind kind = this.Tokens[index].Kind;
            if (braces == 0 && parentheses == 0 && brackets == 0
                && (kind == AsnTokenKind.Comma || kind == AsnTokenKind.RightBrace))
            {
                return index;
            }

            if (kind == AsnTokenKind.LeftBrace)
            {
                braces = braces + 1;
            }
            else if (kind == AsnTokenKind.RightBrace && braces > 0)
            {
                braces = braces - 1;
            }
            else if (kind == AsnTokenKind.LeftParenthesis)
            {
                parentheses = parentheses + 1;
            }
            else if (kind == AsnTokenKind.RightParenthesis && parentheses > 0)
            {
                parentheses = parentheses - 1;
            }
            else if (kind == AsnTokenKind.LeftBracket
                || kind == AsnTokenKind.LeftVersionBracket)
            {
                brackets = brackets + 1;
            }
            else if ((kind == AsnTokenKind.RightBracket
                || kind == AsnTokenKind.RightVersionBracket) && brackets > 0)
            {
                brackets = brackets - 1;
            }

            index = index + 1;
        }

        return index;
    }

    internal int FindTopLevelColon(AsnToken[] tokens)
    {
        int braces = 0;
        int parentheses = 0;
        int brackets = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            AsnTokenKind kind = tokens[index].Kind;
            if (braces == 0 && parentheses == 0 && brackets == 0
                && kind == AsnTokenKind.Colon)
            {
                return index;
            }

            if (kind == AsnTokenKind.LeftBrace)
            {
                braces = braces + 1;
            }
            else if (kind == AsnTokenKind.RightBrace && braces > 0)
            {
                braces = braces - 1;
            }
            else if (kind == AsnTokenKind.LeftParenthesis)
            {
                parentheses = parentheses + 1;
            }
            else if (kind == AsnTokenKind.RightParenthesis && parentheses > 0)
            {
                parentheses = parentheses - 1;
            }
            else if (kind == AsnTokenKind.LeftBracket
                || kind == AsnTokenKind.LeftVersionBracket)
            {
                brackets = brackets + 1;
            }
            else if ((kind == AsnTokenKind.RightBracket
                || kind == AsnTokenKind.RightVersionBracket) && brackets > 0)
            {
                brackets = brackets - 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindAssignmentOperator()
    {
        int index = this.Position;
        while (index < this.Tokens.Length)
        {
            if (this.Tokens[index].Kind == AsnTokenKind.Assignment)
            {
                return index;
            }

            index = index + 1;
        }

        return index;
    }

    internal bool ReadBalancedBraceGroup()
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return false;
        }

        int depth = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.ReadRequiredToken();
            if (current.Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBrace)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal AsnToken[] CreateConstraintTokens(AsnToken[] valueSetTokens)
    {
        AsnToken[] result = new AsnToken[valueSetTokens.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnToken source = valueSetTokens[index];
            if (index == 0 || index == result.Length - 1)
            {
                result[index] = new AsnToken
                {
                    Kind = index == 0
                        ? AsnTokenKind.LeftParenthesis
                        : AsnTokenKind.RightParenthesis,
                    Text = index == 0 ? "(" : ")",
                    Span = source.Span
                };
            }
            else
            {
                result[index] = source;
            }

            index = index + 1;
        }

        return result;
    }

    internal bool IsCertainTypeGovernor(AsnToken token)
    {
        return token.Kind == AsnTokenKind.LeftBracket
            || (token.Kind == AsnTokenKind.ReservedWord
                && token.Text != "TYPE-IDENTIFIER"
                && token.Text != "ABSTRACT-SYNTAX");
    }

    internal bool IsCertainClassGovernor(AsnToken token)
    {
        return token.Text == "TYPE-IDENTIFIER"
            || token.Text == "ABSTRACT-SYNTAX"
            || token.Kind == AsnTokenKind.ObjectClassReference;
    }

    internal bool IsAssignmentNameForKind(AsnToken? token, AsnAssignmentKind kind)
    {
        if (kind == AsnAssignmentKind.Value || kind == AsnAssignmentKind.Object)
        {
            return this.IsLowerReference(token);
        }

        return this.IsUpperReference(token);
    }

    internal bool IsReferenceForKind(AsnToken? token, AsnParameterKind kind)
    {
        if (token == null)
        {
            return false;
        }

        if (kind == AsnParameterKind.Value || kind == AsnParameterKind.Object)
        {
            return this.IsLowerReference(token)
                && (token.Kind == AsnTokenKind.Identifier
                    || token.Kind == AsnTokenKind.ValueReference
                    || token.Kind == AsnTokenKind.ObjectReference);
        }

        if (kind == AsnParameterKind.ObjectClass
            && (token.Text == "TYPE-IDENTIFIER" || token.Text == "ABSTRACT-SYNTAX"))
        {
            return true;
        }

        return this.IsUpperReference(token)
            && (token.Kind == AsnTokenKind.TypeReference
                || token.Kind == AsnTokenKind.ModuleReference
                || token.Kind == AsnTokenKind.ObjectClassReference
                || token.Kind == AsnTokenKind.ObjectSetReference);
    }

    internal bool IsReference(AsnToken token)
    {
        return this.IsLowerReference(token) || this.IsUpperReference(token);
    }

    internal bool IsLowerReference(AsnToken? token)
    {
        return token != null
            && token.Text.Length > 0
            && token.Text[0] >= 'a' && token.Text[0] <= 'z';
    }

    internal bool IsUpperReference(AsnToken? token)
    {
        return token != null
            && token.Text.Length > 0
            && token.Text[0] >= 'A' && token.Text[0] <= 'Z';
    }

    internal ParameterListParseResult CreateParameterListResult(
        bool success,
        ParsedParameterSyntax[] parameters,
        int start)
    {
        return new ParameterListParseResult
        {
            Success = success,
            Parameters = parameters,
            Recoveries = this.ParameterRecoveries.ToArray(),
            ConsumedTokenCount = this.Position - start
        };
    }

    internal ActualParameterListParseResult CreateActualListResult(
        bool success,
        ParsedActualParameterSyntax[] parameters,
        int start)
    {
        return new ActualParameterListParseResult
        {
            Success = success,
            Parameters = parameters,
            Recoveries = this.ParameterRecoveries.ToArray(),
            ConsumedTokenCount = this.Position - start
        };
    }

    internal void AddParameterRecovery(string boundary, AsnToken[] skipped)
    {
        this.ParameterRecoveries.Add(new ParsedRecoverySyntax
        {
            Boundary = boundary,
            SkippedTokens = skipped,
            Span = skipped.Length == 0
                ? null
                : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
        });
    }

    internal ParameterizedReferenceParseResult CreateReferenceResult(
        bool success,
        ParsedParameterizedReferenceSyntax? syntax,
        int start)
    {
        return new ParameterizedReferenceParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal ParameterizedAssignmentParseResult CreateAssignmentResult(
        bool success,
        ParsedParameterizedAssignmentSyntax? syntax,
        int start)
    {
        return new ParameterizedAssignmentParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
    }

    internal AsnToken[] CopyTokenRange(int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = this.Tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnToken[] CopyArrayRange(AsnToken[] source, int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
    }

    internal AsnToken? PeekToken()
    {
        return this.PeekTokenAt(0);
    }

    internal AsnToken? PeekTokenAt(int distance)
    {
        int index = this.Position + distance;
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal AsnToken? TokenAt(int index)
    {
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
    }

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan { Start = first.Span.Start, End = last.Span.End };
    }

    internal void AddDiagnostic(int number, string message, AsnToken? token)
    {
        if (!this.ReportDiagnostics)
        {
            return;
        }

        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Syntax,
            number,
            message,
            token == null ? null : token.Span,
            null);
    }
}
