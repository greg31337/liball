namespace asn1;

internal class ConstraintParser
{
    internal AsnToken[] Tokens;
    internal DiagnosticBag Diagnostics;
    internal bool ReportDiagnostics;
    internal int Position;
    internal AsnType ParentType = new AsnType();
    internal System.Collections.Generic.List<ParsedSubtypeElementSyntax> ParsedElements =
        new System.Collections.Generic.List<ParsedSubtypeElementSyntax>();
    internal System.Collections.Generic.List<ParsedGeneralConstraintSyntax> ParsedGeneralElements =
        new System.Collections.Generic.List<ParsedGeneralConstraintSyntax>();
    internal AsnConstraintElement? PartialRoot;
    internal AsnConstraintElement? PartialAdditional;
    internal AsnConstraintElement? PartialException;
    internal ParsedTypeSyntax? PartialExceptionType;
    internal ParsedValueSyntax? PartialExceptionValue;
    internal bool PartialExtensible;

    internal ConstraintParser(AsnToken[] tokens, DiagnosticBag diagnostics, bool reportDiagnostics)
    {
        this.Tokens = tokens == null ? throw new System.ArgumentNullException("tokens") : tokens;
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.ReportDiagnostics = reportDiagnostics;
    }

    internal ConstraintParseResult ParseConstraintSyntax(AsnType parentType)
    {
        int start = this.Position;
        this.ParentType = parentType == null
            ? throw new System.ArgumentNullException("parentType")
            : parentType;
        this.ParsedElements.Clear();
        this.ParsedGeneralElements.Clear();
        this.PartialRoot = null;
        this.PartialAdditional = null;
        this.PartialException = null;
        this.PartialExceptionType = null;
        this.PartialExceptionValue = null;
        this.PartialExtensible = false;
        AsnToken? left = this.PeekToken();
        if (!this.IsKind(left, AsnTokenKind.LeftParenthesis))
        {
            this.AddDiagnostic(110, "Expected '(' to begin an ASN.1 constraint.", left);
            return this.RecoverConstraintFailure(start, left);
        }

        this.Position = this.Position + 1;
        AsnConstraintElement? root = this.ParseElementSetExpression();
        if (root == null)
        {
            this.AddDiagnostic(111, "Expected an element-set expression in the constraint.", this.PeekToken());
            return this.RecoverConstraintFailure(start, left);
        }

        this.PartialRoot = root;

        bool extensible = false;
        AsnConstraintElement? additional = null;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
        {
            if (this.IsGeneralConstraintKind(root.Kind))
            {
                this.AddDiagnostic(153, "An extension marker is not valid on a general constraint.", this.PeekToken());
                return this.RecoverConstraintFailure(start, left);
            }

            AsnToken? marker = this.PeekTokenAt(1);
            if (!this.IsKind(marker, AsnTokenKind.Ellipsis))
            {
                this.AddDiagnostic(112, "Expected an extension marker after ','.", marker);
                return this.RecoverConstraintFailure(start, left);
            }

            extensible = true;
            this.PartialExtensible = true;
            this.Position = this.Position + 2;
            if (this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.Position = this.Position + 1;
                additional = this.ParseElementSetExpression();
                if (additional == null)
                {
                    this.AddDiagnostic(
                        113,
                        "Expected an additional element-set expression after the extension marker.",
                        this.PeekToken());
                    return this.RecoverConstraintFailure(start, left);
                }

                this.PartialAdditional = additional;
            }
        }

        ParsedTypeSyntax? exceptionType = null;
        ParsedValueSyntax? exceptionValue = null;
        AsnConstraintElement? exceptionElement = null;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.ExclamationMark))
        {
            this.Position = this.Position + 1;
            ExceptionParseResult exception = this.ParseExceptionIdentification();
            if (!exception.Success || exception.ValueSyntax == null)
            {
                this.AddDiagnostic(114, "Expected an exception identification after '!'.", this.PeekToken());
                return this.RecoverConstraintFailure(start, left);
            }

            exceptionType = exception.TypeSyntax;
            exceptionValue = exception.ValueSyntax;
            exceptionElement = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.Exception,
                Value = exception.ValueSyntax.Value
            };
            this.PartialExceptionType = exceptionType;
            this.PartialExceptionValue = exceptionValue;
            this.PartialException = exceptionElement;
        }

        AsnToken? right = this.PeekToken();
        if (!this.IsKind(right, AsnTokenKind.RightParenthesis))
        {
            this.AddDiagnostic(115, "Expected ')' to close the ASN.1 constraint.", right);
            return this.RecoverConstraintFailure(start, left);
        }

        this.Position = this.Position + 1;
        System.Collections.Generic.List<AsnConstraintElement> elements =
            new System.Collections.Generic.List<AsnConstraintElement>();
        if (root.Kind == AsnConstraintKind.Union
            || root.Kind == AsnConstraintKind.Intersection
            || root.Kind == AsnConstraintKind.Except
            || root.Kind == AsnConstraintKind.AllExcept)
        {
            elements.AddRange(root.Children);
        }
        else
        {
            elements.Add(root);
        }
        if (extensible)
        {
            AsnConstraintElement extension = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.Extension
            };
            if (additional != null)
            {
                extension.Children = [additional];
            }

            elements.Add(extension);
        }

        if (exceptionElement != null)
        {
            elements.Add(exceptionElement);
        }

        AsnSourceSpan? span = this.CreateSpan(left, right);
        AsnConstraint constraint = new AsnConstraint
        {
            Kind = root.Kind,
            Elements = elements.ToArray(),
            Extensible = extensible,
            Span = span
        };
        this.ApplyRetainedConstraintMetadata(
            constraint,
            root,
            exceptionType,
            exceptionValue);
        ParsedConstraintSyntax syntax = new ParsedConstraintSyntax
        {
            Constraint = constraint,
            RootElement = root,
            AdditionalElement = additional,
            ExceptionElement = exceptionElement,
            ExceptionTypeSyntax = exceptionType,
            ExceptionValueSyntax = exceptionValue,
            SubtypeElements = this.ParsedElements.ToArray(),
            GeneralConstraints = this.ParsedGeneralElements.ToArray(),
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = span
        };
        return this.CreateResult(true, syntax, start);
    }

    internal ConstraintParseResult ParseSizeConstraintSyntax(AsnType parentType)
    {
        int start = this.Position;
        this.ParentType = parentType == null
            ? throw new System.ArgumentNullException("parentType")
            : parentType;
        this.ParsedElements.Clear();
        this.ParsedGeneralElements.Clear();
        AsnToken? first = this.PeekToken();
        if (!this.IsText(first, "SIZE"))
        {
            this.AddDiagnostic(110, "Expected SIZE to begin a collection constraint.", first);
            return this.CreateResult(false, null, start);
        }

        AsnConstraintElement? root = this.ParseNestedConstraintElement(
            AsnConstraintKind.Size);
        if (root == null)
        {
            return this.CreateResult(false, null, start);
        }

        AsnToken? last = this.PeekTokenAt(-1);
        AsnSourceSpan? span = this.CreateSpan(first, last);
        AsnConstraint constraint = new AsnConstraint
        {
            Kind = AsnConstraintKind.Size,
            Elements = [root],
            Span = span
        };
        ParsedConstraintSyntax syntax = new ParsedConstraintSyntax
        {
            Constraint = constraint,
            RootElement = root,
            SubtypeElements = this.ParsedElements.ToArray(),
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = span
        };
        return this.CreateResult(true, syntax, start);
    }

    internal ConstraintParseResult RecoverConstraintFailure(int start, AsnToken? left)
    {
        int recoveryStart = this.Position;
        int depth = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.ReadRequiredToken();
            if (current.Kind == AsnTokenKind.LeftParenthesis)
            {
                depth = depth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightParenthesis)
            {
                if (depth == 0)
                {
                    break;
                }

                depth = depth - 1;
            }
        }

        System.Collections.Generic.List<AsnConstraintElement> elements =
            new System.Collections.Generic.List<AsnConstraintElement>();
        if (this.PartialRoot != null)
        {
            elements.Add(this.PartialRoot);
        }

        if (this.PartialExtensible)
        {
            AsnConstraintElement extension = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.Extension,
                Children = this.PartialAdditional == null ? [] : [this.PartialAdditional]
            };
            elements.Add(extension);
        }

        if (this.PartialException != null)
        {
            elements.Add(this.PartialException);
        }

        AsnToken[] skipped = this.CopyTokenRange(recoveryStart, this.Position);
        AsnSourceSpan? span = this.CreateSpan(left, this.PeekTokenAt(-1));
        AsnConstraint recoveredConstraint = new AsnConstraint
        {
            Kind = this.PartialRoot == null
                ? AsnConstraintKind.Unknown
                : this.PartialRoot.Kind,
            Elements = elements.ToArray(),
            Extensible = this.PartialExtensible,
            Span = span
        };
        this.ApplyRetainedConstraintMetadata(
            recoveredConstraint,
            this.PartialRoot,
            this.PartialExceptionType,
            this.PartialExceptionValue);
        ParsedConstraintSyntax syntax = new ParsedConstraintSyntax
        {
            Constraint = recoveredConstraint,
            RootElement = this.PartialRoot,
            AdditionalElement = this.PartialAdditional,
            ExceptionElement = this.PartialException,
            ExceptionTypeSyntax = this.PartialExceptionType,
            ExceptionValueSyntax = this.PartialExceptionValue,
            SubtypeElements = this.ParsedElements.ToArray(),
            GeneralConstraints = this.ParsedGeneralElements.ToArray(),
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Recoveries = [new ParsedRecoverySyntax
            {
                Boundary = "constraint",
                SkippedTokens = skipped,
                Span = skipped.Length == 0
                    ? null
                    : this.CreateSpan(skipped[0], skipped[skipped.Length - 1])
            }],
            Span = span
        };
        return this.CreateResult(false, syntax, start);
    }

    internal void ApplyRetainedConstraintMetadata(
        AsnConstraint constraint,
        AsnConstraintElement? root,
        ParsedTypeSyntax? exceptionType,
        ParsedValueSyntax? exceptionValue)
    {
        if (root != null && root.Kind == AsnConstraintKind.UserDefined)
        {
            constraint.UserDefinedBody = root.Text;
            string[] parameters = new string[root.Children.Length];
            int index = 0;
            while (index < root.Children.Length)
            {
                parameters[index] = root.Children[index].Text;
                index = index + 1;
            }

            constraint.UserDefinedParameters = parameters;
            constraint.RequiresApplicationValidation = true;
        }

        if (exceptionValue != null)
        {
            constraint.HasException = true;
            constraint.ExceptionType = exceptionType == null
                ? new AsnType { Kind = AsnTypeKind.Integer }
                : exceptionType.Type;
            constraint.ExceptionValue = exceptionValue.Value;
            if (exceptionValue.ReferenceName.Length > 0)
            {
                constraint.ExceptionIdentifier = exceptionValue.ReferenceModule.Length == 0
                    ? exceptionValue.ReferenceName
                    : exceptionValue.ReferenceModule + "." + exceptionValue.ReferenceName;
            }
            else if (exceptionValue.Value.HasIntegerValue)
            {
                constraint.ExceptionIdentifier = exceptionValue.Value.IntegerValue.ToString(
                    System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                constraint.ExceptionIdentifier = exceptionValue.Value.TextValue;
            }
        }
    }

    internal AsnConstraintElement? ParseUnionExpression()
    {
        AsnConstraintElement? left = this.ParseIntersectionExpression();
        if (left == null)
        {
            return null;
        }

        while (this.IsUnionMark(this.PeekToken()))
        {
            if (this.IsGeneralConstraintKind(left.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", this.PeekToken());
                return null;
            }

            AsnToken mark = this.ReadRequiredToken();
            AsnConstraintElement? right = this.ParseIntersectionExpression();
            if (right == null)
            {
                this.AddDiagnostic(116, "Expected an element after '" + mark.Text + "'.", this.PeekToken());
                return null;
            }

            if (this.IsGeneralConstraintKind(right.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", mark);
                return null;
            }

            left = this.CombineOperation(AsnConstraintKind.Union, left, right);
        }

        return left;
    }

    internal AsnConstraintElement? ParseElementSetExpression()
    {
        if (!this.IsText(this.PeekToken(), "ALL"))
        {
            return this.ParseUnionExpression();
        }

        this.Position = this.Position + 1;
        if (!this.IsText(this.PeekToken(), "EXCEPT"))
        {
            this.AddDiagnostic(119, "Expected EXCEPT after ALL.", this.PeekToken());
            return null;
        }

        this.Position = this.Position + 1;
        AsnConstraintElement? excluded = this.ParseElement();
        if (excluded == null)
        {
            this.AddDiagnostic(120, "Expected an element after ALL EXCEPT.", this.PeekToken());
            return null;
        }

        return new AsnConstraintElement
        {
            Kind = AsnConstraintKind.AllExcept,
            Children = [excluded]
        };
    }

    internal AsnConstraintElement? ParseIntersectionExpression()
    {
        AsnConstraintElement? left = this.ParseExclusionExpression();
        if (left == null)
        {
            return null;
        }

        while (this.IsIntersectionMark(this.PeekToken()))
        {
            if (this.IsGeneralConstraintKind(left.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", this.PeekToken());
                return null;
            }

            AsnToken mark = this.ReadRequiredToken();
            AsnConstraintElement? right = this.ParseExclusionExpression();
            if (right == null)
            {
                this.AddDiagnostic(117, "Expected an element after '" + mark.Text + "'.", this.PeekToken());
                return null;
            }

            if (this.IsGeneralConstraintKind(right.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", mark);
                return null;
            }

            left = this.CombineOperation(AsnConstraintKind.Intersection, left, right);
        }

        return left;
    }

    internal AsnConstraintElement? ParseExclusionExpression()
    {
        AsnConstraintElement? left = this.ParseElement();
        if (left == null)
        {
            return null;
        }

        if (this.IsText(this.PeekToken(), "EXCEPT"))
        {
            if (this.IsGeneralConstraintKind(left.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + 1;
            AsnConstraintElement? right = this.ParseElement();
            if (right == null)
            {
                this.AddDiagnostic(118, "Expected an element after EXCEPT.", this.PeekToken());
                return null;
            }

            if (this.IsGeneralConstraintKind(right.Kind))
            {
                this.AddDiagnostic(152, "A general constraint cannot be used in element-set arithmetic.", this.PeekTokenAt(-1));
                return null;
            }

            return new AsnConstraintElement
            {
                Kind = AsnConstraintKind.Except,
                Children = [left, right]
            };
        }

        return left;
    }

    internal AsnConstraintElement? ParseElement()
    {
        AsnToken? current = this.PeekToken();
        if (this.IsKind(current, AsnTokenKind.LeftParenthesis))
        {
            this.Position = this.Position + 1;
            AsnConstraintElement? nested = this.ParseElementSetExpression();
            if (nested == null)
            {
                this.AddDiagnostic(121, "Expected an element-set expression after '('.", this.PeekToken());
                return null;
            }

            if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightParenthesis))
            {
                this.AddDiagnostic(122, "Expected ')' to close the nested element set.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + 1;
            return nested;
        }

        if (this.IsElementTerminator(current))
        {
            return null;
        }

        if (this.IsText(current, "CONSTRAINED"))
        {
            return this.ParseUserDefinedConstraintElement();
        }

        if (this.IsText(current, "CONTAINING") || this.IsText(current, "ENCODED"))
        {
            return this.ParseContentsConstraintElement();
        }

        if (this.IsKind(current, AsnTokenKind.LeftBrace) && this.IsTableConstraintStart())
        {
            return this.ParseTableConstraintElement();
        }

        if (this.IsText(current, "INCLUDES"))
        {
            return this.ParseTypeElement(true);
        }

        if (this.IsText(current, "SIZE"))
        {
            return this.ParseNestedConstraintElement(AsnConstraintKind.Size);
        }

        if (this.IsText(current, "FROM"))
        {
            return this.ParseNestedConstraintElement(AsnConstraintKind.PermittedAlphabet);
        }

        if (this.IsText(current, "PATTERN"))
        {
            return this.ParsePatternElement();
        }

        if (this.IsText(current, "SETTINGS"))
        {
            return this.ParseSettingsElement();
        }

        if (this.IsText(current, "WITH"))
        {
            return this.ParseInnerTypeElement();
        }

        if (!this.IsText(current, "NULL") || this.ParentType.Kind == AsnTypeKind.OpenType)
        {
            AsnConstraintElement? typeElement = this.ParsePossibleTypeElement();
            if (typeElement != null)
            {
                return typeElement;
            }
        }

        return this.ParseValueOrRangeElement();
    }

    internal AsnConstraintElement? ParseUserDefinedConstraintElement()
    {
        int start = this.Position;
        this.Position = this.Position + 1;
        if (!this.IsText(this.PeekToken(), "BY"))
        {
            this.AddDiagnostic(138, "Expected BY after CONSTRAINED.", this.PeekToken());
            return null;
        }

        this.Position = this.Position + 1;
        TokenGroupParseResult group = this.ParseBraceGroup();
        if (!group.Success)
        {
            this.AddDiagnostic(139, "Expected a brace-delimited user-defined constraint body.", this.PeekToken());
            return null;
        }

        AsnToken[][]? parameters = this.SplitUserDefinedParameters(group.Tokens);
        if (parameters == null)
        {
            this.AddDiagnostic(140, "User-defined constraint parameters must be comma-separated.", this.PeekTokenAt(-1));
            return null;
        }

        AsnConstraintElement[] children = new AsnConstraintElement[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            children[index] = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.UserDefined,
                Text = this.JoinTokenText(parameters[index])
            };
            index = index + 1;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.UserDefined,
            Text = this.JoinTokenText(group.Tokens),
            Children = children
        };
        ParsedGeneralConstraintSyntax syntax = new ParsedGeneralConstraintSyntax
        {
            Element = element,
            BodyTokens = group.Tokens,
            Parameters = parameters,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddGeneralElement(syntax);
    }

    internal AsnConstraintElement? ParseTableConstraintElement()
    {
        int start = this.Position;
        TokenGroupParseResult objectSet = this.ParseBraceGroup();
        if (!objectSet.Success || objectSet.Tokens.Length == 0)
        {
            this.AddDiagnostic(141, "Expected an object set in the table constraint.", this.PeekToken());
            return null;
        }

        AsnConstraintKind kind = AsnConstraintKind.SimpleTable;
        ParsedAtNotationSyntax[] atNotations = [];
        if (this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            TokenGroupParseResult relation = this.ParseBraceGroup();
            if (!relation.Success)
            {
                this.AddDiagnostic(142, "Expected a brace-delimited at-notation list.", this.PeekToken());
                return null;
            }

            AtNotationListParseResult parsedAt = this.ParseAtNotationList(relation.Tokens);
            if (!parsedAt.Success)
            {
                return null;
            }

            kind = AsnConstraintKind.ComponentRelation;
            atNotations = parsedAt.Items;
        }

        AsnConstraintElement[] children = new AsnConstraintElement[atNotations.Length];
        int index = 0;
        while (index < atNotations.Length)
        {
            ParsedAtNotationSyntax notation = atNotations[index];
            children[index] = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.ComponentRelation,
                Name = this.JoinIdentifiers(notation.ComponentIdentifiers),
                Text = this.JoinTokenText(notation.SourceTokens)
            };
            index = index + 1;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = kind,
            Name = this.JoinTokenText(objectSet.Tokens),
            Children = children
        };
        ParsedGeneralConstraintSyntax syntax = new ParsedGeneralConstraintSyntax
        {
            Element = element,
            ObjectSetTokens = objectSet.Tokens,
            AtNotations = atNotations,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddGeneralElement(syntax);
    }

    internal AsnConstraintElement? ParseContentsConstraintElement()
    {
        int start = this.Position;
        ParsedTypeSyntax? containedType = null;
        ParsedValueSyntax? encodingValue = null;
        if (this.IsText(this.PeekToken(), "CONTAINING"))
        {
            this.Position = this.Position + 1;
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            TypeParseResult parsedType = new TypeParser(
                remaining,
                this.Diagnostics,
                false).ParseTypeSyntax();
            if (!parsedType.Success
                || parsedType.Syntax == null
                || parsedType.ConsumedTokenCount == 0)
            {
                this.AddDiagnostic(143, "Expected a type after CONTAINING.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + parsedType.ConsumedTokenCount;
            containedType = parsedType.Syntax;
        }

        if (this.IsText(this.PeekToken(), "ENCODED"))
        {
            this.Position = this.Position + 1;
            if (!this.IsText(this.PeekToken(), "BY"))
            {
                this.AddDiagnostic(144, "Expected BY after ENCODED.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + 1;
            AsnType objectIdentifierType = new AsnType
            {
                Kind = AsnTypeKind.ObjectIdentifier
            };
            ValueParseResult parsedValue = this.ParseValueWithType(objectIdentifierType);
            if (!parsedValue.Success || parsedValue.Syntax == null)
            {
                this.AddDiagnostic(145, "Expected an object identifier value after ENCODED BY.", this.PeekToken());
                return null;
            }

            encodingValue = parsedValue.Syntax;
        }

        if (containedType == null && encodingValue == null)
        {
            this.AddDiagnostic(146, "Expected CONTAINING or ENCODED BY contents syntax.", this.PeekToken());
            return null;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.Contents,
            Type = containedType == null ? null : containedType.Type,
            Value = encodingValue == null ? null : encodingValue.Value,
            Text = containedType != null && encodingValue != null
                ? "CONTAINING ENCODED BY"
                : containedType != null ? "CONTAINING" : "ENCODED BY"
        };
        ParsedGeneralConstraintSyntax syntax = new ParsedGeneralConstraintSyntax
        {
            Element = element,
            ContainedTypeSyntax = containedType,
            EncodingValueSyntax = encodingValue,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddGeneralElement(syntax);
    }

    internal TokenGroupParseResult ParseBraceGroup()
    {
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            return new TokenGroupParseResult();
        }

        this.Position = this.Position + 1;
        int contentStart = this.Position;
        int braceDepth = 0;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        while (this.PeekToken() != null)
        {
            AsnToken current = this.ReadRequiredToken();
            if (current.Kind == AsnTokenKind.LeftBrace)
            {
                braceDepth = braceDepth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBrace)
            {
                if (braceDepth == 0 && parenthesisDepth == 0 && bracketDepth == 0)
                {
                    return new TokenGroupParseResult
                    {
                        Success = true,
                        Tokens = this.CopyTokenRange(contentStart, this.Position - 1)
                    };
                }

                braceDepth = braceDepth - 1;
            }
            else if (current.Kind == AsnTokenKind.LeftParenthesis)
            {
                parenthesisDepth = parenthesisDepth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightParenthesis)
            {
                parenthesisDepth = parenthesisDepth - 1;
            }
            else if (current.Kind == AsnTokenKind.LeftBracket
                || current.Kind == AsnTokenKind.LeftVersionBracket)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (current.Kind == AsnTokenKind.RightBracket
                || current.Kind == AsnTokenKind.RightVersionBracket)
            {
                bracketDepth = bracketDepth - 1;
            }
        }

        return new TokenGroupParseResult();
    }

    internal AsnToken[][]? SplitUserDefinedParameters(AsnToken[] tokens)
    {
        if (tokens.Length == 0)
        {
            return [];
        }

        System.Collections.Generic.List<AsnToken[]> parameters =
            new System.Collections.Generic.List<AsnToken[]>();
        int start = 0;
        int braceDepth = 0;
        int parenthesisDepth = 0;
        int bracketDepth = 0;
        int index = 0;
        while (index < tokens.Length)
        {
            AsnToken token = tokens[index];
            if (token.Kind == AsnTokenKind.LeftBrace)
            {
                braceDepth = braceDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBrace)
            {
                braceDepth = braceDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftParenthesis)
            {
                parenthesisDepth = parenthesisDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightParenthesis)
            {
                parenthesisDepth = parenthesisDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.LeftBracket
                || token.Kind == AsnTokenKind.LeftVersionBracket)
            {
                bracketDepth = bracketDepth + 1;
            }
            else if (token.Kind == AsnTokenKind.RightBracket
                || token.Kind == AsnTokenKind.RightVersionBracket)
            {
                bracketDepth = bracketDepth - 1;
            }
            else if (token.Kind == AsnTokenKind.Comma
                && braceDepth == 0
                && parenthesisDepth == 0
                && bracketDepth == 0)
            {
                if (index == start)
                {
                    return null;
                }

                parameters.Add(this.CopyTokenArrayRange(tokens, start, index));
                start = index + 1;
            }

            index = index + 1;
        }

        if (start == tokens.Length)
        {
            return null;
        }

        parameters.Add(this.CopyTokenArrayRange(tokens, start, tokens.Length));
        return parameters.ToArray();
    }

    internal AtNotationListParseResult ParseAtNotationList(AsnToken[] tokens)
    {
        System.Collections.Generic.List<ParsedAtNotationSyntax> items =
            new System.Collections.Generic.List<ParsedAtNotationSyntax>();
        int index = 0;
        while (index < tokens.Length)
        {
            int start = index;
            if (tokens[index].Kind != AsnTokenKind.AtSign)
            {
                this.AddDiagnostic(147, "Expected '@' in a component relation constraint.", tokens[index]);
                return new AtNotationListParseResult();
            }

            index = index + 1;
            int dotCount = 0;
            while (index < tokens.Length && this.GetDotCount(tokens[index]) > 0)
            {
                dotCount = dotCount + this.GetDotCount(tokens[index]);
                index = index + 1;
            }

            bool relative = dotCount > 0;
            int levels = relative ? dotCount - 1 : 0;

            System.Collections.Generic.List<string> identifiers =
                new System.Collections.Generic.List<string>();
            if (index >= tokens.Length || !this.IsComponentIdentifier(tokens[index]))
            {
                this.AddDiagnostic(148, "Expected a component identifier after '@'.", index < tokens.Length ? tokens[index] : null);
                return new AtNotationListParseResult();
            }

            identifiers.Add(tokens[index].Text);
            index = index + 1;
            while (index < tokens.Length && tokens[index].Kind == AsnTokenKind.Dot)
            {
                index = index + 1;
                if (index >= tokens.Length || !this.IsComponentIdentifier(tokens[index]))
                {
                    this.AddDiagnostic(149, "Expected a component identifier after '.'.", index < tokens.Length ? tokens[index] : null);
                    return new AtNotationListParseResult();
                }

                identifiers.Add(tokens[index].Text);
                index = index + 1;
            }

            int end = index;
            items.Add(new ParsedAtNotationSyntax
            {
                Relative = relative,
                Levels = levels,
                ComponentIdentifiers = identifiers.ToArray(),
                SourceTokens = this.CopyTokenArrayRange(tokens, start, end),
                Span = this.CreateSpan(tokens[start], tokens[end - 1])
            });
            if (index == tokens.Length)
            {
                break;
            }

            if (tokens[index].Kind != AsnTokenKind.Comma)
            {
                this.AddDiagnostic(150, "Expected ',' between at-notations.", tokens[index]);
                return new AtNotationListParseResult();
            }

            index = index + 1;
            if (index == tokens.Length)
            {
                this.AddDiagnostic(151, "A component relation constraint cannot have a trailing comma.", tokens[index - 1]);
                return new AtNotationListParseResult();
            }
        }

        if (items.Count == 0)
        {
            this.AddDiagnostic(147, "A component relation constraint requires at least one at-notation.", null);
            return new AtNotationListParseResult();
        }

        return new AtNotationListParseResult
        {
            Success = true,
            Items = items.ToArray()
        };
    }

    internal bool IsTableConstraintStart()
    {
        if (this.ParentType.Kind == AsnTypeKind.OpenType
            || this.ParentType.Kind == AsnTypeKind.InstanceOf
            || (this.ParentType.Kind == AsnTypeKind.Defined
                && this.ParentType.Name.IndexOf(".&", System.StringComparison.Ordinal) >= 0))
        {
            return true;
        }

        int end = this.FindBraceEnd(this.Position);
        return end >= 0
            && end + 2 < this.Tokens.Length
            && this.Tokens[end + 1].Kind == AsnTokenKind.LeftBrace
            && this.Tokens[end + 2].Kind == AsnTokenKind.AtSign;
    }

    internal int FindBraceEnd(int start)
    {
        if (start < 0
            || start >= this.Tokens.Length
            || this.Tokens[start].Kind != AsnTokenKind.LeftBrace)
        {
            return -1;
        }

        int depth = 1;
        int index = start + 1;
        while (index < this.Tokens.Length)
        {
            if (this.Tokens[index].Kind == AsnTokenKind.LeftBrace)
            {
                depth = depth + 1;
            }
            else if (this.Tokens[index].Kind == AsnTokenKind.RightBrace)
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

    internal bool IsComponentIdentifier(AsnToken token)
    {
        return token.Kind == AsnTokenKind.Identifier
            || token.Kind == AsnTokenKind.ValueReference;
    }

    internal int GetDotCount(AsnToken token)
    {
        if (token.Kind == AsnTokenKind.Dot)
        {
            return 1;
        }

        if (token.Kind == AsnTokenKind.Range)
        {
            return 2;
        }

        return token.Kind == AsnTokenKind.Ellipsis ? 3 : 0;
    }

    internal AsnToken[] CopyTokenArrayRange(AsnToken[] tokens, int start, int end)
    {
        AsnToken[] result = new AsnToken[end - start];
        int index = start;
        while (index < end)
        {
            result[index - start] = tokens[index];
            index = index + 1;
        }

        return result;
    }

    internal string JoinTokenText(AsnToken[] tokens)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                text.Append(" ");
            }

            text.Append(tokens[index].Text);
            index = index + 1;
        }

        return text.ToString();
    }

    internal string JoinIdentifiers(string[] identifiers)
    {
        System.Text.StringBuilder text = new System.Text.StringBuilder();
        int index = 0;
        while (index < identifiers.Length)
        {
            if (index > 0)
            {
                text.Append(".");
            }

            text.Append(identifiers[index]);
            index = index + 1;
        }

        return text.ToString();
    }

    internal AsnConstraintElement AddGeneralElement(ParsedGeneralConstraintSyntax syntax)
    {
        this.ParsedGeneralElements.Add(syntax);
        return syntax.Element;
    }

    internal AsnConstraintElement? ParsePossibleTypeElement()
    {
        int start = this.Position;
        AsnToken[] remaining = this.CopyTokenRange(start, this.Tokens.Length);
        TypeParseResult parsed = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
        if (!parsed.Success || parsed.Syntax == null || parsed.ConsumedTokenCount == 0)
        {
            return null;
        }

        this.Position = start;
        return this.ParseTypeElement(false);
    }

    internal AsnConstraintElement? ParseTypeElement(bool includes)
    {
        int start = this.Position;
        if (includes)
        {
            this.Position = this.Position + 1;
        }

        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        TypeParseResult parsed = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
        if (!parsed.Success || parsed.Syntax == null || parsed.ConsumedTokenCount == 0)
        {
            this.AddDiagnostic(125, "Expected a type in the contained subtype or type constraint.", this.PeekToken());
            return null;
        }

        this.Position = this.Position + parsed.ConsumedTokenCount;
        AsnConstraintKind kind = !includes && this.ParentType.Kind == AsnTypeKind.OpenType
            ? AsnConstraintKind.Type
            : AsnConstraintKind.ContainedSubtype;
        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = kind,
            Type = parsed.Syntax.Type
        };
        ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
        {
            Element = element,
            TypeSyntax = parsed.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(syntax);
    }

    internal AsnConstraintElement? ParseNestedConstraintElement(AsnConstraintKind kind)
    {
        int start = this.Position;
        AsnToken keyword = this.ReadRequiredToken();
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
        {
            this.AddDiagnostic(126, "Expected a parenthesized constraint after " + keyword.Text + ".", this.PeekToken());
            return null;
        }

        AsnType nestedParent = kind == AsnConstraintKind.Size
            ? new AsnType { Kind = AsnTypeKind.Integer }
            : this.ParentType;
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ConstraintParseResult parsed = new ConstraintParser(
            remaining,
            this.Diagnostics,
            this.ReportDiagnostics).ParseConstraintSyntax(nestedParent);
        this.Position = this.Position + parsed.ConsumedTokenCount;
        if (!parsed.Success || parsed.Syntax == null || parsed.Syntax.RootElement == null)
        {
            return null;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = kind,
            Children = this.CreateNestedConstraintChildren(parsed.Syntax)
        };
        ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
        {
            Element = element,
            NestedConstraintSyntax = parsed.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(syntax);
    }

    internal AsnConstraintElement? ParsePatternElement()
    {
        int start = this.Position;
        this.Position = this.Position + 1;
        AsnType stringType = new AsnType
        {
            Kind = AsnTypeKind.UniversalString
        };
        ValueParseResult parsed = this.ParseValueWithType(stringType);
        if (!parsed.Success || parsed.Syntax == null)
        {
            this.AddDiagnostic(127, "Expected a character string value after PATTERN.", this.PeekToken());
            return null;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.Pattern,
            Value = parsed.Syntax.Value,
            Text = parsed.Syntax.Value.TextValue
        };
        ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
        {
            Element = element,
            ValueSyntax = parsed.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(syntax);
    }

    internal AsnConstraintElement? ParseSettingsElement()
    {
        int start = this.Position;
        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.CharacterStringLiteral))
        {
            this.AddDiagnostic(128, "Expected a simple string after SETTINGS.", this.PeekToken());
            return null;
        }

        AsnType stringType = new AsnType
        {
            Kind = AsnTypeKind.Utf8String
        };
        ValueParseResult parsed = this.ParseValueWithType(stringType);
        if (!parsed.Success || parsed.Syntax == null)
        {
            this.AddDiagnostic(128, "Expected a simple string after SETTINGS.", this.PeekToken());
            return null;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.PropertySettings,
            Value = parsed.Syntax.Value,
            Text = parsed.Syntax.Value.TextValue
        };
        ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
        {
            Element = element,
            ValueSyntax = parsed.Syntax,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(syntax);
    }

    internal AsnConstraintElement? ParseInnerTypeElement()
    {
        int start = this.Position;
        this.Position = this.Position + 1;
        AsnToken? form = this.PeekToken();
        if (this.IsText(form, "COMPONENT"))
        {
            this.Position = this.Position + 1;
            if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
            {
                this.AddDiagnostic(129, "Expected a constraint after WITH COMPONENT.", this.PeekToken());
                return null;
            }

            AsnType innerType = this.ParentType.ElementType == null
                ? new AsnType { Kind = AsnTypeKind.Unknown }
                : this.ParentType.ElementType;
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            ConstraintParseResult parsed = new ConstraintParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseConstraintSyntax(innerType);
            this.Position = this.Position + parsed.ConsumedTokenCount;
            if (!parsed.Success || parsed.Syntax == null || parsed.Syntax.RootElement == null)
            {
                return null;
            }

            AsnConstraintElement element = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.WithComponent,
                Children = this.CreateNestedConstraintChildren(parsed.Syntax)
            };
            ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
            {
                Element = element,
                NestedConstraintSyntax = parsed.Syntax,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
            return this.AddParsedElement(syntax);
        }

        if (this.IsText(form, "COMPONENTS"))
        {
            return this.ParseMultipleComponentElement(start);
        }

        this.AddDiagnostic(129, "Expected COMPONENT or COMPONENTS after WITH.", form);
        return null;
    }

    internal AsnConstraintElement? ParseMultipleComponentElement(int start)
    {
        this.Position = this.Position + 1;
        if (!this.IsKind(this.PeekToken(), AsnTokenKind.LeftBrace))
        {
            this.AddDiagnostic(130, "Expected '{' after WITH COMPONENTS.", this.PeekToken());
            return null;
        }

        this.Position = this.Position + 1;
        bool partial = false;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.Ellipsis))
        {
            partial = true;
            this.Position = this.Position + 1;
            if (!this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                this.AddDiagnostic(131, "Expected ',' after the partial specification marker.", this.PeekToken());
                return null;
            }

            this.Position = this.Position + 1;
        }

        System.Collections.Generic.List<ParsedNamedConstraintSyntax> named =
            new System.Collections.Generic.List<ParsedNamedConstraintSyntax>();
        while (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            ParsedNamedConstraintSyntax? item = this.ParseNamedComponentConstraint();
            if (item == null)
            {
                return null;
            }

            named.Add(item);
            if (!this.IsKind(this.PeekToken(), AsnTokenKind.Comma))
            {
                break;
            }

            this.Position = this.Position + 1;
            if (this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
            {
                this.AddDiagnostic(132, "A WITH COMPONENTS list cannot have a trailing comma.", this.PeekToken());
                return null;
            }
        }

        if (named.Count == 0)
        {
            this.AddDiagnostic(133, "WITH COMPONENTS requires at least one named constraint.", this.PeekToken());
            return null;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.RightBrace))
        {
            this.AddDiagnostic(134, "Expected '}' to close WITH COMPONENTS.", this.PeekToken());
            return null;
        }

        this.Position = this.Position + 1;
        AsnConstraintElement[] children = new AsnConstraintElement[named.Count];
        int index = 0;
        while (index < named.Count)
        {
            children[index] = named[index].Element;
            index = index + 1;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.WithComponents,
            Text = partial ? "PARTIAL" : "FULL",
            Children = children
        };
        ParsedSubtypeElementSyntax syntax = new ParsedSubtypeElementSyntax
        {
            Element = element,
            NamedConstraints = named.ToArray(),
            PartialSpecification = partial,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(syntax);
    }

    internal ParsedNamedConstraintSyntax? ParseNamedComponentConstraint()
    {
        int start = this.Position;
        AsnToken? name = this.PeekToken();
        if (name == null
            || (name.Kind != AsnTokenKind.Identifier
                && name.Kind != AsnTokenKind.ValueReference))
        {
            this.AddDiagnostic(135, "Expected a component name in WITH COMPONENTS.", name);
            return null;
        }

        this.Position = this.Position + 1;
        ParsedConstraintSyntax? valueConstraint = null;
        if (this.IsKind(this.PeekToken(), AsnTokenKind.LeftParenthesis))
        {
            AsnType componentType = this.FindParentComponentType(name.Text);
            AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
            ConstraintParseResult parsed = new ConstraintParser(
                remaining,
                this.Diagnostics,
                this.ReportDiagnostics).ParseConstraintSyntax(componentType);
            this.Position = this.Position + parsed.ConsumedTokenCount;
            if (!parsed.Success || parsed.Syntax == null)
            {
                return null;
            }

            valueConstraint = parsed.Syntax;
        }

        string presence = "";
        AsnToken? current = this.PeekToken();
        if (this.IsText(current, "PRESENT")
            || this.IsText(current, "ABSENT")
            || this.IsText(current, "OPTIONAL"))
        {
            presence = current == null ? "" : current.Text;
            this.Position = this.Position + 1;
        }

        AsnConstraintElement element = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.WithComponent,
            Name = name.Text,
            Text = presence,
            Children = valueConstraint == null
                ? []
                : this.CreateNestedConstraintChildren(valueConstraint)
        };
        return new ParsedNamedConstraintSyntax
        {
            Name = name.Text,
            Presence = presence,
            Element = element,
            ValueConstraintSyntax = valueConstraint,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
    }

    internal AsnConstraintElement? ParseValueOrRangeElement()
    {
        int start = this.Position;
        bool lowerMinimum = this.IsText(this.PeekToken(), "MIN");
        ParsedValueSyntax? lowerValue = null;
        if (lowerMinimum)
        {
            this.Position = this.Position + 1;
        }
        else
        {
            ValueParseResult parsedLower = this.ParseValueWithType(this.ParentType);
            if (!parsedLower.Success || parsedLower.Syntax == null)
            {
                this.AddDiagnostic(123, "Expected a subtype element.", this.PeekToken());
                return null;
            }

            lowerValue = parsedLower.Syntax;
        }

        bool lowerOpen = this.IsKind(this.PeekToken(), AsnTokenKind.LessThan);
        if (lowerOpen)
        {
            this.Position = this.Position + 1;
        }

        if (!this.IsKind(this.PeekToken(), AsnTokenKind.Range))
        {
            if (lowerMinimum || lowerOpen || lowerValue == null)
            {
                this.AddDiagnostic(136, "MIN and open endpoints must be followed by '..'.", this.PeekToken());
                return null;
            }

            AsnConstraintElement single = new AsnConstraintElement
            {
                Kind = AsnConstraintKind.SingleValue,
                Value = lowerValue.Value
            };
            ParsedSubtypeElementSyntax singleSyntax = new ParsedSubtypeElementSyntax
            {
                Element = single,
                ValueSyntax = lowerValue,
                SourceTokens = this.CopyTokenRange(start, this.Position),
                Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
            };
            return this.AddParsedElement(singleSyntax);
        }

        this.Position = this.Position + 1;
        bool upperOpen = this.IsKind(this.PeekToken(), AsnTokenKind.LessThan);
        if (upperOpen)
        {
            this.Position = this.Position + 1;
        }

        bool upperMaximum = this.IsText(this.PeekToken(), "MAX");
        ParsedValueSyntax? upperValue = null;
        if (upperMaximum)
        {
            this.Position = this.Position + 1;
        }
        else
        {
            ValueParseResult parsedUpper = this.ParseValueWithType(this.ParentType);
            if (!parsedUpper.Success || parsedUpper.Syntax == null)
            {
                this.AddDiagnostic(137, "Expected a value or MAX as the upper range endpoint.", this.PeekToken());
                return null;
            }

            upperValue = parsedUpper.Syntax;
        }

        AsnConstraintElement range = new AsnConstraintElement
        {
            Kind = AsnConstraintKind.ValueRange,
            LowerValue = lowerValue == null ? null : lowerValue.Value,
            UpperValue = upperValue == null ? null : upperValue.Value,
            LowerEndpointUnbounded = lowerMinimum,
            UpperEndpointUnbounded = upperMaximum,
            LowerEndpointIncluded = !lowerOpen,
            UpperEndpointIncluded = !upperOpen
        };
        this.SetIntegerBounds(range);
        ParsedSubtypeElementSyntax rangeSyntax = new ParsedSubtypeElementSyntax
        {
            Element = range,
            LowerValueSyntax = lowerValue,
            UpperValueSyntax = upperValue,
            LowerOpen = lowerOpen,
            UpperOpen = upperOpen,
            LowerIsMinimum = lowerMinimum,
            UpperIsMaximum = upperMaximum,
            SourceTokens = this.CopyTokenRange(start, this.Position),
            Span = this.CreateSpan(this.TokenAt(start), this.PeekTokenAt(-1))
        };
        return this.AddParsedElement(rangeSyntax);
    }

    internal ValueParseResult ParseValueWithType(AsnType type)
    {
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        ValueParseResult parsed = new ValueParser(
            remaining,
            this.Diagnostics,
            false).ParseValueSyntax(type);
        this.Position = this.Position + parsed.ConsumedTokenCount;
        return parsed;
    }

    internal void SetIntegerBounds(AsnConstraintElement range)
    {
        if (range.LowerValue != null && range.LowerValue.HasIntegerValue)
        {
            range.HasLowerBound = true;
            range.LowerBound = range.LowerValue.IntegerValue;
        }

        if (range.UpperValue != null && range.UpperValue.HasIntegerValue)
        {
            range.HasUpperBound = true;
            range.UpperBound = range.UpperValue.IntegerValue;
        }
    }

    internal AsnType FindParentComponentType(string name)
    {
        int index = 0;
        while (index < this.ParentType.Components.Length)
        {
            AsnComponent component = this.ParentType.Components[index];
            if (component.Name == name && component.Type != null)
            {
                return component.Type;
            }

            index = index + 1;
        }

        return new AsnType
        {
            Kind = AsnTypeKind.Unknown
        };
    }

    internal AsnConstraintElement[] CreateNestedConstraintChildren(ParsedConstraintSyntax syntax)
    {
        System.Collections.Generic.List<AsnConstraintElement> children =
            new System.Collections.Generic.List<AsnConstraintElement>();
        if (syntax.RootElement != null)
        {
            children.Add(syntax.RootElement);
        }

        int index = 0;
        while (index < syntax.Constraint.Elements.Length)
        {
            AsnConstraintElement item = syntax.Constraint.Elements[index];
            if (item.Kind == AsnConstraintKind.Extension
                || item.Kind == AsnConstraintKind.Exception)
            {
                children.Add(item);
            }

            index = index + 1;
        }

        return children.ToArray();
    }

    internal AsnConstraintElement AddParsedElement(ParsedSubtypeElementSyntax syntax)
    {
        this.ParsedElements.Add(syntax);
        return syntax.Element;
    }

    internal ExceptionParseResult ParseExceptionIdentification()
    {
        int start = this.Position;
        AsnToken[] remaining = this.CopyTokenRange(this.Position, this.Tokens.Length);
        TypeParseResult type = new TypeParser(remaining, this.Diagnostics, false).ParseTypeSyntax();
        if (type.Success
            && type.Syntax != null
            && type.ConsumedTokenCount < remaining.Length
            && remaining[type.ConsumedTokenCount].Kind == AsnTokenKind.Colon)
        {
            this.Position = this.Position + type.ConsumedTokenCount + 1;
            AsnToken[] valueTokens = this.CopyTokenRange(this.Position, this.Tokens.Length);
            ValueParseResult typedValue = new ValueParser(
                valueTokens,
                this.Diagnostics,
                false).ParseValueSyntax(type.Syntax.Type);
            if (!typedValue.Success
                || typedValue.Syntax == null
                || typedValue.ConsumedTokenCount == 0)
            {
                this.Position = start;
                return new ExceptionParseResult();
            }

            this.Position = this.Position + typedValue.ConsumedTokenCount;
            return new ExceptionParseResult
            {
                Success = true,
                TypeSyntax = type.Syntax,
                ValueSyntax = typedValue.Syntax
            };
        }

        AsnType integerType = new AsnType
        {
            Kind = AsnTypeKind.Integer
        };
        ValueParseResult integerValue = new ValueParser(
            remaining,
            this.Diagnostics,
            false).ParseValueSyntax(integerType);
        if (!integerValue.Success
            || integerValue.Syntax == null
            || integerValue.ConsumedTokenCount == 0)
        {
            return new ExceptionParseResult();
        }

        this.Position = this.Position + integerValue.ConsumedTokenCount;
        return new ExceptionParseResult
        {
            Success = true,
            ValueSyntax = integerValue.Syntax
        };
    }

    internal AsnConstraintElement CombineOperation(
        AsnConstraintKind kind,
        AsnConstraintElement left,
        AsnConstraintElement right)
    {
        System.Collections.Generic.List<AsnConstraintElement> children =
            new System.Collections.Generic.List<AsnConstraintElement>();
        if (left.Kind == kind)
        {
            children.AddRange(left.Children);
        }
        else
        {
            children.Add(left);
        }

        if (right.Kind == kind)
        {
            children.AddRange(right.Children);
        }
        else
        {
            children.Add(right);
        }

        return new AsnConstraintElement
        {
            Kind = kind,
            Children = children.ToArray()
        };
    }

    internal bool IsUnionMark(AsnToken? token)
    {
        return this.IsKind(token, AsnTokenKind.VerticalBar) || this.IsText(token, "UNION");
    }

    internal bool IsGeneralConstraintKind(AsnConstraintKind kind)
    {
        return kind == AsnConstraintKind.UserDefined
            || kind == AsnConstraintKind.SimpleTable
            || kind == AsnConstraintKind.ComponentRelation
            || kind == AsnConstraintKind.Contents;
    }

    internal bool IsIntersectionMark(AsnToken? token)
    {
        return this.IsKind(token, AsnTokenKind.Caret) || this.IsText(token, "INTERSECTION");
    }

    internal bool IsElementTerminator(AsnToken? token)
    {
        return token == null
            || token.Kind == AsnTokenKind.RightParenthesis
            || token.Kind == AsnTokenKind.Comma
            || token.Kind == AsnTokenKind.ExclamationMark
            || token.Kind == AsnTokenKind.VerticalBar
            || token.Kind == AsnTokenKind.Caret
            || token.Text == "UNION"
            || token.Text == "INTERSECTION"
            || token.Text == "EXCEPT";
    }

    internal bool IsText(AsnToken? token, string text)
    {
        return token != null && token.Text == text;
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
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

    internal AsnToken ReadRequiredToken()
    {
        AsnToken token = this.Tokens[this.Position];
        this.Position = this.Position + 1;
        return token;
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

    internal AsnSourceSpan? CreateSpan(AsnToken? first, AsnToken? last)
    {
        if (first == null || first.Span == null || last == null || last.Span == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = first.Span.Start,
            End = last.Span.End
        };
    }

    internal ConstraintParseResult CreateResult(
        bool success,
        ParsedConstraintSyntax? syntax,
        int start)
    {
        return new ConstraintParseResult
        {
            Success = success,
            Syntax = syntax,
            ConsumedTokenCount = this.Position - start
        };
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
