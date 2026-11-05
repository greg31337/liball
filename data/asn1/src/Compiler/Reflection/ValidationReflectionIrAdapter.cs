namespace asn1;

internal class ValidationReflectionIrAdapter
{
    internal System.Collections.Generic.List<AsnType> TypeSources =
        new System.Collections.Generic.List<AsnType>();
    internal System.Collections.Generic.List<LoweredTypeIr> TypeResults =
        new System.Collections.Generic.List<LoweredTypeIr>();

    internal LoweredCompilationIr LowerValidation(AsnValidationResult validation)
    {
        LoweredModuleIr[] modules = new LoweredModuleIr[validation.Modules.Length];
        int index = 0;
        while (index < modules.Length)
        {
            modules[index] = this.LowerModule(validation.Modules[index]);
            index = index + 1;
        }

        LoweredCompilationIr result = new LoweredCompilationIr
        {
            Modules = modules
        };
        new DeterministicIrIdentityAssigner().AssignCompilationIdentities(result);
        return result;
    }

    internal LoweredModuleIr LowerModule(AsnModule source)
    {
        LoweredImportIr[] imports = new LoweredImportIr[source.Imports.Length];
        int index = 0;
        while (index < imports.Length)
        {
            imports[index] = new LoweredImportIr
            {
                SourceSpan = source.Imports[index].Span,
                ModuleName = source.Imports[index].ModuleName,
                Symbols = this.CopyStrings(source.Imports[index].Symbols)
            };
            index = index + 1;
        }

        LoweredAssignmentIr[] assignments =
            new LoweredAssignmentIr[source.Assignments.Length];
        index = 0;
        while (index < assignments.Length)
        {
            assignments[index] = this.LowerAssignment(
                source.Assignments[index],
                source.Name);
            index = index + 1;
        }

        return new LoweredModuleIr
        {
            Name = source.Name,
            SourceSpan = source.Span,
            Imports = imports,
            Assignments = assignments,
            Instructions = this.LowerInstructions(source.EncodingInstructions)
        };
    }

    internal LoweredAssignmentIr LowerAssignment(
        AsnAssignment source,
        string moduleName)
    {
        LoweredTypeIr? type = source.Type == null
            ? null
            : this.LowerType(source.Type);
        if (type != null && type.Name.Length == 0)
        {
            type.Name = source.Name;
        }

        return new LoweredAssignmentIr
        {
            Name = source.Name,
            QualifiedName = moduleName + "." + source.Name,
            Kind = source.Kind,
            Exported = true,
            Parameterized = source.Parameters.Length > 0,
            Parameters = this.LowerParameters(source.Parameters),
            Type = type,
            Value = source.Value == null
                ? null
                : this.LowerValue(source.Value),
            ObjectClass = source.ObjectClass == null
                ? null
                : this.LowerObjectClass(source.ObjectClass),
            Object = source.Object == null
                ? null
                : this.LowerObject(source.Object),
            ObjectSet = source.ObjectSet == null
                ? null
                : this.LowerObjectSet(source.ObjectSet),
            SourceSpan = source.Span
        };
    }

    internal LoweredParameterIr[] LowerParameters(AsnParameter[] source)
    {
        LoweredParameterIr[] result = new LoweredParameterIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new LoweredParameterIr
            {
                Name = source[index].Name,
                Kind = source[index].Kind,
                GovernorTypeKind = source[index].GovernorType == null
                    ? AsnTypeKind.Unknown
                    : (source[index].GovernorType
                        ?? throw new System.InvalidOperationException()).Kind,
                GovernorObjectClassName = source[index].GovernorObjectClass == null
                    ? ""
                    : (source[index].GovernorObjectClass
                        ?? throw new System.InvalidOperationException()).Name
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredTypeIr LowerType(AsnType source)
    {
        int existing = this.FindTypeIndex(source);
        if (existing >= 0)
        {
            return this.TypeResults[existing];
        }

        LoweredTypeIr result = new LoweredTypeIr
        {
            Name = source.Name,
            Kind = source.Kind,
            Extensible = source.Extensible
        };
        this.TypeSources.Add(source);
        this.TypeResults.Add(result);
        result.Tags = source.Tag == null
            ? []
            : [new LoweredTagIr
            {
                Class = source.Tag.Class,
                Mode = source.Tag.Mode,
                Number = source.Tag.Number
            }];
        result.Fields = this.LowerFields(source.Components);
        result.ElementType = source.ElementType == null
            ? null
            : this.LowerType(source.ElementType);
        result.NamedValues = this.LowerNamedValues(source.NamedValues);
        result.Constraints = this.LowerConstraints(source.Constraints);
        result.Extensions = this.LowerExtensionPartition(
            source.Extensible,
            result.Fields);
        return result;
    }

    internal int FindTypeIndex(AsnType source)
    {
        int index = 0;
        while (index < this.TypeSources.Count)
        {
            if (this.TypeSources[index] == source)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal LoweredFieldIr[] LowerFields(AsnComponent[] source)
    {
        LoweredFieldIr[] result = new LoweredFieldIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new LoweredFieldIr
            {
                Name = source[index].Name,
                Type = source[index].Type == null
                    ? null
                    : this.LowerType(source[index].Type
                        ?? throw new System.InvalidOperationException()),
                Optional = source[index].Optional,
                HasDefaultValue = source[index].HasDefaultValue,
                DefaultValue = source[index].DefaultValue == null
                    ? null
                    : this.LowerValue(source[index].DefaultValue
                        ?? throw new System.InvalidOperationException()),
                Extension = source[index].Extension
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredExtensionPartitionIr LowerExtensionPartition(
        bool extensible,
        LoweredFieldIr[] fields)
    {
        int rootCount = 0;
        int index = 0;
        while (index < fields.Length)
        {
            if (!fields[index].Extension)
            {
                rootCount = rootCount + 1;
            }

            index = index + 1;
        }

        LoweredFieldIr[] roots = new LoweredFieldIr[rootCount];
        LoweredFieldIr[] additions =
            new LoweredFieldIr[fields.Length - rootCount];
        int rootIndex = 0;
        int additionIndex = 0;
        index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Extension)
            {
                additions[additionIndex] = fields[index];
                additionIndex = additionIndex + 1;
            }
            else
            {
                roots[rootIndex] = fields[index];
                rootIndex = rootIndex + 1;
            }

            index = index + 1;
        }

        return new LoweredExtensionPartitionIr
        {
            Extensible = extensible,
            RootFields = roots,
            AdditionalFields = additions
        };
    }

    internal LoweredNamedValueIr[] LowerNamedValues(AsnNamedValue[] source)
    {
        LoweredNamedValueIr[] result = new LoweredNamedValueIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new LoweredNamedValueIr
            {
                Name = source[index].Name,
                Value = source[index].Value == null
                    ? null
                    : this.LowerValue(source[index].Value
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredValueIr LowerValue(AsnValue source)
    {
        LoweredValueIr[] items = new LoweredValueIr[source.Items.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = this.LowerValue(source.Items[index]);
            index = index + 1;
        }

        IrValuePayloadCloner payloads = new IrValuePayloadCloner();
        return new LoweredValueIr
        {
            Kind = source.Kind,
            HasIntegerValue = source.HasIntegerValue,
            IntegerValue = source.IntegerValue,
            TextValue = source.TextValue,
            Octets = this.CopyBytes(source.Octets),
            BitStringValue = payloads.CloneBitString(source.BitStringValue),
            ObjectIdentifierValue = payloads.CloneObjectIdentifier(
                source.ObjectIdentifierValue),
            RealValue = payloads.CloneReal(source.RealValue),
            TimeValue = payloads.CloneTime(source.TimeValue),
            Items = items,
            NamedValues = this.LowerNamedValues(source.NamedValues)
        };
    }

    internal LoweredConstraintIr[] LowerConstraints(AsnConstraint[] source)
    {
        LoweredConstraintIr[] result = new LoweredConstraintIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.LowerConstraint(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal LoweredConstraintIr LowerConstraint(AsnConstraint source)
    {
        LoweredConstraintNodeIr? root = source.Elements.Length == 0
            ? null
            : this.LowerConstraintElement(source.Elements[0]);
        LoweredConstraintNodeIr? extension = source.Extensible
            && source.Elements.Length > 1
            ? this.LowerConstraintElement(source.Elements[1])
            : null;
        return new LoweredConstraintIr
        {
            DomainKind = AsnTypeKind.Unknown,
            Root = root,
            Extension = extension,
            RootIntervals = root == null
                || !root.HasLowerBound && !root.HasUpperBound
                ? []
                : [this.CreateInterval(root)],
            ExtensionIntervals = extension == null
                || !extension.HasLowerBound && !extension.HasUpperBound
                ? []
                : [this.CreateInterval(extension)],
            Extensible = source.Extensible,
            SourceSpan = source.Span
        };
    }

    internal LoweredConstraintNodeIr LowerConstraintElement(
        AsnConstraintElement source)
    {
        LoweredConstraintNodeIr[] children =
            new LoweredConstraintNodeIr[source.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.LowerConstraintElement(source.Children[index]);
            index = index + 1;
        }

        return new LoweredConstraintNodeIr
        {
            Kind = source.Kind,
            Name = source.Name,
            Text = source.Text,
            Value = source.Value == null ? null : this.LowerValue(source.Value),
            LowerValue = source.LowerValue == null
                ? null
                : this.LowerValue(source.LowerValue),
            UpperValue = source.UpperValue == null
                ? null
                : this.LowerValue(source.UpperValue),
            LowerBound = source.LowerBound,
            UpperBound = source.UpperBound,
            HasLowerBound = source.HasLowerBound,
            HasUpperBound = source.HasUpperBound,
            LowerEndpointIncluded = source.LowerEndpointIncluded,
            UpperEndpointIncluded = source.UpperEndpointIncluded,
            Children = children
        };
    }

    internal LoweredIntervalIr CreateInterval(LoweredConstraintNodeIr source)
    {
        return new LoweredIntervalIr
        {
            LowerBound = source.LowerBound,
            UpperBound = source.UpperBound,
            HasLowerBound = source.HasLowerBound,
            HasUpperBound = source.HasUpperBound
        };
    }

    internal LoweredObjectClassIr LowerObjectClass(AsnObjectClass source)
    {
        LoweredObjectClassFieldIr[] fields =
            new LoweredObjectClassFieldIr[source.Fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            AsnObjectClassField field = source.Fields[index];
            fields[index] = new LoweredObjectClassFieldIr
            {
                Name = field.Name,
                Kind = field.Kind,
                Type = field.Type == null ? null : this.LowerType(field.Type),
                GovernorObjectClassName = field.ObjectClass == null
                    ? ""
                    : field.ObjectClass.Name,
                GovernorFieldPath = this.CopyStrings(field.GovernorFieldPath),
                Optional = field.Optional,
                Unique = field.Unique,
                HasDefaultSetting = field.HasDefaultSetting,
                DefaultSetting = field.DefaultSetting == null
                    ? null
                    : this.LowerObjectSetting(field.DefaultSetting)
            };
            index = index + 1;
        }

        return new LoweredObjectClassIr
        {
            Name = source.Name,
            Fields = fields,
            SyntaxTokens = this.CopyStrings(source.SyntaxTokens)
        };
    }

    internal LoweredObjectIr LowerObject(AsnInformationObject source)
    {
        LoweredObjectFieldSettingIr[] settings =
            new LoweredObjectFieldSettingIr[source.FieldSettings.Length];
        int index = 0;
        while (index < settings.Length)
        {
            settings[index] = this.LowerObjectSetting(source.FieldSettings[index]);
            index = index + 1;
        }

        return new LoweredObjectIr
        {
            StableId = source.StableId,
            Name = source.Name,
            ObjectClassName = source.ObjectClass == null
                ? ""
                : source.ObjectClass.Name,
            FieldSettings = settings
        };
    }

    internal LoweredObjectFieldSettingIr LowerObjectSetting(
        AsnObjectFieldSetting source)
    {
        return new LoweredObjectFieldSettingIr
        {
            FieldName = source.FieldName,
            Type = source.Type == null ? null : this.LowerType(source.Type),
            ValueSetType = source.ValueSetType == null
                ? null
                : this.LowerType(source.ValueSetType),
            Value = source.Value == null ? null : this.LowerValue(source.Value),
            ObjectStableId = source.Object == null ? "" : source.Object.StableId,
            ObjectSetStableId = source.ObjectSet == null
                ? ""
                : source.ObjectSet.StableId
        };
    }

    internal LoweredObjectSetIr LowerObjectSet(AsnInformationObjectSet source)
    {
        AsnInformationObject[] publicRoots = source.RootObjects.Length == 0
            ? source.Objects
            : source.RootObjects;
        LoweredObjectIr[] roots = new LoweredObjectIr[publicRoots.Length];
        LoweredObjectIr[] additions =
            new LoweredObjectIr[source.AdditionalObjects.Length];
        int index = 0;
        while (index < roots.Length)
        {
            roots[index] = this.LowerObject(publicRoots[index]);
            index = index + 1;
        }

        index = 0;
        while (index < additions.Length)
        {
            additions[index] = this.LowerObject(source.AdditionalObjects[index]);
            index = index + 1;
        }

        return new LoweredObjectSetIr
        {
            StableId = source.StableId,
            Name = source.Name,
            ObjectClassName = source.ObjectClass == null
                ? ""
                : source.ObjectClass.Name,
            Extensible = source.Extensible,
            RootObjects = roots,
            AdditionalObjects = additions,
            Table = source.DispatchEntries.Length == 0
                ? null
                : this.LowerDispatchTable(source.DispatchEntries)
        };
    }

    internal LoweredObjectTableIr LowerDispatchTable(
        AsnOpenTypeDispatchEntry[] source)
    {
        LoweredObjectTableEntryIr[] entries =
            new LoweredObjectTableEntryIr[source.Length];
        int index = 0;
        while (index < entries.Length)
        {
            entries[index] = new LoweredObjectTableEntryIr
            {
                StableId = source[index].StableId,
                Key = source[index].Key == null
                    ? null
                    : this.LowerValue(source[index].Key
                        ?? throw new System.InvalidOperationException()),
                Type = source[index].Type == null
                    ? null
                    : this.LowerType(source[index].Type
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        return new LoweredObjectTableIr
        {
            Kind = AsnConstraintKind.SimpleTable,
            Entries = entries
        };
    }

    internal LoweredInstructionIr[] LowerInstructions(
        AsnEncodingInstruction[] source)
    {
        LoweredInstructionIr[] result = new LoweredInstructionIr[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new LoweredInstructionIr
            {
                EncodingReference = this.InferEncodingReference(source[index].Kind),
                Name = source[index].Name,
                Kind = source[index].Kind,
                Negated = source[index].Negated,
                Arguments = this.CopyStrings(source[index].Arguments),
                Targets = this.CopyStrings(source[index].Targets),
                SourceSpan = source[index].Span
            };
            index = index + 1;
        }

        return result;
    }

    internal string InferEncodingReference(AsnInstructionKind kind)
    {
        if (kind == AsnInstructionKind.PerApplication)
        {
            return "PER";
        }

        return kind == AsnInstructionKind.Ecn ? "ECN" : "XER";
    }

    internal string[] CopyStrings(string[] source)
    {
        string[] result = new string[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = source[index];
            index = index + 1;
        }

        return result;
    }
}
