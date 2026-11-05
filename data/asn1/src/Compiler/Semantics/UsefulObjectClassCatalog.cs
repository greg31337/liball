namespace asn1;

internal class UsefulObjectClassCatalog
{
    internal DiagnosticBag Diagnostics;
    internal ParsedObjectClassSyntax TypeIdentifier;
    internal ParsedObjectClassSyntax AbstractSyntax;

    internal UsefulObjectClassCatalog(DiagnosticBag diagnostics)
    {
        this.Diagnostics = diagnostics == null
            ? throw new System.ArgumentNullException("diagnostics")
            : diagnostics;
        this.TypeIdentifier = this.CreateTypeIdentifier();
        this.AbstractSyntax = this.CreateAbstractSyntax();
    }

    internal ParsedObjectClassSyntax? ResolveUsefulClass(
        string moduleName,
        string referenceName)
    {
        if (moduleName.Length != 0)
        {
            return null;
        }

        if (referenceName == "TYPE-IDENTIFIER")
        {
            return this.TypeIdentifier;
        }

        if (referenceName == "ABSTRACT-SYNTAX")
        {
            return this.AbstractSyntax;
        }

        return null;
    }

    internal ParsedObjectClassSyntax DeriveUsefulClass(
        string name,
        ParsedObjectClassSyntax source)
    {
        ParsedObjectClassFieldSyntax[] fieldSyntax =
            new ParsedObjectClassFieldSyntax[source.FieldSyntax.Length];
        AsnObjectClassField[] fields = new AsnObjectClassField[fieldSyntax.Length];
        int index = 0;
        while (index < fieldSyntax.Length)
        {
            fieldSyntax[index] = this.CopyFieldSyntax(source.FieldSyntax[index]);
            fields[index] = fieldSyntax[index].Field;
            index = index + 1;
        }

        ParsedObjectClassSyntax result = new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass
            {
                Name = name,
                Fields = fields,
                SyntaxTokens = source.ObjectClass.SyntaxTokens
            },
            FieldSyntax = fieldSyntax,
            SyntaxTemplate = source.SyntaxTemplate
        };
        new SyntaxGrammarCompiler(this.Diagnostics).CompileSyntax(result);
        return result;
    }

    internal ParsedObjectClassSyntax? FindUsefulClass(AsnObjectClass objectClass)
    {
        if (this.TypeIdentifier.ObjectClass == objectClass)
        {
            return this.TypeIdentifier;
        }

        if (this.AbstractSyntax.ObjectClass == objectClass)
        {
            return this.AbstractSyntax;
        }

        return null;
    }

    internal ParsedObjectClassSyntax CreateTypeIdentifier()
    {
        ParsedObjectClassFieldSyntax identifier = this.CreateIdentifierField();
        ParsedObjectClassFieldSyntax type = this.CreateTypeField();
        ParsedObjectClassSyntax syntax = new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass
            {
                Name = "TYPE-IDENTIFIER",
                Fields = [identifier.Field, type.Field],
                SyntaxTokens = ["&Type", "IDENTIFIED", "BY", "&id"]
            },
            FieldSyntax = [identifier, type],
            SyntaxTemplate = [
                this.CreateFieldTemplate("&Type"),
                this.CreateLiteralTemplate("IDENTIFIED"),
                this.CreateLiteralTemplate("BY"),
                this.CreateFieldTemplate("&id")
            ]
        };
        new SyntaxGrammarCompiler(this.Diagnostics).CompileSyntax(syntax);
        return syntax;
    }

    internal ParsedObjectClassSyntax CreateAbstractSyntax()
    {
        ParsedObjectClassFieldSyntax identifier = this.CreateIdentifierField();
        ParsedObjectClassFieldSyntax type = this.CreateTypeField();
        ParsedObjectClassFieldSyntax property = this.CreatePropertyField();
        ParsedObjectClassSyntax syntax = new ParsedObjectClassSyntax
        {
            ObjectClass = new AsnObjectClass
            {
                Name = "ABSTRACT-SYNTAX",
                Fields = [identifier.Field, type.Field, property.Field],
                SyntaxTokens = [
                    "&Type", "IDENTIFIED", "BY", "&id",
                    "[", "HAS", "PROPERTY", "&property", "]"
                ]
            },
            FieldSyntax = [identifier, type, property],
            SyntaxTemplate = [
                this.CreateFieldTemplate("&Type"),
                this.CreateLiteralTemplate("IDENTIFIED"),
                this.CreateLiteralTemplate("BY"),
                this.CreateFieldTemplate("&id"),
                new ParsedSyntaxTemplateItem
                {
                    OptionalGroup = true,
                    Children = [
                        this.CreateLiteralTemplate("HAS"),
                        this.CreateLiteralTemplate("PROPERTY"),
                        this.CreateFieldTemplate("&property")
                    ]
                }
            ]
        };
        new SyntaxGrammarCompiler(this.Diagnostics).CompileSyntax(syntax);
        return syntax;
    }

    internal ParsedObjectClassFieldSyntax CreateIdentifierField()
    {
        ParsedTypeSyntax governor = this.CreateResolvedTypeKind(
            AsnTypeKind.ObjectIdentifier);
        return new ParsedObjectClassFieldSyntax
        {
            Field = new AsnObjectClassField
            {
                Name = "&id",
                Kind = AsnObjectFieldKind.FixedTypeValue,
                Type = governor.Type,
                Unique = true
            },
            GovernorTypeSyntax = governor
        };
    }

    internal ParsedObjectClassFieldSyntax CreateTypeField()
    {
        return new ParsedObjectClassFieldSyntax
        {
            Field = new AsnObjectClassField
            {
                Name = "&Type",
                Kind = AsnObjectFieldKind.Type
            }
        };
    }

    internal ParsedObjectClassFieldSyntax CreatePropertyField()
    {
        AsnNamedValue namedBit = new AsnNamedValue
        {
            Name = "handles-invalid-encodings",
            Value = new AsnValue
            {
                Kind = AsnValueKind.Integer,
                HasIntegerValue = true,
                IntegerValue = System.Numerics.BigInteger.Zero
            }
        };
        AsnType bitString = new AsnType
        {
            Kind = AsnTypeKind.BitString,
            NamedValues = [namedBit]
        };
        ParsedTypeSyntax governor = this.CreateResolvedType(bitString);
        governor.RootNamedValues = [namedBit];
        governor.RootNamedValueSpans = [null];
        AsnValue defaultValue = new AsnValue { Kind = AsnValueKind.BitString };
        ParsedValueSyntax defaultSyntax = new ParsedValueSyntax
        {
            Value = defaultValue,
            BitStringValue = new AsnBitStringValue(),
            SourceTokens = this.CreateEmptySetTokens()
        };
        return new ParsedObjectClassFieldSyntax
        {
            Field = new AsnObjectClassField
            {
                Name = "&property",
                Kind = AsnObjectFieldKind.FixedTypeValue,
                Type = bitString,
                HasDefaultSetting = true,
                DefaultSetting = new AsnObjectFieldSetting
                {
                    FieldName = "&property",
                    Value = defaultValue
                }
            },
            GovernorTypeSyntax = governor,
            DefaultValueSyntax = defaultSyntax,
            DefaultTokens = defaultSyntax.SourceTokens
        };
    }

    internal ParsedTypeSyntax CreateResolvedTypeKind(AsnTypeKind kind)
    {
        return this.CreateResolvedType(new AsnType { Kind = kind });
    }

    internal ParsedTypeSyntax CreateResolvedType(AsnType type)
    {
        ParsedTypeSyntax syntax = new ParsedTypeSyntax { Type = type };
        syntax.Resolution = new ResolvedTypeSyntax
        {
            SourceSyntax = syntax,
            SourceType = type,
            EffectiveType = type,
            EffectiveKind = type.Kind,
            Success = true
        };
        return syntax;
    }

    internal ParsedSyntaxTemplateItem CreateFieldTemplate(string name)
    {
        return new ParsedSyntaxTemplateItem
        {
            Text = name,
            FieldReference = true
        };
    }

    internal ParsedSyntaxTemplateItem CreateLiteralTemplate(string text)
    {
        return new ParsedSyntaxTemplateItem { Text = text };
    }

    internal AsnToken[] CreateEmptySetTokens()
    {
        return [
            new AsnToken { Kind = AsnTokenKind.LeftBrace, Text = "{" },
            new AsnToken { Kind = AsnTokenKind.RightBrace, Text = "}" }
        ];
    }

    internal ParsedObjectClassFieldSyntax CopyFieldSyntax(
        ParsedObjectClassFieldSyntax source)
    {
        AsnObjectClassField copied = new AsnObjectClassField
        {
            Name = source.Field.Name,
            Kind = source.Field.Kind,
            Type = source.Field.Type,
            ObjectClass = source.Field.ObjectClass,
            GovernorFieldPath = source.Field.GovernorFieldPath,
            Optional = source.Field.Optional,
            Unique = source.Field.Unique,
            HasDefaultSetting = source.Field.HasDefaultSetting,
            DefaultSetting = source.Field.DefaultSetting
        };
        return new ParsedObjectClassFieldSyntax
        {
            Field = copied,
            GovernorTypeSyntax = source.GovernorTypeSyntax,
            GovernorFieldPath = source.GovernorFieldPath,
            GovernorObjectClassModule = source.GovernorObjectClassModule,
            GovernorObjectClassName = source.GovernorObjectClassName,
            DefaultTypeSyntax = source.DefaultTypeSyntax,
            DefaultValueSyntax = source.DefaultValueSyntax,
            DefaultTokens = source.DefaultTokens,
            SourceTokens = source.SourceTokens,
            Span = source.Span
        };
    }
}
