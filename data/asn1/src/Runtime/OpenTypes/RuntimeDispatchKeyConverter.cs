namespace asn1;

internal class RuntimeDispatchKeyConverter
{
    internal AsnValue ConvertKey(AsnRuntimeValue value)
    {
        if (value.Value != null)
        {
            return value.Value;
        }

        AsnValue result = new AsnValue
        {
            Kind = (AsnValueKind)(int)value.Kind,
            RealValue = value.RealValue,
            TimeValue = value.TimeValue,
            HasIntegerValue = value.HasIntegerValue,
            IntegerValue = value.IntegerValue,
            TextValue = value.HasBooleanValue
                ? value.BooleanValue ? "TRUE" : "FALSE"
                : value.TextValue == null ? "" : value.TextValue
        };
        if (value.Fields != null && value.Fields.Length > 0)
        {
            result.NamedValues = this.ConvertFields(value.Fields);
        }

        return result;
    }

    internal AsnNamedValue[] ConvertFields(AsnRuntimeFieldValue[] fields)
    {
        System.Collections.Generic.List<AsnNamedValue> values =
            new System.Collections.Generic.List<AsnNamedValue>();
        int index = 0;
        while (index < fields.Length)
        {
            if (fields[index].Present && fields[index].Value != null)
            {
                values.Add(new AsnNamedValue
                {
                    Name = fields[index].Name,
                    Value = this.ConvertKey(fields[index].Value ?? new AsnRuntimeValue())
                });
            }

            index = index + 1;
        }

        return values.ToArray();
    }
}
