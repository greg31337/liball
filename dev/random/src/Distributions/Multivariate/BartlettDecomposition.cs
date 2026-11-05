namespace random;

internal class BartlettDecomposition
{
    NormalSampler normalSampler;
    ChiSampler[] diagonalSamplers;
    int dimension;

    internal BartlettDecomposition(IRandomSource generator, double degreesOfFreedom, int dimension)
    {
        normalSampler = new NormalSampler(generator, 0.0, 1.0);
        diagonalSamplers = new ChiSampler[dimension];
        this.dimension = dimension;
        int row = 0;
        while (row < dimension)
        {
            diagonalSamplers[row] = new ChiSampler(generator, degreesOfFreedom - (double)row);
            row++;
        }
    }

    internal void Sample(double[] destination)
    {
        int row = 0;
        while (row < dimension)
        {
            int column = 0;
            while (column < row)
            {
                destination[row * dimension + column] = normalSampler.Sample();
                column++;
            }

            destination[row * dimension + row] = diagonalSamplers[row].Sample();
            column = row + 1;
            while (column < dimension)
            {
                destination[row * dimension + column] = 0.0;
                column++;
            }

            row++;
        }
    }

}
