namespace asn1;

internal class DispatchKeyNormalizer
{
    internal NormalizedDispatchKey NormalizeKey(AsnValue? value)
    {
        if (value == null)
        {
            return new NormalizedDispatchKey();
        }

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        this.AppendValueIdentity(builder, value);
        return new NormalizedDispatchKey
        {
            Value = value,
            Identity = builder.ToString(),
            Success = true
        };
    }

    internal void AppendValueIdentity(
        System.Text.StringBuilder builder,
        AsnValue value)
    {
        builder.Append(((int)value.Kind).ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        if (value.HasIntegerValue)
        {
            builder.Append('i');
            builder.Append(value.IntegerValue.ToString(
                System.Globalization.CultureInfo.InvariantCulture));
            return;
        }

        if (value.RealValue != null)
        {
            this.AppendRealIdentity(builder, value.RealValue);
            return;
        }

        if (value.TimeValue != null)
        {
            this.AppendTimeIdentity(builder, value.TimeValue);
            return;
        }

        if (value.NamedValues.Length > 0)
        {
            this.AppendNamedValuesIdentity(builder, value.NamedValues);
            return;
        }

        this.AppendTextIdentity(builder, value.TextValue);
    }

    internal void AppendRealIdentity(
        System.Text.StringBuilder builder,
        AsnRealValue value)
    {
        builder.Append('r');
        builder.Append(((int)value.Kind).ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.DecimalCoefficient.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.DecimalExponent.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.Mantissa.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.Radix.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.Exponent.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        this.AppendTextIdentity(builder, value.DecimalText);
    }

    internal void AppendTimeIdentity(
        System.Text.StringBuilder builder,
        AsnTimeValue value)
    {
        builder.Append('t');
        builder.Append(((int)value.Kind).ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.HasUtcOffsetMinutes ? '1' : '0');
        builder.Append(':');
        builder.Append(value.UtcOffsetMinutes.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value.HasFractionalDigits ? '1' : '0');
        builder.Append(':');
        builder.Append(value.FractionalDigits.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        this.AppendTextIdentity(builder, value.Text);
    }

    internal void AppendNamedValuesIdentity(
        System.Text.StringBuilder builder,
        AsnNamedValue[] values)
    {
        builder.Append('n');
        builder.Append(values.Length.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        int index = 0;
        while (index < values.Length)
        {
            this.AppendTextIdentity(builder, values[index].Name);
            if (values[index].Value == null)
            {
                builder.Append("null");
            }
            else
            {
                this.AppendValueIdentity(builder, values[index].Value
                    ?? new AsnValue());
            }

            index = index + 1;
        }
    }

    internal void AppendTextIdentity(
        System.Text.StringBuilder builder,
        string value)
    {
        builder.Append(value.Length.ToString(
            System.Globalization.CultureInfo.InvariantCulture));
        builder.Append(':');
        builder.Append(value);
    }
}
