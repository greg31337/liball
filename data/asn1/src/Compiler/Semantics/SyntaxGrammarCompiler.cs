namespace asn1;

internal class SyntaxGrammarCompiler
{
    internal const int LeftBraceMask = 1;
    internal const int LeftParenthesisMask = 2;
    internal const int LeftBracketMask = 4;
    internal const int LowerReferenceMask = 8;
    internal const int UpperReferenceMask = 16;
    internal const int IntegerLiteralMask = 32;
    internal const int RealLiteralMask = 64;
    internal const int StringLiteralMask = 128;
    internal const int BinaryLiteralMask = 256;
    internal const int XmlValueMask = 512;
    internal const int ReservedValueMask = 1024;
    internal const int ReservedTypeMask = 2048;
    internal const int OtherValueMask = 4096;
    internal const int AllValueMask = 8187;

    internal DiagnosticBag Diagnostics;
    internal System.Collections.Generic.List<string> UsedFields =
        new System.Collections.Generic.List<string>();
    internal bool Success = true;

    internal SyntaxGrammarCompiler(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
    }

    internal bool CompileSyntax(ParsedObjectClassSyntax syntax)
    {
        if (syntax == null)
        {
            throw new System.ArgumentNullException("syntax");
        }

        this.UsedFields.Clear();
        this.Success = true;
        syntax.SyntaxGrammar = null;
        if (syntax.SyntaxTemplate.Length == 0)
        {
            return true;
        }

        CompiledSyntaxItem[] items = this.CompileTemplateItems(
            syntax.SyntaxTemplate,
            syntax);
        this.ValidateFieldCoverage(syntax);
        this.ValidateSyntaxSequence(items);
        if (this.Success)
        {
            syntax.SyntaxGrammar = new CompiledSyntaxGrammar { Items = items };
        }

        return this.Success;
    }

    internal CompiledSyntaxItem[] CompileTemplateItems(
        ParsedSyntaxTemplateItem[] source,
        ParsedObjectClassSyntax owner)
    {
        CompiledSyntaxItem[] result = new CompiledSyntaxItem[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            ParsedSyntaxTemplateItem item = source[index];
            CompiledSyntaxItem compiled = new CompiledSyntaxItem
            {
                OptionalGroup = item.OptionalGroup,
                Span = item.Span
            };
            if (item.OptionalGroup)
            {
                compiled.Children = this.CompileTemplateItems(item.Children, owner);
                compiled.FirstItems = this.CollectSequenceFirstItems(compiled.Children, 0);
                if (!this.ContainsSyntaxField(compiled.Children))
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.EmptySyntaxGroup,
                        "An optional WITH SYNTAX group must contain at least one field setting.",
                        item.Span);
                }
            }
            else if (item.FieldReference)
            {
                compiled.Field = this.FindSyntaxField(owner, item.Text);
                if (compiled.Field == null)
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.UnknownSyntaxField,
                        "The WITH SYNTAX field '" + item.Text
                            + "' is not declared by its object class.",
                        item.Span);
                }
                else
                {
                    if (this.UsedFields.Contains(item.Text))
                    {
                        this.AddDiagnostic(
                            ObjectClassDiagnosticCodes.DuplicateSyntaxField,
                            "The WITH SYNTAX field '" + item.Text
                                + "' appears more than once.",
                            item.Span);
                    }

                    this.UsedFields.Add(item.Text);
                }

                compiled.FirstItems = [compiled];
            }
            else
            {
                compiled.Literal = item.Text;
                if (!this.IsValidSyntaxLiteral(item.Text))
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.InvalidSyntaxLiteral,
                        "The word '" + item.Text
                            + "' cannot be used as a WITH SYNTAX literal.",
                        item.Span);
                }

                compiled.FirstItems = [compiled];
            }

            result[index] = compiled;
            index = index + 1;
        }

        return result;
    }

    internal void ValidateFieldCoverage(ParsedObjectClassSyntax syntax)
    {
        int index = 0;
        while (index < syntax.ObjectClass.Fields.Length)
        {
            AsnObjectClassField field = syntax.ObjectClass.Fields[index];
            if (!this.UsedFields.Contains(field.Name))
            {
                this.AddDiagnostic(
                    ObjectClassDiagnosticCodes.MissingSyntaxField,
                    "The object-class field '" + field.Name
                        + "' is missing from its WITH SYNTAX template.",
                    syntax.Span);
            }

            index = index + 1;
        }
    }

    internal void ValidateSyntaxSequence(CompiledSyntaxItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            CompiledSyntaxItem item = items[index];
            if (item.OptionalGroup)
            {
                CompiledSyntaxItem[] following = this.CollectSequenceFirstItems(
                    items,
                    index + 1);
                if (this.FirstItemSetsOverlap(item.FirstItems, following))
                {
                    this.AddDiagnostic(
                        ObjectClassDiagnosticCodes.AmbiguousSyntaxGroup,
                        "The optional WITH SYNTAX group has the same possible first token as syntax that follows it.",
                        item.Span);
                }

                if (this.HasLiteralFirstItem(item.FirstItems))
                {
                    CompiledSyntaxItem? delimiter = this.FindFollowingRequiredItem(
                        items,
                        index + 1);
                    if (delimiter != null && delimiter.Literal.Length == 0)
                    {
                        this.AddDiagnostic(
                            ObjectClassDiagnosticCodes.UnreachableSyntax,
                            "A literal-led optional WITH SYNTAX group must be followed by a literal delimiter.",
                            item.Span);
                    }
                }

                this.ValidateSyntaxSequence(item.Children);
            }

            index = index + 1;
        }
    }

    internal CompiledSyntaxItem[] CollectSequenceFirstItems(
        CompiledSyntaxItem[] items,
        int start)
    {
        System.Collections.Generic.List<CompiledSyntaxItem> result =
            new System.Collections.Generic.List<CompiledSyntaxItem>();
        int index = start;
        while (index < items.Length)
        {
            CompiledSyntaxItem item = items[index];
            int firstIndex = 0;
            while (firstIndex < item.FirstItems.Length)
            {
                if (!result.Contains(item.FirstItems[firstIndex]))
                {
                    result.Add(item.FirstItems[firstIndex]);
                }

                firstIndex = firstIndex + 1;
            }

            if (!item.OptionalGroup)
            {
                break;
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal bool ContainsSyntaxField(CompiledSyntaxItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Field != null
                || (items[index].OptionalGroup
                    && this.ContainsSyntaxField(items[index].Children)))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool FirstItemSetsOverlap(
        CompiledSyntaxItem[] left,
        CompiledSyntaxItem[] right)
    {
        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            int rightIndex = 0;
            while (rightIndex < right.Length)
            {
                if (this.TerminalItemsOverlap(left[leftIndex], right[rightIndex]))
                {
                    return true;
                }

                rightIndex = rightIndex + 1;
            }

            leftIndex = leftIndex + 1;
        }

        return false;
    }

    internal bool TerminalItemsOverlap(
        CompiledSyntaxItem left,
        CompiledSyntaxItem right)
    {
        if (left.Literal.Length > 0 || right.Literal.Length > 0)
        {
            return left.Literal.Length > 0
                && right.Literal.Length > 0
                && left.Literal == right.Literal;
        }

        if (left.Field == null || right.Field == null)
        {
            return false;
        }

        return (this.GetFieldStartMask(left.Field)
            & this.GetFieldStartMask(right.Field)) != 0;
    }

    internal int GetFieldStartMask(AsnObjectClassField field)
    {
        if (field.Kind == AsnObjectFieldKind.Type)
        {
            return LeftBracketMask
                | LowerReferenceMask
                | UpperReferenceMask
                | ReservedTypeMask;
        }

        if (field.Kind == AsnObjectFieldKind.FixedTypeValue)
        {
            return this.GetValueStartMask(field.Type == null
                ? AsnTypeKind.Unknown
                : field.Type.Kind);
        }

        if (field.Kind == AsnObjectFieldKind.VariableTypeValue)
        {
            return AllValueMask;
        }

        if (field.Kind == AsnObjectFieldKind.FixedTypeValueSet
            || field.Kind == AsnObjectFieldKind.VariableTypeValueSet)
        {
            return LeftBraceMask | LowerReferenceMask | UpperReferenceMask;
        }

        if (field.Kind == AsnObjectFieldKind.Object)
        {
            return LeftBraceMask | LowerReferenceMask;
        }

        if (field.Kind == AsnObjectFieldKind.ObjectSet)
        {
            return LeftBraceMask
                | LeftParenthesisMask
                | LowerReferenceMask
                | UpperReferenceMask
                | ReservedValueMask;
        }

        return AllValueMask;
    }

    internal int GetValueStartMask(AsnTypeKind kind)
    {
        switch (kind)
        {
            case AsnTypeKind.Boolean:
                return LowerReferenceMask | ReservedValueMask | XmlValueMask;
            case AsnTypeKind.Integer:
            case AsnTypeKind.Enumerated:
                return LowerReferenceMask
                    | IntegerLiteralMask
                    | OtherValueMask
                    | XmlValueMask;
            case AsnTypeKind.Real:
                return LowerReferenceMask
                    | IntegerLiteralMask
                    | RealLiteralMask
                    | ReservedValueMask
                    | XmlValueMask;
            case AsnTypeKind.BitString:
            case AsnTypeKind.OctetString:
                return LowerReferenceMask
                    | LeftBraceMask
                    | BinaryLiteralMask
                    | XmlValueMask;
            case AsnTypeKind.Null:
                return LowerReferenceMask | ReservedValueMask | XmlValueMask;
            case AsnTypeKind.ObjectIdentifier:
            case AsnTypeKind.RelativeObjectIdentifier:
                return LowerReferenceMask | LeftBraceMask | XmlValueMask;
            case AsnTypeKind.Sequence:
            case AsnTypeKind.Set:
            case AsnTypeKind.SequenceOf:
            case AsnTypeKind.SetOf:
            case AsnTypeKind.External:
            case AsnTypeKind.EmbeddedPdv:
            case AsnTypeKind.InstanceOf:
                return LowerReferenceMask | LeftBraceMask | XmlValueMask;
            case AsnTypeKind.Choice:
                return LowerReferenceMask | XmlValueMask;
            case AsnTypeKind.OpenType:
                return AllValueMask;
            default:
                if (this.IsStringValueType(kind))
                {
                    return LowerReferenceMask | StringLiteralMask | XmlValueMask;
                }

                return AllValueMask;
        }
    }

    internal bool IsStringValueType(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.BmpString
            || kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.ObjectIdentifierIri
            || kind == AsnTypeKind.RelativeObjectIdentifierIri
            || kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }

    internal bool HasLiteralFirstItem(CompiledSyntaxItem[] items)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Literal.Length > 0)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal CompiledSyntaxItem? FindFollowingRequiredItem(
        CompiledSyntaxItem[] items,
        int start)
    {
        int index = start;
        while (index < items.Length)
        {
            if (!items[index].OptionalGroup)
            {
                return items[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal AsnObjectClassField? FindSyntaxField(
        ParsedObjectClassSyntax owner,
        string name)
    {
        int index = 0;
        while (index < owner.ObjectClass.Fields.Length)
        {
            if (owner.ObjectClass.Fields[index].Name == name)
            {
                return owner.ObjectClass.Fields[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool IsValidSyntaxLiteral(string text)
    {
        if (text == ",")
        {
            return true;
        }

        if (!this.IsWordLiteral(text))
        {
            return false;
        }

        switch (text)
        {
            case "ABSTRACT-SYNTAX":
            case "BIT":
            case "BOOLEAN":
            case "CHARACTER":
            case "CHOICE":
            case "CONTAINING":
            case "DATE":
            case "DATE-TIME":
            case "DURATION":
            case "EMBEDDED":
            case "END":
            case "ENUMERATED":
            case "EXTERNAL":
            case "FALSE":
            case "INSTANCE":
            case "INTEGER":
            case "MINUS-INFINITY":
            case "NOT-A-NUMBER":
            case "NULL":
            case "OBJECT":
            case "OCTET":
            case "OID-IRI":
            case "PLUS-INFINITY":
            case "REAL":
            case "RELATIVE-OID":
            case "RELATIVE-OID-IRI":
            case "SEQUENCE":
            case "SET":
            case "TIME":
            case "TIME-OF-DAY":
            case "TRUE":
            case "TYPE-IDENTIFIER":
                return false;
            default:
                return text.Length > 0;
        }
    }

    internal bool IsWordLiteral(string text)
    {
        if (text.Length == 0
            || text[0] < 'A'
            || text[0] > 'Z'
            || text[text.Length - 1] == '-')
        {
            return false;
        }

        bool previousHyphen = false;
        int index = 0;
        while (index < text.Length)
        {
            char current = text[index];
            if (current == '-')
            {
                if (previousHyphen)
                {
                    return false;
                }

                previousHyphen = true;
            }
            else
            {
                if (current < 'A' || current > 'Z')
                {
                    return false;
                }

                previousHyphen = false;
            }

            index = index + 1;
        }

        return true;
    }

    internal void AddDiagnostic(int code, string message, AsnSourceSpan? span)
    {
        this.Diagnostics.TryAddDiagnostic(
            AsnDiagnosticSeverity.Error,
            AsnDiagnosticCategory.Binding,
            code,
            message,
            span,
            null);
        this.Success = false;
    }
}
