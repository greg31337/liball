namespace svg;

internal class SvgStyleCollectionBudget
{
    internal long Characters;
    internal int Rules;
    internal int Declarations;

    internal SvgStyleCollectionBudget()
    {
        Characters = 0;
        Rules = 0;
        Declarations = 0;
    }
}
