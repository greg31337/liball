namespace qrcode;

internal class GridRefinementResult
{
    internal PerspectiveTransform Transform = new PerspectiveTransform();

    internal QrMatrix Matrix = new QrMatrix();

    internal bool Improved;

    internal double InitialScore;

    internal double FinalScore;

    internal int Iterations;

    internal int Evaluations;
}
