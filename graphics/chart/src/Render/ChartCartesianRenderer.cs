namespace chart;
internal class ChartCartesianLineClip
{
    internal bool Visible;
    internal ChartPoint Start;
    internal ChartPoint End;
    internal ChartCartesianLineClip(bool visible, ChartPoint start, ChartPoint end)
    {
        if (start == null)
        {
            throw new System.ArgumentNullException("start");
        }

        if (end == null)
        {
            throw new System.ArgumentNullException("end");
        }

        Visible = visible;
        Start = start.Copy();
        End = end.Copy();
    }
}
