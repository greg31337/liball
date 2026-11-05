using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace template;

/// <summary>
/// Converts common CLR scalar, dictionary, and enumerable values into template runtime values.
/// </summary>
internal static class TemplateValueConverter
{
    /// <summary>
    /// Converts a supported CLR value recursively using invariant built-in behavior.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    public static TemplateValue Convert(object? value)
    {
        return ConvertCore(value, new TemplateValueConversionPolicy());
    }

    internal static TemplateValue ConvertConfigured(object? value, TemplateValueConversionPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        policy.Validate();
        return ConvertCore(value, policy);
    }

    internal static TemplateValue ConvertCore(object? value, TemplateValueConversionPolicy policy)
    {
        if (value == null)
        {
            return TemplateValue.Null();
        }

        TemplateValue? templateValue = value as TemplateValue;
        if (templateValue != null)
        {
            return templateValue;
        }

        Func<List<TemplateValue>, TemplateValue>? functionValue = value as Func<List<TemplateValue>, TemplateValue>;
        if (functionValue != null)
        {
            return TemplateValue.FromFunction(functionValue);
        }

        List<TemplateSyntaxNode>? fragmentValue = value as List<TemplateSyntaxNode>;
        if (fragmentValue != null)
        {
            return TemplateValue.FromFragment(fragmentValue);
        }

        int converterIndex = 0;
        while (converterIndex < policy.Converters.Count)
        {
            TemplateValue? customValue = policy.Converters[converterIndex].Convert(value);
            if (customValue != null)
            {
                return customValue;
            }

            converterIndex = converterIndex + 1;
        }

        IConvertible? convertible = value as IConvertible;
        if (convertible != null)
        {
            TemplateValue converted = ConvertConvertibleWithCulture(convertible, value, policy.Culture);
            if (converted.Kind != TemplateValueKind.Object)
            {
                return converted;
            }
        }

        IDictionary? dictionary = value as IDictionary;
        if (dictionary != null)
        {
            return FromDictionaryCore(dictionary, policy);
        }

        TemplateValue? readOnlyDictionary = ConvertReadOnlyDictionary(value, policy);
        if (readOnlyDictionary != null)
        {
            return readOnlyDictionary;
        }

        IEnumerable? enumerable = value as IEnumerable;
        if (enumerable != null)
        {
            return FromEnumerableCore(enumerable, policy);
        }

        return TemplateValue.FromObject(value);
    }

    /// <summary>
    /// Creates a template representation from the supplied enumerable.
    /// </summary>
    /// <param name="values">The values to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static TemplateValue FromEnumerable(IEnumerable values)
    {
        return FromEnumerableConfigured(values, new TemplateValueConversionPolicy());
    }

    internal static TemplateValue FromEnumerableConfigured(IEnumerable values, TemplateValueConversionPolicy policy)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        policy.Validate();
        return FromEnumerableCore(values, policy);
    }

    internal static TemplateValue FromEnumerableCore(IEnumerable values, TemplateValueConversionPolicy policy)
    {
        List<TemplateValue> converted = new List<TemplateValue>();
        foreach (object? item in values)
        {
            converted.Add(ConvertCore(item, policy));
        }

        return TemplateValue.FromList(converted);
    }

    /// <summary>
    /// Creates a template representation from the supplied dictionary.
    /// </summary>
    /// <param name="values">The values to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static TemplateValue FromDictionary(IDictionary values)
    {
        return FromDictionaryConfigured(values, new TemplateValueConversionPolicy());
    }

    internal static TemplateValue FromDictionaryConfigured(IDictionary values, TemplateValueConversionPolicy policy)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        policy.Validate();
        return FromDictionaryCore(values, policy);
    }

    internal static TemplateValue FromDictionaryCore(IDictionary values, TemplateValueConversionPolicy policy)
    {
        Dictionary<string, TemplateValue> converted = new Dictionary<string, TemplateValue>();
        foreach (DictionaryEntry entry in values)
        {
            string key = ConvertKeyWithCulture(entry.Key, policy.Culture);
            converted[key] = ConvertCore(entry.Value, policy);
        }

        return TemplateValue.FromMap(converted);
    }

    internal static TemplateValue? ConvertReadOnlyDictionary(object value, TemplateValueConversionPolicy policy)
    {
        Type valueType = value.GetType();
        Type[] interfaces = valueType.GetInterfaces();
        bool found = false;
        int interfaceIndex = 0;
        while (interfaceIndex < interfaces.Length)
        {
            string? interfaceName = interfaces[interfaceIndex].FullName;
            if (interfaceName != null && interfaceName.StartsWith("System.Collections.Generic.IReadOnlyDictionary`2", StringComparison.Ordinal))
            {
                found = true;
                break;
            }

            interfaceIndex = interfaceIndex + 1;
        }

        if (!found)
        {
            return null;
        }

        IEnumerable? entries = value as IEnumerable;
        if (entries == null)
        {
            return null;
        }

        Dictionary<string, TemplateValue> converted = new Dictionary<string, TemplateValue>();
        foreach (object? entry in entries)
        {
            if (entry == null)
            {
                throw new ArgumentException("Read-only dictionary entries cannot be null.", "value");
            }

            Type entryType = entry.GetType();
            PropertyInfo? keyProperty = entryType.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo? valueProperty = entryType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);
            if (keyProperty == null || valueProperty == null)
            {
                throw new ArgumentException("Read-only dictionary entries must expose Key and Value.", "value");
            }

            object? key = keyProperty.GetValue(entry, null);
            object? itemValue = valueProperty.GetValue(entry, null);
            converted[ConvertKeyWithCulture(key, policy.Culture)] = ConvertCore(itemValue, policy);
        }

        return TemplateValue.FromMap(converted);
    }

    internal static TemplateValue ConvertConvertible(IConvertible convertible, object originalValue)
    {
        return ConvertConvertibleWithCulture(convertible, originalValue, CultureInfo.InvariantCulture);
    }

    internal static TemplateValue ConvertConvertibleWithCulture(IConvertible convertible, object originalValue, CultureInfo culture)
    {
        TypeCode typeCode = convertible.GetTypeCode();
        switch (typeCode)
        {
            case TypeCode.Empty:
                return TemplateValue.Null();
            case TypeCode.DBNull:
                return TemplateValue.Null();
            case TypeCode.Boolean:
                return TemplateValue.FromBoolean(convertible.ToBoolean(culture));
            case TypeCode.Char:
                return TemplateValue.FromString(convertible.ToChar(culture).ToString());
            case TypeCode.SByte:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.Byte:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.Int16:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.UInt16:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.Int32:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.UInt32:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.Int64:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.UInt64:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.Single:
                return ConvertFloatingPoint(convertible, culture);
            case TypeCode.Double:
                return ConvertFloatingPoint(convertible, culture);
            case TypeCode.Decimal:
                return TemplateValue.FromNumber(convertible.ToDecimal(culture));
            case TypeCode.String:
                return TemplateValue.FromString(convertible.ToString(culture));
            default:
                return TemplateValue.FromObject(originalValue);
        }
    }

    internal static TemplateValue ConvertFloatingPoint(IConvertible convertible, CultureInfo culture)
    {
        double floatingValue = convertible.ToDouble(culture);
        if (double.IsNaN(floatingValue) || double.IsInfinity(floatingValue) || floatingValue > (double)decimal.MaxValue || floatingValue < (double)decimal.MinValue)
        {
            throw new ArgumentOutOfRangeException("value", "Floating-point template values must be finite and fit in Decimal.");
        }

        return TemplateValue.FromNumber(convertible.ToDecimal(culture));
    }

    internal static string ConvertKey(object? key)
    {
        return ConvertKeyWithCulture(key, CultureInfo.InvariantCulture);
    }

    internal static string ConvertKeyWithCulture(object? key, CultureInfo culture)
    {
        if (key == null)
        {
            throw new ArgumentException("Map keys cannot contain null.", "values");
        }

        string? stringKey = key as string;
        if (stringKey != null)
        {
            return stringKey;
        }

        IConvertible? convertible = key as IConvertible;
        if (convertible != null)
        {
            string? converted = convertible.ToString(culture);
            if (converted != null)
            {
                return converted;
            }
        }

        string? fallback = key.ToString();
        if (fallback == null)
        {
            throw new ArgumentException("Map key could not be converted to a string.", "values");
        }

        return fallback;
    }
}
