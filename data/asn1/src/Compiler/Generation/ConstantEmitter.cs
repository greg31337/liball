namespace asn1;

internal class ConstantEmitter
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();
    internal ModelEmitter ModelEmitter = new ModelEmitter();
    internal CSharpGenerationPlan? GenerationPlan;

    internal CSharpConstantEmission EmitConstants(
        LoweredModuleIr module,
        string namespaceName,
        CSharpNameScope typeScope)
    {
        if (module == null)
        {
            throw new System.ArgumentNullException("module");
        }

        if (namespaceName == null)
        {
            throw new System.ArgumentNullException("namespaceName");
        }

        if (typeScope == null)
        {
            throw new System.ArgumentNullException("typeScope");
        }

        if (namespaceName.Length == 0)
        {
            throw new System.ArgumentException(
                "Generated constants require a namespace.",
                "namespaceName");
        }

        System.Collections.Generic.List<LoweredAssignmentIr> moduleValues =
            new System.Collections.Generic.List<LoweredAssignmentIr>();
        System.Collections.Generic.List<LoweredAssignmentIr> namedTypes =
            new System.Collections.Generic.List<LoweredAssignmentIr>();
        int assignmentIndex = 0;
        while (assignmentIndex < module.Assignments.Length)
        {
            LoweredAssignmentIr assignment = module.Assignments[assignmentIndex];
            if (assignment.Kind == AsnAssignmentKind.Value
                && assignment.Type != null
                && assignment.Value != null)
            {
                moduleValues.Add(assignment);
            }

            if (assignment.Kind == AsnAssignmentKind.Type
                && assignment.Type != null
                && assignment.Type.NamedValues.Length > 0)
            {
                namedTypes.Add(assignment);
            }

            assignmentIndex = assignmentIndex + 1;
        }

        CSharpTextWriter writer = new CSharpTextWriter();
        writer.WriteLine("using asn1;");
        writer.WriteLine("");
        writer.WriteLine("namespace " + namespaceName + ";");
        System.Collections.Generic.List<CSharpNameMapping> containerNames =
            new System.Collections.Generic.List<CSharpNameMapping>();
        System.Collections.Generic.List<CSharpNameMapping> valueNames =
            new System.Collections.Generic.List<CSharpNameMapping>();
        CSharpModulePlan? modulePlan = this.GenerationPlan == null
            ? null
            : this.GenerationPlan.FindModulePlan(module);
        int plannedNameIndex = 0;
        CSharpNameMapping? moduleValuesName = null;
        if (moduleValues.Count > 0)
        {
            writer.WriteLine("");
            moduleValuesName = modulePlan == null
                ? this.NameMapper.MapTypeName(
                    module.Name + "-values",
                    module.StableId + "/generated-values",
                    typeScope)
                : modulePlan.ConstantTypeNames[plannedNameIndex];
            plannedNameIndex = plannedNameIndex + 1;
            this.EmitModuleValues(
                writer,
                module,
                moduleValues,
                moduleValuesName,
                valueNames);
        }

        int namedTypeIndex = 0;
        while (namedTypeIndex < namedTypes.Count)
        {
            LoweredAssignmentIr assignment = namedTypes[namedTypeIndex];
            writer.WriteLine("");
            CSharpNameMapping containerName = modulePlan == null
                ? this.NameMapper.MapTypeName(
                    assignment.Name + "-named-values",
                    assignment.StableId + "/generated-named-values",
                    typeScope)
                : modulePlan.ConstantTypeNames[plannedNameIndex];
            plannedNameIndex = plannedNameIndex + 1;
            containerNames.Add(containerName);
            this.EmitNamedValues(
                writer,
                assignment,
                containerName,
                valueNames);
            namedTypeIndex = namedTypeIndex + 1;
        }

        return new CSharpConstantEmission
        {
            Content = writer.GetText(),
            ModuleValuesName = moduleValuesName,
            NamedValueContainerNames = containerNames.ToArray(),
            ValueNames = valueNames.ToArray()
        };
    }

    internal void EmitModuleValues(
        CSharpTextWriter writer,
        LoweredModuleIr module,
        System.Collections.Generic.List<LoweredAssignmentIr> assignments,
        CSharpNameMapping containerName,
        System.Collections.Generic.List<CSharpNameMapping> allValueNames)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Contains generated values declared by ASN.1 module <c>"
                + this.ModelEmitter.EscapeXml(module.Name) + "</c>.");
        writer.WriteLine("public static class " + containerName.GeneratedName);
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        CSharpNameScope memberScope = new CSharpNameScope(false);
        int index = 0;
        while (index < assignments.Count)
        {
            LoweredAssignmentIr assignment = assignments[index];
            CSharpNameMapping valueName = this.NameMapper.MapMemberName(
                assignment.Name,
                assignment.StableId,
                memberScope);
            allValueNames.Add(valueName);
            if (index > 0)
            {
                writer.WriteLine("");
            }

            this.ModelEmitter.WriteSummary(
                writer,
                "Contains ASN.1 value <c>"
                    + this.ModelEmitter.EscapeXml(assignment.Name) + "</c>.");
            LoweredTypeIr type = assignment.Type
                ?? throw new System.InvalidOperationException();
            LoweredValueIr value = assignment.Value
                ?? throw new System.InvalidOperationException();
            writer.WriteLine("public static readonly "
                + this.GetConstantTypeName(type) + " "
                + valueName.GeneratedName + " = "
                + this.CreateValueInitializer(type, value) + ";");
            index = index + 1;
        }

        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitNamedValues(
        CSharpTextWriter writer,
        LoweredAssignmentIr assignment,
        CSharpNameMapping containerName,
        System.Collections.Generic.List<CSharpNameMapping> allValueNames)
    {
        LoweredTypeIr type = assignment.Type
            ?? throw new System.InvalidOperationException();
        this.ModelEmitter.WriteSummary(
            writer,
            "Contains named values declared by ASN.1 type <c>"
                + this.ModelEmitter.EscapeXml(assignment.Name) + "</c>.");
        writer.WriteLine("public static class " + containerName.GeneratedName);
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        CSharpNameScope memberScope = new CSharpNameScope(false);
        int index = 0;
        while (index < type.NamedValues.Length)
        {
            LoweredNamedValueIr named = type.NamedValues[index];
            if (named.Value == null)
            {
                throw new System.InvalidOperationException(
                    "A generated named value requires a resolved value.");
            }

            CSharpNameMapping valueName = this.NameMapper.MapMemberName(
                named.Name,
                named.StableId,
                memberScope);
            allValueNames.Add(valueName);
            if (index > 0)
            {
                writer.WriteLine("");
            }

            this.ModelEmitter.WriteSummary(
                writer,
                "Contains named value <c>"
                    + this.ModelEmitter.EscapeXml(named.Name) + "</c>.");
            writer.WriteLine("public static readonly "
                + this.GetConstantTypeName(type) + " "
                + valueName.GeneratedName + " = "
                + this.CreateValueInitializer(type, named.Value) + ";");
            index = index + 1;
        }

        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal string GetConstantTypeName(LoweredTypeIr type)
    {
        if (type.Kind == AsnTypeKind.Tagged
            || type.Kind == AsnTypeKind.Prefixed
            || type.Kind == AsnTypeKind.Selection
            || type.Kind == AsnTypeKind.Defined)
        {
            if (type.ElementType == null)
            {
                return "AsnValue";
            }

            return this.GetConstantTypeName(type.ElementType);
        }

        if (type.Kind == AsnTypeKind.Boolean || type.Kind == AsnTypeKind.Null)
        {
            return "bool";
        }

        if (type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated)
        {
            return "System.Numerics.BigInteger";
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return "AsnRealValue";
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return "AsnBitStringValue";
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return "byte[]";
        }

        if (this.ModelEmitter.IsCharacterStringKind(type.Kind)
            || type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return "string";
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return "AsnObjectIdentifierValue";
        }

        if (this.ModelEmitter.IsTimeKind(type.Kind))
        {
            return "AsnTimeValue";
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            if (type.ElementType == null)
            {
                throw new System.InvalidOperationException(
                    "A constant collection requires an element type.");
            }

            return this.GetConstantTypeName(type.ElementType) + "[]";
        }

        return "AsnValue";
    }

    internal string CreateValueInitializer(
        LoweredTypeIr type,
        LoweredValueIr value)
    {
        if (type.Kind == AsnTypeKind.Tagged
            || type.Kind == AsnTypeKind.Prefixed
            || type.Kind == AsnTypeKind.Selection
            || type.Kind == AsnTypeKind.Defined)
        {
            if (type.ElementType == null)
            {
                return this.CreateSchemaValueInitializer(value);
            }

            return this.CreateValueInitializer(type.ElementType, value);
        }

        if (type.Kind == AsnTypeKind.Boolean)
        {
            if (string.Equals(
                value.TextValue,
                "TRUE",
                System.StringComparison.OrdinalIgnoreCase))
            {
                return "true";
            }

            if (string.Equals(
                value.TextValue,
                "FALSE",
                System.StringComparison.OrdinalIgnoreCase))
            {
                return "false";
            }

            throw new System.InvalidOperationException(
                "A generated BOOLEAN value must be TRUE or FALSE.");
        }

        if (type.Kind == AsnTypeKind.Null)
        {
            return "false";
        }

        if (type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated)
        {
            if (!value.HasIntegerValue)
            {
                throw new System.InvalidOperationException(
                    "A generated integer value requires an integer payload.");
            }

            return this.CreateBigInteger(value.IntegerValue);
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return this.CreateRealInitializer(value.RealValue);
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return this.CreateBitStringInitializer(value.BitStringValue);
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.ModelEmitter.CreateByteCollection(value.Octets);
        }

        if (this.ModelEmitter.IsCharacterStringKind(type.Kind)
            || type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return "\"" + this.ModelEmitter.EscapeStringLiteral(value.TextValue)
                + "\"";
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            if (value.ObjectIdentifierValue != null
                && value.ObjectIdentifierValue.Relative
                    != (type.Kind == AsnTypeKind.RelativeObjectIdentifier))
            {
                throw new System.InvalidOperationException(
                    "The object identifier payload does not match its type.");
            }

            return this.CreateObjectIdentifierInitializer(
                value.ObjectIdentifierValue);
        }

        if (this.ModelEmitter.IsTimeKind(type.Kind))
        {
            return this.CreateTimeInitializer(value.TimeValue);
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            if (type.ElementType == null)
            {
                throw new System.InvalidOperationException(
                    "A generated collection value requires an element type.");
            }

            return this.CreateCollectionInitializer(type.ElementType, value.Items);
        }

        return this.CreateSchemaValueInitializer(value);
    }

    internal string CreateBigInteger(System.Numerics.BigInteger value)
    {
        return "System.Numerics.BigInteger.Parse(\""
            + value.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "\")";
    }

    internal string CreateBitStringInitializer(AsnBitStringValue? value)
    {
        if (value == null || !value.IsValid())
        {
            throw new System.InvalidOperationException(
                "A generated BIT STRING value requires valid normalized data.");
        }

        return "new AsnBitStringValue { Bytes = "
            + this.ModelEmitter.CreateByteCollection(value.Bytes)
            + ", BitLength = " + value.BitLength.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " }";
    }

    internal string CreateObjectIdentifierInitializer(
        AsnObjectIdentifierValue? value)
    {
        if (value == null || !value.IsValid())
        {
            throw new System.InvalidOperationException(
                "A generated object identifier requires valid normalized arcs.");
        }

        System.Text.StringBuilder arcs = new System.Text.StringBuilder();
        arcs.Append('[');
        int index = 0;
        while (index < value.Arcs.Length)
        {
            if (index > 0)
            {
                arcs.Append(", ");
            }

            arcs.Append(this.CreateBigInteger(value.Arcs[index]));
            index = index + 1;
        }

        arcs.Append(']');
        return "new AsnObjectIdentifierValue { Relative = "
            + (value.Relative ? "true" : "false")
            + ", Arcs = " + arcs.ToString() + " }";
    }

    internal string CreateRealInitializer(AsnRealValue? value)
    {
        if (value == null || !value.IsValid())
        {
            throw new System.InvalidOperationException(
                "A generated REAL value requires valid normalized data.");
        }

        return "new AsnRealValue { Normalized = "
            + (value.Normalized ? "true" : "false")
            + ", Kind = AsnRealKind." + value.Kind.ToString()
            + ", HasDecimalValue = "
            + (value.HasDecimalValue ? "true" : "false")
            + ", DecimalText = \""
            + this.ModelEmitter.EscapeStringLiteral(value.DecimalText) + "\""
            + ", DecimalCoefficient = "
            + this.CreateBigInteger(value.DecimalCoefficient)
            + ", DecimalExponent = "
            + this.CreateBigInteger(value.DecimalExponent)
            + ", HasBinaryValue = "
            + (value.HasBinaryValue ? "true" : "false")
            + ", Mantissa = " + this.CreateBigInteger(value.Mantissa)
            + ", Radix = " + value.Radix.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ", Exponent = " + this.CreateBigInteger(value.Exponent) + " }";
    }

    internal string CreateTimeInitializer(AsnTimeValue? value)
    {
        if (value == null || !value.IsValid())
        {
            throw new System.InvalidOperationException(
                "A generated time value requires valid normalized data.");
        }

        return "new AsnTimeValue { Kind = AsnTimeKind."
            + value.Kind.ToString() + ", Text = \""
            + this.ModelEmitter.EscapeStringLiteral(value.Text) + "\""
            + ", HasUtcOffsetMinutes = "
            + (value.HasUtcOffsetMinutes ? "true" : "false")
            + ", UtcOffsetMinutes = " + value.UtcOffsetMinutes.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ", HasFractionalDigits = "
            + (value.HasFractionalDigits ? "true" : "false")
            + ", FractionalDigits = " + value.FractionalDigits.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " }";
    }

    internal string CreateCollectionInitializer(
        LoweredTypeIr elementType,
        LoweredValueIr[] values)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append(this.CreateValueInitializer(elementType, values[index]));
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateSchemaValueInitializer(LoweredValueIr value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append("new AsnValue { Kind = AsnValueKind.");
        result.Append(value.Kind.ToString());
        if (value.HasIntegerValue)
        {
            result.Append(", HasIntegerValue = true, IntegerValue = ");
            result.Append(this.CreateBigInteger(value.IntegerValue));
        }

        if (value.TextValue.Length > 0)
        {
            result.Append(", TextValue = \"");
            result.Append(this.ModelEmitter.EscapeStringLiteral(value.TextValue));
            result.Append('"');
        }

        if (value.Octets.Length > 0)
        {
            result.Append(", Octets = ");
            result.Append(this.ModelEmitter.CreateByteCollection(value.Octets));
        }

        if (value.BitStringValue != null)
        {
            result.Append(", BitStringValue = ");
            result.Append(this.CreateBitStringInitializer(value.BitStringValue));
        }

        if (value.ObjectIdentifierValue != null)
        {
            result.Append(", ObjectIdentifierValue = ");
            result.Append(this.CreateObjectIdentifierInitializer(
                value.ObjectIdentifierValue));
        }

        if (value.RealValue != null)
        {
            result.Append(", RealValue = ");
            result.Append(this.CreateRealInitializer(value.RealValue));
        }

        if (value.TimeValue != null)
        {
            result.Append(", TimeValue = ");
            result.Append(this.CreateTimeInitializer(value.TimeValue));
        }

        if (value.Items.Length > 0)
        {
            result.Append(", Items = [");
            int itemIndex = 0;
            while (itemIndex < value.Items.Length)
            {
                if (itemIndex > 0)
                {
                    result.Append(", ");
                }

                result.Append(this.CreateSchemaValueInitializer(
                    value.Items[itemIndex]));
                itemIndex = itemIndex + 1;
            }

            result.Append(']');
        }

        if (value.NamedValues.Length > 0)
        {
            result.Append(", NamedValues = [");
            int namedIndex = 0;
            while (namedIndex < value.NamedValues.Length)
            {
                LoweredNamedValueIr named = value.NamedValues[namedIndex];
                if (named.Value == null)
                {
                    throw new System.InvalidOperationException(
                        "A structured constant requires resolved component values.");
                }

                if (namedIndex > 0)
                {
                    result.Append(", ");
                }

                result.Append("new AsnNamedValue { Name = \"");
                result.Append(this.ModelEmitter.EscapeStringLiteral(named.Name));
                result.Append("\", Value = ");
                result.Append(this.CreateSchemaValueInitializer(named.Value));
                result.Append(" }");
                namedIndex = namedIndex + 1;
            }

            result.Append(']');
        }

        result.Append(" }");
        return result.ToString();
    }
}
