namespace asn1;

internal class SemanticValueValidator
{
    internal DiagnosticBag Diagnostics;

    internal SemanticValueValidator(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool ValidateResolvedValue(ResolvedValueSyntax resolution)
    {
        if (resolution == null)
        {
            throw new System.ArgumentNullException("resolution");
        }

        AsnValue? value = resolution.EffectiveValue;
        ResolvedTypeSyntax? type = resolution.GoverningType;
        if (value == null || type == null)
        {
            return true;
        }

        AsnTypeKind kind = type.EffectiveKind;
        if (this.IsCharacterStringType(kind))
        {
            return this.ValidateCharacterString(resolution, kind);
        }

        switch (kind)
        {
            case AsnTypeKind.BitString:
                return this.ValidateBitString(resolution);
            case AsnTypeKind.ObjectIdentifier:
            case AsnTypeKind.RelativeObjectIdentifier:
                return this.ValidateObjectIdentifier(resolution);
            case AsnTypeKind.ObjectIdentifierIri:
            case AsnTypeKind.RelativeObjectIdentifierIri:
                return this.ValidateObjectIdentifierIri(value.TextValue, kind, resolution);
            case AsnTypeKind.UtcTime:
            case AsnTypeKind.GeneralizedTime:
            case AsnTypeKind.Time:
            case AsnTypeKind.Date:
            case AsnTypeKind.TimeOfDay:
            case AsnTypeKind.DateTime:
            case AsnTypeKind.Duration:
                return this.ValidateTimeValue(value.TimeValue, resolution);
            default:
                return true;
        }
    }

    internal bool ValidateCharacterString(
        ResolvedValueSyntax resolution,
        AsnTypeKind kind)
    {
        AsnToken? tuple = this.FindCharacterTupleToken(resolution);
        if (tuple != null)
        {
            CharacterTupleParseResult parsed = this.ParseCharacterTuple(tuple.Text);
            if (!parsed.Success)
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.CharacterTupleInvalid,
                    "The character tuple or quadruple does not identify a Unicode scalar value.",
                    resolution);
                return false;
            }

            if (kind == AsnTypeKind.BmpString && parsed.Scalar > 65535)
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.CharacterTupleInvalid,
                    "A BMPString tuple or quadruple must identify a Basic Multilingual Plane scalar.",
                    resolution);
                return false;
            }

            if (!this.IsScalarInAlphabet(parsed.Scalar, kind))
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.RestrictedAlphabetViolation,
                    "The character tuple or quadruple is outside the type's permitted alphabet.",
                    resolution);
                return false;
            }

            return true;
        }

        string text = resolution.EffectiveValue == null
            ? ""
            : resolution.EffectiveValue.TextValue;
        int index = 0;
        while (index < text.Length)
        {
            UnicodeScalarReadResult scalar = this.ReadUnicodeScalar(text, index);
            if (!scalar.Success)
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.UnicodeScalarInvalid,
                    "The character string contains an invalid Unicode scalar sequence.",
                    resolution);
                return false;
            }

            if (!this.IsScalarInAlphabet(scalar.Scalar, kind))
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.RestrictedAlphabetViolation,
                    "The character string contains a character outside the type's permitted alphabet.",
                    resolution);
                return false;
            }

            index = scalar.NextIndex;
        }

        return true;
    }

    internal bool ValidateBitString(ResolvedValueSyntax resolution)
    {
        if (resolution.BitStringValue == null
            || !resolution.BitStringValue.IsValid())
        {
            this.AddValidationDiagnostic(
                SchemaDiagnosticCodes.BitStringPayloadInvalid,
                "The BIT STRING payload has an invalid length or non-zero unused bit position.",
                resolution);
            return false;
        }

        return true;
    }

    internal bool ValidateObjectIdentifier(ResolvedValueSyntax resolution)
    {
        if (resolution.ObjectIdentifierValue == null
            || !resolution.ObjectIdentifierValue.IsValid())
        {
            this.AddValidationDiagnostic(
                SchemaDiagnosticCodes.ObjectIdentifierInvalid,
                "The object identifier has an invalid arc count, root arc, or negative arc.",
                resolution);
            return false;
        }

        return true;
    }

    internal bool ValidateObjectIdentifierIri(
        string text,
        AsnTypeKind kind,
        ResolvedValueSyntax resolution)
    {
        bool relative = kind == AsnTypeKind.RelativeObjectIdentifierIri;
        if (text.Length == 0
            || (!relative && text[0] != '/')
            || (relative && text[0] == '/'))
        {
            this.AddValidationDiagnostic(
                SchemaDiagnosticCodes.ObjectIdentifierIriInvalid,
                relative
                    ? "A relative object identifier IRI must be non-empty and relative."
                    : "An object identifier IRI must be non-empty and begin with '/'.",
                resolution);
            return false;
        }

        int index = relative ? 0 : 1;
        bool segmentHasCharacter = false;
        while (index < text.Length)
        {
            if (text[index] == '/')
            {
                if (!segmentHasCharacter)
                {
                    this.AddValidationDiagnostic(
                        SchemaDiagnosticCodes.ObjectIdentifierIriInvalid,
                        "An object identifier IRI cannot contain an empty arc segment.",
                        resolution);
                    return false;
                }

                segmentHasCharacter = false;
                index = index + 1;
                continue;
            }

            if (text[index] == '%')
            {
                if (index + 2 >= text.Length
                    || !this.IsHexDigit(text[index + 1])
                    || !this.IsHexDigit(text[index + 2]))
                {
                    this.AddValidationDiagnostic(
                        SchemaDiagnosticCodes.ObjectIdentifierIriInvalid,
                        "An object identifier IRI contains an invalid percent escape.",
                        resolution);
                    return false;
                }

                segmentHasCharacter = true;
                index = index + 3;
                continue;
            }

            UnicodeScalarReadResult scalar = this.ReadUnicodeScalar(text, index);
            if (!scalar.Success || !this.IsIriSegmentScalar(scalar.Scalar))
            {
                this.AddValidationDiagnostic(
                    SchemaDiagnosticCodes.ObjectIdentifierIriInvalid,
                    "An object identifier IRI contains an invalid character.",
                    resolution);
                return false;
            }

            segmentHasCharacter = true;
            index = scalar.NextIndex;
        }

        if (!segmentHasCharacter)
        {
            this.AddValidationDiagnostic(
                SchemaDiagnosticCodes.ObjectIdentifierIriInvalid,
                "An object identifier IRI cannot end with an empty arc segment.",
                resolution);
            return false;
        }

        return true;
    }

    internal bool ValidateTimeValue(
        AsnTimeValue? value,
        ResolvedValueSyntax resolution)
    {
        TimeValidationResult result = new TimeValueValidator().ValidateTime(value);
        if (!result.Success)
        {
            this.AddValidationDiagnostic(
                SchemaDiagnosticCodes.TimeValueInvalid,
                result.ErrorMessage,
                resolution);
            return false;
        }

        return true;
    }

    internal AsnToken? FindCharacterTupleToken(ResolvedValueSyntax resolution)
    {
        ResolvedValueSyntax? current = resolution;
        int depth = 0;
        while (current != null && depth < 1024)
        {
            ParsedValueSyntax? syntax = current.SourceSyntax;
            if (syntax != null && syntax.SourceTokens.Length == 1)
            {
                AsnToken token = syntax.SourceTokens[0];
                if (token.Kind == AsnTokenKind.TupleLiteral
                    || token.Kind == AsnTokenKind.QuadrupleLiteral)
                {
                    return token;
                }
            }

            current = current.ReferenceTarget;
            depth = depth + 1;
        }

        return null;
    }

    internal CharacterTupleParseResult ParseCharacterTuple(string text)
    {
        System.Collections.Generic.List<System.Numerics.BigInteger> components =
            new System.Collections.Generic.List<System.Numerics.BigInteger>();
        int index = 0;
        while (index < text.Length && text[index] != '{')
        {
            index = index + 1;
        }

        if (index >= text.Length)
        {
            return new CharacterTupleParseResult();
        }

        index = index + 1;
        while (index < text.Length)
        {
            index = this.SkipTupleTrivia(text, index);
            if (index >= text.Length || !this.IsDecimalDigit(text[index]))
            {
                return new CharacterTupleParseResult();
            }

            int start = index;
            while (index < text.Length && this.IsDecimalDigit(text[index]))
            {
                index = index + 1;
            }

            System.Numerics.BigInteger component = System.Numerics.BigInteger.Parse(
                text.Substring(start, index - start),
                System.Globalization.CultureInfo.InvariantCulture);
            if (component > new System.Numerics.BigInteger(255))
            {
                return new CharacterTupleParseResult();
            }

            components.Add(component);
            index = this.SkipTupleTrivia(text, index);
            if (index < text.Length && text[index] == ',')
            {
                index = index + 1;
                continue;
            }

            if (index < text.Length && text[index] == '}')
            {
                index = index + 1;
                break;
            }

            return new CharacterTupleParseResult();
        }

        if (index != text.Length || (components.Count != 2 && components.Count != 4))
        {
            return new CharacterTupleParseResult();
        }

        System.Numerics.BigInteger scalar = components.Count == 2
            ? components[0] * new System.Numerics.BigInteger(256) + components[1]
            : components[0] * new System.Numerics.BigInteger(16777216)
                + components[1] * new System.Numerics.BigInteger(65536)
                + components[2] * new System.Numerics.BigInteger(256)
                + components[3];
        if (scalar > new System.Numerics.BigInteger(1114111)
            || (scalar >= new System.Numerics.BigInteger(55296)
                && scalar <= new System.Numerics.BigInteger(57343)))
        {
            return new CharacterTupleParseResult();
        }

        return new CharacterTupleParseResult
        {
            Success = true,
            Components = components.ToArray(),
            Scalar = (int)scalar
        };
    }

    internal int SkipTupleTrivia(string text, int index)
    {
        bool skipped = true;
        while (skipped && index < text.Length)
        {
            skipped = false;
            while (index < text.Length && char.IsWhiteSpace(text[index]))
            {
                index = index + 1;
                skipped = true;
            }

            if (index + 1 < text.Length && text[index] == '/' && text[index + 1] == '*')
            {
                int end = text.IndexOf("*/", index + 2, System.StringComparison.Ordinal);
                if (end < 0)
                {
                    return text.Length;
                }

                index = end + 2;
                skipped = true;
            }

            else if (index + 1 < text.Length
                && text[index] == '-'
                && text[index + 1] == '-')
            {
                index = index + 2;
                while (index < text.Length
                    && text[index] != '\r'
                    && text[index] != '\n'
                    && !(index + 1 < text.Length
                        && text[index] == '-'
                        && text[index + 1] == '-'))
                {
                    index = index + 1;
                }

                if (index + 1 < text.Length
                    && text[index] == '-'
                    && text[index + 1] == '-')
                {
                    index = index + 2;
                }

                skipped = true;
            }
        }

        return index;
    }

    internal UnicodeScalarReadResult ReadUnicodeScalar(string text, int index)
    {
        if (index < 0 || index >= text.Length)
        {
            return new UnicodeScalarReadResult();
        }

        int first = text[index];
        if (first >= 55296 && first <= 56319)
        {
            if (index + 1 >= text.Length)
            {
                return new UnicodeScalarReadResult();
            }

            int second = text[index + 1];
            if (second < 56320 || second > 57343)
            {
                return new UnicodeScalarReadResult();
            }

            return new UnicodeScalarReadResult
            {
                Success = true,
                Scalar = 65536 + (first - 55296) * 1024 + second - 56320,
                NextIndex = index + 2
            };
        }

        if (first >= 56320 && first <= 57343)
        {
            return new UnicodeScalarReadResult();
        }

        return new UnicodeScalarReadResult
        {
            Success = true,
            Scalar = first,
            NextIndex = index + 1
        };
    }

    internal bool IsScalarInAlphabet(int scalar, AsnTypeKind kind)
    {
        switch (kind)
        {
            case AsnTypeKind.NumericString:
                return scalar == 32 || scalar >= 48 && scalar <= 57;
            case AsnTypeKind.PrintableString:
                return this.IsPrintableStringScalar(scalar);
            case AsnTypeKind.Ia5String:
                return scalar <= 127;
            case AsnTypeKind.VisibleString:
                return scalar >= 32 && scalar <= 126;
            case AsnTypeKind.BmpString:
                return scalar <= 65535;
            case AsnTypeKind.TeletexString:
            case AsnTypeKind.VideotexString:
            case AsnTypeKind.GeneralString:
                return scalar <= 255;
            case AsnTypeKind.GraphicString:
            case AsnTypeKind.ObjectDescriptor:
                return scalar >= 32 && (scalar < 127 || scalar > 159);
            default:
                return true;
        }
    }

    internal bool IsPrintableStringScalar(int scalar)
    {
        return scalar >= 65 && scalar <= 90
            || scalar >= 97 && scalar <= 122
            || scalar >= 48 && scalar <= 57
            || scalar == 32
            || scalar == 39
            || scalar == 40
            || scalar == 41
            || scalar == 43
            || scalar == 44
            || scalar == 45
            || scalar == 46
            || scalar == 47
            || scalar == 58
            || scalar == 61
            || scalar == 63;
    }

    internal bool IsIriSegmentScalar(int scalar)
    {
        if (scalar >= 160)
        {
            return true;
        }

        return scalar >= 65 && scalar <= 90
            || scalar >= 97 && scalar <= 122
            || scalar >= 48 && scalar <= 57
            || scalar == 45
            || scalar == 46
            || scalar == 95
            || scalar == 126
            || scalar == 33
            || scalar == 36
            || scalar == 38
            || scalar == 39
            || scalar == 40
            || scalar == 41
            || scalar == 42
            || scalar == 43
            || scalar == 44
            || scalar == 59
            || scalar == 61
            || scalar == 58
            || scalar == 64;
    }

    internal bool IsCharacterStringType(AsnTypeKind kind)
    {
        int value = (int)kind;
        return kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || (value >= (int)AsnTypeKind.Utf8String
                && value <= (int)AsnTypeKind.BmpString);
    }

    internal bool IsHexDigit(char value)
    {
        return value >= '0' && value <= '9'
            || value >= 'A' && value <= 'F'
            || value >= 'a' && value <= 'f';
    }

    internal bool IsDecimalDigit(char value)
    {
        return value >= '0' && value <= '9';
    }

    internal void AddValidationDiagnostic(
        int number,
        string message,
        ResolvedValueSyntax resolution)
    {
        AsnSourceSpan? span = resolution.SourceSyntax == null
            ? null
            : resolution.SourceSyntax.Span;
        ResolvedTypeSyntax? governingType = resolution.GoverningType;
        AsnSourceSpan? typeSpan = governingType == null
            ? null
            : governingType.EffectiveDeclaration == null
                ? governingType.SourceSyntax == null
                    ? null
                    : governingType.SourceSyntax.Span
                : governingType.EffectiveDeclaration.Span;
        AsnDiagnosticRelatedLocation[] related = typeSpan == null
            ? []
            : [new AsnDiagnosticRelatedLocation
            {
                Message = "The governing type is declared or used here.",
                Span = typeSpan
            }];
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Semantic,
            number,
            message,
            span,
            related);
    }
}
