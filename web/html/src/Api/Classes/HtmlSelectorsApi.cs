using System;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlSelectors operations.</summary>
public class HtmlSelectorsApi : IHtmlSelectorsApi
{
    /// <summary>Executes the Compile operation.</summary>
    public HtmlSelectorCompileResult Compile(string selector)
    {
        return HtmlSelectors.Compile(selector);
    }

    /// <summary>Executes the CompileWithLimits operation.</summary>
    public HtmlSelectorCompileResult CompileWithLimits(string selector, HtmlLimits limits)
    {
        return HtmlSelectors.Compile(selector, limits);
    }

    /// <summary>Compiles a forgiving selector list, discarding invalid branches.</summary>
    public HtmlSelectorCompileResult CompileForgiving(string selector)
    {
        return HtmlSelectorCompiler.CompileForgiving(selector,
            HtmlLimits.CreateDefault());
    }

    /// <summary>Compiles a forgiving selector list with explicit limits.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithLimits(
        string selector, HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileForgiving(selector, limits);
    }

    /// <summary>Compiles a strict selector list with namespace-prefix bindings.</summary>
    public HtmlSelectorCompileResult CompileWithNamespaces(string selector,
        HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        return HtmlSelectorCompiler.CompileWithNamespaces(selector,
            namespaceBindings, HtmlLimits.CreateDefault());
    }

    /// <summary>Compiles a strict selector list with namespace-prefix bindings and explicit limits.</summary>
    public HtmlSelectorCompileResult CompileWithNamespacesAndLimits(
        string selector, HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileWithNamespaces(selector,
            namespaceBindings, limits);
    }

    /// <summary>Compiles a forgiving selector list with namespace-prefix bindings.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithNamespaces(
        string selector, HtmlSelectorNamespaceBinding[] namespaceBindings)
    {
        return HtmlSelectorCompiler.CompileForgivingWithNamespaces(selector,
            namespaceBindings, HtmlLimits.CreateDefault());
    }

    /// <summary>Compiles a forgiving selector list with namespace-prefix bindings and explicit limits.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithNamespacesAndLimits(
        string selector, HtmlSelectorNamespaceBinding[] namespaceBindings,
        HtmlLimits limits)
    {
        return HtmlSelectorCompiler.CompileForgivingWithNamespaces(selector,
            namespaceBindings, limits);
    }

    /// <summary>Executes the EscapeIdentifier operation.</summary>
    public string EscapeIdentifier(string value)
    {
        return HtmlSelectors.EscapeIdentifier(value);
    }

    /// <summary>Executes the EscapeString operation.</summary>
    public string EscapeString(string value)
    {
        return HtmlSelectors.EscapeString(value);
    }
}
