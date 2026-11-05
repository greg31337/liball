namespace asn1;

internal class EcnSyntaxParser
{
    internal AsnToken[] Tokens = [];
    internal int Position;
    internal string ErrorMessage = "";
    internal AsnToken? ErrorToken;

    internal EcnSyntaxParser(AsnToken[] tokens)
    {
        if (tokens == null)
        {
            throw new System.ArgumentNullException("tokens");
        }

        System.Collections.Generic.List<AsnToken> significant =
            new System.Collections.Generic.List<AsnToken>();
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind != AsnTokenKind.Whitespace
                && tokens[index].Kind != AsnTokenKind.Comment
                && tokens[index].Kind != AsnTokenKind.EndOfInput)
            {
                significant.Add(tokens[index]);
            }

            index = index + 1;
        }

        this.Tokens = significant.ToArray();
    }

    internal EcnParseResult ParseModuleSyntax()
    {
        EcnModuleSyntax module = new EcnModuleSyntax();
        if (this.Tokens.Length == 0)
        {
            return this.CreateFailureResult(
                "An ECN module requires a module reference.",
                null);
        }

        AsnToken moduleToken = this.Tokens[this.Position];
        module.Name = moduleToken.Text;
        this.Position = this.Position + 1;
        if (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftBrace)
        {
            if (!this.ConsumeBalancedGroup())
            {
                return this.CreateFailureResult(
                    this.ErrorMessage,
                    this.ErrorToken);
            }
        }

        if (this.Position >= this.Tokens.Length
            || (this.Tokens[this.Position].Text != "ENCODING-DEFINITIONS"
                && this.Tokens[this.Position].Text != "LINK-DEFINITIONS"))
        {
            return this.CreateFailureResult(
                "Expected ENCODING-DEFINITIONS or LINK-DEFINITIONS in the ECN module header.",
                this.PeekToken());
        }

        module.LinkDefinitions =
            this.Tokens[this.Position].Text == "LINK-DEFINITIONS";
        this.Position = this.Position + 1;
        if (!this.ReadRequiredText("::=")
            || !this.ReadRequiredText("BEGIN"))
        {
            return this.CreateFailureResult(
                this.ErrorMessage,
                this.ErrorToken);
        }

        System.Collections.Generic.List<string> exports =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<string> imports =
            new System.Collections.Generic.List<string>();
        System.Collections.Generic.List<EcnImportSyntax> importedModules =
            new System.Collections.Generic.List<EcnImportSyntax>();
        System.Collections.Generic.List<AsnToken> renames =
            new System.Collections.Generic.List<AsnToken>();
        System.Collections.Generic.List<EcnAssignmentSyntax> assignments =
            new System.Collections.Generic.List<EcnAssignmentSyntax>();
        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Text != "END")
        {
            if (this.Tokens[this.Position].Text == "EXPORTS")
            {
                module.ExportsSpecified = true;
                this.Position = this.Position + 1;
                AsnToken[] clause = this.ReadClauseToSemicolon();
                module.ExportsAll = clause.Length == 1
                    && clause[0].Text == "ALL";
                this.AppendReferenceTexts(exports, clause, false);
            }
            else if (this.Tokens[this.Position].Text == "IMPORTS")
            {
                this.Position = this.Position + 1;
                AsnToken[] clause = this.ReadClauseToSemicolon();
                EcnImportSyntax[] groups = this.ParseImportGroups(clause);
                int groupIndex = 0;
                while (groupIndex < groups.Length)
                {
                    importedModules.Add(groups[groupIndex]);
                    int symbolIndex = 0;
                    while (symbolIndex < groups[groupIndex].Symbols.Length)
                    {
                        imports.Add(groups[groupIndex].Symbols[symbolIndex]);
                        symbolIndex = symbolIndex + 1;
                    }

                    groupIndex = groupIndex + 1;
                }
            }
            else if (this.Tokens[this.Position].Text == "RENAMES")
            {
                this.Position = this.Position + 1;
                AsnToken[] clause = this.ReadClauseToSemicolon();
                this.AppendTokensToList(renames, clause);
            }
            else
            {
                EcnAssignmentSyntax? assignment = this.ParseAssignmentSyntax();
                if (assignment == null)
                {
                    return this.CreateFailureResult(
                        this.ErrorMessage,
                        this.ErrorToken);
                }

                assignments.Add(assignment);
            }
        }

        if (this.Position >= this.Tokens.Length
            || this.Tokens[this.Position].Text != "END")
        {
            return this.CreateFailureResult(
                "The ECN module is missing END.",
                this.PeekToken());
        }

        AsnToken endToken = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        if (this.Position != this.Tokens.Length)
        {
            return this.CreateFailureResult(
                "The ECN module contains content after END.",
                this.PeekToken());
        }

        module.Exports = exports.ToArray();
        module.Imports = imports.ToArray();
        module.ImportedModules = importedModules.ToArray();
        module.RenameTokens = renames.ToArray();
        module.Assignments = assignments.ToArray();
        module.Span = this.CreateSpanFromBounds(moduleToken, endToken);
        return new EcnParseResult
        {
            Success = true,
            Module = module,
            Assignments = module.Assignments
        };
    }

    internal EcnParseResult ParseAssignmentListSyntax()
    {
        System.Collections.Generic.List<EcnAssignmentSyntax> assignments =
            new System.Collections.Generic.List<EcnAssignmentSyntax>();
        while (this.Position < this.Tokens.Length)
        {
            EcnAssignmentSyntax? assignment = this.ParseAssignmentSyntax();
            if (assignment == null)
            {
                return this.CreateFailureResult(
                    this.ErrorMessage,
                    this.ErrorToken);
            }

            assignments.Add(assignment);
        }

        return new EcnParseResult
        {
            Success = true,
            Assignments = assignments.ToArray()
        };
    }

    internal EcnAssignmentSyntax? ParseAssignmentSyntax()
    {
        int start = this.Position;
        int assignmentIndex = this.FindAssignmentOperator(start);
        if (assignmentIndex < 0)
        {
            this.SetError(
                "Expected '::=' in the ECN assignment.",
                this.PeekToken());
            return null;
        }

        if (assignmentIndex == start)
        {
            this.SetError(
                "An ECN assignment requires a reference before '::='.",
                this.Tokens[assignmentIndex]);
            return null;
        }

        AsnToken[] head = this.CopyTokenRange(
            start,
            assignmentIndex - start);
        EcnAssignmentSyntax assignment = new EcnAssignmentSyntax
        {
            Reference = head[0].Text,
            HeadTokens = head
        };
        int parameterStart = this.FindParameterStart(head);
        int parameterEnd = parameterStart < 0
            ? -1
            : this.FindParameterEnd(head, parameterStart);
        if (parameterStart >= 0 && parameterEnd < 0)
        {
            this.SetError(
                "The ECN parameter list is not terminated by '>}'.",
                head[parameterStart]);
            return null;
        }

        if (parameterStart >= 0)
        {
            assignment.Parameters = this.ParseParameters(
                head,
                parameterStart + 2,
                parameterEnd - parameterStart - 2);
        }

        int governorStart = parameterEnd >= 0
            ? parameterEnd + 2
            : 1;
        int governorCount = head.Length - governorStart;
        bool objectSet = this.ContainsText(
            head,
            governorStart,
            governorCount,
            "#ENCODINGS");
        if (head[0].Kind == AsnTokenKind.EncodingClassReference)
        {
            assignment.Kind = EcnAssignmentKind.EncodingClass;
            if (governorCount != 0)
            {
                this.SetError(
                    "An encoding class assignment cannot have a governor before '::='.",
                    head[governorStart]);
                return null;
            }
        }
        else if (objectSet)
        {
            assignment.Kind = EcnAssignmentKind.EncodingObjectSet;
            assignment.Governor = "#ENCODINGS";
        }
        else
        {
            assignment.Kind = EcnAssignmentKind.EncodingObject;
            if (governorCount <= 0)
            {
                this.SetError(
                    "An encoding object assignment requires an encoding class governor.",
                    head[0]);
                return null;
            }

            assignment.Governor = this.JoinTokenText(
                head,
                governorStart,
                governorCount);
        }

        this.Position = assignmentIndex + 1;
        int bodyStart = this.Position;
        if (!this.ConsumeAssignmentBody())
        {
            return null;
        }

        assignment.BodyTokens = this.CopyTokenRange(
            bodyStart,
            this.Position - bodyStart);
        if (assignment.BodyTokens.Length == 0)
        {
            this.SetError(
                "An ECN assignment requires a definition after '::='.",
                this.Tokens[assignmentIndex]);
            return null;
        }

        assignment.Clauses = this.ParseBodyClauses(assignment.BodyTokens);
        assignment.Span = this.CreateSpanFromBounds(
            head[0],
            assignment.BodyTokens[assignment.BodyTokens.Length - 1]);
        return assignment;
    }

    internal bool ConsumeAssignmentBody()
    {
        if (this.Position >= this.Tokens.Length)
        {
            this.SetError(
                "The ECN assignment definition is missing.",
                null);
            return false;
        }

        if (this.Tokens[this.Position].Text == "NON-ECN-BEGIN")
        {
            while (this.Position < this.Tokens.Length
                && this.Tokens[this.Position].Text != "NON-ECN-END")
            {
                this.Position = this.Position + 1;
            }

            if (this.Position >= this.Tokens.Length)
            {
                this.SetError(
                    "The NON-ECN definition is missing NON-ECN-END.",
                    this.Tokens[this.Position - 1]);
                return false;
            }

            this.Position = this.Position + 1;
            return true;
        }

        while (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Kind == AsnTokenKind.LeftBracket)
        {
            if (!this.ConsumeBalancedGroup())
            {
                return false;
            }
        }

        if (!this.ConsumeBodyPrimary())
        {
            return false;
        }

        if (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Text == "COMPLETED")
        {
            this.Position = this.Position + 1;
            if (this.Position >= this.Tokens.Length
                || this.Tokens[this.Position].Text != "BY")
            {
                this.SetError(
                    "COMPLETED in an encoding object set must be followed by BY.",
                    this.PeekToken());
                return false;
            }

            this.Position = this.Position + 1;
            return this.ConsumeBodyPrimary();
        }

        return true;
    }

    internal bool ConsumeBodyPrimary()
    {
        if (this.Position >= this.Tokens.Length)
        {
            return false;
        }

        if (this.IsOpeningKind(this.Tokens[this.Position].Kind))
        {
            return this.ConsumeBalancedGroup();
        }

        this.Position = this.Position + 1;
        while (this.Position < this.Tokens.Length
            && this.IsOpeningKind(this.Tokens[this.Position].Kind))
        {
            if (!this.ConsumeBalancedGroup())
            {
                return false;
            }
        }

        return true;
    }

    internal bool ConsumeBalancedGroup()
    {
        if (this.Position >= this.Tokens.Length
            || !this.IsOpeningKind(this.Tokens[this.Position].Kind))
        {
            return false;
        }

        System.Collections.Generic.List<AsnTokenKind> closings =
            new System.Collections.Generic.List<AsnTokenKind>();
        while (this.Position < this.Tokens.Length)
        {
            AsnToken token = this.Tokens[this.Position];
            if (this.IsOpeningKind(token.Kind))
            {
                closings.Add(this.GetClosingKind(token.Kind));
            }
            else if (this.IsClosingKind(token.Kind))
            {
                if (closings.Count == 0
                    || closings[closings.Count - 1] != token.Kind)
                {
                    this.SetError(
                        "The ECN definition contains mismatched delimiters.",
                        token);
                    return false;
                }

                closings.RemoveAt(closings.Count - 1);
                if (closings.Count == 0)
                {
                    this.Position = this.Position + 1;
                    return true;
                }
            }

            this.Position = this.Position + 1;
        }

        this.SetError(
            "The ECN definition contains an unterminated delimiter group.",
            this.Tokens[this.Tokens.Length - 1]);
        return false;
    }

    internal EcnParameterSyntax[] ParseParameters(
        AsnToken[] head,
        int offset,
        int count)
    {
        EcnTokenGroupSyntax[] groups = this.SplitTokenGroups(
            head,
            offset,
            count,
            AsnTokenKind.Comma);
        System.Collections.Generic.List<EcnParameterSyntax> parameters =
            new System.Collections.Generic.List<EcnParameterSyntax>();
        int index = 0;
        while (index < groups.Length)
        {
            AsnToken[] tokens = groups[index].Tokens;
            int colon = this.FindKindAtTopLevel(
                tokens,
                0,
                tokens.Length,
                AsnTokenKind.Colon);
            EcnParameterSyntax parameter = new EcnParameterSyntax
            {
                Tokens = tokens,
                Span = groups[index].Span
            };
            if (colon >= 0)
            {
                parameter.Governor = this.JoinTokenText(
                    tokens,
                    0,
                    colon);
                parameter.Reference = this.JoinTokenText(
                    tokens,
                    colon + 1,
                    tokens.Length - colon - 1);
            }
            else
            {
                parameter.Reference = this.JoinTokenText(
                    tokens,
                    0,
                    tokens.Length);
            }

            parameters.Add(parameter);
            index = index + 1;
        }

        return parameters.ToArray();
    }

    internal EcnClauseSyntax[] ParseBodyClauses(AsnToken[] body)
    {
        System.Collections.Generic.List<EcnClauseSyntax> clauses =
            new System.Collections.Generic.List<EcnClauseSyntax>();
        int index = 0;
        while (index < body.Length)
        {
            AsnToken token = body[index];
            if (token.Kind == AsnTokenKind.NonEcnText)
            {
                clauses.Add(this.CreateClause(
                    EcnClauseKind.NonEcn,
                    "NON-ECN",
                    [token]));
            }

            if (token.Text == "MAPPING")
            {
                AsnToken[] mappingTokens = this.CopyClauseTokens(
                    body,
                    index);
                EcnClauseSyntax mapping = this.CreateClause(
                    EcnClauseKind.Mapping,
                    "MAPPING",
                    mappingTokens);
                mapping.MappingEntries = this.ParseMappingEntries(
                    mappingTokens);
                clauses.Add(mapping);
            }

            if (token.Text == "DETERMINED"
                || token.Text == "USING"
                || token.Text == "USE"
                || token.Text == "USE-SET")
            {
                clauses.Add(this.CreateClause(
                    EcnClauseKind.Link,
                    token.Text,
                    this.CopyClauseTokens(body, index)));
            }

            if (token.Text == "IF" || token.Text == "IF-ALL")
            {
                clauses.Add(this.CreateClause(
                    EcnClauseKind.Condition,
                    token.Text,
                    this.CopyClauseTokens(body, index)));
            }

            if (token.Text == "TRANSFORMS"
                || token.Text == "OPTIONS"
                || token.Text == "ORDERED")
            {
                EcnClauseSyntax ordered = this.CreateClause(
                    EcnClauseKind.OrderedEncodingObjectList,
                    token.Text,
                    this.CopyClauseTokens(body, index));
                ordered.Items = this.ParseOrderedItems(ordered.Tokens);
                clauses.Add(ordered);
            }

            if (token.Text == "STRUCTURE"
                || token.Text == "STRUCTURED")
            {
                clauses.Add(this.CreateClause(
                    EcnClauseKind.EncodingStructure,
                    token.Text,
                    this.CopyClauseTokens(body, index)));
            }

            if (this.IsPropertyKeyword(token.Text))
            {
                clauses.Add(this.CreateClause(
                    EcnClauseKind.Property,
                    token.Text,
                    this.CopyClauseTokens(body, index)));
            }

            if (index + 2 < body.Length
                && this.IsReferenceToken(body[index])
                && body[index + 1].Kind == AsnTokenKind.Dot
                && this.IsReferenceToken(body[index + 2]))
            {
                int pathEnd = index + 3;
                while (pathEnd + 1 < body.Length
                    && body[pathEnd].Kind == AsnTokenKind.Dot
                    && this.IsReferenceToken(body[pathEnd + 1]))
                {
                    pathEnd = pathEnd + 2;
                }

                AsnToken[] pathTokens = this.CopyTokenRangeFrom(
                    body,
                    index,
                    pathEnd - index);
                EcnClauseSyntax path = this.CreateClause(
                    EcnClauseKind.Path,
                    "PATH",
                    pathTokens);
                path.Paths = [this.JoinTokenText(
                    pathTokens,
                    0,
                    pathTokens.Length)];
                clauses.Add(path);
                index = pathEnd - 1;
            }

            index = index + 1;
        }

        return clauses.ToArray();
    }

    internal EcnMappingEntrySyntax[] ParseMappingEntries(AsnToken[] tokens)
    {
        int brace = this.FindKindAtTopLevel(
            tokens,
            0,
            tokens.Length,
            AsnTokenKind.LeftBrace);
        if (brace < 0)
        {
            return [];
        }

        int close = this.FindMatchingInArray(tokens, brace);
        if (close < 0)
        {
            return [];
        }

        EcnTokenGroupSyntax[] groups = this.SplitTokenGroups(
            tokens,
            brace + 1,
            close - brace - 1,
            AsnTokenKind.Comma);
        System.Collections.Generic.List<EcnMappingEntrySyntax> entries =
            new System.Collections.Generic.List<EcnMappingEntrySyntax>();
        int index = 0;
        while (index < groups.Length)
        {
            AsnToken[] group = groups[index].Tokens;
            int to = this.FindTextAtTopLevel(
                group,
                0,
                group.Length,
                "TO");
            EcnMappingEntrySyntax entry = new EcnMappingEntrySyntax
            {
                SourceTokens = to < 0
                    ? group
                    : this.CopyTokenRangeFrom(group, 0, to),
                TargetTokens = to < 0
                    ? []
                    : this.CopyTokenRangeFrom(
                        group,
                        to + 1,
                        group.Length - to - 1),
                Span = groups[index].Span
            };
            entries.Add(entry);
            index = index + 1;
        }

        return entries.ToArray();
    }

    internal EcnTokenGroupSyntax[] ParseOrderedItems(AsnToken[] tokens)
    {
        int brace = this.FindKindAtTopLevel(
            tokens,
            0,
            tokens.Length,
            AsnTokenKind.LeftBrace);
        if (brace < 0)
        {
            return [];
        }

        int close = this.FindMatchingInArray(tokens, brace);
        return close < 0
            ? []
            : this.SplitTokenGroups(
                tokens,
                brace + 1,
                close - brace - 1,
                AsnTokenKind.Comma);
    }

    internal EcnTokenGroupSyntax[] SplitTokenGroups(
        AsnToken[] tokens,
        int offset,
        int count,
        AsnTokenKind separator)
    {
        System.Collections.Generic.List<EcnTokenGroupSyntax> groups =
            new System.Collections.Generic.List<EcnTokenGroupSyntax>();
        int start = offset;
        int end = offset + count;
        int depth = 0;
        int index = offset;
        while (index <= end)
        {
            bool boundary = index == end;
            if (!boundary)
            {
                AsnTokenKind kind = tokens[index].Kind;
                if (this.IsOpeningKind(kind))
                {
                    depth = depth + 1;
                }
                else if (this.IsClosingKind(kind))
                {
                    depth = depth - 1;
                }
                else if (depth == 0 && kind == separator)
                {
                    boundary = true;
                }
            }

            if (boundary)
            {
                int groupCount = index - start;
                if (groupCount > 0)
                {
                    AsnToken[] groupTokens = this.CopyTokenRangeFrom(
                        tokens,
                        start,
                        groupCount);
                    groups.Add(new EcnTokenGroupSyntax
                    {
                        Tokens = groupTokens,
                        Span = this.CreateSpanFromBounds(
                            groupTokens[0],
                            groupTokens[groupTokens.Length - 1])
                    });
                }

                start = index + 1;
            }

            index = index + 1;
        }

        return groups.ToArray();
    }

    internal AsnToken[] CopyClauseTokens(AsnToken[] tokens, int offset)
    {
        int end = offset + 1;
        if (end < tokens.Length
            && this.IsOpeningKind(tokens[end].Kind))
        {
            int close = this.FindMatchingInArray(tokens, end);
            end = close < 0 ? end : close + 1;
        }
        else
        {
            while (end < tokens.Length
                && tokens[end].Kind != AsnTokenKind.Comma
                && !this.IsClosingKind(tokens[end].Kind)
                && !this.IsPropertyKeyword(tokens[end].Text)
                && tokens[end].Text != "WITH")
            {
                if (this.IsOpeningKind(tokens[end].Kind))
                {
                    int close = this.FindMatchingInArray(tokens, end);
                    end = close < 0 ? end + 1 : close + 1;
                }
                else
                {
                    end = end + 1;
                }
            }
        }

        return this.CopyTokenRangeFrom(tokens, offset, end - offset);
    }

    internal EcnClauseSyntax CreateClause(
        EcnClauseKind kind,
        string name,
        AsnToken[] tokens)
    {
        return new EcnClauseSyntax
        {
            Kind = kind,
            Name = name,
            Tokens = tokens,
            Span = tokens.Length == 0
                ? null
                : this.CreateSpanFromBounds(
                    tokens[0],
                    tokens[tokens.Length - 1])
        };
    }

    internal AsnToken[] ReadClauseToSemicolon()
    {
        int start = this.Position;
        int depth = 0;
        while (this.Position < this.Tokens.Length)
        {
            AsnToken token = this.Tokens[this.Position];
            if (depth == 0 && token.Kind == AsnTokenKind.Semicolon)
            {
                AsnToken[] result = this.CopyTokenRange(
                    start,
                    this.Position - start);
                this.Position = this.Position + 1;
                return result;
            }

            if (this.IsOpeningKind(token.Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingKind(token.Kind))
            {
                depth = depth - 1;
            }

            this.Position = this.Position + 1;
        }

        this.SetError(
            "The ECN module clause is missing a semicolon.",
            this.Tokens[this.Tokens.Length - 1]);
        return [];
    }

    internal int FindAssignmentOperator(int offset)
    {
        int depth = 0;
        int index = offset;
        while (index < this.Tokens.Length)
        {
            AsnToken token = this.Tokens[index];
            if (depth == 0 && token.Kind == AsnTokenKind.Assignment)
            {
                return index;
            }

            if (depth == 0 && token.Text == "END")
            {
                return -1;
            }

            if (this.IsOpeningKind(token.Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingKind(token.Kind))
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindParameterStart(AsnToken[] head)
    {
        int index = 1;
        while (index + 1 < head.Length)
        {
            if (head[index].Kind == AsnTokenKind.LeftBrace
                && head[index + 1].Kind == AsnTokenKind.LessThan)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindParameterEnd(AsnToken[] head, int start)
    {
        int depth = 0;
        int index = start + 2;
        while (index + 1 < head.Length)
        {
            if (this.IsOpeningKind(head[index].Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingKind(head[index].Kind))
            {
                if (depth > 0)
                {
                    depth = depth - 1;
                }
            }

            if (depth == 0
                && head[index].Kind == AsnTokenKind.GreaterThan
                && head[index + 1].Kind == AsnTokenKind.RightBrace)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindKindAtTopLevel(
        AsnToken[] tokens,
        int offset,
        int count,
        AsnTokenKind kind)
    {
        int depth = 0;
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            if (depth == 0 && tokens[index].Kind == kind)
            {
                return index;
            }

            if (this.IsOpeningKind(tokens[index].Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingKind(tokens[index].Kind))
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindTextAtTopLevel(
        AsnToken[] tokens,
        int offset,
        int count,
        string text)
    {
        int depth = 0;
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            if (depth == 0 && tokens[index].Text == text)
            {
                return index;
            }

            if (this.IsOpeningKind(tokens[index].Kind))
            {
                depth = depth + 1;
            }
            else if (this.IsClosingKind(tokens[index].Kind))
            {
                depth = depth - 1;
            }

            index = index + 1;
        }

        return -1;
    }

    internal int FindMatchingInArray(AsnToken[] tokens, int opening)
    {
        AsnTokenKind expected = this.GetClosingKind(tokens[opening].Kind);
        int depth = 1;
        int index = opening + 1;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == tokens[opening].Kind)
            {
                depth = depth + 1;
            }
            else if (tokens[index].Kind == expected)
            {
                depth = depth - 1;
                if (depth == 0)
                {
                    return index;
                }
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool ContainsText(
        AsnToken[] tokens,
        int offset,
        int count,
        string text)
    {
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            if (tokens[index].Text == text)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool IsPropertyKeyword(string text)
    {
        return text == "ALIGNMENT"
            || text == "ALIGNED"
            || text == "ALTERNATIVE"
            || text == "ALPHABET"
            || text == "BIT-ORDER"
            || text == "BIT-REVERSAL"
            || text == "BYTE-ORDER"
            || text == "ENCODING-SPACE"
            || text == "FALSE-PATTERN"
            || text == "JUSTIFIED"
            || text == "MULTIPLE"
            || text == "OPTIONAL-ENCODING"
            || text == "PAD-PATTERN"
            || text == "PADDING"
            || text == "PATTERN"
            || text == "POST-PADDING"
            || text == "PRE-PADDING"
            || text == "PRESENCE"
            || text == "RANGE"
            || text == "REPRESENTATION"
            || text == "REPETITION-ENCODING"
            || text == "REPETITION-SPACE"
            || text == "SIGNEDNESS"
            || text == "SIZE"
            || text == "TERMINATION"
            || text == "TRUE-PATTERN"
            || text == "UNUSED"
            || text == "UNUSED-BITS"
            || text == "VALUE-PADDING"
            || text == "VALUE-REVERSAL"
            || text == "WORD-ORDER";
    }

    internal bool IsReferenceToken(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference
            || token.Kind == AsnTokenKind.TypeReference
            || token.Kind == AsnTokenKind.ModuleReference
            || token.Kind == AsnTokenKind.ObjectReference
            || token.Kind == AsnTokenKind.ObjectSetReference
            || token.Kind == AsnTokenKind.ObjectClassReference
            || token.Kind == AsnTokenKind.EncodingClassReference;
    }

    internal bool IsOpeningKind(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.LeftBrace
            || kind == AsnTokenKind.LeftParenthesis
            || kind == AsnTokenKind.LeftBracket
            || kind == AsnTokenKind.LeftVersionBracket
            || kind == AsnTokenKind.LessThan;
    }

    internal bool IsClosingKind(AsnTokenKind kind)
    {
        return kind == AsnTokenKind.RightBrace
            || kind == AsnTokenKind.RightParenthesis
            || kind == AsnTokenKind.RightBracket
            || kind == AsnTokenKind.RightVersionBracket
            || kind == AsnTokenKind.GreaterThan;
    }

    internal AsnTokenKind GetClosingKind(AsnTokenKind kind)
    {
        if (kind == AsnTokenKind.LeftBrace)
        {
            return AsnTokenKind.RightBrace;
        }

        if (kind == AsnTokenKind.LeftParenthesis)
        {
            return AsnTokenKind.RightParenthesis;
        }

        if (kind == AsnTokenKind.LeftBracket)
        {
            return AsnTokenKind.RightBracket;
        }

        if (kind == AsnTokenKind.LeftVersionBracket)
        {
            return AsnTokenKind.RightVersionBracket;
        }

        return AsnTokenKind.GreaterThan;
    }

    internal bool ReadRequiredText(string text)
    {
        if (this.Position < this.Tokens.Length
            && this.Tokens[this.Position].Text == text)
        {
            this.Position = this.Position + 1;
            return true;
        }

        this.SetError(
            "Expected '" + text + "' in the ECN module.",
            this.PeekToken());
        return false;
    }

    internal void AppendReferenceTexts(
        System.Collections.Generic.List<string> destination,
        AsnToken[] tokens,
        bool stopAtFrom)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (stopAtFrom && tokens[index].Text == "FROM")
            {
                while (index < tokens.Length
                    && tokens[index].Kind != AsnTokenKind.Comma)
                {
                    index = index + 1;
                }
            }
            else if (this.IsReferenceToken(tokens[index]))
            {
                destination.Add(tokens[index].Text);
            }

            index = index + 1;
        }
    }

    internal EcnImportSyntax[] ParseImportGroups(AsnToken[] tokens)
    {
        System.Collections.Generic.List<EcnImportSyntax> imports =
            new System.Collections.Generic.List<EcnImportSyntax>();
        int start = 0;
        while (start < tokens.Length)
        {
            int from = this.FindTextAtTopLevel(
                tokens,
                start,
                tokens.Length - start,
                "FROM");
            if (from < 0 || from + 1 >= tokens.Length)
            {
                break;
            }

            int moduleIndex = from + 1;
            int end = moduleIndex + 1;
            if (end < tokens.Length
                && tokens[end].Kind == AsnTokenKind.LeftBrace)
            {
                int close = this.FindMatchingInArray(tokens, end);
                end = close < 0 ? tokens.Length : close + 1;
            }

            EcnTokenGroupSyntax[] symbolGroups = this.SplitTokenGroups(
                tokens,
                start,
                from - start,
                AsnTokenKind.Comma);
            System.Collections.Generic.List<string> symbols =
                new System.Collections.Generic.List<string>();
            int symbolIndex = 0;
            while (symbolIndex < symbolGroups.Length)
            {
                AsnToken[] symbolTokens = symbolGroups[symbolIndex].Tokens;
                if (symbolTokens.Length > 0)
                {
                    symbols.Add(symbolTokens[0].Text);
                }

                symbolIndex = symbolIndex + 1;
            }

            imports.Add(new EcnImportSyntax
            {
                ModuleName = tokens[moduleIndex].Text,
                Symbols = symbols.ToArray(),
                Span = this.CreateSpanFromBounds(
                    tokens[start],
                    tokens[end - 1])
            });
            start = end;
        }

        return imports.ToArray();
    }

    internal void AppendTokensToList(
        System.Collections.Generic.List<AsnToken> destination,
        AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            destination.Add(tokens[index]);
            index = index + 1;
        }
    }

    internal string JoinTokenText(
        AsnToken[] tokens,
        int offset,
        int count)
    {
        string result = "";
        int index = offset;
        int end = offset + count;
        while (index < end)
        {
            result = result + tokens[index].Text;
            index = index + 1;
        }

        return result;
    }

    internal AsnToken[] CopyTokenRange(int offset, int count)
    {
        return this.CopyTokenRangeFrom(this.Tokens, offset, count);
    }

    internal AsnToken[] CopyTokenRangeFrom(
        AsnToken[] tokens,
        int offset,
        int count)
    {
        AsnToken[] result = new AsnToken[count];
        int index = 0;
        while (index < count)
        {
            result[index] = tokens[offset + index];
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CreateSpanFromBounds(
        AsnToken first,
        AsnToken last)
    {
        return first.Span == null || last.Span == null
            ? null
            : new AsnSourceSpan
            {
                Start = first.Span.Start,
                End = last.Span.End
            };
    }

    internal AsnToken? PeekToken()
    {
        return this.Position < this.Tokens.Length
            ? this.Tokens[this.Position]
            : null;
    }

    internal void SetError(string message, AsnToken? token)
    {
        if (this.ErrorMessage.Length == 0)
        {
            this.ErrorMessage = message;
            this.ErrorToken = token;
        }
    }

    internal EcnParseResult CreateFailureResult(
        string message,
        AsnToken? token)
    {
        return new EcnParseResult
        {
            ErrorMessage = message,
            ErrorToken = token
        };
    }
}
