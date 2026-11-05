using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Represents a value that the template runtime can inspect, render, iterate, index, or invoke.
/// </summary>
/// <remarks>
/// Only the field associated with <c>Kind</c> is meaningful. Safe strings bypass automatic escaping and should contain only content trusted for the selected output format.
/// </remarks>
public class TemplateValue
{
    /// <summary>
    /// Identifies the kind represented by this value.
    /// </summary>
    public TemplateValueKind Kind;
    /// <summary>
    /// Stores the Boolean payload when <c>Kind</c> is <c>Boolean</c>.
    /// </summary>
    public bool BooleanValue;
    /// <summary>
    /// Stores the numeric payload when <c>Kind</c> is <c>Number</c>.
    /// </summary>
    public decimal NumberValue;
    /// <summary>
    /// Stores the text payload for string and safe-string values.
    /// </summary>
    public string StringValue;
    /// <summary>
    /// Stores list items when <c>Kind</c> is <c>List</c>.
    /// </summary>
    public List<TemplateValue> ListValue;
    /// <summary>
    /// Stores ordinally keyed entries when <c>Kind</c> is <c>Map</c>.
    /// </summary>
    public Dictionary<string, TemplateValue> MapValue;
    /// <summary>
    /// Stores the wrapped CLR object when <c>Kind</c> is <c>Object</c>.
    /// </summary>
    public object? ObjectValue;
    /// <summary>
    /// Stores the callable delegate when <c>Kind</c> is <c>Function</c>.
    /// </summary>
    public Func<List<TemplateValue>, TemplateValue>? FunctionValue;
    /// <summary>
    /// Stores reusable syntax nodes when <c>Kind</c> is <c>Fragment</c>.
    /// </summary>
    public List<TemplateSyntaxNode> FragmentValue;

    /// <summary>
    /// Initializes a new template value and validates any supplied values.
    /// </summary>
    /// <param name="kind">The kind represented by the new value.</param>
    public TemplateValue(TemplateValueKind kind)
    {
        Kind = kind;
        BooleanValue = false;
        NumberValue = 0m;
        StringValue = "";
        ListValue = new List<TemplateValue>();
        MapValue = new Dictionary<string, TemplateValue>();
        ObjectValue = null;
        FunctionValue = null;
        FragmentValue = new List<TemplateSyntaxNode>();
    }

    /// <summary>
    /// Creates the sentinel used when name or member lookup finds no value.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal static TemplateValue Missing()
    {
        return new TemplateValue(TemplateValueKind.Missing);
    }

    /// <summary>
    /// Creates a present template value whose content is explicitly null.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    internal static TemplateValue Null()
    {
        return new TemplateValue(TemplateValueKind.Null);
    }

    /// <summary>
    /// Creates a template representation from the supplied boolean.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateValue FromBoolean(bool value)
    {
        TemplateValue result = new TemplateValue(TemplateValueKind.Boolean);
        result.BooleanValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied number.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    internal static TemplateValue FromNumber(decimal value)
    {
        TemplateValue result = new TemplateValue(TemplateValueKind.Number);
        result.NumberValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied string.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.String);
        result.StringValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied safe string.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromSafeString(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.SafeString);
        result.StringValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied list.
    /// </summary>
    /// <param name="values">The values to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromList(List<TemplateValue> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.List);
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
            {
                throw new ArgumentException("List values cannot contain null.", "values");
            }

            result.ListValue.Add(values[index]);
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied map.
    /// </summary>
    /// <param name="values">The values to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromMap(Dictionary<string, TemplateValue> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.Map);
        foreach (KeyValuePair<string, TemplateValue> pair in values)
        {
            if (pair.Key == null)
            {
                throw new ArgumentException("Map keys cannot contain null.", "values");
            }

            if (pair.Value == null)
            {
                throw new ArgumentException("Map values cannot contain null.", "values");
            }

            result.MapValue[pair.Key] = pair.Value;
        }

        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied object.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromObject(object value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.Object);
        result.ObjectValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied function.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromFunction(Func<List<TemplateValue>, TemplateValue> value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.Function);
        result.FunctionValue = value;
        return result;
    }

    /// <summary>
    /// Creates a template representation from the supplied fragment.
    /// </summary>
    /// <param name="nodes">The nodes used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateValue FromFragment(List<TemplateSyntaxNode> nodes)
    {
        if (nodes == null)
        {
            throw new ArgumentNullException("nodes");
        }

        TemplateValue result = new TemplateValue(TemplateValueKind.Fragment);
        int index = 0;
        while (index < nodes.Count)
        {
            if (nodes[index] == null)
            {
                throw new ArgumentException("Fragment nodes cannot contain null.", "nodes");
            }

            result.FragmentValue.Add(nodes[index]);
            index = index + 1;
        }

        return result;
    }

    /// <summary>
    /// Determines whether the missing condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsMissing()
    {
        return Kind == TemplateValueKind.Missing;
    }

    /// <summary>
    /// Determines whether the null condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsNull()
    {
        return Kind == TemplateValueKind.Null;
    }

    /// <summary>
    /// Determines whether the string like condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsStringLike()
    {
        return Kind == TemplateValueKind.String || Kind == TemplateValueKind.SafeString;
    }

    /// <summary>
    /// Determines whether the safe string condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsSafeString()
    {
        return Kind == TemplateValueKind.SafeString;
    }

    /// <summary>
    /// Determines whether the collection condition is satisfied.
    /// </summary>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsCollection()
    {
        return Kind == TemplateValueKind.List || Kind == TemplateValueKind.Map || Kind == TemplateValueKind.Fragment;
    }

    /// <summary>
    /// Returns the item at a zero-based list index, or a missing value when the index is outside the list.
    /// </summary>
    /// <param name="index">The index or key used for lookup.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    internal TemplateValue GetListItem(int index)
    {
        if (Kind != TemplateValueKind.List)
        {
            return Missing();
        }

        if (index < 0 || index >= ListValue.Count)
        {
            return Missing();
        }

        return ListValue[index];
    }

    /// <summary>
    /// Returns the value for an ordinal map key, or a missing value when the key is absent.
    /// </summary>
    /// <param name="key">The key used by the operation.</param>
    /// <returns>The requested value or a defensive copy of the requested collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal TemplateValue GetMapItem(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (Kind != TemplateValueKind.Map)
        {
            return Missing();
        }

        if (!MapValue.ContainsKey(key))
        {
            return Missing();
        }

        return MapValue[key];
    }

    /// <summary>
    /// Invokes a function value with the supplied arguments and rejects non-function values.
    /// </summary>
    /// <param name="arguments">The ordered arguments supplied by the template.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the operation violates template structure, configuration, security, or runtime limits.</exception>
    internal TemplateValue Invoke(List<TemplateValue> arguments)
    {
        if (arguments == null)
        {
            throw new ArgumentNullException("arguments");
        }

        if (Kind != TemplateValueKind.Function || FunctionValue == null)
        {
            throw new InvalidOperationException("Template value is not a function.");
        }

        TemplateValue result = FunctionValue(arguments);
        if (result == null)
        {
            throw new InvalidOperationException("Template function returned null.");
        }

        return result;
    }
}
