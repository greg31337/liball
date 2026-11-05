using System.Diagnostics;

namespace molecule;

internal class MaximumCommonSubstructureSearchState
{
    internal Molecule Left;
    internal Molecule Right;
    internal MaximumCommonSubstructureOptions Options;
    internal MaximumCommonSubstructureResult Result;
    internal int[] Mapping;
    internal int[] BestMapping;
    internal bool[] UsedRightAtoms;
    internal Stopwatch Timer;
    internal int CurrentAtomCount;
    internal bool Stop;

    internal MaximumCommonSubstructureSearchState()
    {
        Left = new Molecule();
        Right = new Molecule();
        Options = new MaximumCommonSubstructureOptions();
        Result = new MaximumCommonSubstructureResult();
        Mapping = new int[0];
        BestMapping = new int[0];
        UsedRightAtoms = new bool[0];
        Timer = new Stopwatch();
        CurrentAtomCount = 0;
        Stop = false;
    }
}
