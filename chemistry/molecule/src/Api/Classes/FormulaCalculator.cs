using System.Collections.Generic;
using System.Text;

namespace molecule;


/// <summary>
/// Calculates formula information from molecular graphs.
/// </summary>
public class FormulaCalculator : IFormulaCalculator
{
    internal ElementTable Elements;

    /// <summary>
    /// Initializes a new instance of the <see cref="FormulaCalculator"/> class.
    /// </summary>
    public FormulaCalculator()
    {
        Elements = new ElementTable();
    }

    /// <summary>
    /// Returns the Hill-system molecular formula without isotope labels.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string Formula(Molecule? molecule)
    {
        return CreateFormula(molecule, false);
    }

    /// <summary>
    /// Returns the Hill-system molecular formula with explicit isotope labels.
    /// </summary>
    /// <param name="molecule">The molecule to process.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    public string IsotopeFormula(Molecule? molecule)
    {
        return CreateFormula(molecule, true);
    }

    internal string CreateFormula(Molecule? molecule, bool includeIsotopes)
    {
        List<FormulaTerm> terms = CreateTerms(molecule, includeIsotopes);
        SortTerms(terms, ContainsCarbon(terms));
        return WriteFormula(terms);
    }

    internal List<FormulaTerm> CreateTerms(Molecule? molecule, bool includeIsotopes)
    {
        List<FormulaTerm> terms = new List<FormulaTerm>();

        if (molecule == null)
        {
            return terms;
        }

        int atomIndex = 0;
        while (atomIndex < molecule.Atoms.Count)
        {
            Atom atom = molecule.Atoms[atomIndex];
            AddAtomTerm(terms, atom, includeIsotopes);

            int hydrogenCount = atom.ExplicitHydrogenCount + atom.ImplicitHydrogenCount;
            if (hydrogenCount > 0)
            {
                AddTerm(terms, 1, 0, "H", hydrogenCount, false);
            }

            atomIndex = atomIndex + 1;
        }

        return terms;
    }

    internal void AddAtomTerm(List<FormulaTerm> terms, Atom? atom, bool includeIsotopes)
    {
        if (atom == null)
        {
            return;
        }

        if (atom.AtomicNumber <= 0)
        {
            return;
        }

        Element element = Elements.GetElement(atom.AtomicNumber);
        if (element.AtomicNumber == 0)
        {
            return;
        }

        int isotopeMassNumber = 0;
        bool isIsotope = false;
        string symbol = element.Symbol;

        if (includeIsotopes && atom.IsotopeMassNumber > 0)
        {
            isotopeMassNumber = atom.IsotopeMassNumber;
            isIsotope = true;
            symbol = "[" + NumberFormatting.FormatInt(atom.IsotopeMassNumber) + element.Symbol + "]";
        }

        AddTerm(terms, atom.AtomicNumber, isotopeMassNumber, symbol, 1, isIsotope);
    }

    internal void AddTerm(List<FormulaTerm> terms, int atomicNumber, int isotopeMassNumber, string symbol, int count, bool isIsotope)
    {
        if (count <= 0)
        {
            return;
        }

        int index = 0;
        while (index < terms.Count)
        {
            FormulaTerm term = terms[index];
            if (term.AtomicNumber == atomicNumber && term.IsotopeMassNumber == isotopeMassNumber && term.Symbol == symbol)
            {
                term.Count = term.Count + count;
                return;
            }

            index = index + 1;
        }

        terms.Add(new FormulaTerm(atomicNumber, isotopeMassNumber, symbol, count, isIsotope));
    }

    internal bool ContainsCarbon(List<FormulaTerm> terms)
    {
        int index = 0;
        while (index < terms.Count)
        {
            if (terms[index].AtomicNumber == 6)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal void SortTerms(List<FormulaTerm> terms, bool hasCarbon)
    {
        int index = 1;
        while (index < terms.Count)
        {
            FormulaTerm value = terms[index];
            int scan = index - 1;

            while (scan >= 0 && CompareTerms(terms[scan], value, hasCarbon) > 0)
            {
                terms[scan + 1] = terms[scan];
                scan = scan - 1;
            }

            terms[scan + 1] = value;
            index = index + 1;
        }
    }

    internal int CompareTerms(FormulaTerm left, FormulaTerm right, bool hasCarbon)
    {
        int leftRank = GetHillRank(left, hasCarbon);
        int rightRank = GetHillRank(right, hasCarbon);

        if (leftRank != rightRank)
        {
            return leftRank - rightRank;
        }

        int symbolComparison = string.CompareOrdinal(GetBaseSymbol(left), GetBaseSymbol(right));
        if (symbolComparison != 0)
        {
            return symbolComparison;
        }

        if (left.IsotopeMassNumber != right.IsotopeMassNumber)
        {
            return left.IsotopeMassNumber - right.IsotopeMassNumber;
        }

        return string.CompareOrdinal(left.Symbol, right.Symbol);
    }

    internal int GetHillRank(FormulaTerm term, bool hasCarbon)
    {
        if (hasCarbon)
        {
            if (term.AtomicNumber == 6)
            {
                return 0;
            }

            if (term.AtomicNumber == 1)
            {
                return 1;
            }

            return 2;
        }

        return 2;
    }

    internal string GetBaseSymbol(FormulaTerm term)
    {
        Element element = Elements.GetElement(term.AtomicNumber);
        if (element.AtomicNumber == 0)
        {
            return term.Symbol;
        }

        return element.Symbol;
    }

    internal string WriteFormula(List<FormulaTerm> terms)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < terms.Count)
        {
            FormulaTerm term = terms[index];
            builder.Append(term.Symbol);
            if (term.Count != 1)
            {
                builder.Append(NumberFormatting.FormatInt(term.Count));
            }

            index = index + 1;
        }

        return builder.ToString();
    }
}

