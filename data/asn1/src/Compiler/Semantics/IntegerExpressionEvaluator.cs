namespace asn1;

internal class IntegerExpressionEvaluator
{
    internal ValueResolver Resolver;
    internal AsnToken[] Tokens;
    internal SymbolTable Module;
    internal SymbolTableEntry Declaration;
    internal ResolvedTypeSyntax? GoverningType;
    internal int Position;

    internal IntegerExpressionEvaluator(
        ValueResolver resolver,
        AsnToken[] tokens,
        SymbolTable module,
        SymbolTableEntry declaration,
        ResolvedTypeSyntax? governingType)
    {
        this.Resolver = resolver == null
            ? throw new System.ArgumentNullException("resolver")
            : resolver;
        this.Tokens = tokens == null
            ? throw new System.ArgumentNullException("tokens")
            : tokens;
        this.Module = module == null
            ? throw new System.ArgumentNullException("module")
            : module;
        this.Declaration = declaration == null
            ? throw new System.ArgumentNullException("declaration")
            : declaration;
        this.GoverningType = governingType;
    }

    internal IntegerEvaluationResult EvaluateExpression()
    {
        IntegerEvaluationResult result = this.EvaluateAdditiveExpression();
        if (!result.Success || this.Position != this.Tokens.Length)
        {
            this.Resolver.AddIntegerExpressionDiagnostic(
                "The integer expression is not valid.",
                this.PeekToken());
            return new IntegerEvaluationResult();
        }

        return result;
    }

    internal IntegerEvaluationResult EvaluateAdditiveExpression()
    {
        IntegerEvaluationResult result = this.EvaluateMultiplicativeExpression();
        while (result.Success
            && (this.IsKind(this.PeekToken(), AsnTokenKind.Plus)
                || this.IsKind(this.PeekToken(), AsnTokenKind.Minus)))
        {
            AsnToken operation = this.ReadRequiredToken();
            IntegerEvaluationResult right = this.EvaluateMultiplicativeExpression();
            if (!right.Success)
            {
                return right;
            }

            result.Value = operation.Kind == AsnTokenKind.Plus
                ? result.Value + right.Value
                : result.Value - right.Value;
        }

        return result;
    }

    internal IntegerEvaluationResult EvaluateMultiplicativeExpression()
    {
        IntegerEvaluationResult result = this.EvaluateUnaryExpression();
        while (result.Success
            && (this.IsKind(this.PeekToken(), AsnTokenKind.Asterisk)
                || this.IsKind(this.PeekToken(), AsnTokenKind.Slash)))
        {
            AsnToken operation = this.ReadRequiredToken();
            IntegerEvaluationResult right = this.EvaluateUnaryExpression();
            if (!right.Success)
            {
                return right;
            }

            if (operation.Kind == AsnTokenKind.Slash
                && right.Value == System.Numerics.BigInteger.Zero)
            {
                this.Resolver.AddIntegerExpressionDiagnostic(
                    "An integer expression cannot divide by zero.",
                    operation);
                return new IntegerEvaluationResult();
            }

            result.Value = operation.Kind == AsnTokenKind.Asterisk
                ? result.Value * right.Value
                : result.Value / right.Value;
        }

        return result;
    }

    internal IntegerEvaluationResult EvaluateUnaryExpression()
    {
        AsnToken? token = this.PeekToken();
        if (this.IsKind(token, AsnTokenKind.Plus)
            || this.IsKind(token, AsnTokenKind.Minus))
        {
            this.Position = this.Position + 1;
            IntegerEvaluationResult operand = this.EvaluateUnaryExpression();
            if (operand.Success && token != null && token.Kind == AsnTokenKind.Minus)
            {
                operand.Value = -operand.Value;
            }

            return operand;
        }

        return this.EvaluatePrimaryExpression();
    }

    internal IntegerEvaluationResult EvaluatePrimaryExpression()
    {
        AsnToken? token = this.PeekToken();
        if (token == null)
        {
            return new IntegerEvaluationResult();
        }

        if (token.Kind == AsnTokenKind.IntegerLiteral)
        {
            this.Position = this.Position + 1;
            return new IntegerEvaluationResult
            {
                Success = true,
                Value = System.Numerics.BigInteger.Parse(
                    token.Text,
                    System.Globalization.CultureInfo.InvariantCulture)
            };
        }

        if (this.IsValueName(token))
        {
            this.Position = this.Position + 1;
            return this.Resolver.ResolveIntegerReference(
                "",
                token.Text,
                this.Module,
                this.Declaration,
                this.GoverningType,
                token);
        }

        if (this.IsTypeReferenceToken(token)
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
            && this.IsValueName(this.PeekTokenAt(2)))
        {
            AsnToken? name = this.PeekTokenAt(2);
            this.Position = this.Position + 3;
            return this.Resolver.ResolveIntegerReference(
                token.Text,
                name == null ? "" : name.Text,
                this.Module,
                this.Declaration,
                this.GoverningType,
                token);
        }

        if (!this.IsKind(token, AsnTokenKind.LeftParenthesis))
        {
            return new IntegerEvaluationResult();
        }

        this.Position = this.Position + 1;
        IntegerEvaluationResult nested = this.EvaluateAdditiveExpression();
        if (!nested.Success
            || !this.IsKind(this.PeekToken(), AsnTokenKind.RightParenthesis))
        {
            return new IntegerEvaluationResult();
        }

        this.Position = this.Position + 1;
        return nested;
    }

    internal bool IsValueName(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.Identifier
                || token.Kind == AsnTokenKind.ValueReference
                || token.Kind == AsnTokenKind.ObjectReference);
    }

    internal bool IsTypeReferenceToken(AsnToken? token)
    {
        return token != null
            && (token.Kind == AsnTokenKind.TypeReference
                || token.Kind == AsnTokenKind.ModuleReference
                || token.Kind == AsnTokenKind.ObjectClassReference);
    }

    internal bool IsKind(AsnToken? token, AsnTokenKind kind)
    {
        return token != null && token.Kind == kind;
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

    internal AsnToken? PeekTokenAt(int offset)
    {
        int index = this.Position + offset;
        return index < 0 || index >= this.Tokens.Length ? null : this.Tokens[index];
    }
}
