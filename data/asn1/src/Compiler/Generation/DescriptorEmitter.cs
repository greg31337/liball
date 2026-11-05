namespace asn1;

internal class DescriptorEmitter
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();
    internal ModelEmitter ModelEmitter = new ModelEmitter();
    internal ConstantEmitter ConstantEmitter = new ConstantEmitter();
    internal CSharpGenerationPlan? GenerationPlan;
    internal System.Collections.Generic.List<AsnTypeDescriptor> Types =
        new System.Collections.Generic.List<AsnTypeDescriptor>();
    internal System.Collections.Generic.List<AsnFieldDescriptor> Fields =
        new System.Collections.Generic.List<AsnFieldDescriptor>();
    internal System.Collections.Generic.List<AsnValueDescriptor> Values =
        new System.Collections.Generic.List<AsnValueDescriptor>();
    internal System.Collections.Generic.List<AsnConstraintDescriptor> Constraints =
        new System.Collections.Generic.List<AsnConstraintDescriptor>();
    internal System.Collections.Generic.List<AsnObjectTableDescriptor> Tables =
        new System.Collections.Generic.List<AsnObjectTableDescriptor>();
    internal CSharpNameMapping[] TypeNames = [];
    internal CSharpNameMapping[] FieldNames = [];
    internal CSharpNameMapping[] AssignmentNames = [];

    internal CSharpDescriptorEmission EmitDescriptors(
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
                "Generated descriptors require a namespace.",
                "namespaceName");
        }

        this.ResetCollections();
        LoweredCompilationIr compilation = new LoweredCompilationIr
        {
            StableId = "ir:generated-descriptors/" + module.StableId,
            PerInstructionRegistrations = module.PerInstructionRegistrations,
            Modules = [module]
        };
        AsnReflectionCatalog catalog = new ReflectionCatalogBuilder()
            .BuildCatalog(compilation);
        if (catalog.Modules.Length != 1)
        {
            throw new System.InvalidOperationException(
                "Descriptor generation requires exactly one reflected module.");
        }

        AsnModuleDescriptor reflectedModule = catalog.Modules[0];
        this.CollectGraph(catalog, reflectedModule);
        this.CreateGeneratedNames(reflectedModule);
        CSharpModulePlan? modulePlan = this.GenerationPlan == null
            ? null
            : this.GenerationPlan.FindModulePlan(module);
        CSharpNameMapping schemaName = modulePlan == null
            ? this.NameMapper.MapTypeName(
                module.Name + "-schema",
                module.StableId + "/generated-schema",
                typeScope)
            : modulePlan.SchemaName;
        CSharpTextWriter writer = new CSharpTextWriter();
        writer.WriteLine("using asn1;");
        writer.WriteLine("");
        writer.WriteLine("namespace " + namespaceName + ";");
        writer.WriteLine("");
        this.EmitSchemaClass(
            writer,
            catalog,
            reflectedModule,
            schemaName);
        return new CSharpDescriptorEmission
        {
            Content = writer.GetText(),
            SchemaName = schemaName,
            TypeNames = this.TypeNames,
            FieldNames = this.FieldNames,
            AssignmentNames = this.AssignmentNames
        };
    }

    internal void ResetCollections()
    {
        this.Types = new System.Collections.Generic.List<AsnTypeDescriptor>();
        this.Fields = new System.Collections.Generic.List<AsnFieldDescriptor>();
        this.Values = new System.Collections.Generic.List<AsnValueDescriptor>();
        this.Constraints =
            new System.Collections.Generic.List<AsnConstraintDescriptor>();
        this.Tables =
            new System.Collections.Generic.List<AsnObjectTableDescriptor>();
        this.TypeNames = [];
        this.FieldNames = [];
        this.AssignmentNames = [];
    }

    internal void CollectGraph(
        AsnReflectionCatalog catalog,
        AsnModuleDescriptor module)
    {
        AsnTypeDescriptor[] types =
            new ReflectionServiceImplementation().GetTypes(catalog);
        int index = 0;
        while (index < types.Length)
        {
            if (!this.Types.Contains(types[index]))
            {
                this.Types.Add(types[index]);
            }

            index = index + 1;
        }

        index = 0;
        while (index < this.Types.Count)
        {
            AsnTypeDescriptor type = this.Types[index];
            int fieldIndex = 0;
            while (fieldIndex < type.Fields.Length)
            {
                if (!this.Fields.Contains(type.Fields[fieldIndex]))
                {
                    this.Fields.Add(type.Fields[fieldIndex]);
                }

                if (type.Fields[fieldIndex].DefaultValue != null)
                {
                    this.AddValue(type.Fields[fieldIndex].DefaultValue
                        ?? new AsnValueDescriptor());
                }

                fieldIndex = fieldIndex + 1;
            }

            int valueIndex = 0;
            while (valueIndex < type.NamedValues.Length)
            {
                this.AddValue(type.NamedValues[valueIndex]);
                valueIndex = valueIndex + 1;
            }

            int constraintIndex = 0;
            while (constraintIndex < type.Constraints.Length)
            {
                this.AddConstraint(type.Constraints[constraintIndex]);
                constraintIndex = constraintIndex + 1;
            }

            int tableIndex = 0;
            while (tableIndex < type.ObjectTables.Length)
            {
                this.AddTable(type.ObjectTables[tableIndex]);
                tableIndex = tableIndex + 1;
            }

            index = index + 1;
        }

        index = 0;
        while (index < module.Values.Length)
        {
            this.AddValue(module.Values[index]);
            index = index + 1;
        }

        index = 0;
        while (index < module.Assignments.Length)
        {
            AsnAssignmentDescriptor assignment = module.Assignments[index];
            if (assignment.Value != null)
            {
                this.AddValue(assignment.Value ?? new AsnValueDescriptor());
            }

            int tableIndex = 0;
            while (tableIndex < assignment.ObjectTables.Length)
            {
                this.AddTable(assignment.ObjectTables[tableIndex]);
                tableIndex = tableIndex + 1;
            }

            index = index + 1;
        }
    }

    internal void AddValue(AsnValueDescriptor value)
    {
        if (!this.Values.Contains(value))
        {
            this.Values.Add(value);
        }
    }

    internal void AddConstraint(AsnConstraintDescriptor constraint)
    {
        if (this.Constraints.Contains(constraint))
        {
            return;
        }

        this.Constraints.Add(constraint);
        int index = 0;
        while (index < constraint.Applications.Length)
        {
            this.AddConstraint(constraint.Applications[index]);
            index = index + 1;
        }
    }

    internal void AddTable(AsnObjectTableDescriptor table)
    {
        if (this.Tables.Contains(table))
        {
            return;
        }

        this.Tables.Add(table);
        int index = 0;
        while (index < table.Entries.Length)
        {
            if (table.Entries[index].Key != null)
            {
                this.AddValue(table.Entries[index].Key
                    ?? new AsnValueDescriptor());
            }

            index = index + 1;
        }
    }

    internal void CreateGeneratedNames(AsnModuleDescriptor module)
    {
        this.TypeNames = new CSharpNameMapping[this.Types.Count];
        CSharpNameScope typeNameScope = new CSharpNameScope(false);
        int index = 0;
        while (index < this.Types.Count)
        {
            AsnTypeDescriptor type = this.Types[index];
            string sourceName = type.Name.Length == 0
                ? "anonymous-type-" + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                : type.Name;
            this.TypeNames[index] = this.NameMapper.MapTypeName(
                sourceName,
                type.StableId,
                typeNameScope);
            index = index + 1;
        }

        this.FieldNames = new CSharpNameMapping[this.Fields.Count];
        index = 0;
        while (index < this.Types.Count)
        {
            AsnTypeDescriptor type = this.Types[index];
            CSharpNameScope memberScope = new CSharpNameScope(false);
            int fieldIndex = 0;
            while (fieldIndex < type.Fields.Length)
            {
                int globalIndex = this.FindFieldIndex(type.Fields[fieldIndex]);
                if (globalIndex >= 0
                    && this.FieldNames[globalIndex] == null)
                {
                    this.FieldNames[globalIndex] = this.NameMapper.MapMemberName(
                        type.Fields[fieldIndex].Name,
                        type.Fields[fieldIndex].StableId,
                        memberScope);
                }

                fieldIndex = fieldIndex + 1;
            }

            index = index + 1;
        }

        this.AssignmentNames =
            new CSharpNameMapping[module.Assignments.Length];
        CSharpNameScope assignmentScope = new CSharpNameScope(false);
        index = 0;
        while (index < module.Assignments.Length)
        {
            this.AssignmentNames[index] = this.NameMapper.MapMemberName(
                module.Assignments[index].Name,
                module.Assignments[index].StableId,
                assignmentScope);
            index = index + 1;
        }
    }

    internal void EmitSchemaClass(
        CSharpTextWriter writer,
        AsnReflectionCatalog catalog,
        AsnModuleDescriptor module,
        CSharpNameMapping schemaName)
    {
        this.ModelEmitter.WriteSummary(
            writer,
            "Contains generated reflection descriptors for ASN.1 module <c>"
                + this.ModelEmitter.EscapeXml(module.Name) + "</c>.");
        writer.WriteLine("public class " + schemaName.GeneratedName);
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        this.ModelEmitter.WriteSummary(
            writer,
            "Contains the generated reflection catalog.");
        writer.WriteLine(
            "public AsnReflectionCatalog Catalog = new AsnReflectionCatalog();");
        writer.WriteLine("");
        this.ModelEmitter.WriteSummary(
            writer,
            "Contains the generated module descriptor.");
        writer.WriteLine(
            "public AsnModuleDescriptor Module = new AsnModuleDescriptor();");
        writer.WriteLine("");
        this.ModelEmitter.WriteSummary(
            writer,
            "Initializes the generated descriptor graph.");
        writer.WriteLine("public " + schemaName.GeneratedName + "()");
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        this.EmitAllocations(writer);
        this.EmitValueDescriptors(writer);
        this.EmitConstraintDescriptors(writer);
        this.EmitObjectTables(writer);
        this.EmitFieldDescriptors(writer);
        this.EmitTypeDescriptors(writer);
        this.EmitModuleAndCatalog(writer, catalog, module);
        writer.DecreaseIndentation();
        writer.WriteLine("}");
        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitAllocations(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Types.Count)
        {
            writer.WriteLine("AsnTypeDescriptor type" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + " = new AsnTypeDescriptor();");
            index = index + 1;
        }

        index = 0;
        while (index < this.Fields.Count)
        {
            writer.WriteLine("AsnFieldDescriptor field" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + " = new AsnFieldDescriptor();");
            index = index + 1;
        }

        index = 0;
        while (index < this.Values.Count)
        {
            writer.WriteLine("AsnValueDescriptor value" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + " = new AsnValueDescriptor();");
            index = index + 1;
        }

        index = 0;
        while (index < this.Constraints.Count)
        {
            writer.WriteLine("AsnConstraintDescriptor constraint"
                + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + " = new AsnConstraintDescriptor();");
            index = index + 1;
        }

        index = 0;
        while (index < this.Tables.Count)
        {
            writer.WriteLine("AsnObjectTableDescriptor table" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
                + " = new AsnObjectTableDescriptor();");
            index = index + 1;
        }

        if (this.Types.Count + this.Fields.Count + this.Values.Count
            + this.Constraints.Count + this.Tables.Count > 0)
        {
            writer.WriteLine("");
        }
    }

    internal void EmitValueDescriptors(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Values.Count)
        {
            AsnValueDescriptor value = this.Values[index];
            string prefix = "value" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".";
            writer.WriteLine(prefix + "StableId = "
                + this.Quote(value.StableId) + ";");
            writer.WriteLine(prefix + "Name = " + this.Quote(value.Name) + ";");
            writer.WriteLine(prefix + "GeneratedName = "
                + this.Quote(this.MapLooseMemberName(value.Name)) + ";");
            writer.WriteLine(prefix + "Kind = AsnValueKind."
                + value.Kind.ToString() + ";");
            if (value.Value != null)
            {
                writer.WriteLine(prefix + "Value = "
                    + this.CreateValueExpression(value.Value) + ";");
            }

            writer.WriteLine(prefix + "SourceSpan = "
                + this.CreateSpanExpression(value.SourceSpan) + ";");
            writer.WriteLine(prefix + "Derivations = "
                + this.CreateDerivationArray(value.Derivations) + ";");
            writer.WriteLine("");
            index = index + 1;
        }
    }

    internal void EmitConstraintDescriptors(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Constraints.Count)
        {
            AsnConstraintDescriptor constraint = this.Constraints[index];
            string prefix = "constraint" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".";
            writer.WriteLine(prefix + "StableId = "
                + this.Quote(constraint.StableId) + ";");
            writer.WriteLine(prefix + "Kind = AsnConstraintKind."
                + constraint.Kind.ToString() + ";");
            writer.WriteLine(prefix + "DomainKind = AsnTypeKind."
                + constraint.DomainKind.ToString() + ";");
            if (constraint.Root != null)
            {
                writer.WriteLine(prefix + "Root = "
                    + this.CreateConstraintElementExpression(constraint.Root)
                    + ";");
            }

            if (constraint.Extension != null)
            {
                writer.WriteLine(prefix + "Extension = "
                    + this.CreateConstraintElementExpression(
                        constraint.Extension) + ";");
            }

            writer.WriteLine(prefix + "Applications = "
                + this.CreateConstraintReferenceArray(constraint.Applications)
                + ";");
            writer.WriteLine(prefix + "RootIntervals = "
                + this.CreateIntervalArray(constraint.RootIntervals) + ";");
            writer.WriteLine(prefix + "ExtensionIntervals = "
                + this.CreateIntervalArray(constraint.ExtensionIntervals) + ";");
            writer.WriteLine(prefix + "Extensible = "
                + this.BooleanText(constraint.Extensible) + ";");
            writer.WriteLine(prefix + "OpenExtension = "
                + this.BooleanText(constraint.OpenExtension) + ";");
            writer.WriteLine(prefix + "SourceSpan = "
                + this.CreateSpanExpression(constraint.SourceSpan) + ";");
            writer.WriteLine(prefix + "Derivations = "
                + this.CreateDerivationArray(constraint.Derivations) + ";");
            writer.WriteLine("");
            index = index + 1;
        }
    }

    internal void EmitObjectTables(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Tables.Count)
        {
            AsnObjectTableDescriptor table = this.Tables[index];
            string prefix = "table" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".";
            writer.WriteLine(prefix + "StableId = "
                + this.Quote(table.StableId) + ";");
            writer.WriteLine(prefix + "Kind = AsnConstraintKind."
                + table.Kind.ToString() + ";");
            writer.WriteLine(prefix + "ObjectClassName = "
                + this.Quote(table.ObjectClassName) + ";");
            writer.WriteLine(prefix + "UniqueFieldName = "
                + this.Quote(table.UniqueFieldName) + ";");
            writer.WriteLine(prefix + "Entries = "
                + this.CreateTableEntryArray(table.Entries) + ";");
            writer.WriteLine(prefix + "SourceSpan = "
                + this.CreateSpanExpression(table.SourceSpan) + ";");
            writer.WriteLine(prefix + "Derivations = "
                + this.CreateDerivationArray(table.Derivations) + ";");
            writer.WriteLine("");
            index = index + 1;
        }
    }

    internal void EmitFieldDescriptors(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Fields.Count)
        {
            AsnFieldDescriptor field = this.Fields[index];
            string prefix = "field" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".";
            writer.WriteLine(prefix + "StableId = "
                + this.Quote(field.StableId) + ";");
            writer.WriteLine(prefix + "Name = " + this.Quote(field.Name) + ";");
            writer.WriteLine(prefix + "GeneratedName = "
                + this.Quote(this.FieldNames[index].GeneratedName) + ";");
            writer.WriteLine(prefix + "Index = " + field.Index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ";");
            if (field.Type != null)
            {
                writer.WriteLine(prefix + "Type = type"
                    + this.FindTypeIndex(field.Type).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ";");
            }

            writer.WriteLine(prefix + "Optional = "
                + this.BooleanText(field.Optional) + ";");
            writer.WriteLine(prefix + "HasDefaultValue = "
                + this.BooleanText(field.HasDefaultValue) + ";");
            if (field.DefaultValue != null)
            {
                writer.WriteLine(prefix + "DefaultValue = value"
                    + this.FindValueIndex(field.DefaultValue).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ";");
            }

            writer.WriteLine(prefix + "Extension = "
                + this.BooleanText(field.Extension) + ";");
            writer.WriteLine(prefix + "ExtensionGroupIndex = "
                + field.ExtensionGroupIndex.ToString(
                    System.Globalization.CultureInfo.InvariantCulture) + ";");
            writer.WriteLine(prefix + "Tags = "
                + this.CreateTagArray(field.Tags) + ";");
            writer.WriteLine(prefix + "Encodings = "
                + this.CreateEncodingArray(field.Encodings) + ";");
            writer.WriteLine(prefix + "SourceSpan = "
                + this.CreateSpanExpression(field.SourceSpan) + ";");
            writer.WriteLine(prefix + "Derivations = "
                + this.CreateDerivationArray(field.Derivations) + ";");
            writer.WriteLine("");
            index = index + 1;
        }
    }

    internal void EmitTypeDescriptors(CSharpTextWriter writer)
    {
        int index = 0;
        while (index < this.Types.Count)
        {
            AsnTypeDescriptor type = this.Types[index];
            string prefix = "type" + index.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + ".";
            writer.WriteLine(prefix + "StableId = "
                + this.Quote(type.StableId) + ";");
            writer.WriteLine(prefix + "Name = " + this.Quote(type.Name) + ";");
            writer.WriteLine(prefix + "GeneratedName = "
                + this.Quote(this.TypeNames[index].GeneratedName) + ";");
            writer.WriteLine(prefix + "Kind = AsnTypeKind."
                + type.Kind.ToString() + ";");
            writer.WriteLine(prefix + "Fields = "
                + this.CreateFieldReferenceArray(type.Fields) + ";");
            writer.WriteLine(prefix + "RootFields = "
                + this.CreateFieldReferenceArray(type.RootFields) + ";");
            writer.WriteLine(prefix + "ExtensionFields = "
                + this.CreateFieldReferenceArray(type.ExtensionFields) + ";");
            if (type.ElementType != null)
            {
                writer.WriteLine(prefix + "ElementType = type"
                    + this.FindTypeIndex(type.ElementType).ToString(
                        System.Globalization.CultureInfo.InvariantCulture)
                    + ";");
            }

            writer.WriteLine(prefix + "Tags = "
                + this.CreateTagArray(type.Tags) + ";");
            writer.WriteLine(prefix + "NamedValues = "
                + this.CreateValueReferenceArray(type.NamedValues) + ";");
            writer.WriteLine(prefix + "RootNamedValues = "
                + this.CreateValueReferenceArray(type.RootNamedValues) + ";");
            writer.WriteLine(prefix + "ExtensionNamedValues = "
                + this.CreateValueReferenceArray(type.ExtensionNamedValues) + ";");
            writer.WriteLine(prefix + "Constraints = "
                + this.CreateConstraintReferenceArray(type.Constraints) + ";");
            writer.WriteLine(prefix + "Encodings = "
                + this.CreateEncodingArray(type.Encodings) + ";");
            writer.WriteLine(prefix + "ObjectTables = "
                + this.CreateTableReferenceArray(type.ObjectTables) + ";");
            writer.WriteLine(prefix + "Extensible = "
                + this.BooleanText(type.Extensible) + ";");
            writer.WriteLine(prefix + "ParameterReference = "
                + this.BooleanText(type.ParameterReference) + ";");
            writer.WriteLine(prefix + "SourceSpan = "
                + this.CreateSpanExpression(type.SourceSpan) + ";");
            writer.WriteLine(prefix + "Derivations = "
                + this.CreateDerivationArray(type.Derivations) + ";");
            writer.WriteLine("");
            index = index + 1;
        }
    }

    internal void EmitModuleAndCatalog(
        CSharpTextWriter writer,
        AsnReflectionCatalog catalog,
        AsnModuleDescriptor module)
    {
        int index = 0;
        while (index < module.Assignments.Length)
        {
            writer.WriteLine("AsnAssignmentDescriptor assignment"
                + index.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + " = " + this.CreateAssignmentExpression(
                    module.Assignments[index],
                    index) + ";");
            index = index + 1;
        }

        writer.WriteLine("");
        writer.WriteLine("this.Module.StableId = "
            + this.Quote(module.StableId) + ";");
        writer.WriteLine("this.Module.Name = " + this.Quote(module.Name) + ";");
        writer.WriteLine("this.Module.GeneratedName = "
            + this.Quote(this.MapLooseTypeName(module.Name)) + ";");
        writer.WriteLine("this.Module.ObjectIdentifier = "
            + this.Quote(module.ObjectIdentifier) + ";");
        writer.WriteLine("this.Module.Iri = " + this.Quote(module.Iri) + ";");
        writer.WriteLine("this.Module.VersionModule = "
            + this.Quote(module.VersionModule) + ";");
        writer.WriteLine("this.Module.Version = "
            + this.Quote(module.Version) + ";");
        writer.WriteLine("this.Module.Imports = "
            + this.CreateImportArray(module.Imports) + ";");
        writer.WriteLine("this.Module.Exports = "
            + this.CreateStringArray(module.Exports) + ";");
        writer.WriteLine("this.Module.Assignments = "
            + this.CreateAssignmentReferenceArray(module.Assignments.Length)
            + ";");
        writer.WriteLine("this.Module.Types = "
            + this.CreateTypeReferenceArray(module.Types) + ";");
        writer.WriteLine("this.Module.Values = "
            + this.CreateValueReferenceArray(module.Values) + ";");
        writer.WriteLine("this.Module.Encodings = "
            + this.CreateEncodingArray(module.Encodings) + ";");
        writer.WriteLine("this.Module.SourceSpan = "
            + this.CreateSpanExpression(module.SourceSpan) + ";");
        writer.WriteLine("this.Module.Derivations = "
            + this.CreateDerivationArray(module.Derivations) + ";");
        writer.WriteLine("");
        writer.WriteLine("this.Catalog.StableId = "
            + this.Quote(catalog.StableId) + ";");
        writer.WriteLine("this.Catalog.Modules = [this.Module];");
        writer.WriteLine("this.Catalog.Assignments = "
            + this.CreateAssignmentReferenceArray(module.Assignments.Length)
            + ";");
    }

    internal string CreateAssignmentExpression(
        AsnAssignmentDescriptor assignment,
        int index)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append("new AsnAssignmentDescriptor { StableId = ");
        result.Append(this.Quote(assignment.StableId));
        result.Append(", Name = ");
        result.Append(this.Quote(assignment.Name));
        result.Append(", QualifiedName = ");
        result.Append(this.Quote(assignment.QualifiedName));
        result.Append(", GeneratedName = ");
        result.Append(this.Quote(this.AssignmentNames[index].GeneratedName));
        result.Append(", Kind = AsnAssignmentKind.");
        result.Append(assignment.Kind.ToString());
        result.Append(", Exported = ");
        result.Append(this.BooleanText(assignment.Exported));
        result.Append(", Parameterized = ");
        result.Append(this.BooleanText(assignment.Parameterized));
        result.Append(", Parameters = ");
        result.Append(this.CreateParameterArray(assignment.Parameters));
        if (assignment.Type != null)
        {
            result.Append(", Type = type");
            result.Append(this.FindTypeIndex(assignment.Type).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }

        if (assignment.Value != null)
        {
            result.Append(", Value = value");
            result.Append(this.FindValueIndex(assignment.Value).ToString(
                System.Globalization.CultureInfo.InvariantCulture));
        }

        result.Append(", ObjectTables = ");
        result.Append(this.CreateTableReferenceArray(assignment.ObjectTables));
        result.Append(", SourceSpan = ");
        result.Append(this.CreateSpanExpression(assignment.SourceSpan));
        result.Append(", Derivations = ");
        result.Append(this.CreateDerivationArray(assignment.Derivations));
        result.Append(" }");
        return result.ToString();
    }

    internal string CreateConstraintElementExpression(AsnConstraintElement value)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append("new AsnConstraintElement { Kind = AsnConstraintKind.");
        result.Append(value.Kind.ToString());
        result.Append(", HasLowerBound = ");
        result.Append(this.BooleanText(value.HasLowerBound));
        result.Append(", LowerBound = ");
        result.Append(this.ConstantEmitter.CreateBigInteger(value.LowerBound));
        result.Append(", HasUpperBound = ");
        result.Append(this.BooleanText(value.HasUpperBound));
        result.Append(", UpperBound = ");
        result.Append(this.ConstantEmitter.CreateBigInteger(value.UpperBound));
        if (value.LowerValue != null)
        {
            result.Append(", LowerValue = ");
            result.Append(this.CreateValueExpression(value.LowerValue));
        }

        if (value.UpperValue != null)
        {
            result.Append(", UpperValue = ");
            result.Append(this.CreateValueExpression(value.UpperValue));
        }

        if (value.Value != null)
        {
            result.Append(", Value = ");
            result.Append(this.CreateValueExpression(value.Value));
        }

        result.Append(", LowerEndpointUnbounded = ");
        result.Append(this.BooleanText(value.LowerEndpointUnbounded));
        result.Append(", UpperEndpointUnbounded = ");
        result.Append(this.BooleanText(value.UpperEndpointUnbounded));
        result.Append(", LowerEndpointIncluded = ");
        result.Append(this.BooleanText(value.LowerEndpointIncluded));
        result.Append(", UpperEndpointIncluded = ");
        result.Append(this.BooleanText(value.UpperEndpointIncluded));
        result.Append(", Name = ");
        result.Append(this.Quote(value.Name));
        result.Append(", Text = ");
        result.Append(this.Quote(value.Text));
        result.Append(", Children = ");
        result.Append(this.CreateConstraintElementArray(value.Children));
        result.Append(" }");
        return result.ToString();
    }

    internal string CreateValueExpression(AsnValue value)
    {
        LoweredValueIr lowered =
            new ValidationReflectionIrAdapter().LowerValue(value);
        return this.ConstantEmitter.CreateSchemaValueInitializer(lowered);
    }

    internal string CreateSpanExpression(AsnSourceSpan? span)
    {
        if (span == null)
        {
            return "null";
        }

        return "new AsnSourceSpan { Start = "
            + this.CreatePositionExpression(span.Start)
            + ", End = " + this.CreatePositionExpression(span.End) + " }";
    }

    internal string CreatePositionExpression(AsnSourcePosition position)
    {
        return "new AsnSourcePosition { SourceName = "
            + this.Quote(position.SourceName) + ", Offset = "
            + position.Offset.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ", Line = " + position.Line.ToString(
                System.Globalization.CultureInfo.InvariantCulture)
            + ", Column = " + position.Column.ToString(
                System.Globalization.CultureInfo.InvariantCulture) + " }";
    }

    internal string CreateDerivationArray(AsnDerivationDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append("new AsnDerivationDescriptor { StableId = ");
            result.Append(this.Quote(values[index].StableId));
            result.Append(", Kind = AsnDerivationKind.");
            result.Append(values[index].Kind.ToString());
            result.Append(", Name = ");
            result.Append(this.Quote(values[index].Name));
            result.Append(", Span = ");
            result.Append(this.CreateSpanExpression(values[index].Span));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateTagArray(AsnTagDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnTagDescriptor value = values[index];
            result.Append("new AsnTagDescriptor { StableId = ");
            result.Append(this.Quote(value.StableId));
            result.Append(", Class = AsnTagClass.");
            result.Append(value.Class.ToString());
            result.Append(", Mode = AsnTaggingMode.");
            result.Append(value.Mode.ToString());
            result.Append(", Number = ");
            result.Append(this.ConstantEmitter.CreateBigInteger(value.Number));
            result.Append(", Universal = ");
            result.Append(this.BooleanText(value.Universal));
            result.Append(", AutomaticallyGenerated = ");
            result.Append(this.BooleanText(value.AutomaticallyGenerated));
            result.Append(", SourceSpan = ");
            result.Append(this.CreateSpanExpression(value.SourceSpan));
            result.Append(", Derivations = ");
            result.Append(this.CreateDerivationArray(value.Derivations));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateIntervalArray(AsnIntervalDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnIntervalDescriptor value = values[index];
            result.Append("new AsnIntervalDescriptor { StableId = ");
            result.Append(this.Quote(value.StableId));
            result.Append(", HasLowerBound = ");
            result.Append(this.BooleanText(value.HasLowerBound));
            result.Append(", LowerBound = ");
            result.Append(this.ConstantEmitter.CreateBigInteger(value.LowerBound));
            result.Append(", HasUpperBound = ");
            result.Append(this.BooleanText(value.HasUpperBound));
            result.Append(", UpperBound = ");
            result.Append(this.ConstantEmitter.CreateBigInteger(value.UpperBound));
            result.Append(", SourceSpan = ");
            result.Append(this.CreateSpanExpression(value.SourceSpan));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateEncodingArray(AsnEncodingDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnEncodingDescriptor value = values[index];
            result.Append("new AsnEncodingDescriptor { StableId = ");
            result.Append(this.Quote(value.StableId));
            result.Append(", EncodingRules = AsnEncodingRules.");
            result.Append(value.EncodingRules.ToString());
            result.Append(", EncodingReference = ");
            result.Append(this.Quote(value.EncodingReference));
            result.Append(", Instructions = ");
            result.Append(this.CreateInstructionArray(value.Instructions));
            result.Append(", SourceSpan = ");
            result.Append(this.CreateSpanExpression(value.SourceSpan));
            if (value.Plan != null)
            {
                result.Append(", Plan = ");
                result.Append(this.CreatePlanExpression(value.Plan));
            }

            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateInstructionArray(AsnEncodingInstruction[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnEncodingInstruction value = values[index];
            result.Append("new AsnEncodingInstruction { Name = ");
            result.Append(this.Quote(value.Name));
            result.Append(", Kind = AsnInstructionKind.");
            result.Append(value.Kind.ToString());
            result.Append(", Negated = ");
            result.Append(this.BooleanText(value.Negated));
            result.Append(", Arguments = ");
            result.Append(this.CreateStringArray(value.Arguments));
            result.Append(", Targets = ");
            result.Append(this.CreateStringArray(value.Targets));
            result.Append(", Span = ");
            result.Append(this.CreateSpanExpression(value.Span));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreatePlanExpression(AsnEncodingPlan value)
    {
        string result = "new AsnEncodingPlan { StableId = "
            + this.Quote(value.StableId)
            + ", EncodingRules = AsnEncodingRules."
            + value.EncodingRules.ToString() + ", Steps = "
            + this.CreatePlanStepArray(value.Steps) + ", Instructions = "
            + this.CreateInstructionArray(value.Instructions);
        if (value.EcnClasses.Length > 0)
        {
            result = result + ", EcnClasses = "
                + this.CreateEcnClassArray(value.EcnClasses);
        }
        if (value.EcnObjects.Length > 0)
        {
            result = result + ", EcnObjects = "
                + this.CreateEcnObjectArray(value.EcnObjects);
        }
        if (value.PerInstructionRegistrations.Length > 0)
        {
            result = result + ", PerInstructionRegistrations = "
                + this.CreatePerInstructionRegistrationArray(
                    value.PerInstructionRegistrations);
        }
        return result + " }";
    }

    internal string CreatePerInstructionRegistrationArray(
        AsnPerInstructionRegistration[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnPerInstructionRegistration value = values[index];
            result.Append("new AsnPerInstructionRegistration { Keyword = ");
            result.Append(this.Quote(value.Keyword));
            result.Append(", HandlerId = ");
            result.Append(this.Quote(value.HandlerId));
            result.Append(", SupportsAligned = ");
            result.Append(this.BooleanText(value.SupportsAligned));
            result.Append(", SupportsUnaligned = ");
            result.Append(this.BooleanText(value.SupportsUnaligned));
            result.Append(", Reversible = ");
            result.Append(this.BooleanText(value.Reversible));
            result.Append(", ConflictingKeywords = ");
            result.Append(this.CreateStringArray(value.ConflictingKeywords));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateEcnClassArray(AsnEcnClass[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }
            result.Append("new AsnEcnClass { Name = ");
            result.Append(this.Quote(values[index].Name));
            result.Append(", Category = ");
            result.Append(this.Quote(values[index].Category));
            result.Append(", Steps = ");
            result.Append(this.CreatePlanStepArray(values[index].Steps));
            result.Append(" }");
            index = index + 1;
        }
        result.Append(']');
        return result.ToString();
    }

    internal string CreateEcnObjectArray(AsnEcnObject[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }
        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }
            result.Append("new AsnEcnObject { Name = ");
            result.Append(this.Quote(values[index].Name));
            result.Append(", ClassName = ");
            result.Append(this.Quote(values[index].ClassName));
            result.Append(", Instructions = ");
            result.Append(this.CreateInstructionArray(
                values[index].Instructions));
            result.Append(" }");
            index = index + 1;
        }
        result.Append(']');
        return result.ToString();
    }

    internal string CreatePlanStepArray(AsnEncodingPlanStep[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnEncodingPlanStep value = values[index];
            result.Append("new AsnEncodingPlanStep { Kind = AsnPlanStepKind.");
            result.Append(value.Kind.ToString());
            result.Append(", Path = ");
            result.Append(this.Quote(value.Path));
            result.Append(", Children = ");
            result.Append(this.CreatePlanStepArray(value.Children));
            if (value.Instruction != null)
            {
                result.Append(", Instruction = ");
                result.Append(this.CreateInstructionExpression(
                    value.Instruction));
            }

            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateInstructionExpression(AsnEncodingInstruction value)
    {
        return "new AsnEncodingInstruction { Name = "
            + this.Quote(value.Name) + ", Kind = AsnInstructionKind."
            + value.Kind.ToString() + ", Negated = "
            + this.BooleanText(value.Negated) + ", Arguments = "
            + this.CreateStringArray(value.Arguments) + ", Targets = "
            + this.CreateStringArray(value.Targets) + ", Span = "
            + this.CreateSpanExpression(value.Span) + " }";
    }

    internal string CreateImportArray(AsnImport[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnImport value = values[index];
            result.Append("new AsnImport { StableId = ");
            result.Append(this.Quote(value.StableId));
            result.Append(", ModuleName = ");
            result.Append(this.Quote(value.ModuleName));
            result.Append(", GeneratedModuleName = ");
            result.Append(this.Quote(this.MapLooseTypeName(value.ModuleName)));
            result.Append(", Symbols = ");
            result.Append(this.CreateStringArray(value.Symbols));
            result.Append(", Span = ");
            result.Append(this.CreateSpanExpression(value.Span));
            result.Append(", Derivations = ");
            result.Append(this.CreateDerivationArray(value.Derivations));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateParameterArray(AsnParameterDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnParameterDescriptor value = values[index];
            result.Append("new AsnParameterDescriptor { StableId = ");
            result.Append(this.Quote(value.StableId));
            result.Append(", Name = ");
            result.Append(this.Quote(value.Name));
            result.Append(", GeneratedName = ");
            result.Append(this.Quote(this.MapLooseMemberName(value.Name)));
            result.Append(", Kind = AsnParameterKind.");
            result.Append(value.Kind.ToString());
            result.Append(", GovernorTypeKind = AsnTypeKind.");
            result.Append(value.GovernorTypeKind.ToString());
            result.Append(", GovernorObjectClassName = ");
            result.Append(this.Quote(value.GovernorObjectClassName));
            result.Append(", SourceSpan = ");
            result.Append(this.CreateSpanExpression(value.SourceSpan));
            result.Append(", Derivations = ");
            result.Append(this.CreateDerivationArray(value.Derivations));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateTableEntryArray(AsnObjectTableEntryDescriptor[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            AsnObjectTableEntryDescriptor value = values[index];
            result.Append("new AsnObjectTableEntryDescriptor { StableId = ");
            result.Append(this.Quote(value.StableId));
            if (value.Key != null)
            {
                result.Append(", Key = value");
                result.Append(this.FindValueIndex(value.Key).ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            }

            if (value.Type != null)
            {
                result.Append(", Type = type");
                result.Append(this.FindTypeIndex(value.Type).ToString(
                    System.Globalization.CultureInfo.InvariantCulture));
            }

            result.Append(", SourceSpan = ");
            result.Append(this.CreateSpanExpression(value.SourceSpan));
            result.Append(" }");
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateConstraintElementArray(AsnConstraintElement[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append(this.CreateConstraintElementExpression(values[index]));
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateFieldReferenceArray(AsnFieldDescriptor[] values)
    {
        return this.CreateReferenceArray(
            "field",
            this.FindFieldIndices(values));
    }

    internal string CreateTypeReferenceArray(AsnTypeDescriptor[] values)
    {
        return this.CreateReferenceArray(
            "type",
            this.FindTypeIndices(values));
    }

    internal string CreateValueReferenceArray(AsnValueDescriptor[] values)
    {
        return this.CreateReferenceArray(
            "value",
            this.FindValueIndices(values));
    }

    internal string CreateConstraintReferenceArray(
        AsnConstraintDescriptor[] values)
    {
        return this.CreateReferenceArray(
            "constraint",
            this.FindConstraintIndices(values));
    }

    internal string CreateTableReferenceArray(AsnObjectTableDescriptor[] values)
    {
        return this.CreateReferenceArray(
            "table",
            this.FindTableIndices(values));
    }

    internal string CreateAssignmentReferenceArray(int count)
    {
        int[] indices = new int[count];
        int index = 0;
        while (index < count)
        {
            indices[index] = index;
            index = index + 1;
        }

        return this.CreateReferenceArray("assignment", indices);
    }

    internal string CreateReferenceArray(string prefix, int[] indices)
    {
        if (indices.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < indices.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append(prefix);
            result.Append(indices[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string CreateStringArray(string[] values)
    {
        if (values.Length == 0)
        {
            return "[]";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        result.Append('[');
        int index = 0;
        while (index < values.Length)
        {
            if (index > 0)
            {
                result.Append(", ");
            }

            result.Append(this.Quote(values[index]));
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal int[] FindFieldIndices(AsnFieldDescriptor[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = this.FindFieldIndex(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal int[] FindTypeIndices(AsnTypeDescriptor[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = this.FindTypeIndex(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal int[] FindValueIndices(AsnValueDescriptor[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = this.FindValueIndex(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal int[] FindConstraintIndices(AsnConstraintDescriptor[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = this.FindConstraintIndex(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal int[] FindTableIndices(AsnObjectTableDescriptor[] values)
    {
        int[] result = new int[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            result[index] = this.FindTableIndex(values[index]);
            index = index + 1;
        }

        return result;
    }

    internal int FindTypeIndex(AsnTypeDescriptor value)
    {
        return this.Types.IndexOf(value);
    }

    internal int FindFieldIndex(AsnFieldDescriptor value)
    {
        return this.Fields.IndexOf(value);
    }

    internal int FindValueIndex(AsnValueDescriptor value)
    {
        return this.Values.IndexOf(value);
    }

    internal int FindConstraintIndex(AsnConstraintDescriptor value)
    {
        return this.Constraints.IndexOf(value);
    }

    internal int FindTableIndex(AsnObjectTableDescriptor value)
    {
        return this.Tables.IndexOf(value);
    }

    internal string MapLooseTypeName(string sourceName)
    {
        string normalized = this.NameMapper.NormalizeSourceName(sourceName);
        string result = this.NameMapper.CreateIdentifier(
            normalized,
            "UnnamedType");
        return this.NameMapper.AvoidKeyword(result, normalized);
    }

    internal string MapLooseMemberName(string sourceName)
    {
        string normalized = this.NameMapper.NormalizeSourceName(sourceName);
        string result = this.NameMapper.CreateIdentifier(
            normalized,
            "UnnamedMember");
        return this.NameMapper.AvoidKeyword(result, normalized);
    }

    internal string Quote(string value)
    {
        return "\"" + this.ModelEmitter.EscapeStringLiteral(value) + "\"";
    }

    internal string BooleanText(bool value)
    {
        return value ? "true" : "false";
    }
}
