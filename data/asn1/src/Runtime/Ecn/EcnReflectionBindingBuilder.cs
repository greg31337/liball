namespace asn1;

internal class EcnReflectionBindingBuilder
{
    internal EcnPlanExecutionBinding[] BuildBindings(
        EcnExecutablePlan plan,
        AsnTypeDescriptor type)
    {
        if (plan == null)
        {
            throw new System.ArgumentNullException("plan");
        }
        if (type == null)
        {
            throw new System.ArgumentNullException("type");
        }
        System.Collections.Generic.List<EcnPlanExecutionBinding> result =
            new System.Collections.Generic.List<EcnPlanExecutionBinding>();
        int stepIndex = 0;
        while (stepIndex < plan.Steps.Length)
        {
            EcnExecutablePlanStep step = plan.Steps[stepIndex];
            if (step.Kind == EcnPlanStepKind.Field
                && step.Trace.SemanticKind == "field")
            {
                AsnFieldDescriptor? field = this.FindField(type, step.Name);
                if (field != null && field.Type != null)
                {
                    result.Add(new EcnPlanExecutionBinding
                    {
                        Path = step.Path,
                        Category = this.MapTypeCategory(field.Type.Kind),
                        Present = true
                    });
                }
            }
            stepIndex = stepIndex + 1;
        }
        if (result.Count == 0)
        {
            result.Add(new EcnPlanExecutionBinding
            {
                Path = "$value",
                Category = this.MapTypeCategory(type.Kind),
                BitCount = this.FixedPlanBitCount(plan)
            });
        }
        return result.ToArray();
    }

    internal AsnFieldDescriptor? FindField(
        AsnTypeDescriptor type,
        string name)
    {
        int index = 0;
        while (index < type.Fields.Length)
        {
            if (type.Fields[index].Name == name)
            {
                return type.Fields[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal EcnClassCategory MapTypeCategory(AsnTypeKind kind)
    {
        switch (kind)
        {
            case AsnTypeKind.Boolean:
                return EcnClassCategory.Boolean;
            case AsnTypeKind.Integer:
            case AsnTypeKind.Enumerated:
                return EcnClassCategory.Integer;
            case AsnTypeKind.Real:
                return EcnClassCategory.Real;
            case AsnTypeKind.BitString:
                return EcnClassCategory.BitString;
            case AsnTypeKind.OctetString:
                return EcnClassCategory.OctetString;
            case AsnTypeKind.CharacterString:
            case AsnTypeKind.BmpString:
            case AsnTypeKind.GeneralString:
            case AsnTypeKind.GraphicString:
            case AsnTypeKind.Ia5String:
            case AsnTypeKind.NumericString:
            case AsnTypeKind.ObjectDescriptor:
            case AsnTypeKind.PrintableString:
            case AsnTypeKind.TeletexString:
            case AsnTypeKind.UniversalString:
            case AsnTypeKind.Utf8String:
            case AsnTypeKind.VideotexString:
            case AsnTypeKind.VisibleString:
                return EcnClassCategory.CharacterString;
            case AsnTypeKind.ObjectIdentifier:
            case AsnTypeKind.RelativeObjectIdentifier:
                return EcnClassCategory.ObjectIdentifier;
            case AsnTypeKind.Null:
                return EcnClassCategory.Null;
            case AsnTypeKind.OpenType:
                return EcnClassCategory.OpenType;
            case AsnTypeKind.Choice:
                return EcnClassCategory.Alternatives;
            case AsnTypeKind.Sequence:
            case AsnTypeKind.Set:
                return EcnClassCategory.Concatenation;
            case AsnTypeKind.SequenceOf:
            case AsnTypeKind.SetOf:
                return EcnClassCategory.Repetition;
            case AsnTypeKind.Time:
            case AsnTypeKind.Date:
            case AsnTypeKind.TimeOfDay:
            case AsnTypeKind.DateTime:
            case AsnTypeKind.Duration:
            case AsnTypeKind.GeneralizedTime:
            case AsnTypeKind.UtcTime:
                return EcnClassCategory.Time;
            default:
                return EcnClassCategory.Unknown;
        }
    }

    internal int FixedPlanBitCount(EcnExecutablePlan plan)
    {
        if (plan.Bounds.Exact && plan.Bounds.HasMinimumBits
            && plan.Bounds.HasMaximumBits
            && plan.Bounds.MinimumBits == plan.Bounds.MaximumBits
            && plan.Bounds.MinimumBits >= 0
            && plan.Bounds.MinimumBits <= int.MaxValue)
        {
            return (int)plan.Bounds.MinimumBits;
        }
        return -1;
    }
}
