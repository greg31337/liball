namespace asn1;

internal class IrBinaryReader
{
    internal byte[] Data;
    internal int Index;
    internal ResourceCounter Resources;
    internal LoweredTypeIr[] Types = [];
    internal static readonly System.Text.UTF8Encoding StrictUtf8 =
        new System.Text.UTF8Encoding(false, true);

    internal IrBinaryReader(byte[] data, AsnResourceLimits limits)
    {
        this.Data = data == null ? throw new System.ArgumentNullException("data") : data;
        this.Resources = new ResourceCounter(limits);
        if (!this.Resources.TryChargeAllocationBytes(data.Length))
        {
            this.ThrowResourceLimit("IR input exceeds the configured allocation limit.");
        }
    }

    internal LoweredCompilationIr ReadCompilation()
    {
        this.ReadAndValidateMagic();
        int version = this.ReadInt32Value();
        if (version != IrBinaryFormat.Version)
        {
            throw new System.IO.InvalidDataException(
                "The IR binary format version is not supported.");
        }

        int typeCount = this.ReadCountValue();
        this.ChargeArrayAllocation(typeCount, 8);
        this.Types = new LoweredTypeIr[typeCount];
        int typeIndex = 0;
        while (typeIndex < typeCount)
        {
            this.Types[typeIndex] = new LoweredTypeIr();
            typeIndex = typeIndex + 1;
        }

        typeIndex = 0;
        while (typeIndex < typeCount)
        {
            this.ReadTypeBody(this.Types[typeIndex]);
            typeIndex = typeIndex + 1;
        }

        LoweredCompilationIr compilation = new LoweredCompilationIr();
        compilation.StableId = this.ReadStringValue();
        compilation.PerInstructionRegistrations =
            this.ReadPerInstructionRegistrationArray();
        int moduleCount = this.ReadCountValue();
        this.ChargeArrayAllocation(moduleCount, 8);
        compilation.Modules = new LoweredModuleIr[moduleCount];
        int moduleIndex = 0;
        while (moduleIndex < moduleCount)
        {
            compilation.Modules[moduleIndex] = this.ReadModule();
            moduleIndex = moduleIndex + 1;
        }

        if (this.Index != this.Data.Length)
        {
            throw new System.IO.InvalidDataException(
                "The IR binary payload contains trailing data.");
        }

        return compilation;
    }

    internal void ReadTypeBody(LoweredTypeIr type)
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        this.ApplyTypeMetadata(type, metadata);
        type.Name = this.ReadStringValue();
        type.Kind = (AsnTypeKind)this.ReadInt32Value();
        type.Extensible = this.ReadBooleanValue();
        type.ParameterReference = this.ReadBooleanValue();
        int tagCount = this.ReadCountValue();
        this.ChargeArrayAllocation(tagCount, 8);
        type.Tags = new LoweredTagIr[tagCount];
        int index = 0;
        while (index < tagCount)
        {
            type.Tags[index] = this.ReadTag();
            index = index + 1;
        }

        int fieldCount = this.ReadCountValue();
        this.ChargeArrayAllocation(fieldCount, 8);
        type.Fields = new LoweredFieldIr[fieldCount];
        index = 0;
        while (index < fieldCount)
        {
            type.Fields[index] = this.ReadField();
            index = index + 1;
        }

        type.ElementType = this.ReadTypeReference();
        type.NamedValues = this.ReadNamedValueArray();
        type.Constraints = this.ReadConstraintArray();
        type.Extensions = this.ReadExtensionPartition(type.Fields);
        type.ObjectTables = this.ReadObjectTableArray();
        type.Instructions = this.ReadInstructionArray();
        type.PerInstructionPlanSteps = this.ReadPlanStepArray();
    }

    internal LoweredModuleIr ReadModule()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredModuleIr module = new LoweredModuleIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            EmissionOrder = this.ReadInt32Value(),
            Name = this.ReadStringValue(),
            ObjectIdentifier = this.ReadStringValue(),
            Iri = this.ReadStringValue(),
            VersionModule = this.ReadStringValue(),
            Version = this.ReadStringValue(),
            TagDefault = (AsnTaggingMode)this.ReadInt32Value(),
            ExtensibilityImplied = this.ReadBooleanValue()
        };
        int importCount = this.ReadCountValue();
        this.ChargeArrayAllocation(importCount, 8);
        module.Imports = new LoweredImportIr[importCount];
        int index = 0;
        while (index < importCount)
        {
            module.Imports[index] = this.ReadImport();
            index = index + 1;
        }

        int assignmentCount = this.ReadCountValue();
        this.ChargeArrayAllocation(assignmentCount, 8);
        module.Assignments = new LoweredAssignmentIr[assignmentCount];
        index = 0;
        while (index < assignmentCount)
        {
            module.Assignments[index] = this.ReadAssignment();
            index = index + 1;
        }

        module.Instructions = this.ReadInstructionArray();
        module.PerInstructionRegistrations =
            this.ReadPerInstructionRegistrationArray();
        return module;
    }

    internal LoweredImportIr ReadImport()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredImportIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            ModuleName = this.ReadStringValue(),
            Symbols = this.ReadStringArray()
        };
    }

    internal LoweredAssignmentIr ReadAssignment()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredAssignmentIr assignment = new LoweredAssignmentIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            EmissionOrder = this.ReadInt32Value(),
            Name = this.ReadStringValue(),
            QualifiedName = this.ReadStringValue(),
            Kind = (AsnAssignmentKind)this.ReadInt32Value(),
            Exported = this.ReadBooleanValue(),
            Parameterized = this.ReadBooleanValue()
        };
        int parameterCount = this.ReadCountValue();
        this.ChargeArrayAllocation(parameterCount, 8);
        assignment.Parameters = new LoweredParameterIr[parameterCount];
        int index = 0;
        while (index < parameterCount)
        {
            assignment.Parameters[index] = this.ReadParameter();
            index = index + 1;
        }

        assignment.Type = this.ReadTypeReference();
        assignment.Value = this.ReadNullableValue();
        assignment.ObjectClass = this.ReadNullableObjectClass();
        assignment.Object = this.ReadNullableObject();
        assignment.ObjectSet = this.ReadNullableObjectSet();
        return assignment;
    }

    internal LoweredParameterIr ReadParameter()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredParameterIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue(),
            Kind = (AsnParameterKind)this.ReadInt32Value(),
            GovernorTypeKind = (AsnTypeKind)this.ReadInt32Value(),
            GovernorObjectClassName = this.ReadStringValue()
        };
    }

    internal LoweredFieldIr ReadField()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredFieldIr field = new LoweredFieldIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue(),
            Type = this.ReadTypeReference(),
            Optional = this.ReadBooleanValue(),
            HasDefaultValue = this.ReadBooleanValue(),
            DefaultValue = this.ReadNullableValue(),
            Extension = this.ReadBooleanValue(),
            ExtensionGroupIndex = this.ReadInt32Value()
        };
        int tagCount = this.ReadCountValue();
        this.ChargeArrayAllocation(tagCount, 8);
        field.Tags = new LoweredTagIr[tagCount];
        int index = 0;
        while (index < tagCount)
        {
            field.Tags[index] = this.ReadTag();
            index = index + 1;
        }

        field.Instructions = this.ReadInstructionArray();
        field.PerInstructionPlanSteps = this.ReadPlanStepArray();

        return field;
    }

    internal LoweredTagIr ReadTag()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredTagIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Class = (AsnTagClass)this.ReadInt32Value(),
            Mode = (AsnTaggingMode)this.ReadInt32Value(),
            Number = this.ReadBigIntegerValue(),
            Universal = this.ReadBooleanValue(),
            AutomaticallyGenerated = this.ReadBooleanValue()
        };
    }

    internal LoweredValueIr? ReadNullableValue()
    {
        return this.ReadBooleanValue() ? this.ReadValue() : null;
    }

    internal LoweredValueIr ReadValue()
    {
        this.EnterNesting();
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredValueIr value = new LoweredValueIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Kind = (AsnValueKind)this.ReadInt32Value(),
            HasIntegerValue = this.ReadBooleanValue(),
            IntegerValue = this.ReadBigIntegerValue(),
            TextValue = this.ReadStringValue(),
            Octets = this.ReadByteArrayValue(),
            BitStringValue = this.ReadBitStringPayload(),
            ObjectIdentifierValue = this.ReadObjectIdentifierPayload(),
            RealValue = this.ReadRealPayload(),
            TimeValue = this.ReadTimePayload()
        };
        int itemCount = this.ReadCountValue();
        this.ChargeArrayAllocation(itemCount, 8);
        value.Items = new LoweredValueIr[itemCount];
        int index = 0;
        while (index < itemCount)
        {
            value.Items[index] = this.ReadValue();
            index = index + 1;
        }

        value.NamedValues = this.ReadNamedValueArray();
        this.Resources.LeaveNesting();
        return value;
    }

    internal AsnBitStringValue? ReadBitStringPayload()
    {
        if (!this.ReadBooleanValue())
        {
            return null;
        }

        return new AsnBitStringValue
        {
            Bytes = this.ReadByteArrayValue(),
            BitLength = this.ReadInt32Value()
        };
    }

    internal AsnObjectIdentifierValue? ReadObjectIdentifierPayload()
    {
        if (!this.ReadBooleanValue())
        {
            return null;
        }

        AsnObjectIdentifierValue result = new AsnObjectIdentifierValue
        {
            Relative = this.ReadBooleanValue()
        };
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 16);
        result.Arcs = new System.Numerics.BigInteger[count];
        int index = 0;
        while (index < count)
        {
            result.Arcs[index] = this.ReadBigIntegerValue();
            index = index + 1;
        }

        return result;
    }

    internal AsnRealValue? ReadRealPayload()
    {
        if (!this.ReadBooleanValue())
        {
            return null;
        }

        return new AsnRealValue
        {
            Normalized = this.ReadBooleanValue(),
            Kind = (AsnRealKind)this.ReadInt32Value(),
            HasDecimalValue = this.ReadBooleanValue(),
            DecimalText = this.ReadStringValue(),
            DecimalCoefficient = this.ReadBigIntegerValue(),
            DecimalExponent = this.ReadBigIntegerValue(),
            HasBinaryValue = this.ReadBooleanValue(),
            Mantissa = this.ReadBigIntegerValue(),
            Radix = this.ReadInt32Value(),
            Exponent = this.ReadBigIntegerValue()
        };
    }

    internal AsnTimeValue? ReadTimePayload()
    {
        if (!this.ReadBooleanValue())
        {
            return null;
        }

        return new AsnTimeValue
        {
            Kind = (AsnTimeKind)this.ReadInt32Value(),
            Text = this.ReadStringValue(),
            HasUtcOffsetMinutes = this.ReadBooleanValue(),
            UtcOffsetMinutes = this.ReadInt32Value(),
            HasFractionalDigits = this.ReadBooleanValue(),
            FractionalDigits = this.ReadInt32Value()
        };
    }

    internal LoweredNamedValueIr[] ReadNamedValueArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredNamedValueIr[] result = new LoweredNamedValueIr[count];
        int index = 0;
        while (index < count)
        {
            IrNodeMetadata metadata = this.ReadNodeMetadata();
            result[index] = new LoweredNamedValueIr
            {
                StableId = metadata.StableId,
                SourceSpan = metadata.SourceSpan,
                Derivations = metadata.Derivations,
                Name = this.ReadStringValue(),
                Value = this.ReadNullableValue()
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredConstraintIr[] ReadConstraintArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredConstraintIr[] result = new LoweredConstraintIr[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.ReadConstraint();
            index = index + 1;
        }

        return result;
    }

    internal LoweredConstraintIr ReadConstraint()
    {
        this.EnterNesting();
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredConstraintIr constraint = new LoweredConstraintIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            DomainKind = (AsnTypeKind)this.ReadInt32Value(),
            Root = this.ReadNullableConstraintNode(),
            Extension = this.ReadNullableConstraintNode(),
            Applications = this.ReadConstraintArray(),
            RootIntervals = this.ReadIntervalArray(),
            ExtensionIntervals = this.ReadIntervalArray(),
            Extensible = this.ReadBooleanValue(),
            OpenExtension = this.ReadBooleanValue()
        };
        this.Resources.LeaveNesting();
        return constraint;
    }

    internal LoweredConstraintNodeIr? ReadNullableConstraintNode()
    {
        return this.ReadBooleanValue() ? this.ReadConstraintNode() : null;
    }

    internal LoweredConstraintNodeIr ReadConstraintNode()
    {
        this.EnterNesting();
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredConstraintNodeIr node = new LoweredConstraintNodeIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Kind = (AsnConstraintKind)this.ReadInt32Value(),
            Name = this.ReadStringValue(),
            Text = this.ReadStringValue(),
            Value = this.ReadNullableValue(),
            LowerValue = this.ReadNullableValue(),
            UpperValue = this.ReadNullableValue(),
            LowerBound = this.ReadBigIntegerValue(),
            UpperBound = this.ReadBigIntegerValue(),
            HasLowerBound = this.ReadBooleanValue(),
            HasUpperBound = this.ReadBooleanValue(),
            LowerEndpointIncluded = this.ReadBooleanValue(),
            UpperEndpointIncluded = this.ReadBooleanValue()
        };
        int childCount = this.ReadCountValue();
        this.ChargeArrayAllocation(childCount, 8);
        node.Children = new LoweredConstraintNodeIr[childCount];
        int index = 0;
        while (index < childCount)
        {
            node.Children[index] = this.ReadConstraintNode();
            index = index + 1;
        }

        node.NestedConstraints = this.ReadConstraintArray();
        this.Resources.LeaveNesting();
        return node;
    }

    internal LoweredIntervalIr[] ReadIntervalArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredIntervalIr[] result = new LoweredIntervalIr[count];
        int index = 0;
        while (index < count)
        {
            IrNodeMetadata metadata = this.ReadNodeMetadata();
            result[index] = new LoweredIntervalIr
            {
                StableId = metadata.StableId,
                SourceSpan = metadata.SourceSpan,
                Derivations = metadata.Derivations,
                LowerBound = this.ReadBigIntegerValue(),
                UpperBound = this.ReadBigIntegerValue(),
                HasLowerBound = this.ReadBooleanValue(),
                HasUpperBound = this.ReadBooleanValue()
            };
            index = index + 1;
        }

        return result;
    }

    internal LoweredExtensionPartitionIr ReadExtensionPartition(
        LoweredFieldIr[] fields)
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredExtensionPartitionIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Extensible = this.ReadBooleanValue(),
            RootFields = this.ReadFieldReferenceArray(fields),
            AdditionalFields = this.ReadFieldReferenceArray(fields),
            RootNamedValues = this.ReadNamedValueArray(),
            AdditionalNamedValues = this.ReadNamedValueArray()
        };
    }

    internal LoweredFieldIr[] ReadFieldReferenceArray(LoweredFieldIr[] fields)
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredFieldIr[] result = new LoweredFieldIr[count];
        int index = 0;
        while (index < count)
        {
            int fieldIndex = this.ReadInt32Value();
            if (fieldIndex < 0 || fieldIndex >= fields.Length)
            {
                throw new System.IO.InvalidDataException(
                    "The IR extension partition contains an invalid field reference.");
            }

            result[index] = fields[fieldIndex];
            index = index + 1;
        }

        return result;
    }

    internal LoweredObjectClassIr? ReadNullableObjectClass()
    {
        return this.ReadBooleanValue() ? this.ReadObjectClass() : null;
    }

    internal LoweredObjectClassIr ReadObjectClass()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredObjectClassIr objectClass = new LoweredObjectClassIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue()
        };
        int fieldCount = this.ReadCountValue();
        this.ChargeArrayAllocation(fieldCount, 8);
        objectClass.Fields = new LoweredObjectClassFieldIr[fieldCount];
        int index = 0;
        while (index < fieldCount)
        {
            objectClass.Fields[index] = this.ReadObjectClassField();
            index = index + 1;
        }

        objectClass.SyntaxTokens = this.ReadStringArray();
        return objectClass;
    }

    internal LoweredObjectClassFieldIr ReadObjectClassField()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredObjectClassFieldIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue(),
            Kind = (AsnObjectFieldKind)this.ReadInt32Value(),
            Type = this.ReadTypeReference(),
            GovernorObjectClassName = this.ReadStringValue(),
            GovernorFieldPath = this.ReadStringArray(),
            Optional = this.ReadBooleanValue(),
            Unique = this.ReadBooleanValue(),
            HasDefaultSetting = this.ReadBooleanValue(),
            DefaultSetting = this.ReadNullableObjectSetting()
        };
    }

    internal LoweredObjectIr? ReadNullableObject()
    {
        return this.ReadBooleanValue() ? this.ReadObject() : null;
    }

    internal LoweredObjectIr ReadObject()
    {
        this.EnterNesting();
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredObjectIr value = new LoweredObjectIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue(),
            ObjectClassName = this.ReadStringValue()
        };
        int settingCount = this.ReadCountValue();
        this.ChargeArrayAllocation(settingCount, 8);
        value.FieldSettings = new LoweredObjectFieldSettingIr[settingCount];
        int index = 0;
        while (index < settingCount)
        {
            value.FieldSettings[index] = this.ReadObjectSetting();
            index = index + 1;
        }

        this.Resources.LeaveNesting();
        return value;
    }

    internal LoweredObjectFieldSettingIr? ReadNullableObjectSetting()
    {
        return this.ReadBooleanValue() ? this.ReadObjectSetting() : null;
    }

    internal LoweredObjectFieldSettingIr ReadObjectSetting()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        return new LoweredObjectFieldSettingIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            FieldName = this.ReadStringValue(),
            Type = this.ReadTypeReference(),
            ValueSetType = this.ReadTypeReference(),
            Value = this.ReadNullableValue(),
            ObjectStableId = this.ReadStringValue(),
            ObjectSetStableId = this.ReadStringValue()
        };
    }

    internal LoweredObjectSetIr? ReadNullableObjectSet()
    {
        return this.ReadBooleanValue() ? this.ReadObjectSet() : null;
    }

    internal LoweredObjectSetIr ReadObjectSet()
    {
        this.EnterNesting();
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredObjectSetIr set = new LoweredObjectSetIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Name = this.ReadStringValue(),
            ObjectClassName = this.ReadStringValue(),
            Extensible = this.ReadBooleanValue()
        };
        int rootCount = this.ReadCountValue();
        this.ChargeArrayAllocation(rootCount, 8);
        set.RootObjects = new LoweredObjectIr[rootCount];
        int index = 0;
        while (index < rootCount)
        {
            set.RootObjects[index] = this.ReadObject();
            index = index + 1;
        }

        int additionCount = this.ReadCountValue();
        this.ChargeArrayAllocation(additionCount, 8);
        set.AdditionalObjects = new LoweredObjectIr[additionCount];
        index = 0;
        while (index < additionCount)
        {
            set.AdditionalObjects[index] = this.ReadObject();
            index = index + 1;
        }

        set.Table = this.ReadNullableObjectTable();
        this.Resources.LeaveNesting();
        return set;
    }

    internal LoweredObjectTableIr[] ReadObjectTableArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredObjectTableIr[] result = new LoweredObjectTableIr[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.ReadObjectTable();
            index = index + 1;
        }

        return result;
    }

    internal LoweredObjectTableIr? ReadNullableObjectTable()
    {
        return this.ReadBooleanValue() ? this.ReadObjectTable() : null;
    }

    internal LoweredObjectTableIr ReadObjectTable()
    {
        IrNodeMetadata metadata = this.ReadNodeMetadata();
        LoweredObjectTableIr table = new LoweredObjectTableIr
        {
            StableId = metadata.StableId,
            SourceSpan = metadata.SourceSpan,
            Derivations = metadata.Derivations,
            Kind = (AsnConstraintKind)this.ReadInt32Value(),
            ObjectClassName = this.ReadStringValue(),
            UniqueFieldName = this.ReadStringValue()
        };
        int entryCount = this.ReadCountValue();
        this.ChargeArrayAllocation(entryCount, 8);
        table.Entries = new LoweredObjectTableEntryIr[entryCount];
        int index = 0;
        while (index < entryCount)
        {
            IrNodeMetadata entryMetadata = this.ReadNodeMetadata();
            table.Entries[index] = new LoweredObjectTableEntryIr
            {
                StableId = entryMetadata.StableId,
                SourceSpan = entryMetadata.SourceSpan,
                Derivations = entryMetadata.Derivations,
                Key = this.ReadNullableValue(),
                Type = this.ReadTypeReference()
            };
            index = index + 1;
        }

        return table;
    }

    internal LoweredInstructionIr[] ReadInstructionArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        LoweredInstructionIr[] result = new LoweredInstructionIr[count];
        int index = 0;
        while (index < count)
        {
            IrNodeMetadata metadata = this.ReadNodeMetadata();
            result[index] = new LoweredInstructionIr
            {
                StableId = metadata.StableId,
                SourceSpan = metadata.SourceSpan,
                Derivations = metadata.Derivations,
                EncodingReference = this.ReadStringValue(),
                Name = this.ReadStringValue(),
                Kind = (AsnInstructionKind)this.ReadInt32Value(),
                Negated = this.ReadBooleanValue(),
                Arguments = this.ReadStringArray(),
                Targets = this.ReadStringArray()
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnPerInstructionRegistration[] ReadPerInstructionRegistrationArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        AsnPerInstructionRegistration[] result =
            new AsnPerInstructionRegistration[count];
        int index = 0;
        while (index < count)
        {
            result[index] = new AsnPerInstructionRegistration
            {
                Keyword = this.ReadStringValue(),
                HandlerId = this.ReadStringValue(),
                SupportsAligned = this.ReadBooleanValue(),
                SupportsUnaligned = this.ReadBooleanValue(),
                Reversible = this.ReadBooleanValue(),
                ConflictingKeywords = this.ReadStringArray()
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingPlanStep[] ReadPlanStepArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        AsnEncodingPlanStep[] result = new AsnEncodingPlanStep[count];
        int index = 0;
        while (index < count)
        {
            AsnPlanStepKind kind = (AsnPlanStepKind)this.ReadInt32Value();
            AsnEncodingInstruction? instruction = this.ReadBooleanValue()
                ? this.ReadPublicInstruction()
                : null;
            string path = this.ReadStringValue();
            AsnEncodingPlanStep[] children = this.ReadPlanStepArray();
            result[index] = new AsnEncodingPlanStep
            {
                Kind = kind,
                Instruction = instruction,
                Path = path,
                Children = children
            };
            index = index + 1;
        }

        return result;
    }

    internal AsnEncodingInstruction ReadPublicInstruction()
    {
        return new AsnEncodingInstruction
        {
            Name = this.ReadStringValue(),
            Kind = (AsnInstructionKind)this.ReadInt32Value(),
            Negated = this.ReadBooleanValue(),
            Arguments = this.ReadStringArray(),
            Targets = this.ReadStringArray(),
            Span = this.ReadNullableSpan()
        };
    }

    internal IrNodeMetadata ReadNodeMetadata()
    {
        IrNodeMetadata metadata = new IrNodeMetadata
        {
            StableId = this.ReadStringValue(),
            SourceSpan = this.ReadNullableSpan()
        };
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        metadata.Derivations = new LoweredDerivationIr[count];
        int index = 0;
        while (index < count)
        {
            metadata.Derivations[index] = new LoweredDerivationIr
            {
                StableId = this.ReadStringValue(),
                Kind = (LoweredDerivationKind)this.ReadInt32Value(),
                Name = this.ReadStringValue(),
                Span = this.ReadNullableSpan()
            };
            index = index + 1;
        }

        return metadata;
    }

    internal AsnSourceSpan? ReadNullableSpan()
    {
        if (!this.ReadBooleanValue())
        {
            return null;
        }

        AsnSourcePosition start = this.ReadPosition();
        AsnSourcePosition end = this.ReadPosition();
        if (start.SourceName != end.SourceName || end.Offset < start.Offset)
        {
            throw new System.IO.InvalidDataException(
                "The IR source span is not a valid half-open source range.");
        }

        return new AsnSourceSpan { Start = start, End = end };
    }

    internal AsnSourcePosition ReadPosition()
    {
        AsnSourcePosition position = new AsnSourcePosition
        {
            SourceName = this.ReadStringValue(),
            Offset = this.ReadInt32Value(),
            Line = this.ReadInt32Value(),
            Column = this.ReadInt32Value()
        };
        if (position.Offset < 0 || position.Line < 1 || position.Column < 1)
        {
            throw new System.IO.InvalidDataException(
                "The IR source position is invalid.");
        }

        return position;
    }

    internal LoweredTypeIr? ReadTypeReference()
    {
        int index = this.ReadInt32Value();
        if (index == -1)
        {
            return null;
        }

        if (index < 0 || index >= this.Types.Length)
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains an invalid type reference.");
        }

        return this.Types[index];
    }

    internal string[] ReadStringArray()
    {
        int count = this.ReadCountValue();
        this.ChargeArrayAllocation(count, 8);
        string[] result = new string[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.ReadStringValue();
            index = index + 1;
        }

        return result;
    }

    internal string ReadStringValue()
    {
        int length = this.ReadLengthValue();
        long maximumBytes = (long)this.Resources.Limits.MaximumTextCharacters * 4;
        if (!this.Resources.Limits.Unlimited && length > maximumBytes)
        {
            this.ThrowResourceLimit("IR text exceeds the configured character limit.");
        }

        byte[] bytes = this.ReadRawBytes(length);
        string result = StrictUtf8.GetString(bytes);
        if (!this.Resources.AllowsTextCharacters(result.Length))
        {
            this.ThrowResourceLimit("IR text exceeds the configured character limit.");
        }

        if (!this.Resources.TryChargeAllocationBytes((long)result.Length * 2))
        {
            this.ThrowResourceLimit("IR text exceeds the configured allocation limit.");
        }

        return result;
    }

    internal byte[] ReadByteArrayValue()
    {
        int length = this.ReadLengthValue();
        if (!this.Resources.AllowsOpenTypeBytes(length))
        {
            this.ThrowResourceLimit("IR byte data exceeds the configured byte limit.");
        }

        return this.ReadRawBytes(length);
    }

    internal System.Numerics.BigInteger ReadBigIntegerValue()
    {
        int length = this.ReadLengthValue();
        if (length == 0)
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains an empty integer encoding.");
        }

        long bits = (long)length * 8;
        if (!this.Resources.Limits.Unlimited
            && bits > (long)this.Resources.Limits.MaximumIntegerBits + 8)
        {
            this.ThrowResourceLimit("IR integer exceeds the configured bit limit.");
        }

        byte[] bytes = this.ReadRawBytes(length);
        if (bytes.Length > 1
            && (bytes[0] == 0 && (bytes[1] & 128) == 0
                || bytes[0] == 255 && (bytes[1] & 128) != 0))
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains a non-canonical integer encoding.");
        }

        return BigIntegerConversions.FromSignedBigEndian(bytes);
    }

    internal int ReadCountValue()
    {
        int count = this.ReadInt32Value();
        if (count < 0)
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains a negative collection count.");
        }

        if (!this.Resources.AllowsCollectionElements(count))
        {
            this.ThrowResourceLimit("IR collection exceeds the configured element limit.");
        }

        return count;
    }

    internal int ReadLengthValue()
    {
        int length = this.ReadInt32Value();
        if (length < 0)
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains a negative length.");
        }

        return length;
    }

    internal bool ReadBooleanValue()
    {
        byte value = this.ReadByteValue();
        if (value > 1)
        {
            throw new System.IO.InvalidDataException(
                "The IR payload contains an invalid Boolean value.");
        }

        return value == 1;
    }

    internal int ReadInt32Value()
    {
        int first = this.ReadByteValue();
        int second = this.ReadByteValue();
        int third = this.ReadByteValue();
        int fourth = this.ReadByteValue();
        return first | second << 8 | third << 16 | fourth << 24;
    }

    internal byte ReadByteValue()
    {
        if (this.Index >= this.Data.Length)
        {
            throw new System.IO.InvalidDataException(
                "The IR binary payload is truncated.");
        }

        byte result = this.Data[this.Index];
        this.Index = this.Index + 1;
        return result;
    }

    internal byte[] ReadRawBytes(int count)
    {
        if (count > this.Data.Length - this.Index)
        {
            throw new System.IO.InvalidDataException(
                "The IR binary payload is truncated.");
        }

        if (!this.Resources.TryChargeAllocationBytes(count))
        {
            this.ThrowResourceLimit("IR data exceeds the configured allocation limit.");
        }

        byte[] result = new byte[count];
        int index = 0;
        while (index < count)
        {
            result[index] = this.Data[this.Index + index];
            index = index + 1;
        }

        this.Index = this.Index + count;
        return result;
    }

    internal void ReadAndValidateMagic()
    {
        int index = 0;
        while (index < IrBinaryFormat.Magic.Length)
        {
            if (this.ReadByteValue() != IrBinaryFormat.Magic[index])
            {
                throw new System.IO.InvalidDataException(
                    "The IR binary payload has an invalid header.");
            }

            index = index + 1;
        }
    }

    internal void ApplyTypeMetadata(LoweredTypeIr type, IrNodeMetadata metadata)
    {
        type.StableId = metadata.StableId;
        type.SourceSpan = metadata.SourceSpan;
        type.Derivations = metadata.Derivations;
    }

    internal void ChargeArrayAllocation(int count, int elementBytes)
    {
        long bytes = (long)count * elementBytes;
        if (!this.Resources.TryChargeAllocationBytes(bytes))
        {
            this.ThrowResourceLimit("IR arrays exceed the configured allocation limit.");
        }
    }

    internal void EnterNesting()
    {
        if (!this.Resources.TryEnterNesting())
        {
            this.ThrowResourceLimit("IR nesting exceeds the configured depth limit.");
        }
    }

    internal void ThrowResourceLimit(string message)
    {
        throw new AsnResourceLimitException(message, "ASNRES0001", []);
    }
}
