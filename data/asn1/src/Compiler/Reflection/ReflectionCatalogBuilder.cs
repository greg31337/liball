namespace asn1;

internal class ReflectionCatalogBuilder
{
    internal System.Collections.Generic.List<LoweredTypeIr> TypeSources =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal System.Collections.Generic.List<AsnTypeDescriptor> TypeResults =
        new System.Collections.Generic.List<AsnTypeDescriptor>();
    internal System.Collections.Generic.List<LoweredTypeIr> PublicTypeSources =
        new System.Collections.Generic.List<LoweredTypeIr>();
    internal System.Collections.Generic.List<AsnType> PublicTypeResults =
        new System.Collections.Generic.List<AsnType>();
    internal AsnPerInstructionRegistration[] PerInstructionRegistrations = [];

    internal AsnReflectionCatalog BuildCatalog(LoweredCompilationIr compilation)
    {
        if (compilation == null)
        {
            throw new System.ArgumentNullException("compilation");
        }

        this.TypeSources.Clear();
        this.TypeResults.Clear();
        this.PublicTypeSources.Clear();
        this.PublicTypeResults.Clear();
        this.PerInstructionRegistrations =
            this.ClonePerInstructionRegistrations(
                compilation.PerInstructionRegistrations);
        AsnModuleDescriptor[] modules =
            new AsnModuleDescriptor[compilation.Modules.Length];
        int assignmentCount = 0;
        int moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            assignmentCount = assignmentCount
                + compilation.Modules[moduleIndex].Assignments.Length;
            moduleIndex = moduleIndex + 1;
        }

        AsnAssignmentDescriptor[] assignments =
            new AsnAssignmentDescriptor[assignmentCount];
        int assignmentIndex = 0;
        moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            modules[moduleIndex] = this.BuildModule(
                compilation.Modules[moduleIndex]);
            int moduleAssignmentIndex = 0;
            while (moduleAssignmentIndex
                < modules[moduleIndex].Assignments.Length)
            {
                assignments[assignmentIndex] =
                    modules[moduleIndex].Assignments[moduleAssignmentIndex];
                assignmentIndex = assignmentIndex + 1;
                moduleAssignmentIndex = moduleAssignmentIndex + 1;
            }

            moduleIndex = moduleIndex + 1;
        }

        return new AsnReflectionCatalog
        {
            StableId = "reflection:" + compilation.StableId,
            Modules = modules,
            Assignments = assignments
        };
    }

    internal AsnModuleDescriptor BuildModule(LoweredModuleIr source)
    {
        AsnAssignmentDescriptor[] assignments =
            new AsnAssignmentDescriptor[source.Assignments.Length];
        int typeCount = 0;
        int valueCount = 0;
        int exportCount = 0;
        int index = 0;
        while (index < source.Assignments.Length)
        {
            LoweredAssignmentIr assignment = source.Assignments[index];
            assignments[index] = this.BuildAssignment(assignment);
            if (assignment.Kind == AsnAssignmentKind.Type
                && assignment.Type != null)
            {
                typeCount = typeCount + 1;
            }

            if (assignment.Kind == AsnAssignmentKind.Value
                && assignment.Value != null)
            {
                valueCount = valueCount + 1;
            }

            if (assignment.Exported)
            {
                exportCount = exportCount + 1;
            }

            index = index + 1;
        }

        AsnTypeDescriptor[] types = new AsnTypeDescriptor[typeCount];
        AsnValueDescriptor[] values = new AsnValueDescriptor[valueCount];
        string[] exports = new string[exportCount];
        int typeIndex = 0;
        int valueIndex = 0;
        int exportIndex = 0;
        index = 0;
        while (index < assignments.Length)
        {
            if (assignments[index].Kind == AsnAssignmentKind.Type
                && assignments[index].Type != null)
            {
                types[typeIndex] = assignments[index].Type
                    ?? throw new System.InvalidOperationException();
                typeIndex = typeIndex + 1;
            }

            if (assignments[index].Kind == AsnAssignmentKind.Value
                && assignments[index].Value != null)
            {
                values[valueIndex] = assignments[index].Value
                    ?? throw new System.InvalidOperationException();
                valueIndex = valueIndex + 1;
            }

            if (assignments[index].Exported)
            {
                exports[exportIndex] = assignments[index].Name;
                exportIndex = exportIndex + 1;
            }

            index = index + 1;
        }

        AsnImport[] imports = this.BuildImports(source.Imports);
        return new AsnModuleDescriptor
        {
            StableId = source.StableId,
            Name = source.Name,
            GeneratedName = source.Name,
            ObjectIdentifier = source.ObjectIdentifier,
            Iri = source.Iri,
            VersionModule = source.VersionModule,
            Version = source.Version,
            Imports = imports,
            Exports = exports,
            Assignments = assignments,
            Types = types,
            Values = values,
            Encodings = this.BuildEncodings(
                source.Instructions,
                source.StableId,
                [],
                false),
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations),
            SourceModule = this.BuildSourceModule(
                source,
                imports,
                assignments)
        };
    }

    internal AsnImport[] BuildImports(LoweredImportIr[] source)
    {
        AsnImport[] result = new AsnImport[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnImport
            {
                StableId = source[index].StableId,
                ModuleName = source[index].ModuleName,
                GeneratedModuleName = source[index].ModuleName,
                Symbols = this.CopyStrings(source[index].Symbols),
                Span = this.CloneSpan(source[index].SourceSpan),
                Derivations = this.BuildDerivations(source[index].Derivations)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnAssignmentDescriptor BuildAssignment(LoweredAssignmentIr source)
    {
        AsnTypeDescriptor? type = source.Type == null
            ? null
            : this.BuildTypeDescriptor(source.Type);
        if (type != null && (type.Name.Length == 0
            || source.Kind == AsnAssignmentKind.Type))
        {
            type.Name = source.Name;
            type.GeneratedName = source.Name;
            if (type.SourceType != null)
            {
                type.SourceType.Name = source.Name;
            }
        }

        AsnValueDescriptor? value = source.Value == null
            ? null
            : this.BuildValueDescriptor(source.Value, source.Name);
        AsnObjectTableDescriptor[] tables = type == null
            ? []
            : type.ObjectTables;
        if (source.ObjectSet != null && source.ObjectSet.Table != null)
        {
            tables = [this.BuildObjectTable(source.ObjectSet.Table)];
        }

        return new AsnAssignmentDescriptor
        {
            StableId = source.StableId,
            Name = source.Name,
            QualifiedName = source.QualifiedName,
            GeneratedName = source.Name,
            Kind = source.Kind,
            Exported = source.Exported,
            Parameterized = source.Parameterized,
            Parameters = this.BuildParameters(source.Parameters),
            Type = type,
            Value = value,
            ObjectClass = source.ObjectClass == null
                ? null
                : this.BuildObjectClass(source.ObjectClass),
            Object = source.Object == null
                ? null
                : this.BuildObject(source.Object),
            ObjectSet = source.ObjectSet == null
                ? null
                : this.BuildObjectSet(source.ObjectSet),
            ObjectTables = tables,
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations)
        };
    }

    internal AsnParameterDescriptor[] BuildParameters(LoweredParameterIr[] source)
    {
        AsnParameterDescriptor[] result =
            new AsnParameterDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnParameterDescriptor
            {
                StableId = source[index].StableId,
                Name = source[index].Name,
                GeneratedName = source[index].Name,
                Kind = source[index].Kind,
                GovernorTypeKind = source[index].GovernorTypeKind,
                GovernorObjectClassName = source[index].GovernorObjectClassName,
                SourceSpan = this.CloneSpan(source[index].SourceSpan),
                Derivations = this.BuildDerivations(source[index].Derivations)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnTypeDescriptor BuildTypeDescriptor(LoweredTypeIr source)
    {
        int existing = this.FindTypeDescriptorIndex(source);
        if (existing >= 0)
        {
            return this.TypeResults[existing];
        }

        AsnTypeDescriptor result = new AsnTypeDescriptor
        {
            StableId = source.StableId,
            Name = source.Name,
            GeneratedName = source.Name,
            Kind = source.Kind,
            Extensible = source.Extensible,
            ParameterReference = source.ParameterReference,
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations)
        };
        this.TypeSources.Add(source);
        this.TypeResults.Add(result);
        result.Fields = this.BuildFields(source.Fields);
        result.RootFields = this.BuildFieldReferences(
            source.Extensions.RootFields,
            source.Fields,
            result.Fields);
        result.ExtensionFields = this.BuildFieldReferences(
            source.Extensions.AdditionalFields,
            source.Fields,
            result.Fields);
        result.ElementType = source.ElementType == null
            ? null
            : this.BuildTypeDescriptor(source.ElementType);
        result.Tags = this.BuildTags(source.Tags);
        result.NamedValues = this.BuildNamedValues(source.NamedValues);
        result.RootNamedValues = this.BuildNamedValues(
            source.Extensions.RootNamedValues);
        result.ExtensionNamedValues = this.BuildNamedValues(
            source.Extensions.AdditionalNamedValues);
        result.Constraints = this.BuildConstraints(source.Constraints);
        result.Encodings = this.BuildEncodings(
            source.Instructions,
            source.StableId,
            source.PerInstructionPlanSteps,
            true);
        result.ObjectTables = this.BuildObjectTables(source.ObjectTables);
        result.SourceType = this.BuildPublicType(source);
        return result;
    }

    internal int FindTypeDescriptorIndex(LoweredTypeIr source)
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

    internal AsnFieldDescriptor[] BuildFields(LoweredFieldIr[] source)
    {
        AsnFieldDescriptor[] result = new AsnFieldDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            LoweredFieldIr field = source[index];
            result[index] = new AsnFieldDescriptor
            {
                StableId = field.StableId,
                Name = field.Name,
                GeneratedName = field.Name,
                Index = index,
                Type = field.Type == null
                    ? null
                    : this.BuildTypeDescriptor(field.Type),
                Optional = field.Optional,
                HasDefaultValue = field.HasDefaultValue,
                DefaultValue = field.DefaultValue == null
                    ? null
                    : this.BuildValueDescriptor(field.DefaultValue, field.Name),
                Extension = field.Extension,
                ExtensionGroupIndex = field.ExtensionGroupIndex,
                Tags = this.BuildTags(field.Tags),
                Encodings = this.BuildEncodings(
                    field.Instructions,
                    field.StableId,
                    field.PerInstructionPlanSteps,
                    true),
                SourceSpan = this.CloneSpan(field.SourceSpan),
                Derivations = this.BuildDerivations(field.Derivations)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnFieldDescriptor[] BuildFieldReferences(
        LoweredFieldIr[] references,
        LoweredFieldIr[] sourceFields,
        AsnFieldDescriptor[] reflectedFields)
    {
        AsnFieldDescriptor[] result = new AsnFieldDescriptor[references.Length];
        int referenceIndex = 0;
        while (referenceIndex < references.Length)
        {
            int fieldIndex = 0;
            while (fieldIndex < sourceFields.Length
                && sourceFields[fieldIndex] != references[referenceIndex])
            {
                fieldIndex = fieldIndex + 1;
            }

            if (fieldIndex == sourceFields.Length)
            {
                throw new System.InvalidOperationException(
                    "An IR extension partition references an unknown field.");
            }

            result[referenceIndex] = reflectedFields[fieldIndex];
            referenceIndex = referenceIndex + 1;
        }

        return result;
    }

    internal AsnTagDescriptor[] BuildTags(LoweredTagIr[] source)
    {
        AsnTagDescriptor[] result = new AsnTagDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnTagDescriptor
            {
                StableId = source[index].StableId,
                Class = source[index].Class,
                Mode = source[index].Mode,
                Number = source[index].Number,
                Universal = source[index].Universal,
                AutomaticallyGenerated = source[index].AutomaticallyGenerated,
                SourceSpan = this.CloneSpan(source[index].SourceSpan),
                Derivations = this.BuildDerivations(source[index].Derivations)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnValueDescriptor[] BuildNamedValues(LoweredNamedValueIr[] source)
    {
        AsnValueDescriptor[] result = new AsnValueDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            LoweredNamedValueIr named = source[index];
            result[index] = named.Value == null
                ? new AsnValueDescriptor
                {
                    StableId = named.StableId,
                    Name = named.Name,
                    GeneratedName = named.Name,
                    SourceSpan = this.CloneSpan(named.SourceSpan),
                    Derivations = this.BuildDerivations(named.Derivations)
                }
                : this.BuildValueDescriptor(named.Value, named.Name);
            if (result[index].StableId.Length == 0)
            {
                result[index].StableId = named.StableId;
            }

            index = index + 1;
        }

        return result;
    }

    internal AsnValueDescriptor BuildValueDescriptor(
        LoweredValueIr source,
        string name)
    {
        return new AsnValueDescriptor
        {
            StableId = source.StableId,
            Name = name,
            GeneratedName = name,
            Kind = source.Kind,
            Value = this.BuildPublicValue(source),
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations)
        };
    }

    internal AsnValue BuildPublicValue(LoweredValueIr source)
    {
        AsnValue[] items = new AsnValue[source.Items.Length];
        int index = 0;
        while (index < items.Length)
        {
            items[index] = this.BuildPublicValue(source.Items[index]);
            index = index + 1;
        }

        AsnNamedValue[] named = new AsnNamedValue[source.NamedValues.Length];
        index = 0;
        while (index < named.Length)
        {
            named[index] = new AsnNamedValue
            {
                Name = source.NamedValues[index].Name,
                Value = source.NamedValues[index].Value == null
                    ? null
                    : this.BuildPublicValue(source.NamedValues[index].Value
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        IrValuePayloadCloner payloads = new IrValuePayloadCloner();
        return new AsnValue
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
            NamedValues = named
        };
    }

    internal AsnConstraintDescriptor[] BuildConstraints(
        LoweredConstraintIr[] source)
    {
        AsnConstraintDescriptor[] result =
            new AsnConstraintDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.BuildConstraint(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal AsnConstraintDescriptor BuildConstraint(LoweredConstraintIr source)
    {
        AsnConstraintElement? root = source.Root == null
            ? null
            : this.BuildConstraintElement(source.Root);
        AsnConstraintElement? extension = source.Extension == null
            ? null
            : this.BuildConstraintElement(source.Extension);
        int elementCount = root == null ? 0 : 1;
        if (extension != null)
        {
            elementCount = elementCount + 1;
        }

        AsnConstraintElement[] elements = new AsnConstraintElement[elementCount];
        int elementIndex = 0;
        if (root != null)
        {
            elements[elementIndex] = root;
            elementIndex = elementIndex + 1;
        }

        if (extension != null)
        {
            elements[elementIndex] = extension;
        }

        AsnConstraintKind kind = root == null
            ? extension == null ? AsnConstraintKind.Unknown : extension.Kind
            : root.Kind;
        return new AsnConstraintDescriptor
        {
            StableId = source.StableId,
            Kind = kind,
            DomainKind = source.DomainKind,
            Root = root,
            Extension = extension,
            Applications = this.BuildConstraints(source.Applications),
            RootIntervals = this.BuildIntervals(source.RootIntervals),
            ExtensionIntervals = this.BuildIntervals(source.ExtensionIntervals),
            Extensible = source.Extensible,
            OpenExtension = source.OpenExtension,
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations),
            Constraint = new AsnConstraint
            {
                Kind = kind,
                Elements = elements,
                Extensible = source.Extensible,
                Span = this.CloneSpan(source.SourceSpan)
            }
        };
    }

    internal AsnConstraintElement BuildConstraintElement(
        LoweredConstraintNodeIr source)
    {
        AsnConstraintElement[] children =
            new AsnConstraintElement[source.Children.Length];
        int index = 0;
        while (index < children.Length)
        {
            children[index] = this.BuildConstraintElement(source.Children[index]);
            index = index + 1;
        }

        return new AsnConstraintElement
        {
            Kind = source.Kind,
            Name = source.Name,
            Text = source.Text,
            Value = source.Value == null
                ? null
                : this.BuildPublicValue(source.Value),
            LowerValue = source.LowerValue == null
                ? null
                : this.BuildPublicValue(source.LowerValue),
            UpperValue = source.UpperValue == null
                ? null
                : this.BuildPublicValue(source.UpperValue),
            LowerBound = source.LowerBound,
            UpperBound = source.UpperBound,
            HasLowerBound = source.HasLowerBound,
            HasUpperBound = source.HasUpperBound,
            LowerEndpointIncluded = source.LowerEndpointIncluded,
            UpperEndpointIncluded = source.UpperEndpointIncluded,
            Children = children
        };
    }

    internal AsnIntervalDescriptor[] BuildIntervals(LoweredIntervalIr[] source)
    {
        AsnIntervalDescriptor[] result =
            new AsnIntervalDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnIntervalDescriptor
            {
                StableId = source[index].StableId,
                HasLowerBound = source[index].HasLowerBound,
                LowerBound = source[index].LowerBound,
                HasUpperBound = source[index].HasUpperBound,
                UpperBound = source[index].UpperBound,
                SourceSpan = this.CloneSpan(source[index].SourceSpan)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnObjectTableDescriptor[] BuildObjectTables(
        LoweredObjectTableIr[] source)
    {
        AsnObjectTableDescriptor[] result =
            new AsnObjectTableDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = this.BuildObjectTable(source[index]);
            index = index + 1;
        }

        return result;
    }

    internal AsnObjectTableDescriptor BuildObjectTable(LoweredObjectTableIr source)
    {
        AsnObjectTableEntryDescriptor[] entries =
            new AsnObjectTableEntryDescriptor[source.Entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            LoweredObjectTableEntryIr entry = source.Entries[index];
            entries[index] = new AsnObjectTableEntryDescriptor
            {
                StableId = entry.StableId,
                Key = entry.Key == null
                    ? null
                    : this.BuildValueDescriptor(entry.Key, ""),
                Type = entry.Type == null
                    ? null
                    : this.BuildTypeDescriptor(entry.Type),
                SourceSpan = this.CloneSpan(entry.SourceSpan)
            };
            index = index + 1;
        }

        return new AsnObjectTableDescriptor
        {
            StableId = source.StableId,
            Kind = source.Kind,
            ObjectClassName = source.ObjectClassName,
            UniqueFieldName = source.UniqueFieldName,
            Entries = entries,
            SourceSpan = this.CloneSpan(source.SourceSpan),
            Derivations = this.BuildDerivations(source.Derivations)
        };
    }

    internal AsnObjectClass BuildObjectClass(LoweredObjectClassIr source)
    {
        AsnObjectClassField[] fields =
            new AsnObjectClassField[source.Fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            LoweredObjectClassFieldIr field = source.Fields[index];
            fields[index] = new AsnObjectClassField
            {
                Name = field.Name,
                Kind = field.Kind,
                Type = field.Type == null
                    ? null
                    : this.BuildPublicType(field.Type),
                ObjectClass = field.GovernorObjectClassName.Length == 0
                    ? null
                    : new AsnObjectClass
                    {
                        Name = field.GovernorObjectClassName
                    },
                GovernorFieldPath = this.CopyStrings(field.GovernorFieldPath),
                Optional = field.Optional,
                Unique = field.Unique,
                HasDefaultSetting = field.HasDefaultSetting,
                DefaultSetting = field.DefaultSetting == null
                    ? null
                    : this.BuildObjectSetting(field.DefaultSetting)
            };
            index = index + 1;
        }

        return new AsnObjectClass
        {
            Name = source.Name,
            Fields = fields,
            SyntaxTokens = this.CopyStrings(source.SyntaxTokens)
        };
    }

    internal AsnInformationObject BuildObject(LoweredObjectIr source)
    {
        AsnObjectFieldSetting[] settings =
            new AsnObjectFieldSetting[source.FieldSettings.Length];
        int index = 0;
        while (index < settings.Length)
        {
            settings[index] = this.BuildObjectSetting(source.FieldSettings[index]);
            index = index + 1;
        }

        return new AsnInformationObject
        {
            StableId = source.StableId,
            Name = source.Name,
            ObjectClass = source.ObjectClassName.Length == 0
                ? null
                : new AsnObjectClass { Name = source.ObjectClassName },
            FieldSettings = settings
        };
    }

    internal AsnObjectFieldSetting BuildObjectSetting(
        LoweredObjectFieldSettingIr source)
    {
        return new AsnObjectFieldSetting
        {
            FieldName = source.FieldName,
            Type = source.Type == null
                ? null
                : this.BuildPublicType(source.Type),
            ValueSetType = source.ValueSetType == null
                ? null
                : this.BuildPublicType(source.ValueSetType),
            Value = source.Value == null
                ? null
                : this.BuildPublicValue(source.Value),
            Object = source.ObjectStableId.Length == 0
                ? null
                : new AsnInformationObject
                {
                    StableId = source.ObjectStableId
                },
            ObjectSet = source.ObjectSetStableId.Length == 0
                ? null
                : new AsnInformationObjectSet
                {
                    StableId = source.ObjectSetStableId
                }
        };
    }

    internal AsnInformationObjectSet BuildObjectSet(LoweredObjectSetIr source)
    {
        AsnInformationObject[] roots =
            new AsnInformationObject[source.RootObjects.Length];
        AsnInformationObject[] additions =
            new AsnInformationObject[source.AdditionalObjects.Length];
        int index = 0;
        while (index < roots.Length)
        {
            roots[index] = this.BuildObject(source.RootObjects[index]);
            index = index + 1;
        }

        index = 0;
        while (index < additions.Length)
        {
            additions[index] = this.BuildObject(source.AdditionalObjects[index]);
            index = index + 1;
        }

        AsnInformationObject[] objects =
            new AsnInformationObject[roots.Length + additions.Length];
        index = 0;
        while (index < roots.Length)
        {
            objects[index] = roots[index];
            index = index + 1;
        }

        int additionIndex = 0;
        while (additionIndex < additions.Length)
        {
            objects[index] = additions[additionIndex];
            index = index + 1;
            additionIndex = additionIndex + 1;
        }

        AsnOpenTypeDispatchEntry[] dispatch = source.Table == null
            ? []
            : this.BuildDispatchEntries(source.Table.Entries);
        return new AsnInformationObjectSet
        {
            StableId = source.StableId,
            Name = source.Name,
            ObjectClass = source.ObjectClassName.Length == 0
                ? null
                : new AsnObjectClass { Name = source.ObjectClassName },
            Objects = objects,
            RootObjects = roots,
            AdditionalObjects = additions,
            Extensible = source.Extensible,
            DispatchEntries = dispatch
        };
    }

    internal AsnOpenTypeDispatchEntry[] BuildDispatchEntries(
        LoweredObjectTableEntryIr[] source)
    {
        AsnOpenTypeDispatchEntry[] result =
            new AsnOpenTypeDispatchEntry[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnOpenTypeDispatchEntry
            {
                StableId = source[index].StableId,
                Key = source[index].Key == null
                    ? null
                    : this.BuildPublicValue(source[index].Key
                        ?? throw new System.InvalidOperationException()),
                Type = source[index].Type == null
                    ? null
                    : this.BuildPublicType(source[index].Type
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnType BuildPublicType(LoweredTypeIr source)
    {
        int existing = this.FindPublicTypeIndex(source);
        if (existing >= 0)
        {
            return this.PublicTypeResults[existing];
        }

        AsnType result = new AsnType
        {
            Name = source.Name,
            Kind = source.Kind,
            Extensible = source.Extensible
        };
        this.PublicTypeSources.Add(source);
        this.PublicTypeResults.Add(result);
        AsnComponent[] components = new AsnComponent[source.Fields.Length];
        int index = 0;
        while (index < components.Length)
        {
            LoweredFieldIr field = source.Fields[index];
            components[index] = new AsnComponent
            {
                Name = field.Name,
                Type = field.Type == null
                    ? null
                    : this.BuildPublicType(field.Type),
                Optional = field.Optional,
                HasDefaultValue = field.HasDefaultValue,
                DefaultValue = field.DefaultValue == null
                    ? null
                    : this.BuildPublicValue(field.DefaultValue),
                Extension = field.Extension
            };
            index = index + 1;
        }

        result.Components = components;
        result.ElementType = source.ElementType == null
            ? null
            : this.BuildPublicType(source.ElementType);
        AsnNamedValue[] namedValues = new AsnNamedValue[source.NamedValues.Length];
        index = 0;
        while (index < namedValues.Length)
        {
            namedValues[index] = new AsnNamedValue
            {
                Name = source.NamedValues[index].Name,
                Value = source.NamedValues[index].Value == null
                    ? null
                    : this.BuildPublicValue(source.NamedValues[index].Value
                        ?? throw new System.InvalidOperationException())
            };
            index = index + 1;
        }

        result.NamedValues = namedValues;
        AsnConstraintDescriptor[] constraints = this.BuildConstraints(
            source.Constraints);
        result.Constraints = new AsnConstraint[constraints.Length];
        index = 0;
        while (index < constraints.Length)
        {
            result.Constraints[index] = constraints[index].Constraint
                ?? new AsnConstraint();
            index = index + 1;
        }

        if (source.Tags.Length > 0)
        {
            result.Tag = new AsnTag
            {
                Class = source.Tags[0].Class,
                Mode = source.Tags[0].Mode,
                Number = source.Tags[0].Number
            };
        }

        return result;
    }

    internal int FindPublicTypeIndex(LoweredTypeIr source)
    {
        int index = 0;
        while (index < this.PublicTypeSources.Count)
        {
            if (this.PublicTypeSources[index] == source)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnEncodingDescriptor[] BuildEncodings(
        LoweredInstructionIr[] source,
        string ownerStableId,
        AsnEncodingPlanStep[] perInstructionPlanSteps,
        bool useFinalPerPlan)
    {
        System.Collections.Generic.List<ReflectionEncodingGroup> groups =
            new System.Collections.Generic.List<ReflectionEncodingGroup>();
        int index = 0;
        while (index < source.Length)
        {
            int groupIndex = this.FindEncodingGroupIndex(
                groups,
                source[index].EncodingReference);
            if (groupIndex < 0)
            {
                ReflectionEncodingGroup group = new ReflectionEncodingGroup
                {
                    EncodingReference = source[index].EncodingReference
                };
                groups.Add(group);
                groupIndex = groups.Count - 1;
            }

            groups[groupIndex].Instructions.Add(source[index]);
            index = index + 1;
        }

        AsnEncodingDescriptor[] result =
            new AsnEncodingDescriptor[groups.Count];
        index = 0;
        while (index < result.Length)
        {
            ReflectionEncodingGroup group = groups[index];
            LoweredInstructionIr[] instructions = group.Instructions.ToArray();
            AsnEncodingInstruction[] publicInstructions =
                this.BuildEncodingInstructions(instructions);
            AsnEncodingRules rules = this.MapEncodingRules(
                group.EncodingReference);
            result[index] = new AsnEncodingDescriptor
            {
                StableId = ownerStableId + "/encoding/"
                    + index.ToString(
                        System.Globalization.CultureInfo.InvariantCulture),
                EncodingReference = group.EncodingReference,
                EncodingRules = rules,
                Instructions = publicInstructions,
                SourceSpan = instructions.Length == 0
                    ? null
                    : this.CloneSpan(instructions[0].SourceSpan),
                Plan = this.BuildEncodingPlan(
                    ownerStableId,
                    index,
                    rules,
                    instructions,
                    publicInstructions,
                    perInstructionPlanSteps,
                    useFinalPerPlan)
            };
            index = index + 1;
        }

        return result;
    }

    internal int FindEncodingGroupIndex(
        System.Collections.Generic.List<ReflectionEncodingGroup> groups,
        string encodingReference)
    {
        int index = 0;
        while (index < groups.Count)
        {
            if (groups[index].EncodingReference == encodingReference)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal AsnEncodingInstruction[] BuildEncodingInstructions(
        LoweredInstructionIr[] source)
    {
        AsnEncodingInstruction[] result =
            new AsnEncodingInstruction[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnEncodingInstruction
            {
                Name = source[index].Name,
                Kind = source[index].Kind,
                Negated = source[index].Negated,
                Arguments = this.CopyStrings(source[index].Arguments),
                Targets = this.CopyStrings(source[index].Targets),
                Span = this.CloneSpan(source[index].SourceSpan)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingPlan BuildEncodingPlan(
        string ownerStableId,
        int encodingIndex,
        AsnEncodingRules rules,
        LoweredInstructionIr[] source,
        AsnEncodingInstruction[] instructions,
        AsnEncodingPlanStep[] perInstructionPlanSteps,
        bool useFinalPerPlan)
    {
        AsnEncodingPlanStep[] steps = new AsnEncodingPlanStep[source.Length];
        int index = 0;
        while (index < steps.Length)
        {
            steps[index] = new AsnEncodingPlanStep
            {
                Kind = this.MapPlanStepKind(source[index].Kind),
                Instruction = instructions[index],
                Path = this.JoinTargets(source[index].Targets)
            };
            index = index + 1;
        }

        if (useFinalPerPlan
            && source.Length > 0
            && source[0].Kind == AsnInstructionKind.PerApplication)
        {
            steps = this.ClonePlanSteps(perInstructionPlanSteps);
        }

        return new AsnEncodingPlan
        {
            StableId = ownerStableId + "/codec-plan/"
                + encodingIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture),
            EncodingRules = rules,
            Steps = steps,
            Instructions = instructions,
            PerInstructionRegistrations =
                this.ClonePerInstructionRegistrations(
                    this.PerInstructionRegistrations)
        };
    }

    internal AsnEncodingRules MapEncodingRules(string encodingReference)
    {
        string normalized = encodingReference.ToUpperInvariant();
        if (normalized == "BER")
        {
            return AsnEncodingRules.Ber;
        }

        if (normalized == "DER")
        {
            return AsnEncodingRules.Der;
        }

        if (normalized == "CER")
        {
            return AsnEncodingRules.Cer;
        }

        if (normalized == "PER")
        {
            return AsnEncodingRules.Aper;
        }

        if (normalized == "UPER")
        {
            return AsnEncodingRules.Uper;
        }

        if (normalized == "CPER")
        {
            return AsnEncodingRules.CanonicalAlignedPer;
        }

        if (normalized == "CUPER")
        {
            return AsnEncodingRules.CanonicalUnalignedPer;
        }

        if (normalized == "OER")
        {
            return AsnEncodingRules.Oer;
        }

        if (normalized == "COER")
        {
            return AsnEncodingRules.Coer;
        }

        if (normalized == "XER")
        {
            return AsnEncodingRules.ExtendedXer;
        }

        return AsnEncodingRules.Ecn;
    }

    internal AsnEncodingPlanStep[] ClonePlanSteps(
        AsnEncodingPlanStep[] source)
    {
        AsnEncodingPlanStep[] result = new AsnEncodingPlanStep[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnEncodingPlanStep step = source[index];
            result[index] = new AsnEncodingPlanStep
            {
                Kind = step.Kind,
                Instruction = step.Instruction == null
                    ? null
                    : this.ClonePublicInstruction(step.Instruction),
                Path = step.Path,
                Children = this.ClonePlanSteps(step.Children)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingInstruction ClonePublicInstruction(
        AsnEncodingInstruction source)
    {
        return new AsnEncodingInstruction
        {
            Name = source.Name,
            Kind = source.Kind,
            Negated = source.Negated,
            Arguments = this.CopyStrings(source.Arguments),
            Targets = this.CopyStrings(source.Targets),
            Span = this.CloneSpan(source.Span)
        };
    }

    internal AsnPerInstructionRegistration[] ClonePerInstructionRegistrations(
        AsnPerInstructionRegistration[] source)
    {
        AsnPerInstructionRegistration[] result =
            new AsnPerInstructionRegistration[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            AsnPerInstructionRegistration registration = source[index];
            result[index] = new AsnPerInstructionRegistration
            {
                Keyword = registration.Keyword,
                HandlerId = registration.HandlerId,
                SupportsAligned = registration.SupportsAligned,
                SupportsUnaligned = registration.SupportsUnaligned,
                Reversible = registration.Reversible,
                ConflictingKeywords = this.CopyStrings(
                    registration.ConflictingKeywords)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnPlanStepKind MapPlanStepKind(AsnInstructionKind kind)
    {
        if (kind == AsnInstructionKind.PerApplication)
        {
            return AsnPlanStepKind.StandardEncoding;
        }

        if (kind == AsnInstructionKind.Ecn)
        {
            return AsnPlanStepKind.Transformation;
        }

        return kind == AsnInstructionKind.Unknown
            ? AsnPlanStepKind.Unknown
            : AsnPlanStepKind.Mapping;
    }

    internal string JoinTargets(string[] targets)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < targets.Length)
        {
            if (index > 0)
            {
                result.Append('.');
            }

            result.Append(targets[index]);
            index = index + 1;
        }

        return result.ToString();
    }

    internal AsnModule BuildSourceModule(
        LoweredModuleIr source,
        AsnImport[] imports,
        AsnAssignmentDescriptor[] descriptors)
    {
        AsnAssignment[] assignments = new AsnAssignment[descriptors.Length];
        int index = 0;
        while (index < assignments.Length)
        {
            AsnAssignmentDescriptor descriptor = descriptors[index];
            assignments[index] = new AsnAssignment
            {
                Name = descriptor.Name,
                Kind = descriptor.Kind,
                Type = source.Assignments[index].Type == null
                    ? null
                    : this.BuildPublicType(source.Assignments[index].Type
                        ?? throw new System.InvalidOperationException()),
                Value = descriptor.Value == null ? null : descriptor.Value.Value,
                Parameters = this.BuildPublicParameters(descriptor.Parameters),
                ObjectFields = descriptor.ObjectClass == null
                    ? []
                    : descriptor.ObjectClass.Fields,
                ObjectClass = descriptor.ObjectClass,
                Object = descriptor.Object,
                ObjectSet = descriptor.ObjectSet,
                Span = this.CloneSpan(descriptor.SourceSpan)
            };
            index = index + 1;
        }

        AsnEncodingInstruction[] instructions =
            this.BuildEncodingInstructions(source.Instructions);
        return new AsnModule
        {
            Name = source.Name,
            Imports = imports,
            Assignments = assignments,
            EncodingInstructions = instructions,
            Span = this.CloneSpan(source.SourceSpan)
        };
    }

    internal AsnParameter[] BuildPublicParameters(AsnParameterDescriptor[] source)
    {
        AsnParameter[] result = new AsnParameter[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnParameter
            {
                Name = source[index].Name,
                Kind = source[index].Kind,
                GovernorType = source[index].GovernorTypeKind == AsnTypeKind.Unknown
                    ? null
                    : new AsnType { Kind = source[index].GovernorTypeKind },
                GovernorObjectClass = source[index].GovernorObjectClassName.Length == 0
                    ? null
                    : new AsnObjectClass
                    {
                        Name = source[index].GovernorObjectClassName
                    }
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnDerivationDescriptor[] BuildDerivations(
        LoweredDerivationIr[] source)
    {
        AsnDerivationDescriptor[] result =
            new AsnDerivationDescriptor[source.Length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = new AsnDerivationDescriptor
            {
                StableId = source[index].StableId,
                Kind = (AsnDerivationKind)source[index].Kind,
                Name = source[index].Name,
                Span = this.CloneSpan(source[index].Span)
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnSourceSpan? CloneSpan(AsnSourceSpan? source)
    {
        if (source == null)
        {
            return null;
        }

        return new AsnSourceSpan
        {
            Start = this.ClonePosition(source.Start),
            End = this.ClonePosition(source.End)
        };
    }

    internal AsnSourcePosition ClonePosition(AsnSourcePosition source)
    {
        return new AsnSourcePosition
        {
            SourceName = source.SourceName,
            Offset = source.Offset,
            Line = source.Line,
            Column = source.Column
        };
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
