using System;
using System.Collections.Generic;

namespace fake;

/// <summary>Provides stateful named-token registration, resolution, validation, and rendering.</summary>
public class TemplateProvider : ITemplateProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal TemplateResolverRegistry Resolvers;

    internal TemplateProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("template");
        Resolvers = new TemplateResolverRegistry(context.CapturedOptions.MaximumCount);
    }

    /// <summary>Registers a case-sensitive caller resolver for one token name.</summary>
    /// <remarks>Built-in names cannot be replaced. The resolver receives null when no format argument was supplied and otherwise receives the exact text after the first colon.</remarks>
    /// <param name="name">The token name containing ASCII letters, digits, dots, hyphens, or underscores.</param>
    /// <param name="resolver">The callback that returns final replacement text.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name is malformed, reserved, or already registered.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count limit is insufficient.</exception>
    public void RegisterFromNameAndResolver(string name, Func<string?, string> resolver)
    {
        Resolvers.Register(name, resolver);
    }

    /// <summary>Resolves one built-in or registered named token without a format argument.</summary>
    /// <param name="name">The token name to resolve.</param>
    /// <returns>The resolved replacement text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when name is malformed, unknown, or requires an argument.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Resolve(string name)
    {
        return TemplateEngine.ResolveWithResolvers(
            RandomValue,
            name,
            null,
            Resolvers,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Resolves one built-in or registered named token with an explicit format argument.</summary>
    /// <param name="name">The token name to resolve.</param>
    /// <param name="argument">The exact format text supplied to the resolver.</param>
    /// <returns>The resolved replacement text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name, argument, or resolved text is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string ResolveFromNameAndArgument(string name, string argument)
    {
        Guard.NotNull(argument, "argument");
        return TemplateEngine.ResolveWithResolvers(
            RandomValue,
            name,
            argument,
            Resolvers,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Renders a template using built-in and registered resolvers.</summary>
    /// <remarks>Evaluation is left-to-right and depth-first. A backslash escapes a backslash or brace.</remarks>
    /// <param name="template">The template containing <c>{{name}}</c> placeholders.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when template is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Render(string template)
    {
        return RenderFromTemplateAndValuesAndMaximumDepth(
            template,
            new Dictionary<string, string>(),
            16);
    }

    /// <summary>Renders a template with caller-supplied nested values.</summary>
    /// <remarks>Built-ins take precedence over all caller names. Per-render values take precedence over registered resolvers.</remarks>
    /// <param name="template">The template to render.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, nested values, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string RenderFromTemplateAndValues(string template, Dictionary<string, string> values)
    {
        return RenderFromTemplateAndValuesAndMaximumDepth(template, values, 16);
    }

    /// <summary>Renders a template with caller values and an explicit nesting limit.</summary>
    /// <param name="template">The template to render.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <param name="maximumDepth">The positive maximum number of nested value expansions.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a reference argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, nesting, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string RenderFromTemplateAndValuesAndMaximumDepth(string template, Dictionary<string, string> values, int maximumDepth)
    {
        return TemplateEngine.GenerateWithResolvers(
            RandomValue,
            template,
            values,
            Resolvers,
            maximumDepth,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Validates a template without invoking resolvers or consuming random state.</summary>
    /// <param name="template">The template to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when template is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with the failing index when the template is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public void Validate(string template)
    {
        ValidateFromTemplateAndValues(template, new Dictionary<string, string>());
    }

    /// <summary>Validates a template and its referenced caller values without invoking resolvers.</summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with a token and position diagnostic when validation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public void ValidateFromTemplateAndValues(string template, Dictionary<string, string> values)
    {
        ValidateFromTemplateAndValuesAndMaximumDepth(template, values, 16);
    }

    /// <summary>Validates a template and caller values with an explicit nesting limit without invoking resolvers.</summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <param name="maximumDepth">The positive maximum number of nested value expansions.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a reference argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with a token and position diagnostic when validation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public void ValidateFromTemplateAndValuesAndMaximumDepth(string template, Dictionary<string, string> values, int maximumDepth)
    {
        TemplateEngine.ValidateWithResolvers(
            RandomValue,
            template,
            values,
            Resolvers,
            maximumDepth,
            Context.CapturedOptions.MaximumCount,
            Context.CapturedOptions.MaximumOutputSize);
    }

    /// <summary>Returns built-in tokens followed by caller tokens in registration order.</summary>
    /// <remarks>The returned array is independent and does not include per-render value names.</remarks>
    /// <returns>A new array containing every currently resolvable provider token.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string[] Tokens()
    {
        string[] builtIn = TemplateEngine.Tokens();
        string[] caller = Resolvers.Names();
        Guard.Count(
            builtIn.Length + caller.Length,
            Context.CapturedOptions.MaximumCount,
            "tokens");
        long outputSize = 0L;
        int sizeIndex = 0;
        while (sizeIndex < builtIn.Length)
        {
            outputSize = outputSize + builtIn[sizeIndex].Length;
            sizeIndex = sizeIndex + 1;
        }
        sizeIndex = 0;
        while (sizeIndex < caller.Length)
        {
            outputSize = outputSize + caller[sizeIndex].Length;
            sizeIndex = sizeIndex + 1;
        }
        Guard.OutputSize(
            outputSize,
            Context.CapturedOptions.MaximumOutputSize,
            "tokens");
        string[] result = new string[builtIn.Length + caller.Length];
        int index = 0;
        while (index < builtIn.Length)
        {
            result[index] = builtIn[index];
            index = index + 1;
        }
        int callerIndex = 0;
        while (callerIndex < caller.Length)
        {
            result[index] = caller[callerIndex];
            index = index + 1;
            callerIndex = callerIndex + 1;
        }
        return result;
    }
}
