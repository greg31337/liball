namespace svg;

/// <summary>Stores selector namespace bindings independently of stylesheet parsing.</summary>
public class SvgSelectorNamespaceBindings
{
    /// <summary>Whether an explicit default namespace is configured.</summary>
    public bool HasDefaultNamespace;
    /// <summary>The explicit default namespace URI, or an empty string.</summary>
    public string DefaultNamespace;
    /// <summary>Named prefix-to-namespace bindings.</summary>
    public System.Collections.Generic.Dictionary<string, string> NamedNamespaces;

    /// <summary>Creates bindings with no default namespace and the reserved XML prefix.</summary>
    public SvgSelectorNamespaceBindings()
    {
        HasDefaultNamespace = false;
        DefaultNamespace = "";
        NamedNamespaces = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal);
        NamedNamespaces["xml"] = SvgNamespaces.Xml;
    }

    /// <summary>Sets an explicit default namespace, including the empty namespace.</summary>
    /// <param name="namespaceUri">The namespace URI.</param>
    internal void SetDefault(string namespaceUri)
    {
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        HasDefaultNamespace = true;
        DefaultNamespace = namespaceUri;
    }

    /// <summary>Clears the explicit default namespace constraint.</summary>
    internal void ClearDefault()
    {
        HasDefaultNamespace = false;
        DefaultNamespace = "";
    }

    /// <summary>Sets or replaces one nonempty named prefix binding.</summary>
    /// <param name="prefix">The nonempty prefix.</param>
    /// <param name="namespaceUri">The namespace URI, including an empty namespace.</param>
    internal void Bind(string prefix, string namespaceUri)
    {
        if (prefix == null) throw new System.ArgumentNullException("prefix");
        if (namespaceUri == null) throw new System.ArgumentNullException("namespaceUri");
        if (prefix.Length == 0) throw new System.ArgumentOutOfRangeException("prefix");
        NamedNamespaces[prefix] = namespaceUri;
    }

    /// <summary>Removes one named prefix binding.</summary>
    /// <param name="prefix">The prefix to remove.</param>
    /// <returns>Whether a binding was removed.</returns>
    internal bool Remove(string prefix)
    {
        if (prefix == null) throw new System.ArgumentNullException("prefix");
        if (prefix == "xml") return false;
        return NamedNamespaces.Remove(prefix);
    }
}

/// <summary>Configures one bounded selector matching or query operation.</summary>
public class SvgSelectorMatchOptions
{
    /// <summary>The scoping root used by <c>:scope</c>, or <c>null</c> for the operation default.</summary>
    public SvgNode? Scope;
    /// <summary>The explicit namespace bindings.</summary>
    public SvgSelectorNamespaceBindings Namespaces;
    /// <summary>The host-provided dynamic selector state.</summary>
    public ISvgDynamicSelectorState DynamicState;
    /// <summary>The maximum matching and traversal work units.</summary>
    public long MaxWork;

    /// <summary>Creates deterministic selector matching defaults.</summary>
    public SvgSelectorMatchOptions()
    {
        Scope = null;
        Namespaces = new SvgSelectorNamespaceBindings();
        DynamicState = new SvgDynamicSelectorState(new SvgDynamicSelectorStateOptions());
        MaxWork = 1000000;
    }
}

/// <summary>Reports one bounded selector match.</summary>
public class SvgSelectorMatchResult
{
    /// <summary>Whether any selector-list item matched.</summary>
    public bool Matches;
    /// <summary>Whether matching completed without invalid input or a work limit.</summary>
    public bool Success;
    /// <summary>Whether the configured work limit was exceeded.</summary>
    public bool LimitExceeded;
    /// <summary>The consumed matching work units.</summary>
    public long Work;
    /// <summary>The strongest matching selector item's ID specificity.</summary>
    public int IdSpecificity;
    /// <summary>The strongest matching selector item's class-like specificity.</summary>
    public int ClassSpecificity;
    /// <summary>The strongest matching selector item's type specificity.</summary>
    public int TypeSpecificity;
    /// <summary>Diagnostics produced by matching.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgSelectorMatchResult(bool matches, bool success, bool limitExceeded, long work, int idSpecificity, int classSpecificity, int typeSpecificity, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        Matches = matches;
        Success = success;
        LimitExceeded = limitExceeded;
        Work = work;
        IdSpecificity = idSpecificity;
        ClassSpecificity = classSpecificity;
        TypeSpecificity = typeSpecificity;
        Diagnostics = diagnostics;
    }
}

/// <summary>Reports one bounded selector query in document order.</summary>
public class SvgSelectorQueryResult
{
    /// <summary>Whether parsing and matching completed without an invalid input or work limit.</summary>
    public bool Success;
    /// <summary>The first matching element, or <c>null</c>.</summary>
    public SvgNode? First;
    /// <summary>The matching elements in preorder.</summary>
    public System.Collections.Generic.List<SvgNode> Nodes;
    /// <summary>Whether the configured work limit was exceeded.</summary>
    public bool LimitExceeded;
    /// <summary>The consumed traversal and matching work units.</summary>
    public long Work;
    /// <summary>Diagnostics produced by parsing or matching.</summary>
    public System.Collections.Generic.List<SvgDiagnostic> Diagnostics;

    internal SvgSelectorQueryResult(bool success, System.Collections.Generic.List<SvgNode> nodes, bool limitExceeded, long work, System.Collections.Generic.List<SvgDiagnostic> diagnostics)
    {
        Success = success;
        Nodes = nodes;
        First = nodes.Count == 0 ? null : nodes[0];
        LimitExceeded = limitExceeded;
        Work = work;
        Diagnostics = diagnostics;
    }
}
