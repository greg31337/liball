namespace asn1;

internal class IrBinaryWriter
{
    internal System.IO.MemoryStream Stream = new System.IO.MemoryStream();
    internal ResourceCounter Resources;
    internal IrTypeCollector TypeCollector;

    internal IrBinaryWriter(AsnResourceLimits limits, IrTypeCollector typeCollector)
    {
        this.Resources = new ResourceCounter(limits);
        this.TypeCollector = typeCollector;
    }

    internal byte[] WriteCompilation(LoweredCompilationIr compilation)
    {
        this.WriteRawBytes(IrBinaryFormat.Magic);
        this.WriteInt32Value(IrBinaryFormat.Version);
        this.WriteCountValue(this.TypeCollector.Types.Count);
        int typeIndex = 0;
        while (typeIndex < this.TypeCollector.Types.Count)
        {
            this.WriteTypeBody(this.TypeCollector.Types[typeIndex]);
            typeIndex = typeIndex + 1;
        }

        this.WriteStringValue(compilation.StableId);
        this.WritePerInstructionRegistrationArray(
            compilation.PerInstructionRegistrations);
        this.WriteCountValue(compilation.Modules.Length);
        int moduleIndex = 0;
        while (moduleIndex < compilation.Modules.Length)
        {
            this.WriteModule(compilation.Modules[moduleIndex]);
            moduleIndex = moduleIndex + 1;
        }

        if (!this.Resources.TryChargeAllocationBytes(this.Stream.Length))
        {
            this.ThrowResourceLimit("IR output exceeds the configured allocation limit.");
        }

        return this.Stream.ToArray();
    }

    internal void WriteTypeBody(LoweredTypeIr type)
    {
        this.WriteNodeMetadata(type.StableId, type.SourceSpan, type.Derivations);
        this.WriteStringValue(type.Name);
        this.WriteInt32Value((int)type.Kind);
        this.WriteBooleanValue(type.Extensible);
        this.WriteBooleanValue(type.ParameterReference);
        this.WriteCountValue(type.Tags.Length);
        int index = 0;
        while (index < type.Tags.Length)
        {
            this.WriteTag(type.Tags[index]);
            index = index + 1;
        }

        this.WriteCountValue(type.Fields.Length);
        index = 0;
        while (index < type.Fields.Length)
        {
            this.WriteField(type.Fields[index]);
            index = index + 1;
        }

        this.WriteTypeReference(type.ElementType);
        this.WriteNamedValueArray(type.NamedValues);
        this.WriteConstraintArray(type.Constraints);
        this.WriteExtensionPartition(type.Extensions, type.Fields);
        this.WriteObjectTableArray(type.ObjectTables);
        this.WriteInstructionArray(type.Instructions);
        this.WritePlanStepArray(type.PerInstructionPlanSteps);
    }

    internal void WriteModule(LoweredModuleIr module)
    {
        this.WriteNodeMetadata(module.StableId, module.SourceSpan, module.Derivations);
        this.WriteInt32Value(module.EmissionOrder);
        this.WriteStringValue(module.Name);
        this.WriteStringValue(module.ObjectIdentifier);
        this.WriteStringValue(module.Iri);
        this.WriteStringValue(module.VersionModule);
        this.WriteStringValue(module.Version);
        this.WriteInt32Value((int)module.TagDefault);
        this.WriteBooleanValue(module.ExtensibilityImplied);
        this.WriteCountValue(module.Imports.Length);
        int index = 0;
        while (index < module.Imports.Length)
        {
            this.WriteImport(module.Imports[index]);
            index = index + 1;
        }

        this.WriteCountValue(module.Assignments.Length);
        index = 0;
        while (index < module.Assignments.Length)
        {
            this.WriteAssignment(module.Assignments[index]);
            index = index + 1;
        }

        this.WriteInstructionArray(module.Instructions);
        this.WritePerInstructionRegistrationArray(
            module.PerInstructionRegistrations);
    }

    internal void WriteImport(LoweredImportIr import)
    {
        this.WriteNodeMetadata(import.StableId, import.SourceSpan, import.Derivations);
        this.WriteStringValue(import.ModuleName);
        this.WriteStringArray(import.Symbols);
    }

    internal void WriteAssignment(LoweredAssignmentIr assignment)
    {
        this.WriteNodeMetadata(
            assignment.StableId,
            assignment.SourceSpan,
            assignment.Derivations);
        this.WriteInt32Value(assignment.EmissionOrder);
        this.WriteStringValue(assignment.Name);
        this.WriteStringValue(assignment.QualifiedName);
        this.WriteInt32Value((int)assignment.Kind);
        this.WriteBooleanValue(assignment.Exported);
        this.WriteBooleanValue(assignment.Parameterized);
        this.WriteCountValue(assignment.Parameters.Length);
        int index = 0;
        while (index < assignment.Parameters.Length)
        {
            this.WriteParameter(assignment.Parameters[index]);
            index = index + 1;
        }

        this.WriteTypeReference(assignment.Type);
        this.WriteNullableValue(assignment.Value);
        this.WriteNullableObjectClass(assignment.ObjectClass);
        this.WriteNullableObject(assignment.Object);
        this.WriteNullableObjectSet(assignment.ObjectSet);
    }

    internal void WriteParameter(LoweredParameterIr parameter)
    {
        this.WriteNodeMetadata(
            parameter.StableId,
            parameter.SourceSpan,
            parameter.Derivations);
        this.WriteStringValue(parameter.Name);
        this.WriteInt32Value((int)parameter.Kind);
        this.WriteInt32Value((int)parameter.GovernorTypeKind);
        this.WriteStringValue(parameter.GovernorObjectClassName);
    }

    internal void WriteField(LoweredFieldIr field)
    {
        this.WriteNodeMetadata(field.StableId, field.SourceSpan, field.Derivations);
        this.WriteStringValue(field.Name);
        this.WriteTypeReference(field.Type);
        this.WriteBooleanValue(field.Optional);
        this.WriteBooleanValue(field.HasDefaultValue);
        this.WriteNullableValue(field.DefaultValue);
        this.WriteBooleanValue(field.Extension);
        this.WriteInt32Value(field.ExtensionGroupIndex);
        this.WriteCountValue(field.Tags.Length);
        int index = 0;
        while (index < field.Tags.Length)
        {
            this.WriteTag(field.Tags[index]);
            index = index + 1;
        }

        this.WriteInstructionArray(field.Instructions);
        this.WritePlanStepArray(field.PerInstructionPlanSteps);
    }

    internal void WriteTag(LoweredTagIr tag)
    {
        this.WriteNodeMetadata(tag.StableId, tag.SourceSpan, tag.Derivations);
        this.WriteInt32Value((int)tag.Class);
        this.WriteInt32Value((int)tag.Mode);
        this.WriteBigIntegerValue(tag.Number);
        this.WriteBooleanValue(tag.Universal);
        this.WriteBooleanValue(tag.AutomaticallyGenerated);
    }

    internal void WriteNullableValue(LoweredValueIr? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteValue(value);
        }
    }

    internal void WriteValue(LoweredValueIr value)
    {
        this.EnterNesting();
        this.WriteNodeMetadata(value.StableId, value.SourceSpan, value.Derivations);
        this.WriteInt32Value((int)value.Kind);
        this.WriteBooleanValue(value.HasIntegerValue);
        this.WriteBigIntegerValue(value.IntegerValue);
        this.WriteStringValue(value.TextValue);
        this.WriteByteArrayValue(value.Octets);
        this.WriteBitStringPayload(value.BitStringValue);
        this.WriteObjectIdentifierPayload(value.ObjectIdentifierValue);
        this.WriteRealPayload(value.RealValue);
        this.WriteTimePayload(value.TimeValue);
        this.WriteCountValue(value.Items.Length);
        int index = 0;
        while (index < value.Items.Length)
        {
            this.WriteValue(value.Items[index]);
            index = index + 1;
        }

        this.WriteNamedValueArray(value.NamedValues);
        this.Resources.LeaveNesting();
    }

    internal void WriteBitStringPayload(AsnBitStringValue? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteByteArrayValue(value.Bytes);
            this.WriteInt32Value(value.BitLength);
        }
    }

    internal void WriteObjectIdentifierPayload(AsnObjectIdentifierValue? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteBooleanValue(value.Relative);
            this.WriteCountValue(value.Arcs.Length);
            int index = 0;
            while (index < value.Arcs.Length)
            {
                this.WriteBigIntegerValue(value.Arcs[index]);
                index = index + 1;
            }
        }
    }

    internal void WriteRealPayload(AsnRealValue? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteBooleanValue(value.Normalized);
            this.WriteInt32Value((int)value.Kind);
            this.WriteBooleanValue(value.HasDecimalValue);
            this.WriteStringValue(value.DecimalText);
            this.WriteBigIntegerValue(value.DecimalCoefficient);
            this.WriteBigIntegerValue(value.DecimalExponent);
            this.WriteBooleanValue(value.HasBinaryValue);
            this.WriteBigIntegerValue(value.Mantissa);
            this.WriteInt32Value(value.Radix);
            this.WriteBigIntegerValue(value.Exponent);
        }
    }

    internal void WriteTimePayload(AsnTimeValue? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteInt32Value((int)value.Kind);
            this.WriteStringValue(value.Text);
            this.WriteBooleanValue(value.HasUtcOffsetMinutes);
            this.WriteInt32Value(value.UtcOffsetMinutes);
            this.WriteBooleanValue(value.HasFractionalDigits);
            this.WriteInt32Value(value.FractionalDigits);
        }
    }

    internal void WriteNamedValueArray(LoweredNamedValueIr[] values)
    {
        this.WriteCountValue(values.Length);
        int index = 0;
        while (index < values.Length)
        {
            LoweredNamedValueIr value = values[index];
            this.WriteNodeMetadata(
                value.StableId,
                value.SourceSpan,
                value.Derivations);
            this.WriteStringValue(value.Name);
            this.WriteNullableValue(value.Value);
            index = index + 1;
        }
    }

    internal void WriteConstraintArray(LoweredConstraintIr[] constraints)
    {
        this.WriteCountValue(constraints.Length);
        int index = 0;
        while (index < constraints.Length)
        {
            this.WriteConstraint(constraints[index]);
            index = index + 1;
        }
    }

    internal void WriteConstraint(LoweredConstraintIr constraint)
    {
        this.EnterNesting();
        this.WriteNodeMetadata(
            constraint.StableId,
            constraint.SourceSpan,
            constraint.Derivations);
        this.WriteInt32Value((int)constraint.DomainKind);
        this.WriteNullableConstraintNode(constraint.Root);
        this.WriteNullableConstraintNode(constraint.Extension);
        this.WriteConstraintArray(constraint.Applications);
        this.WriteIntervalArray(constraint.RootIntervals);
        this.WriteIntervalArray(constraint.ExtensionIntervals);
        this.WriteBooleanValue(constraint.Extensible);
        this.WriteBooleanValue(constraint.OpenExtension);
        this.Resources.LeaveNesting();
    }

    internal void WriteNullableConstraintNode(LoweredConstraintNodeIr? node)
    {
        this.WriteBooleanValue(node != null);
        if (node != null)
        {
            this.WriteConstraintNode(node);
        }
    }

    internal void WriteConstraintNode(LoweredConstraintNodeIr node)
    {
        this.EnterNesting();
        this.WriteNodeMetadata(node.StableId, node.SourceSpan, node.Derivations);
        this.WriteInt32Value((int)node.Kind);
        this.WriteStringValue(node.Name);
        this.WriteStringValue(node.Text);
        this.WriteNullableValue(node.Value);
        this.WriteNullableValue(node.LowerValue);
        this.WriteNullableValue(node.UpperValue);
        this.WriteBigIntegerValue(node.LowerBound);
        this.WriteBigIntegerValue(node.UpperBound);
        this.WriteBooleanValue(node.HasLowerBound);
        this.WriteBooleanValue(node.HasUpperBound);
        this.WriteBooleanValue(node.LowerEndpointIncluded);
        this.WriteBooleanValue(node.UpperEndpointIncluded);
        this.WriteCountValue(node.Children.Length);
        int index = 0;
        while (index < node.Children.Length)
        {
            this.WriteConstraintNode(node.Children[index]);
            index = index + 1;
        }

        this.WriteConstraintArray(node.NestedConstraints);
        this.Resources.LeaveNesting();
    }

    internal void WriteIntervalArray(LoweredIntervalIr[] intervals)
    {
        this.WriteCountValue(intervals.Length);
        int index = 0;
        while (index < intervals.Length)
        {
            LoweredIntervalIr interval = intervals[index];
            this.WriteNodeMetadata(
                interval.StableId,
                interval.SourceSpan,
                interval.Derivations);
            this.WriteBigIntegerValue(interval.LowerBound);
            this.WriteBigIntegerValue(interval.UpperBound);
            this.WriteBooleanValue(interval.HasLowerBound);
            this.WriteBooleanValue(interval.HasUpperBound);
            index = index + 1;
        }
    }

    internal void WriteExtensionPartition(
        LoweredExtensionPartitionIr extension,
        LoweredFieldIr[] fields)
    {
        this.WriteNodeMetadata(
            extension.StableId,
            extension.SourceSpan,
            extension.Derivations);
        this.WriteBooleanValue(extension.Extensible);
        this.WriteFieldReferenceArray(extension.RootFields, fields);
        this.WriteFieldReferenceArray(extension.AdditionalFields, fields);
        this.WriteNamedValueArray(extension.RootNamedValues);
        this.WriteNamedValueArray(extension.AdditionalNamedValues);
    }

    internal void WriteFieldReferenceArray(
        LoweredFieldIr[] references,
        LoweredFieldIr[] fields)
    {
        this.WriteCountValue(references.Length);
        int referenceIndex = 0;
        while (referenceIndex < references.Length)
        {
            int fieldIndex = 0;
            while (fieldIndex < fields.Length && fields[fieldIndex] != references[referenceIndex])
            {
                fieldIndex = fieldIndex + 1;
            }

            if (fieldIndex == fields.Length)
            {
                throw new System.InvalidOperationException(
                    "An extension partition references an unknown field.");
            }

            this.WriteInt32Value(fieldIndex);
            referenceIndex = referenceIndex + 1;
        }
    }

    internal void WriteNullableObjectClass(LoweredObjectClassIr? objectClass)
    {
        this.WriteBooleanValue(objectClass != null);
        if (objectClass != null)
        {
            this.WriteObjectClass(objectClass);
        }
    }

    internal void WriteObjectClass(LoweredObjectClassIr objectClass)
    {
        this.WriteNodeMetadata(
            objectClass.StableId,
            objectClass.SourceSpan,
            objectClass.Derivations);
        this.WriteStringValue(objectClass.Name);
        this.WriteCountValue(objectClass.Fields.Length);
        int index = 0;
        while (index < objectClass.Fields.Length)
        {
            this.WriteObjectClassField(objectClass.Fields[index]);
            index = index + 1;
        }

        this.WriteStringArray(objectClass.SyntaxTokens);
    }

    internal void WriteObjectClassField(LoweredObjectClassFieldIr field)
    {
        this.WriteNodeMetadata(field.StableId, field.SourceSpan, field.Derivations);
        this.WriteStringValue(field.Name);
        this.WriteInt32Value((int)field.Kind);
        this.WriteTypeReference(field.Type);
        this.WriteStringValue(field.GovernorObjectClassName);
        this.WriteStringArray(field.GovernorFieldPath);
        this.WriteBooleanValue(field.Optional);
        this.WriteBooleanValue(field.Unique);
        this.WriteBooleanValue(field.HasDefaultSetting);
        this.WriteNullableObjectSetting(field.DefaultSetting);
    }

    internal void WriteNullableObject(LoweredObjectIr? value)
    {
        this.WriteBooleanValue(value != null);
        if (value != null)
        {
            this.WriteObject(value);
        }
    }

    internal void WriteObject(LoweredObjectIr value)
    {
        this.EnterNesting();
        this.WriteNodeMetadata(value.StableId, value.SourceSpan, value.Derivations);
        this.WriteStringValue(value.Name);
        this.WriteStringValue(value.ObjectClassName);
        this.WriteCountValue(value.FieldSettings.Length);
        int index = 0;
        while (index < value.FieldSettings.Length)
        {
            this.WriteObjectSetting(value.FieldSettings[index]);
            index = index + 1;
        }

        this.Resources.LeaveNesting();
    }

    internal void WriteNullableObjectSetting(LoweredObjectFieldSettingIr? setting)
    {
        this.WriteBooleanValue(setting != null);
        if (setting != null)
        {
            this.WriteObjectSetting(setting);
        }
    }

    internal void WriteObjectSetting(LoweredObjectFieldSettingIr setting)
    {
        this.WriteNodeMetadata(setting.StableId, setting.SourceSpan, setting.Derivations);
        this.WriteStringValue(setting.FieldName);
        this.WriteTypeReference(setting.Type);
        this.WriteTypeReference(setting.ValueSetType);
        this.WriteNullableValue(setting.Value);
        this.WriteStringValue(setting.ObjectStableId);
        this.WriteStringValue(setting.ObjectSetStableId);
    }

    internal void WriteNullableObjectSet(LoweredObjectSetIr? set)
    {
        this.WriteBooleanValue(set != null);
        if (set != null)
        {
            this.WriteObjectSet(set);
        }
    }

    internal void WriteObjectSet(LoweredObjectSetIr set)
    {
        this.EnterNesting();
        this.WriteNodeMetadata(set.StableId, set.SourceSpan, set.Derivations);
        this.WriteStringValue(set.Name);
        this.WriteStringValue(set.ObjectClassName);
        this.WriteBooleanValue(set.Extensible);
        this.WriteCountValue(set.RootObjects.Length);
        int index = 0;
        while (index < set.RootObjects.Length)
        {
            this.WriteObject(set.RootObjects[index]);
            index = index + 1;
        }

        this.WriteCountValue(set.AdditionalObjects.Length);
        index = 0;
        while (index < set.AdditionalObjects.Length)
        {
            this.WriteObject(set.AdditionalObjects[index]);
            index = index + 1;
        }

        this.WriteNullableObjectTable(set.Table);
        this.Resources.LeaveNesting();
    }

    internal void WriteObjectTableArray(LoweredObjectTableIr[] tables)
    {
        this.WriteCountValue(tables.Length);
        int index = 0;
        while (index < tables.Length)
        {
            this.WriteObjectTable(tables[index]);
            index = index + 1;
        }
    }

    internal void WriteNullableObjectTable(LoweredObjectTableIr? table)
    {
        this.WriteBooleanValue(table != null);
        if (table != null)
        {
            this.WriteObjectTable(table);
        }
    }

    internal void WriteObjectTable(LoweredObjectTableIr table)
    {
        this.WriteNodeMetadata(table.StableId, table.SourceSpan, table.Derivations);
        this.WriteInt32Value((int)table.Kind);
        this.WriteStringValue(table.ObjectClassName);
        this.WriteStringValue(table.UniqueFieldName);
        this.WriteCountValue(table.Entries.Length);
        int index = 0;
        while (index < table.Entries.Length)
        {
            LoweredObjectTableEntryIr entry = table.Entries[index];
            this.WriteNodeMetadata(entry.StableId, entry.SourceSpan, entry.Derivations);
            this.WriteNullableValue(entry.Key);
            this.WriteTypeReference(entry.Type);
            index = index + 1;
        }
    }

    internal void WriteInstructionArray(LoweredInstructionIr[] instructions)
    {
        this.WriteCountValue(instructions.Length);
        int index = 0;
        while (index < instructions.Length)
        {
            LoweredInstructionIr instruction = instructions[index];
            this.WriteNodeMetadata(
                instruction.StableId,
                instruction.SourceSpan,
                instruction.Derivations);
            this.WriteStringValue(instruction.EncodingReference);
            this.WriteStringValue(instruction.Name);
            this.WriteInt32Value((int)instruction.Kind);
            this.WriteBooleanValue(instruction.Negated);
            this.WriteStringArray(instruction.Arguments);
            this.WriteStringArray(instruction.Targets);
            index = index + 1;
        }
    }

    internal void WritePerInstructionRegistrationArray(
        AsnPerInstructionRegistration[] registrations)
    {
        this.WriteCountValue(registrations.Length);
        int index = 0;
        while (index < registrations.Length)
        {
            AsnPerInstructionRegistration registration = registrations[index];
            this.WriteStringValue(registration.Keyword);
            this.WriteStringValue(registration.HandlerId);
            this.WriteBooleanValue(registration.SupportsAligned);
            this.WriteBooleanValue(registration.SupportsUnaligned);
            this.WriteBooleanValue(registration.Reversible);
            this.WriteStringArray(registration.ConflictingKeywords);
            index = index + 1;
        }
    }

    internal void WritePlanStepArray(AsnEncodingPlanStep[] steps)
    {
        this.WriteCountValue(steps.Length);
        int index = 0;
        while (index < steps.Length)
        {
            AsnEncodingPlanStep step = steps[index];
            this.WriteInt32Value((int)step.Kind);
            this.WriteBooleanValue(step.Instruction != null);
            if (step.Instruction != null)
            {
                this.WritePublicInstruction(step.Instruction);
            }

            this.WriteStringValue(step.Path);
            this.WritePlanStepArray(step.Children);
            index = index + 1;
        }
    }

    internal void WritePublicInstruction(AsnEncodingInstruction instruction)
    {
        this.WriteStringValue(instruction.Name);
        this.WriteInt32Value((int)instruction.Kind);
        this.WriteBooleanValue(instruction.Negated);
        this.WriteStringArray(instruction.Arguments);
        this.WriteStringArray(instruction.Targets);
        this.WriteNullableSpan(instruction.Span);
    }

    internal void WriteNodeMetadata(
        string stableId,
        AsnSourceSpan? span,
        LoweredDerivationIr[] derivations)
    {
        this.WriteStringValue(stableId);
        this.WriteNullableSpan(span);
        this.WriteCountValue(derivations.Length);
        int index = 0;
        while (index < derivations.Length)
        {
            LoweredDerivationIr derivation = derivations[index];
            this.WriteStringValue(derivation.StableId);
            this.WriteInt32Value((int)derivation.Kind);
            this.WriteStringValue(derivation.Name);
            this.WriteNullableSpan(derivation.Span);
            index = index + 1;
        }
    }

    internal void WriteNullableSpan(AsnSourceSpan? span)
    {
        this.WriteBooleanValue(span != null);
        if (span != null)
        {
            this.WritePosition(span.Start);
            this.WritePosition(span.End);
        }
    }

    internal void WritePosition(AsnSourcePosition position)
    {
        this.WriteStringValue(position.SourceName);
        this.WriteInt32Value(position.Offset);
        this.WriteInt32Value(position.Line);
        this.WriteInt32Value(position.Column);
    }

    internal void WriteTypeReference(LoweredTypeIr? type)
    {
        this.WriteInt32Value(this.TypeCollector.FindTypeIndex(type));
    }

    internal void WriteStringArray(string[] values)
    {
        this.WriteCountValue(values.Length);
        int index = 0;
        while (index < values.Length)
        {
            this.WriteStringValue(values[index]);
            index = index + 1;
        }
    }

    internal void WriteStringValue(string value)
    {
        if (!this.Resources.AllowsTextCharacters(value.Length))
        {
            this.ThrowResourceLimit("IR text exceeds the configured character limit.");
        }

        int byteCount = System.Text.Encoding.UTF8.GetByteCount(value);
        if (!this.Resources.TryChargeAllocationBytes(byteCount))
        {
            this.ThrowResourceLimit("IR text exceeds the configured allocation limit.");
        }

        byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
        this.WriteLengthValue(bytes.Length);
        this.WriteRawBytes(bytes);
    }

    internal void WriteByteArrayValue(byte[] value)
    {
        if (!this.Resources.AllowsOpenTypeBytes(value.Length))
        {
            this.ThrowResourceLimit("IR byte data exceeds the configured byte limit.");
        }

        this.WriteLengthValue(value.Length);
        this.WriteRawBytes(value);
    }

    internal void WriteBigIntegerValue(System.Numerics.BigInteger value)
    {
        byte[] bytes = BigIntegerConversions.ToSignedBigEndian(value);
        long bits = (long)bytes.Length * 8;
        if (!this.Resources.Limits.Unlimited
            && bits > (long)this.Resources.Limits.MaximumIntegerBits + 8)
        {
            this.ThrowResourceLimit("IR integer exceeds the configured bit limit.");
        }

        this.WriteLengthValue(bytes.Length);
        this.WriteRawBytes(bytes);
    }

    internal void WriteCountValue(int count)
    {
        if (!this.Resources.AllowsCollectionElements(count))
        {
            this.ThrowResourceLimit("IR collection exceeds the configured element limit.");
        }

        this.WriteInt32Value(count);
    }

    internal void WriteLengthValue(int length)
    {
        if (length < 0)
        {
            throw new System.ArgumentOutOfRangeException("length");
        }

        this.WriteInt32Value(length);
    }

    internal void WriteBooleanValue(bool value)
    {
        this.WriteByteValue(value ? (byte)1 : (byte)0);
    }

    internal void WriteInt32Value(int value)
    {
        byte[] bytes = new byte[4];
        bytes[0] = (byte)(value & 255);
        bytes[1] = (byte)(value >> 8 & 255);
        bytes[2] = (byte)(value >> 16 & 255);
        bytes[3] = (byte)(value >> 24 & 255);
        this.WriteRawBytes(bytes);
    }

    internal void WriteByteValue(byte value)
    {
        if (!this.Resources.TryChargeAllocationBytes(1))
        {
            this.ThrowResourceLimit("IR output exceeds the configured allocation limit.");
        }

        this.Stream.WriteByte(value);
    }

    internal void WriteRawBytes(byte[] bytes)
    {
        if (!this.Resources.TryChargeAllocationBytes(bytes.Length))
        {
            this.ThrowResourceLimit("IR output exceeds the configured allocation limit.");
        }

        this.Stream.Write(bytes, 0, bytes.Length);
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
