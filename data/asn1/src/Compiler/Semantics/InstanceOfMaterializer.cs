namespace asn1;

internal class InstanceOfMaterializer
{
    internal ValueResolver ValueResolver;
    internal ObjectClassBinder ObjectClassBinder;
    internal DiagnosticBag Diagnostics;
    internal System.Collections.Generic.List<ResolvedTypeSyntax> Visited =
        new System.Collections.Generic.List<ResolvedTypeSyntax>();
    internal bool Success = true;

    internal InstanceOfMaterializer(
        ValueResolver valueResolver,
        ObjectClassBinder objectClassBinder)
    {
        this.ValueResolver = valueResolver == null
            ? throw new System.ArgumentNullException("valueResolver")
            : valueResolver;
        this.ObjectClassBinder = objectClassBinder == null
            ? throw new System.ArgumentNullException("objectClassBinder")
            : objectClassBinder;
        this.Diagnostics = valueResolver.Diagnostics;
    }

    internal bool MaterializeTypes(SymbolTable[] tables)
    {
        this.Visited.Clear();
        this.Success = true;
        int tableIndex = 0;
        while (tableIndex < tables.Length)
        {
            SymbolTable module = tables[tableIndex];
            int typeIndex = 0;
            while (typeIndex < module.TypeSymbols.Length)
            {
                ResolvedTypeSyntax? type = module.TypeSymbols[typeIndex].ResolvedType;
                if (type != null)
                {
                    this.MaterializeType(type, module);
                }

                typeIndex = typeIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < module.ValueSymbols.Length)
            {
                ResolvedTypeSyntax? type = module.ValueSymbols[valueIndex].GoverningType;
                if (type != null)
                {
                    this.MaterializeType(type, module);
                }

                valueIndex = valueIndex + 1;
            }

            int classIndex = 0;
            while (classIndex < module.ObjectClassSymbols.Length)
            {
                ParsedObjectClassSyntax? objectClass = module.ObjectClassSymbols[classIndex]
                    .ResolvedObjectClassSyntax;
                if (objectClass != null)
                {
                    this.MaterializeClassFields(objectClass, module);
                }

                classIndex = classIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return this.Success;
    }

    internal bool MaterializeType(ResolvedTypeSyntax type, SymbolTable module)
    {
        if (this.Visited.Contains(type))
        {
            return type.Success;
        }

        this.Visited.Add(type);
        ParsedTypeSyntax? syntax = type.SourceSyntax;
        if (syntax != null && syntax.Type.Kind == AsnTypeKind.InstanceOf)
        {
            this.MaterializeInstance(type, syntax, module);
        }

        if (type.AliasTarget != null)
        {
            this.MaterializeType(type.AliasTarget, module);
            type.EffectiveType = type.AliasTarget.EffectiveType;
            type.EffectiveKind = type.AliasTarget.EffectiveKind;
            type.EffectiveDeclaration = type.AliasTarget.EffectiveDeclaration;
            type.Components = type.AliasTarget.Components;
            type.ObjectClassField = type.AliasTarget.ObjectClassField;
            type.Success = type.AliasTarget.Success;
        }

        int componentIndex = 0;
        while (componentIndex < type.Components.Length)
        {
            ResolvedTypeSyntax? component = type.Components[componentIndex].TypeResolution;
            if (component != null)
            {
                this.MaterializeType(component, module);
            }

            componentIndex = componentIndex + 1;
        }

        if (type.InnerType != null)
        {
            this.MaterializeType(type.InnerType, module);
        }

        return type.Success;
    }

    internal void MaterializeClassFields(
        ParsedObjectClassSyntax objectClass,
        SymbolTable module)
    {
        int index = 0;
        while (index < objectClass.FieldSyntax.Length)
        {
            ResolvedTypeSyntax? type = objectClass.FieldSyntax[index]
                .GovernorTypeSyntax?.Resolution;
            if (type != null)
            {
                this.MaterializeType(type, module);
            }

            index = index + 1;
        }
    }

    internal void MaterializeInstance(
        ResolvedTypeSyntax resolution,
        ParsedTypeSyntax syntax,
        SymbolTable module)
    {
        ParsedObjectClassSyntax? objectClass = this.ObjectClassBinder.ResolveClassReference(
            module,
            syntax.ReferenceModule,
            syntax.ReferenceName);
        if (objectClass == null)
        {
            this.AddDiagnostic(
                InstanceOfDiagnosticCodes.UnknownObjectClass,
                "The object class '" + this.FormatReference(
                    syntax.ReferenceModule,
                    syntax.ReferenceName) + "' used by INSTANCE OF could not be resolved.",
                syntax.Span);
            resolution.Success = false;
            return;
        }

        ParsedObjectClassFieldSyntax? uniqueField = this.FindUniqueField(objectClass);
        if (uniqueField == null
            || uniqueField.GovernorTypeSyntax == null
            || uniqueField.GovernorTypeSyntax.Resolution == null
            || uniqueField.GovernorTypeSyntax.Resolution.EffectiveType == null)
        {
            this.AddDiagnostic(
                InstanceOfDiagnosticCodes.MissingUniqueField,
                "The object class '" + objectClass.ObjectClass.Name
                    + "' used by INSTANCE OF requires a resolvable UNIQUE value field.",
                syntax.Span);
            resolution.Success = false;
            return;
        }

        ParsedObjectClassFieldSyntax? typeField = this.FindTypeField(objectClass);
        if (typeField == null)
        {
            this.AddDiagnostic(
                InstanceOfDiagnosticCodes.MissingTypeField,
                "The object class '" + objectClass.ObjectClass.Name
                    + "' used by INSTANCE OF requires a type field.",
                syntax.Span);
            resolution.Success = false;
            return;
        }

        ResolvedTypeSyntax identifierType = uniqueField.GovernorTypeSyntax.Resolution;
        ResolvedTypeSyntax openType = this.CreateOpenTypeResolution(
            resolution,
            objectClass,
            typeField.Field);
        ResolvedTypeSyntax taggedOpenType = this.CreateTaggedOpenTypeResolution(
            resolution,
            openType);
        ResolvedComponentSyntax[] components = [
            new ResolvedComponentSyntax
            {
                Component = new AsnComponent
                {
                    Name = "type-id",
                    Type = identifierType.EffectiveType
                },
                TypeResolution = identifierType
            },
            new ResolvedComponentSyntax
            {
                Component = new AsnComponent
                {
                    Name = "value",
                    Type = taggedOpenType.SourceType
                },
                TypeResolution = taggedOpenType
            }
        ];
        resolution.Components = components;
        resolution.ObjectClassField = openType.ObjectClassField;
        resolution.EffectiveType = this.ValueResolver.TypeResolver.CopyType(
            syntax.Type,
            syntax.Type.ElementType,
            [components[0].Component ?? new AsnComponent(),
                components[1].Component ?? new AsnComponent()]);
        resolution.EffectiveKind = AsnTypeKind.InstanceOf;
        resolution.Success = true;
    }

    internal ResolvedTypeSyntax CreateOpenTypeResolution(
        ResolvedTypeSyntax owner,
        ParsedObjectClassSyntax objectClass,
        AsnObjectClassField typeField)
    {
        AsnType openType = new AsnType
        {
            Kind = AsnTypeKind.OpenType,
            Name = objectClass.ObjectClass.Name + "." + typeField.Name
        };
        return new ResolvedTypeSyntax
        {
            SourceType = openType,
            EffectiveType = openType,
            EffectiveKind = AsnTypeKind.OpenType,
            SourceDeclaration = owner.SourceDeclaration,
            EffectiveDeclaration = owner.EffectiveDeclaration,
            ObjectClassField = new ResolvedObjectClassFieldSyntax
            {
                ObjectClassSymbol = this.FindClassSymbol(objectClass),
                ObjectClassSyntax = objectClass,
                Field = typeField
            },
            Success = true
        };
    }

    internal ResolvedTypeSyntax CreateTaggedOpenTypeResolution(
        ResolvedTypeSyntax owner,
        ResolvedTypeSyntax openType)
    {
        AsnType tagged = new AsnType
        {
            Kind = AsnTypeKind.Tagged,
            Tag = new AsnTag
            {
                Class = AsnTagClass.ContextSpecific,
                Mode = AsnTaggingMode.Explicit,
                Number = System.Numerics.BigInteger.Zero
            },
            ElementType = openType.EffectiveType
        };
        return new ResolvedTypeSyntax
        {
            SourceType = tagged,
            EffectiveType = tagged,
            EffectiveKind = AsnTypeKind.OpenType,
            SourceDeclaration = owner.SourceDeclaration,
            EffectiveDeclaration = owner.EffectiveDeclaration,
            InnerType = openType,
            ObjectClassField = openType.ObjectClassField,
            Success = true
        };
    }

    internal ParsedObjectClassFieldSyntax? FindUniqueField(
        ParsedObjectClassSyntax objectClass)
    {
        int index = 0;
        while (index < objectClass.FieldSyntax.Length)
        {
            ParsedObjectClassFieldSyntax field = objectClass.FieldSyntax[index];
            if (field.Field.Kind == AsnObjectFieldKind.FixedTypeValue
                && field.Field.Unique)
            {
                return field;
            }

            index = index + 1;
        }

        return null;
    }

    internal ParsedObjectClassFieldSyntax? FindTypeField(
        ParsedObjectClassSyntax objectClass)
    {
        int index = 0;
        while (index < objectClass.FieldSyntax.Length)
        {
            if (objectClass.FieldSyntax[index].Field.Kind == AsnObjectFieldKind.Type)
            {
                return objectClass.FieldSyntax[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal SymbolTableEntry? FindClassSymbol(ParsedObjectClassSyntax objectClass)
    {
        int tableIndex = 0;
        while (tableIndex < this.ValueResolver.Tables.Length)
        {
            SymbolTable table = this.ValueResolver.Tables[tableIndex];
            int symbolIndex = 0;
            while (symbolIndex < table.ObjectClassSymbols.Length)
            {
                SymbolTableEntry symbol = table.ObjectClassSymbols[symbolIndex];
                if (symbol.ResolvedObjectClassSyntax == objectClass)
                {
                    return symbol;
                }

                symbolIndex = symbolIndex + 1;
            }

            tableIndex = tableIndex + 1;
        }

        return null;
    }

    internal string FormatReference(string moduleName, string referenceName)
    {
        return moduleName.Length == 0
            ? referenceName
            : moduleName + "." + referenceName;
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
