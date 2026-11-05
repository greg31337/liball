namespace asn1;

internal class IntegerExpressionSyntaxParser
{
    internal AsnToken[] Tokens;
    internal int Position;
    internal bool SawOperator;

    internal IntegerExpressionSyntaxParser(AsnToken[] tokens, int position)
    {
        this.Tokens = tokens == null
            ? throw new System.ArgumentNullException("tokens")
            : tokens;
        this.Position = position;
    }

    internal bool ParseExpression()
    {
        int start = this.Position;
        if (!this.ParseAdditiveExpression())
        {
            this.Position = start;
            return false;
        }

        return true;
    }

    internal bool ParseAdditiveExpression()
    {
        if (!this.ParseMultiplicativeExpression())
        {
            return false;
        }

        while (this.IsKind(this.PeekToken(), AsnTokenKind.Plus)
            || this.IsKind(this.PeekToken(), AsnTokenKind.Minus))
        {
            this.SawOperator = true;
            this.Position = this.Position + 1;
            if (!this.ParseMultiplicativeExpression())
            {
                return false;
            }
        }

        return true;
    }

    internal bool ParseMultiplicativeExpression()
    {
        if (!this.ParseUnaryExpression())
        {
            return false;
        }

        while (this.IsKind(this.PeekToken(), AsnTokenKind.Asterisk)
            || this.IsKind(this.PeekToken(), AsnTokenKind.Slash))
        {
            this.SawOperator = true;
            this.Position = this.Position + 1;
            if (!this.ParseUnaryExpression())
            {
                return false;
            }
        }

        return true;
    }

    internal bool ParseUnaryExpression()
    {
        AsnToken? token = this.PeekToken();
        if (this.IsKind(token, AsnTokenKind.Plus)
            || this.IsKind(token, AsnTokenKind.Minus))
        {
            this.SawOperator = true;
            this.Position = this.Position + 1;
            return this.ParseUnaryExpression();
        }

        return this.ParsePrimaryExpression();
    }

    internal bool ParsePrimaryExpression()
    {
        AsnToken? token = this.PeekToken();
        if (token == null)
        {
            return false;
        }

        if (token.Kind == AsnTokenKind.IntegerLiteral)
        {
            this.Position = this.Position + 1;
            return true;
        }

        if (this.IsValueName(token))
        {
            this.Position = this.Position + 1;
            return true;
        }

        if (this.IsTypeReferenceToken(token)
            && this.IsKind(this.PeekTokenAt(1), AsnTokenKind.Dot)
            && this.IsValueName(this.PeekTokenAt(2)))
        {
            this.Position = this.Position + 3;
            return true;
        }

        if (!this.IsKind(token, AsnTokenKind.LeftParenthesis))
        {
            return false;
        }

        this.SawOperator = true;
        this.Position = this.Position + 1;
        if (!this.ParseAdditiveExpression()
            || !this.IsKind(this.PeekToken(), AsnTokenKind.RightParenthesis))
        {
            return false;
        }

        this.Position = this.Position + 1;
        return true;
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
