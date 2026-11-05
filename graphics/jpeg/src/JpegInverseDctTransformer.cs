namespace jpeg;

internal class JpegInverseDctTransformer
{
    internal IJpegInverseDct Accurate = new JpegAccurateIntegerInverseDct();
    internal IJpegInverseDct Fast = new JpegFastIntegerInverseDct();
    internal JpegScaledIntegerInverseDct Scaled = new JpegScaledIntegerInverseDct();

    internal int[] InverseTransformBlock(int[] coefficients, JpegReadOptions options)
    {
        IJpegInverseDct transformer = ResolveTransformer(options);
        return transformer.InverseTransformBlock(coefficients);
    }

    internal byte[] InverseTransformBlockToSamples(int[] coefficients, JpegReadOptions options)
    {
        IJpegInverseDct transformer = ResolveTransformer(options);
        return transformer.InverseTransformBlockToSamples(coefficients);
    }

    internal int[] InverseTransformScaledBlock(int[] coefficients, JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        return Scaled.InverseTransformBlock(coefficients, options.IdctScaleNumerator, options.IdctScaleDenominator);
    }

    internal byte[] InverseTransformBlockToScaledSamples(int[] coefficients, JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        return Scaled.InverseTransformBlockToSamples(coefficients, options.IdctScaleNumerator, options.IdctScaleDenominator);
    }

    internal void CopyInverseTransformBlock(int[] coefficients, int[] destination, JpegReadOptions options)
    {
        IJpegInverseDct transformer = ResolveTransformer(options);
        transformer.CopyInverseTransformBlock(coefficients, destination);
    }

    internal void CopyInverseTransformBlockToSamples(int[] coefficients, byte[] destination, JpegReadOptions options)
    {
        IJpegInverseDct transformer = ResolveTransformer(options);
        transformer.CopyInverseTransformBlockToSamples(coefficients, destination);
    }

    internal void CopyInverseTransformScaledBlock(int[] coefficients, int[] destination, JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        Scaled.CopyInverseTransformBlock(coefficients, destination, options.IdctScaleNumerator, options.IdctScaleDenominator);
    }

    internal void CopyInverseTransformBlockToScaledSamples(int[] coefficients, byte[] destination, JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        Scaled.CopyInverseTransformBlockToSamples(coefficients, destination, options.IdctScaleNumerator, options.IdctScaleDenominator);
    }

    internal JpegInverseDctMethod ResolveMethod(JpegReadOptions options)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.InverseDctMethod == JpegInverseDctMethod.Accurate)
        {
            return JpegInverseDctMethod.Accurate;
        }

        if (options.InverseDctMethod == JpegInverseDctMethod.Fast)
        {
            return JpegInverseDctMethod.Fast;
        }

        throw new System.ArgumentOutOfRangeException("InverseDctMethod");
    }

    internal IJpegInverseDct ResolveTransformer(JpegReadOptions options)
    {
        JpegInverseDctMethod method = ResolveMethod(options);
        if (method == JpegInverseDctMethod.Fast)
        {
            return Fast;
        }

        return Accurate;
    }
}
