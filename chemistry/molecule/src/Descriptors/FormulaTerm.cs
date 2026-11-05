using System.Collections.Generic;
using System.Text;

namespace molecule;


internal class FormulaTerm
{
    internal int AtomicNumber;
    internal int IsotopeMassNumber;
    internal string Symbol;
    internal int Count;
    internal bool IsIsotope;

    internal FormulaTerm(int atomicNumber, int isotopeMassNumber, string symbol, int count, bool isIsotope)
    {
        AtomicNumber = atomicNumber;
        IsotopeMassNumber = isotopeMassNumber;
        Symbol = symbol;
        Count = count;
        IsIsotope = isIsotope;
    }
}

