namespace asn1;

internal class AsnModuleTextPrinter
{
    internal System.Text.StringBuilder Output =
        new System.Text.StringBuilder();

    internal string PrintModules(AsnModule[] modules)
    {
        if (modules == null)
        {
            throw new System.ArgumentNullException("modules");
        }

        this.Output.Clear();
        int index = 0;
        while (index < modules.Length)
        {
            if (index > 0)
            {
                this.Output.AppendLine();
            }

            this.PrintModule(modules[index]);
            index = index + 1;
        }

        return this.Output.ToString();
    }

    internal void PrintModule(AsnModule module)
    {
        if (module == null)
        {
            throw new System.ArgumentNullException("module");
        }

        this.Output.Append(module.Name);
        this.Output.AppendLine(" DEFINITIONS AUTOMATIC TAGS ::= BEGIN");
        int index = 0;
        while (index < module.Assignments.Length)
        {
            this.PrintAssignment(module.Assignments[index]);
            index = index + 1;
        }
        this.Output.AppendLine("END");
    }

    internal void PrintAssignment(AsnAssignment assignment)
    {
        if (assignment == null)
        {
            throw new System.ArgumentNullException("assignment");
        }

        if (assignment.Kind != AsnAssignmentKind.Type
            || assignment.Type == null)
        {
            throw new System.NotSupportedException(
                "Normalized source printing currently supports type assignments only.");
        }

        this.Output.Append(assignment.Name);
        this.Output.Append(" ::= ");
        this.PrintType(assignment.Type);
        this.Output.AppendLine(";");
    }

    internal void PrintType(AsnType type)
    {
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }

        if (type.Kind == AsnTypeKind.Sequence
            || type.Kind == AsnTypeKind.Set)
        {
            this.Output.Append(type.Kind == AsnTypeKind.Sequence
                ? "SEQUENCE { "
                : "SET { ");
            this.PrintComponents(type.Components);
            this.Output.Append(" }");
            return;
        }

        if (type.Kind == AsnTypeKind.SequenceOf
            || type.Kind == AsnTypeKind.SetOf)
        {
            this.Output.Append(type.Kind == AsnTypeKind.SequenceOf
                ? "SEQUENCE OF "
                : "SET OF ");
            if (type.ElementType == null)
            {
                throw new System.InvalidOperationException(
                    "A collection type requires an element type.");
            }

            this.PrintType(type.ElementType);
            return;
        }

        this.Output.Append(this.GetTypeText(type));
    }

    internal void PrintComponents(AsnComponent[] components)
    {
        int index = 0;
        while (index < components.Length)
        {
            AsnComponent component = components[index];
            if (index > 0)
            {
                this.Output.Append(", ");
            }

            this.Output.Append(component.Name);
            this.Output.Append(' ');
            if (component.Type == null)
            {
                throw new System.InvalidOperationException(
                    "A structured component requires a type.");
            }

            this.PrintType(component.Type);
            if (component.Optional)
            {
                this.Output.Append(" OPTIONAL");
            }

            index = index + 1;
        }
    }

    internal string GetTypeText(AsnType type)
    {
        switch (type.Kind)
        {
            case AsnTypeKind.Boolean:
                return "BOOLEAN";
            case AsnTypeKind.Integer:
                return "INTEGER";
            case AsnTypeKind.Null:
                return "NULL";
            case AsnTypeKind.BitString:
                return "BIT STRING";
            case AsnTypeKind.OctetString:
                return "OCTET STRING";
            case AsnTypeKind.ObjectIdentifier:
                return "OBJECT IDENTIFIER";
            case AsnTypeKind.RelativeObjectIdentifier:
                return "RELATIVE-OID";
            case AsnTypeKind.Utf8String:
                return "UTF8String";
            case AsnTypeKind.NumericString:
                return "NumericString";
            case AsnTypeKind.PrintableString:
                return "PrintableString";
            case AsnTypeKind.Ia5String:
                return "IA5String";
            case AsnTypeKind.Defined:
                if (type.Name.Length == 0)
                {
                    throw new System.InvalidOperationException(
                        "A defined type requires a reference name.");
                }
                return type.Name;
            default:
                throw new System.NotSupportedException(
                    "Normalized source printing does not support type kind "
                        + type.Kind.ToString() + ".");
        }
    }
}
