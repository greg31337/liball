using System;
using System.Collections.Generic;

namespace fix;

internal class CSharpNamePlan
{
    internal string RootNamespace = "";
    internal string VersionNamespace = "";
    internal Dictionary<int, string> FieldNames = new Dictionary<int, string>();
    internal Dictionary<string, string> MessageNames = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<string, string> CodeSetNames = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<string, string> CodeNames = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<int, string> ComponentNames = new Dictionary<int, string>();
    internal Dictionary<int, string> GroupNames = new Dictionary<int, string>();
    internal Dictionary<string, string> ScenarioNames = new Dictionary<string, string>(StringComparer.Ordinal);
    internal Dictionary<string, string> ScenarioNamespaces = new Dictionary<string, string>(StringComparer.Ordinal);
    internal List<GenerationDiagnostic> Diagnostics = [];
}
