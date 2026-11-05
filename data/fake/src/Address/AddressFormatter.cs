using System;
using System.Text;

namespace fake;

internal class AddressFormatParts
{
    internal string BuildingNumber = string.Empty;
    internal string StreetName = string.Empty;
    internal string StreetType = string.Empty;
    internal string UnitType = string.Empty;
    internal string Unit = string.Empty;
    internal string PoBox = string.Empty;
    internal string District = string.Empty;
    internal string City = string.Empty;
    internal string County = string.Empty;
    internal string Subdivision = string.Empty;
    internal string PostalCode = string.Empty;
    internal string Country = string.Empty;
}

internal static class AddressFormatter
{
    internal static string MultiLine(
        GenerationContext context,
        FakeRandom random)
    {
        return Format(context, random, null, false);
    }

    internal static string MultiLine(
        GenerationContext context,
        FakeRandom random,
        string style)
    {
        return Format(context, random, style, false);
    }

    internal static string SingleLine(
        GenerationContext context,
        FakeRandom random)
    {
        return Format(context, random, null, true);
    }

    internal static string SingleLine(
        GenerationContext context,
        FakeRandom random,
        string style)
    {
        return Format(context, random, style, true);
    }

    static string Format(
        GenerationContext context,
        FakeRandom random,
        string? styleId,
        bool singleLine)
    {
        AddressLocaleData data = AddressLocaleDataParser.Resolve(context);
        AddressFormatStyleData style = styleId == null
            ? ChooseStyle(random, data.FormatStyles)
            : FindStyle(data.FormatStyles, styleId);
        AddressTemplateData template = ChooseTemplate(random, data.Templates);
        AddressTemplateLineData[] segments = singleLine
            ? template.SingleLineParts
            : template.Lines;
        AddressFormatParts parts = Parts(
            context,
            random,
            data,
            style,
            segments);
        if (singleLine)
        {
            return RenderSegments(
                context,
                segments,
                template.SingleLineSeparator,
                parts);
        }
        return RenderSegments(context, segments, "\n", parts);
    }

    static AddressTemplateData ChooseTemplate(
        FakeRandom random,
        AddressTemplateData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = values[index].Weight;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    static AddressFormatStyleData ChooseStyle(
        FakeRandom random,
        AddressFormatStyleData[] values)
    {
        double[] weights = new double[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            weights[index] = values[index].Weight;
            index++;
        }
        return values[random.WeightedDiscrete(weights)];
    }

    static AddressFormatStyleData FindStyle(
        AddressFormatStyleData[] values,
        string style)
    {
        Guard.NotNullOrWhiteSpace(style, "style");
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].Id == style)
            {
                return values[index];
            }
            index++;
        }
        throw new ArgumentException(
            "style '" + style + "' is not declared by the resolved address locale data.",
            "style");
    }

    static AddressFormatParts Parts(
        GenerationContext context,
        FakeRandom random,
        AddressLocaleData data,
        AddressFormatStyleData style,
        AddressTemplateLineData[] segments)
    {
        AddressFormatParts result = new AddressFormatParts();
        result.BuildingNumber = AddressGenerator.Mask(
            context,
            random,
            data,
            data.BuildingNumberMasks);
        result.StreetName = data.Choose(random, data.StreetNames).Value;
        AddressTypeData streetType = data.ChooseType(random, data.StreetTypes);
        result.StreetType = style.StreetType == "name"
            ? streetType.Name
            : streetType.Abbreviation;
        bool requiredUnit = Requires(segments, "unitType") ||
            Requires(segments, "unit");
        if (requiredUnit || random.Bool())
        {
            AddressTypeData unitType = data.ChooseType(random, data.UnitTypes);
            result.UnitType = style.UnitType == "name"
                ? unitType.Name
                : unitType.Abbreviation;
            result.Unit = AddressGenerator.Mask(
                context,
                random,
                data,
                data.UnitMasks);
        }
        else
        {
            result.UnitType = "";
            result.Unit = "";
        }
        result.PoBox = AddressGenerator.Mask(
            context,
            random,
            data,
            data.PoBoxMasks);
        result.District = Requires(segments, "district") || random.Bool()
            ? data.Choose(random, data.Districts).Value
            : "";
        result.City = data.Choose(random, data.Cities).Value;
        result.County = data.Choose(random, data.Counties).Value;
        AddressCodedData subdivision = data.ChooseCoded(
            random,
            data.Subdivisions);
        result.Subdivision = style.Subdivision == "name"
            ? subdivision.Name
            : subdivision.Abbreviation;
        result.PostalCode = AddressGenerator.Mask(
            context,
            random,
            data,
            data.PostalMasks);
        AddressCountryData country = data.ChooseCountry(random, data.Countries);
        result.Country = style.Country == "name"
            ? country.Name
            : country.Abbreviation;
        return result;
    }

    static bool Requires(
        AddressTemplateLineData[] segments,
        string component)
    {
        string token = "{" + component + "}";
        int index = 0;
        while (index < segments.Length)
        {
            if (!segments[index].Optional &&
                segments[index].Value.IndexOf(
                    token,
                    StringComparison.Ordinal) >= 0)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    static string RenderSegments(
        GenerationContext context,
        AddressTemplateLineData[] segments,
        string separator,
        AddressFormatParts parts)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < segments.Length)
        {
            AddressTemplateLineData segment = segments[index];
            if (segment.Optional && !Complete(segment.Value, parts))
            {
                index++;
                continue;
            }
            string value = Render(
                context,
                segment.Value,
                parts);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (segment.Optional)
                {
                    index++;
                    continue;
                }
                throw FakeException.Create(
                    "Resolved address template produced an empty required segment.");
            }
            if (result.Length > 0)
            {
                Append(context, result, separator);
            }
            Append(context, result, value);
            index++;
        }
        if (result.Length == 0)
        {
            throw FakeException.Create(
                "Resolved address template produced no output segments.");
        }
        return result.ToString();
    }

    static bool Complete(string template, AddressFormatParts parts)
    {
        int index = 0;
        while (index < template.Length)
        {
            if (template[index] != '{')
            {
                index++;
                continue;
            }
            int close = template.IndexOf('}', index + 1);
            string component = template.Substring(index + 1, close - index - 1);
            if (Value(parts, component).Length == 0)
            {
                return false;
            }
            index = close + 1;
        }
        return true;
    }

    static string Render(
        GenerationContext context,
        string template,
        AddressFormatParts parts)
    {
        StringBuilder result = new StringBuilder();
        int index = 0;
        while (index < template.Length)
        {
            if (template[index] == '{')
            {
                int close = template.IndexOf('}', index + 1);
                string component = template.Substring(
                    index + 1,
                    close - index - 1);
                Append(context, result, Value(parts, component));
                index = close + 1;
                continue;
            }
            Append(context, result, template[index].ToString());
            index++;
        }
        return result.ToString();
    }

    static string Value(AddressFormatParts parts, string component)
    {
        if (component == "buildingNumber")
        {
            return parts.BuildingNumber;
        }
        if (component == "streetName")
        {
            return parts.StreetName;
        }
        if (component == "streetType")
        {
            return parts.StreetType;
        }
        if (component == "unitType")
        {
            return parts.UnitType;
        }
        if (component == "unit")
        {
            return parts.Unit;
        }
        if (component == "poBox")
        {
            return parts.PoBox;
        }
        if (component == "district")
        {
            return parts.District;
        }
        if (component == "city")
        {
            return parts.City;
        }
        if (component == "county")
        {
            return parts.County;
        }
        if (component == "subdivision")
        {
            return parts.Subdivision;
        }
        if (component == "postalCode")
        {
            return parts.PostalCode;
        }
        return parts.Country;
    }

    static void Append(
        GenerationContext context,
        StringBuilder builder,
        string value)
    {
        Guard.OutputSize(
            (long)builder.Length + (long)value.Length,
            context.CapturedOptions.MaximumOutputSize,
            "address");
        builder.Append(value);
    }
}
