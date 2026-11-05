namespace svg;

internal class SvgProcessPlan
{
    internal System.Collections.Generic.List<SvgProcessJob> Jobs;
    internal SvgProcessError Error;
    internal string Message;

    internal SvgProcessPlan()
    {
        Jobs = new System.Collections.Generic.List<SvgProcessJob>();
        Error = SvgProcessError.None;
        Message = "";
    }
}
