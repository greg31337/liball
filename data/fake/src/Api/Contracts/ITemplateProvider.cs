using System;
using System.Collections.Generic;

namespace fake;

/// <summary>Defines named-token registration, resolution, validation, and rendering operations.</summary>
public interface ITemplateProvider
{
    /// <summary>Registers a case-sensitive caller resolver for one token name.</summary>
    /// <remarks>Built-in names cannot be replaced. The resolver receives null when no format argument was supplied and otherwise receives the exact text after the first colon.</remarks>
    /// <param name="name">The token name containing ASCII letters, digits, dots, hyphens, or underscores.</param>
    /// <param name="resolver">The callback that returns final replacement text.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name is malformed, reserved, or already registered.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured count limit is insufficient.</exception>
    void RegisterFromNameAndResolver(string name, Func<string?, string> resolver);

    /// <summary>Resolves one built-in or registered named token without a format argument.</summary>
    /// <param name="name">The token name to resolve.</param>
    /// <returns>The resolved replacement text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when name is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when name is malformed, unknown, or requires an argument.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Resolve(string name);

    /// <summary>Resolves one built-in or registered named token with an explicit format argument.</summary>
    /// <param name="name">The token name to resolve.</param>
    /// <param name="argument">The exact format text supplied to the resolver.</param>
    /// <returns>The resolved replacement text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when the name, argument, or resolved text is malformed.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string ResolveFromNameAndArgument(string name, string argument);

    /// <summary>Renders a template using built-in and registered resolvers.</summary>
    /// <remarks>Evaluation is left-to-right and depth-first. A backslash escapes a backslash or brace.</remarks>
    /// <param name="template">The template containing <c>{{name}}</c> placeholders.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when template is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Render(string template);

    /// <summary>Renders a template with caller-supplied nested values.</summary>
    /// <remarks>Built-ins take precedence over all caller names. Per-render values take precedence over registered resolvers.</remarks>
    /// <param name="template">The template to render.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, nested values, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string RenderFromTemplateAndValues(string template, Dictionary<string, string> values);

    /// <summary>Renders a template with caller values and an explicit nesting limit.</summary>
    /// <param name="template">The template to render.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <param name="maximumDepth">The positive maximum number of nested value expansions.</param>
    /// <returns>The rendered text.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when a reference argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when tokenization, escaping, resolution, nesting, or callback output is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string RenderFromTemplateAndValuesAndMaximumDepth(string template, Dictionary<string, string> values, int maximumDepth);

    /// <summary>Validates a template without invoking resolvers or consuming random state.</summary>
    /// <param name="template">The template to validate.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when template is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with the failing index when the template is invalid.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    void Validate(string template);

    /// <summary>Validates a template and its referenced caller values without invoking resolvers.</summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with a token and position diagnostic when validation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    void ValidateFromTemplateAndValues(string template, Dictionary<string, string> values);

    /// <summary>Validates a template and caller values with an explicit nesting limit without invoking resolvers.</summary>
    /// <param name="template">The template to validate.</param>
    /// <param name="values">Named nested templates resolved using the dictionary comparer.</param>
    /// <param name="maximumDepth">The positive maximum number of nested value expansions.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when a reference argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown with a token and position diagnostic when validation fails.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    void ValidateFromTemplateAndValuesAndMaximumDepth(string template, Dictionary<string, string> values, int maximumDepth);

    /// <summary>Returns built-in tokens followed by caller tokens in registration order.</summary>
    /// <remarks>The returned array is independent and does not include per-render value names.</remarks>
    /// <returns>A new array containing every currently resolvable provider token.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string[] Tokens();
}
