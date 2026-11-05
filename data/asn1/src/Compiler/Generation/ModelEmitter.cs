namespace asn1;

internal class ModelEmitter
{
    internal CSharpNameMapper NameMapper = new CSharpNameMapper();
    internal CSharpGenerationPlan? GenerationPlan;
    internal CSharpModulePlan? CurrentModulePlan;

    internal CSharpModelEmission EmitModel(
        LoweredAssignmentIr assignment,
        string namespaceName,
        CSharpNameScope typeScope)
    {
        if (assignment == null)
        {
            throw new System.ArgumentNullException("assignment");
        }

        if (namespaceName == null)
        {
            throw new System.ArgumentNullException("namespaceName");
        }

        if (typeScope == null)
        {
            throw new System.ArgumentNullException("typeScope");
        }

        if (assignment.Kind != AsnAssignmentKind.Type
            || assignment.Type == null)
        {
            throw new System.InvalidOperationException(
                "Only a resolved type assignment can produce a data model.");
        }

        if (namespaceName.Length == 0)
        {
            throw new System.ArgumentException(
                "A generated model requires a namespace.",
                "namespaceName");
        }

        CSharpTypePlan? plannedType = this.GenerationPlan == null
            ? null
            : this.GenerationPlan.FindTypePlan(assignment.Type);
        CSharpNameMapping typeName = plannedType == null
            ? this.NameMapper.MapTypeName(
                assignment.Name,
                assignment.StableId,
                typeScope)
            : plannedType.TypeName;
        this.CurrentModulePlan = plannedType == null
            ? null
            : plannedType.Module;
        CSharpNameMapping? choiceName = null;
        if (assignment.Type.Kind == AsnTypeKind.Choice)
        {
            choiceName = plannedType == null
                ? this.NameMapper.MapTypeName(
                    assignment.Name + "-selection",
                    assignment.StableId + "/choice-selection",
                    typeScope)
                : plannedType.ChoiceName;
        }

        CSharpNameScope memberScope = new CSharpNameScope(false);
        this.ReserveSupportMemberNames(assignment.Type, memberScope);
        CSharpNameMapping[] fieldNames =
            new CSharpNameMapping[assignment.Type.Fields.Length];
        System.Collections.Generic.List<CSharpNameMapping> presenceNames =
            new System.Collections.Generic.List<CSharpNameMapping>();
        int fieldIndex = 0;
        while (fieldIndex < assignment.Type.Fields.Length)
        {
            LoweredFieldIr field = assignment.Type.Fields[fieldIndex];
            CSharpNameMapping fieldName = this.NameMapper.MapMemberName(
                field.Name,
                field.StableId,
                memberScope);
            fieldNames[fieldIndex] = fieldName;
            if (assignment.Type.Kind != AsnTypeKind.Choice
                && this.RequiresPresenceField(field))
            {
                CSharpNameReservation reservation = memberScope.ReserveName(
                    "Has" + fieldName.GeneratedName);
                presenceNames.Add(this.NameMapper.CreateMapping(
                    field.Name,
                    this.NameMapper.NormalizeSourceName(field.Name),
                    field.StableId + "/presence",
                    reservation.Name,
                    reservation.CollisionIndex));
            }

            fieldIndex = fieldIndex + 1;
        }

        CSharpTextWriter writer = new CSharpTextWriter();
        writer.WriteLine("using asn1;");
        writer.WriteLine("");
        writer.WriteLine("namespace " + namespaceName + ";");
        writer.WriteLine("");
        if (choiceName != null)
        {
            this.EmitChoiceEnum(
                writer,
                assignment.Type,
                choiceName,
                fieldNames);
            writer.WriteLine("");
        }

        this.EmitDataClass(
            writer,
            assignment,
            typeName,
            choiceName,
            fieldNames,
            presenceNames.ToArray());
        return new CSharpModelEmission
        {
            Content = writer.GetText(),
            TypeName = typeName,
            ChoiceName = choiceName,
            FieldNames = fieldNames,
            PresenceNames = presenceNames.ToArray()
        };
    }

    internal void ReserveSupportMemberNames(
        LoweredTypeIr type,
        CSharpNameScope scope)
    {
        if (type.Kind == AsnTypeKind.Choice)
        {
            scope.ReserveName("Selection");
            scope.ReserveName("None");
        }
        else if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            scope.ReserveName("Items");
        }
        else if (!this.IsStructuredKind(type.Kind))
        {
            scope.ReserveName("Value");
        }
        else if (type.Fields.Length == 0
            && !this.IsExtensibleType(type))
        {
            scope.ReserveName("Present");
        }

        if (this.IsExtensibleType(type))
        {
            scope.ReserveName("UnknownExtensions");
            scope.ReserveName("UnknownFields");
        }
    }

    internal void EmitChoiceEnum(
        CSharpTextWriter writer,
        LoweredTypeIr type,
        CSharpNameMapping choiceName,
        CSharpNameMapping[] fieldNames)
    {
        this.WriteSummary(
            writer,
            "Identifies the selected alternative of the ASN.1 CHOICE.");
        writer.WriteLine("public enum " + choiceName.GeneratedName);
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        this.WriteSummary(writer, "Indicates that no alternative is selected.");
        writer.WriteLine("None = 0,");
        int index = 0;
        while (index < type.Fields.Length)
        {
            writer.WriteLine("");
            this.WriteSummary(
                writer,
                "Selects the <c>" + this.EscapeXml(type.Fields[index].Name)
                    + "</c> alternative.");
            writer.WriteLine(fieldNames[index].GeneratedName + " = "
                + (index + 1).ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + (index + 1 == type.Fields.Length ? "" : ","));
            index = index + 1;
        }

        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitDataClass(
        CSharpTextWriter writer,
        LoweredAssignmentIr assignment,
        CSharpNameMapping typeName,
        CSharpNameMapping? choiceName,
        CSharpNameMapping[] fieldNames,
        CSharpNameMapping[] presenceNames)
    {
        LoweredTypeIr type = assignment.Type
            ?? throw new System.InvalidOperationException();
        this.WriteSummary(
            writer,
            "Contains the generated data fields for ASN.1 <c>"
                + this.EscapeXml(assignment.Name) + "</c>.");
        writer.WriteLine("public class " + typeName.GeneratedName);
        writer.WriteLine("{");
        writer.IncreaseIndentation();
        if (type.Kind == AsnTypeKind.Choice)
        {
            this.EmitChoiceFields(
                writer,
                type,
                typeName,
                choiceName,
                fieldNames);
        }
        else if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            this.EmitCollectionField(writer, type, typeName);
        }
        else if (this.IsStructuredKind(type.Kind))
        {
            this.EmitStructuredFields(
                writer,
                type,
                typeName,
                fieldNames,
                presenceNames);
        }
        else
        {
            this.EmitScalarField(writer, type, typeName);
        }

        if (this.IsExtensibleType(type))
        {
            if (this.HasEmittedModelField(type))
            {
                writer.WriteLine("");
            }

            this.EmitUnknownDataFields(writer);
        }

        writer.DecreaseIndentation();
        writer.WriteLine("}");
    }

    internal void EmitChoiceFields(
        CSharpTextWriter writer,
        LoweredTypeIr type,
        CSharpNameMapping ownerName,
        CSharpNameMapping? choiceName,
        CSharpNameMapping[] fieldNames)
    {
        if (choiceName == null)
        {
            throw new System.InvalidOperationException(
                "A CHOICE model requires a discriminant name.");
        }

        this.WriteSummary(writer, "Identifies the selected CHOICE alternative.");
        writer.WriteLine("public " + choiceName.GeneratedName
            + " Selection = " + choiceName.GeneratedName + ".None;");
        int index = 0;
        while (index < type.Fields.Length)
        {
            LoweredFieldIr field = type.Fields[index];
            if (field.Type == null)
            {
                throw new System.InvalidOperationException(
                    "A generated CHOICE alternative requires a resolved type.");
            }

            CSharpTypeReference reference = this.GetTypeReference(
                field.Type,
                type,
                ownerName.GeneratedName);
            writer.WriteLine("");
            this.WriteSummary(
                writer,
                "Contains the <c>" + this.EscapeXml(field.Name)
                    + "</c> alternative value.");
            writer.WriteLine("public " + reference.TypeName + " "
                + fieldNames[index].GeneratedName + " = "
                + reference.Initializer + ";");
            index = index + 1;
        }
    }

    internal void EmitCollectionField(
        CSharpTextWriter writer,
        LoweredTypeIr type,
        CSharpNameMapping ownerName)
    {
        if (type.ElementType == null)
        {
            throw new System.InvalidOperationException(
                "A generated collection requires an element type.");
        }

        CSharpTypeReference element = this.GetTypeReference(
            type.ElementType,
            type,
            ownerName.GeneratedName);
        this.WriteSummary(writer, "Contains collection items in abstract-value order.");
        writer.WriteLine("public " + element.TypeName + "[] Items = [];");
    }

    internal void EmitStructuredFields(
        CSharpTextWriter writer,
        LoweredTypeIr type,
        CSharpNameMapping ownerName,
        CSharpNameMapping[] fieldNames,
        CSharpNameMapping[] presenceNames)
    {
        int presenceIndex = 0;
        int fieldIndex = 0;
        while (fieldIndex < type.Fields.Length)
        {
            LoweredFieldIr field = type.Fields[fieldIndex];
            if (field.Type == null)
            {
                throw new System.InvalidOperationException(
                    "A generated data field requires a resolved type.");
            }

            if (fieldIndex > 0)
            {
                writer.WriteLine("");
            }

            if (this.RequiresPresenceField(field))
            {
                this.WriteSummary(
                    writer,
                    "Indicates whether <c>" + this.EscapeXml(field.Name)
                        + "</c> is present.");
                writer.WriteLine("public bool "
                    + presenceNames[presenceIndex].GeneratedName + ";");
                presenceIndex = presenceIndex + 1;
                writer.WriteLine("");
            }

            CSharpTypeReference reference = this.GetTypeReference(
                field.Type,
                type,
                ownerName.GeneratedName);
            string initializer = field.HasDefaultValue
                ? this.GetDefaultInitializer(
                    field.Type,
                    field.DefaultValue,
                    reference.Initializer)
                : reference.Initializer;
            this.WriteSummary(
                writer,
                "Contains the ASN.1 <c>" + this.EscapeXml(field.Name)
                    + "</c> field.");
            writer.WriteLine("public " + reference.TypeName + " "
                + fieldNames[fieldIndex].GeneratedName + " = "
                + initializer + ";");
            fieldIndex = fieldIndex + 1;
        }

        if (type.Fields.Length == 0 && !this.IsExtensibleType(type))
        {
            this.WriteSummary(
                writer,
                "Indicates whether the single empty structured value is present.");
            writer.WriteLine("public bool Present;");
        }
    }

    internal void EmitScalarField(
        CSharpTextWriter writer,
        LoweredTypeIr type,
        CSharpNameMapping ownerName)
    {
        CSharpTypeReference reference = this.GetTypeReference(
            type,
            type,
            ownerName.GeneratedName);
        this.WriteSummary(writer, "Contains the scalar ASN.1 value.");
        writer.WriteLine("public " + reference.TypeName + " Value = "
            + reference.Initializer + ";");
    }

    internal void EmitUnknownDataFields(CSharpTextWriter writer)
    {
        this.WriteSummary(writer, "Contains preserved unknown extension additions.");
        writer.WriteLine(
            "public AsnUnknownExtension[] UnknownExtensions = [];");
        writer.WriteLine("");
        this.WriteSummary(writer, "Contains preserved unknown named fields.");
        writer.WriteLine("public AsnUnknownField[] UnknownFields = [];");
    }

    internal CSharpTypeReference GetTypeReference(
        LoweredTypeIr type,
        LoweredTypeIr ownerType,
        string ownerName)
    {
        if (this.GenerationPlan != null
            && (this.IsStructuredKind(type.Kind)
                || type.Kind == AsnTypeKind.Choice))
        {
            CSharpTypePlan? planned = this.GenerationPlan.FindTypePlan(type);
            if (planned != null)
            {
                string typeName = planned.TypeName.GeneratedName;
                if (this.CurrentModulePlan != null
                    && planned.Module != this.CurrentModulePlan)
                {
                    typeName = "global::"
                        + planned.Module.NamespaceName.GeneratedName
                        + "." + typeName;
                }

                if (this.GenerationPlan.IsRecursiveReference(
                    ownerType,
                    type))
                {
                    return new CSharpTypeReference
                    {
                        TypeName = typeName + "?",
                        Initializer = "null"
                    };
                }

                return new CSharpTypeReference
                {
                    TypeName = typeName,
                    Initializer = "new " + typeName + "()"
                };
            }
        }

        if (type == ownerType
            && (this.IsStructuredKind(type.Kind)
                || type.Kind == AsnTypeKind.Choice))
        {
            return new CSharpTypeReference
            {
                TypeName = ownerName + "?",
                Initializer = "null"
            };
        }

        if (type.Kind == AsnTypeKind.Tagged
            || type.Kind == AsnTypeKind.Prefixed
            || type.Kind == AsnTypeKind.Selection
            || type.Kind == AsnTypeKind.Defined)
        {
            if (type.ElementType == null)
            {
                return new CSharpTypeReference();
            }

            return this.GetTypeReference(type.ElementType, ownerType, ownerName);
        }

        if (type.Kind == AsnTypeKind.Boolean || type.Kind == AsnTypeKind.Null)
        {
            return new CSharpTypeReference
            {
                TypeName = "bool",
                Initializer = "false"
            };
        }

        if (type.Kind == AsnTypeKind.Integer
            || type.Kind == AsnTypeKind.Enumerated)
        {
            return new CSharpTypeReference
            {
                TypeName = "System.Numerics.BigInteger",
                Initializer = "new System.Numerics.BigInteger(0)"
            };
        }

        if (type.Kind == AsnTypeKind.Real)
        {
            return new CSharpTypeReference
            {
                TypeName = "AsnRealValue",
                Initializer = "new AsnRealValue()"
            };
        }

        if (type.Kind == AsnTypeKind.BitString)
        {
            return new CSharpTypeReference
            {
                TypeName = "AsnBitStringValue",
                Initializer = "new AsnBitStringValue()"
            };
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return new CSharpTypeReference
            {
                TypeName = "byte[]",
                Initializer = "[]"
            };
        }

        if (this.IsCharacterStringKind(type.Kind)
            || type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return new CSharpTypeReference
            {
                TypeName = "string",
                Initializer = "\"\""
            };
        }

        if (type.Kind == AsnTypeKind.ObjectIdentifier
            || type.Kind == AsnTypeKind.RelativeObjectIdentifier)
        {
            return new CSharpTypeReference
            {
                TypeName = "AsnObjectIdentifierValue",
                Initializer = "new AsnObjectIdentifierValue()"
            };
        }

        if (this.IsTimeKind(type.Kind))
        {
            return new CSharpTypeReference
            {
                TypeName = "AsnTimeValue",
                Initializer = "new AsnTimeValue()"
            };
        }

        if (type.Kind == AsnTypeKind.OpenType)
        {
            return new CSharpTypeReference
            {
                TypeName = "AsnOpenTypeValue",
                Initializer = "new AsnOpenTypeValue()"
            };
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            if (type.ElementType == null)
            {
                return new CSharpTypeReference();
            }

            CSharpTypeReference element = this.GetTypeReference(
                type.ElementType,
                ownerType,
                ownerName);
            return new CSharpTypeReference
            {
                TypeName = element.TypeName + "[]",
                Initializer = "[]"
            };
        }

        if (this.IsStructuredKind(type.Kind)
            || type.Kind == AsnTypeKind.Choice)
        {
            if (type.Name.Length == 0)
            {
                return new CSharpTypeReference();
            }

            string normalized = this.NameMapper.NormalizeSourceName(type.Name);
            string generated = this.NameMapper.CreateIdentifier(
                normalized,
                "UnnamedType");
            generated = this.NameMapper.AvoidKeyword(generated, normalized);
            return new CSharpTypeReference
            {
                TypeName = generated,
                Initializer = "new " + generated + "()"
            };
        }

        return new CSharpTypeReference();
    }

    internal string GetDefaultInitializer(
        LoweredTypeIr type,
        LoweredValueIr? value,
        string fallbackInitializer)
    {
        if (value == null)
        {
            return fallbackInitializer;
        }

        if ((type.Kind == AsnTypeKind.Integer
                || type.Kind == AsnTypeKind.Enumerated)
            && value.HasIntegerValue)
        {
            return "System.Numerics.BigInteger.Parse(\""
                + value.IntegerValue.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)
                + "\")";
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
        }

        if (this.IsCharacterStringKind(type.Kind)
            || type.Kind == AsnTypeKind.ObjectIdentifierIri
            || type.Kind == AsnTypeKind.RelativeObjectIdentifierIri)
        {
            return "\"" + this.EscapeStringLiteral(value.TextValue) + "\"";
        }

        if (type.Kind == AsnTypeKind.OctetString)
        {
            return this.CreateByteCollection(value.Octets);
        }

        return fallbackInitializer;
    }

    internal string CreateByteCollection(byte[] values)
    {
        if (values == null || values.Length == 0)
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

            result.Append(values[index].ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            index = index + 1;
        }

        result.Append(']');
        return result.ToString();
    }

    internal string EscapeStringLiteral(string value)
    {
        if (value == null)
        {
            return "";
        }

        System.Text.StringBuilder result = new System.Text.StringBuilder();
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            if (character == '\\')
            {
                result.Append("\\\\");
            }
            else if (character == '"')
            {
                result.Append("\\\"");
            }
            else if (character == '\n')
            {
                result.Append("\\n");
            }
            else if (character == '\r')
            {
                result.Append("\\r");
            }
            else if (character == '\t')
            {
                result.Append("\\t");
            }
            else if (character >= 32 && character <= 126)
            {
                result.Append(character);
            }
            else
            {
                result.Append("\\u");
                result.Append(((int)character).ToString(
                    "X4",
                    System.Globalization.CultureInfo.InvariantCulture));
            }

            index = index + 1;
        }

        return result.ToString();
    }

    internal string EscapeXml(string value)
    {
        if (value == null)
        {
            return "";
        }

        return value.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }

    internal void WriteSummary(CSharpTextWriter writer, string text)
    {
        writer.WriteLine("/// <summary>");
        writer.WriteLine("/// " + text);
        writer.WriteLine("/// </summary>");
    }

    internal bool RequiresPresenceField(LoweredFieldIr field)
    {
        return field.Optional || field.HasDefaultValue || field.Extension;
    }

    internal bool IsExtensibleType(LoweredTypeIr type)
    {
        return type.Extensible || type.Extensions.Extensible;
    }

    internal bool HasEmittedModelField(LoweredTypeIr type)
    {
        return type.Kind == AsnTypeKind.Choice
            || type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf
            || !this.IsStructuredKind(type.Kind)
            || type.Fields.Length > 0;
    }

    internal bool IsStructuredKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.Sequence
            || kind == AsnTypeKind.Set
            || kind == AsnTypeKind.External
            || kind == AsnTypeKind.EmbeddedPdv
            || kind == AsnTypeKind.InstanceOf;
    }

    internal bool IsCharacterStringKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.CharacterString
            || kind == AsnTypeKind.ObjectDescriptor
            || kind == AsnTypeKind.Utf8String
            || kind == AsnTypeKind.NumericString
            || kind == AsnTypeKind.PrintableString
            || kind == AsnTypeKind.TeletexString
            || kind == AsnTypeKind.VideotexString
            || kind == AsnTypeKind.Ia5String
            || kind == AsnTypeKind.GraphicString
            || kind == AsnTypeKind.VisibleString
            || kind == AsnTypeKind.GeneralString
            || kind == AsnTypeKind.UniversalString
            || kind == AsnTypeKind.BmpString;
    }

    internal bool IsTimeKind(AsnTypeKind kind)
    {
        return kind == AsnTypeKind.UtcTime
            || kind == AsnTypeKind.GeneralizedTime
            || kind == AsnTypeKind.Time
            || kind == AsnTypeKind.Date
            || kind == AsnTypeKind.TimeOfDay
            || kind == AsnTypeKind.DateTime
            || kind == AsnTypeKind.Duration;
    }
}
