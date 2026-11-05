using System.Collections.Generic;
using System.Text.Json;

namespace template;

internal static class TemplateJsonSerializer
{
    internal static string Serialize(TemplateValue value)
    {
        object? jsonValue = ConvertValue(value);
        return JsonSerializer.Serialize(jsonValue);
    }

    internal static object? ConvertValue(TemplateValue value)
    {
        if (value.IsMissing() || value.IsNull())
        {
            return null;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue;
        }

        if (value.IsStringLike())
        {
            return value.StringValue;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            List<object?> items = new List<object?>();
            int index = 0;
            while (index < value.ListValue.Count)
            {
                items.Add(ConvertValue(value.ListValue[index]));
                index = index + 1;
            }

            return items;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            Dictionary<string, object?> items = new Dictionary<string, object?>();
            foreach (KeyValuePair<string, TemplateValue> pair in value.MapValue)
            {
                items[pair.Key] = ConvertValue(pair.Value);
            }

            return items;
        }

        if (value.ObjectValue != null)
        {
            return value.ObjectValue;
        }

        return null;
    }
}
