namespace asn1;

internal class EcnPropertyCompiler
{
    internal DiagnosticBag Diagnostics;
    internal bool Success = true;

    internal EcnPropertyCompiler(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool CompileTables(EcnSymbolTable[] tables)
    {
        if (tables == null)
        {
            throw new System.ArgumentNullException("tables");
        }

        this.Success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            int objectIndex = 0;
            while (objectIndex < tables[tableIndex].EncodingObjects.Length)
            {
                EcnSymbol symbol = tables[tableIndex].EncodingObjects[objectIndex];
                if (!symbol.Imported && symbol.ObjectDefinition != null)
                {
                    this.ResolveObjectProperties(symbol.ObjectDefinition);
                }

                objectIndex = objectIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal EcnEncodingProperty[] ResolveObjectProperties(
        EcnEncodingObjectDefinition definition)
    {
        if (definition.PropertyResolutionState == 2)
        {
            return definition.Properties;
        }

        if (definition.PropertyResolutionState == 1)
        {
            definition.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.PropertyReferenceCycle,
                "The encoding object '" + definition.Name
                    + "' has a cyclic property reference.",
                definition.Span);
            return [];
        }

        definition.PropertyResolutionState = 1;
        if (definition.ReferenceDefinition)
        {
            EcnEncodingObjectDefinition? referenced =
                definition.ReferencedObjectSymbol == null
                    ? null
                    : definition.ReferencedObjectSymbol.ObjectDefinition;
            if (referenced != null)
            {
                definition.Properties = this.CopyProperties(
                    this.ResolveObjectProperties(referenced));
                if (!referenced.Valid)
                {
                    definition.Valid = false;
                }
            }
        }
        else
        {
            EcnAssignmentSyntax? assignment = definition.Symbol == null
                ? null
                : definition.Symbol.Assignment;
            definition.Properties = assignment == null
                ? []
                : this.ParseProperties(
                    assignment.Clauses,
                    definition.Symbol == null
                        ? EcnClassCategory.Unknown
                        : definition.Symbol.ClassCategory,
                    definition);
        }

        definition.PropertyResolutionState = 2;
        return definition.Properties;
    }

    internal EcnEncodingProperty[] ParseProperties(
        EcnClauseSyntax[] clauses,
        EcnClassCategory category,
        EcnEncodingObjectDefinition owner)
    {
        System.Collections.Generic.List<EcnEncodingProperty> properties =
            new System.Collections.Generic.List<EcnEncodingProperty>();
        EcnEncodingPropertyScope scope = EcnEncodingPropertyScope.General;
        int index = 0;
        while (index < clauses.Length)
        {
            EcnClauseSyntax clause = clauses[index];
            if (clause.Kind == EcnClauseKind.Property)
            {
                EcnEncodingPropertyKind kind = this.GetPropertyKind(
                    clause.Name);
                EcnEncodingPropertyScope propertyScope =
                    this.GetPropertyScope(kind, scope);
                EcnEncodingProperty property = this.ParseProperty(
                    clause,
                    kind,
                    propertyScope);
                if (!EcnPropertyApplicability.IsAllowed(kind, category))
                {
                    property.Valid = false;
                    owner.Valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.IncompatibleProperty,
                        "The ECN property '" + property.Name
                            + "' is not valid for encoding class category '"
                            + category.ToString() + "'.",
                        property.Span);
                }

                if (this.HasDuplicateProperty(properties, property))
                {
                    property.Valid = false;
                    owner.Valid = false;
                    this.AddDiagnostic(
                        EcnDiagnosticCodes.DuplicateProperty,
                        "The ECN property '" + property.Name
                            + "' is specified more than once in the same property group.",
                        property.Span);
                }

                if (!property.Valid)
                {
                    owner.Valid = false;
                }

                properties.Add(property);
                scope = this.AdvanceScope(kind, scope);
            }

            index = index + 1;
        }

        return properties.ToArray();
    }

    internal EcnEncodingProperty ParseProperty(
        EcnClauseSyntax clause,
        EcnEncodingPropertyKind kind,
        EcnEncodingPropertyScope scope)
    {
        int valueStart = 1;
        string propertyName = clause.Name;
        if (kind == EcnEncodingPropertyKind.UnusedBits
            && clause.Name == "UNUSED"
            && clause.Tokens.Length > 1
            && clause.Tokens[1].Text == "BITS")
        {
            valueStart = 2;
            propertyName = "UNUSED BITS";
        }

        AsnToken[] values = clause.Tokens.Length <= valueStart
            ? []
            : this.CopyTokens(
                clause.Tokens,
                valueStart,
                clause.Tokens.Length - valueStart);
        EcnEncodingProperty property = new EcnEncodingProperty
        {
            Kind = kind,
            Scope = scope,
            Name = propertyName,
            Tokens = clause.Tokens,
            ValueTokens = values,
            Value = this.JoinTokens(values),
            Span = clause.Span,
            Valid = kind != EcnEncodingPropertyKind.Unknown
        };
        if (this.IsGroupProperty(kind))
        {
            property.ValueKind = EcnEncodingPropertyValueKind.None;
            return property;
        }

        if (values.Length == 0)
        {
            property.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidProperty,
                "The ECN property '" + property.Name
                    + "' requires a value.",
                property.Span);
            return property;
        }

        this.ClassifyPropertyValue(property);
        this.ValidatePropertyValue(property);
        return property;
    }

    internal void ClassifyPropertyValue(EcnEncodingProperty property)
    {
        AsnToken[] values = property.ValueTokens;
        int range = this.FindRangeToken(values);
        if (property.Kind == EcnEncodingPropertyKind.Range && range >= 0)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.Range;
            property.LowerBound = this.JoinCompact(values, 0, range);
            property.UpperBound = this.JoinCompact(
                values,
                range + 1,
                values.Length - range - 1);
            return;
        }

        if (property.Kind == EcnEncodingPropertyKind.Multiple)
        {
            int unitIndex = values[0].Text == "OF" ? 1 : 0;
            property.Unit = unitIndex < values.Length
                ? values[unitIndex].Text
                : "";
            property.Value = property.Unit;
        }
        else if (property.Kind == EcnEncodingPropertyKind.Alignment)
        {
            property.Unit = values[values.Length - 1].Text;
        }

        string compact = this.JoinCompact(values, 0, values.Length);
        EcnIntegerParseResult integer = this.ParseInteger(compact);
        if (integer.Success)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.Integer;
            property.IntegerValue = integer.Value;
            property.HasIntegerValue = true;
            return;
        }

        if (values.Length == 1
            && values[0].Kind == AsnTokenKind.CharacterStringLiteral)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.String;
        }
        else if (values.Length == 1
            && values[0].Kind == AsnTokenKind.BinaryStringLiteral)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.BitString;
        }
        else if (values.Length == 1
            && values[0].Kind == AsnTokenKind.HexStringLiteral)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.OctetString;
        }
        else if (values.Length == 1)
        {
            property.ValueKind = EcnEncodingPropertyValueKind.Identifier;
        }
        else
        {
            property.ValueKind = EcnEncodingPropertyValueKind.TokenSequence;
        }
    }

    internal void ValidatePropertyValue(EcnEncodingProperty property)
    {
        bool valid = true;
        if (property.Kind == EcnEncodingPropertyKind.Size)
        {
            valid = this.ValidateSize(property);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Range)
        {
            valid = this.ValidateRange(property);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Alphabet)
        {
            valid = property.ValueTokens.Length > 0;
        }
        else if (property.Kind == EcnEncodingPropertyKind.Alignment)
        {
            valid = this.IsUnit(property.Unit)
                || this.IsOneOf(
                    property.Value,
                    ["none", "aligned", "unaligned"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Padding)
        {
            valid = this.IsOneOf(
                property.Value,
                ["zero", "one", "pattern", "encoder-option"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.ByteOrder
            || property.Kind == EcnEncodingPropertyKind.WordOrder)
        {
            valid = this.IsOneOf(
                property.Value,
                ["big-endian", "little-endian", "network", "most-significant-first", "least-significant-first"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.BitOrder)
        {
            valid = this.IsOneOf(
                property.Value,
                ["most-significant-first", "least-significant-first", "msb-first", "lsb-first"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Signedness)
        {
            valid = this.IsOneOf(
                property.Value,
                ["signed", "unsigned", "twos-complement", "ones-complement", "sign-magnitude"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Multiple)
        {
            valid = this.IsUnit(property.Unit);
        }
        else if (property.Kind == EcnEncodingPropertyKind.BitReversal)
        {
            valid = this.IsOneOf(
                property.Value,
                ["no-reversal", "reverse-bits-in-units", "reverse-half-units", "reverse-bits-in-half-units"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.ValueReversal)
        {
            valid = this.IsOneOf(property.Value, ["TRUE", "FALSE"]);
        }
        else if (property.Kind == EcnEncodingPropertyKind.Justification)
        {
            valid = property.ValueTokens[0].Text == "left"
                || property.ValueTokens[0].Text == "right";
        }

        if (!valid)
        {
            property.Valid = false;
            this.AddDiagnostic(
                EcnDiagnosticCodes.InvalidProperty,
                "The value '" + property.Value
                    + "' is not valid for ECN property '" + property.Name
                    + "'.",
                property.Span);
        }
    }

    internal bool ValidateSize(EcnEncodingProperty property)
    {
        if (property.HasIntegerValue)
        {
            return property.IntegerValue >= -3;
        }

        string compact = this.JoinCompact(
            property.ValueTokens,
            0,
            property.ValueTokens.Length);
        if (this.IsDecimalText(compact))
        {
            return compact.Length > 0 && compact[0] != '-';
        }

        return property.ValueKind == EcnEncodingPropertyValueKind.Identifier
            || this.IsOneOf(
                property.Value,
                ["encoder-option-with-determinant", "variable-with-determinant", "self-delimiting-values", "fixed-to-max", "variable"]);
    }

    internal bool ValidateRange(EcnEncodingProperty property)
    {
        if (property.ValueKind != EcnEncodingPropertyValueKind.Range
            || property.LowerBound.Length == 0
            || property.UpperBound.Length == 0)
        {
            return false;
        }

        EcnIntegerParseResult lower = this.ParseInteger(property.LowerBound);
        EcnIntegerParseResult upper = this.ParseInteger(property.UpperBound);
        return !lower.Success || !upper.Success || lower.Value <= upper.Value;
    }

    internal bool HasDuplicateProperty(
        System.Collections.Generic.List<EcnEncodingProperty> properties,
        EcnEncodingProperty candidate)
    {
        int index = 0;
        while (index < properties.Count)
        {
            EcnEncodingProperty current = properties[index];
            bool nameSensitive = candidate.Kind == EcnEncodingPropertyKind.Padding
                || candidate.Kind == EcnEncodingPropertyKind.Pattern;
            if (current.Kind == candidate.Kind
                && current.Scope == candidate.Scope
                && (!nameSensitive || current.Name == candidate.Name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal EcnEncodingPropertyScope AdvanceScope(
        EcnEncodingPropertyKind kind,
        EcnEncodingPropertyScope current)
    {
        if (kind == EcnEncodingPropertyKind.EncodingSpace)
        {
            return EcnEncodingPropertyScope.EncodingSpace;
        }

        if (kind == EcnEncodingPropertyKind.RepetitionSpace)
        {
            return EcnEncodingPropertyScope.RepetitionSpace;
        }

        if (kind == EcnEncodingPropertyKind.RepetitionEncoding)
        {
            return EcnEncodingPropertyScope.RepetitionEncoding;
        }

        if (kind == EcnEncodingPropertyKind.OptionalEncoding)
        {
            return EcnEncodingPropertyScope.OptionalEncoding;
        }

        if (kind == EcnEncodingPropertyKind.Alternative)
        {
            return EcnEncodingPropertyScope.Alternative;
        }

        if (kind == EcnEncodingPropertyKind.Presence)
        {
            return EcnEncodingPropertyScope.Presence;
        }

        return kind == EcnEncodingPropertyKind.ValuePadding
            ? EcnEncodingPropertyScope.ValuePadding
            : current;
    }

    internal EcnEncodingPropertyScope GetPropertyScope(
        EcnEncodingPropertyKind kind,
        EcnEncodingPropertyScope current)
    {
        return this.IsGroupProperty(kind)
            ? EcnEncodingPropertyScope.General
            : current;
    }

    internal bool IsGroupProperty(EcnEncodingPropertyKind kind)
    {
        return kind == EcnEncodingPropertyKind.EncodingSpace
            || kind == EcnEncodingPropertyKind.RepetitionSpace
            || kind == EcnEncodingPropertyKind.RepetitionEncoding
            || kind == EcnEncodingPropertyKind.OptionalEncoding
            || kind == EcnEncodingPropertyKind.Alternative
            || kind == EcnEncodingPropertyKind.Presence
            || kind == EcnEncodingPropertyKind.ValuePadding;
    }

    internal EcnEncodingPropertyKind GetPropertyKind(string name)
    {
        switch (name)
        {
            case "REPRESENTATION": return EcnEncodingPropertyKind.Representation;
            case "SIZE": return EcnEncodingPropertyKind.Size;
            case "RANGE": return EcnEncodingPropertyKind.Range;
            case "ALPHABET": return EcnEncodingPropertyKind.Alphabet;
            case "ALIGNMENT":
            case "ALIGNED": return EcnEncodingPropertyKind.Alignment;
            case "PADDING":
            case "PRE-PADDING":
            case "POST-PADDING": return EcnEncodingPropertyKind.Padding;
            case "BYTE-ORDER": return EcnEncodingPropertyKind.ByteOrder;
            case "BIT-ORDER": return EcnEncodingPropertyKind.BitOrder;
            case "SIGNEDNESS": return EcnEncodingPropertyKind.Signedness;
            case "TERMINATION": return EcnEncodingPropertyKind.Termination;
            case "ENCODING-SPACE": return EcnEncodingPropertyKind.EncodingSpace;
            case "REPETITION-SPACE": return EcnEncodingPropertyKind.RepetitionSpace;
            case "REPETITION-ENCODING": return EcnEncodingPropertyKind.RepetitionEncoding;
            case "OPTIONAL-ENCODING": return EcnEncodingPropertyKind.OptionalEncoding;
            case "ALTERNATIVE": return EcnEncodingPropertyKind.Alternative;
            case "PRESENCE": return EcnEncodingPropertyKind.Presence;
            case "MULTIPLE": return EcnEncodingPropertyKind.Multiple;
            case "PAD-PATTERN":
            case "PATTERN": return EcnEncodingPropertyKind.Pattern;
            case "JUSTIFIED": return EcnEncodingPropertyKind.Justification;
            case "BIT-REVERSAL": return EcnEncodingPropertyKind.BitReversal;
            case "VALUE-PADDING": return EcnEncodingPropertyKind.ValuePadding;
            case "VALUE-REVERSAL": return EcnEncodingPropertyKind.ValueReversal;
            case "TRUE-PATTERN": return EcnEncodingPropertyKind.TruePattern;
            case "FALSE-PATTERN": return EcnEncodingPropertyKind.FalsePattern;
            case "UNUSED":
            case "UNUSED-BITS": return EcnEncodingPropertyKind.UnusedBits;
            case "WORD-ORDER": return EcnEncodingPropertyKind.WordOrder;
            default: return EcnEncodingPropertyKind.Unknown;
        }
    }

    internal bool IsUnit(string value)
    {
        if (this.IsOneOf(
            value,
            ["repetitions", "bit", "bits", "nibble", "octet", "word16", "dword32"]))
        {
            return true;
        }

        EcnIntegerParseResult parsed = this.ParseInteger(value);
        return parsed.Success && parsed.Value >= 0 && parsed.Value <= 256;
    }

    internal bool IsOneOf(string value, string[] choices)
    {
        int index = 0;
        while (index < choices.Length)
        {
            if (value == choices[index])
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal EcnIntegerParseResult ParseInteger(string text)
    {
        EcnIntegerParseResult result = new EcnIntegerParseResult();
        if (!this.IsDecimalText(text))
        {
            return result;
        }

        bool negative = text[0] == '-';
        int index = negative || text[0] == '+' ? 1 : 0;
        long value = 0;
        while (index < text.Length)
        {
            int digit = text[index] - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                return result;
            }

            value = value * 10 + digit;
            index = index + 1;
        }

        result.Value = negative ? -value : value;
        result.Success = true;
        return result;
    }

    internal bool IsDecimalText(string text)
    {
        if (text.Length == 0)
        {
            return false;
        }

        int index = text[0] == '-' || text[0] == '+' ? 1 : 0;
        if (index == text.Length)
        {
            return false;
        }

        while (index < text.Length)
        {
            if (text[index] < '0' || text[index] > '9')
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal int FindRangeToken(AsnToken[] tokens)
    {
        int index = 0;
        while (index < tokens.Length)
        {
            if (tokens[index].Kind == AsnTokenKind.Range)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal string JoinTokens(AsnToken[] tokens)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = 0;
        while (index < tokens.Length)
        {
            if (index > 0)
            {
                builder.Append(' ');
            }

            builder.Append(tokens[index].Text);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal string JoinCompact(AsnToken[] tokens, int start, int count)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        int index = start;
        int end = start + count;
        while (index < end)
        {
            builder.Append(tokens[index].Text);
            index = index + 1;
        }

        return builder.ToString();
    }

    internal EcnEncodingProperty[] CopyProperties(
        EcnEncodingProperty[] properties)
    {
        EcnEncodingProperty[] copied =
            new EcnEncodingProperty[properties.Length];
        int index = 0;
        while (index < properties.Length)
        {
            copied[index] = properties[index];
            index = index + 1;
        }

        return copied;
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

    internal void AddDiagnostic(
        int code,
        string message,
        AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Ecn,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
