namespace avif;

internal class Av1TransformBlockEncodeRequest
{
    internal Av1TransformSize TransformSize;
    internal Av1TransformType TransformType;
    internal int Plane;
    internal int BitDepth;
    internal int QIndex;
    internal int DcDelta;
    internal int AcDelta;
    internal bool Lossless;
    internal bool UseQuantizerMatrix;
    internal int MatrixLevel;
    internal int TransformBlockSkipContext;
    internal int DcSignContext;
    internal int[] Residual;

    internal Av1TransformBlockEncodeRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        BitDepth = 8;
        MatrixLevel = Av1SyntaxConstants.NumQuantizerMatrixLevels - 1;
        Residual = System.Array.Empty<int>();
    }
}

internal class Av1TransformBlockEncodeResult
{
    internal AvifResultCode Code;
    internal int[] TransformCoefficients;
    internal int[] QuantizedCoefficients;
    internal int[] DequantizedCoefficients;
    internal int[] ReconstructedResidual;
    internal int EndOfBlock;
    internal Av1CoefficientTokenizationResult? Tokenization;

    internal Av1TransformBlockEncodeResult(AvifResultCode code)
    {
        Code = code;
        TransformCoefficients = System.Array.Empty<int>();
        QuantizedCoefficients = System.Array.Empty<int>();
        DequantizedCoefficients = System.Array.Empty<int>();
        ReconstructedResidual = System.Array.Empty<int>();
    }
}

internal static class Av1TransformBlockEncoder
{
    internal static Av1TransformBlockEncodeResult Encode(
        Av1TransformBlockEncodeRequest request)
    {
        if (request == null)
        {
            return new Av1TransformBlockEncodeResult(
                AvifResultCode.InvalidArgument);
        }

        Av1ForwardTransformRequest transformRequest =
            new Av1ForwardTransformRequest();
        transformRequest.TransformSize = request.TransformSize;
        transformRequest.TransformType = request.TransformType;
        transformRequest.BitDepth = request.BitDepth;
        transformRequest.Residual = request.Residual;
        transformRequest.Lossless = request.Lossless;
        Av1ForwardTransformResult transformed =
            Av1ForwardTransformer.Transform(transformRequest);
        if (transformed.Code != AvifResultCode.Success)
        {
            return new Av1TransformBlockEncodeResult(transformed.Code);
        }

        Av1CoefficientQuantizationRequest quantizationRequest =
            new Av1CoefficientQuantizationRequest();
        quantizationRequest.TransformSize = request.TransformSize;
        quantizationRequest.TransformType = request.TransformType;
        quantizationRequest.Plane = request.Plane;
        quantizationRequest.BitDepth = request.BitDepth;
        quantizationRequest.QIndex = request.QIndex;
        quantizationRequest.DcDelta = request.DcDelta;
        quantizationRequest.AcDelta = request.AcDelta;
        quantizationRequest.Lossless = request.Lossless;
        quantizationRequest.UseQuantizerMatrix = request.UseQuantizerMatrix;
        quantizationRequest.MatrixLevel = request.MatrixLevel;
        quantizationRequest.Coefficients = transformed.Coefficients;
        Av1CoefficientQuantizationResult quantized =
            Av1CoefficientQuantizer.Quantize(quantizationRequest);
        if (quantized.Code != AvifResultCode.Success)
        {
            return new Av1TransformBlockEncodeResult(quantized.Code);
        }

        Av1CoefficientTokenizationRequest tokenRequest =
            new Av1CoefficientTokenizationRequest();
        tokenRequest.TransformSize = request.TransformSize;
        tokenRequest.TransformType = request.TransformType;
        tokenRequest.Plane = request.Plane;
        tokenRequest.TransformBlockSkipContext =
            request.TransformBlockSkipContext;
        tokenRequest.DcSignContext = request.DcSignContext;
        tokenRequest.QuantizedCoefficients = quantized.QuantizedCoefficients;
        tokenRequest.EndOfBlock = quantized.EndOfBlock;
        Av1CoefficientTokenizationResult tokenization =
            Av1CoefficientTokenizer.Tokenize(tokenRequest);
        if (tokenization.Code != AvifResultCode.Success)
        {
            return new Av1TransformBlockEncodeResult(tokenization.Code);
        }

        Av1InverseTransformRequest inverseRequest =
            new Av1InverseTransformRequest();
        inverseRequest.TransformSize = request.TransformSize;
        inverseRequest.TransformType = request.TransformType;
        inverseRequest.BitDepth = request.BitDepth;
        inverseRequest.Coefficients = quantized.DequantizedCoefficients;
        inverseRequest.EndOfBlock = quantized.EndOfBlock;
        inverseRequest.Lossless = request.Lossless;
        Av1InverseTransformResult reconstructed =
            Av1InverseTransformer.Transform(inverseRequest);
        if (reconstructed.Code != AvifResultCode.Success)
        {
            return new Av1TransformBlockEncodeResult(reconstructed.Code);
        }

        Av1TransformBlockEncodeResult result =
            new Av1TransformBlockEncodeResult(AvifResultCode.Success);
        result.TransformCoefficients = transformed.Coefficients;
        result.QuantizedCoefficients = quantized.QuantizedCoefficients;
        result.DequantizedCoefficients = quantized.DequantizedCoefficients;
        result.ReconstructedResidual = reconstructed.Residual;
        result.EndOfBlock = quantized.EndOfBlock;
        result.Tokenization = tokenization;
        return result;
    }
}
