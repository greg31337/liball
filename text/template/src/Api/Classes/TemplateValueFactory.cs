using System;
using System.Collections;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Creates template values and converts CLR values for rendering.
/// </summary>
public class TemplateValueFactory : ITemplateValueFactory
{
    internal TemplateValueConversionPolicy Policy;

    /// <summary>
    /// Initializes a template value factory.
    /// </summary>
    public TemplateValueFactory()
    {
        Policy = new TemplateValueConversionPolicy();
    }

    /// <summary>
    /// Configures culture and custom conversion behavior.
    /// </summary>
    /// <param name="policy">The conversion policy to use.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>policy</c> is <c>null</c>.</exception>
    public void Configure(TemplateValueConversionPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        policy.Validate();
        Policy = policy;
    }

    /// <summary>Creates a missing value.</summary>
    /// <returns>A missing template value.</returns>
    public TemplateValue CreateMissing()
    {
        return TemplateValue.Missing();
    }

    /// <summary>Creates a null value.</summary>
    /// <returns>A null template value.</returns>
    public TemplateValue CreateNull()
    {
        return TemplateValue.Null();
    }

    /// <summary>Creates a Boolean value.</summary>
    /// <param name="value">The Boolean value.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateBoolean(bool value)
    {
        return TemplateValue.FromBoolean(value);
    }

    /// <summary>Creates a numeric value.</summary>
    /// <param name="value">The numeric value.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateNumber(decimal value)
    {
        return TemplateValue.FromNumber(value);
    }

    /// <summary>Creates a string value.</summary>
    /// <param name="value">The string value.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateString(string value)
    {
        return TemplateValue.FromString(value);
    }

    /// <summary>Creates a string value that bypasses output escaping.</summary>
    /// <param name="value">The trusted string value.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateSafeString(string value)
    {
        return TemplateValue.FromSafeString(value);
    }

    /// <summary>Creates a list value.</summary>
    /// <param name="values">The list values.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateList(List<TemplateValue> values)
    {
        return TemplateValue.FromList(values);
    }

    /// <summary>Creates a map value.</summary>
    /// <param name="values">The map values.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateMap(Dictionary<string, TemplateValue> values)
    {
        return TemplateValue.FromMap(values);
    }

    /// <summary>Creates an object-backed value.</summary>
    /// <param name="value">The CLR object.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateObject(object value)
    {
        return TemplateValue.FromObject(value);
    }

    /// <summary>Creates a callable value.</summary>
    /// <param name="value">The callable.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateFunction(Func<List<TemplateValue>, TemplateValue> value)
    {
        return TemplateValue.FromFunction(value);
    }

    /// <summary>Creates a template fragment value.</summary>
    /// <param name="nodes">The fragment nodes.</param>
    /// <returns>The template value.</returns>
    public TemplateValue CreateFragment(List<TemplateSyntaxNode> nodes)
    {
        return TemplateValue.FromFragment(nodes);
    }

    /// <summary>Converts a CLR value.</summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The template value.</returns>
    public TemplateValue Convert(object? value)
    {
        return TemplateValueConverter.ConvertConfigured(value, Policy);
    }

    /// <summary>Converts a non-generic enumerable.</summary>
    /// <param name="values">The values to convert.</param>
    /// <returns>The template value.</returns>
    public TemplateValue ConvertEnumerable(IEnumerable values)
    {
        return TemplateValueConverter.FromEnumerableConfigured(values, Policy);
    }

    /// <summary>Converts a non-generic dictionary.</summary>
    /// <param name="values">The values to convert.</param>
    /// <returns>The template value.</returns>
    public TemplateValue ConvertDictionary(IDictionary values)
    {
        return TemplateValueConverter.FromDictionaryConfigured(values, Policy);
    }
}
