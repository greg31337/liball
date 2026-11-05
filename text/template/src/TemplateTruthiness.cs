using System;

namespace template;

/// <summary>
/// Applies native or compatibility-profile truthiness rules to template values.
/// </summary>
internal static class TemplateTruthiness
{
    /// <summary>
    /// Determines whether the truthy condition is satisfied.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public static bool IsTruthy(TemplateValue value)
    {
        TemplateOptions options = new TemplateOptions();
        return IsTruthy(value, options);
    }

    /// <summary>
    /// Determines whether the truthy condition is satisfied.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static bool IsTruthy(TemplateValue value, TemplateOptions options)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        TemplateTruthinessMode mode = ResolveMode(options);
        return IsTruthy(value, mode);
    }

    /// <summary>
    /// Determines whether the truthy condition is satisfied.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static bool IsTruthy(TemplateValue value, TemplateTruthinessMode mode)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (mode == TemplateTruthinessMode.ProfileDefault || mode == TemplateTruthinessMode.Native)
        {
            return IsTruthyNative(value);
        }

        if (mode == TemplateTruthinessMode.Mustache)
        {
            return IsTruthyMustache(value);
        }

        if (mode == TemplateTruthinessMode.Handlebars)
        {
            return IsTruthyHandlebars(value);
        }

        if (mode == TemplateTruthinessMode.Liquid)
        {
            return IsTruthyLiquid(value);
        }

        if (mode == TemplateTruthinessMode.Jinja)
        {
            return IsTruthyJinja(value);
        }

        return IsTruthyNative(value);
    }

    /// <summary>
    /// Determines whether the falsy condition is satisfied.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public static bool IsFalsy(TemplateValue value, TemplateOptions options)
    {
        return !IsTruthy(value, options);
    }

    /// <summary>
    /// Determines whether the falsy condition is satisfied.
    /// </summary>
    /// <param name="value">The value to process.</param>
    /// <param name="mode">The behavior mode to apply.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    public static bool IsFalsy(TemplateValue value, TemplateTruthinessMode mode)
    {
        return !IsTruthy(value, mode);
    }

    /// <summary>
    /// Resolves the requested value using configured template semantics and access policy.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public static TemplateTruthinessMode ResolveMode(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (options.TruthinessMode != TemplateTruthinessMode.ProfileDefault)
        {
            return options.TruthinessMode;
        }

        if (options.SyntaxProfile == "mustache")
        {
            return TemplateTruthinessMode.Mustache;
        }

        if (options.SyntaxProfile == "handlebars")
        {
            return TemplateTruthinessMode.Handlebars;
        }

        if (options.SyntaxProfile == "liquid")
        {
            return TemplateTruthinessMode.Liquid;
        }

        if (options.SyntaxProfile == "jinja" || options.SyntaxProfile == "django" || options.SyntaxProfile == "nunjucks")
        {
            return TemplateTruthinessMode.Jinja;
        }

        return TemplateTruthinessMode.Native;
    }

    internal static bool IsTruthyNative(TemplateValue value)
    {
        if (IsMissingOrNull(value))
        {
            return false;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue != 0m;
        }

        if (value.IsStringLike())
        {
            return value.StringValue.Length > 0;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count > 0;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            return value.MapValue.Count > 0;
        }

        if (value.Kind == TemplateValueKind.Fragment)
        {
            return value.FragmentValue.Count > 0;
        }

        return true;
    }

    internal static bool IsTruthyMustache(TemplateValue value)
    {
        if (IsMissingOrNull(value))
        {
            return false;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.IsStringLike())
        {
            return value.StringValue.Length > 0;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count > 0;
        }

        if (value.Kind == TemplateValueKind.Fragment)
        {
            return value.FragmentValue.Count > 0;
        }

        return true;
    }

    internal static bool IsTruthyHandlebars(TemplateValue value)
    {
        if (IsMissingOrNull(value))
        {
            return false;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        if (value.Kind == TemplateValueKind.Number)
        {
            return value.NumberValue != 0m;
        }

        if (value.IsStringLike())
        {
            return value.StringValue.Length > 0;
        }

        if (value.Kind == TemplateValueKind.List)
        {
            return value.ListValue.Count > 0;
        }

        if (value.Kind == TemplateValueKind.Map)
        {
            return true;
        }

        if (value.Kind == TemplateValueKind.Fragment)
        {
            return value.FragmentValue.Count > 0;
        }

        return true;
    }

    internal static bool IsTruthyLiquid(TemplateValue value)
    {
        if (IsMissingOrNull(value))
        {
            return false;
        }

        if (value.Kind == TemplateValueKind.Boolean)
        {
            return value.BooleanValue;
        }

        return true;
    }

    internal static bool IsTruthyJinja(TemplateValue value)
    {
        return IsTruthyNative(value);
    }

    internal static bool IsMissingOrNull(TemplateValue value)
    {
        return value.Kind == TemplateValueKind.Missing || value.Kind == TemplateValueKind.Null;
    }
}
