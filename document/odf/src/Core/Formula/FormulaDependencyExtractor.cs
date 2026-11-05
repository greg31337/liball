using System.Collections.Generic;

namespace odf;

internal static class FormulaDependencyExtractor
{
    internal static OdfFormulaDependency[] Extract(
        OdfFormula formula,
        string ownerSheet)
    {
        if (formula == null || formula.Root == null)
        {
            return [];
        }
        List<OdfFormulaDependency> dependencies =
            new List<OdfFormulaDependency>();
        Collect(formula.Root, ownerSheet, dependencies);
        formula.Dependencies = dependencies.ToArray();
        return formula.Dependencies;
    }

    static void Collect(
        OdfFormulaNode node,
        string ownerSheet,
        List<OdfFormulaDependency> dependencies)
    {
        if (node.Children.Length == 0 && node.Reference != null)
        {
            OdfFormulaDependency dependency = CreateDependency(
                node.Reference,
                ownerSheet);
            if (!ContainsDependency(dependencies, dependency))
            {
                dependencies.Add(dependency);
            }
        }
        int index = 0;
        while (index < node.Children.Length)
        {
            Collect(node.Children[index], ownerSheet, dependencies);
            index = index + 1;
        }
    }

    static OdfFormulaDependency CreateDependency(
        OdfFormulaReference reference,
        string ownerSheet)
    {
        OdfFormulaDependency result = new OdfFormulaDependency();
        result.Reference = reference;
        result.LexicalValue = reference.LexicalValue;
        result.OwnerSheet = ownerSheet;
        result.IsExternal = reference.HasSource;
        result.IsRange = reference.HasEnd
            || reference.Kind == OdfFormulaNodeKind.Range;
        if (reference.Name.Length > 0)
        {
            string sheet = reference.NameSheet.Length > 0
                ? reference.NameSheet
                : ownerSheet;
            result.Key = sheet.Length > 0
                ? sheet + ".$$" + reference.Name
                : "$$" + reference.Name;
        }
        else if (!reference.HasEnd
            && reference.Start.HasColumn
            && reference.Start.HasRow)
        {
            string sheet = reference.Start.EffectiveSheetName.Length > 0
                ? reference.Start.EffectiveSheetName
                : ownerSheet;
            result.Key = CellKey(sheet, reference.Start);
        }
        return result;
    }

    static string CellKey(
        string sheet,
        OdfFormulaReferenceEndpoint endpoint)
    {
        string coordinate = endpoint.ColumnName
            + endpoint.RowNumber.ToString(
                global::System.Globalization.CultureInfo.InvariantCulture);
        return sheet.Length > 0 ? sheet + "." + coordinate : coordinate;
    }

    static bool ContainsDependency(
        List<OdfFormulaDependency> dependencies,
        OdfFormulaDependency candidate)
    {
        int index = 0;
        while (index < dependencies.Count)
        {
            OdfFormulaDependency current = dependencies[index];
            bool sameIdentity = candidate.Key.Length > 0
                ? current.Key == candidate.Key
                : current.LexicalValue == candidate.LexicalValue;
            if (sameIdentity
                && current.IsExternal == candidate.IsExternal)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
