namespace svg;

internal class SvgStructuralTextState
{
    public System.Text.StringBuilder Builder;
    public long Characters;
    public int Nodes;
    public bool LimitExceeded;

    public SvgStructuralTextState()
    {
        Builder = new System.Text.StringBuilder();
        Characters = 0;
        Nodes = 0;
        LimitExceeded = false;
    }
}
