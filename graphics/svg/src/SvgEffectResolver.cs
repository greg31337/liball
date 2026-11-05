namespace svg;

internal class SvgEffectTraversal
{
    public System.Collections.Generic.List<SvgNode> Active;
    public int ReferenceCount;
    public int ContentCount;

    public SvgEffectTraversal()
    {
        Active = new System.Collections.Generic.List<SvgNode>();
        ReferenceCount = 0;
        ContentCount = 0;
    }
}
