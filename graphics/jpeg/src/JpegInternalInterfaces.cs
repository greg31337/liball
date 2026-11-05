namespace jpeg;

internal interface IJpegInverseDct
{
    int[] InverseTransformBlock(int[] coefficients);

    byte[] InverseTransformBlockToSamples(int[] coefficients);

    void CopyInverseTransformBlock(int[] coefficients, int[] destination);

    void CopyInverseTransformBlockToSamples(int[] coefficients, byte[] destination);
}

internal interface IJpegChromaUpsampler
{
    JpegPlanarComponentBuffer UpsampleComponent(JpegPlanarComponentBuffer source, int outputWidth, int outputHeight);
}
