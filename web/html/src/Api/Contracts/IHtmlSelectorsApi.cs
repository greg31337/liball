using System;
using System.Threading;

namespace html;
/// <summary>Defines instance-based access to the HtmlSelectors operations.</summary>
public interface IHtmlSelectorsApi
{
    /// <summary>Executes the Compile operation.</summary>
    public HtmlSelectorCompileResult Compile(string selector);
    /// <summary>Executes the CompileWithLimits operation.</summary>
    public HtmlSelectorCompileResult CompileWithLimits(string selector, HtmlLimits limits);
    /// <summary>Compiles a forgiving selector list, discarding invalid branches.</summary>
    public HtmlSelectorCompileResult CompileForgiving(string selector);
    /// <summary>Compiles a forgiving selector list with explicit limits.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithLimits(string selector, HtmlLimits limits);
    /// <summary>Compiles a strict selector list with namespace-prefix bindings.</summary>
    public HtmlSelectorCompileResult CompileWithNamespaces(string selector, HtmlSelectorNamespaceBinding[] namespaceBindings);
    /// <summary>Compiles a strict selector list with namespace-prefix bindings and explicit limits.</summary>
    public HtmlSelectorCompileResult CompileWithNamespacesAndLimits(string selector, HtmlSelectorNamespaceBinding[] namespaceBindings, HtmlLimits limits);
    /// <summary>Compiles a forgiving selector list with namespace-prefix bindings.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithNamespaces(string selector, HtmlSelectorNamespaceBinding[] namespaceBindings);
    /// <summary>Compiles a forgiving selector list with namespace-prefix bindings and explicit limits.</summary>
    public HtmlSelectorCompileResult CompileForgivingWithNamespacesAndLimits(string selector, HtmlSelectorNamespaceBinding[] namespaceBindings, HtmlLimits limits);
    /// <summary>Executes the EscapeIdentifier operation.</summary>
    public string EscapeIdentifier(string value);
    /// <summary>Executes the EscapeString operation.</summary>
    public string EscapeString(string value);
}
