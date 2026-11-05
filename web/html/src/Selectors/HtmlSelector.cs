using System;
using System.Threading;

namespace html;

internal enum HtmlSelectorCandidateKind
{
    AllElements = 0,
    Type = 1,
    Class = 2,
    Id = 3
}

internal class HtmlSelectorMatchPlan
{
    internal HtmlComplexSelectorAst selector;
    internal HtmlSelectorSpecificity specificity;
    internal HtmlSelectorCandidateKind candidateKind;
    internal string candidateValue;

    internal HtmlSelectorMatchPlan(HtmlComplexSelectorAst selectorValue,
        HtmlSelectorSpecificity specificityValue,
        HtmlSelectorCandidateKind candidateKindValue,
        string candidateValueValue)
    {
        this.selector = selectorValue;
        this.specificity = specificityValue;
        this.candidateKind = candidateKindValue;
        this.candidateValue = candidateValueValue;
    }

    internal HtmlSelectorSpecificity GetSpecificity()
    {
        return this.specificity;
    }

    internal HtmlSelectorCandidateKind GetCandidateKind()
    {
        return this.candidateKind;
    }

    internal string GetCandidateValue()
    {
        return this.candidateValue;
    }

    internal int GetCompoundCount()
    {
        return this.selector.GetCompoundsStorage().Length;
    }

    internal HtmlComplexSelectorAst GetSelectorStorage()
    {
        return this.selector;
    }
}

/// <summary>Compiles CSS selectors and queries HTML trees with the resulting match plans.</summary>
internal static class HtmlSelectors
{
    /// <summary>Validates the current settings and returns an immutable configuration for repeated use.</summary>
    /// <param name="selector">The CSS selector text to parse and compile.</param>
    /// <returns>The compiled selector when parsing completed without errors or a limit violation; otherwise, the bounded compilation errors and diagnostics.</returns>
    public static HtmlSelectorCompileResult Compile(string selector)
    {
        return Compile(selector, HtmlLimits.CreateDefault());
    }

    /// <summary>Validates the current settings and returns an immutable configuration for repeated use.</summary>
    /// <param name="selector">The CSS selector text to parse and compile.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <returns>The compiled selector when parsing completed without errors or a limit violation; otherwise, the bounded compilation errors and diagnostics.</returns>
    public static HtmlSelectorCompileResult Compile(string selector,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.Compile(selector, limits);
    }

    /// <summary>Compiles a forgiving selector list and discards invalid branches.</summary>
    /// <param name="selector">The CSS selector-list text.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <returns>A complete or partial compiled selector, or diagnostics when no branch is usable.</returns>
    public static HtmlSelectorCompileResult CompileForgiving(string selector,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileForgiving(selector, limits);
    }

    /// <summary>Compiles a strict selector list using explicit namespace-prefix bindings.</summary>
    /// <param name="selector">The CSS selector-list text.</param>
    /// <param name="namespaceBindings">The prefix bindings available to qualified names.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <returns>The compiled selector or bounded diagnostics.</returns>
    public static HtmlSelectorCompileResult CompileWithNamespaces(
        string selector,
        HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileWithNamespaces(selector,
            namespaceBindings, limits);
    }

    /// <summary>Compiles a forgiving selector list using explicit namespace-prefix bindings.</summary>
    /// <param name="selector">The CSS selector-list text.</param>
    /// <param name="namespaceBindings">The prefix bindings available to qualified names.</param>
    /// <param name="limits">The limits copied and enforced for this operation.</param>
    /// <returns>A complete or partial compiled selector, or diagnostics when no branch is usable.</returns>
    public static HtmlSelectorCompileResult CompileForgivingWithNamespaces(
        string selector,
        HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileForgivingWithNamespaces(selector,
            namespaceBindings, limits);
    }

    /// <summary>Escapes the input as a valid CSS selector escape identifier.</summary>
    /// <param name="value">The identifier text to escape.</param>
    /// <returns>A CSS identifier spelling with unsafe code points escaped.</returns>
    public static string EscapeIdentifier(string value)
    {
        return HtmlSelectorEscaping.EscapeIdentifier(value);
    }

    /// <summary>Escapes the input as a valid CSS selector escape string.</summary>
    /// <param name="value">The string value to escape.</param>
    /// <returns>A CSS string spelling with quotes, backslashes, controls, and line breaks escaped.</returns>
    public static string EscapeString(string value)
    {
        return HtmlSelectorEscaping.EscapeString(value);
    }

    internal static HtmlSelector CompileOrThrowStorage(string selector)
    {
        HtmlSelectorCompileResult result = Compile(selector);
        if (result.IsSuccess())
        {
            HtmlSelector? compiled = result.GetSelector();
            if (compiled == null)
            {
                throw new InvalidOperationException(
                    "Successful selector compilation must produce a selector.");
            }

            return compiled;
        }

        HtmlSelectorCompileError[] errors = result.GetErrors();
        string message = errors.Length == 0
            ? "The selector is invalid."
            : errors[0].GetMessage();
        throw new ArgumentException(message, "selector");
    }
}
