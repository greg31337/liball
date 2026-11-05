namespace pdf;

internal class PdfPathCommand
{
    internal string OperatorName;
    internal double X1;
    internal double Y1;
    internal double X2;
    internal double Y2;
    internal double X3;
    internal double Y3;

    internal PdfPathCommand(string operatorName, double x1, double y1, double x2, double y2, double x3, double y3)
    {
        OperatorName = operatorName;
        X1 = x1;
        Y1 = y1;
        X2 = x2;
        Y2 = y2;
        X3 = x3;
        Y3 = y3;
    }
}
